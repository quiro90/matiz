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
    private const string PendingImportFile = "import.mpalette.pending";

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
            // Ya hay una instancia: le delega las paletas abiertas por doble click y le pide que se muestre.
            foreach (var path in e.Args.Where(IsPaletteFile)) EnqueuePendingImport(path);
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

        // Importa la paleta dejada pendiente (delegada antes de que el listener exista) y las de los args
        // de arranque (doble click del OS sobre un .mpalette), ya con la UI lista.
        foreach (var path in TryReadPendingImports().Concat(e.Args.Where(IsPaletteFile)).Distinct(StringComparer.OrdinalIgnoreCase))
            Dispatcher.BeginInvoke(() => _vm?.ImportPaletteFile(path));

        var listener = new Thread(() =>
        {
            while (_activate.WaitOne())
                Dispatcher.BeginInvoke(() =>
                {
                    foreach (var path in TryReadPendingImports()) _vm?.ImportPaletteFile(path);
                    (MainWindow as IShell)?.ShowAndActivate();
                });
        }) { IsBackground = true, Name = "Matiz.SingleInstance" };
        listener.Start();
    }

    private static bool IsPaletteFile(string arg) => arg.EndsWith(".mpalette", StringComparison.OrdinalIgnoreCase);

    /// <summary>Deja anotado un .mpalette para que la instancia viva lo importe al recibir la señal.</summary>
    private static void EnqueuePendingImport(string path)
    {
        try
        {
            var dir = DataPaths.Default().Directory;
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(dir, PendingImportFile), path + Environment.NewLine);
        }
        catch (System.IO.IOException)
        {
        }
    }

    /// <summary>Lee y borra las rutas pendientes; descarta líneas vacías o archivos inexistentes.</summary>
    private static List<string> TryReadPendingImports()
    {
        var result = new List<string>();
        try
        {
            var file = System.IO.Path.Combine(DataPaths.Default().Directory, PendingImportFile);
            if (!System.IO.File.Exists(file)) return result;
            result.AddRange(System.IO.File.ReadAllLines(file).Where(l => l.Length > 0));
            System.IO.File.Delete(file);
        }
        catch (System.IO.IOException)
        {
        }
        return result;
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
