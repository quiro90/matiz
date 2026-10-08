using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.Core.Colors;

namespace Matiz.App.Controls;

/// <summary>
/// Visor de imagen con zoom (rueda), desplazamiento (arrastrar) y lupa. Un click sin arrastre selecciona el
/// píxel de la imagen original (no del render escalado) y ejecuta <see cref="PickCommand"/> con un <see cref="Argb"/>.
/// </summary>
public sealed class ImageCanvas : FrameworkElement
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(BitmapSource), typeof(ImageCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((ImageCanvas)d).OnSourceChanged()));

    public static readonly DependencyProperty PickCommandProperty = DependencyProperty.Register(nameof(PickCommand), typeof(ICommand), typeof(ImageCanvas));

    private byte[] _pixels = [];
    private int _w, _h, _stride;
    private double _scale = 1;
    private Vector _offset;
    private Point? _mouse;
    private Point _downPos;
    private Vector _downOffset;
    private bool _down, _panning;
    private bool _needsFit;

    public ImageCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = Cursors.Cross;
        AutomationProperties.SetName(this, "Imagen para seleccionar colores");
    }

    public BitmapSource? Source { get => (BitmapSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public ICommand? PickCommand { get => (ICommand?)GetValue(PickCommandProperty); set => SetValue(PickCommandProperty, value); }

    private void OnSourceChanged()
    {
        if (Source is not { } src)
        {
            _pixels = [];
            _w = _h = 0;
            return;
        }
        var bgra = src.Format == PixelFormats.Bgra32 ? src : new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
        _w = bgra.PixelWidth;
        _h = bgra.PixelHeight;
        _stride = _w * 4;
        _pixels = new byte[_stride * _h];
        bgra.CopyPixels(_pixels, _stride, 0);
        _needsFit = true;
        InvalidateVisual();
    }

    private void Fit()
    {
        if (_w == 0 || ActualWidth <= 0 || ActualHeight <= 0) return;
        _scale = Math.Min(1, Math.Min(ActualWidth / _w, ActualHeight / _h));
        _offset = new Vector((ActualWidth - _w * _scale) / 2, (ActualHeight - _h * _scale) / 2);
        _needsFit = false;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        _needsFit = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        if (Source is null || _w == 0) return;
        if (_needsFit) Fit();

        RenderOptions.SetBitmapScalingMode(this, _scale >= 2 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
        dc.DrawImage(Source, new Rect(_offset.X, _offset.Y, _w * _scale, _h * _scale));

        if (_mouse is { } m && !_panning && PixelUnder(m) is var (px, py) && px >= 0 && py >= 0 && px < _w && py < _h)
        {
            var size = MagnifierRenderer.Size;
            var pos = new Point(m.X + 20, m.Y + 20);
            if (pos.X + size.Width > ActualWidth) pos.X = m.X - 20 - size.Width;
            if (pos.Y + size.Height > ActualHeight) pos.Y = m.Y - 20 - size.Height;
            MagnifierRenderer.Draw(dc, pos, 11, (dx, dy) => PixelAt(px + dx, py + dy), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }
    }

    private (int X, int Y) PixelUnder(Point p) =>
        ((int)Math.Floor((p.X - _offset.X) / _scale), (int)Math.Floor((p.Y - _offset.Y) / _scale));

    private Argb? PixelAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _w || y >= _h) return null;
        var o = y * _stride + x * 4;
        return new Argb(_pixels[o + 3], _pixels[o + 2], _pixels[o + 1], _pixels[o]);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (_w == 0) return;
        var p = e.GetPosition(this);
        var factor = e.Delta > 0 ? 1.25 : 0.8;
        var newScale = Math.Clamp(_scale * factor, 0.05, 64);
        // Zoom alrededor del cursor
        _offset = (Vector)p - ((Vector)p - _offset) * (newScale / _scale);
        _scale = newScale;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        _down = true;
        _panning = false;
        _downPos = e.GetPosition(this);
        _downOffset = _offset;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var p = e.GetPosition(this);
        _mouse = p;
        if (_down)
        {
            var delta = p - _downPos;
            if (!_panning && delta.Length > 4)
            {
                _panning = true;
                Cursor = Cursors.SizeAll;
            }
            if (_panning) _offset = _downOffset + delta;
        }
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!_down) return;
        _down = false;
        ReleaseMouseCapture();
        Cursor = Cursors.Cross;
        if (!_panning)
        {
            var (x, y) = PixelUnder(e.GetPosition(this));
            if (PixelAt(x, y) is { } c && PickCommand?.CanExecute(c) == true) PickCommand.Execute(c.Opaque);
        }
        _panning = false;
        InvalidateVisual();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        _mouse = null;
        InvalidateVisual();
    }

    /// <summary>Restablece el ajuste a la ventana.</summary>
    public void FitToView()
    {
        _needsFit = true;
        InvalidateVisual();
    }
}
