using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Matiz.App.Imaging;
using Matiz.App.Localization;
using Matiz.App.Services;
using Matiz.Core.Export;
using Microsoft.Win32;

namespace Matiz.App.Views;

/// <summary>Vista previa y opciones de exportación de una paleta a PNG apilando sus colores como superpuestos.</summary>
public partial class ExportOverlayWindow : Window
{
    private readonly PaletteExportModel _palette;
    private BitmapSource? _bitmap;
    private bool _ready;

    public ExportOverlayWindow(PaletteExportModel palette, ThemeService theme)
    {
        _palette = palette;
        InitializeComponent();
        SourceInitialized += (_, _) => theme.ApplyTitleBar(this);
        _ready = true;
        Render();
    }

    private OverlayImageOptions Options => new(
        DarkBackground: DarkBg.IsChecked == true && TransparentBg.IsChecked != true,
        Transparent: TransparentBg.IsChecked == true,
        Scale: Scale1.IsChecked == true ? 1 : Scale3.IsChecked == true ? 3 : 2,
        Shape: ShapeCircle.IsChecked == true ? OverlayShape.Circle : ShapeTriangle.IsChecked == true ? OverlayShape.Triangle : OverlayShape.Square,
        Reverse: OrderReverse.IsChecked == true,
        ShowHsl: ShowHsl.IsChecked == true,
        ShowCmyk: ShowCmyk.IsChecked == true);

    private void OnOptionChanged(object sender, RoutedEventArgs e) => Render();

    private void Render()
    {
        if (!_ready) return;
        _bitmap = OverlayImageRenderer.Render(_palette, Options);
        Preview.Source = _bitmap;
        SizeText.Text = $"{_bitmap.PixelWidth} × {_bitmap.PixelHeight} px";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_bitmap is null) return;
        var name = string.Concat(_palette.Name.Split(Path.GetInvalidFileNameChars()));
        var dlg = new SaveFileDialog
        {
            Filter = Loc.T("export.pngFilter"),
            FileName = string.IsNullOrWhiteSpace(name) ? Loc.T("export.defaultFileName") + ".png" : $"{name}.png",
            Title = Loc.T("overlay.saveDialog"),
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            PaletteImageRenderer.SavePng(_bitmap, dlg.FileName);
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, Loc.F("export.saveError", ex.Message), "Matiz", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyImage_Click(object sender, RoutedEventArgs e)
    {
        if (_bitmap is null) return;
        try
        {
            Clipboard.SetImage(_bitmap);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(this, Loc.T("export.copyFailed"), "Matiz");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}