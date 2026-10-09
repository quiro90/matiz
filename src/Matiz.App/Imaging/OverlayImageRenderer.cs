using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Formatting;

namespace Matiz.App.Imaging;

public enum OverlayShape { Square, Circle, Triangle }

public sealed record OverlayImageOptions(
    bool DarkBackground = false,
    bool Transparent = false,
    int Scale = 2,
    OverlayShape Shape = OverlayShape.Square,
    bool Reverse = false,
    bool ShowHsl = false,
    bool ShowCmyk = false);

/// <summary>
/// Compone la imagen de "superpuestos": título de la paleta, una pila de capas apiladas una sobre otra
/// con tamaño decreciente incremental (cada capa muestra un anillo o borde visible de la anterior,
/// sin combinar colores; menos colores → anillos más anchos, hasta 64 → angostos pero visibles) y,
/// a la derecha, el listado de valores (nombre, HEX, RGB y opcionalmente HSL/CMYK) en el orden visual
/// de la pila. Las capas pueden ser cuadradas, circulares o triangulares.
/// </summary>
public static class OverlayImageRenderer
{
    private const double Pad = 40, TitleH = 58, LineH = 19, EntryGap = 14, TextGap = 18, TextW = 260;
    private const double BaseDip = 480, MinRing = 4, MinLayer = 10;

    private static readonly Typeface Title = new(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Bold = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Mono = new(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    private static int Lines(OverlayImageOptions o) => 3 + (o.ShowHsl ? 1 : 0) + (o.ShowCmyk ? 1 : 0);

    /// <summary>Ancho de la capa mayor: crece si hace falta para que cada anillo quede visible (&#8805; MinRing).</summary>
    private static double StackBase(int count)
    {
        var b = BaseDip;
        if (b / (2.0 * (count + 1)) < MinRing) b = 2 * (count + 1) * MinRing;
        return b;
    }

    /// <summary>Disminución de tamaño entre capas consecutivas; el anillo visible queda en step/2.</summary>
    private static double Step(int count) => StackBase(count) / (count + 1);

    public static Size LayoutSize(int count, OverlayImageOptions o)
    {
        count = Math.Max(1, count);
        var textH = TextHeight(count, Lines(o));
        var contentH = Math.Max(StackBase(count), textH);
        return new Size(Pad * 2 + StackBase(count) + TextGap + TextW, Pad + TitleH + contentH + Pad);
    }

    private static double TextHeight(int count, int lines) => count * lines * LineH + Math.Max(0, count - 1) * EntryGap;

    public static BitmapSource Render(PaletteExportModel palette, OverlayImageOptions o)
    {
        var size = LayoutSize(palette.Colors.Count, o);
        var scale = Math.Clamp(o.Scale, 1, 4);
        var visual = new DrawingVisual();
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Grayscale);
        var dark = o.DarkBackground && !o.Transparent;
        var bg = dark ? Color.FromRgb(24, 24, 27) : Colors.White;
        var fg = new SolidColorBrush(dark ? Color.FromRgb(240, 240, 240) : Color.FromRgb(24, 24, 27));
        var muted = new SolidColorBrush(dark ? Color.FromRgb(160, 160, 166) : Color.FromRgb(100, 100, 108));
        var border = new Pen(new SolidColorBrush(dark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0)), 1);

        // Capas apiladas en el orden elegido: la primera es la capa mayor.
        var colors = (o.Reverse ? palette.Colors.Reverse() : palette.Colors).ToList();
        var count = Math.Max(1, colors.Count);
        var b = StackBase(count);
        var step = Step(count);
        var lines = Lines(o);
        var top = Pad + TitleH;
        var contentH = Math.Max(b, TextHeight(count, lines));
        var cx = Pad + b / 2;
        var cy = top + contentH / 2;

        using (var dc = visual.RenderOpen())
        {
            if (!o.Transparent) dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(size));
            dc.DrawText(Text(palette.Name, Title, 28, fg), new Point(Pad, Pad - 4));

            for (var k = 0; k < colors.Count; k++)
            {
                var c = colors[k];
                var s = Math.Max(b - k * step, MinLayer);
                var r = new Rect(cx - s / 2, cy - s / 2, s, s);
                var brush = new SolidColorBrush(Color.FromArgb(c.Color.A, c.Color.R, c.Color.G, c.Color.B));
                switch (o.Shape)
                {
                    case OverlayShape.Circle:
                        dc.DrawEllipse(brush, border, new Point(r.X + r.Width / 2, r.Y + r.Height / 2), r.Width / 2, r.Height / 2);
                        break;
                    case OverlayShape.Triangle:
                        dc.DrawGeometry(brush, border, Triangle(r));
                        break;
                    default:
                        dc.DrawRectangle(brush, border, r);
                        break;
                }
            }

            var x = Pad + b + TextGap;
            var y = top;
            for (var i = 0; i < colors.Count; i++)
            {
                var c = colors[i];
                var name = string.IsNullOrWhiteSpace(c.Name) ? $"Color {i + 1}" : c.Name!;
                var t = new Point(x, y);
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
                y += lines * LineH + EntryGap;
            }
        }

        var rtb = new RenderTargetBitmap((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        return rtb;

        FormattedText Text(string s, Typeface tf, double em, Brush br) =>
            new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, em, br, scale);
    }

    /// <summary>Triángulo isósceles con vértice hacia arriba, inscripto en el rectángulo de la capa.</summary>
    private static Geometry Triangle(Rect r)
    {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point((r.Left + r.Right) / 2, r.Top), true, true);
            ctx.LineTo(new Point(r.Right, r.Bottom), true, false);
            ctx.LineTo(new Point(r.Left, r.Bottom), true, false);
        }
        geo.Freeze();
        return geo;
    }
}