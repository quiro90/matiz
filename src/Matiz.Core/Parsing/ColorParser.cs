using System.Globalization;
using System.Text.RegularExpressions;
using Matiz.Core.Colors;

namespace Matiz.Core.Parsing;

/// <summary>
/// Interpreta texto como color. Formatos aceptados (sin distinguir mayúsculas, tolerante a espacios):
/// HEX de 3/6/8 dígitos con o sin '#' (8 dígitos = #AARRGGBB), 0xAARRGGBB / 0xRRGGBB,
/// rgb()/rgba(), hsl()/hsla(), hsv()/hsb(), y tres enteros 0–255 separados por comas/espacios.
/// </summary>
public static partial class ColorParser
{
    public static bool TryParse(string? input, out Argb color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = input.Trim().TrimEnd(';').Trim().ToLowerInvariant();

        if (s.StartsWith("0x", StringComparison.Ordinal))
            return TryParseHexDigits(s[2..], out color);

        var fn = FunctionRegex().Match(s);
        if (fn.Success)
            return TryParseFunction(fn.Groups["name"].Value, fn.Groups["args"].Value, out color);

        if (s.StartsWith('#'))
            return TryParseHexDigits(s[1..].Trim(), out color);

        if (HexOnlyRegex().IsMatch(s))
            return TryParseHexDigits(s, out color);

        var parts = SplitArgs(s);
        if (parts.Length == 3 && parts.All(p => IntRegex().IsMatch(p)))
            return TryRgbChannels(parts, null, out color);

        return false;
    }

    public static Argb? Parse(string? input) => TryParse(input, out var c) ? c : null;

    private static bool TryParseHexDigits(string hex, out Argb color)
    {
        color = default;
        if (!HexOnlyRegex().IsMatch(hex)) return false;
        switch (hex.Length)
        {
            case 3:
                color = Argb.FromRgb(Dup(hex[0]), Dup(hex[1]), Dup(hex[2]));
                return true;
            case 6:
                color = Argb.FromUInt32(0xFF000000u | uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                return true;
            case 8:
                color = Argb.FromUInt32(uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                return true;
            default:
                return false;
        }

        static byte Dup(char ch)
        {
            var v = Convert.ToByte(ch.ToString(), 16);
            return (byte)(v * 17);
        }
    }

    private static bool TryParseFunction(string name, string args, out Argb color)
    {
        color = default;
        var parts = SplitArgs(args);
        switch (name)
        {
            case "rgb":
            case "rgba":
                if (parts.Length is not (3 or 4)) return false;
                return TryRgbChannels(parts[..3], parts.Length == 4 ? parts[3] : null, out color);

            case "hsl":
            case "hsla":
            case "hsv":
            case "hsb":
                if (parts.Length is not (3 or 4)) return false;
                if (!TryHue(parts[0], out var h) || !TryPercent(parts[1], out var p1) || !TryPercent(parts[2], out var p2))
                    return false;
                byte a = 255;
                if (parts.Length == 4 && !TryAlpha(parts[3], out a)) return false;
                color = name.StartsWith("hsl", StringComparison.Ordinal)
                    ? ColorMath.FromHsl(h, p1, p2, a)
                    : ColorMath.FromHsv(h, p1, p2, a);
                return true;

            default:
                return false;
        }
    }

    private static bool TryRgbChannels(string[] parts, string? alpha, out Argb color)
    {
        color = default;
        var ch = new byte[3];
        for (var i = 0; i < 3; i++)
        {
            var p = parts[i];
            if (p.EndsWith('%'))
            {
                if (!TryNumber(p[..^1], out var pct) || pct < 0 || pct > 100) return false;
                ch[i] = Argb.ToByte(pct / 100);
            }
            else
            {
                if (!TryNumber(p, out var v) || v < 0 || v > 255) return false;
                ch[i] = (byte)Math.Round(v, MidpointRounding.AwayFromZero);
            }
        }
        byte a = 255;
        if (alpha is not null && !TryAlpha(alpha, out a)) return false;
        color = new Argb(a, ch[0], ch[1], ch[2]);
        return true;
    }

    private static bool TryHue(string s, out double h)
    {
        s = s.Replace("deg", "", StringComparison.Ordinal).Replace("°", "", StringComparison.Ordinal);
        if (!TryNumber(s, out h) || h < 0 || h > 360) return false;
        h = ColorMath.NormalizeHue(h);
        return true;
    }

    private static bool TryPercent(string s, out double unit)
    {
        unit = 0;
        if (!TryNumber(s.TrimEnd('%'), out var v) || v < 0 || v > 100) return false;
        unit = v / 100;
        return true;
    }

    private static bool TryAlpha(string s, out byte a)
    {
        a = 255;
        if (s.EndsWith('%'))
        {
            if (!TryNumber(s[..^1], out var pct) || pct < 0 || pct > 100) return false;
            a = Argb.ToByte(pct / 100);
            return true;
        }
        if (!TryNumber(s, out var v) || v < 0 || v > 1) return false;
        a = Argb.ToByte(v);
        return true;
    }

    private static bool TryNumber(string s, out double v) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && double.IsFinite(v);

    private static string[] SplitArgs(string s) =>
        SeparatorRegex().Split(s.Trim()).Where(p => p.Length > 0).ToArray();

    [GeneratedRegex(@"^(?<name>[a-z]+)\s*\((?<args>[^)]*)\)$")]
    private static partial Regex FunctionRegex();

    [GeneratedRegex(@"^[0-9a-f]+$")]
    private static partial Regex HexOnlyRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex IntRegex();

    [GeneratedRegex(@"[\s,;/]+")]
    private static partial Regex SeparatorRegex();
}
