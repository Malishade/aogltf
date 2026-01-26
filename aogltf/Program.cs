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
        var names = rdbController.GetNames();

        ConsoleLoadingMenu
            .Create(_title, $"Exporting abiff / cir names ...")
            .WithSuccessMessage(objectName => $"Names exported to : {exportDir}")
            .WithErrorMessage(error => $"Failed to export names: {error}")
            .Show(() => {
                bool success = SaveNameJson(names[AODB.Common.RDBObjects.ResourceTypeId.CatMesh], exportDir, "CirNames") &&
                SaveNameJson(names[AODB.Common.RDBObjects.ResourceTypeId.RdbMesh], exportDir, "AbiffNames");
                return (success);
            });
    }

    private static bool SaveNameJson(Dictionary<int,string> names, string exportDir, string fileName)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            File.WriteAllText(Path.Combine(exportDir, $"{fileName}.json"), JsonSerializer.Serialize(names, jsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private static void HandleBrowser(RdbController rdbController, string exportDir, bool isCir)
    {
        try
        {
            var names = rdbController.GetNames();
            var resourceType = isCir
                ? AODB.Common.RDBObjects.ResourceTypeId.CatMesh
                : AODB.Common.RDBObjects.ResourceTypeId.RdbMesh;

            ConsoleSelectionMenu
                .Create<KeyValuePair<int, string>>()
                .WithItems(names[resourceType])
                .WithDynamicHeight()
                .WithShowInfo()
                .WithTitle(isCir ? "Cir Browser" : "Abiff Browser")
                .WithDisplayFunc(kvp => $"{kvp.Key} - {kvp.Value}")
                .WithFilterFunc((search, kvp) =>
                    kvp.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.ToString().Contains(search))
                .EnableSearch()
                .OnSelect(searchResult =>
                {
                    var modelId = searchResult.Key;

                    ConsoleLoadingMenu
                        .Create(_title, $"Exporting model {modelId}...")
                        .WithSuccessMessage(objectName => $"Model exported to: {exportDir}\\{objectName}.glb")
                        .WithErrorMessage(error => $"Failed to export model {modelId}: {error}")
                        .Show(() => {
                            bool success = isCir
                                ? new CirExporter(rdbController).ExportGlb(exportDir, modelId, out string objectName)
                                : new AbiffExporter(rdbController).ExportGlb(exportDir, modelId, out objectName);

                            return (success, objectName);
                        });

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