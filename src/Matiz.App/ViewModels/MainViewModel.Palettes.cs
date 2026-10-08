using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.Core.Export;
using Matiz.Core.Formatting;
using Matiz.Core.Session;

namespace Matiz.App.ViewModels;

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial bool IsLibraryOpen { get; set; }
    [ObservableProperty] public partial string ActivePaletteName { get; private set; } = "";
    [ObservableProperty] public partial bool HasActiveColors { get; private set; }
    [ObservableProperty] public partial PaletteItem? SelectedPalette { get; set; }

    public ObservableCollection<PaletteColorItem> ActivePaletteColors { get; } = [];
    public ObservableCollection<PaletteItem> Palettes { get; } = [];
    public ObservableCollection<SwatchItem> HistoryItems { get; } = [];

    private bool _syncingPalettes;

    private void SyncPalettes()
    {
        _syncingPalettes = true;
        try
        {
            // Biblioteca: reutiliza items por Id.
            var models = _palettes.Palettes;
            for (var i = Palettes.Count - 1; i >= 0; i--)
                if (models.All(m => m.Id != Palettes[i].Id)) Palettes.RemoveAt(i);
            for (var i = 0; i < models.Count; i++)
            {
                var existing = Palettes.FirstOrDefault(p => p.Id == models[i].Id);
                if (existing is null)
                {
                    Palettes.Insert(i, new PaletteItem(models[i], OnPaletteItemEdited));
                }
                else
                {
                    var at = Palettes.IndexOf(existing);
                    if (at != i) Palettes.Move(at, i);
                    existing.Refresh();
                }
            }

            var active = _palettes.Active;
            SelectedPalette = active is null ? null : Palettes.FirstOrDefault(p => p.Id == active.Id);
            ActivePaletteName = active?.Name ?? "Sin paleta";

            ActivePaletteColors.Clear();
            if (active is not null)
                foreach (var c in active.Colors) ActivePaletteColors.Add(new PaletteColorItem(c, OnPaletteColorRenamed));
            HasActiveColors = ActivePaletteColors.Count > 0;
            ExportActivePaletteImageCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            _syncingPalettes = false;
        }
    }

    private void SyncHistory()
    {
        HistoryItems.Clear();
        foreach (var c in _history.Items) HistoryItems.Add(new SwatchItem(c));
    }

    partial void OnSelectedPaletteChanged(PaletteItem? value)
    {
        if (_syncingPalettes || value is null) return;
        _palettes.SetActive(value.Id);
    }

    private void OnPaletteItemEdited(PaletteItem item)
    {
        _palettes.Rename(item.Id, item.Model.Name);
        _palettes.SetDescription(item.Id, item.Model.Description);
    }

    private void OnPaletteColorRenamed(PaletteColorItem item, string? name)
    {
        if (_palettes.Active is { } p) _palettes.RenameColor(p.Id, item.Id, name);
    }

    [RelayCommand]
    private void ToggleLibrary()
    {
        IsSettingsOpen = false;
        IsLibraryOpen = !IsLibraryOpen;
    }

    [RelayCommand]
    private void AddCurrentToPalette()
    {
        var p = _palettes.EnsureActive();
        _palettes.AddColor(p.Id, Session.Current.Argb);
        ShowToast($"{Session.Current.Argb} agregado a «{p.Name}»");
    }

    [RelayCommand]
    private void NewPalette()
    {
        _palettes.Create();
        IsSettingsOpen = false;
        IsLibraryOpen = true;
    }

    [RelayCommand]
    private void DuplicatePalette()
    {
        if (_palettes.Active is { } p) _palettes.Duplicate(p.Id);
    }

    [RelayCommand]
    private void DeletePalette(PaletteItem? item)
    {
        var id = item?.Id ?? _palettes.Active?.Id;
        if (id is not { } v || _palettes.Delete(v) is not { } removed) return;
        ShowToast($"Paleta «{removed.Palette.Name}» eliminada", "Deshacer", () => _palettes.Restore(removed.Palette, removed.Index));
    }

    [RelayCommand]
    private void UsePaletteColor(PaletteColorItem? item)
    {
        if (item is not null) Session.Commit(item.Color, ColorChangeSource.Palette);
    }

    [RelayCommand]
    private void CopyPaletteColor(PaletteColorItem? item)
    {
        if (item is not null) CopyDefault(item.Color);
    }

    [RelayCommand]
    private void CopyPaletteColorHex(PaletteColorItem? item)
    {
        if (item is not null) Copy(ColorFormats.Hex(item.Color, FormatOptions), item.Color);
    }

    [RelayCommand]
    private void ReplacePaletteColor(PaletteColorItem? item)
    {
        if (item is not null && _palettes.Active is { } p) _palettes.ReplaceColor(p.Id, item.Id, Session.Current.Argb);
    }

    [RelayCommand]
    private void RemovePaletteColor(PaletteColorItem? item)
    {
        if (item is null || _palettes.Active is not { } p) return;
        var index = p.Colors.FindIndex(c => c.Id == item.Id);
        var model = p.Colors[index];
        _palettes.RemoveColor(p.Id, item.Id);
        ShowToast($"Color {item.Hex} eliminado", "Deshacer", () =>
        {
            if (_palettes.Find(p.Id) is not { } target) return;
            var added = _palettes.AddColor(target.Id, model.Color, model.Name);
            if (added is not null) _palettes.MoveColor(target.Id, added.Id, index);
        });
    }

    [RelayCommand]
    private void MovePaletteColorLeft(PaletteColorItem? item) => MovePaletteColor(item, -1, relative: true);

    [RelayCommand]
    private void MovePaletteColorRight(PaletteColorItem? item) => MovePaletteColor(item, 1, relative: true);

    /// <summary>Mueve un color de la paleta activa (usado también por arrastrar y soltar).</summary>
    public void MovePaletteColor(PaletteColorItem? item, int index, bool relative = false)
    {
        if (item is null || _palettes.Active is not { } p) return;
        var current = p.Colors.FindIndex(c => c.Id == item.Id);
        if (current < 0) return;
        _palettes.MoveColor(p.Id, item.Id, relative ? current + index : index);
    }

    [RelayCommand]
    private void CopyActivePaletteAs(string? formatId)
    {
        if (_palettes.Active is not { } p || p.Colors.Count == 0)
        {
            ShowToast("La paleta activa no tiene colores");
            return;
        }
        var f = PaletteFormatters.Get(formatId ?? "css");
        Copy(f.Format(PaletteExportModel.From(p), FormatOptions), what: $"«{p.Name}» como {f.DisplayName}");
    }

    [RelayCommand(CanExecute = nameof(HasActiveColors))]
    private void ExportActivePaletteImage()
    {
        if (_palettes.Active is { Colors.Count: > 0 } p) Shell?.ShowExportImage(PaletteExportModel.From(p));
    }

    [RelayCommand]
    private void ClearHistory() => _history.Clear();
}
