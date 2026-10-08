using System.Windows;
using Matiz.App.Localization;
using Matiz.App.Services;
using Matiz.App.ViewModels;
using Matiz.App.Views;
using Matiz.Core.Localization;
using Matiz.Core.Palettes;
using Matiz.Core.Persistence;

namespace Matiz.App;

/// <summary>Raíz de composición manual (sin contenedor DI) e instancia única.</summary>
public partial class App : Application
{
    private static readonly string InstanceKey = $"Matiz.{Environment.UserName}";

    private Mutex? _mutex;
    private EventWaitHandle? _activate;
    private HotkeyService? _hotkeys;
    private MainViewModel? _vm;
    private readonly List<IDisposable> _stores = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, InstanceKey + ".Mutex", out var first);
        if (!first)
        {
            // Ya hay una instancia: se le pide que se muestre y se sale.
            try
            {
                EventWaitHandle.OpenExisting(InstanceKey + ".Activate").Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
            Shutdown();
            return;
        }
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, InstanceKey + ".Activate");

        var paths = DataPaths.Default();
        DispatcherUnhandledException += (_, ex) =>
        {
            LogError(paths, ex.Exception);
            MessageBox.Show(Loc.F("app.error", ex.Exception.Message), "Matiz",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ex.Handled = true;
        };
        var settingsStore = SettingsRepository.Create(paths);
        var paletteStore = PaletteRepository.Create(paths);
        var historyStore = HistoryRepository.Create(paths);
        _stores.AddRange([settingsStore, paletteStore, historyStore]);

        var settings = settingsStore.Load();
        var library = paletteStore.Load();
        var history = historyStore.Load();

        LocalizationService.Initialize(settings.Value.Language);
        Texts.Current = LocalizationTexts.Instance;

        var theme = new ThemeService();
        theme.Apply(settings.Value.Theme);
        _hotkeys = new HotkeyService();

        _vm = new MainViewModel(
            settings.Value, settingsStore,
            new PaletteService(library.Value), paletteStore,
            history.Value.ToHistory(), historyStore,
            new ClipboardService(), theme, _hotkeys);

        var window = new MainWindow(_vm, theme, settings.Value);
        MainWindow = window;
        window.Show();

        _hotkeys.Pressed += (_, _) => _vm.CaptureCommand.Execute(null);
        _vm.RegisterHotkey();
        _vm.ReportRecovered([settings.RecoveredCorruptFile, library.RecoveredCorruptFile, history.RecoveredCorruptFile]);

        var listener = new Thread(() =>
        {
            while (_activate.WaitOne())
                Dispatcher.BeginInvoke(() => (MainWindow as IShell)?.ShowAndActivate());
        }) { IsBackground = true, Name = "Matiz.SingleInstance" };
        listener.Start();
    }

    private static void LogError(DataPaths paths, Exception ex)
    {
        try
        {
            System.IO.Directory.CreateDirectory(paths.Directory);
            System.IO.File.AppendAllText(System.IO.Path.Combine(paths.Directory, "error.log"), $"[{DateTime.Now:O}] {ex}\n\n");
        }
        catch (System.IO.IOException)
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _vm?.FlushAll();
        }
        finally
        {
            _hotkeys?.Dispose();
            foreach (var s in _stores) s.Dispose();
            _mutex?.Dispose();
        }
        base.OnExit(e);
    }
}
