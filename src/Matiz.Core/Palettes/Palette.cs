using System.Text.Json.Serialization;
using Matiz.Core.Colors;
using Matiz.Core.Parsing;

namespace Matiz.Core.Palettes;

/// <summary>Color dentro de una paleta. Se persiste como "#RRGGBB" + alpha opcional (omitido si 255).</summary>
public sealed class PaletteColor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Name { get; set; }

    public string Hex { get; set; } = "#000000";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? Alpha { get; set; }

    [JsonIgnore]
    public Argb Color
    {
        get => ColorParser.TryParse(Hex, out var c) ? c.WithAlpha(Alpha ?? 255) : Argb.FromRgb(0, 0, 0);
        set
        {
            Hex = value.ToHex();
            Alpha = value.IsOpaque ? null : value.A;
        }
    }

    public static PaletteColor Create(Argb color, string? name = null) => new() { Color = color, Name = name };

    public PaletteColor Clone() => new() { Id = Guid.NewGuid(), Name = Name, Hex = Hex, Alpha = Alpha };
}

public sealed class Palette
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public List<PaletteColor> Colors { get; set; } = [];
}

/// <summary>Contenido de palettes.json.</summary>
public sealed class PaletteLibrary
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public Guid? ActivePaletteId { get; set; }
    public List<Palette> Palettes { get; set; } = [];
}
