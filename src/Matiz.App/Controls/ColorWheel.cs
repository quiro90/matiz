using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.Core.Colors;

namespace Matiz.App.Controls;

/// <summary>
/// Rueda hue (ángulo, 0° arriba, sentido horario) × saturación (radio, S = r^γ), dibujada a brillo 100%
/// (centro blanco). El mapa se genera como bitmap en píxeles físicos y se cachea por (tamaño, DPI, γ):
/// mover el marcador no lo regenera.
/// </summary>
public sealed class ColorWheel : FrameworkElement
{
    public static readonly DependencyProperty MarkersProperty = DependencyProperty.Register(nameof(Markers), typeof(IReadOnlyList<WheelMarker>), typeof(ColorWheel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Click en un punto secundario (parámetro: índice del marcador).</summary>
    public static readonly DependencyProperty MarkerClickCommandProperty = DependencyProperty.Register(nameof(MarkerClickCommand), typeof(ICommand), typeof(ColorWheel));

    /// <summary>Doble click en un punto secundario (parámetro: índice del marcador).</summary>
    public static readonly DependencyProperty MarkerActivateCommandProperty = DependencyProperty.Register(nameof(MarkerActivateCommand), typeof(ICommand), typeof(ColorWheel));

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(nameof(Hue), typeof(double), typeof(ColorWheel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(nameof(Saturation), typeof(double), typeof(ColorWheel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GammaProperty = DependencyProperty.Register(nameof(Gamma), typeof(double), typeof(ColorWheel),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CommitCommandProperty = DependencyProperty.Register(nameof(CommitCommand), typeof(ICommand), typeof(ColorWheel));

    private const double Padding = 12;
    private const double FineFactor = 0.25;

    private WriteableBitmap? _bitmap;
    private (int Px, double Gamma) _bitmapKey;
    private Point _virtual;
    private Point _last;
    private bool _dragging;

    /// <summary>Número de regeneraciones del bitmap (diagnóstico de rendimiento).</summary>
    public int RenderCount { get; private set; }

    public ColorWheel()
    {
        Focusable = true;
        FocusVisualStyle = null;
        Cursor = Cursors.Cross;
        AutomationProperties.SetName(this, "Rueda de color: tono y saturación");
    }

    public double Hue { get => (double)GetValue(HueProperty); set => SetValue(HueProperty, value); }
    public double Saturation { get => (double)GetValue(SaturationProperty); set => SetValue(SaturationProperty, value); }
    public double Gamma { get => (double)GetValue(GammaProperty); set => SetValue(GammaProperty, value); }
    public ICommand? CommitCommand { get => (ICommand?)GetValue(CommitCommandProperty); set => SetValue(CommitCommandProperty, value); }
    public IReadOnlyList<WheelMarker>? Markers { get => (IReadOnlyList<WheelMarker>?)GetValue(MarkersProperty); set => SetValue(MarkersProperty, value); }
    public ICommand? MarkerClickCommand { get => (ICommand?)GetValue(MarkerClickCommandProperty); set => SetValue(MarkerClickCommandProperty, value); }
    public ICommand? MarkerActivateCommand { get => (ICommand?)GetValue(MarkerActivateCommandProperty); set => SetValue(MarkerActivateCommandProperty, value); }

    private const double MarkerHitRadius = 9;
    private const double MainHitRadius = 11;

    private Point PositionOf(double hue, double saturation)
    {
        var (x, y) = WheelMapping.ToPoint(hue, saturation, Gamma);
        var c = Center;
        return new Point(c.X + x * Radius, c.Y + y * Radius);
    }

    /// <summary>Índice del punto secundario bajo <paramref name="p"/>, o -1.</summary>
    private int HitMarker(Point p)
    {
        if (Markers is not { Count: > 0 } markers) return -1;
        if ((p - PositionOf(Hue, Saturation)).Length <= MainHitRadius) return -1; // el marcador principal tiene prioridad
        var best = -1;
        var bestDist = MarkerHitRadius;
        for (var i = 0; i < markers.Count; i++)
        {
            var d = (p - PositionOf(markers[i].Hue, markers[i].Saturation)).Length;
            if (d <= bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private double Radius => Math.Max(0, Math.Min(ActualWidth, ActualHeight) / 2 - Padding);
    private Point Center => new(ActualWidth / 2, ActualHeight / 2);

    protected override Size MeasureOverride(Size available)
    {
        var s = Math.Min(double.IsInfinity(available.Width) ? 300 : available.Width, double.IsInfinity(available.Height) ? 300 : available.Height);
        return new Size(s, s);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) => InvalidateVisual();

    protected override void OnRender(DrawingContext dc)
    {
        var r = Radius;
        if (r < 4) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var px = (int)Math.Ceiling(2 * r * dpi.DpiScaleX);
        EnsureBitmap(px, Gamma, dpi);
        var c = Center;
        dc.DrawImage(_bitmap, new Rect(c.X - r, c.Y - r, 2 * r, 2 * r));

        if (Markers is { Count: > 0 } markers)
        {
            var line = new Pen(new SolidColorBrush(Color.FromArgb(110, 30, 30, 30)), 1) { DashStyle = DashStyles.Dot };
            var halo = new Pen(new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)), 3);
            var ring = new Pen(Brushes.White, 1.5);
            dc.DrawLine(line, c, PositionOf(Hue, Saturation));
            foreach (var mk in markers)
            {
                var p = PositionOf(mk.Hue, mk.Saturation);
                dc.DrawLine(line, c, p);
                // Igual que el marcador principal: relleno a brillo 100% para que el tono se lea a cualquier brillo.
                var top = ColorMath.FromHsv(mk.Hue, mk.Saturation, 1);
                var dot = new SolidColorBrush(Color.FromRgb(top.R, top.G, top.B));
                var size = mk.IsSelected ? 7 : 5;
                dc.DrawEllipse(null, halo, p, size, size);
                dc.DrawEllipse(dot, ring, p, size, size);
                if (mk.IsSelected)
                    dc.DrawEllipse(null, new Pen(Brushes.White, 1) { DashStyle = DashStyles.Dash }, p, size + 4, size + 4);
            }
        }

        var m = PositionOf(Hue, Saturation);
        var fill = ColorMath.FromHsv(Hue, Saturation, 1);
        var fillBrush = new SolidColorBrush(Color.FromRgb(fill.R, fill.G, fill.B));
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), 4), m, 9, 9);
        dc.DrawEllipse(fillBrush, new Pen(Brushes.White, 2.5), m, 9, 9);
        if (IsKeyboardFocused)
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 128, 128, 128)), 1) { DashStyle = DashStyles.Dash }, c, r + 5, r + 5);
    }

    private void EnsureBitmap(int px, double gamma, DpiScale dpi)
    {
        if (_bitmap is not null && _bitmapKey == (px, gamma)) return;
        _bitmapKey = (px, gamma);
        RenderCount++;

        var buffer = new int[px * px];
        var radius = px / 2.0;
        Parallel.For(0, px, y =>
        {
            var dy = y + 0.5 - radius;
            var row = y * px;
            for (var x = 0; x < px; x++)
            {
                var dx = x + 0.5 - radius;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                var coverage = Math.Clamp(radius - dist + 0.5, 0, 1); // borde antialiasado
                if (coverage <= 0) continue;
                var (hue, sat) = WheelMapping.FromPoint(dx, dy, radius, gamma);
                var (r, g, b) = ColorMath.HsvToRgb(double.IsNaN(hue) ? 0 : hue, sat, 1);
                var a = (int)(coverage * 255);
                // Pbgra32 premultiplicado
                buffer[row + x] = (a << 24) | ((int)(r * a + 0.5) << 16) | ((int)(g * a + 0.5) << 8) | (int)(b * a + 0.5);
            }
        });

        _bitmap = new WriteableBitmap(px, px, 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32, null);
        _bitmap.WritePixels(new Int32Rect(0, 0, px, px), buffer, px * 4, 0);
        _bitmap.Freeze();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        var hit = HitMarker(e.GetPosition(this));
        if (hit >= 0)
        {
            var cmd = e.ClickCount >= 2 ? MarkerActivateCommand : MarkerClickCommand;
            if (cmd?.CanExecute(hit) == true) cmd.Execute(hit);
            e.Handled = true;
            return;
        }
        CaptureMouse();
        _dragging = true;
        _last = e.GetPosition(this);
        _virtual = _last;
        ApplyPoint(_virtual);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging)
        {
            Cursor = HitMarker(e.GetPosition(this)) >= 0 ? Cursors.Hand : Cursors.Cross;
            return;
        }
        var p = e.GetPosition(this);
        var delta = p - _last;
        _last = p;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) delta *= FineFactor;
        _virtual += delta;
        // Mantiene el punto virtual dentro de la rueda para que el ajuste fino no acumule fuera del borde.
        var c = Center;
        var v = _virtual - c;
        if (v.Length > Radius && v.Length > 0) _virtual = c + v * (Radius / v.Length);
        ApplyPoint(_virtual);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();
        Commit();
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            Commit();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var big = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        switch (e.Key)
        {
            case Key.Left: Hue = ColorMath.NormalizeHue(Hue - (big ? 10 : 1)); break;
            case Key.Right: Hue = ColorMath.NormalizeHue(Hue + (big ? 10 : 1)); break;
            case Key.Up: Saturation = Math.Min(1, Saturation + (big ? 0.1 : 0.01)); break;
            case Key.Down: Saturation = Math.Max(0, Saturation - (big ? 0.1 : 0.01)); break;
            default: return;
        }
        e.Handled = true;
        Commit();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) => InvalidateVisual();
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) => InvalidateVisual();

    private void ApplyPoint(Point p)
    {
        var c = Center;
        var (hue, sat) = WheelMapping.FromPoint(p.X - c.X, p.Y - c.Y, Radius, Gamma);
        if (!double.IsNaN(hue)) Hue = hue;
        Saturation = sat;
    }

    private void Commit()
    {
        if (CommitCommand?.CanExecute(null) == true) CommitCommand.Execute(null);
    }
}

/// <summary>Punto secundario en la rueda (p. ej. un color de la armonía).</summary>
public sealed record WheelMarker(double Hue, double Saturation, Argb Color, bool IsSelected);
