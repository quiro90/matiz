using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Matiz.Core.Persistence;

namespace Matiz.Core.Palettes;

/// <summary>
/// Formato de archivo de paleta (.mpalette): JSON versionado standalone con la paleta completa
/// (nombre, descripción, fechas y colores con nombre individual, en orden). Las fechas y los
/// colores se leen tal cual; al importar la responsabilidad de normalizar/identificar es de
/// <see cref="PaletteService.Import"/>.
/// </summary>
public sealed class PaletteFile
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }

    /// <summary>Porcentaje de escala de grises recordado (null = 0 %). Opcional: los archivos antiguos
    /// lo leen como null y las apps anteriores ignoran el campo sin error.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? GrayPercent { get; set; }

    public List<PaletteColor> Colors { get; set; } = [];

    public static PaletteFile From(Palette palette) => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        Name = palette.Name,
        Description = palette.Description,
        CreatedAt = palette.CreatedAt,
        ModifiedAt = palette.ModifiedAt,
        GrayPercent = palette.GrayPercent,
        Colors = [.. palette.Colors],
    };

    /// <summary>Paleta nueva con datos del archivo y ids nuevos (paleta y colores).</summary>
    public Palette ToPalette() => new()
    {
        Name = Name,
        Description = Description,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt,
        GrayPercent = GrayPercent,
        Colors = [.. Colors.Select(c => new PaletteColor { Name = c.Name, Hex = c.Hex, Alpha = c.Alpha })],
    };

    public void Write(string path)
    {
        var json = JsonSerializer.Serialize(this, MatizJsonContext.Default.PaletteFile);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path))
            File.Replace(tmp, path, null, ignoreMetadataErrors: true);
        else
            File.Move(tmp, path);
    }

    public static PaletteFile Read(string path)
    {
        var node = JsonNode.Parse(File.ReadAllText(path)) ?? throw new JsonException("Documento vacío");
        var version = node["schemaVersion"]?.GetValue<int>() ?? 0;
        if (version > CurrentSchemaVersion)
            throw new NotSupportedException($".mpalette versión {version} no soportada");
        return node.Deserialize(MatizJsonContext.Default.PaletteFile) ?? throw new JsonException("Documento nulo");
    }
}