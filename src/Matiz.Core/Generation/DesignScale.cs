using Matiz.Core.Colors;

namespace Matiz.Core.Generation;

/// <summary>
/// Design Scale 50–950 en OKLCH: L estrictamente decreciente según una curva de referencia remapeada
/// para que el color base caiga exactamente en el paso ancla; croma derivada del base reducida hacia
/// los extremos; hue constante; ajuste a gama reduciendo croma.
/// </summary>
public static class DesignScale
{
    public static IReadOnlyList<int> Steps { get; } = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];

    /// <summary>Luminosidades OKLCH de referencia (inspiradas en Tailwind v4). Ajustables.</summary>
    public static IReadOnlyList<double> ReferenceL { get; } =
        [0.975, 0.945, 0.89, 0.82, 0.72, 0.63, 0.55, 0.47, 0.39, 0.32, 0.24];

    /// <summary>Diferencia mínima de L entre pasos consecutivos.</summary>
    public const double MinStep = 0.02;

    /// <summary>Reducción máxima de croma en los extremos (0.6 → los extremos conservan el 40%).</summary>
    public const double ChromaFalloff = 0.6;

    private const int FixedAnchorIndex = 5;

    public static List<GeneratedColor> Generate(Argb baseColor, ScaleAnchorMode mode = ScaleAnchorMode.Fixed500)
    {
        var lch = ColorMath.ToOklch(baseColor);
        var l = Lightness(lch.L, mode, out var anchor);

        var lightSpan = Math.Max(l[0] - lch.L, 1e-6);
        var darkSpan = Math.Max(lch.L - l[^1], 1e-6);

        var result = new List<GeneratedColor>(Steps.Count);
        for (var i = 0; i < Steps.Count; i++)
        {
            if (i == anchor)
            {
                result.Add(new GeneratedColor(Steps[i].ToString(), baseColor, true));
                continue;
            }
            var t = i < anchor ? (l[i] - lch.L) / lightSpan : (lch.L - l[i]) / darkSpan;
            var chroma = lch.C * (1 - ChromaFalloff * t * t);
            var color = lch.C < 1e-4
                ? GrayForL(l[i], baseColor.A)
                : ColorMath.FromOklchGamutMapped(new Oklch(l[i], chroma, lch.H), baseColor.A);
            result.Add(new GeneratedColor(Steps[i].ToString(), color));
        }
        return result;
    }

    /// <summary>Calcula las 11 luminosidades para un base de luminosidad <paramref name="baseL"/>.</summary>
    public static double[] Lightness(double baseL, ScaleAnchorMode mode, out int anchor)
    {
        baseL = Math.Clamp(baseL, 0, 1);
        anchor = ChooseAnchor(baseL, mode);
        var n = Steps.Count;
        var l = new double[n];
        l[anchor] = baseL;

        // Extremos: al menos los de referencia, y con espacio suficiente para MinStep por paso.
        var top = Math.Min(1, Math.Max(ReferenceL[0], baseL + MinStep * anchor));
        var bottom = Math.Max(0, Math.Min(ReferenceL[^1], baseL - MinStep * (n - 1 - anchor)));
        if (anchor == 0) top = baseL;
        if (anchor == n - 1) bottom = baseL;

        Distribute(l, anchor, 0, top);       // pasos más claros
        Distribute(l, anchor, n - 1, bottom); // pasos más oscuros
        return l;
    }

    /// <summary>
    /// Reparte la distancia entre el ancla y un extremo garantizando MinStep por hueco y
    /// repartiendo el sobrante proporcionalmente a los huecos de la curva de referencia.
    /// </summary>
    private static void Distribute(double[] l, int anchor, int end, double endValue)
    {
        var count = Math.Abs(end - anchor);
        if (count == 0) return;
        var dir = Math.Sign(end - anchor);
        var total = Math.Abs(endValue - l[anchor]);
        var refGaps = new double[count];
        double refSum = 0;
        for (var k = 0; k < count; k++)
        {
            var a = anchor + k * dir;
            refGaps[k] = Math.Abs(ReferenceL[a + dir] - ReferenceL[a]);
            refSum += refGaps[k];
        }
        var spare = Math.Max(0, total - MinStep * count);
        var sign = dir < 0 ? 1 : -1; // hacia los claros L sube; hacia los oscuros baja
        var acc = l[anchor];
        for (var k = 0; k < count; k++)
        {
            var gap = MinStep + spare * refGaps[k] / refSum;
            acc += sign * gap;
            l[anchor + (k + 1) * dir] = Math.Clamp(acc, 0, 1);
        }
    }

    private static bool Feasible(double baseL, int anchor) =>
        baseL + MinStep * anchor <= 1 + 1e-9 &&
        baseL - MinStep * (Steps.Count - 1 - anchor) >= -1e-9;

    private static int ChooseAnchor(double baseL, ScaleAnchorMode mode)
    {
        if (mode == ScaleAnchorMode.Fixed500 && Feasible(baseL, FixedAnchorIndex)) return FixedAnchorIndex;
        // Automático (o 500 imposible: no cabe un paso por cada 0.02 de L): el paso de referencia más cercano que sea factible.
        return Enumerable.Range(0, Steps.Count)
            .OrderBy(i => Math.Abs(ReferenceL[i] - baseL))
            .First(i => Feasible(baseL, i));
    }

    private static Argb GrayForL(double l, byte alpha)
    {
        var v = Argb.ToByte(ColorMath.LinearToSrgb(Math.Clamp(l * l * l, 0, 1)));
        return new Argb(alpha, v, v, v);
    }
}
