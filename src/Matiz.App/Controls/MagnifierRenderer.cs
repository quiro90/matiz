using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Matiz.Core.Colors;
using Matiz.Core.Formatting;

namespace Matiz.App.Controls;

/// <summary>
/// Dibuja la lupa: cuadrícula de N×N píxeles ampliados sin suavizado, píxel central resaltado
/// y una franja con muestra, HEX y RGB. Compartida por el screen picker y el visor de imágenes.
/// </summary>
internal static class MagnifierRenderer
{
    public const double GridSize = 154;
    public const double InfoHeight = 40;
    public static Size Size => new(GridSize, GridSize + InfoHeight);

    private static readonly Typeface Mono = new(new FontFamily("Cascadia Mono, Consolas"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface Ui = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private static readonly Brush Background = Freeze(new SolidColorBrush(Color.FromRgb(24, 24, 24)));
    private static readonly Brush Outside = Freeze(new SolidColorBrush(Color.FromRgb(48, 48, 48)));
    private static readonly Pen GridPen = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromArgb(46, 128, 128, 128))), 1));
    private static readonly Pen Frame = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(90, 90, 90))), 1));
    private static readonly Pen CenterOuter = Freeze(new Pen(Brushes.Black, 3));
    private static readonly Pen CenterInner = Freeze(new Pen(Brushes.White, 1.5));

    /// <param name="pixelAt">Color del píxel en el desplazamiento (dx, dy) respecto al central, o null si no existe.</param>
    public static void Draw(DrawingContext dc, Point topLeft, int n, Func<int, int, Argb?> pixelAt, double pixelsPerDip)
    {
        n = Math.Max(3, n | 1);
        var cell = GridSize / n;
        var total = new Rect(topLeft, Size);
        dc.DrawRoundedRectangle(Background, Frame, total, 8, 8);

        var grid = new Rect(topLeft.X, topLeft.Y, GridSize, GridSize);
        dc.PushClip(new RectangleGeometry(grid, 8, 8));
        var half = n / 2;
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var c = pixelAt(x - half, y - half);
            var r = new Rect(topLeft.X + x * cell, topLeft.Y + y * cell, cell + 0.5, cell + 0.5);
            dc.DrawRectangle(c is { } v ? BrushOf(v) : Outside, null, r);
        }
        for (var i = 1; i < n; i++)
        {
            dc.DrawLine(GridPen, new Point(topLeft.X + i * cell, topLeft.Y), new Point(topLeft.X + i * cell, topLeft.Y + GridSize));
            dc.DrawLine(GridPen, new Point(topLeft.X, topLeft.Y + i * cell), new Point(topLeft.X + GridSize, topLeft.Y + i * cell));
        }
        dc.Pop();

        var center = new Rect(topLeft.X + half * cell, topLeft.Y + half * cell, cell, cell);
        dc.DrawRectangle(null, CenterOuter, center);
        dc.DrawRectangle(null, CenterInner, center);

        var current = pixelAt(0, 0);
        var infoTop = topLeft.Y + GridSize;
        if (current is { } col)
        {
            dc.DrawRoundedRectangle(BrushOf(col), Frame, new Rect(topLeft.X + 8, infoTop + 8, 24, 24), 4, 4);
            var hex = new FormattedText(col.ToHex(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Mono, 13, Brushes.White, pixelsPerDip);
            dc.DrawText(hex, new Point(topLeft.X + 40, infoTop + 5));
            var rgb = new FormattedText($"RGB {ColorFormats.Rgb(col)}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Ui, 11,
                Freeze(new SolidColorBrush(Color.FromRgb(170, 170, 170))), pixelsPerDip);
            dc.DrawText(rgb, new Point(topLeft.X + 40, infoTop + 21));
        }
    }

    private static SolidColorBrush BrushOf(Argb c) => Freeze(new SolidColorBrush(Color.FromArgb(255, c.R, c.G, c.B)));

    private static T Freeze<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }
}

/// <summary>Elemento que hospeda la lupa (usado en el overlay de captura).</summary>
internal sealed class MagnifierView : FrameworkElement
{
    private Func<int, int, Argb?> _pixelAt = (_, _) => null;
    private int _n = 11;

    public MagnifierView()
    {
        Width = MagnifierRenderer.Size.Width;
        Height = MagnifierRenderer.Size.Height;
        IsHitTestVisible = false;
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    public void Update(Func<int, int, Argb?> pixelAt, int n)
    {
        _pixelAt = pixelAt;
        _n = n;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc) =>
        MagnifierRenderer.Draw(dc, new Point(0, 0), _n, _pixelAt, VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
