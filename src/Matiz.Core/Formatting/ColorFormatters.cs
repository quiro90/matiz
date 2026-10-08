using Matiz.Core.Colors;

namespace Matiz.Core.Formatting;

/// <summary>Formato de texto de un color individual. Se registra en <see cref="ColorFormatters"/>.</summary>
public interface IColorFormatter
{
    string Id { get; }
    string DisplayName { get; }
    string Format(Argb color, FormatOptions options);
}

public sealed record ColorFormatter(string Id, string DisplayName, Func<Argb, FormatOptions, string> Formatter)
    : IColorFormatter
{
    public string Format(Argb color, FormatOptions options) => Formatter(color, options);
}

/// <summary>
/// Registro de formatos de color. La UI construye sus menús iterando <see cref="All"/>;
/// añadir un formato nuevo solo requiere registrarlo aquí.
/// </summary>
public static class ColorFormatters
{
    public const string HexId = "hex";

    public static IReadOnlyList<IColorFormatter> All { get; } =
    [
        new ColorFormatter(HexId, "HEX", (c, o) => ColorFormats.Hex(c, o)),
        new ColorFormatter("rgb", "RGB", (c, _) => ColorFormats.Rgb(c)),
        new ColorFormatter("hsl", "HSL", (c, _) => ColorFormats.Hsl(c)),
        new ColorFormatter("hsv", "HSV", (c, _) => ColorFormats.Hsv(c)),
        new ColorFormatter("cmyk", "CMYK (aprox.)", (c, _) => ColorFormats.Cmyk(c)),
        new ColorFormatter("css-rgb", "CSS rgb()", (c, _) => ColorFormats.CssRgb(c)),
        new ColorFormatter("css-hsl", "CSS hsl()", (c, _) => ColorFormats.CssHsl(c)),
        new ColorFormatter("css-oklch", "CSS oklch()", (c, _) => ColorFormats.CssOklch(c)),
        new ColorFormatter("csharp-wpf", "C# Color.FromRgb", (c, _) => ColorFormats.CSharpWpf(c)),
        new ColorFormatter("csharp-argb", "C# Color.FromArgb", (c, _) => ColorFormats.CSharpArgb(c)),
        new ColorFormatter("xaml", "XAML #AARRGGBB", (c, _) => ColorFormats.Xaml(c)),
        new ColorFormatter("dart", "Dart / Flutter", (c, _) => ColorFormats.Dart(c)),
        new ColorFormatter("argb-int", "Entero 0xAARRGGBB", (c, _) => ColorFormats.ArgbInt(c)),
        new ColorFormatter("all", "Todo (HEX, RGB, HSL, HSV, CMYK)", (c, o) => ColorFormats.CopyAll(c, o)),
    ];

    public static IColorFormatter Get(string? id) =>
        All.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}
