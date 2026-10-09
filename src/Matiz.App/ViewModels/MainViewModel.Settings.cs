using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.App.Localization;
using Matiz.App.Services;
using Matiz.Core.Formatting;
using Matiz.Core.Generation;
using Matiz.Core.Persistence;
using Matiz.Core.Settings;

namespace Matiz.App.ViewModels;

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial bool IsSettingsOpen { get; set; }
    [ObservableProperty] public partial ThemePreference ThemePreference { get; set; }
    [ObservableProperty] public partial AppLanguage Language { get; set; }
    [ObservableProperty] public partial bool AlwaysOnTop { get; set; }
    [ObservableProperty] public partial bool WindowButtonsOnLeft { get; set; }
    [ObservableProperty] public partial string CaptureHotkey { get; set; } = "Alt+C";
    [ObservableProperty] public partial string HotkeyStatus { get; private set; } = "";
    [ObservableProperty] public partial bool HotkeyFailed { get; private set; }
    [ObservableProperty] public partial IColorFormatter? DefaultFormat { get; set; }
    [ObservableProperty] public partial bool HexUppercase { get; set; }
    [ObservableProperty] public partial bool HexHash { get; set; }
    [ObservableProperty] public partial bool CopyOnCapture { get; set; }
    [ObservableProperty] public partial bool ShowAfterCapture { get; set; }
    [ObservableProperty] public partial bool ScaleAnchorAuto { get; set; }
    [ObservableProperty] public partial string ScalePrefix { get; set; } = "Primary";
    [ObservableProperty] public partial bool HarmonyBalanceLightness { get; set; } = true;

    public string DataFolder => System.IO.Path.GetDirectoryName(_settingsStore.FilePath)!;

    /// <summary>Versión visible ("1.0.2"), tomada de InformationalVersion.</summary>
    public static string Version { get; } =
        (System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(MainViewModel).Assembly)?.InformationalVersion ?? "")
        .Split('+')[0];

    public string WindowTitle => $"Matiz v{Version}";

    private bool _loadingSettings;

    private void InitSettings()
    {
        _loadingSettings = true;
        ThemePreference = _settings.Theme;
        Language = _settings.Language;
        AlwaysOnTop = _settings.AlwaysOnTop;
        WindowButtonsOnLeft = _settings.WindowButtonsOnLeft;
        CaptureHotkey = _settings.CaptureHotkey;
        DefaultFormat = ColorFormatters.Get(_settings.DefaultFormatId);
        HexUppercase = _settings.HexUppercase;
        HexHash = _settings.HexHash;
        CopyOnCapture = _settings.CopyOnCapture;
        ShowAfterCapture = _settings.ShowAfterCapture;
        ScaleAnchorAuto = _settings.ScaleAnchor == ScaleAnchorMode.Automatic;
        ScalePrefix = _settings.ScalePrefix;
        HarmonyBalanceLightness = _settings.HarmonyBalanceLightness;
        WheelFocus = _settings.WheelFocus;
        WheelGamma = Core.Colors.WheelMapping.GammaFromFocus(WheelFocus);
        _loadingSettings = false;
    }

    private void Changed(Action<AppSettings> apply)
    {
        if (_loadingSettings) return;
        apply(_settings);
        SaveSettings();
    }

    partial void OnThemePreferenceChanged(ThemePreference value)
    {
        Changed(s => s.Theme = value);
        if (!_loadingSettings) _theme.Apply(value);
    }

    partial void OnLanguageChanged(AppLanguage value)
    {
        Changed(s => s.Language = value);
        if (_loadingSettings) return;
        LocalizationService.Instance.SetLanguage(value);
        RefreshForLanguage();
    }

    /// <summary>Actualiza los textos generados en código (tooltips de formatos, nombres de armonías y resúmenes).</summary>
    private void RefreshForLanguage()
    {
        foreach (var row in FormatRows.Concat(ExtraFormatRows)) row.RefreshTexts();
        foreach (var option in HarmonyKinds) option.RefreshName();
        RefreshGenerated();
        SyncPalettes();
        SyncHistory();
    }

    partial void OnAlwaysOnTopChanged(bool value) => Changed(s => s.AlwaysOnTop = value);
    partial void OnWindowButtonsOnLeftChanged(bool value) => Changed(s => s.WindowButtonsOnLeft = value);

    partial void OnDefaultFormatChanged(IColorFormatter? value) => Changed(s => s.DefaultFormatId = value?.Id ?? ColorFormatters.HexId);

    partial void OnHexUppercaseChanged(bool value)
    {
        Changed(s => s.HexUppercase = value);
        if (!_loadingSettings) Refresh();
    }

    partial void OnHexHashChanged(bool value)
    {
        Changed(s => s.HexHash = value);
        if (!_loadingSettings) Refresh();
    }

    partial void OnCopyOnCaptureChanged(bool value) => Changed(s => s.CopyOnCapture = value);
    partial void OnShowAfterCaptureChanged(bool value) => Changed(s => s.ShowAfterCapture = value);

    partial void OnScaleAnchorAutoChanged(bool value)
    {
        Changed(s => s.ScaleAnchor = value ? ScaleAnchorMode.Automatic : ScaleAnchorMode.Fixed500);
        if (!_loadingSettings) RefreshGenerated();
    }

    partial void OnScalePrefixChanged(string value) => Changed(s => s.ScalePrefix = value);

    partial void OnHarmonyBalanceLightnessChanged(bool value)
    {
        Changed(s => s.HarmonyBalanceLightness = value);
        if (!_loadingSettings) RefreshGenerated();
    }

    /// <summary>Registra el atajo global guardado. Se llama al iniciar, cuando ya existe la ventana.</summary>
    public void RegisterHotkey() => ApplyHotkey(_settings.CaptureHotkey, save: false);

    [RelayCommand]
    private void SetHotkey(string? gesture)
    {
        if (string.IsNullOrWhiteSpace(gesture)) return;
        ApplyHotkey(gesture, save: true);
    }

    private void ApplyHotkey(string gesture, bool save)
    {
        if (_hotkeys is null) return;
        if (!HotkeyGesture.TryParse(gesture, out _, out _))
        {
            HotkeyFailed = true;
            HotkeyStatus = Loc.F("settings.hotkeyInvalid", gesture);
            return;
        }
        var ok = _hotkeys.Register(gesture);
        HotkeyFailed = !ok;
        CaptureHotkey = ok ? _hotkeys.Current! : gesture;
        HotkeyStatus = ok
            ? Loc.F("settings.hotkeyActive", CaptureHotkey)
            : Loc.F("settings.hotkeyRegisterFailed", gesture);
        if (ok && save) Changed(s => s.CaptureHotkey = CaptureHotkey);
        if (!ok && !save) ShowToast(Loc.F("toasts.hotkeyBusy", gesture), seconds: 6);
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsLibraryOpen = false;
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void CycleTheme() => ThemePreference = _theme.IsDark ? ThemePreference.Light : ThemePreference.Dark;

    [RelayCommand]
    private void OpenDataFolder() => Shell?.OpenFolder(DataFolder);

    /// <summary>Avisos de recuperación de archivos corruptos al iniciar.</summary>
    public void ReportRecovered(IEnumerable<string?> files)
    {
        var list = files.OfType<string>().ToList();
        if (list.Count > 0)
            ShowToast(Loc.F("toasts.recoveredFile", System.IO.Path.GetFileName(list[0])), seconds: 8);
    }
}
