using System.Windows.Data;

namespace Matiz.App.Localization;

/// <summary>
/// Texto localizable en XAML: <c>Text="{loc:Loc common.copy}"</c>. Hereda de Binding (indexadora del
/// servicio) para que WPF lo aplique como expresión en cualquier propiedad, incluidas las adjuntas
/// (AutomationProperties.Name, WindowChrome...), y se actualice en caliente al cambiar el idioma.
/// </summary>
public sealed class LocExtension : Binding
{
    public LocExtension(string key) : base("[" + key + "]")
    {
        Source = LocalizationService.Instance;
        Mode = BindingMode.OneWay;
    }

    public string Key => Path?.Path is string p && p.Length >= 2 ? p[1..^1] : string.Empty;
}