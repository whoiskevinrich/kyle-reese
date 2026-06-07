using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyleReese.Core;

/// <summary>
/// The editable list of process names to kill. Persisted as JSON next to the executable so
/// the list can be extended without recompiling (see CLAUDE.md).
/// </summary>
public sealed class KillListConfig
{
    /// <summary>Names killed when no config file is present.</summary>
    public static readonly IReadOnlyList<string> DefaultProcessNames =
        new[] { "claude", "bash", "git", "sh" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    [JsonPropertyName("processNames")]
    public List<string> ProcessNames { get; set; } = new(DefaultProcessNames);

    /// <summary>Default on-disk location of the config: <c>killlist.json</c> beside the exe.</summary>
    public static string DefaultConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "killlist.json");

    /// <summary>
    /// Loads the config from <paramref name="path"/>. Falls back to the defaults if the file is
    /// missing, malformed, or empty — loading must never throw.
    /// </summary>
    public static KillListConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var cfg = JsonSerializer.Deserialize<KillListConfig>(File.ReadAllText(path), JsonOptions);
                var cleaned = Normalize(cfg?.ProcessNames);
                if (cleaned.Count > 0)
                {
                    return new KillListConfig { ProcessNames = cleaned };
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Ignore and fall back to defaults.
        }

        return new KillListConfig();
    }

    /// <summary>Writes the config to <paramref name="path"/> as indented JSON.</summary>
    public void Save(string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));

    private static List<string> Normalize(IEnumerable<string>? names) =>
        (names ?? Enumerable.Empty<string>())
            .Where(n => n is not null)
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
