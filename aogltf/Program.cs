using AODB;
using ConsoleUI;
using gltf;
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
        RdbController rdbController;

        ConsoleInputPrompt
            .Create()
            .WithTitle(_title)
            .WithPrompt("Anarchy Online installation path:")
            .WithDefaultValue(string.IsNullOrEmpty(config.AoPath) ? "" : config.AoPath)
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

        ConsoleInputPrompt
            .Create()
            .WithTitle(_title)
            .WithPrompt("Export path:")
            .WithDefaultValue(string.IsNullOrEmpty(config.ExportPath) ?
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AOExport") :
                config.ExportPath)
            .WithValidator(path =>
            {
                if (string.IsNullOrWhiteSpace(path))
                    return (false, "Path is empty");

                try
                {
                    var fullPath = Path.GetFullPath(path);
                    var root = Path.GetPathRoot(fullPath);

                    if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                        return (false, $"Drive does not exist: {root}");

                    Directory.CreateDirectory(fullPath);

                    return (true, "Success");
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            })
            .OnInput(exportPath =>
            {
                config.ExportPath = exportPath;
                config.SaveConfig(configPath);
            })
            .Show();

        rdbController = new RdbController(config.AoPath!);

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
                        HandleNameDump(rdbController, config.ExportPath);
                        return true;
                    case ExportOption.CirExport:
                        HandleBrowser(rdbController, config.ExportPath, true);
                        return true;
                    case ExportOption.AbiffExport:
                        HandleBrowser(rdbController, config.ExportPath, false);
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

    private static void HandleNameDump(RdbController rdbController, string exportDir)
    {
        var names = rdbController.GetNames();

        ConsoleLoadingMenu
            .Create(_title, $"Exporting abiff / cir names ...")
            .WithSuccessMessage(objectName => $"Names exported to : {exportDir}")
            .WithErrorMessage(error => $"Failed to export names: {error}")
            .Show(() => 
            {
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
        var names = rdbController.GetNames();
        var resourceType = isCir
            ? AODB.Common.RDBObjects.ResourceTypeId.CatMesh
            : AODB.Common.RDBObjects.ResourceTypeId.RdbMesh;

        var width = 80;
        ConsoleSelectionMenu
            .Create<KeyValuePair<int, string>>()
            .WithBorderWidth(width)
            .WithItems(names[resourceType])
            .WithDynamicHeight()
            .WithShowInfo()
            .WithTitle(isCir ? "Cir Browser" : "Abiff Browser")
            .WithHeaders([new Header("ID", 0.15f), new Header("Name", 0.85f)])
            .WithDisplayFunc(kvp => $"{kvp.Key.ToString().PadRight((int)(width * 0.15f - 1))} {kvp.Value}")
            .WithFilterFunc((search, kvp) =>
                kvp.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.ToString().Contains(search))
            .EnableSearch()
            .KeepOpen()
            .OnSelect(searchResult =>
            {
                var modelId = searchResult.Key;

                ConsoleSelectionMenu
                    .Create<FileFormat>()
                    .WithBorderWidth(35)
                    .WithItems(Enum.GetValues<FileFormat>())
                    .WithTitle("Select file format")
                    .WithDisplayFunc(opt => opt switch
                    {
                        FileFormat.Glb => "Glb",
                        FileFormat.Gltf => "Gltf",
                        _ => opt.ToString()
                    })
                    .OnSelect(fileFormat =>
                    {
                        ConsoleLoadingMenu
                            .Create(_title, $"Exporting model {modelId}...")
                            .WithSuccessMessage(objectName => $"Model exported to: {exportDir}\\{objectName}.{fileFormat.ToString().ToLower()}")
                            .WithErrorMessage(error => $"Failed to export model {modelId}: {error}")
                            .Show(() => 
                            {
                                bool success = isCir
                                    ? new CirExporter(rdbController).Export(exportDir, modelId, fileFormat, out string objectName)
                                    : new AbiffExporter(rdbController).Export(exportDir, modelId, fileFormat, out objectName);

                                return (success, objectName);
                            });
                    })
                    .Show();
            })
            .Show();
    }
}