using System.Globalization;
using Matiz.Core.Colors;

namespace Matiz.Core.Formatting;

/// <summary>Opciones de presentación del HEX.</summary>
public sealed record FormatOptions(bool HexUppercase = true, bool HexHash = true)
{
    public static FormatOptions Default { get; } = new();
}

/// <summary>Representaciones de texto de un color con los redondeos definidos en la spec color-model.</summary>
public static class ColorFormats
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static int Pct(double unit) => (int)Math.Round(unit * 100, MidpointRounding.AwayFromZero);

    public static int Deg(double hue) => (int)Math.Round(hue, MidpointRounding.AwayFromZero) % 360;

    public static string Hex(Argb c, FormatOptions? o = null)
    {
        o ??= FormatOptions.Default;
        var hex = $"{c.R:X2}{c.G:X2}{c.B:X2}";
        if (!o.HexUppercase) hex = hex.ToLowerInvariant();
        return o.HexHash ? "#" + hex : hex;
    }

    public static string ArgbHex(Argb c, FormatOptions? o = null)
    {
        o ??= FormatOptions.Default;
        var hex = $"{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        if (!o.HexUppercase) hex = hex.ToLowerInvariant();
        return o.HexHash ? "#" + hex : hex;
    }

    public static string Rgb(Argb c) => $"{c.R}, {c.G}, {c.B}";

    public static string Hsl(Argb c)
    {
        var h = ColorMath.ToHsl(c);
        return $"{Deg(h.H)}°, {Pct(h.S)}%, {Pct(h.L)}%";
    }

    public static string Hsv(Argb c)
    {
        var h = ColorMath.ToHsv(c);
        return $"{Deg(h.H)}°, {Pct(h.S)}%, {Pct(h.V)}%";
    }

    public static string Cmyk(Argb c)
    {
        var k = ColorMath.ToCmyk(c);
        return $"{Pct(k.C)}%, {Pct(k.M)}%, {Pct(k.Y)}%, {Pct(k.K)}%";
    }

    public static string Oklch(Argb c)
    {
        var o = ColorMath.ToOklch(c);
        return string.Create(Inv, $"{o.L:0.000} {o.C:0.000} {OklchHue(o):0.0}");
    }

    public static string CssRgb(Argb c) => c.IsOpaque
        ? $"rgb({c.R}, {c.G}, {c.B})"
        : string.Create(Inv, $"rgba({c.R}, {c.G}, {c.B}, {c.A / 255.0:0.##})");

    public static string CssHsl(Argb c)
    {
        var h = ColorMath.ToHsl(c);
        return c.IsOpaque
            ? $"hsl({Deg(h.H)}, {Pct(h.S)}%, {Pct(h.L)}%)"
            : string.Create(Inv, $"hsla({Deg(h.H)}, {Pct(h.S)}%, {Pct(h.L)}%, {c.A / 255.0:0.##})");
    }

    public static string CssOklch(Argb c)
    {
        var o = ColorMath.ToOklch(c);
        return c.IsOpaque
            ? string.Create(Inv, $"oklch({o.L:0.000} {o.C:0.000} {OklchHue(o):0.0})")
            : string.Create(Inv, $"oklch({o.L:0.000} {o.C:0.000} {OklchHue(o):0.0} / {c.A / 255.0:0.##})");
    }

    public static string CSharpWpf(Argb c) => c.IsOpaque
        ? $"Color.FromRgb({c.R}, {c.G}, {c.B})"
        : CSharpArgb(c);

    public static string CSharpArgb(Argb c) => $"Color.FromArgb({c.A}, {c.R}, {c.G}, {c.B})";

    public static string Xaml(Argb c) => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    public static string Dart(Argb c) => $"Color(0x{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2})";

    public static string ArgbInt(Argb c) => $"0x{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    public static string CopyAll(Argb c, FormatOptions? o = null)
    {
        var lines = new List<string>
        {
            $"HEX: {Hex(c, o)}",
            $"RGB: {Rgb(c)}",
            $"HSL: {Hsl(c)}",
            $"HSV: {Hsv(c)}",
            $"CMYK: {Cmyk(c)}",
        };
        if (!c.IsOpaque) lines.Insert(1, $"ARGB: {ArgbHex(c, o)}");
        return string.Join(Environment.NewLine, lines);
    }

    private static double OklchHue(Oklch o)
    {
        var h = Math.Round(o.H, 1, MidpointRounding.AwayFromZero);
        return h >= 360 ? 0 : h;
    }
}
