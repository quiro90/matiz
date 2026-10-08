using System.Windows.Input;
using Matiz.App.Localization;
using Matiz.App.ViewModels;

namespace Matiz.App.Services;

/// <summary>
/// Tabla central de atajos de ventana (acción → combinación). Preparada para hacerse configurable:
/// la ventana construye sus InputBindings a partir de esta tabla. El atajo global de captura vive en ajustes.
/// Los atajos no interceptan la edición de texto: los TextBox gestionan antes Ctrl+C/V/Z/Y.
/// </summary>
public static class Shortcuts
{
    public sealed record Entry(string Gesture, string TextKey, Func<MainViewModel, ICommand> Command);

    public static IReadOnlyList<Entry> Default { get; } =
    [
        new("Ctrl+C", "shortcuts.copyDefault", vm => vm.CopyDefaultFormatCommand),
        new("Ctrl+Shift+C", "swatch.copyHex", vm => vm.CopyHexCommand),
        new("Ctrl+V", "shortcuts.paste", vm => vm.PasteCommand),
        new("Ctrl+Z", "shortcuts.undo", vm => vm.UndoCommand),
        new("Ctrl+Y", "shortcuts.redo", vm => vm.RedoCommand),
        new("Ctrl+Shift+Z", "shortcuts.redo", vm => vm.RedoCommand),
        new("Ctrl+S", "shortcuts.addPalette", vm => vm.AddCurrentToPaletteCommand),
        new("Ctrl+N", "shortcuts.newPalette", vm => vm.NewPaletteCommand),
        new("Ctrl+O", "shortcuts.openImage", vm => vm.OpenImageCommand),
        new("Ctrl+E", "shortcuts.export", vm => vm.ExportActivePaletteImageCommand),
        new("Escape", "shortcuts.escape", vm => vm.EscapeCommand),
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
        string.Join("\n", new[] { $"{captureHotkey}  —  {Loc.T("shortcuts.capture")}" }
            .Concat(Default.Where(e => e.Gesture != "Ctrl+Shift+Z").Select(e => $"{e.Gesture}  —  {Loc.T(e.TextKey)}")));
}
