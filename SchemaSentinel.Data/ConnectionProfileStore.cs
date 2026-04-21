using System.Text.Json;
using SchemaSentinel.Core.Models;

namespace SchemaSentinel.Data;

public class ConnectionProfileStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SchemaSentinel", "connections.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public List<ConnectionProfile> Load()
    {
        if (!File.Exists(StorePath)) return [];
        try
        {
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<List<ConnectionProfile>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<ConnectionProfile> profiles)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
        File.WriteAllText(StorePath, JsonSerializer.Serialize(profiles.ToList(), JsonOpts));
    }
}
