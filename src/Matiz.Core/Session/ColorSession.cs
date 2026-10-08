using Matiz.Core.Colors;

namespace Matiz.Core.Session;

public enum ColorChangeKind
{
    /// <summary>Cambio en curso (arrastre). No entra en la pila de deshacer.</summary>
    Preview,
    /// <summary>Cambio confirmado.</summary>
    Commit,
    Undo,
    Redo,
}

/// <summary>Origen de un cambio confirmado (para historial y otras reacciones).</summary>
public enum ColorChangeSource
{
    Picker,
    ManualInput,
    ScreenCapture,
    Image,
    History,
    Palette,
    Generated,
    Gray,
    Previous,
    UndoRedo,
    Startup,
}

public sealed record ColorChangedEventArgs(ColorState State, ColorChangeKind Kind, ColorChangeSource Source);

/// <summary>
/// Fuente única de verdad del Color Actual: color actual, color anterior y pilas de deshacer/rehacer
/// formadas solo por cambios confirmados.
/// </summary>
public sealed class ColorSession
{
    public const int UndoLimit = 50;

    private readonly LinkedList<ColorState> _undo = new();
    private readonly Stack<ColorState> _redo = new();
    private ColorState _committed;

    public ColorSession(ColorState initial)
    {
        _committed = initial;
        Current = initial;
        Previous = initial;
    }

    public ColorState Current { get; private set; }

    /// <summary>Color actual previo al último cambio confirmado.</summary>
    public ColorState Previous { get; private set; }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public int UndoCount => _undo.Count;

    public event EventHandler<ColorChangedEventArgs>? Changed;

    public void SetPreview(ColorState state)
    {
        Current = state;
        Changed?.Invoke(this, new(state, ColorChangeKind.Preview, ColorChangeSource.Picker));
    }

    public void Commit(ColorState state, ColorChangeSource source = ColorChangeSource.Picker)
    {
        if (state.Argb != _committed.Argb)
        {
            _undo.AddLast(_committed);
            if (_undo.Count > UndoLimit) _undo.RemoveFirst();
            _redo.Clear();
            Previous = _committed;
        }
        _committed = state;
        Current = state;
        Changed?.Invoke(this, new(state, ColorChangeKind.Commit, source));
    }

    /// <summary>Confirma un color canónico conservando hue/saturación si es acromático.</summary>
    public void Commit(Argb color, ColorChangeSource source) =>
        Commit(ColorState.FromArgb(color, Current), source);

    /// <summary>Restaura el color anterior (también es un cambio confirmado: anterior y actual se intercambian).</summary>
    public void RestorePrevious() => Commit(Previous, ColorChangeSource.Previous);

    public bool Undo()
    {
        if (_undo.Count == 0) return false;
        var state = _undo.Last!.Value;
        _undo.RemoveLast();
        _redo.Push(_committed);
        Previous = _committed;
        _committed = state;
        Current = state;
        Changed?.Invoke(this, new(state, ColorChangeKind.Undo, ColorChangeSource.UndoRedo));
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0) return false;
        var state = _redo.Pop();
        _undo.AddLast(_committed);
        Previous = _committed;
        _committed = state;
        Current = state;
        Changed?.Invoke(this, new(state, ColorChangeKind.Redo, ColorChangeSource.UndoRedo));
        return true;
    }
}
