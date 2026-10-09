using Matiz.Core.Colors;
using Matiz.Core.Generation;
using Matiz.Core.Parsing;

namespace Matiz.Core.Tests;

public class GenerationTests
{
    private static readonly Argb Violet = Argb.FromRgb(82, 70, 188);

    public static TheoryData<string> ScaleInputs => new()
    {
        "#5246BC", "#FACC15", "#22C55E", "#000000", "#FFFFFF", "#FF0000", "#0000FF", "#808080",
        "#F0F0FF", "#101018", "#00FFFF", "#FF00FF", "#8B4513", "#FFF8E1",
    };

    [Fact]
    public void Tint_and_shade_50()
    {
        Assert.Equal("#A9A3DE", PaletteGenerator.Tint(Violet, 0.5).ToHex());
        Assert.Equal("#29235E", PaletteGenerator.Shade(Violet, 0.5).ToHex());
    }

    [Fact]
    public void Tints_and_shades_have_base_in_the_middle()
    {
        var list = PaletteGenerator.TintsAndShades(Violet);
        Assert.Equal(11, list.Count);
        Assert.True(list[5].IsBase);
        Assert.Equal(Violet, list[5].Color);
    }

    [Theory]
    [InlineData(HarmonyKind.Complementary, 2)]
    [InlineData(HarmonyKind.Analogous, 5)]
    [InlineData(HarmonyKind.SplitComplementary, 3)]
    [InlineData(HarmonyKind.Triadic, 3)]
    [InlineData(HarmonyKind.Tetradic, 4)]
    [InlineData(HarmonyKind.Monochromatic, 5)]
    public void Harmony_sizes_and_base_present(HarmonyKind kind, int count)
    {
        var list = PaletteGenerator.Harmony(Violet, kind);
        Assert.Equal(count, list.Count);
        Assert.Contains(list, g => g.IsBase && g.Color == Violet);
    }

    private static readonly HarmonyKind[] Rotations =
        [HarmonyKind.Complementary, HarmonyKind.Analogous, HarmonyKind.SplitComplementary, HarmonyKind.Triadic, HarmonyKind.Tetradic];

    [Theory]
    [MemberData(nameof(ScaleInputs))]
    public void Balanced_harmonies_match_lightness_when_reachable(string hex)
    {
        var c = ColorParser.Parse(hex)!.Value;
        var baseL = ColorMath.ToOklab(c).L;
        foreach (var kind in Rotations)
        foreach (var g in PaletteGenerator.Harmony(c, kind).Where(g => !g.IsBase))
        {
            var reachable = PaletteGenerator.LightnessOf(g.WheelHue!.Value, g.WheelSaturation!.Value, 1) >= baseL;
            if (reachable) Assert.InRange(Math.Abs(ColorMath.ToOklab(g.Color).L - baseL), 0, 0.01);
            else Assert.Equal(1, ColorMath.ToHsv(g.Color).V, 2); // no alcanzable: brillo máximo
        }
    }

    [Fact]
    public void Triadic_is_exact_geometry_on_the_wheel()
    {
        var list = PaletteGenerator.Harmony(new ColorState(246.1, 0.63, 0.74), HarmonyKind.Triadic);
        Assert.Equal(6.1, list[1].WheelHue!.Value, 6);
        Assert.Equal(126.1, list[2].WheelHue!.Value, 6);
        Assert.All(list, g => Assert.Equal(0.63, g.WheelSaturation!.Value, 9));
    }

    [Fact]
    public void Harmony_offsets_shift_angle_and_saturation()
    {
        var state = new ColorState(246.1, 0.63, 0.74);
        (double HueDelta, double SatDelta)[] offsets = [default, (10, 0.05), default];
        var list = PaletteGenerator.Harmony(state, HarmonyKind.Triadic, colorOffsets: offsets);
        // índice de la armonía a +120° se desplaza +10° de hue y +0.05 de saturación
        Assert.Equal((246.1 + 120 + 10) % 360, list[1].WheelHue!.Value, 6);
        Assert.Equal(0.68, list[1].WheelSaturation!.Value, 9);
        // el resto conserva la geometría canónica (+240° con base 246.1° → 126.1°)
        Assert.Equal(126.1, list[2].WheelHue!.Value, 6);
        Assert.Equal(0.63, list[2].WheelSaturation!.Value, 9);
    }

    [Fact]
    public void Harmony_offsets_ignore_the_base()
    {
        var state = new ColorState(246.1, 0.63, 0.74);
        var canonical = PaletteGenerator.Harmony(state, HarmonyKind.Triadic);
        (double, double)[] offsets = [(15, 0.1), default, default];
        var shifted = PaletteGenerator.Harmony(state, HarmonyKind.Triadic, colorOffsets: offsets);
        Assert.Equal(canonical[0], shifted[0]);
        Assert.Equal(canonical[1], shifted[1]);
        Assert.Equal(canonical[2], shifted[2]);
    }

    [Fact]
    public void Harmony_without_or_zero_offsets_matches_canonical()
    {
        var state = new ColorState(246.1, 0.63, 0.74);
        foreach (var kind in Rotations)
        {
            var canonical = PaletteGenerator.Harmony(state, kind);
            var withNull = PaletteGenerator.Harmony(state, kind, colorOffsets: null);
            var withZeros = PaletteGenerator.Harmony(state, kind,
                colorOffsets: Enumerable.Repeat(((double, double))default, canonical.Count).ToList());
            for (var i = 0; i < canonical.Count; i++)
            {
                Assert.Equal(canonical[i], withNull[i]);
                Assert.Equal(canonical[i], withZeros[i]);
            }
        }
    }

    [Fact]
    public void Harmony_offsets_follow_base_rotation()
    {
        (double HueDelta, double SatDelta)[] offsets = [default, (10, 0.05), default];
        var a = PaletteGenerator.Harmony(new ColorState(246.1, 0.63, 0.74), HarmonyKind.Triadic, colorOffsets: offsets);
        var b = PaletteGenerator.Harmony(new ColorState(251.1, 0.63, 0.74), HarmonyKind.Triadic, colorOffsets: offsets);
        // el punto personalizado sigue rígidamente al base (251.1 + 120 + 10 = 381.1 → 21.1°)
        Assert.Equal(21.1, b[1].WheelHue!.Value, 6);
        Assert.Equal(a[1].WheelSaturation!.Value, b[1].WheelSaturation!.Value, 9);
    }

    [Fact]
    public void Harmony_offsets_clamp_saturation_to_gamut()
    {
        var state = new ColorState(246.1, 0.9, 0.74);
        (double HueDelta, double SatDelta)[] offsets = [default, (0, 0.5), (0, -2)];
        var list = PaletteGenerator.Harmony(state, HarmonyKind.Triadic, colorOffsets: offsets);
        Assert.Equal(1, list[1].WheelSaturation!.Value, 9);
        Assert.Equal(0, list[2].WheelSaturation!.Value, 9);
    }

    [Fact]
    public void Lowering_brightness_keeps_hue_and_saturation()
    {
        foreach (var kind in Rotations)
        {
            var bright = PaletteGenerator.Harmony(new ColorState(246.1, 0.63, 0.74), kind);
            var dark = PaletteGenerator.Harmony(new ColorState(246.1, 0.63, 0.15), kind);
            for (var i = 0; i < bright.Count; i++)
            {
                Assert.Equal(bright[i].WheelHue!.Value, dark[i].WheelHue!.Value, 9);
                Assert.Equal(bright[i].WheelSaturation!.Value, dark[i].WheelSaturation!.Value, 9);
            }
        }
    }

    [Fact]
    public void Unbalanced_harmonies_keep_base_brightness()
    {
        foreach (var g in PaletteGenerator.Harmony(new ColorState(30, 0.9, 0.5), HarmonyKind.Tetradic, balanceLightness: false))
            Assert.InRange(Math.Abs(ColorMath.ToHsv(g.Color).V - 0.5), 0, 0.003);
    }

    [Theory]
    [MemberData(nameof(ScaleInputs))]
    public void Design_scale_is_strictly_decreasing_and_anchored(string hex)
    {
        var c = ColorParser.Parse(hex)!.Value;
        foreach (var mode in new[] { ScaleAnchorMode.Fixed500, ScaleAnchorMode.Automatic })
        {
            var scale = DesignScale.Generate(c, mode);
            Assert.Equal(11, scale.Count);
            Assert.Single(scale, s => s.IsBase);
            Assert.Equal(c, scale.Single(s => s.IsBase).Color);
            var l = scale.Select(s => ColorMath.ToOklab(s.Color).L).ToArray();
            for (var i = 1; i < l.Length; i++)
                Assert.True(l[i] < l[i - 1], $"{hex} {mode}: L[{i}]={l[i]:0.000} >= L[{i - 1}]={l[i - 1]:0.000}");
        }
    }

    [Fact]
    public void Design_scale_base_at_500()
    {
        var scale = DesignScale.Generate(Violet, ScaleAnchorMode.Fixed500);
        Assert.Equal("500", scale[5].Label);
        Assert.Equal(Violet, scale[5].Color);
    }

    [Fact]
    public void Automatic_anchor_places_light_color_on_light_step_with_distinct_steps()
    {
        var yellow = ColorParser.Parse("#FACC15")!.Value;
        var scale = DesignScale.Generate(yellow, ScaleAnchorMode.Automatic);
        var anchor = scale.FindIndex(s => s.IsBase);
        Assert.True(anchor <= 3, $"anchor {scale[anchor].Label}");
        var l = scale.Select(s => ColorMath.ToOklab(s.Color).L).ToArray();
        for (var i = 1; i < l.Length; i++) Assert.True(l[i - 1] - l[i] >= 0.019, $"ΔL {l[i - 1] - l[i]:0.000} at {i}");
    }

    [Fact]
    public void Design_scale_golden_5246BC()
    {
        // Referencia "golden": si se ajustan las constantes de la curva, actualizar con revisión visual.
        var scale = DesignScale.Generate(Violet).Select(s => s.Color.ToHex()).ToArray();
        Assert.Equal("#5246BC", scale[5]);
        Assert.All(scale[..5], h => Assert.True(ColorMath.ToOklab(ColorParser.Parse(h)!.Value).L > ColorMath.ToOklab(Violet).L));
    }

    [Fact]
    public void Neutrals_have_low_chroma_and_gray_input_gives_pure_grays()
    {
        foreach (var g in PaletteGenerator.Neutrals(Violet))
            Assert.True(ColorMath.ToOklch(g.Color).C <= 0.02, $"{g.Color} C={ColorMath.ToOklch(g.Color).C}");

        foreach (var g in PaletteGenerator.Neutrals(Argb.FromRgb(128, 128, 128)))
        {
            Assert.Equal(g.Color.R, g.Color.G);
            Assert.Equal(g.Color.G, g.Color.B);
        }
    }

    [Fact]
    public void Dominant_colors_are_deterministic_and_find_main_colors()
    {
        const int w = 300, h = 200;
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var o = (y * w + x) * 4;
            var (r, g, b) = x < 150 ? (82, 70, 188) : y < 100 ? (255, 138, 0) : (34, 197, 94);
            px[o] = (byte)b; px[o + 1] = (byte)g; px[o + 2] = (byte)r; px[o + 3] = 255;
        }
        var a = DominantColors.Extract(px, w, h, w * 4, 3);
        var b2 = DominantColors.Extract(px, w, h, w * 4, 3);
        Assert.Equal(a, b2);
        Assert.Equal(Violet, a[0]); // la mitad de la imagen
        Assert.Contains(Argb.FromRgb(255, 138, 0), a);
        Assert.Contains(Argb.FromRgb(34, 197, 94), a);
    }

    [Fact]
    public void Dominant_colors_large_image_is_fast()
    {
        const int w = 6000, h = 4000;
        var px = new byte[w * h * 4];
        new Random(1).NextBytes(px);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = DominantColors.Extract(px, w, h, w * 4, 6);
        sw.Stop();
        Assert.NotEmpty(result);
        Assert.True(sw.ElapsedMilliseconds < 1000, $"{sw.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void Dominant_colors_clamps_count_to_1_and_64()
    {
        const int w = 300, h = 200;
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var o = (y * w + x) * 4;
            var (r, g, b) = x < 150 ? (82, 70, 188) : y < 100 ? (255, 138, 0) : (34, 197, 94);
            px[o] = (byte)b; px[o + 1] = (byte)g; px[o + 2] = (byte)r; px[o + 3] = 255;
        }
        var one = DominantColors.Extract(px, w, h, w * 4, 0); // fuera de rango → mínimo 1
        Assert.Single(one);

        // 64 franjas constantes: con k=64 extrae 64 colores distintos
        const int sw = 256, sh = 128;
        var stripes = new byte[sw * sh * 4];
        for (var y = 0; y < sh; y++)
        for (var x = 0; x < sw; x++)
        {
            var o = (y * sw + x) * 4;
            var v = (byte)(x * 64 / sw); // franja de 4 px
            stripes[o] = v; stripes[o + 1] = v; stripes[o + 2] = v; stripes[o + 3] = 255;
        }
        var many = DominantColors.Extract(stripes, sw, sh, sw * 4, 64);
        Assert.Equal(64, many.Distinct().Count());

        var max = DominantColors.Extract(px, w, h, w * 4, 70); // nunca más de 64
        Assert.InRange(max.Count, 1, DominantColors.MaxCount);
    }

    private static double HueDistance(double a, double b) => Math.Abs(((a - b) % 360 + 540) % 360 - 180);
}
