using Matiz.Core.Colors;
using Matiz.Core.Localization;

namespace Matiz.Core.Palettes;

/// <summary>
/// Operaciones sobre la biblioteca de paletas. Toda modificación actualiza ModifiedAt y emite <see cref="Changed"/>
/// (la capa de persistencia se suscribe para autoguardar).
/// </summary>
public sealed class PaletteService
{
    public static string DefaultName => Texts.Current.UntitledPalette;

    private readonly Func<DateTimeOffset> _clock;

    public PaletteService(PaletteLibrary library, Func<DateTimeOffset>? clock = null)
    {
        Library = library;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        if (Library.ActivePaletteId is { } id && Find(id) is null) Library.ActivePaletteId = null;
        Library.ActivePaletteId ??= Library.Palettes.FirstOrDefault()?.Id;
    }

    public PaletteLibrary Library { get; }

    public IReadOnlyList<Palette> Palettes => Library.Palettes;

    public Palette? Active => Library.ActivePaletteId is { } id ? Find(id) : null;

    public event EventHandler? Changed;

    public Palette? Find(Guid id) => Library.Palettes.FirstOrDefault(p => p.Id == id);

    public void SetActive(Guid? id)
    {
        if (id is { } v && Find(v) is null) return;
        Library.ActivePaletteId = id;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Palette Create(string? name = null, bool activate = true)
    {
        var now = _clock();
        var p = new Palette
        {
            Name = UniqueName(string.IsNullOrWhiteSpace(name) ? DefaultName : name.Trim()),
            CreatedAt = now,
            ModifiedAt = now,
        };
        Library.Palettes.Add(p);
        if (activate) Library.ActivePaletteId = p.Id;
        Changed?.Invoke(this, EventArgs.Empty);
        return p;
    }

    /// <summary>Paleta activa, creando una nueva si no hay ninguna.</summary>
    public Palette EnsureActive() => Active ?? Create();

    public void Rename(Guid id, string name)
    {
        if (Find(id) is not { } p || string.IsNullOrWhiteSpace(name)) return;
        p.Name = name.Trim();
        Touch(p);
    }

    public void SetDescription(Guid id, string? description)
    {
        if (Find(id) is not { } p) return;
        p.Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Touch(p);
    }

    public Palette? Duplicate(Guid id)
    {
        if (Find(id) is not { } src) return null;
        var now = _clock();
        var copy = new Palette
        {
            Name = UniqueName($"{src.Name} {Texts.Current.CopySuffix}"),
            Description = src.Description,
            CreatedAt = now,
            ModifiedAt = now,
            Colors = src.Colors.Select(c => c.Clone()).ToList(),
        };
        Library.Palettes.Insert(Library.Palettes.IndexOf(src) + 1, copy);
        Library.ActivePaletteId = copy.Id;
        Changed?.Invoke(this, EventArgs.Empty);
        return copy;
    }

    /// <summary>Elimina y devuelve la paleta y su índice (para deshacer con <see cref="Restore"/>).</summary>
    public (Palette Palette, int Index)? Delete(Guid id)
    {
        if (Find(id) is not { } p) return null;
        var index = Library.Palettes.IndexOf(p);
        Library.Palettes.RemoveAt(index);
        if (Library.ActivePaletteId == id)
            Library.ActivePaletteId = Library.Palettes.Count == 0 ? null : Library.Palettes[Math.Min(index, Library.Palettes.Count - 1)].Id;
        Changed?.Invoke(this, EventArgs.Empty);
        return (p, index);
    }

    public void Restore(Palette palette, int index)
    {
        if (Find(palette.Id) is not null) return;
        Library.Palettes.Insert(Math.Clamp(index, 0, Library.Palettes.Count), palette);
        Library.ActivePaletteId = palette.Id;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public PaletteColor? AddColor(Guid paletteId, Argb color, string? name = null)
    {
        if (Find(paletteId) is not { } p) return null;
        var pc = PaletteColor.Create(color, Normalize(name));
        p.Colors.Add(pc);
        Touch(p);
        return pc;
    }

    public void AddColors(Guid paletteId, IEnumerable<(Argb Color, string? Name)> colors)
    {
        if (Find(paletteId) is not { } p) return;
        foreach (var (c, n) in colors) p.Colors.Add(PaletteColor.Create(c, Normalize(n)));
        Touch(p);
    }

    public void RenameColor(Guid paletteId, Guid colorId, string? name)
    {
        if (FindColor(paletteId, colorId) is not ({ } p, { } c)) return;
        c.Name = Normalize(name);
        Touch(p);
    }

    public void ReplaceColor(Guid paletteId, Guid colorId, Argb color)
    {
        if (FindColor(paletteId, colorId) is not ({ } p, { } c)) return;
        c.Color = color;
        Touch(p);
    }

    public void RemoveColor(Guid paletteId, Guid colorId)
    {
        if (FindColor(paletteId, colorId) is not ({ } p, { } c)) return;
        p.Colors.Remove(c);
        Touch(p);
    }

    public void MoveColor(Guid paletteId, Guid colorId, int newIndex)
    {
        if (FindColor(paletteId, colorId) is not ({ } p, { } c)) return;
        var old = p.Colors.IndexOf(c);
        newIndex = Math.Clamp(newIndex, 0, p.Colors.Count - 1);
        if (old == newIndex) return;
        p.Colors.RemoveAt(old);
        p.Colors.Insert(newIndex, c);
        Touch(p);
    }

    private (Palette, PaletteColor)? FindColor(Guid paletteId, Guid colorId)
    {
        if (Find(paletteId) is not { } p) return null;
        var c = p.Colors.FirstOrDefault(x => x.Id == colorId);
        return c is null ? null : (p, c);
    }

    private void Touch(Palette p)
    {
        var now = _clock();
        p.ModifiedAt = now > p.ModifiedAt ? now : p.ModifiedAt.AddTicks(1);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private string UniqueName(string name)
    {
        if (Library.Palettes.All(p => !string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase))) return name;
        for (var i = 2; ; i++)
        {
            var candidate = $"{name} {i}";
            if (Library.Palettes.All(p => !string.Equals(p.Name, candidate, StringComparison.CurrentCultureIgnoreCase)))
                return candidate;
        }
    }

    private static string? Normalize(string? name) => string.IsNullOrWhiteSpace(name) ? null : name.Trim();
}
