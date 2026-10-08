using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Matiz.App.Controls;

/// <summary>
/// Campo numérico: publica <see cref="Value"/> solo al confirmar (Enter, pérdida de foco, flechas, rueda del ratón).
/// Valores inválidos o fuera de rango marcan <see cref="IsInvalid"/> y no se publican.
/// </summary>
public sealed class NumericBox : TextBox
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericBox),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((NumericBox)d).SyncText()));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericBox), new(0.0));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericBox), new(100.0));
    public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(nameof(Decimals), typeof(int), typeof(NumericBox), new(0));
    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(nameof(Step), typeof(double), typeof(NumericBox), new(1.0));
    public static readonly DependencyProperty IsInvalidProperty = DependencyProperty.Register(nameof(IsInvalid), typeof(bool), typeof(NumericBox), new(false));
    public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(nameof(Wrap), typeof(bool), typeof(NumericBox), new(false));

    private bool _editing;

    public NumericBox()
    {
        HorizontalContentAlignment = HorizontalAlignment.Right;
        SyncText();
    }

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public int Decimals { get => (int)GetValue(DecimalsProperty); set => SetValue(DecimalsProperty, value); }
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public bool IsInvalid { get => (bool)GetValue(IsInvalidProperty); set => SetValue(IsInvalidProperty, value); }
    /// <summary>Si es true (hue), los pasos con flechas dan la vuelta en lugar de limitarse.</summary>
    public bool Wrap { get => (bool)GetValue(WrapProperty); set => SetValue(WrapProperty, value); }

    private string Format(double v) => Math.Round(v, Decimals, MidpointRounding.AwayFromZero)
        .ToString("F" + Decimals, CultureInfo.CurrentCulture);

    private void SyncText()
    {
        if (_editing) return;
        Text = Format(Value);
        IsInvalid = false;
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        if (IsKeyboardFocusWithin) _editing = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var big = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        switch (e.Key)
        {
            case Key.Enter:
                CommitText();
                SelectAll();
                e.Handled = true;
                return;
            case Key.Escape:
                _editing = false;
                SyncText();
                e.Handled = true;
                return;
            case Key.Up:
                StepBy(big ? 10 : 1);
                e.Handled = true;
                return;
            case Key.Down:
                StepBy(big ? -10 : -1);
                e.Handled = true;
                return;
        }
        base.OnPreviewKeyDown(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (!IsKeyboardFocusWithin && !IsMouseOver) return;
        StepBy(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (_editing) CommitText();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        SelectAll();
    }

    private void StepBy(double steps)
    {
        var baseValue = TryParse(Text, out var parsed) ? parsed : Value;
        var v = baseValue + steps * Step;
        if (Wrap)
        {
            var range = Maximum - Minimum;
            v = ((v - Minimum) % range + range) % range + Minimum;
        }
        Publish(Math.Clamp(v, Minimum, Maximum));
    }

    private void CommitText()
    {
        if (TryParse(Text, out var v) && v >= Minimum && v <= Maximum)
        {
            Publish(v);
        }
        else
        {
            IsInvalid = true;
        }
    }

    private void Publish(double v)
    {
        _editing = false;
        v = Math.Round(v, Decimals, MidpointRounding.AwayFromZero);
        if (v.Equals(Value)) SyncText();
        else Value = v;
        GetBindingExpression(ValueProperty)?.UpdateSource();
    }

    private static bool TryParse(string text, out double v)
    {
        text = text.Trim().TrimEnd('%', '°').Trim();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out v)
               || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }
}
