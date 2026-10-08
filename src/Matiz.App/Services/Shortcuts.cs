using System.Windows.Input;
using Matiz.App.ViewModels;

namespace Matiz.App.Services;

/// <summary>
/// Tabla central de atajos de ventana (acción → combinación). Preparada para hacerse configurable:
/// la ventana construye sus InputBindings a partir de esta tabla. El atajo global de captura vive en ajustes.
/// Los atajos no interceptan la edición de texto: los TextBox gestionan antes Ctrl+C/V/Z/Y.
/// </summary>
public static class Shortcuts
{
    public sealed record Entry(string Gesture, string Description, Func<MainViewModel, ICommand> Command);

    public static IReadOnlyList<Entry> Default { get; } =
    [
        new("Ctrl+C", "Copiar el color en el formato principal", vm => vm.CopyDefaultFormatCommand),
        new("Ctrl+Shift+C", "Copiar HEX", vm => vm.CopyHexCommand),
        new("Ctrl+V", "Pegar un color o una imagen", vm => vm.PasteCommand),
        new("Ctrl+Z", "Deshacer cambio de color", vm => vm.UndoCommand),
        new("Ctrl+Y", "Rehacer cambio de color", vm => vm.RedoCommand),
        new("Ctrl+Shift+Z", "Rehacer cambio de color", vm => vm.RedoCommand),
        new("Ctrl+S", "Agregar el color actual a la paleta activa", vm => vm.AddCurrentToPaletteCommand),
        new("Ctrl+N", "Nueva paleta", vm => vm.NewPaletteCommand),
        new("Ctrl+O", "Abrir imagen", vm => vm.OpenImageCommand),
        new("Ctrl+E", "Exportar la paleta activa como PNG", vm => vm.ExportActivePaletteImageCommand),
        new("Escape", "Cerrar panel / volver al selector", vm => vm.EscapeCommand),
    ];

    public static IEnumerable<KeyBinding> CreateBindings(MainViewModel vm)
    {
        var converter = new KeyGestureConverter();
        foreach (var e in Default)
        {
            if (converter.ConvertFromInvariantString(e.Gesture) is KeyGesture g)
                yield return new KeyBinding(e.Command(vm), g);
        }
    }

    public static string Describe(string captureHotkey) =>
        string.Join("\n", new[] { $"{captureHotkey}  —  Capturar (global, desde cualquier aplicación)" }
            .Concat(Default.Where(e => e.Gesture != "Ctrl+Shift+Z").Select(e => $"{e.Gesture}  —  {e.Description}")));
}
