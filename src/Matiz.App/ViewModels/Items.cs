using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Matiz.App.Localization;
using Matiz.Core.Colors;
using Matiz.Core.Formatting;
using Matiz.Core.Palettes;

namespace Matiz.App.ViewModels;

internal static class BrushCache
{
    public static SolidColorBrush Of(Argb c)
    {
        var b = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        b.Freeze();
        return b;
    }

    /// <summary>Texto legible (negro o blanco) sobre el color dado.</summary>
    public static SolidColorBrush ContrastOf(Argb c) =>
        ColorMath.ToOklab(c).L > 0.66 || c.A < 110 ? Dark : Light;

    private static readonly SolidColorBrush Dark = Freeze(new SolidColorBrush(Color.FromRgb(20, 20, 20)));
    private static readonly SolidColorBrush Light = Freeze(new SolidColorBrush(Colors.White));

    private static SolidColorBrush Freeze(SolidColorBrush b)
    {
        b.Freeze();
        return b;
    }
}

/// <summary>Muestra de color (tarjetas generadas, historial).</summary>
public sealed partial class SwatchItem : ObservableObject
{
    public SwatchItem(Argb color, string label = "", bool isBase = false) => Set(color, label, isBase);

    [ObservableProperty] public partial Argb Color { get; private set; }
    [ObservableProperty] public partial string Label { get; private set; } = "";
    [ObservableProperty] public partial bool IsBase { get; private set; }
    [ObservableProperty] public partial SolidColorBrush Brush { get; private set; } = Brushes.Transparent;
    [ObservableProperty] public partial SolidColorBrush Foreground { get; private set; } = Brushes.Black;
    [ObservableProperty] public partial string Hex { get; private set; } = "";
    [ObservableProperty] public partial string Rgb { get; private set; } = "";
    [ObservableProperty] public partial bool IsSelected { get; set; }

    /// <summary>Coordenadas exactas en la rueda (armonías); null si no aplica.</summary>
    public double? WheelHue { get; set; }
    public double? WheelSaturation { get; set; }

    public void Set(Argb color, string label, bool isBase)
    {
        Label = label;
        IsBase = isBase;
        if (Color == color && Hex.Length > 0) return;
        Color = color;
        Brush = BrushCache.Of(color);
        Foreground = BrushCache.ContrastOf(color);
        Hex = color.ToString();
        Rgb = $"RGB {ColorFormats.Rgb(color)}";
    }
}

/// <summary>Fila del panel de formatos del color actual.</summary>
public sealed partial class FormatRow(string id, string label, string? tooltipKey = null) : ObservableObject
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public string? Tooltip => tooltipKey is null ? null : Loc.T(tooltipKey);
    public string CopyAutomation => Loc.F("row.copyAutomation", Label);
    [ObservableProperty] public partial string Value { get; set; } = "";

    public void RefreshTexts()
    {
        OnPropertyChanged(nameof(Tooltip));
        OnPropertyChanged(nameof(CopyAutomation));
    }
}

/// <summary>Color de la paleta activa. Renombrar escribe directamente en el servicio.</summary>
public sealed partial class PaletteColorItem : ObservableObject
{
    private readonly Action<PaletteColorItem, string?> _rename;

    public PaletteColorItem(PaletteColor model, Action<PaletteColorItem, string?> rename)
    {
        _rename = rename;
        Id = model.Id;
        Color = model.Color;
        _name = model.Name ?? "";
        Brush = BrushCache.Of(Color);
        Foreground = BrushCache.ContrastOf(Color);
        DisplayBrush = Brush;
        DisplayForeground = Foreground;
        DisplayHex = Hex;
    }

    private string _name;

    public Guid Id { get; }
    public Argb Color { get; }
    public SolidColorBrush Brush { get; }
    public SolidColorBrush Foreground { get; }
    public string Hex => Color.ToString();

    [ObservableProperty] public partial SolidColorBrush DisplayBrush { get; private set; } = Brushes.Transparent;
    [ObservableProperty] public partial SolidColorBrush DisplayForeground { get; private set; } = Brushes.Black;
    [ObservableProperty] public partial string DisplayHex { get; private set; } = "";

    /// <summary>Recalcula lo mostrado con el modo de vista en grises (0 = color original). No altera el color almacenado.</summary>
    public void ApplyGrayMix(double amount)
    {
        var c = ColorMath.MixToGray(Color, amount);
        DisplayBrush = BrushCache.Of(c);
        DisplayForeground = BrushCache.ContrastOf(c);
        DisplayHex = c.ToString();
    }

    public string Name
    {
        get => _name;
        set
        {
            var v = value?.Trim() ?? "";
            if (v == _name) return;
            _name = v;
            OnPropertyChanged();
            _rename(this, v);
        }
    }
}

/// <summary>Paleta en la biblioteca.</summary>
public sealed partial class PaletteItem : ObservableObject
{
    private readonly Action<PaletteItem> _changed;

    public PaletteItem(Palette model, Action<PaletteItem> changed)
    {
        Model = model;
        _changed = changed;
        Refresh();
    }

    public Palette Model { get; }
    public Guid Id => Model.Id;

    [ObservableProperty] public partial IReadOnlyList<SolidColorBrush> Preview { get; private set; } = [];
    [ObservableProperty] public partial string Summary { get; private set; } = "";

    public string Name
    {
        get => Model.Name;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == Model.Name) { OnPropertyChanged(); return; }
            Model.Name = value.Trim();
            OnPropertyChanged();
            _changed(this);
        }
    }

    public string Description
    {
        get => Model.Description ?? "";
        set
        {
            if ((value ?? "") == (Model.Description ?? "")) return;
            Model.Description = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
            _changed(this);
        }
    }

    public void Refresh()
    {
        Preview = Model.Colors.Select(c => BrushCache.Of(c.Color)).ToList();
        var count = Model.Colors.Count;
        Summary = Loc.F(count == 1 ? "palette.summaryOne" : "palette.summaryMany", count, Model.ModifiedAt.ToLocalTime().ToString("d MMM yyyy HH:mm"));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
    }
}

/// <summary>Opción con nombre visible (formatos, enums).</summary>
public sealed record Option<T>(T Value, string Name);
