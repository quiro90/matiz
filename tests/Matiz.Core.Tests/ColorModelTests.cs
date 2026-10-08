using Matiz.Core.Colors;
using Matiz.Core.Formatting;
using Matiz.Core.Parsing;

namespace Matiz.Core.Tests;

public class ColorModelTests
{
    private static readonly Argb Violet = Argb.FromRgb(82, 70, 188);

    [Fact]
    public void Rounding_is_away_from_zero_at_midpoint()
    {
        Assert.Equal(128, Argb.ToByte(127.5 / 255));
        Assert.Equal(0, Argb.ToByte(-0.1));
        Assert.Equal(255, Argb.ToByte(1.2));
    }

    [Fact]
    public void Reference_conversions_of_5246BC()
    {
        Assert.Equal("#5246BC", ColorFormats.Hex(Violet));
        Assert.Equal("82, 70, 188", ColorFormats.Rgb(Violet));
        Assert.Equal("246°, 47%, 51%", ColorFormats.Hsl(Violet));
        Assert.Equal("246°, 63%, 74%", ColorFormats.Hsv(Violet));
        Assert.Equal("56%, 63%, 0%, 26%", ColorFormats.Cmyk(Violet));
    }

    [Theory]
    [InlineData("#FF0000", "0°, 100%, 50%", "0°, 100%, 100%", "0%, 100%, 100%, 0%")]
    [InlineData("#00FF00", "120°, 100%, 50%", "120°, 100%, 100%", "100%, 0%, 100%, 0%")]
    [InlineData("#0000FF", "240°, 100%, 50%", "240°, 100%, 100%", "100%, 100%, 0%, 0%")]
    [InlineData("#FFFFFF", "0°, 0%, 100%", "0°, 0%, 100%", "0%, 0%, 0%, 0%")]
    [InlineData("#000000", "0°, 0%, 0%", "0°, 0%, 0%", "0%, 0%, 0%, 100%")]
    [InlineData("#808080", "0°, 0%, 50%", "0°, 0%, 50%", "0%, 0%, 0%, 50%")]
    public void Primaries_and_neutrals(string hex, string hsl, string hsv, string cmyk)
    {
        var c = ColorParser.Parse(hex)!.Value;
        Assert.Equal(hsl, ColorFormats.Hsl(c));
        Assert.Equal(hsv, ColorFormats.Hsv(c));
        Assert.Equal(cmyk, ColorFormats.Cmyk(c));
    }

    [Fact]
    public void Exhaustive_round_trip_hsv_and_hsl()
    {
        // Los 16,7 M colores de 24 bits: RGB → HSV/HSL (double) → RGB sin deriva.
        Parallel.For(0, 256, r =>
        {
            for (var g = 0; g < 256; g++)
            for (var b = 0; b < 256; b++)
            {
                var c = Argb.FromRgb((byte)r, (byte)g, (byte)b);
                var hsv = ColorMath.ToHsv(c);
                var back = ColorMath.FromHsv(hsv.H, hsv.S, hsv.V);
                if (back != c) Assert.Fail($"HSV {c} → {back}");
                var hsl = ColorMath.ToHsl(c);
                var back2 = ColorMath.FromHsl(hsl.H, hsl.S, hsl.L);
                if (back2 != c) Assert.Fail($"HSL {c} → {back2}");
            }
        });
    }

    [Fact]
    public void Oklab_round_trip_sampled()
    {
        var rnd = new Random(7);
        for (var i = 0; i < 200_000; i++)
        {
            var c = Argb.FromUInt32(0xFF000000u | (uint)rnd.Next(0, 1 << 24));
            var back = ColorMath.FromLinear(ColorMath.ToLinear(ColorMath.ToOklab(ColorMath.ToOklch(c))));
            Assert.Equal(c, back);
        }
    }

    [Fact]
    public void Oklch_of_white_and_black()
    {
        var white = ColorMath.ToOklch(Argb.FromRgb(255, 255, 255));
        Assert.Equal(1.0, white.L, 3);
        Assert.True(white.C < 1e-4);
        Assert.Equal(0.0, ColorMath.ToOklch(Argb.FromRgb(0, 0, 0)).L, 6);
    }

    [Fact]
    public void Gamut_mapping_keeps_lightness_and_hue()
    {
        var target = new Oklch(0.7, 0.4, 150); // fuera de gama sRGB
        var mapped = ColorMath.FromOklchGamutMapped(target);
        var lch = ColorMath.ToOklch(mapped);
        Assert.Equal(0.7, lch.L, 2);
        Assert.InRange(Math.Abs(lch.H - 150), 0, 2);
        Assert.True(lch.C < 0.4);
    }

    [Fact]
    public void Gray_equivalent_preserves_perceptual_lightness()
    {
        var gray = ColorMath.GrayEquivalent(Violet);
        Assert.Equal(gray.R, gray.G);
        Assert.Equal(gray.G, gray.B);
        Assert.InRange(Math.Abs(ColorMath.ToOklab(gray).L - ColorMath.ToOklab(Violet).L), 0, 0.005);
    }

    [Fact]
    public void Achromatic_state_preserves_previous_hue()
    {
        var violet = ColorState.FromArgb(Violet);
        var gray = ColorState.FromArgb(Argb.FromRgb(128, 128, 128), violet);
        Assert.Equal(violet.Hue, gray.Hue, 9);
        Assert.Equal(0, gray.Saturation);
        Assert.Equal(Argb.FromRgb(128, 128, 128), gray.Argb);

        var black = ColorState.FromArgb(Argb.FromRgb(0, 0, 0), violet);
        Assert.Equal(violet.Hue, black.Hue, 9);
        Assert.Equal(violet.Saturation, black.Saturation, 9);
    }

    [Fact]
    public void Darken_to_black_and_back_recovers_color()
    {
        var s = ColorState.FromArgb(Violet);
        var dark = s.WithValue(0);
        Assert.Equal(Argb.FromRgb(0, 0, 0), dark.Argb);
        Assert.Equal(Violet, dark.WithValue(s.Value).Argb);
    }

    [Fact]
    public void Brightness_37_percent_gives_29235E()
    {
        var s = ColorState.FromArgb(Violet).WithValue(0.37);
        Assert.Equal("#29235E", s.Argb.ToHex());
    }

    [Fact]
    public void Wheel_edge_at_120_is_pure_green_and_center_is_white()
    {
        var (h, sat) = WheelMapping.FromPoint(Math.Sin(120 * Math.PI / 180) * 100, -Math.Cos(120 * Math.PI / 180) * 100, 100, 1);
        Assert.Equal("#00FF00", new ColorState(h, sat, 1).Argb.ToHex());
        var (_, s0) = WheelMapping.FromPoint(0, 0, 100, 1);
        Assert.Equal("#FFFFFF", new ColorState(0, s0, 1).Argb.ToHex());
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void Wheel_mapping_round_trip(double focus)
    {
        var gamma = WheelMapping.GammaFromFocus(focus);
        for (var hue = 0.0; hue < 360; hue += 13.7)
        for (var sat = 0.05; sat <= 1; sat += 0.05)
        {
            var (x, y) = WheelMapping.ToPoint(hue, sat, gamma);
            var (h2, s2) = WheelMapping.FromPoint(x * 200, y * 200, 200, gamma);
            Assert.Equal(sat, s2, 9);
            Assert.InRange(Math.Abs(((h2 - hue + 540) % 360) - 180), 0, 1e-6);
        }
    }

    [Fact]
    public void Pastel_focus_gives_more_than_half_radius_to_low_saturation()
    {
        var gamma = WheelMapping.GammaFromFocus(1);
        var (x, y) = WheelMapping.ToPoint(0, 0.30, gamma);
        Assert.True(Math.Sqrt(x * x + y * y) > 0.5);
    }

    [Fact]
    public void Wheel_clamps_outside_to_full_saturation()
    {
        var (_, s) = WheelMapping.FromPoint(300, 0, 100, 1);
        Assert.Equal(1, s);
    }
}
