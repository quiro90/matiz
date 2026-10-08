using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Matiz.App.Imaging;
using Matiz.App.Services;
using Matiz.Core.Export;
using Microsoft.Win32;

namespace Matiz.App.Views;

/// <summary>Vista previa y opciones de exportación de una paleta a PNG.</summary>
public partial class ExportImageWindow : Window
{
    private readonly PaletteExportModel _palette;
    private BitmapSource? _bitmap;
    private bool _ready;

    public ExportImageWindow(PaletteExportModel palette, ThemeService theme)
    {
        _palette = palette;
        InitializeComponent();
        SourceInitialized += (_, _) => theme.ApplyTitleBar(this);
        _ready = true;
        Render();
    }

    private PaletteImageOptions Options => new(
        Horizontal: Horizontal.IsChecked == true,
        Scale: Scale1.IsChecked == true ? 1 : Scale3.IsChecked == true ? 3 : 2,
        DarkBackground: LightBg.IsChecked != true,
        ShowHsl: ShowHsl.IsChecked == true,
        ShowCmyk: ShowCmyk.IsChecked == true);

    private void OnOptionChanged(object sender, RoutedEventArgs e) => Render();

    private void Render()
    {
        if (!_ready) return;
        _bitmap = PaletteImageRenderer.Render(_palette, Options);
        Preview.Source = _bitmap;
        SizeText.Text = $"{_bitmap.PixelWidth} × {_bitmap.PixelHeight} px";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_bitmap is null) return;
        var name = string.Concat(_palette.Name.Split(Path.GetInvalidFileNameChars()));
        var dlg = new SaveFileDialog
        {
            Filter = "Imagen PNG|*.png",
            FileName = string.IsNullOrWhiteSpace(name) ? "paleta.png" : $"{name}.png",
            Title = "Guardar paleta como PNG",
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            PaletteImageRenderer.SavePng(_bitmap, dlg.FileName);
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"No se pudo guardar la imagen:\n{ex.Message}", "Matiz", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show(this, "No se pudo copiar: el portapapeles está ocupado.", "Matiz");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
