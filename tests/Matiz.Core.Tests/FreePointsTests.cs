using Matiz.Core.Colors;
using Matiz.Core.Generation;

namespace Matiz.Core.Tests;

public class FreePointsTests
{
    private static readonly ColorState VioletState = new(246.1, 0.63, 0.74);

    [Fact]
    public void Empty_offsets_give_base_only()
    {
        var list = PaletteGenerator.FreePoints(VioletState, []);
        Assert.Single(list);
        Assert.True(list[0].IsBase);
        Assert.Equal(VioletState.Argb, list[0].Color);
    }

    [Fact]
    public void Points_are_relative_to_the_base()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(120, 0, null)], balanceLightness: false);
        Assert.Equal(2, list.Count);
        Assert.True(list[0].IsBase);
        // 246.1° + 120° = 366.1° → 6.1°
        Assert.Equal(6.1, list[1].WheelHue!.Value, 6);
        Assert.Equal(0.63, list[1].WheelSaturation!.Value, 9);
        Assert.All(list, g => Assert.Equal(HsvToV(VioletState.Argb), HsvToV(g.Color), 6));
        Assert.Equal("1", list[1].Label);
    }

    [Fact]
    public void Labels_are_consecutive_numbers()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(90, 0, null), (180, 0, null)]);
        Assert.Equal(["Base", "1", "2"], list.Select(g => g.Label));
    }

    [Fact]
    public void Negative_saturation_delta_clamps_to_gamut()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(0, -2, null), (10, 0.5, null)], balanceLightness: false);
        Assert.Equal(0, list[1].WheelSaturation!.Value, 9);
        Assert.Equal(1, list[2].WheelSaturation!.Value, 9);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(120, 0)]
    [InlineData(180, -0.3)]
    [InlineData(-45, 0.2)]
    public void Balanced_points_match_base_lightness(double hueDelta, double satDelta)
    {
        var baseL = PaletteGenerator.LightnessOf(VioletState.Hue, VioletState.Saturation, VioletState.Value);
        var g = PaletteGenerator.FreePoints(VioletState, [(hueDelta, satDelta, null)])[1];
        var reachable = g.WheelSaturation!.Value > 0 && PaletteGenerator.LightnessOf(g.WheelHue!.Value, g.WheelSaturation.Value, 1) >= baseL;
        if (reachable) Assert.InRange(Math.Abs(ColorMath.ToOklab(g.Color).L - baseL), 0, 0.01);
        else Assert.Equal(1, HsvToV(g.Color), 2); // no alcanzable: brillo máximo
    }

    [Fact]
    public void All_points_are_valid_srgb()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(120, 0, null), (30, -0.5, null), (300, 0.4, null), (400, 1.2, null)]);
        Assert.All(list, g =>
        {
            var hsv = ColorMath.ToHsv(g.Color);
            Assert.InRange(g.WheelHue!.Value, 0, 360);
            Assert.InRange(g.WheelSaturation!.Value, 0, 1);
            Assert.InRange(hsv.H, 0, 360);
            Assert.StartsWith("#", g.Color.ToHex());
        });
    }

    [Fact]
    public void Points_follow_base_rotation_rigidly()
    {
        var a = PaletteGenerator.FreePoints(VioletState, [(120, 0, null)], balanceLightness: false);
        var b = PaletteGenerator.FreePoints(new ColorState(251.1, 0.63, 0.74), [(120, 0, null)], balanceLightness: false);
        Assert.Equal(11.1, b[1].WheelHue!.Value, 6); // 251.1 + 120
        Assert.Equal(6.1, a[1].WheelHue!.Value, 6);  // 246.1 + 120
        Assert.Equal(a[1].WheelSaturation!.Value, b[1].WheelSaturation!.Value, 9);
    }

    [Fact]
    public void Explicit_value_delta_reproduces_exact_value()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(120, 0, -0.04)], balanceLightness: false);
        var hsv = ColorMath.ToHsv(list[1].Color);
        Assert.Equal(6.1, list[1].WheelHue!.Value, 6);
        Assert.Equal(0.63, list[1].WheelSaturation!.Value, 9);
        // el color se cuantiza a 8 bits (r=179, g=77, b=66): V ≈ 0.70 con error ≤ 0.5/255
        Assert.Equal(VioletState.Value - 0.04, hsv.V, 2);
    }

    [Fact]
    public void Explicit_value_delta_is_immune_to_balance()
    {
        var with = PaletteGenerator.FreePoints(VioletState, [(120, 0, 0.25)], balanceLightness: true);
        var without = PaletteGenerator.FreePoints(VioletState, [(120, 0, 0.25)], balanceLightness: false);
        Assert.Equal(with[1].Color, without[1].Color);
        // cuantización 8 bits: error ≤ 0.5/255
        Assert.Equal(Math.Clamp(VioletState.Value + 0.25, 0, 1), ColorMath.ToHsv(with[1].Color).V, 2);
    }

    [Fact]
    public void Explicit_value_delta_clamps_to_gamut()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(0, 0, -1), (0, 0, 2)]);
        Assert.Equal(0, ColorMath.ToHsv(list[1].Color).V, 9);
        Assert.Equal(1, ColorMath.ToHsv(list[2].Color).V, 9);
    }

    [Fact]
    public void Mixed_offsets_use_own_value_only_when_explicit()
    {
        var list = PaletteGenerator.FreePoints(VioletState, [(90, 0, null), (180, 0, 0.5)]);
        var targetL = PaletteGenerator.LightnessOf(VioletState.Hue, VioletState.Saturation, VioletState.Value); // sin brillo propio: regla de Equilibrar
        var ruleV = PaletteGenerator.ValueForLightness(ColorMath.NormalizeHue(VioletState.Hue + 90), 0.63, targetL);
        // cuantización 8 bits: error ≤ 0.5/255
        Assert.Equal(ruleV, ColorMath.ToHsv(list[1].Color).V, 2);
        Assert.Equal(1, ColorMath.ToHsv(list[2].Color).V, 9); // 0.74 + 0.5 → clamp a 1
    }

    private static double HsvToV(Argb c) => ColorMath.ToHsv(c).V;
}