using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Palettes;

namespace Matiz.Core.Tests;

public class GrayscalePreviewTests
{
    private static readonly Argb Violet = Argb.FromRgb(82, 70, 188);
    private static readonly Argb Orange = Argb.FromRgb(0xFF, 0x8A, 0x00);

    [Fact]
    public void MixToGray_at_zero_is_identity_and_at_one_is_gray_equivalent()
    {
        var samples = new[]
        {
            Violet, Orange, Argb.FromRgb(0, 0, 0), Argb.FromRgb(128, 128, 128), Violet with { A = 100 },
        };
        foreach (var c in samples)
        {
            Assert.Equal(c, ColorMath.MixToGray(c, 0));
            Assert.Equal(c, ColorMath.MixToGray(c, -5));
            var g = ColorMath.GrayEquivalent(c);
            Assert.Equal(g, ColorMath.MixToGray(c, 1));
            Assert.Equal(g, ColorMath.MixToGray(c, 5));
            Assert.Equal(c.A, g.A);
            Assert.Equal(g.R, g.G);
            Assert.Equal(g.G, g.B);
        }
    }

    [Fact]
    public void MixToGray_intermediate_lerps_toward_gray_keeping_alpha()
    {
        var c = Violet with { A = 77 };
        var g = ColorMath.GrayEquivalent(c);
        var mix = ColorMath.MixToGray(c, 0.5);
        Assert.Equal(77, mix.A);
        Assert.Equal(Step(c.R, g.R), mix.R);
        Assert.Equal(Step(c.G, g.G), mix.G);
        Assert.Equal(Step(c.B, g.B), mix.B);

        var full = ColorMath.MixToGray(c, 0.7);
        Assert.InRange(full.R, Math.Min(c.R, g.R), Math.Max(c.R, g.R));
        return;

        static byte Step(byte a, byte b) => (byte)Math.Clamp(Math.Round(a + (b - a) * 0.5, MidpointRounding.AwayFromZero), 0, 255);
    }

    [Fact]
    public void Gray_equivalent_keeps_perceptual_lightness()
    {
        Assert.True(Math.Abs(OklabL(ColorMath.GrayEquivalent(Violet)) - OklabL(Violet)) < 0.005);
        Assert.True(Math.Abs(OklabL(ColorMath.GrayEquivalent(Orange)) - OklabL(Orange)) < 0.005);
    }

    [Fact]
    public void Export_model_with_gray_mix_keeps_names_and_mixes_colors()
    {
        var p = new Palette
        {
            Name = "PuchiApp",
            Colors = [PaletteColor.Create(Violet, "Primary"), PaletteColor.Create(Orange, "")],
        };
        var model = PaletteExportModel.From(p);
        Assert.Same(model, model.WithGrayMix(0));

        var gray = model.WithGrayMix(1);
        Assert.Equal(2, gray.Colors.Count);
        Assert.Equal("Primary", gray.Colors[0].Name);
        Assert.Equal("", gray.Colors[1].Name);
        Assert.Equal(ColorMath.GrayEquivalent(Violet), gray.Colors[0].Color);
        Assert.Equal(ColorMath.GrayEquivalent(Orange), gray.Colors[1].Color);
        Assert.Equal(Violet, model.Colors[0].Color);
    }

    [Fact]
    public void Export_model_intermediate_mix_applies_per_color()
    {
        var model = new PaletteExportModel("Demo", [new PaletteExportColor("A", Violet), new PaletteExportColor(null, Orange)]);
        var mix = model.WithGrayMix(0.5);
        Assert.Equal(ColorMath.MixToGray(Violet, 0.5), mix.Colors[0].Color);
        Assert.Equal(ColorMath.MixToGray(Orange, 0.5), mix.Colors[1].Color);
        Assert.Equal("Demo", mix.Name);
    }

    private static double OklabL(Argb c) => ColorMath.ToOklab(c).L;
}