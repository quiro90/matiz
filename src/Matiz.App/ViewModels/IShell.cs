using System.Windows.Media.Imaging;
using Matiz.App.ScreenCapture;
using Matiz.Core.Colors;
using Matiz.Core.Export;

namespace Matiz.App.ViewModels;

/// <summary>Operaciones de ventana/plataforma que el view model delega en la vista.</summary>
public interface IShell
{
    void ShowAndActivate();
    void StartScreenCapture(Action<CaptureResult?> onDone);
    string? PickImageFile();
    void ShowExportImage(PaletteExportModel palette);
    void ShowExportOverlay(PaletteExportModel palette);
    void OpenFolder(string path);
    BitmapSource? LoadImage(string path, out string? error);
}
