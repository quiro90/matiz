using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Matiz.Core.Colors;
using Matiz.Core.History;
using Matiz.Core.Palettes;
using Matiz.Core.Parsing;
using Matiz.Core.Settings;

namespace Matiz.Core.Persistence;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PaletteLibrary))]
[JsonSerializable(typeof(PaletteFile))]
[JsonSerializable(typeof(HistoryData))]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class MatizJsonContext : JsonSerializerContext;

/// <summary>Rutas de datos de usuario. Por defecto %APPDATA%\Matiz (MATIZ_DATA_DIR lo redefine).</summary>
public sealed record DataPaths(string Directory)
{
    public static DataPaths Default()
    {
        var overridden = Environment.GetEnvironmentVariable("MATIZ_DATA_DIR");
        return new(string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Matiz")
            : overridden);
    }

    public string Palettes => Path.Combine(Directory, "palettes.json");
    public string History => Path.Combine(Directory, "history.json");
    public string Settings => Path.Combine(Directory, "settings.json");
}

public static class PaletteRepository
{
    public static JsonStore<PaletteLibrary> Create(DataPaths paths, TimeSpan? debounce = null) =>
        new(paths.Palettes, MatizJsonContext.Default.PaletteLibrary, () => new PaletteLibrary(), Migrate, debounce);

    /// <summary>
    /// Migraciones: v0 = array JSON de paletas sin envoltorio (formato previo a schemaVersion) → v1.
    /// </summary>
    public static JsonNode Migrate(JsonNode node)
    {
        if (node is JsonArray array)
        {
            node = new JsonObject
            {
                ["schemaVersion"] = PaletteLibrary.CurrentSchemaVersion,
                ["palettes"] = array.DeepClone(),
            };
        }
        if (node is JsonObject obj)
        {
            var version = obj["schemaVersion"]?.GetValue<int>() ?? 0;
            if (version > PaletteLibrary.CurrentSchemaVersion)
                throw new NotSupportedException($"palettes.json versión {version} no soportada");
            obj["schemaVersion"] = PaletteLibrary.CurrentSchemaVersion;
        }
        return node;
    }
}

/// <summary>Contenido de history.json.</summary>
public sealed class HistoryData
{
    public int SchemaVersion { get; set; } = 1;
    public List<string> Colors { get; set; } = [];

    public static HistoryData From(ColorHistory history) =>
        new() { Colors = history.Items.Select(c => c.ToString()).ToList() };

    public ColorHistory ToHistory()
    {
        var parsed = Colors.Select(ColorParser.Parse).OfType<Argb>();
        return new ColorHistory(parsed);
    }
}

public static class HistoryRepository
{
    public static JsonStore<HistoryData> Create(DataPaths paths, TimeSpan? debounce = null) =>
        new(paths.History, MatizJsonContext.Default.HistoryData, () => new HistoryData(), debounce: debounce);
}

public static class SettingsRepository
{
    public static JsonStore<AppSettings> Create(DataPaths paths, TimeSpan? debounce = null) =>
        new(paths.Settings, MatizJsonContext.Default.AppSettings, () => new AppSettings(), debounce: debounce);
}

public static class JsonDefaults
{
    public static JsonSerializerOptions Options => MatizJsonContext.Default.Options;
}
