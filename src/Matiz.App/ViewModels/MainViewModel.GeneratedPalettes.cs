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
    Extracted,
}

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial GeneratedTab GeneratedTab { get; set; } = GeneratedTab.Scale;
    [ObservableProperty] public partial HarmonyKind HarmonyKind { get; set; } = HarmonyKind.Complementary;
    [ObservableProperty] public partial bool HasExtracted { get; private set; }

    /// <summary>Puntos de la armonía que se dibujan en la rueda (vacío fuera de la pestaña Armonías).</summary>
    [ObservableProperty] public partial IReadOnlyList<WheelMarker> WheelMarkers { get; private set; } = [];

    /// <summary>Índice en <see cref="GeneratedColors"/> del color de armonía seleccionado en la rueda (-1 ninguno).</summary>
    private int _selectedHarmonyIndex = -1;

    /// <summary>Desfases personalizados de los puntos de armonía (Δhue°/Δsat del canónico), por índice generado; null = armonía canónica.</summary>
    private (double HueDelta, double SatDelta)[]? _harmonyOffsets;

    /// <summary>Indica si hay desfases personalizados activos: solo entonces se muestra el botón "Restaurar armonía".</summary>
    [ObservableProperty] public partial bool HasHarmonyOffsets { get; private set; }

    private void ClearHarmonyOffsets()
    {
        _harmonyOffsets = null;
        HasHarmonyOffsets = false;
    }

    public ObservableCollection<SwatchItem> GeneratedColors { get; } = [];

    [ObservableProperty] public partial IReadOnlyList<HarmonyOption> HarmonyKinds { get; private set; } =
        Enum.GetValues<HarmonyKind>().Select(k => new HarmonyOption(k, PaletteGenerator.HarmonyName(k))).ToList();

    public IReadOnlyList<IPaletteFormatter> PaletteFormats => PaletteFormatters.All;

    private IReadOnlyList<GeneratedColor> _extracted = [];

    partial void OnGeneratedTabChanged(GeneratedTab value)
    {
        _selectedHarmonyIndex = -1;
        RefreshGenerated();
    }

    partial void OnHarmonyKindChanged(HarmonyKind value)
    {
        _selectedHarmonyIndex = -1;
        ClearHarmonyOffsets();
        RefreshGenerated();
    }

    private void RefreshGenerated()
    {
        var c = Session.Current.Argb;
        var anchor = ScaleAnchorAuto ? ScaleAnchorMode.Automatic : ScaleAnchorMode.Fixed500;
        IReadOnlyList<GeneratedColor> list = GeneratedTab switch
        {
            GeneratedTab.Scale => DesignScale.Generate(c, anchor),
            GeneratedTab.Harmony => PaletteGenerator.Harmony(Session.Current, HarmonyKind, anchor, HarmonyBalanceLightness, _harmonyOffsets),
            GeneratedTab.TintsShades => PaletteGenerator.TintsAndShades(c),
            GeneratedTab.Neutrals => PaletteGenerator.Neutrals(c),
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
        RefreshWheelMarkers();
    }

    private void RefreshWheelMarkers()
    {
        if (_selectedHarmonyIndex >= GeneratedColors.Count) _selectedHarmonyIndex = -1;
        for (var i = 0; i < GeneratedColors.Count; i++)
            GeneratedColors[i].IsSelected = GeneratedTab == GeneratedTab.Harmony && i == _selectedHarmonyIndex;

        if (GeneratedTab != GeneratedTab.Harmony)
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
        ShowToast($"{PaletteGenerator.HarmonyName(HarmonyKind)} {s.Label}  ·  {text}", Loc.T("common.copy"), () => Copy(text, s.Color), seconds: 6);
    }

    /// <summary>Arrastre de un punto secundario en la rueda: fija su desfase personalizado (hue/sat de la rueda) sin tocar el color actual.</summary>
    [RelayCommand]
    private void SetWheelMarkerOffset(WheelMarkerDrag? drag)
    {
        if (drag is null || HarmonyKind == HarmonyKind.Monochromatic) return;
        if (drag.MarkerIndex < 0 || drag.MarkerIndex >= _markerToSwatch.Count) return;
        var angles = PaletteGenerator.HarmonyOffsets(HarmonyKind);
        var swatch = _markerToSwatch[drag.MarkerIndex];
        if (swatch >= angles.Count || angles[swatch] == 0) return;
        if (double.IsNaN(drag.Hue)) return;
        var hue = ColorMath.NormalizeHue(drag.Hue);
        var sat = Math.Clamp(drag.Saturation, 0, 1);
        var st = Session.Current;
        _harmonyOffsets ??= new (double HueDelta, double SatDelta)[angles.Count];
        _harmonyOffsets[swatch] = (
            ColorMath.NormalizeHue(hue - ColorMath.NormalizeHue(st.Hue + angles[swatch])),
            sat - st.Saturation);
        HasHarmonyOffsets = _harmonyOffsets.Any(o => o != default);
        RefreshGenerated();
    }

    /// <summary>Doble click en un punto de la rueda: reinicia ese punto a su desfase canónico sin cambiar el color actual.</summary>
    [RelayCommand]
    private void ResetWheelMarkerOffset(int markerIndex)
    {
        if (_harmonyOffsets is null || markerIndex < 0 || markerIndex >= _markerToSwatch.Count) return;
        var angles = PaletteGenerator.HarmonyOffsets(HarmonyKind);
        var swatch = _markerToSwatch[markerIndex];
        if (swatch >= angles.Count || angles[swatch] == 0) return;
        if (_harmonyOffsets[swatch] == default) return;
        _harmonyOffsets[swatch] = default;
        if (_harmonyOffsets.All(o => o == default)) ClearHarmonyOffsets();
        RefreshGenerated();
    }

    /// <summary>Botón "Restaurar armonía": restablece todos los puntos a la armonía canónica (no es deshacible).</summary>
    [RelayCommand]
    private void ResetHarmonyOffsets()
    {
        if (!HasHarmonyOffsets) return;
        ClearHarmonyOffsets();
        RefreshGenerated();
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
        _palettes.AddColor(p.Id, s.Color, index >= 0 ? GeneratedName(s, index) : null);
        ShowToast(Loc.F("toasts.addedToPaletteSimple", p.Name));
    }

    [RelayCommand]
    private void AddAllGenerated()
    {
        if (GeneratedColors.Count == 0) return;
        var p = _palettes.EnsureActive();
        _palettes.AddColors(p.Id, GeneratedColors.Select((s, i) => (s.Color, (string?)GeneratedName(s, i))).ToList());
        ShowToast(Loc.F("toasts.addedCount", GeneratedColors.Count, p.Name));
    }

    [RelayCommand]
    private void CopyGeneratedAs(string? formatId)
    {
        if (GeneratedColors.Count == 0) return;
        var f = PaletteFormatters.Get(formatId ?? "css");
        Copy(f.Format(GeneratedExportModel(), FormatOptions), what: Loc.F("toasts.asFormat", f.DisplayName));
    }
}
