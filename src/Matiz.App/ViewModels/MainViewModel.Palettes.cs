using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.App.Localization;
using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Formatting;
using Matiz.Core.Generation;
using Matiz.Core.Palettes;
using Matiz.Core.Session;
using Microsoft.Win32;

namespace Matiz.App.ViewModels;

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial bool IsLibraryOpen { get; set; }
    [ObservableProperty] public partial string ActivePaletteName { get; private set; } = "";
    [ObservableProperty] public partial bool HasActiveColors { get; private set; }
    [ObservableProperty] public partial PaletteItem? SelectedPalette { get; set; }
    [ObservableProperty] public partial int GrayScalePercent { get; set; }
    [ObservableProperty] public partial bool HasGrayScale { get; private set; }

    partial void OnGrayScalePercentChanged(int value)
    {
        HasGrayScale = value > 0;
        var amount = value / 100.0;
        foreach (var item in ActivePaletteColors) item.ApplyGrayMix(amount);

        // Persiste el % recordado de la paleta activa; el Changed resultante solo autoguarda
        // (_applyingGray evita reconstruir los ítems en cada tick: ya están refrescados arriba).
        if (_palettes.Active is not { } p || (p.GrayPercent ?? 0) == value) return;
        _applyingGray = true;
        try { _palettes.SetGrayPercent(p.Id, value); }
        finally { _applyingGray = false; }
    }

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
            ActivePaletteName = active?.Name ?? Loc.T("palette.none");

            ActivePaletteColors.Clear();
            if (active is not null)
                foreach (var c in active.Colors) ActivePaletteColors.Add(new PaletteColorItem(c, OnPaletteColorRenamed));
            HasActiveColors = ActivePaletteColors.Count > 0;
            // Restauración del % recordado por la paleta (única ruta: arranque, cambio de marcada, import, undo).
            var grayPercent = active?.GrayPercent ?? 0;
            if (GrayScalePercent != grayPercent)
                GrayScalePercent = grayPercent; // OnGrayScalePercentChanged aplica la mezcla y sincroniza la persistencia
            else if (GrayScalePercent > 0)
                foreach (var item in ActivePaletteColors) item.ApplyGrayMix(GrayScalePercent / 100.0);
            ExportActivePaletteImageCommand.NotifyCanExecuteChanged();
            ExportActivePaletteOverlayCommand.NotifyCanExecuteChanged();
            ExportPaletteCommand.NotifyCanExecuteChanged();
            ReloadPaletteToFreeCommand.NotifyCanExecuteChanged();
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
        if (_palettes.AddColor(p.Id, Session.Current.Argb) is null) { ShowToast(Loc.T("toasts.maxPaletteColors")); return; }
        ShowToast(Loc.F("toasts.addedToPalette", Session.Current.Argb, p.Name));
    }

    [RelayCommand]
    private void NewPalette()
    {
        _palettes.Create();
        IsSettingsOpen = false;
        IsLibraryOpen = true;
    }

    /// <summary>Recargar: carga la paleta marcada en la rueda cromática (modo Libre) tras advertir que
    /// se perderán las selecciones actuales. Deshabilitado sin paleta marcada con colores.</summary>
    [RelayCommand(CanExecute = nameof(HasActiveColors))]
    private void ReloadPaletteToFree() =>
        ShowToast(Loc.T("toasts.reloadPaletteWarning"), Loc.T("library.reload"), LoadPaletteToFree, seconds: 5);

    /// <summary>Confirmación del "Recargar": evalúa la paleta marcada al confirmar (si se marcó otra entre
    /// aviso y confirmación, carga esa); primer color = principal/color actual, resto = secundarios con su
    /// brillo propio (cada tarjeta reproduce el color exacto). Reemplaza el conjunto libre previo, cierra
    /// la Biblioteca y no modifica la paleta (ni su % de grises).</summary>
    private void LoadPaletteToFree()
    {
        if (_palettes.Active is not { Colors.Count: > 0 } p) return;
        // La rueda carga la vista vigente: mismos colores mezclados (MixToGray) que muestran las muestras,
        // no los crudos. La paleta conserva su % de grises tal cual. A 0 % la mezcla es identidad.
        var amount = GrayScalePercent / 100.0;
        Session.Commit(ColorMath.MixToGray(p.Colors[0].Color, amount), ColorChangeSource.Palette);
        var st = Session.Current;
        _freeOffsets.Clear();
        foreach (var c in p.Colors.Skip(1))
        {
            var hsv = ColorMath.ToHsv(ColorMath.MixToGray(c.Color, amount));
            _freeOffsets.Add((
                ColorMath.NormalizeHue(hsv.H - st.Hue),
                Math.Clamp(hsv.S, 0, 1) - st.Saturation,
                hsv.V - st.Value));
        }
        if (GeneratedTab == GeneratedTab.Free) RefreshGenerated();
        else GeneratedTab = GeneratedTab.Free; // dispara RefreshGenerated
        IsSettingsOpen = false;
        IsLibraryOpen = false;
        if (IsImageMode) IsImageMode = false; // para que se vea la rueda
        NotifyFreePointCommands();
        ShowToast(Loc.F("toasts.paletteLoaded", p.Name));
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
        ShowToast(Loc.F("toasts.paletteDeleted", removed.Palette.Name), Loc.T("common.undo"), () => _palettes.Restore(removed.Palette, removed.Index));
    }

    /// <summary>Exporta la paleta marcada a un archivo .mpalette (JSON). Deshabilitado sin colores.</summary>
    [RelayCommand(CanExecute = nameof(HasActiveColors))]
    private void ExportPalette()
    {
        if (_palettes.Active is not { Colors.Count: > 0 } p) return;
        var name = string.Concat(p.Name.Split(Path.GetInvalidFileNameChars())).Trim();
        var dlg = new SaveFileDialog
        {
            Filter = Loc.T("dialogs.paletteFilter"),
            FileName = (string.IsNullOrWhiteSpace(name) ? Loc.T("library.export.defaultFileName") : name) + ".mpalette",
            Title = Loc.T("dialogs.exportPalette.title"),
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                PaletteFile.From(p).Write(dlg.FileName);
                ShowToast(Loc.F("toasts.paletteExported", p.Name));
            }
            catch (Exception ex)
            {
                ShowFileError(ex.Message);
            }
        }
    }

    /// <summary>Importa un archivo .mpalette como paleta nueva (UniqueName resuelve el nombre repetido).</summary>
    [RelayCommand]
    private void ImportPalette()
    {
        var dlg = new OpenFileDialog { Filter = Loc.T("dialogs.paletteFilter"), Title = Loc.T("dialogs.importPalette.title") };
        if (dlg.ShowDialog() == true) ImportPaletteFile(dlg.FileName);
    }

    /// <summary>Importa un archivo .mpalette siempre como paleta nueva; errores (json roto, versión futura)
    /// avisan con toast sin alterar la biblioteca. Usado también por la apertura por doble click del SO.</summary>
    public void ImportPaletteFile(string path)
    {
        try
        {
            var imported = _palettes.Import(PaletteFile.Read(path));
            ShowToast(Loc.F("toasts.paletteImported", imported.Name));
        }
        catch (Exception ex)
        {
            ShowFileError(ex.Message);
        }
    }

    private void ShowFileError(string message) => ShowToast(Loc.F("toasts.paletteFileError", message));

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
        ShowToast(Loc.F("toasts.colorDeleted", item.Hex), Loc.T("common.undo"), () =>
        {
            if (_palettes.Find(p.Id) is not { } target) return;
            var added = _palettes.AddColor(target.Id, model.Color, model.Name);
            if (added is not null) _palettes.MoveColor(target.Id, added.Id, index);
            else ShowToast(Loc.T("toasts.maxPaletteColors")); // la paleta llegó al límite: no se puede restaurar
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

    /// <summary>Modelo de export de la paleta activa conforme al modo de vista en grises vigente (0 % = intacto).</summary>
    private PaletteExportModel ActiveExport(Palette p) =>
        PaletteExportModel.From(p).WithGrayMix(GrayScalePercent / 100.0);

    [RelayCommand]
    private void CopyActivePaletteAs(string? formatId)
    {
        if (_palettes.Active is not { } p || p.Colors.Count == 0)
        {
            ShowToast(Loc.T("toasts.noColors"));
            return;
        }
        var f = PaletteFormatters.Get(formatId ?? "css");
        Copy(f.Format(ActiveExport(p), FormatOptions), what: Loc.F("toasts.paletteAsFormat", p.Name, f.DisplayName));
    }

    [RelayCommand(CanExecute = nameof(HasActiveColors))]
    private void ExportActivePaletteImage()
    {
        if (_palettes.Active is { Colors.Count: > 0 } p) Shell?.ShowExportImage(ActiveExport(p));
    }

    [RelayCommand(CanExecute = nameof(HasActiveColors))]
    private void ExportActivePaletteOverlay()
    {
        if (_palettes.Active is { Colors.Count: > 0 } p) Shell?.ShowExportOverlay(ActiveExport(p));
    }

    [RelayCommand]
    private void ResetGrayScale() => GrayScalePercent = 0;

    [RelayCommand]
    private void ClearHistory() => _history.Clear();
}
