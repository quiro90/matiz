using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Formatting;

namespace Matiz.App.Imaging;

public sealed record PaletteImageOptions(
    bool Horizontal = true,
    int Scale = 2,
    bool DarkBackground = false,
    bool ShowHsl = false,
    bool ShowCmyk = false);

/// <summary>
/// Compone la imagen de una paleta (título, bloques de color, nombre, HEX, RGB y opcionalmente HSL/CMYK)
/// con DrawingVisual y la rasteriza a escala entera. Los bloques se dibujan sin antialiasing: el centro
/// de cada bloque tiene exactamente el color de la paleta.
/// </summary>
public static class PaletteImageRenderer
{
    private const double Pad = 40, TitleH = 58, Gap = 16, LineH = 19;
    private const double HBlockW = 176, HBlockH = 132;
    private const double VBlockW = 132, VBlockH = 76, VTextW = 260;
    private const int MaxPerRow = 6;

    private static readonly Typeface Title = new(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Bold = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Mono = new(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    public static Size LayoutSize(int count, PaletteImageOptions o)
    {
        var lines = Lines(o);
        count = Math.Max(1, count);
        if (o.Horizontal)
        {
            var cols = Math.Min(count, MaxPerRow);
            var rows = (int)Math.Ceiling(count / (double)cols);
            var cellH = HBlockH + 12 + lines * LineH;
            return new Size(Pad * 2 + cols * HBlockW + (cols - 1) * Gap, Pad + TitleH + rows * cellH + (rows - 1) * Gap * 1.5 + Pad);
        }
        var rowH = Math.Max(VBlockH, lines * LineH);
        return new Size(Pad * 2 + VBlockW + 18 + VTextW, Pad + TitleH + count * rowH + (count - 1) * Gap + Pad);
    }

    public static BitmapSource Render(PaletteExportModel palette, PaletteImageOptions o)
    {
        var size = LayoutSize(palette.Colors.Count, o);
        var scale = Math.Clamp(o.Scale, 1, 4);
        var visual = new DrawingVisual();
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Grayscale);
        var bg = o.DarkBackground ? Color.FromRgb(24, 24, 27) : Colors.White;
        var fg = new SolidColorBrush(o.DarkBackground ? Color.FromRgb(240, 240, 240) : Color.FromRgb(24, 24, 27));
        var muted = new SolidColorBrush(o.DarkBackground ? Color.FromRgb(160, 160, 166) : Color.FromRgb(100, 100, 108));
        var border = new Pen(new SolidColorBrush(o.DarkBackground ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0)), 1);

        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(size));
            dc.DrawText(Text(palette.Name, Title, 28, fg), new Point(Pad, Pad - 4));

            var lines = Lines(o);
            for (var i = 0; i < palette.Colors.Count; i++)
            {
                var c = palette.Colors[i];
                var brush = new SolidColorBrush(Color.FromArgb(c.Color.A, c.Color.R, c.Color.G, c.Color.B));
                Rect block;
                Point text;
                if (o.Horizontal)
                {
                    var cols = Math.Min(palette.Colors.Count, MaxPerRow);
                    var col = i % cols;
                    var row = i / cols;
                    var cellH = HBlockH + 12 + lines * LineH;
                    var x = Pad + col * (HBlockW + Gap);
                    var y = Pad + TitleH + row * (cellH + Gap * 1.5);
                    block = new Rect(x, y, HBlockW, HBlockH);
                    text = new Point(x, y + HBlockH + 10);
                }
                else
                {
                    var rowH = Math.Max(VBlockH, lines * LineH);
                    var y = Pad + TitleH + i * (rowH + Gap);
                    block = new Rect(Pad, y, VBlockW, VBlockH);
                    text = new Point(Pad + VBlockW + 18, y + Math.Max(0, (VBlockH - lines * LineH) / 2));
                }
                dc.DrawRectangle(brush, null, block);
                dc.DrawRectangle(null, border, block);

                var name = string.IsNullOrWhiteSpace(c.Name) ? $"Color {i + 1}" : c.Name!;
                var t = text;
                dc.DrawText(Text(name.ToUpperInvariant(), Bold, 13, fg), t);
                t.Y += LineH;
                dc.DrawText(Text(ColorFormats.Hex(c.Color), Mono, 13, fg), t);
                t.Y += LineH;
                dc.DrawText(Text($"RGB({c.Color.R},{c.Color.G},{c.Color.B})", Mono, 12, muted), t);
                if (o.ShowHsl)
                {
                    t.Y += LineH;
                    dc.DrawText(Text($"HSL({ColorFormats.Hsl(c.Color)})", Mono, 12, muted), t);
                }
                if (o.ShowCmyk)
                {
                    t.Y += LineH;
                    dc.DrawText(Text($"CMYK({ColorFormats.Cmyk(c.Color)})", Mono, 12, muted), t);
                }
            }
        }

        var rtb = new RenderTargetBitmap((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        return rtb;

        FormattedText Text(string s, Typeface tf, double em, Brush b) =>
            new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, em, b, scale);
    }

    public static void SavePng(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var fs = File.Create(path);
        encoder.Save(fs);
    }

    private static int Lines(PaletteImageOptions o) => 3 + (o.ShowHsl ? 1 : 0) + (o.ShowCmyk ? 1 : 0);
}
