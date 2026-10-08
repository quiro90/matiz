using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Matiz.App.Services;

/// <summary>Portapapeles con reintentos breves cuando otra aplicación lo mantiene abierto.</summary>
public sealed class ClipboardService
{
    private const int Attempts = 6;

    public bool TrySetText(string text)
    {
        for (var i = 0; i < Attempts; i++)
        {
            try
            {
                Clipboard.SetDataObject(text, copy: true);
                return true;
            }
            catch (Exception ex) when (ex is COMException or ExternalException)
            {
                Thread.Sleep(30);
            }
        }
        return false;
    }

    public string? TryGetText()
    {
        for (var i = 0; i < Attempts; i++)
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (Exception ex) when (ex is COMException or ExternalException)
            {
                Thread.Sleep(30);
            }
        }
        return null;
    }

    /// <summary>Imagen del portapapeles (bitmap o archivo de imagen copiado), si la hay.</summary>
    public BitmapSource? TryGetImage()
    {
        try
        {
            if (Clipboard.ContainsFileDropList())
            {
                var file = Clipboard.GetFileDropList().Cast<string>().FirstOrDefault();
                if (file is not null && File.Exists(file)) return ImageLoader.TryLoad(file, out _);
            }
            if (Clipboard.ContainsImage()) return Clipboard.GetImage();
        }
        catch (Exception ex) when (ex is COMException or ExternalException)
        {
        }
        return null;
    }
}
