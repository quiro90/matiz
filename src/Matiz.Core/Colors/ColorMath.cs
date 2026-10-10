namespace Matiz.Core.Colors;

/// <summary>
/// Conversiones entre espacios de color. Fórmulas estándar:
/// HSV/HSL hexcónicos, CMYK ingenuo sin perfil, Oklab/OKLCH según Björn Ottosson
/// con la función de transferencia sRGB IEC 61966-2-1.
/// </summary>
public static class ColorMath
{
    // ---------- HSV ----------

    public static Hsv ToHsv(Argb c) => ToHsv(c.RUnit, c.GUnit, c.BUnit);

    public static Hsv ToHsv(double r, double g, double b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;
        var s = max <= 0 ? 0 : d / max;
        return new Hsv(Hue(r, g, b, max, d), s, max);
    }

    public static (double R, double G, double B) HsvToRgb(double h, double s, double v)
    {
        h = NormalizeHue(h);
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);
        var c = v * s;
        var hp = h / 60.0;
        var x = c * (1 - Math.Abs(hp % 2 - 1));
        var (r, g, b) = Sector(hp, c, x);
        var m = v - c;
        return (r + m, g + m, b + m);
    }

    public static Argb FromHsv(double h, double s, double v, byte alpha = 255)
    {
        var (r, g, b) = HsvToRgb(h, s, v);
        return Argb.FromUnit(r, g, b, alpha);
    }

    // ---------- HSL ----------

    public static Hsl ToHsl(Argb c)
    {
        double r = c.RUnit, g = c.GUnit, b = c.BUnit;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;
        var l = (max + min) / 2;
        var denom = 1 - Math.Abs(2 * l - 1);
        var s = d <= 0 || denom <= 0 ? 0 : d / denom;
        return new Hsl(Hue(r, g, b, max, d), Math.Clamp(s, 0, 1), l);
    }

    public static (double R, double G, double B) HslToRgb(double h, double s, double l)
    {
        h = NormalizeHue(h);
        s = Math.Clamp(s, 0, 1);
        l = Math.Clamp(l, 0, 1);
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var hp = h / 60.0;
        var x = c * (1 - Math.Abs(hp % 2 - 1));
        var (r, g, b) = Sector(hp, c, x);
        var m = l - c / 2;
        return (r + m, g + m, b + m);
    }

    public static Argb FromHsl(double h, double s, double l, byte alpha = 255)
    {
        var (r, g, b) = HslToRgb(h, s, l);
        return Argb.FromUnit(r, g, b, alpha);
    }

    // ---------- CMYK (aproximado, sin perfil ICC) ----------

    public static Cmyk ToCmyk(Argb c)
    {
        double r = c.RUnit, g = c.GUnit, b = c.BUnit;
        var k = 1 - Math.Max(r, Math.Max(g, b));
        if (k >= 1) return new Cmyk(0, 0, 0, 1);
        return new Cmyk((1 - r - k) / (1 - k), (1 - g - k) / (1 - k), (1 - b - k) / (1 - k), k);
    }

    // ---------- sRGB lineal ----------

    public static double SrgbToLinear(double c) =>
        c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    public static double LinearToSrgb(double c) =>
        c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

    public static LinearRgb ToLinear(Argb c) =>
        new(SrgbToLinear(c.RUnit), SrgbToLinear(c.GUnit), SrgbToLinear(c.BUnit));

    public static Argb FromLinear(LinearRgb c, byte alpha = 255) =>
        Argb.FromUnit(
            LinearToSrgb(Math.Clamp(c.R, 0, 1)),
            LinearToSrgb(Math.Clamp(c.G, 0, 1)),
            LinearToSrgb(Math.Clamp(c.B, 0, 1)),
            alpha);

    // ---------- Oklab / OKLCH ----------

    public static Oklab ToOklab(Argb c) => ToOklab(ToLinear(c));

    public static Oklab ToOklab(LinearRgb c)
    {
        var l = 0.4122214708 * c.R + 0.5363325363 * c.G + 0.0514459929 * c.B;
        var m = 0.2119034982 * c.R + 0.6806995451 * c.G + 0.1073969566 * c.B;
        var s = 0.0883024619 * c.R + 0.2817188376 * c.G + 0.6299787005 * c.B;
        var l_ = Math.Cbrt(l);
        var m_ = Math.Cbrt(m);
        var s_ = Math.Cbrt(s);
        return new Oklab(
            0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
            1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
            0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_);
    }

    public static LinearRgb ToLinear(Oklab c)
    {
        var l_ = c.L + 0.3963377774 * c.A + 0.2158037573 * c.B;
        var m_ = c.L - 0.1055613458 * c.A - 0.0638541728 * c.B;
        var s_ = c.L - 0.0894841775 * c.A - 1.2914855480 * c.B;
        var l = l_ * l_ * l_;
        var m = m_ * m_ * m_;
        var s = s_ * s_ * s_;
        return new LinearRgb(
            +4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    public static Oklch ToOklch(Argb c) => ToOklch(ToOklab(c));

    public static Oklch ToOklch(Oklab c)
    {
        var chroma = Math.Sqrt(c.A * c.A + c.B * c.B);
        var h = chroma < 1e-9 ? 0 : NormalizeHue(Math.Atan2(c.B, c.A) * 180 / Math.PI);
        return new Oklch(c.L, chroma, h);
    }

    public static Oklab ToOklab(Oklch c)
    {
        var rad = c.H * Math.PI / 180;
        return new Oklab(c.L, c.C * Math.Cos(rad), c.C * Math.Sin(rad));
    }

    public static bool IsInGamut(LinearRgb c, double eps = 1e-6) =>
        c.R >= -eps && c.R <= 1 + eps &&
        c.G >= -eps && c.G <= 1 + eps &&
        c.B >= -eps && c.B <= 1 + eps;

    /// <summary>
    /// Convierte OKLCH a sRGB de 8 bits. Si queda fuera de gama reduce solo la croma
    /// (búsqueda binaria) conservando L y h.
    /// </summary>
    public static Argb FromOklchGamutMapped(Oklch c, byte alpha = 255)
    {
        var l = Math.Clamp(c.L, 0, 1);
        var target = new Oklch(l, Math.Max(0, c.C), c.H);
        var lin = ToLinear(ToOklab(target));
        if (IsInGamut(lin)) return FromLinear(lin, alpha);

        double lo = 0, hi = target.C;
        for (var i = 0; i < 30; i++)
        {
            var mid = (lo + hi) / 2;
            if (IsInGamut(ToLinear(ToOklab(target with { C = mid })))) lo = mid; else hi = mid;
        }
        return FromLinear(ToLinear(ToOklab(target with { C = lo })), alpha);
    }

    /// <summary>Gris neutro con la misma luminosidad perceptual (Oklab L) que el color dado.</summary>
    public static Argb GrayEquivalent(Argb c)
    {
        var l = ToOklab(c).L;
        var lin = Math.Clamp(l * l * l, 0, 1);
        var v = Argb.ToByte(LinearToSrgb(lin));
        return new Argb(c.A, v, v, v);
    }

    /// <summary>Mezcla por canal del color hacia su gris equivalente perceptual (0 = original, 1 = gris).
    /// Solo presentación: conserva el alfa del original y nunca muta colores almacenados.</summary>
    public static Argb MixToGray(Argb c, double amount)
    {
        if (double.IsNaN(amount)) return c;
        amount = Math.Clamp(amount, 0, 1);
        if (amount <= 0) return c;
        var g = GrayEquivalent(c);
        if (amount >= 1) return g;
        return c with
        {
            R = LerpByte(c.R, g.R, amount),
            G = LerpByte(c.G, g.G, amount),
            B = LerpByte(c.B, g.B, amount),
        };
    }

    private static byte LerpByte(byte a, byte b, double t)
    {
        var v = Math.Round(a + (b - a) * t, MidpointRounding.AwayFromZero);
        return (byte)Math.Clamp(v, 0, 255);
    }

    // ---------- utilidades ----------

    public static double NormalizeHue(double h)
    {
        if (double.IsNaN(h) || double.IsInfinity(h)) return 0;
        h %= 360;
        if (h < 0) h += 360;
        return h >= 360 ? 0 : h;
    }

    private static double Hue(double r, double g, double b, double max, double d)
    {
        if (d <= 0) return 0;
        double h;
        if (max == r) h = (g - b) / d % 6;
        else if (max == g) h = (b - r) / d + 2;
        else h = (r - g) / d + 4;
        return NormalizeHue(h * 60);
    }

    private static (double, double, double) Sector(double hp, double c, double x) => hp switch
    {
        < 1 => (c, x, 0),
        < 2 => (x, c, 0),
        < 3 => (0, c, x),
        < 4 => (0, x, c),
        < 5 => (x, 0, c),
        _ => (c, 0, x),
    };
}
