using Matiz.Core.Colors;

namespace Matiz.Core.History;

/// <summary>Colores recientes: más reciente primero, sin duplicados, máximo <see cref="Capacity"/>.</summary>
public sealed class ColorHistory
{
    public const int Capacity = 30;

    private readonly List<Argb> _items = [];

    public ColorHistory(IEnumerable<Argb>? items = null)
    {
        if (items is null) return;
        foreach (var c in items.Reverse()) Add(c, raise: false);
    }

    public IReadOnlyList<Argb> Items => _items;

    public event EventHandler? Changed;

    public void Add(Argb color) => Add(color, raise: true);

    public void Clear()
    {
        _items.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Add(Argb color, bool raise)
    {
        var existing = _items.IndexOf(color);
        if (existing == 0) return;
        if (existing > 0) _items.RemoveAt(existing);
        _items.Insert(0, color);
        if (_items.Count > Capacity) _items.RemoveRange(Capacity, _items.Count - Capacity);
        if (raise) Changed?.Invoke(this, EventArgs.Empty);
    }
}
