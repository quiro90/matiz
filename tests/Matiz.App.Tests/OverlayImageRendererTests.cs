using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.App.Imaging;
using Matiz.Core.Colors;
using Matiz.Core.Export;

namespace Matiz.App.Tests;

public class OverlayImageRendererTests
{
    private static readonly PaletteExportModel Eight = new("Stack",
    [
        new("C1", Argb.FromRgb(1, 1, 1)),
        new("C2", Argb.FromRgb(2, 2, 2)),
        new("C3", Argb.FromRgb(3, 3, 3)),
        new("C4", Argb.FromRgb(4, 4, 4)),
        new("C5", Argb.FromRgb(5, 5, 5)),
        new("C6", Argb.FromRgb(6, 6, 6)),
        new("C7", Argb.FromRgb(7, 7, 7)),
        new("C8", Argb.FromRgb(8, 8, 8)),
    ]);

    // Geometría a escala 1× con 8 colores: base 480, step 480/9 ≈ 53,33 ⇒ cx=280; alto de texto 554 ⇒ cy=375.
    private const double CenterX = 280, CenterY = 375;

    /// <summary>Radio medio del anillo visible de la capa k (entre la capa k y la k+1).</summary>
    private static double RingRadius(int k)
    {
        var step = 480.0 / 9;
        return (480 - k * step) / 2 - step / 4;
    }

    private static T RunSta<T>(Func<T> f)
    {
        T result = default!;
        Exception? error = null;
        var t = new Thread(() =>
        {
            try { result = f(); }
            catch (Exception ex) { error = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (error is not null) throw error;
        return result;
    }

    private static (byte A, byte R, byte G, byte B) Decode(BitmapSource bitmap, double x, double y)
    {
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var px = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(px, stride, 0);
        var o = (int)y * stride + (int)x * 4;
        return (px[o + 3], px[o + 2], px[o + 1], px[o]);
    }

    /// <summary>Punto de muestreo del anillo k según la forma (eje horizontal para cuadrado/círculo, eje vertical para triángulo).</summary>
    private static (double X, double Y) RingPoint(OverlayShape shape, int k) => shape switch
    {
        OverlayShape.Triangle => (CenterX, CenterY + RingRadius(k)),
        _ => (CenterX + RingRadius(k), CenterY),
    };

    [Theory]
    [InlineData(OverlayShape.Square)]
    [InlineData(OverlayShape.Circle)]
    public void Stacks_forward_order_with_visible_rings(OverlayShape shape)
    {
        var d = RunSta(() =>
        {
            var bmp = OverlayImageRenderer.Render(Eight, new OverlayImageOptions(Scale: 1, Shape: shape, Reverse: false));
            var ring0 = RingPoint(shape, 0);
            var ring3 = RingPoint(shape, 3);
            return (
                center: Decode(bmp, CenterX, CenterY),
                ring0: Decode(bmp, ring0.X, ring0.Y),
                ring3: Decode(bmp, ring3.X, ring3.Y),
                corner: Decode(bmp, 5, 5));
        });

        Assert.Equal((255, 8, 8, 8), d.center);
        Assert.Equal((255, 1, 1, 1), d.ring0);
        Assert.Equal((255, 4, 4, 4), d.ring3);
        Assert.Equal((255, 255, 255, 255), d.corner);
    }

    [Fact]
    public void Reverse_order_puts_last_color_on_top()
    {
        var d = RunSta(() =>
        {
            var bmp = OverlayImageRenderer.Render(Eight, new OverlayImageOptions(Scale: 1, Shape: OverlayShape.Square, Reverse: true));
            return (center: Decode(bmp, CenterX, CenterY), ring0: Decode(bmp, CenterX + RingRadius(0), CenterY));
        });

        Assert.Equal((255, 1, 1, 1), d.center);
        Assert.Equal((255, 8, 8, 8), d.ring0);
    }

    [Fact]
    public void Triangle_leaves_background_outside_the_shape()
    {
        var d = RunSta(() =>
        {
            var bmp = OverlayImageRenderer.Render(Eight, new OverlayImageOptions(Scale: 1, Shape: OverlayShape.Triangle, Reverse: false));
            return (
                center: Decode(bmp, CenterX, CenterY),
                ring0: Decode(bmp, CenterX, CenterY + RingRadius(0)),
                ring3: Decode(bmp, CenterX, CenterY + RingRadius(3)),
                side: Decode(bmp, CenterX + 239, CenterY));
        });

        Assert.Equal((255, 8, 8, 8), d.center);
        Assert.Equal((255, 1, 1, 1), d.ring0);
        Assert.Equal((255, 4, 4, 4), d.ring3);
        // A la altura del centro el triángulo es más angosto que 239 DIP: se ve el fondo.
        Assert.Equal((255, 255, 255, 255), d.side);
    }

    [Fact]
    public void Circle_covers_only_inside_the_shape()
    {
        var d = RunSta(() =>
        {
            var bmp = OverlayImageRenderer.Render(Eight, new OverlayImageOptions(Scale: 1, Shape: OverlayShape.Circle, Reverse: false));
            return (
                inside: Decode(bmp, CenterX + RingRadius(0), CenterY),
                outside: Decode(bmp, CenterX + 239, CenterY - 239));
        });

        Assert.Equal((255, 1, 1, 1), d.inside);
        // La diagonal a 239 DIP cae fuera del círculo (radio 240 ⇒ distancia ≈ 338): se ve el fondo.
        Assert.Equal((255, 255, 255, 255), d.outside);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(64)]
    public void Renders_for_extreme_counts(int count)
    {
        var d = RunSta(() =>
        {
            var model = new PaletteExportModel("Extremo", Enumerable.Range(1, count)
                .Select(i => new PaletteExportColor($"C{i}", Argb.FromRgb((byte)i, (byte)(255 - i), (byte)(i * 3))))
                .ToList());
            var o = new OverlayImageOptions(Scale: 1);
            var bmp = OverlayImageRenderer.Render(model, o);
            var size = OverlayImageRenderer.LayoutSize(count, o);

            // Centro de la pila: capa interior con base ajustada y alto de texto a 3 líneas.
            var b = Math.Max(480.0, 2 * (count + 1) * 4);
            var cx = 40 + b / 2;
            var contentH = Math.Max(b, count * 3 * 19 + Math.Max(0, count - 1) * 14.0);
            var cy = 40 + 58 + contentH / 2;
            var stride = bmp.PixelWidth * 4;
            var px = new byte[stride * bmp.PixelHeight];
            bmp.CopyPixels(px, stride, 0);
            var idx = (int)cy * stride + (int)cx * 4;
            return (w: bmp.PixelWidth, h: bmp.PixelHeight, w2: (int)Math.Ceiling(size.Width), h2: (int)Math.Ceiling(size.Height), a: px[idx + 3]);
        });

        Assert.Equal(d.w2, d.w);
        Assert.Equal(d.h2, d.h);
        Assert.Equal(255, d.a);
    }

    [Fact]
    public void Transparent_background_keeps_alpha_and_layer_colors()
    {
        var (bg, center) = RunSta(() =>
        {
            var bmp = OverlayImageRenderer.Render(Eight, new OverlayImageOptions(Scale: 1, Shape: OverlayShape.Square, Transparent: true));
            return (Decode(bmp, 5, 5), Decode(bmp, CenterX, CenterY));
        });
        Assert.Equal(0, bg.A);
        Assert.Equal((A: (byte)255, R: (byte)8, G: (byte)8, B: (byte)8), (center.A, center.R, center.G, center.B));
    }
}