using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.App.Imaging;
using Matiz.App.Services;
using Matiz.Core.Colors;
using Matiz.Core.Export;

namespace Matiz.App.Tests;

public class PaletteImageRendererTests
{
    private static readonly PaletteExportModel Puchi = new("PuchiApp",
    [
        new("Primary", Argb.FromRgb(0x52, 0x46, 0xBC)),
        new("Secondary", Argb.FromRgb(0xFF, 0x8A, 0x00)),
        new("Success", Argb.FromRgb(0x22, 0xC5, 0x5E)),
    ]);

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

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Two_x_is_double_size_and_block_centers_are_exact(bool horizontal, bool dark)
    {
        var (w1, h1, w2, h2, centers) = RunSta(() =>
        {
            var o1 = new PaletteImageOptions(horizontal, 1, dark);
            var o2 = o1 with { Scale = 2 };
            var b1 = PaletteImageRenderer.Render(Puchi, o1);
            var b2 = PaletteImageRenderer.Render(Puchi, o2);

            // Lee el PNG codificado (no solo el bitmap en memoria).
            var path = Path.Combine(Path.GetTempPath(), $"matiz-{Guid.NewGuid():N}.png");
            PaletteImageRenderer.SavePng(b2, path);
            var decoded = new FormatConvertedBitmap(BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad), PixelFormats.Bgra32, null, 0);
            File.Delete(path);

            var stride = decoded.PixelWidth * 4;
            var px = new byte[stride * decoded.PixelHeight];
            decoded.CopyPixels(px, stride, 0);
            var list = new List<Argb>();
            for (var i = 0; i < Puchi.Colors.Count; i++)
            {
                // Centros de bloque según el layout (en DIP) × escala 2.
                double cx, cy;
                if (horizontal) { cx = 40 + i * (176 + 16) + 88; cy = 40 + 58 + 66; }
                else { cx = 40 + 66; cy = 40 + 58 + i * (76 + 16) + 38; }
                var o = (int)(cy * 2) * stride + (int)(cx * 2) * 4;
                list.Add(Argb.FromRgb(px[o + 2], px[o + 1], px[o]));
            }
            return (b1.PixelWidth, b1.PixelHeight, b2.PixelWidth, b2.PixelHeight, list);
        });

        Assert.Equal(w1 * 2, w2);
        Assert.Equal(h1 * 2, h2);
        Assert.Equal(Puchi.Colors.Select(c => c.Color), centers);
    }

    [Fact]
    public void Transparent_background_keeps_alpha_and_block_colors()
    {
        var (bg, block) = RunSta(() =>
        {
            // Horizontal, escala 2, fondo transparente (texto estilo claro).
            var b = PaletteImageRenderer.Render(Puchi, new PaletteImageOptions(true, 2, DarkBackground: false, Transparent: true));
            var path = Path.Combine(Path.GetTempPath(), $"matiz-{Guid.NewGuid():N}.png");
            PaletteImageRenderer.SavePng(b, path);
            var decoded = new FormatConvertedBitmap(BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad), PixelFormats.Bgra32, null, 0);
            File.Delete(path);
            var stride = decoded.PixelWidth * 4;
            var px = new byte[stride * decoded.PixelHeight];
            decoded.CopyPixels(px, stride, 0);
            (byte, byte, byte, byte) At(double x, double y)
            {
                var o = (int)(y * 2) * stride + (int)(x * 2) * 4;
                return (px[o + 3], px[o + 2], px[o + 1], px[o]);
            }
            return (At(5, 5), At(128, 164));
        });

        Assert.Equal((0, 0, 0, 0), bg);
        Assert.Equal((255, 0x52, 0x46, 0xBC), block);
    }
}

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("Alt+C", "Alt+C")]
    [InlineData("ctrl+alt+c", "Ctrl+Alt+C")]
    [InlineData("Win+Shift+C", "Shift+Win+C")]
    public void Parses_and_formats(string input, string expected)
    {
        Assert.True(HotkeyGesture.TryParse(input, out var m, out var k));
        Assert.Equal(expected, HotkeyGesture.Format(m, k));
    }

    [Theory]
    [InlineData("C")]
    [InlineData("Alt+")]
    [InlineData("Foo+C")]
    [InlineData("")]
    public void Rejects_invalid(string input) => Assert.False(HotkeyGesture.TryParse(input, out _, out _));
}
