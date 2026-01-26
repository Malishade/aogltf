using System.Text.Json;

namespace aogltf;

internal class Config
{
    public string? AoPath { get; set; }
    public string? ExportPath { get; set; }

    public static Config LoadConfig(string configPath)
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
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Could not load config: {ex.Message}");
            Console.ResetColor();
        }

        return new Config();
    }

    public void SaveConfig(string configPath)
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Could not save config: {ex.Message}");
            Console.ResetColor();
        }
    }
}