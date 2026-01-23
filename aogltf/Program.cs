using AODB;
using ConsoleUI;
using System.Text.Json;

namespace aogltf;

internal enum ExportOption
{
    CirExport,
    AbiffExport,
    DumpIds,
    Exit
}

internal class Program
{
    private static string _title = "AO glTF Model Exporter";

    static void Main(string[] args)
    {
        Console.WindowHeight = 40;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.CursorVisible = false;
        Console.ResetColor();

        string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        Config config = Config.LoadConfig(configPath);

        ConsoleInputPrompt
            .Create()
            .WithTitle(_title)
            .WithPrompt("Anarchy Online installation path:")
            .WithDefaultValue(config.AoPath)
            .WithValidator(path =>
            {
                if (!Directory.Exists(path))
                {
                    return (false, "Directory not found. Please try again.");
                }

                string rdbPath = Path.Combine(path, "cd_image", "data", "db", "ResourceDatabase.dat");

                if (!File.Exists(rdbPath))
                {
                    return (false, $"Directory exists but ResourceDatabase.dat not found");
                }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.ResetColor();
                return (true, "Success");
            })
            .OnInput(aoPath =>
            {
                config.AoPath = aoPath;
                config.SaveConfig(configPath);
            })
            .Show();

        var rdbController = new RdbController(config.AoPath!);
        string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AOExport");
        Directory.CreateDirectory(exportDir);

        Console.WriteLine();
        Console.WriteLine();
        ConsoleSelectionMenu
            .Create<ExportOption>()
            .WithBorderWidth(35)
            .WithItems(Enum.GetValues<ExportOption>())
            .WithTitle(_title)
            .WithDisplayFunc(opt => opt switch
            {
                ExportOption.CirExport => "CIR Export",
                ExportOption.AbiffExport => "ABIFF Export",
                ExportOption.DumpIds => "Dump CIR / ABIFF IDs",
                ExportOption.Exit => "Exit",
                _ => opt.ToString()
            })
            .WithLoop()
            .OnSelect(exportType =>
            {
                switch (exportType)
                {
                    case ExportOption.Exit:
                        return false;
                    case ExportOption.DumpIds:
                        DumpNames(rdbController, exportDir);
                        return true;
                    case ExportOption.CirExport:
                        HandleBrowser(rdbController, exportDir, true);
                        return true;
                    case ExportOption.AbiffExport:
                        HandleBrowser(rdbController, exportDir, false);
                        return true;
                    default:
                        return true;
                }
            })
            .Show();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Exiting...");
        Console.ResetColor();
    }

    private static void DumpNames(RdbController rdbController, string exportDir)
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Dumping names...");
            Console.ResetColor();

            var names = rdbController.GetNames();

            File.WriteAllText(Path.Combine(exportDir, "CirNames.json"),
                JsonSerializer.Serialize(names[AODB.Common.RDBObjects.ResourceTypeId.CatMesh],
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            File.WriteAllText(Path.Combine(exportDir, "AbiffNames.json"),
                JsonSerializer.Serialize(names[AODB.Common.RDBObjects.ResourceTypeId.RdbMesh],
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Names dumped successfully to {Path.Combine(exportDir, "CirNames.json")}");
            Console.WriteLine($"Names dumped successfully to {Path.Combine(exportDir, "AbiffNames.json")}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Dump failed: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    private static void HandleBrowser(RdbController rdbController, string exportDir, bool isCir)
    {
        try
        {
            var names = rdbController.GetNames();
            var resourceType = isCir
                ? AODB.Common.RDBObjects.ResourceTypeId.CatMesh
                : AODB.Common.RDBObjects.ResourceTypeId.RdbMesh;

            var modelDict = names[resourceType];
            ConsoleSelectionMenu
                .Create<KeyValuePair<int, string>>()
                .WithItems(modelDict)
                .WithTitle(isCir ? "Cir Browser" : "Abiff Browser")
                .WithDynamicHeight()
                .WithDisplayFunc(kvp => $"{kvp.Key} - {kvp.Value}")
                .WithFilterFunc((search, kvp) =>
                    kvp.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.ToString().Contains(search))
                .EnableSearch()
                .WithLoop()
                .OnSelect(searchResult =>
                {
                    var modelId = searchResult.Key;
                    Console.Clear();

                    try
                    {
                        bool success = false;
                        string objectName = string.Empty;
                        Console.ResetColor();

                        ConsoleBorder
                            .Create(80)
                            .GetCenter(out int startLeft, out int startTop)
                            .WithTopBorderText(_title, ConsoleColor.Yellow)
                            .WithBorderColor(ConsoleColor.DarkGray)
                            .AddLine("")
                            .AddEmptyLine()
                            .AddEmptyLine()
                            .Draw(centered: true);

                        using (var spinner = ConsoleSpinner.Start($"Exporting model {modelId}...", startLeft + 2, startTop, ConsoleColor.Yellow))
                        {
                            success = isCir ?
                                new CirExporter(rdbController).ExportGlb(exportDir, modelId, out objectName) :
                                new AbiffExporter(rdbController).ExportGlb(exportDir, modelId, out objectName);
                        }

                        Console.Clear();

                        ConsoleBorder
                             .Create(80)
                             .WithTopBorderText(_title, ConsoleColor.Yellow)
                             .WithBorderColor(ConsoleColor.DarkGray)
                             .AddEmptyLine()
                             .AddLine(success ? $"Saved at: {exportDir}\\{objectName}.glb" : $"Resource with id {modelId} not found in database / parser error", success ? ConsoleColor.Green : ConsoleColor.Red)
                             .AddEmptyLine()
                             .Draw(centered: true);
                        Console.ResetColor();
                        Console.ReadKey();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Export failed: {ex.Message}");
                        Console.ResetColor();
                    }

                    return true;
                })
                .Show();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Browse failed: {ex.Message}");
            Console.ResetColor();
        }
    }
}