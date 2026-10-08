using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.Core.Colors;
using Matiz.Core.Session;

namespace Matiz.App.ViewModels;

public sealed partial class MainViewModel
{
    // Coordenadas del selector (rueda + brillo): cambian en vivo (preview) y se confirman al soltar.
    [ObservableProperty] public partial double Hue { get; set; }
    [ObservableProperty] public partial double Saturation { get; set; }
    [ObservableProperty] public partial double Brightness { get; set; }

    // Entradas numéricas: cada cambio es un cambio confirmado.
    [ObservableProperty] public partial double HueDeg { get; set; }
    [ObservableProperty] public partial double SatPct { get; set; }
    [ObservableProperty] public partial double BriPct { get; set; }

    /// <summary>Luminosidad del gris actual (NaN si el color no es gris).</summary>
    [ObservableProperty] public partial double GrayValue { get; set; } = double.NaN;

    /// <summary>Enfoque de la rueda: −1 vivo, 0 lineal, +1 pastel.</summary>
    [ObservableProperty] public partial double WheelFocus { get; set; }
    [ObservableProperty] public partial double WheelGamma { get; set; } = 1;

    partial void OnHueChanged(double value)
    {
        if (!_syncing) Session.SetPreview(Session.Current.WithHue(value));
    }

    partial void OnSaturationChanged(double value)
    {
        if (!_syncing) Session.SetPreview(Session.Current.WithSaturation(value));
    }

    partial void OnBrightnessChanged(double value)
    {
        if (!_syncing) Session.SetPreview(Session.Current.WithValue(value));
    }

    partial void OnHueDegChanged(double value)
    {
        if (!_syncing) Session.Commit(Session.Current.WithHue(value), ColorChangeSource.Picker);
    }

    partial void OnSatPctChanged(double value)
    {
        if (!_syncing) Session.Commit(Session.Current.WithSaturation(value / 100), ColorChangeSource.Picker);
    }

    partial void OnBriPctChanged(double value)
    {
        if (!_syncing) Session.Commit(Session.Current.WithValue(value / 100), ColorChangeSource.Picker);
    }

    partial void OnGrayValueChanged(double value)
    {
        if (_syncing || double.IsNaN(value)) return;
        var v = Argb.ToByte(value);
        Session.SetPreview(ColorState.FromArgb(new Argb(Session.Current.Alpha, v, v, v), Session.Current));
    }

    partial void OnWheelFocusChanged(double value)
    {
        WheelGamma = WheelMapping.GammaFromFocus(value);
        _settings.WheelFocus = value;
        SaveSettings();
    }

    [RelayCommand]
    private void CommitPicker() => Session.Commit(Session.Current, ColorChangeSource.Picker);

    [RelayCommand]
    private void CommitGray() => Session.Commit(Session.Current, ColorChangeSource.Gray);

    [RelayCommand]
    private void ResetFocus() => WheelFocus = 0;

    [RelayCommand]
    private void GrayEquivalent() =>
        Session.Commit(ColorMath.GrayEquivalent(Session.Current.Argb), ColorChangeSource.Gray);
}
