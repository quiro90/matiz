using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.Core.Colors;
using Matiz.Core.Generation;
using Matiz.Core.Session;

namespace Matiz.App.ViewModels;

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial bool IsImageMode { get; set; }
    [ObservableProperty] public partial BitmapSource? ImageSource { get; private set; }
    [ObservableProperty] public partial string ImageInfo { get; private set; } = "";
    [ObservableProperty] public partial double ExtractCount { get; set; } = 6;
    [ObservableProperty] public partial bool IsExtracting { get; private set; }

    [RelayCommand]
    private void OpenImage()
    {
        if (Shell?.PickImageFile() is { } path) LoadImageFile(path);
    }

    public void LoadImageFile(string path)
    {
        if (Shell is null) return;
        if (Shell.LoadImage(path, out var error) is { } img) ShowImage(img, System.IO.Path.GetFileName(path));
        else ShowToast(error ?? "No se pudo abrir la imagen", seconds: 5);
    }

    public void ShowImage(BitmapSource image, string? name = null)
    {
        ImageSource = image;
        ImageInfo = $"{name ?? "Imagen pegada"} · {image.PixelWidth}×{image.PixelHeight}";
        IsSettingsOpen = false;
        IsLibraryOpen = false;
        IsImageMode = true;
    }

    [RelayCommand]
    private void CloseImage() => IsImageMode = false;

    [RelayCommand]
    private void ToggleImageMode()
    {
        if (IsImageMode) IsImageMode = false;
        else if (ImageSource is not null) IsImageMode = true;
        else OpenImage();
    }

    [RelayCommand]
    private void PickImageColor(Argb color) => Session.Commit(color, ColorChangeSource.Image);

    [RelayCommand]
    private async Task ExtractColors()
    {
        if (ImageSource is not { } src || IsExtracting) return;
        IsExtracting = true;
        try
        {
            var bgra = src.Format == PixelFormats.Bgra32 ? src : new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
            var w = bgra.PixelWidth;
            var h = bgra.PixelHeight;
            var pixels = new byte[w * h * 4];
            bgra.CopyPixels(pixels, w * 4, 0);
            var count = (int)Math.Clamp(ExtractCount, 3, 10);
            var colors = await Task.Run(() => DominantColors.Extract(pixels, w, h, w * 4, count));
            _extracted = colors.Select((c, i) => new GeneratedColor((i + 1).ToString(), c)).ToList();
            HasExtracted = _extracted.Count > 0;
            GeneratedTab = GeneratedTab.Extracted;
            RefreshGenerated();
            ShowToast($"{_extracted.Count} colores principales extraídos");
        }
        finally
        {
            IsExtracting = false;
        }
    }
}
