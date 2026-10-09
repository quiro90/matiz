using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.App.Localization;
using Matiz.App.ScreenCapture;
using Matiz.App.Services;
using Matiz.Core.Colors;
using Matiz.Core.Formatting;
using Matiz.Core.History;
using Matiz.Core.Palettes;
using Matiz.Core.Parsing;
using Matiz.Core.Persistence;
using Matiz.Core.Session;
using Matiz.Core.Settings;

namespace Matiz.App.ViewModels;

/// <summary>
/// View model de la ventana principal. Dividido en archivos parciales por zona (selector, color actual,
/// generadas, paletas, ajustes, imagen). Todo se sincroniza desde <see cref="Session"/>, la fuente única de verdad.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly JsonStore<AppSettings> _settingsStore;
    private readonly PaletteService _palettes;
    private readonly JsonStore<PaletteLibrary> _paletteStore;
    private readonly ColorHistory _history;
    private readonly JsonStore<HistoryData> _historyStore;
    private readonly ClipboardService _clipboard;
    private readonly ThemeService _theme;
    private readonly HotkeyService? _hotkeys;
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.4) };
    private bool _syncing;
    private bool _frameQueued;
    private Action? _toastAction;

    public MainViewModel(
        AppSettings settings, JsonStore<AppSettings> settingsStore,
        PaletteService palettes, JsonStore<PaletteLibrary> paletteStore,
        ColorHistory history, JsonStore<HistoryData> historyStore,
        ClipboardService clipboard, ThemeService theme, HotkeyService? hotkeys)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _palettes = palettes;
        _paletteStore = paletteStore;
        _history = history;
        _historyStore = historyStore;
        _clipboard = clipboard;
        _theme = theme;
        _hotkeys = hotkeys;

        Session = new ColorSession(InitialState(settings));
        Session.Changed += OnSessionChanged;

        _palettes.Changed += (_, _) =>
        {
            _paletteStore.ScheduleSave(_palettes.Library);
            SyncPalettes();
        };
        _history.Changed += (_, _) =>
        {
            _historyStore.ScheduleSave(HistoryData.From(_history));
            SyncHistory();
        };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            IsToastVisible = false;
        };

        InitSettings();
        InitFormatRows();
        SyncPalettes();
        SyncHistory();
        Refresh();
    }

    public ColorSession Session { get; }

    public IShell? Shell { get; set; }

    private FormatOptions FormatOptions => _settings.ToFormatOptions();

    private static ColorState InitialState(AppSettings s)
    {
        var c = ColorParser.Parse(s.LastColor) ?? Argb.FromRgb(82, 70, 188);
        var state = ColorState.FromArgb(c);
        return s.LastHue is { } h && state.Saturation == 0 ? state.WithHue(h) : state;
    }

    // ---------- sincronización ----------

    private void OnSessionChanged(object? sender, ColorChangedEventArgs e)
    {
        if (e.Kind == ColorChangeKind.Preview)
        {
            // Coalescencia: como máximo un refresco por frame durante arrastres.
            if (_frameQueued) return;
            _frameQueued = true;
            CompositionTarget.Rendering += OnRendering;
            return;
        }

        Refresh();
        if (e.Kind == ColorChangeKind.Commit && e.Source is ColorChangeSource.ScreenCapture or ColorChangeSource.Image or ColorChangeSource.ManualInput)
            _history.Add(e.State.Argb);

        _settings.LastColor = e.State.Argb.ToString();
        _settings.LastHue = e.State.Hue;
        SaveSettings();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _frameQueued = false;
        Refresh();
    }

    private void Refresh()
    {
        var s = Session.Current;
        var c = s.Argb;
        _syncing = true;
        try
        {
            Hue = s.Hue;
            Saturation = s.Saturation;
            Brightness = s.Value;
            HueDeg = Math.Round(s.Hue, 1);
            SatPct = Math.Round(s.Saturation * 100, MidpointRounding.AwayFromZero);
            BriPct = Math.Round(s.Value * 100, MidpointRounding.AwayFromZero);
            RValue = c.R;
            GValue = c.G;
            BValue = c.B;
        }
        finally
        {
            _syncing = false;
        }

        RefreshCurrentColor();
        RefreshGenerated();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    // ---------- copiar / toast ----------

    private void Copy(string text, Argb? color = null, string? what = null)
    {
        if (!_clipboard.TrySetText(text))
        {
            ShowToast(Loc.T("toasts.copyFailed"));
            return;
        }
        if (color is { } c) _history.Add(c);
        var preview = text.Replace("\r", "").Split('\n')[0];
        if (preview.Length > 48) preview = preview[..48] + "…";
        ShowToast(what is null ? Loc.F("toasts.copied", preview) : Loc.F("toasts.copiedWhat", what));
    }

    private void CopyDefault(Argb c) => Copy(ColorFormatters.Get(_settings.DefaultFormatId).Format(c, FormatOptions), c);

    [ObservableProperty] public partial string ToastMessage { get; set; } = "";
    [ObservableProperty] public partial string? ToastActionLabel { get; set; }
    [ObservableProperty] public partial bool IsToastVisible { get; set; }

    public void ShowToast(string message, string? actionLabel = null, Action? action = null, double seconds = 2.4)
    {
        ToastMessage = message;
        ToastActionLabel = actionLabel;
        _toastAction = action;
        IsToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Interval = TimeSpan.FromSeconds(action is null ? seconds : Math.Max(seconds, 6));
        _toastTimer.Start();
    }

    [RelayCommand]
    private void ToastAction()
    {
        var a = _toastAction;
        _toastAction = null;
        IsToastVisible = false;
        a?.Invoke();
    }

    // ---------- comandos globales ----------

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => Session.Undo();

    private bool CanUndo() => Session.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => Session.Redo();

    private bool CanRedo() => Session.CanRedo;

    [RelayCommand]
    private void Capture()
    {
        Shell?.StartScreenCapture(result =>
        {
            if (result is not { } r) return;
            ApplyCapture(r);
        });
    }

    /// <summary>
    /// Enruta el resultado del modo captura: principal (como antes) o secundario del conjunto Personalizado.
    /// El secundario no cambia el color actual ni copia al portapapeles; en modo continuo (Shift) la ventana
    /// no se muestra ni se activa hasta que la sesión de captura termina.
    /// </summary>
    private void ApplyCapture(CaptureResult r)
    {
        if (r.Kind == CaptureResultKind.Secondary)
        {
            AddFreePointFromColor(r.Color);
            if (r.Continue) return;
            if (_settings.ShowAfterCapture) Shell?.ShowAndActivate();
            else ShowToast(Loc.F("toasts.captured", r.Color));
            return;
        }
        Session.Commit(r.Color, ColorChangeSource.ScreenCapture);
        if (r.Continue) return;
        if (_settings.ShowAfterCapture) Shell?.ShowAndActivate();
        if (_settings.CopyOnCapture) CopyDefault(r.Color);
        else ShowToast(Loc.F("toasts.captured", r.Color));
    }

    [RelayCommand]
    private void Paste()
    {
        var text = _clipboard.TryGetText();
        if (text is not null && ColorParser.TryParse(text, out var c))
        {
            Session.Commit(c, ColorChangeSource.ManualInput);
            ShowToast(Loc.F("toasts.pasted", c));
            return;
        }
        if (_clipboard.TryGetImage() is { } img)
        {
            ShowImage(img);
            return;
        }
        ShowToast(Loc.T("toasts.clipboardEmpty"));
    }

    [RelayCommand]
    private void Escape()
    {
        if (IsLibraryOpen) IsLibraryOpen = false;
        else if (IsSettingsOpen) IsSettingsOpen = false;
        else if (IsImageMode) IsImageMode = false;
    }

    /// <summary>Guarda inmediatamente todo lo pendiente (al cerrar la aplicación).</summary>
    public void FlushAll()
    {
        _settingsStore.Save(_settings);
        _paletteStore.Flush();
        _historyStore.Flush();
    }

    private void SaveSettings() => _settingsStore.ScheduleSave(_settings);
}
