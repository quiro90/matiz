using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;
using Matiz.App.Localization;
using Matiz.Core.Colors;

namespace Matiz.App.Controls;

/// <summary>Base de los controles de arrastre de un solo valor (brillo y grises).</summary>
public abstract class DragValueControl : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(DragValueControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CommitCommandProperty = DependencyProperty.Register(nameof(CommitCommand), typeof(ICommand), typeof(DragValueControl));

    private bool _dragging;

    protected DragValueControl()
    {
        Focusable = true;
        FocusVisualStyle = null;
        Cursor = Cursors.Hand;
    }

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public ICommand? CommitCommand { get => (ICommand?)GetValue(CommitCommandProperty); set => SetValue(CommitCommandProperty, value); }

    protected abstract double ValueFromPoint(Point p);
    protected abstract Key IncreaseKey { get; }
    protected abstract Key DecreaseKey { get; }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        CaptureMouse();
        _dragging = true;
        Value = ValueFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging) Value = ValueFromPoint(e.GetPosition(this));
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
        if (!_dragging) return;
        _dragging = false;
        Commit();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 0.1 : 0.01;
        var v = double.IsNaN(Value) ? 0.5 : Value;
        if (e.Key == IncreaseKey) Value = Math.Min(1, v + step);
        else if (e.Key == DecreaseKey) Value = Math.Max(0, v - step);
        else return;
        e.Handled = true;
        Commit();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) => InvalidateVisual();
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) => InvalidateVisual();

    private void Commit()
    {
        if (CommitCommand?.CanExecute(null) == true) CommitCommand.Execute(null);
    }

    protected static readonly Pen FocusPen = new(new SolidColorBrush(Color.FromArgb(140, 128, 128, 128)), 1) { DashStyle = DashStyles.Dash };
}

/// <summary>
/// Control vertical de brillo (HSV Value). Su fondo es el propio color: de su versión a brillo 100% (arriba) a negro.
/// Un degradado sRGB de 2 paradas reproduce exactamente la rampa de V.
/// </summary>
public sealed class BrightnessSlider : DragValueControl
{
    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(nameof(Hue), typeof(double), typeof(BrightnessSlider),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(nameof(Saturation), typeof(double), typeof(BrightnessSlider),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public BrightnessSlider()
    {
        AutomationProperties.SetName(this, Loc.T("controls.brightness"));
        LocalizationService.Instance.LanguageChanged += (_, _) => AutomationProperties.SetName(this, Loc.T("controls.brightness"));
    }

    public double Hue { get => (double)GetValue(HueProperty); set => SetValue(HueProperty, value); }
    public double Saturation { get => (double)GetValue(SaturationProperty); set => SetValue(SaturationProperty, value); }

    private const double Inset = 8;
    protected override Key IncreaseKey => Key.Up;
    protected override Key DecreaseKey => Key.Down;

    protected override double ValueFromPoint(Point p) =>
        Math.Clamp(1 - (p.Y - Inset) / Math.Max(1, ActualHeight - 2 * Inset), 0, 1);

    protected override void OnRender(DrawingContext dc)
    {
        var top = ColorMath.FromHsv(Hue, Saturation, 1);
        var track = new Rect(4, Inset, Math.Max(0, ActualWidth - 8), Math.Max(0, ActualHeight - 2 * Inset));
        var brush = new LinearGradientBrush(Color.FromRgb(top.R, top.G, top.B), Colors.Black, 90)
        {
            ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation,
        };
        dc.DrawRoundedRectangle(brush, new Pen(new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)), 1), track, 6, 6);

        var y = Inset + (1 - Value) * track.Height;
        var handle = new Rect(0, y - 4, ActualWidth, 8);
        dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), 4), handle, 4, 4);
        dc.DrawRoundedRectangle(null, new Pen(Brushes.White, 2), handle, 4, 4);
        if (IsKeyboardFocused) dc.DrawRoundedRectangle(null, FocusPen, new Rect(0, 2, ActualWidth, ActualHeight - 4), 6, 6);
    }
}
