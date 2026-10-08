using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Matiz.Core.Settings;

namespace Matiz.App.Localization;

/// <summary>
/// Fuente de textos de la interfaz (resx en inglés y español). El servicio expone el idioma activo,
/// una indexadora para los bindings XAML (recargados al cambiar de idioma vía PropertyChanged("Item[]"))
/// y funciones T/F para el código.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources = new("Matiz.App.Localization.Strings", typeof(LocalizationService).Assembly);

    public static LocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>Todos los textos de la interfaz deben releerse (controles construidos en código).</summary>
    public event EventHandler? LanguageChanged;

    public AppLanguage Language { get; private set; } = AppLanguage.English;

    public string this[string key] => T(key);

    /// <summary>Se llama al iniciar (antes de crear la ventana): fija la cultura de recursos.</summary>
    public static void Initialize(AppLanguage language) => Instance.SetLanguage(language, force: true);

    public void SetLanguage(AppLanguage language) => SetLanguage(language, force: false);

    private void SetLanguage(AppLanguage language, bool force)
    {
        if (!force && Language == language) return;
        Language = language;

        // Solo la cultura de recursos: números y fechas siguen la del sistema (evita cambiar decimales).
        CultureInfo.CurrentUICulture = new CultureInfo(language switch
        {
            AppLanguage.Spanish => "es",
            _ => "en",
        });

        LanguageChanged?.Invoke(this, EventArgs.Empty);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string T(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public string F(string key, params object[] args) => string.Format(CultureInfo.CurrentUICulture, T(key), args);
}

/// <summary>Atajos de acceso estático (uso en VisualModels, controles y code-behind).</summary>
public static class Loc
{
    public static string T(string key) => LocalizationService.Instance.T(key);
    public static string F(string key, params object[] args) => LocalizationService.Instance.F(key, args);
}