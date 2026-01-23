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
            Console.CancelKeyPress += (sender, e) =>
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    e.Cancel = true;
                }
            };

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
                        .Title("[yellow]Select option (ESC to exit):[/]")
                        .HighlightStyle(new Style(foreground: Color.Black, background: Color.Yellow))
                        .EnableSearch()
                        .AddChoices("CIR Export", "ABIFF Export", "CIR Export (Browser)", "ABIFF Export (Browser)", "<<< Exit >>>"));

                if (exportType == "<<< Exit >>>")
                    break;

                if (exportType == "CIR Export (Browser)" || exportType == "ABIFF Export (Browser)")
                {
                    try
                    {
                        var names = rdbController.GetNames();
                        var resourceType = exportType == "CIR Export (Browser)"
                            ? AODB.Common.RDBObjects.ResourceTypeId.CatMesh
                            : AODB.Common.RDBObjects.ResourceTypeId.RdbMesh;

                        var modelDict = names[resourceType];

                        BrowseAndExportModels(modelDict, rdbController, exportDir, exportType.StartsWith("CIR"));
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Browse failed: {ex.Message}[/]");
                    }

                    continue;
                }

                AnsiConsole.MarkupLine("[dim]Press ESC to go back[/]");
                var modelIdInput = PromptWithEscape("[yellow]Enter model ID:[/]");

                if (modelIdInput == null)
                    continue;

                if (!int.TryParse(modelIdInput, out int modelId) || modelId <= 0)
                {
                    AnsiConsole.MarkupLine("[red]Model ID must be a positive number[/]");
                    continue;
                }

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

        private static string? PromptWithEscape(string promptText)
        {
            var input = "";
            AnsiConsole.Markup(promptText + " ");

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    AnsiConsole.WriteLine();
                    return null;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    AnsiConsole.WriteLine();
                    return input;
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }
        }

        private static void BrowseAndExportModels(Dictionary<int, string> models, RdbController rdbController, string exportDir, bool isCir)
        {
            while (true)
            {
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine("[dim]Type to search, ENTER to select, ESC to return to main menu[/]");

                var searchResult = LiveSearchAndSelect(models);

                if (searchResult == null)
                    break;

                var modelId = searchResult.Value;

                try
                {
                    bool success = false;

                    AnsiConsole.Status()
                        .Start($"Exporting model {modelId}...", ctx =>
                        {
                            ctx.Spinner(Spinner.Known.Dots);
                            ctx.SpinnerStyle(Style.Parse("yellow"));

                            if (isCir)
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
            }
        }

        private static int? LiveSearchAndSelect(Dictionary<int, string> models)
        {
            string searchTerm = "";
            int selectedIndex = 0;
            int scrollOffset = 0;
            int startLine = Console.CursorTop;
            const int maxDisplay = 15;

            while (true)
            {
                var filteredModels = string.IsNullOrWhiteSpace(searchTerm)
                    ? models.ToList()
                    : models.Where(kvp =>
                        kvp.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.ToString().Contains(searchTerm))
                      .ToList();

                if (selectedIndex >= filteredModels.Count)
                    selectedIndex = Math.Max(0, filteredModels.Count - 1);

                if (selectedIndex < scrollOffset)
                {
                    scrollOffset = selectedIndex;
                }
                else if (selectedIndex >= scrollOffset + maxDisplay)
                {
                    scrollOffset = selectedIndex - maxDisplay + 1;
                }

                Console.SetCursorPosition(0, startLine);
                for (int i = 0; i < maxDisplay + 3; i++)
                {
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.WriteLine();
                }
                Console.SetCursorPosition(0, startLine);

                AnsiConsole.MarkupLine($"[yellow]Search:[/] {searchTerm}_");
                AnsiConsole.MarkupLine($"[green]Found {filteredModels.Count} model(s)[/]");

                int displayCount = Math.Min(maxDisplay, filteredModels.Count - scrollOffset);
                for (int i = 0; i < displayCount; i++)
                {
                    int modelIndex = scrollOffset + i;
                    var model = filteredModels[modelIndex];
                    if (modelIndex == selectedIndex)
                    {
                        AnsiConsole.MarkupLine($"[black on yellow]> {model.Key} - {model.Value.EscapeMarkup()}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"  {model.Key} - {model.Value.EscapeMarkup()}");
                    }
                }

                if (scrollOffset > 0 || filteredModels.Count > scrollOffset + maxDisplay)
                {
                    string scrollInfo = "";
                    if (scrollOffset > 0)
                        scrollInfo += "[dim]↑ More above[/] ";
                    if (filteredModels.Count > scrollOffset + maxDisplay)
                        scrollInfo += $"[dim]↓ More below ({filteredModels.Count - scrollOffset - maxDisplay} more)[/]";

                    if (!string.IsNullOrEmpty(scrollInfo))
                        AnsiConsole.MarkupLine(scrollInfo);
                }

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    Console.SetCursorPosition(0, startLine);
                    for (int i = 0; i < maxDisplay + 3; i++)
                    {
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.WriteLine();
                    }
                    Console.SetCursorPosition(0, startLine);
                    return null;
                }
                else if (key.Key == ConsoleKey.Enter && filteredModels.Count > 0)
                {
                    Console.SetCursorPosition(0, startLine);
                    for (int i = 0; i < maxDisplay + 3; i++)
                    {
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.WriteLine();
                    }
                    Console.SetCursorPosition(0, startLine);
                    return filteredModels[selectedIndex].Key;
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex = Math.Max(0, selectedIndex - 1);
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex = Math.Min(filteredModels.Count - 1, selectedIndex + 1);
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    selectedIndex = Math.Max(0, selectedIndex - maxDisplay);
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    selectedIndex = Math.Min(filteredModels.Count - 1, selectedIndex + maxDisplay);
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    selectedIndex = 0;
                }
                else if (key.Key == ConsoleKey.End)
                {
                    selectedIndex = filteredModels.Count - 1;
                }
                else if (key.Key == ConsoleKey.Backspace && searchTerm.Length > 0)
                {
                    searchTerm = searchTerm.Substring(0, searchTerm.Length - 1);
                    selectedIndex = 0;
                    scrollOffset = 0;
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    searchTerm += key.KeyChar;
                    selectedIndex = 0;
                    scrollOffset = 0;
                }
            }
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