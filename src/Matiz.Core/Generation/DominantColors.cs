using Matiz.Core.Colors;

namespace Matiz.Core.Generation;

/// <summary>
/// Extracción de colores predominantes: muestreo a ≤128 px del lado mayor, k-means++ en Oklab
/// con semilla fija (resultado determinista), ordenado por población.
/// </summary>
public static class DominantColors
{
    public const int MaxSide = 128;
    private const int Seed = 1234;
    private const int Iterations = 12;

    /// <param name="bgra">Píxeles BGRA de 32 bits, fila a fila.</param>
    public static IReadOnlyList<Argb> Extract(ReadOnlySpan<byte> bgra, int width, int height, int stride, int count)
    {
        count = Math.Clamp(count, 1, 32);
        var samples = Sample(bgra, width, height, stride);
        if (samples.Count == 0) return [];

        var k = Math.Min(count, samples.Count);
        var centers = InitPlusPlus(samples, k);
        var assign = new int[samples.Count];

        for (var it = 0; it < Iterations; it++)
        {
            var changed = false;
            for (var i = 0; i < samples.Count; i++)
            {
                var best = Nearest(samples[i], centers);
                if (best != assign[i] || it == 0) { changed |= best != assign[i]; assign[i] = best; }
            }

            var sums = new (double L, double A, double B, int N)[k];
            for (var i = 0; i < samples.Count; i++)
            {
                ref var s = ref sums[assign[i]];
                s.L += samples[i].L; s.A += samples[i].A; s.B += samples[i].B; s.N++;
            }
            for (var c = 0; c < k; c++)
                if (sums[c].N > 0)
                    centers[c] = new Oklab(sums[c].L / sums[c].N, sums[c].A / sums[c].N, sums[c].B / sums[c].N);

            if (!changed && it > 0) break;
        }

        var population = new int[k];
        foreach (var a in assign) population[a]++;

        return Enumerable.Range(0, k)
            .Where(c => population[c] > 0)
            .OrderByDescending(c => population[c])
            .ThenBy(c => centers[c].L)
            .Select(c => ColorMath.FromLinear(ColorMath.ToLinear(centers[c])))
            .Distinct()
            .ToList();
    }

    private static List<Oklab> Sample(ReadOnlySpan<byte> bgra, int width, int height, int stride)
    {
        var step = Math.Max(1, (int)Math.Ceiling(Math.Max(width, height) / (double)MaxSide));
        var list = new List<Oklab>((width / step + 1) * (height / step + 1));
        for (var y = 0; y < height; y += step)
        {
            var row = y * stride;
            for (var x = 0; x < width; x += step)
            {
                var o = row + x * 4;
                if (bgra[o + 3] < 128) continue; // ignora píxeles transparentes
                list.Add(ColorMath.ToOklab(Argb.FromRgb(bgra[o + 2], bgra[o + 1], bgra[o])));
            }
        }
        return list;
    }

    private static Oklab[] InitPlusPlus(List<Oklab> samples, int k)
    {
        var rnd = new Random(Seed);
        var centers = new Oklab[k];
        centers[0] = samples[rnd.Next(samples.Count)];
        var dist = new double[samples.Count];
        for (var c = 1; c < k; c++)
        {
            double sum = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                var d = double.MaxValue;
                for (var j = 0; j < c; j++) d = Math.Min(d, Dist2(samples[i], centers[j]));
                dist[i] = d;
                sum += d;
            }
            if (sum <= 0) { centers[c] = centers[0]; continue; }
            var target = rnd.NextDouble() * sum;
            var idx = 0;
            for (double acc = 0; idx < samples.Count - 1; idx++)
            {
                acc += dist[idx];
                if (acc >= target) break;
            }
            centers[c] = samples[idx];
        }
        return centers;
    }

    private static int Nearest(Oklab p, Oklab[] centers)
    {
        var best = 0;
        var bestD = double.MaxValue;
        for (var c = 0; c < centers.Length; c++)
        {
            var d = Dist2(p, centers[c]);
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    private static double Dist2(Oklab a, Oklab b)
    {
        double dl = a.L - b.L, da = a.A - b.A, db = a.B - b.B;
        return dl * dl + da * da + db * db;
    }
}
