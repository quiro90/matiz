using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Matiz.App.Interop;
using Matiz.App.Localization;
using Matiz.App.ScreenCapture;
using Matiz.App.Services;
using Matiz.App.ViewModels;
using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Settings;
using Microsoft.Win32;

namespace Matiz.App.Views;

public partial class MainWindow : Window, IShell
{
    private readonly MainViewModel _vm;
    private readonly ThemeService _theme;
    private readonly AppSettings _settings;
    private readonly ScreenPickerController _picker = new();
    private NativeMethods.RECT? _lastNormalRect;

    public MainWindow(MainViewModel vm, ThemeService theme, AppSettings settings)
    {
        _vm = vm;
        _theme = theme;
        _settings = settings;
        InitializeComponent();
        DataContext = vm;
        // Tamaño inicial ajustado al área de trabajo de pantallas pequeñas.
        var work = SystemParameters.WorkArea;
        Width = Math.Min(Width, work.Width - 24);
        Height = Math.Min(Height, work.Height - 24);
        vm.Shell = this;

        foreach (var kb in Shortcuts.CreateBindings(vm)) InputBindings.Add(kb);
        ShortcutList.Text = Shortcuts.Describe(vm.CaptureHotkey);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CaptureHotkey)) ShortcutList.Text = Shortcuts.Describe(vm.CaptureHotkey);
        };
        LocalizationService.Instance.LanguageChanged += (_, _) => ShortcutList.Text = Shortcuts.Describe(_vm.CaptureHotkey);

        SourceInitialized += (_, _) =>
        {
            _theme.ApplyTitleBar(this);
            RestorePlacement();
        };
        LocationChanged += (_, _) => RememberNormalRect();
        SizeChanged += (_, _) => RememberNormalRect();
        Closing += (_, _) => SavePlacement();
        StateChanged += (_, _) => FitMaximized();
        Drop += OnFileDrop;
        DragOver += (_, e) =>
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        };
    }

    // ---------- IShell ----------

    public void ShowAndActivate()
    {
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show();
        Activate();
        // Truco habitual para traer al frente sin dejarla topmost.
        if (!Topmost)
        {
            Topmost = true;
            Topmost = false;
        }
        NativeMethods.SetForegroundWindow(new WindowInteropHelper(this).Handle);
        Focus();
    }

    public void StartScreenCapture(Action<Argb?> onDone) => _picker.Start(onDone);

    public string? PickImageFile()
    {
        var dlg = new OpenFileDialog { Filter = ImageLoader.DialogFilter, Title = Loc.T("dialogs.openImage.title") };
        return dlg.ShowDialog(this) == true ? dlg.FileName : null;
    }

    public void ShowExportImage(PaletteExportModel palette)
    {
        var w = new ExportImageWindow(palette, _theme) { Owner = this };
        w.ShowDialog();
    }

    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    public BitmapSource? LoadImage(string path, out string? error) => ImageLoader.TryLoad(path, out error);

    /// <summary>Abre los hipervínculos externos (p. ej. el Instagram en los créditos) con el navegador predeterminado.</summary>
    private void ExternalHyperlink_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        e.Handled = true;
    }

    // ---------- barra de título integrada ----------

    private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void Close_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    /// <summary>Con WindowChrome, una ventana maximizada desborda la pantalla por el grosor del borde: se compensa.</summary>
    private void FitMaximized()
    {
        if (WindowState != WindowState.Maximized)
        {
            RootGrid.Margin = new Thickness(0);
            return;
        }
        var b = SystemParameters.WindowResizeBorderThickness;
        var f = SystemParameters.WindowNonClientFrameThickness;
        RootGrid.Margin = new Thickness(b.Left + f.Left / 2, b.Top + 2, b.Right + f.Right / 2, b.Bottom + f.Bottom / 2);
    }

    // ---------- eventos de UI ----------

    /// <summary>Abre el ContextMenu de un botón con click izquierdo (botones "▾").</summary>
    private void OpenMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { ContextMenu: { } menu } fe) return;
        menu.DataContext = DataContext;
        menu.PlacementTarget = fe;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void Scrim_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _vm.IsLibraryOpen = false;
        _vm.IsSettingsOpen = false;
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.Tab) return;
        e.Handled = true;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;
        var mods = Keyboard.Modifiers;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) mods |= ModifierKeys.Windows;
        if (mods == ModifierKeys.None)
        {
            _vm.ShowToast(Loc.T("toasts.hotkeyModifier"));
            return;
        }
        _vm.SetHotkeyCommand.Execute(HotkeyGesture.Format(mods, key));
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            _vm.LoadImageFile(files[0]);
    }

    // ---------- paleta activa: click y arrastrar para reordenar ----------

    private Point _dragStart;
    private PaletteColorItem? _dragItem;
    private bool _dragged;

    private void PaletteSwatch_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragItem = (sender as FrameworkElement)?.DataContext as PaletteColorItem;
        _dragged = false;
    }

    private void PaletteSwatch_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragItem is null || e.LeftButton != MouseButtonState.Pressed) return;
        var d = e.GetPosition(this) - _dragStart;
        if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragged = true;
        var item = _dragItem;
        _dragItem = null;
        DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(PaletteColorItem), item), DragDropEffects.Move);
    }

    private void PaletteSwatch_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragged && (sender as FrameworkElement)?.DataContext is PaletteColorItem item)
            _vm.UsePaletteColorCommand.Execute(item);
        _dragItem = null;
        _dragged = false;
    }

    private void PaletteSwatch_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(PaletteColorItem)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void PaletteSwatch_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(PaletteColorItem)) is not PaletteColorItem source) return;
        if ((sender as FrameworkElement)?.DataContext is not PaletteColorItem target || target.Id == source.Id) return;
        var index = _vm.ActivePaletteColors.IndexOf(target);
        _vm.MovePaletteColor(source, index);
        e.Handled = true;
    }

    // ---------- posición de la ventana (píxeles físicos) ----------

    private void RememberNormalRect()
    {
        if (WindowState != WindowState.Normal) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero && NativeMethods.GetWindowRect(hwnd, out var r)) _lastNormalRect = r;
    }

    private void RestorePlacement()
    {
        if (_settings.Window is not { Width: > 200, Height: > 200 } p) return;
        var rect = new NativeMethods.RECT { Left = p.Left, Top = p.Top, Right = p.Left + p.Width, Bottom = p.Top + p.Height };
        // Si el monitor donde estaba ya no existe, se deja la posición por defecto (monitor principal).
        if (NativeMethods.MonitorFromRect(ref rect, NativeMethods.MONITOR_DEFAULTTONULL) == IntPtr.Zero) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        WindowStartupLocation = WindowStartupLocation.Manual;
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, p.Left, p.Top, p.Width, p.Height, NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        if (p.Maximized) WindowState = WindowState.Maximized;
    }

    private void SavePlacement()
    {
        RememberNormalRect();
        if (_lastNormalRect is not { } r) return;
        _settings.Window = new WindowPlacement
        {
            Left = r.Left,
            Top = r.Top,
            Width = r.Width,
            Height = r.Height,
            Maximized = WindowState == WindowState.Maximized,
        };
    }
}
