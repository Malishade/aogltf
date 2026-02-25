using System.Text.Json;
using gltf;

namespace aogltf;

internal class Config
{
    public string? AoPath { get; set; }
    public string? ExportPath { get; set; }
    public FileFormat FileFormat { get; set; } = FileFormat.Glb;
    public ExportMirror ExportTransforms { get; set; } = ExportMirror.NoMirror;

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
            Console.WriteLine($"Could not load config: {ex.Message}");
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
            Console.WriteLine($"Could not save config: {ex.Message}");
        }
    }
}