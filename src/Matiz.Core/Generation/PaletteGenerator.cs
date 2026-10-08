using Matiz.Core.Colors;
using Matiz.Core.Localization;

namespace Matiz.Core.Generation;

public enum HarmonyKind
{
    Complementary,
    Analogous,
    SplitComplementary,
    Triadic,
    Tetradic,
    Monochromatic,
}

public enum ScaleAnchorMode
{
    /// <summary>El color base ocupa siempre el paso 500 (si es geométricamente posible).</summary>
    Fixed500,
    /// <summary>El color base ocupa el paso cuya luminosidad de referencia es más cercana.</summary>
    Automatic,
}

/// <summary>
/// Color generado: etiqueta visible (p. ej. "500", "+120°", "Tint 20%"), si es el color base y, para armonías,
/// sus coordenadas exactas en la rueda (hue y saturación HSV antes de redondear a 8 bits).
/// </summary>
public sealed record GeneratedColor(string Label, Argb Color, bool IsBase = false, double? WheelHue = null, double? WheelSaturation = null);

public static class PaletteGenerator
{
    public static string HarmonyName(HarmonyKind kind) => Texts.Current.HarmonyName(kind);

    public static IReadOnlyList<int> HarmonyOffsets(HarmonyKind kind) => kind switch
    {
        HarmonyKind.Complementary => [0, 180],
        HarmonyKind.Analogous => [-60, -30, 0, 30, 60],
        HarmonyKind.SplitComplementary => [0, 150, 210],
        HarmonyKind.Triadic => [0, 120, 240],
        HarmonyKind.Tetradic => [0, 90, 180, 270],
        _ => [0],
    };

    /// <summary>Armonía a partir de un color canónico (se convierte a coordenadas del selector).</summary>
    public static IReadOnlyList<GeneratedColor> Harmony(Argb baseColor, HarmonyKind kind,
        ScaleAnchorMode anchor = ScaleAnchorMode.Fixed500, bool balanceLightness = true) =>
        Harmony(ColorState.FromArgb(baseColor), kind, anchor, balanceLightness);

    /// <summary>
    /// Armonías geométricas: giran el hue de la rueda (HSV) el ángulo exacto y conservan la saturación, de modo que
    /// sus puntos forman la figura exacta en la rueda para cualquier brillo. Con <paramref name="balanceLightness"/>
    /// se ajusta solo el brillo de cada color para igualar la luminosidad perceptual (OKLab L) del base: la posición
    /// en la rueda depende solo de H y S, así que la geometría se conserva. Se calcula desde las coordenadas continuas
    /// del selector, no desde el HEX redondeado (estable en colores muy oscuros).
    /// Monocromática toma 5 pasos de la Design Scale.
    /// <paramref name="colorOffsets"/> aplica desfases personalizados por índice generado (alineado con
    /// <see cref="HarmonyOffsets"/>, el base lo ignora): suma hue (grados) y saturación (clamped 0–1) del color
    /// canónico de esa posición. Nulo o vacío = armonía canónica bit a bit.
    /// </summary>
    public static IReadOnlyList<GeneratedColor> Harmony(ColorState baseState, HarmonyKind kind,
        ScaleAnchorMode anchor = ScaleAnchorMode.Fixed500, bool balanceLightness = true,
        IReadOnlyList<(double HueDelta, double SatDelta)>? colorOffsets = null)
    {
        var baseColor = baseState.Argb;
        if (kind == HarmonyKind.Monochromatic) return Monochromatic(baseColor, anchor);

        var targetL = LightnessOf(baseState.Hue, baseState.Saturation, baseState.Value);
        return HarmonyOffsets(kind).Select((d, i) =>
        {
            if (d == 0) return new GeneratedColor(Texts.Current.Base, baseColor, true, baseState.Hue, baseState.Saturation);
            (double HueDelta, double SatDelta) o = default;
            if (colorOffsets is { Count: > 0 } && i < colorOffsets.Count) o = colorOffsets[i];
            var s = Math.Clamp(baseState.Saturation + o.SatDelta, 0, 1);
            var h = ColorMath.NormalizeHue(baseState.Hue + d + o.HueDelta);
            var v = balanceLightness ? ValueForLightness(h, s, targetL) : baseState.Value;
            var c = ColorMath.FromHsv(h, s, v, baseState.Alpha);
            return new GeneratedColor(d > 0 ? $"+{d}°" : $"{d}°", c, false, h, s);
        }).ToList();
    }

    /// <summary>OKLab L de un color HSV continuo (sin redondear a 8 bits).</summary>
    public static double LightnessOf(double h, double s, double v)
    {
        var (r, g, b) = ColorMath.HsvToRgb(h, s, v);
        return ColorMath.ToOklab(new LinearRgb(ColorMath.SrgbToLinear(r), ColorMath.SrgbToLinear(g), ColorMath.SrgbToLinear(b))).L;
    }

    /// <summary>
    /// Brillo (HSV V) con el que (h, s) alcanza la luminosidad <paramref name="targetL"/>. L crece de forma monótona
    /// con V, así que basta una búsqueda binaria; si no es alcanzable devuelve 0 o 1.
    /// </summary>
    public static double ValueForLightness(double h, double s, double targetL)
    {
        if (LightnessOf(h, s, 1) <= targetL) return 1;
        double lo = 0, hi = 1;
        for (var i = 0; i < 40; i++)
        {
            var mid = (lo + hi) / 2;
            if (LightnessOf(h, s, mid) < targetL) lo = mid; else hi = mid;
        }
        return (lo + hi) / 2;
    }

    public static IReadOnlyList<GeneratedColor> Monochromatic(Argb baseColor, ScaleAnchorMode anchor = ScaleAnchorMode.Fixed500)
    {
        var scale = DesignScale.Generate(baseColor, anchor);
        var anchorIndex = scale.FindIndex(s => s.IsBase);
        int[] picks = [1, 3, 5, 7, 9];
        var nearest = picks.OrderBy(i => Math.Abs(i - anchorIndex)).First();
        return picks.Select(i => i == nearest ? anchorIndex : i)
            .Distinct()
            .Order()
            .Select(i => scale[i])
            .ToList();
    }

    /// <summary>Tint N% = mezcla sRGB con blanco al N% (definición Sass/Bootstrap).</summary>
    public static Argb Tint(Argb c, double amount) => new(c.A,
        MixChannel(c.R, 255, amount), MixChannel(c.G, 255, amount), MixChannel(c.B, 255, amount));

    /// <summary>Shade N% = mezcla sRGB con negro al N%.</summary>
    public static Argb Shade(Argb c, double amount) => new(c.A,
        MixChannel(c.R, 0, amount), MixChannel(c.G, 0, amount), MixChannel(c.B, 0, amount));

    /// <summary>Tints 50…10%, base, Shades 10…50%.</summary>
    public static IReadOnlyList<GeneratedColor> TintsAndShades(Argb c)
    {
        var t = Texts.Current;
        var list = new List<GeneratedColor>();
        for (var p = 50; p >= 10; p -= 10) list.Add(new(t.Tint(p), Tint(c, p / 100.0)));
        list.Add(new(t.Base, c, true));
        for (var p = 10; p <= 50; p += 10) list.Add(new(t.Shade(p), Shade(c, p / 100.0)));
        return list;
    }

    public static IReadOnlyList<GeneratedColor> Tints(Argb c) =>
        [new(Texts.Current.Base, c, true), .. Enumerable.Range(1, 5).Select(i => new GeneratedColor(Texts.Current.Tint(i * 10), Tint(c, i / 10.0)))];

    public static IReadOnlyList<GeneratedColor> Shades(Argb c) =>
        [new(Texts.Current.Base, c, true), .. Enumerable.Range(1, 5).Select(i => new GeneratedColor(Texts.Current.Shade(i * 10), Shade(c, i / 10.0)))];

    /// <summary>
    /// Neutros: mismas luminosidades que la Design Scale, hue del color base y croma muy baja (≤ 0.015 antes de redondeo).
    /// Un color base acromático produce grises puros.
    /// </summary>
    public static IReadOnlyList<GeneratedColor> Neutrals(Argb baseColor)
    {
        var lch = ColorMath.ToOklch(baseColor);
        var lightness = DesignScale.ReferenceL;
        var chroma = lch.C < 0.002 ? 0 : Math.Min(0.015, lch.C * 0.12);
        return DesignScale.Steps.Select((step, i) =>
        {
            var c = chroma == 0
                ? Gray(lightness[i])
                : ColorMath.FromOklchGamutMapped(new Oklch(lightness[i], chroma, lch.H));
            return new GeneratedColor(step.ToString(), c);
        }).ToList();

        static Argb Gray(double l)
        {
            var v = Argb.ToByte(ColorMath.LinearToSrgb(Math.Clamp(l * l * l, 0, 1)));
            return Argb.FromRgb(v, v, v);
        }
    }

    private static byte MixChannel(byte from, byte to, double amount) =>
        (byte)Math.Clamp(Math.Round(from + (to - from) * amount, MidpointRounding.AwayFromZero), 0, 255);
}
