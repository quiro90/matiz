using Matiz.Core.Colors;
using Matiz.Core.Formatting;
using Matiz.Core.Parsing;

namespace Matiz.Core.Tests;

public class ParsingAndFormattingTests
{
    private static readonly Argb Violet = Argb.FromRgb(82, 70, 188);

    [Theory]
    [InlineData("5246bc")]
    [InlineData("#5246BC")]
    [InlineData("  #5246bc ")]
    [InlineData("rgb(82, 70, 188)")]
    [InlineData("RGB(82 70 188)")]
    [InlineData("rgba(82, 70, 188, 1)")]
    [InlineData("82 70 188")]
    [InlineData("82,70,188")]
    [InlineData("0xFF5246BC")]
    [InlineData("0x5246BC")]
    [InlineData("#FF5246BC")]
    [InlineData("rgb(82, 70, 188);")]
    public void Equivalent_inputs_parse_to_5246BC(string input)
    {
        Assert.True(ColorParser.TryParse(input, out var c));
        Assert.Equal(Violet, c);
    }

    [Fact]
    public void Short_hex_is_expanded()
    {
        Assert.Equal("#55AACC", ColorParser.Parse("#5AC")!.Value.ToHex());
    }

    [Fact]
    public void Eight_digit_hex_is_argb()
    {
        var c = ColorParser.Parse("#805246BC")!.Value;
        Assert.Equal(new Argb(128, 82, 70, 188), c);
    }

    [Theory]
    [InlineData("hsl(246, 47%, 51%)")]
    [InlineData("hsl(246deg 47% 51%)")]
    [InlineData("hsv(246, 63%, 74%)")]
    [InlineData("hsb(246°, 63%, 74%)")]
    public void Hsl_and_hsv_functions(string input)
    {
        Assert.True(ColorParser.TryParse(input, out var c));
        // Los valores redondeados no reproducen el byte exacto: tolerancia de ±3 por canal.
        Assert.InRange(Math.Abs(c.R - 82), 0, 3);
        Assert.InRange(Math.Abs(c.G - 70), 0, 3);
        Assert.InRange(Math.Abs(c.B - 188), 0, 3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rgb(300, 0, 0)")]
    [InlineData("rgb(-1, 0, 0)")]
    [InlineData("rgb(1, 2)")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("hsl(400, 50%, 50%)")]
    [InlineData("hsl(200, 150%, 50%)")]
    [InlineData("256 0 0")]
    [InlineData("rgba(1, 2, 3, 2)")]
    [InlineData("banana")]
    public void Invalid_inputs_are_rejected(string input)
    {
        Assert.False(ColorParser.TryParse(input, out _));
    }

    [Fact]
    public void Code_formats()
    {
        Assert.Equal("rgb(82, 70, 188)", ColorFormats.CssRgb(Violet));
        Assert.Equal("hsl(246, 47%, 51%)", ColorFormats.CssHsl(Violet));
        Assert.Equal("Color.FromRgb(82, 70, 188)", ColorFormats.CSharpWpf(Violet));
        Assert.Equal("Color.FromArgb(255, 82, 70, 188)", ColorFormats.CSharpArgb(Violet));
        Assert.Equal("#FF5246BC", ColorFormats.Xaml(Violet));
        Assert.Equal("Color(0xFF5246BC)", ColorFormats.Dart(Violet));
        Assert.Equal("0xFF5246BC", ColorFormats.ArgbInt(Violet));
        Assert.StartsWith("oklch(0.", ColorFormats.CssOklch(Violet));
        Assert.DoesNotContain(",", ColorFormats.CssOklch(Violet)); // cultura invariante
    }

    [Fact]
    public void Alpha_formats()
    {
        var c = new Argb(128, 82, 70, 188);
        Assert.Equal("rgba(82, 70, 188, 0.5)", ColorFormats.CssRgb(c));
        Assert.Equal("#805246BC", ColorFormats.ArgbHex(c));
        Assert.Equal("Color.FromArgb(128, 82, 70, 188)", ColorFormats.CSharpWpf(c));
    }

    [Fact]
    public void Copy_all_block()
    {
        var expected = string.Join(Environment.NewLine,
            "HEX: #5246BC", "RGB: 82, 70, 188", "HSL: 246°, 47%, 51%", "HSV: 246°, 63%, 74%", "CMYK: 56%, 63%, 0%, 26%");
        Assert.Equal(expected, ColorFormats.CopyAll(Violet));
    }

    [Fact]
    public void Hex_options()
    {
        Assert.Equal("5246bc", ColorFormats.Hex(Violet, new FormatOptions(HexUppercase: false, HexHash: false)));
    }

    [Fact]
    public void Formatter_registry_lookup()
    {
        Assert.Equal("Color(0xFF5246BC)", ColorFormatters.Get("dart").Format(Violet, FormatOptions.Default));
        Assert.Equal("hex", ColorFormatters.Get("unknown").Id);
        Assert.Equal(ColorFormatters.All.Count, ColorFormatters.All.Select(f => f.Id).Distinct().Count());
    }
}
