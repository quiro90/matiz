using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Matiz.App.Controls;

/// <summary>Comportamientos adjuntos pequeños para TextBox.</summary>
public static class Behaviors
{
    /// <summary>Ejecuta el comando con el texto al pulsar Enter o al perder el foco.</summary>
    public static readonly DependencyProperty CommitCommandProperty = DependencyProperty.RegisterAttached(
        "CommitCommand", typeof(ICommand), typeof(Behaviors), new PropertyMetadata(null, OnCommitCommandChanged));

    public static ICommand? GetCommitCommand(DependencyObject d) => (ICommand?)d.GetValue(CommitCommandProperty);
    public static void SetCommitCommand(DependencyObject d, ICommand? v) => d.SetValue(CommitCommandProperty, v);

    /// <summary>Marca visual de entrada inválida (los estilos de TextBox lo usan).</summary>
    public static readonly DependencyProperty IsInvalidProperty = DependencyProperty.RegisterAttached(
        "IsInvalid", typeof(bool), typeof(Behaviors), new PropertyMetadata(false));

    public static bool GetIsInvalid(DependencyObject d) => (bool)d.GetValue(IsInvalidProperty);
    public static void SetIsInvalid(DependencyObject d, bool v) => d.SetValue(IsInvalidProperty, v);

    /// <summary>Enter actualiza el origen del binding de Text y quita el foco.</summary>
    public static readonly DependencyProperty EnterUpdatesSourceProperty = DependencyProperty.RegisterAttached(
        "EnterUpdatesSource", typeof(bool), typeof(Behaviors), new PropertyMetadata(false, OnEnterUpdatesSourceChanged));

    public static bool GetEnterUpdatesSource(DependencyObject d) => (bool)d.GetValue(EnterUpdatesSourceProperty);
    public static void SetEnterUpdatesSource(DependencyObject d, bool v) => d.SetValue(EnterUpdatesSourceProperty, v);

    private static void OnCommitCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        tb.KeyDown -= CommitOnEnter;
        tb.LostKeyboardFocus -= CommitOnLostFocus;
        if (e.NewValue is null) return;
        tb.KeyDown += CommitOnEnter;
        tb.LostKeyboardFocus += CommitOnLostFocus;
    }

    private static void CommitOnEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Execute((TextBox)sender);
        ((TextBox)sender).SelectAll();
        e.Handled = true;
    }

    private static void CommitOnLostFocus(object sender, KeyboardFocusChangedEventArgs e) => Execute((TextBox)sender);

    private static void Execute(TextBox tb)
    {
        var cmd = GetCommitCommand(tb);
        if (cmd?.CanExecute(tb.Text) == true) cmd.Execute(tb.Text);
    }

    private static void OnEnterUpdatesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        tb.KeyDown -= UpdateOnEnter;
        if (e.NewValue is true) tb.KeyDown += UpdateOnEnter;
    }

    private static void UpdateOnEnter(object sender, KeyEventArgs e)
    {
        var tb = (TextBox)sender;
        if (e.Key == Key.Enter)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            tb.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }
}

/// <summary>enum ↔ bool para RadioButtons (ConverterParameter = nombre del valor). Con destino Visibility devuelve Visible/Collapsed.</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var equal = value?.ToString() == parameter?.ToString();
        if (targetType == typeof(Visibility)) return equal ? Visibility.Visible : Visibility.Collapsed;
        return equal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is not null ? Enum.Parse(targetType, parameter.ToString()!) : Binding.DoNothing;
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is true) ^ Invert ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible si el valor no es null, cadena vacía ni 0. ConverterParameter=inverse invierte el resultado.</summary>
public sealed class NotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var empty = value is null || value is string { Length: 0 } || value is int and 0;
        if (parameter as string == "inverse") empty = !empty;
        return empty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
