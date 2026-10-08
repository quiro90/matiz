using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Matiz.App.Localization;

namespace Matiz.App.Services;

/// <summary>Carga de imágenes vía WIC (PNG, JPEG, BMP, GIF primer fotograma, TIFF, WebP si el códec está instalado).</summary>
public static class ImageLoader
{
    public static string DialogFilter => Loc.T("dialogs.imageFilter");

    public static BitmapSource? TryLoad(string path, out string? error)
    {
        error = null;
        try
        {
            using var stream = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
            {
                error = Loc.T("image.noFrames");
                return null;
            }
            BitmapSource frame = decoder.Frames[0];
            if (frame.Format != PixelFormats.Bgra32) frame = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            frame.Freeze();
            return frame;
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            error = Loc.F("toasts.openImageFailed.reason", ex.Message);
            return null;
        }
    }

    public static bool LooksLikeImage(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff" or ".webp";
}
