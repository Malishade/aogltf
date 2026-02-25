using AODB;
using ConsoleUI;
using gltf;
using System.Text.Json;

namespace aogltf;

internal enum Option
{
    CirExport,
    AbiffExport,
    PlayfieldBrowser,
    DumpIds,
    ExportOptions,
    Exit
}

internal enum ExportOptionSetting
{
    GamePath,
    ExportPath,
    FileFormat,
    FlipX,
    FlipY,
    FlipZ,
    Back
}

internal class Program
{
    private static string _title = "AO glTF Model Exporter";

    static void Main(string[] args)
    {
        LoadUI();
    }

    private static void LoadUI()
    {
        Console.WindowHeight = 40;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.CursorVisible = false;
        Console.ResetColor();

        string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        Config config = Config.LoadConfig(configPath);

        ConsoleSelectionMenu
            .Create<Option>()
            .WithBorderWidth(35)
            .WithItems(Enum.GetValues<Option>())
            .WithTitle(_title)
            .WithDisplayFunc(opt => opt switch
            {
                Option.CirExport => "CIR Export",
                Option.AbiffExport => "ABIFF Export",
                Option.PlayfieldBrowser => "Playfield Export",
                Option.DumpIds => "Dump CIR / ABIFF IDs",
                Option.ExportOptions => "Export Options",
                Option.Exit => "Exit",
                _ => opt.ToString()
            })
            .OnSelect(exportType =>
            {
                switch (exportType)
                {
                    case Option.Exit:
                        return false;
                    case Option.ExportOptions:
                        HandleExportOptions(config, configPath);
                        return true;
                    case Option.DumpIds:
                        if (!ValidateConfig(config)) return true;
                        HandleNameDump(new RdbController(config.AoPath!), config.ExportPath!);
                        return true;
                    case Option.CirExport:
                        if (!ValidateConfig(config)) return true;
                        HandleBrowser(new RdbController(config.AoPath!), config, configPath, true);
                        return true;
                    case Option.AbiffExport:
                        if (!ValidateConfig(config)) return true;
                        HandleBrowser(new RdbController(config.AoPath!), config, configPath, false);
                        return true;
                    case Option.PlayfieldBrowser:
                        if (!ValidateConfig(config)) return true;
                        HandlePlayfieldBrowser(config);
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
    private static void HandlePlayfieldBrowser(Config config)
    {
        string binPath = Path.Combine(AppContext.BaseDirectory, "ZoneIdToZoneName.bin");
        Dictionary<int, string> zones = ZoneNameSerializer.DeserializeCompressed(File.ReadAllBytes(binPath));

        var width = 80;
        ConsoleSelectionMenu
            .Create<KeyValuePair<int, string>>()
            .WithBorderWidth(width)
            .WithItems(zones)
            .WithDynamicHeight()
            .WithShowInfo()
            .WithTitle("Playfield Export")
            .WithHeaders([new Header("ID", 0.15f), new Header("Name", 0.85f)])
            .WithDisplayFunc(kvp => $"{kvp.Key.ToString().PadRight((int)(width * 0.15f - 1))} {kvp.Value}")
            .WithFilterFunc((search, kvp) =>
                kvp.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.ToString().Contains(search))
            .EnableSearch()
            .KeepOpen()
           .OnSelect(kvp =>
           {
               var playfieldId = kvp.Key;
               var outputPath = Path.Combine(config.ExportPath!, $"Playfield_{playfieldId}");
               Directory.CreateDirectory(outputPath);
               ConsoleLoadingMenu
                   .Create(_title, $"Exporting playfield {playfieldId}. [{config.FileFormat}] [{config.ExportTransforms}]")
                   .WithSuccessMessage(_ => $"Playfield exported to: {outputPath}")
                   .WithErrorMessage(error => $"Failed to export playfield {playfieldId}: {error}")
                   .Show(() =>
                   {
                       bool success = new PlayfieldExporter(new RdbController(config.AoPath!)).Export(playfieldId, outputPath, config.FileFormat, config.ExportTransforms);
                       return (success);
                   });
           })
            .Show();
    }

    private static bool ValidateConfig(Config config)
    {
        if (string.IsNullOrEmpty(config.AoPath) || string.IsNullOrEmpty(config.ExportPath))
        {
            ConsoleMessageBox
                .Create()
                .WithTitle(_title)
                .WithMessage("Please set 'Export Options'")
                .WithMessageColor(ConsoleUI.Color.Red)
                .Show();
            return false;
        }
        return true;
    }

    private static void HandleExportOptions(Config config, string configPath)
    {
        ConsoleSelectionMenu
            .Create<ExportOptionSetting>()
            .WithBorderWidth(55)
            .WithItems(Enum.GetValues<ExportOptionSetting>())
            .WithTitle("Export Options")
            .WithDisplayFunc(opt => opt switch
            {
                ExportOptionSetting.GamePath => $"Game Path      [{(string.IsNullOrEmpty(config.AoPath) ? "NOT SET" : config.AoPath)}]",
                ExportOptionSetting.ExportPath => $"Export Path    [{(string.IsNullOrEmpty(config.ExportPath) ? "NOT SET" : config.ExportPath)}]",
                ExportOptionSetting.FileFormat => $"File Format    [{config.FileFormat}]",
                ExportOptionSetting.FlipX => $"Flip X         [{(config.ExportTransforms.HasFlag(ExportMirror.MirrorX) ? "ON" : "OFF")}]",
                ExportOptionSetting.FlipY => $"Flip Y         [{(config.ExportTransforms.HasFlag(ExportMirror.MirrorY) ? "ON" : "OFF")}]",
                ExportOptionSetting.FlipZ => $"Flip Z         [{(config.ExportTransforms.HasFlag(ExportMirror.MirrorZ) ? "ON" : "OFF")}]",
                ExportOptionSetting.Back => "Back",
                _ => opt.ToString()
            })
            .OnSelect(setting =>
            {
                switch (setting)
                {
                    case ExportOptionSetting.Back:
                        return false;

                    case ExportOptionSetting.GamePath:
                        ConsoleInputPrompt
                            .Create()
                            .WithTitle("Export Options")
                            .WithPrompt("Anarchy Online installation path:")
                            .WithDefaultValue(config.AoPath ?? "")
                            .WithValidator(path =>
                            {
                                if (!Directory.Exists(path))
                                    return (false, "Directory not found. Please try again.");

                                string rdbPath = Path.Combine(path, "cd_image", "data", "db", "ResourceDatabase.dat");

                                if (!File.Exists(rdbPath))
                                    return (false, "Directory exists but ResourceDatabase.dat not found");

                                return (true, "Success");
                            })
                            .OnInput(aoPath =>
                            {
                                config.AoPath = aoPath;
                                config.SaveConfig(configPath);
                            })
                            .Show();
                        return true;

                    case ExportOptionSetting.ExportPath:
                        ConsoleInputPrompt
                            .Create()
                            .WithTitle("Export Options")
                            .WithPrompt("Export path:")
                            .WithDefaultValue(config.ExportPath ??
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AOExport"))
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
                        return true;

                    case ExportOptionSetting.FileFormat:
                        config.FileFormat = config.FileFormat == FileFormat.Glb ? FileFormat.Gltf : FileFormat.Glb;
                        config.SaveConfig(configPath);
                        return true;

                    case ExportOptionSetting.FlipX:
                        config.ExportTransforms ^= ExportMirror.MirrorX;
                        config.SaveConfig(configPath);
                        return true;

                    case ExportOptionSetting.FlipY:
                        config.ExportTransforms ^= ExportMirror.MirrorY;
                        config.SaveConfig(configPath);
                        return true;

                    case ExportOptionSetting.FlipZ:
                        config.ExportTransforms ^= ExportMirror.MirrorZ;
                        config.SaveConfig(configPath);
                        return true;

                    default:
                        return true;
                }
            })
            .Show();
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

    private static bool SaveNameJson(Dictionary<int, string> names, string exportDir, string fileName)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(Path.Combine(exportDir, $"{fileName}.json"), JsonSerializer.Serialize(names, jsonOptions));
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private static void HandleBrowser(RdbController rdbController, Config config, string configPath, bool isCir)
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
               var modelName = searchResult.Value;
               var outputPath = Path.Combine(config.ExportPath!, modelName);
               Directory.CreateDirectory(outputPath);

               ConsoleLoadingMenu
                   .Create(_title, $"Exporting model {modelId}.  [{config.FileFormat}] [{config.ExportTransforms}]")
                   .WithSuccessMessage(objectName => $"Model exported to: {outputPath}\\{objectName}.{config.FileFormat.ToString().ToLower()}")
                   .WithErrorMessage(error => $"Failed to export model {modelId}: {error}")
                   .Show(() =>
                   {
                       bool success = isCir
                           ? new CirExporter(rdbController).Export(outputPath, modelId, config.FileFormat, out string objectName, config.ExportTransforms)
                           : new AbiffExporter(rdbController).Export(outputPath, modelId, config.FileFormat, out objectName, config.ExportTransforms);

                       return (success, objectName);
                   });
           })
            .Show();
    }
}

//private static void ImportTest()
//{
//    AbiffImporter.Import(new ImportOptions
//    {
//        AOPath = $"D:\\Funcom\\Anarchy Online",
//        ModelPath = $"D:\\AOExport\\cube.gltf",
//        RecordId = 118168
//    });
//}