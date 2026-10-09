using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.App.Controls;
using Matiz.App.Localization;
using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Formatting;
using Matiz.Core.Generation;
using Matiz.Core.Localization;
using Matiz.Core.Session;

namespace Matiz.App.ViewModels;

public enum GeneratedTab
{
    Scale,
    Harmony,
    TintsShades,
    Neutrals,
    Free,
    Extracted,
}

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial GeneratedTab GeneratedTab { get; set; } = GeneratedTab.Scale;
    [ObservableProperty] public partial HarmonyKind HarmonyKind { get; set; } = HarmonyKind.Complementary;
    [ObservableProperty] public partial bool HasExtracted { get; private set; }

    /// <summary>Puntos de la armonía que se dibujan en la rueda (vacío fuera de Armonías y Libre).</summary>
    [ObservableProperty] public partial IReadOnlyList<WheelMarker> WheelMarkers { get; private set; } = [];

    /// <summary>Índice en <see cref="GeneratedColors"/> del punto seleccionado en la rueda (-1 ninguno).</summary>
    private int _selectedHarmonyIndex = -1;

    /// <summary>Límite de puntos secundarios que admite el modo Libre (regla global de 64 colores por paleta, v1.0.7).</summary>
    private const int MaxFreePoints = 64;

    /// <summary>Puntos del modo Libre: desfases relativos al color principal (Δhue°, Δsat y Δbrillo opcional; brillo propio solo al cargar paletas).</summary>
    private readonly List<(double HueDelta, double SatDelta, double? ValueDelta)> _freeOffsets = [];

    /// <summary>True si el conjunto libre tiene al menos 2 colores: habilita el botón "−" de las tarjetas.</summary>
    [ObservableProperty] public partial bool CanRemoveFreePoints { get; private set; }

    public ObservableCollection<SwatchItem> GeneratedColors { get; } = [];

    [ObservableProperty] public partial IReadOnlyList<HarmonyOption> HarmonyKinds { get; private set; } =
        Enum.GetValues<HarmonyKind>().Select(k => new HarmonyOption(k, PaletteGenerator.HarmonyName(k))).ToList();

    public IReadOnlyList<IPaletteFormatter> PaletteFormats => PaletteFormatters.All;

    private IReadOnlyList<GeneratedColor> _extracted = [];

    partial void OnGeneratedTabChanged(GeneratedTab value)
    {
        _selectedHarmonyIndex = -1;
        RefreshGenerated();
        AddFreePointCommand.NotifyCanExecuteChanged();
    }

    partial void OnHarmonyKindChanged(HarmonyKind value)
    {
        _selectedHarmonyIndex = -1;
        RefreshGenerated();
    }

    private void RefreshGenerated()
    {
        var c = Session.Current.Argb;
        var anchor = ScaleAnchorAuto ? ScaleAnchorMode.Automatic : ScaleAnchorMode.Fixed500;
        IReadOnlyList<GeneratedColor> list = GeneratedTab switch
        {
            GeneratedTab.Scale => DesignScale.Generate(c, anchor),
            GeneratedTab.Harmony => PaletteGenerator.Harmony(Session.Current, HarmonyKind, anchor, HarmonyBalanceLightness),
            GeneratedTab.TintsShades => PaletteGenerator.TintsAndShades(c),
            GeneratedTab.Neutrals => PaletteGenerator.Neutrals(c),
            GeneratedTab.Free => PaletteGenerator.FreePoints(Session.Current, _freeOffsets, HarmonyBalanceLightness),
            GeneratedTab.Extracted => _extracted,
            _ => [],
        };

        // Actualiza en sitio para no regenerar contenedores en cada frame de arrastre.
        if (GeneratedColors.Count != list.Count)
        {
            GeneratedColors.Clear();
            foreach (var g in list) GeneratedColors.Add(new SwatchItem(g.Color, g.Label, g.IsBase) { WheelHue = g.WheelHue, WheelSaturation = g.WheelSaturation });
        }
        else
        {
            for (var i = 0; i < list.Count; i++)
            {
                GeneratedColors[i].Set(list[i].Color, list[i].Label, list[i].IsBase);
                GeneratedColors[i].WheelHue = list[i].WheelHue;
                GeneratedColors[i].WheelSaturation = list[i].WheelSaturation;
            }
        }
        CanRemoveFreePoints = GeneratedTab == GeneratedTab.Free && GeneratedColors.Count >= 2;
        RefreshWheelMarkers();
    }

    private void RefreshWheelMarkers()
    {
        var markersVisible = GeneratedTab is GeneratedTab.Harmony or GeneratedTab.Free;
        if (_selectedHarmonyIndex >= GeneratedColors.Count) _selectedHarmonyIndex = -1;
        for (var i = 0; i < GeneratedColors.Count; i++)
            GeneratedColors[i].IsSelected = markersVisible && i == _selectedHarmonyIndex;

        if (!markersVisible)
        {
            if (WheelMarkers.Count > 0) WheelMarkers = [];
            return;
        }
        // Posición en la rueda (HSV) de cada color de la armonía, salvo el base (es el marcador principal).
        var markers = new List<WheelMarker>();
        _markerToSwatch.Clear();
        for (var i = 0; i < GeneratedColors.Count; i++)
        {
            var s = GeneratedColors[i];
            if (s.IsBase) continue;
            // Coordenadas exactas de la armonía (no re-derivadas del HEX redondeado): la figura es geométrica.
            var hsv = ColorMath.ToHsv(s.Color);
            markers.Add(new WheelMarker(s.WheelHue ?? hsv.H, s.WheelSaturation ?? hsv.S, s.Color, i == _selectedHarmonyIndex));
            _markerToSwatch.Add(i);
        }
        WheelMarkers = markers;
    }

    private readonly List<int> _markerToSwatch = [];

    /// <summary>Click en un punto de la rueda: lo selecciona y ofrece copiarlo sin cambiar el color actual.</summary>
    [RelayCommand]
    private void SelectWheelMarker(int markerIndex)
    {
        if (markerIndex < 0 || markerIndex >= _markerToSwatch.Count) return;
        _selectedHarmonyIndex = _markerToSwatch[markerIndex];
        RefreshWheelMarkers();
        var s = GeneratedColors[_selectedHarmonyIndex];
        var text = ColorFormatters.Get(_settings.DefaultFormatId).Format(s.Color, FormatOptions);
        var title = GeneratedTab == GeneratedTab.Harmony ? $"{PaletteGenerator.HarmonyName(HarmonyKind)} {s.Label}" : s.Label;
        ShowToast($"{title}  ·  {text}", Loc.T("common.copy"), () => Copy(text, s.Color), seconds: 6);
    }

    /// <summary>
    /// Conversión Armonías → Libre: el conjunto libre se define desde la armonía canónica actual (un punto por
    /// cada color de la armonía en su ángulo, Δsat 0; en Monocromática solo el principal) y reemplaza el conjunto
    /// previo. El punto interactuado toma el desfase de la posición dada.
    /// </summary>
    private void ConvertHarmonyToFree(WheelMarkerDrag? drag)
    {
        List<(double HueDelta, double SatDelta, double? ValueDelta)> offsets = [];
        if (HarmonyKind != HarmonyKind.Monochromatic)
            offsets.AddRange(PaletteGenerator.HarmonyOffsets(HarmonyKind).Where(a => a != 0).Select(a => ((double)a, 0.0, (double?)null)));
        if (drag is { } d)
        {
            var st = Session.Current;
            var delta = (ColorMath.NormalizeHue(d.Hue - st.Hue), Math.Clamp(d.Saturation, 0, 1) - st.Saturation, (double?)null);
            var idx = Math.Min(d.MarkerIndex, offsets.Count); // el marcador i-ésimo ↔ el i-ésimo punto no base
            if (idx == offsets.Count) offsets.Add(delta); else offsets[idx] = delta;
        }
        _freeOffsets.Clear();
        _freeOffsets.AddRange(offsets);
        GeneratedTab = GeneratedTab.Free;
        NotifyFreePointCommands();
    }

    /// <summary>
    /// Arrastre de un punto en la rueda: en Armonías pasa a Libre (conversión) conservando el orden de marcadores;
    /// en Libre actualiza el desfase relativo del punto. No toca el color actual.
    /// </summary>
    [RelayCommand]
    private void SetWheelMarkerOffset(WheelMarkerDrag? drag)
    {
        if (drag is null || drag.MarkerIndex < 0 || drag.MarkerIndex >= _markerToSwatch.Count) return;
        if (double.IsNaN(drag.Hue)) return;
        if (GeneratedTab == GeneratedTab.Harmony) ConvertHarmonyToFree(drag);
        if (drag.MarkerIndex >= _freeOffsets.Count) return;
        var st = Session.Current;
        // Conserva el Δbrillo propio del punto si lo tenía (cargado de una paleta).
        _freeOffsets[drag.MarkerIndex] = (
            ColorMath.NormalizeHue(drag.Hue - st.Hue),
            Math.Clamp(drag.Saturation, 0, 1) - st.Saturation,
            _freeOffsets[drag.MarkerIndex].ValueDelta);
        RefreshGenerated();
    }

    private bool CanAddFreePoint() => GeneratedTab == GeneratedTab.Harmony || _freeOffsets.Count < MaxFreePoints;

    /// <summary>Botón "+": añade un punto opuesto al principal si aún no hay secundarios y, si los hay, al lado del último; en Armonías además convierte la armonía a Libre.</summary>
    [RelayCommand(CanExecute = nameof(CanAddFreePoint))]
    private void AddFreePoint()
    {
        if (GeneratedTab == GeneratedTab.Harmony) ConvertHarmonyToFree(null);
        else if (GeneratedTab != GeneratedTab.Free) return;
        if (_freeOffsets.Count < MaxFreePoints) _freeOffsets.Add(NextAddPointPosition());
        RefreshGenerated();
        NotifyFreePointCommands();
    }

    /// <summary>
    /// Posición del punto que añade el botón "+": opuesto al principal si el conjunto no tiene aún
    /// secundarios; en caso contrario, al lado del último añadido (hue +30° por paso, conservando su
    /// saturación) hasta no solaparse con ningún punto existente ni con el principal.
    /// </summary>
    private (double HueDelta, double SatDelta, double? ValueDelta) NextAddPointPosition()
    {
        if (_freeOffsets.Count == 0) return (180.0, 0.0, null);
        var (hue, sat, _) = _freeOffsets[^1];
        for (var i = 0; i < 12; i++)
        {
            hue = ColorMath.NormalizeHue(hue + 30.0);
            if (IsFreePosition(hue)) return (hue, sat, null);
        }
        return (ColorMath.NormalizeHue(hue + 30.0), sat, null);
    }

    private bool IsFreePosition(double hueDelta)
    {
        if (AngularDistance(hueDelta, 0) < 10.0) return false;
        foreach (var o in _freeOffsets)
            if (AngularDistance(o.HueDelta, hueDelta) < 10.0) return false;
        return true;
    }

    private static double AngularDistance(double a, double b)
    {
        var d = Math.Abs(ColorMath.NormalizeHue(a - b));
        return d > 180 ? 360 - d : d;
    }

    /// <summary>Click derecho en la rueda: añade un punto libre en esa posición desde cualquier pestaña y pasa a Libre; al límite advierte y no añade.</summary>
    [RelayCommand]
    private void AddFreePointAt(WheelPoint? point)
    {
        if (point is null || double.IsNaN(point.Hue)) return;
        if (_freeOffsets.Count >= MaxFreePoints)
        {
            ShowToast(Loc.T("toasts.maxPaletteColors"));
            return;
        }
        if (GeneratedTab == GeneratedTab.Harmony) ConvertHarmonyToFree(null);
        var st = Session.Current;
        _freeOffsets.Add((ColorMath.NormalizeHue(point.Hue - st.Hue), Math.Clamp(point.Saturation, 0, 1) - st.Saturation, (double?)null));
        if (GeneratedTab != GeneratedTab.Free)
        {
            GeneratedTab = GeneratedTab.Free; // dispara RefreshGenerated
        }
        else RefreshGenerated();
        NotifyFreePointCommands();
    }

    /// <summary>
    /// Botón "−" de una tarjeta en Libre: quita ese color del conjunto. Quitar el principal promueve al primer
    /// secundario como cambio confirmado, con los demás en sus posiciones absolutas (no es deshacible).
    /// </summary>
    [RelayCommand]
    private void RemoveFreePoint(SwatchItem? s)
    {
        if (GeneratedTab != GeneratedTab.Free || s is null) return;
        var idx = GeneratedColors.IndexOf(s);
        if (idx < 0 || (idx == 0 && _freeOffsets.Count == 0)) return;
        if (idx == 0)
        {
            var k = _freeOffsets[0];
            var promoted = GeneratedColors[1].Color;
            // Con Δbrillos explícitos (paleta cargada): recalcula Δv desde los colores generados reales
            // para que los puntos sigan mostrando su color absoluto tras la promoción.
            var promotedV = ColorMath.ToHsv(promoted).V;
            for (var i = 1; i < _freeOffsets.Count; i++)
                _freeOffsets[i] = (
                    ColorMath.NormalizeHue(_freeOffsets[i].HueDelta - k.HueDelta),
                    _freeOffsets[i].SatDelta - k.SatDelta,
                    _freeOffsets[i].ValueDelta is { } ? (double?)Math.Clamp(ColorMath.ToHsv(GeneratedColors[i + 1].Color).V - promotedV, -1, 1) : null);
            _freeOffsets.RemoveAt(0);
            Session.Commit(promoted, ColorChangeSource.Generated); // refresca el panel vía Session.Changed
        }
        else
        {
            _freeOffsets.RemoveAt(idx - 1);
            RefreshGenerated();
        }
        NotifyFreePointCommands();
    }

    /// <summary>
    /// Doble click sobre un punto secundario en la rueda: elimina ese punto del conjunto. Con la pestaña
    /// Armonías activa convierte la armonía a Libre (base + puntos canónicos) y elimina el punto indicado;
    /// en Libre quita ese punto. No toca el color actual.
    /// </summary>
    [RelayCommand]
    private void RemoveWheelMarker(int index)
    {
        if (index < 0) return;
        if (GeneratedTab == GeneratedTab.Harmony)
        {
            var offsets = new List<(double HueDelta, double SatDelta, double? ValueDelta)>();
            if (HarmonyKind != HarmonyKind.Monochromatic)
                offsets.AddRange(PaletteGenerator.HarmonyOffsets(HarmonyKind).Where(a => a != 0).Select(a => ((double)a, 0.0, (double?)null)));
            if (index < offsets.Count) offsets.RemoveAt(index);
            _freeOffsets.Clear();
            _freeOffsets.AddRange(offsets);
            GeneratedTab = GeneratedTab.Free;
            NotifyFreePointCommands();
            return;
        }
        if (GeneratedTab != GeneratedTab.Free || index >= _freeOffsets.Count) return;
        _freeOffsets.RemoveAt(index);
        RefreshGenerated();
        NotifyFreePointCommands();
    }

    /// <summary>
    /// Botón "Borrar" junto al "+" en Libre: quita de una vez todos los puntos secundarios conservando
    /// únicamente el principal. No cambia el color actual ni es deshacible.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanClearFreePoints))]
    private void ClearFreePoints()
    {
        if (_freeOffsets.Count == 0) return;
        _freeOffsets.Clear();
        RefreshGenerated();
        NotifyFreePointCommands();
    }

    private bool CanClearFreePoints() => GeneratedTab == GeneratedTab.Free && _freeOffsets.Count > 0;

    private void NotifyFreePointCommands()
    {
        AddFreePointCommand.NotifyCanExecuteChanged();
        ClearFreePointsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Nombres de los colores generados al agregarlos a una paleta o exportarlos.</summary>
    private string GeneratedName(SwatchItem s, int index)
    {
        var prefix = string.IsNullOrWhiteSpace(ScalePrefix) ? "Color" : ScalePrefix.Trim();
        return GeneratedTab switch
        {
            GeneratedTab.Neutrals => $"Neutral {s.Label}",
            GeneratedTab.Extracted => Loc.F("gen.imageColorName", index + 1),
            _ when s.Label == Texts.Current.Base => prefix,
            _ => $"{prefix} {s.Label}",
        };
    }

    private PaletteExportModel GeneratedExportModel()
    {
        var title = GeneratedTab switch
        {
            GeneratedTab.Scale => ScalePrefix,
            GeneratedTab.Harmony => PaletteGenerator.HarmonyName(HarmonyKind),
            GeneratedTab.TintsShades => Loc.T("gen.exportTitle.tints"),
            GeneratedTab.Neutrals => "Neutral",
            GeneratedTab.Free => Loc.T("gen.exportTitle.free"),
            _ => Loc.T("gen.exportTitle.imageColors"),
        };
        return new PaletteExportModel(title, GeneratedColors.Select((s, i) => new PaletteExportColor(GeneratedName(s, i), s.Color)).ToList());
    }

    [RelayCommand]
    private void UseSwatch(SwatchItem? s)
    {
        if (s is null) return;
        var source = GeneratedColors.Contains(s) ? ColorChangeSource.Generated : ColorChangeSource.History;
        Session.Commit(s.Color, source);
    }

    [RelayCommand]
    private void CopySwatchHex(SwatchItem? s)
    {
        if (s is not null) Copy(ColorFormats.Hex(s.Color, FormatOptions), s.Color);
    }

    [RelayCommand]
    private void CopySwatchRgb(SwatchItem? s)
    {
        if (s is not null) Copy(ColorFormats.Rgb(s.Color), s.Color);
    }

    [RelayCommand]
    private void CopySwatchDefault(SwatchItem? s)
    {
        if (s is not null) CopyDefault(s.Color);
    }

    [RelayCommand]
    private void AddSwatchToPalette(SwatchItem? s)
    {
        if (s is null) return;
        var p = _palettes.EnsureActive();
        var index = GeneratedColors.IndexOf(s);
        var added = _palettes.AddColor(p.Id, s.Color, index >= 0 ? GeneratedName(s, index) : null);
        if (added is null) { ShowToast(Loc.T("toasts.maxPaletteColors")); return; }
        ShowToast(Loc.F("toasts.addedToPaletteSimple", p.Name));
    }

    [RelayCommand]
    private void AddAllGenerated()
    {
        if (GeneratedColors.Count == 0) return;
        var p = _palettes.EnsureActive();
        var added = _palettes.AddColors(p.Id, GeneratedColors.Select((s, i) => (s.Color, (string?)GeneratedName(s, i))).ToList());
        if (added == 0) { ShowToast(Loc.T("toasts.maxPaletteColors")); return; } // todo-o-nada
        ShowToast(Loc.F("toasts.addedCount", added, p.Name));
    }

    [RelayCommand]
    private void CopyGeneratedAs(string? formatId)
    {
        if (GeneratedColors.Count == 0) return;
        var f = PaletteFormatters.Get(formatId ?? "css");
        Copy(f.Format(GeneratedExportModel(), FormatOptions), what: Loc.F("toasts.asFormat", f.DisplayName));
    }
}
