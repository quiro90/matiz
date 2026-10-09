using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
        // Indicador del hueco al arrastrar tarjetas de la paleta activa.
        if (AdornerLayer.GetAdornerLayer(PaletteList) is { } adornerLayer)
        {
            _dropIndicator = new DropIndicatorAdorner(PaletteList);
            adornerLayer.Add(_dropIndicator);
            _dropIndicator.Hide();
        }

        foreach (var kb in Shortcuts.CreateBindings(vm)) InputBindings.Add(kb);
        UpdateShortcuts(vm.CaptureHotkey);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CaptureHotkey)) UpdateShortcuts(vm.CaptureHotkey);
        };
        LocalizationService.Instance.LanguageChanged += (_, _) => UpdateShortcuts(_vm.CaptureHotkey);

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

    private void UpdateShortcuts(string captureHotkey)
    {
        ShortcutList.Inlines.Clear();
        ShortcutList.Inlines.Add(new Run(Shortcuts.Describe(captureHotkey)));
        var ruleTitle = Loc.T("settings.shortcuts.ruleTitle");
        var rule = Loc.T("settings.shortcuts.rule");
        if (ruleTitle.Length == 0 && rule.Length == 0) return;
        ShortcutList.Inlines.Add(new LineBreak());
        ShortcutList.Inlines.Add(new LineBreak());
        if (ruleTitle.Length > 0) ShortcutList.Inlines.Add(new Run(ruleTitle) { FontWeight = FontWeights.Bold });
        if (rule.Length > 0)
        {
            ShortcutList.Inlines.Add(new LineBreak());
            ShortcutList.Inlines.Add(new Run(rule));
        }
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

    public void StartScreenCapture(Action<CaptureResult?> onDone) => _picker.Start(onDone);

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

    public void ShowExportOverlay(PaletteExportModel palette)
    {
        var w = new ExportOverlayWindow(palette, _theme) { Owner = this };
        w.ShowDialog();
    }

    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    public BitmapSource? LoadImage(string path, out string? error) => ImageLoader.TryLoad(path, out error);

    /// <summary>Abre los hipervínculos externos (p. ej. el Instagram del bloque de créditos) con el navegador predeterminado.</summary>
    private void ExternalHyperlink_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        OpenExternal(e.Uri.ToString());
        e.Handled = true;
    }

    /// <summary>Abre el enlace del Tag de los botones externos (p. ej. los de donación en Ajustes) con el navegador predeterminado.</summary>
    private void ExternalLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string url) OpenExternal(url);
    }

    private static void OpenExternal(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

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

    // La tarjeta de origen se atenúa mientras está "levantada"; el indicador marca el hueco objetivo.
    private Point _dragStart;
    private PaletteColorItem? _dragItem;
    private Border? _dragOrigin;
    private bool _dragged;
    private DropIndicatorAdorner? _dropIndicator;

    private void PaletteSwatch_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragItem = (sender as FrameworkElement)?.DataContext as PaletteColorItem;
        _dragOrigin = sender as Border;
        _dragged = false;
    }

    private void PaletteSwatch_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragItem is null || e.LeftButton != MouseButtonState.Pressed) return;
        var d = e.GetPosition(this) - _dragStart;
        if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragged = true;
        var item = _dragItem;
        var origin = _dragOrigin;
        _dragItem = null;
        _dragOrigin = null;
        if (origin != null) origin.Opacity = 0.45;
        try
        {
            DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(typeof(PaletteColorItem), item), DragDropEffects.Move);
        }
        finally
        {
            if (origin != null) origin.Opacity = 1.0;
            HideDropIndicator();
        }
    }

    private void PaletteSwatch_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragged && (sender as FrameworkElement)?.DataContext is PaletteColorItem item)
            _vm.UsePaletteColorCommand.Execute(item);
        _dragItem = null;
        _dragOrigin = null;
        _dragged = false;
    }

    // El arrastre se gestiona a nivel del ScrollViewer de la lista: por geometría de las tarjetas deduce el
    // hueco objetivo (cubre también los espacios entre tarjetas y la zona vacía al final).

    private void PaletteList_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(PaletteColorItem))) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
        ShowDropIndicator(ComputeDropGap(e));
        e.Handled = true;
    }

    private void PaletteList_Drop(object sender, DragEventArgs e)
    {
        HideDropIndicator();
        if (e.Data.GetData(typeof(PaletteColorItem)) is not PaletteColorItem source) return; // otros drops (archivos) siguen subiendo
        ApplyDrop(source, ComputeDropGap(e));
        e.Handled = true;
    }

    private void PaletteList_DragLeave(object sender, DragEventArgs e) => HideDropIndicator();

    /// <summary>Hueco objetivo (0..count): primera tarjeta cuyo punto medio queda a la derecha del mouse.</summary>
    private int ComputeDropGap(DragEventArgs e)
    {
        var x = e.GetPosition(PaletteList).X;
        var count = _vm.ActivePaletteColors.Count;
        for (var i = 0; i < count; i++)
        {
            if (PaletteList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement c) continue;
            if (x < c.TranslatePoint(new Point(0, 0), PaletteList).X + c.ActualWidth / 2) return i;
        }
        return count;
    }

    private void ApplyDrop(PaletteColorItem source, int gap)
    {
        var sourceIdx = _vm.ActivePaletteColors.IndexOf(source);
        if (sourceIdx < 0) return;
        var newIndex = gap - (sourceIdx < gap ? 1 : 0); // moveColor trabaja con el índice tras remover
        if (newIndex != sourceIdx) _vm.MovePaletteColor(source, newIndex);
    }

    private void ShowDropIndicator(int gap)
    {
        if (_dropIndicator is null) return;
        var count = _vm.ActivePaletteColors.Count;
        if (count == 0) { _dropIndicator.Hide(); return; }
        // Tarjeta de referencia: la que queda a la derecha del hueco, o la última si el hueco es el final.
        var index = Math.Min(gap, count - 1);
        if (PaletteList.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
        {
            _dropIndicator.Hide();
            return;
        }
        var left = container.TranslatePoint(new Point(0, 0), PaletteList).X;
        // El slot incluye 6px de margen tras la tarjeta: el hueco visual queda centrado en ±3px del borde.
        var x = gap >= count ? left + container.ActualWidth - 3 : left - 3;
        _dropIndicator.Show(x);
    }

    private void HideDropIndicator() => _dropIndicator?.Hide();

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

    /// <summary>Línea vertical que marca dónde caerá la tarjeta mientras se arrastra sobre la paleta activa.</summary>
    private sealed class DropIndicatorAdorner : Adorner
    {
        private static readonly Brush Fill = CreateFill();
        private double _x = double.NaN;

        public DropIndicatorAdorner(UIElement adorned) : base(adorned) => IsHitTestVisible = false;

        private static Brush CreateFill()
        {
            var b = new SolidColorBrush(Color.FromArgb(200, 0x52, 0x46, 0xBC));
            b.Freeze();
            return b;
        }

        public void Show(double x) { _x = x; Visibility = Visibility.Visible; InvalidateVisual(); }

        public void Hide() { _x = double.NaN; Visibility = Visibility.Collapsed; InvalidateVisual(); }

        protected override void OnRender(DrawingContext dc)
        {
            if (double.IsNaN(_x)) return;
            var h = AdornedElement.RenderSize.Height;
            if (h <= 8) return;
            dc.DrawRoundedRectangle(Fill, null, new Rect(_x, 2, 3, h - 4), 1.5, 1.5);
        }
    }
}
