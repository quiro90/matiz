using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.App.Interop;
using Matiz.Core.Colors;
using static Matiz.App.Interop.NativeMethods;

namespace Matiz.App.ScreenCapture;

/// <summary>Instantánea congelada de un monitor en píxeles físicos (BGRA, fila a fila).</summary>
internal sealed class MonitorSnapshot
{
    private MonitorSnapshot(RECT bounds, double dpiScale, byte[] pixels)
    {
        Bounds = bounds;
        DpiScale = dpiScale;
        Pixels = pixels;
    }

    /// <summary>Rectángulo del monitor en coordenadas físicas de la pantalla virtual (puede ser negativo).</summary>
    public RECT Bounds { get; }
    public double DpiScale { get; }
    public byte[] Pixels { get; }
    public int Width => Bounds.Width;
    public int Height => Bounds.Height;
    public int Stride => Width * 4;

    public bool Contains(int x, int y) => Bounds.Contains(x, y);

    /// <summary>Color del píxel en coordenadas físicas globales, o null si está fuera de este monitor.</summary>
    public Argb? PixelAt(int x, int y)
    {
        if (!Contains(x, y)) return null;
        var o = (y - Bounds.Top) * Stride + (x - Bounds.Left) * 4;
        return Argb.FromRgb(Pixels[o + 2], Pixels[o + 1], Pixels[o]);
    }

    public BitmapSource ToBitmapSource()
    {
        var bmp = BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Bgr32, null, Pixels, Stride);
        bmp.Freeze();
        return bmp;
    }

    public static List<(IntPtr Handle, RECT Bounds, double Scale)> EnumerateMonitors()
    {
        var list = new List<(IntPtr, RECT, double)>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr h, IntPtr _, ref RECT r, IntPtr _) =>
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            var bounds = GetMonitorInfo(h, ref info) ? info.rcMonitor : r;
            var scale = GetDpiForMonitor(h, MDT_EFFECTIVE_DPI, out var dx, out _) == 0 ? dx / 96.0 : 1.0;
            list.Add((h, bounds, scale));
            return true;
        }, IntPtr.Zero);
        return list;
    }

    /// <summary>Captura todos los monitores con BitBlt (SRCCOPY | CAPTUREBLT).</summary>
    public static List<MonitorSnapshot> CaptureAll() =>
        EnumerateMonitors().Select(m => Capture(m.Bounds, m.Scale)).ToList();

    public static MonitorSnapshot Capture(RECT bounds, double scale)
    {
        int w = bounds.Width, h = bounds.Height;
        var screenDc = GetDC(IntPtr.Zero);
        var memDc = CreateCompatibleDC(screenDc);
        var bmi = new BITMAPINFOHEADER
        {
            biSize = Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = w,
            biHeight = -h, // top-down
            biPlanes = 1,
            biBitCount = 32,
        };
        var hbm = CreateDIBSection(memDc, ref bmi, 0, out var bits, IntPtr.Zero, 0);
        var old = SelectObject(memDc, hbm);
        try
        {
            BitBlt(memDc, 0, 0, w, h, screenDc, bounds.Left, bounds.Top, SRCCOPY | CAPTUREBLT);
            var pixels = new byte[w * h * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            return new MonitorSnapshot(bounds, scale, pixels);
        }
        finally
        {
            SelectObject(memDc, old);
            DeleteObject(hbm);
            DeleteDC(memDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}
