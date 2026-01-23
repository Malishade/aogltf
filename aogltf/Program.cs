using Spectre.Console;
using System.Text.Json;
using AODB;

namespace aogltf
{
    internal class Program
    {
        private class Config
        {
            public string? AoPath { get; set; }
        }

        static void Main(string[] args)
        {
            AnsiConsole.Write(
                new FigletText("AO glTF")
                    .LeftJustified()
                    .Color(Color.Yellow));

            AnsiConsole.Write(
                new FigletText("Model Export")
                    .LeftJustified()
                    .Color(Color.Yellow));

            string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            Config config = LoadConfig(configPath);

            string aoPath;
            while (true)
            {
                string defaultPath = config.AoPath ?? "";

                AnsiConsole.MarkupLine($"[yellow]Enter path to Anarchy Online installation (or press enter to use last path) [green]({defaultPath})[/]:[/]");
                aoPath = AnsiConsole.Prompt(
                    new TextPrompt<string>(string.Empty)
                        .AllowEmpty()
                        .ValidationErrorMessage("[red]Please enter a valid path[/]"));

                if (string.IsNullOrWhiteSpace(aoPath))
                {
                    aoPath = defaultPath;
                }

                if (Directory.Exists(aoPath))
                {
                    string rdbPath = Path.Combine(aoPath, "cd_image", "data", "db", "ResourceDatabase.dat");

                    if (File.Exists(rdbPath))
                    {
                        AnsiConsole.MarkupLine($"[green]Found installation at: {aoPath}[/]");
                        AnsiConsole.MarkupLine($"[green]Found ResourceDatabase.dat[/]");

                        config.AoPath = aoPath;
                        SaveConfig(configPath, config);

                        break;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Directory exists but ResourceDatabase.dat not found at:[/]");
                        AnsiConsole.MarkupLine($"[red]{rdbPath}[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Directory not found. Please try again.[/]");
                }
            }

            var rdbController = new RdbController(aoPath);
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AOExport");
            Directory.CreateDirectory(exportDir);

            while (true)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                var exportType = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Select resource database object type:[/]")
                        .HighlightStyle(new Style(foreground: Color.Black, background: Color.Yellow))
                        .AddChoices("CIR", "ABIFF", "Exit"));

                if (exportType == "Exit")
                    break;

                var modelId = AnsiConsole.Prompt(
                    new TextPrompt<int>("[yellow]Enter model ID:[/]")
                        .ValidationErrorMessage("[red]Please enter a valid number[/]")
                        .Validate(id => id > 0
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Model ID must be positive[/]")));

                try
                {
                    bool success = false;

                    AnsiConsole.Status()
                        .Start($"Exporting model {modelId}...", ctx =>
                        {
                            ctx.Spinner(Spinner.Known.Dots);
                            ctx.SpinnerStyle(Style.Parse("yellow"));

                            if (exportType.StartsWith("CIR"))
                            {
                                var exporter = new CirExporter(rdbController);
                                success = exporter.ExportGlb(exportDir, modelId);
                            }
                            else
                            {
                                var exporter = new AbiffExporter(rdbController);
                                success = exporter.ExportGlb(exportDir, modelId);
                            }
                        });

                    AnsiConsole.MarkupLine(success ? $"[green]Model exported successfully to {exportDir}[/]" : $"[red]Resource with id {modelId} not found in database / parser error[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Export failed: {ex.Message}[/]");
                }

                AnsiConsole.WriteLine();
            }

            AnsiConsole.MarkupLine("[yellow]Exiting...[/]");
        }

        private static Config LoadConfig(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<Config>(json) ?? new Config();
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[dim red]Could not load config: {ex.Message}[/]");
            }

            return new Config();
        }

        private static void SaveConfig(string configPath, Config config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[dim red]Could not save config: {ex.Message}[/]");
            }
        }
    }
}