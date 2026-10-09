using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Matiz.App.Controls;
using Matiz.Core.Colors;
using static Matiz.App.Interop.NativeMethods;

namespace Matiz.App.ScreenCapture;

/// <summary>
/// Modo captura: congela cada monitor y muestra un overlay por monitor con la instantánea 1:1.
/// El cursor se lee siempre con GetCursorPos (píxeles físicos) para indexar la instantánea sin conversiones DIP.
/// </summary>
internal sealed class ScreenPickerController
{
    public const int MinZoom = 7, MaxZoom = 21;

    private List<MonitorSnapshot> _snapshots = [];
    private readonly List<OverlayWindow> _overlays = [];
    private Action<CaptureResult?>? _onDone;
    private int _n = 11;

    public bool IsActive { get; private set; }

    public void Start(Action<CaptureResult?> onDone)
    {
        if (IsActive) return;
        IsActive = true;
        _onDone = onDone;
        _snapshots = MonitorSnapshot.CaptureAll();
        foreach (var snap in _snapshots)
        {
            var w = new OverlayWindow(this, snap);
            _overlays.Add(w);
            w.Show();
        }
        GetCursorPos(out var p);
        (_overlays.FirstOrDefault(o => o.Snapshot.Contains(p.X, p.Y)) ?? _overlays.FirstOrDefault())?.ActivateOverlay();
        Refresh();
    }

    public Argb? PixelAt(int x, int y)
    {
        foreach (var s in _snapshots)
            if (s.PixelAt(x, y) is { } c) return c;
        return null;
    }

    public void Refresh()
    {
        if (!IsActive) return;
        GetCursorPos(out var p);
        foreach (var o in _overlays) o.UpdateMagnifier(p.X, p.Y, _n);
    }

    public void Zoom(int delta)
    {
        _n = Math.Clamp(_n + (delta > 0 ? -2 : 2), MinZoom, MaxZoom);
        Refresh();
    }

    public void Nudge(int dx, int dy)
    {
        GetCursorPos(out var p);
        SetCursorPos(p.X + dx, p.Y + dy);
        Refresh();
    }

    /// <summary>Click izquierdo o Enter: confirma el píxel como color principal (con Shift el overlay sigue abierto).</summary>
    public void Confirm()
    {
        GetCursorPos(out var p);
        var c = PixelAt(p.X, p.Y);
        Finish(c is null ? null : new CaptureResult(c.Value, CaptureResultKind.Principal, IsShiftPressed()));
    }

    /// <summary>Click derecho: captura el píxel como punto secundario del conjunto Personalizado (el principal no cambia).</summary>
    public void ConfirmSecondary()
    {
        GetCursorPos(out var p);
        var c = PixelAt(p.X, p.Y);
        Finish(c is null ? null : new CaptureResult(c.Value, CaptureResultKind.Secondary, IsShiftPressed()));
    }

    public void Cancel() => Finish(null);

    private static bool IsShiftPressed() => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

    /// <summary>
    /// Entrega el resultado: con Shift (modo continuo) el overlay sigue abierto para capturar varios colores
    /// y el callback queda vivo para las siguientes acciones; sin Shift (o al cancelar) cierra todo.
    /// </summary>
    private void Finish(CaptureResult? result)
    {
        if (!IsActive) return;
        var cb = _onDone;
        if (result is { Continue: true })
        {
            cb?.Invoke(result);
            return;
        }
        IsActive = false;
        foreach (var o in _overlays) o.CloseOverlay();
        _overlays.Clear();
        _snapshots = [];
        _onDone = null;
        cb?.Invoke(result);
    }
}

/// <summary>Ventana topmost sin bordes que cubre exactamente un monitor (posicionada en píxeles físicos).</summary>
internal sealed class OverlayWindow : Window
{
    private readonly ScreenPickerController _controller;
    private readonly Image _image;
    private readonly Canvas _canvas;
    private readonly MagnifierView _magnifier = new();
    private bool _closing;

    public OverlayWindow(ScreenPickerController controller, MonitorSnapshot snapshot)
    {
        _controller = controller;
        Snapshot = snapshot;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Black;
        Cursor = Cursors.Cross;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = snapshot.Bounds.Left / snapshot.DpiScale;
        Top = snapshot.Bounds.Top / snapshot.DpiScale;
        Width = snapshot.Width / snapshot.DpiScale;
        Height = snapshot.Height / snapshot.DpiScale;
        Title = "Matiz — captura";

        _image = new Image { Source = snapshot.ToBitmapSource(), Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        _canvas = new Canvas();
        _canvas.Children.Add(_magnifier);
        var root = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        root.Children.Add(_image);
        root.Children.Add(_canvas);
        Content = root;

        SourceInitialized += (_, _) => PlaceOnMonitor();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(PlaceOnMonitor);
        MouseMove += (_, _) => _controller.Refresh();
        MouseLeftButtonDown += (_, e) => { e.Handled = true; _controller.Confirm(); };
        MouseRightButtonDown += (_, e) => { e.Handled = true; _controller.ConfirmSecondary(); };
        MouseWheel += (_, e) => _controller.Zoom(e.Delta);
        KeyDown += OnKeyDown;
        Deactivated += (_, _) => { /* el foco puede pasar a otro overlay: no cancelar */ };
    }

    public MonitorSnapshot Snapshot { get; }

    private double Scale => VisualTreeHelper.GetDpi(this).DpiScaleX;

    private void PlaceOnMonitor()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        var b = Snapshot.Bounds;
        SetWindowPos(hwnd, HWND_TOPMOST, b.Left, b.Top, b.Width, b.Height, SWP_SHOWWINDOW);
        // 1 píxel de la instantánea = 1 píxel físico, con el DPI real de esta ventana.
        var s = Scale;
        _image.Width = Snapshot.Width / s;
        _image.Height = Snapshot.Height / s;
    }

    public void ActivateOverlay()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        SetForegroundWindow(hwnd);
        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    public void UpdateMagnifier(int x, int y, int n)
    {
        if (!Snapshot.Contains(x, y))
        {
            _magnifier.Visibility = Visibility.Collapsed;
            return;
        }
        if (!IsActive && !_closing) ActivateOverlay();
        _magnifier.Visibility = Visibility.Visible;
        _magnifier.Update((dx, dy) => _controller.PixelAt(x + dx, y + dy), n);

        var s = Scale;
        var lx = (x - Snapshot.Bounds.Left) / s;
        var ly = (y - Snapshot.Bounds.Top) / s;
        var size = MagnifierRenderer.Size;
        const double gap = 22;
        var w = Snapshot.Width / s;
        var h = Snapshot.Height / s;
        var px = lx + gap + size.Width > w ? lx - gap - size.Width : lx + gap;
        var py = ly + gap + size.Height > h ? ly - gap - size.Height : ly + gap;
        Canvas.SetLeft(_magnifier, Math.Max(0, px));
        Canvas.SetTop(_magnifier, Math.Max(0, py));
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        switch (e.Key)
        {
            case Key.Escape: _controller.Cancel(); break;
            case Key.Enter: case Key.Space: _controller.Confirm(); break;
            case Key.Left: _controller.Nudge(-step, 0); break;
            case Key.Right: _controller.Nudge(step, 0); break;
            case Key.Up: _controller.Nudge(0, -step); break;
            case Key.Down: _controller.Nudge(0, step); break;
            case Key.Add: case Key.OemPlus: _controller.Zoom(1); break;
            case Key.Subtract: case Key.OemMinus: _controller.Zoom(-1); break;
            default: return;
        }
        e.Handled = true;
    }

    public void CloseOverlay()
    {
        _closing = true;
        Close();
    }
}
