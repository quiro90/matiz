using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Matiz.App.Localization;
using Matiz.Core.Colors;
using Matiz.Core.Formatting;
using Matiz.Core.Parsing;
using Matiz.Core.Session;

namespace Matiz.App.ViewModels;

public sealed partial class MainViewModel
{
    [ObservableProperty] public partial SolidColorBrush CurrentBrush { get; private set; } = Brushes.Transparent;
    [ObservableProperty] public partial SolidColorBrush CurrentForeground { get; private set; } = Brushes.White;
    [ObservableProperty] public partial SolidColorBrush PreviousBrush { get; private set; } = Brushes.Transparent;
    [ObservableProperty] public partial SolidColorBrush PreviousForeground { get; private set; } = Brushes.White;
    [ObservableProperty] public partial string CurrentHex { get; private set; } = "";
    [ObservableProperty] public partial string PreviousHex { get; private set; } = "";
    [ObservableProperty] public partial bool HasAlpha { get; private set; }
    [ObservableProperty] public partial string AlphaText { get; private set; } = "";

    [ObservableProperty] public partial string HexInput { get; set; } = "";
    [ObservableProperty] public partial bool HexInvalid { get; set; }

    [ObservableProperty] public partial double RValue { get; set; }
    [ObservableProperty] public partial double GValue { get; set; }
    [ObservableProperty] public partial double BValue { get; set; }

    [ObservableProperty] public partial bool ShowMoreFormats { get; set; }

    public ObservableCollection<FormatRow> FormatRows { get; } = [];
    public ObservableCollection<FormatRow> ExtraFormatRows { get; } = [];

    public IReadOnlyList<IColorFormatter> CodeFormatters => ColorFormatters.All;

    private void InitFormatRows()
    {
        FormatRows.Add(new FormatRow("hex", "HEX"));
        FormatRows.Add(new FormatRow("rgb", "RGB"));
        FormatRows.Add(new FormatRow("hsl", "HSL"));
        FormatRows.Add(new FormatRow("hsv", "HSV"));
        FormatRows.Add(new FormatRow("cmyk", "CMYK*", "CMYK aproximado, sin perfil ICC: no apto para impresión profesional."));
        ExtraFormatRows.Add(new FormatRow("oklch", "OKLCH", "Oklab LCh (L C h)."));
        ExtraFormatRows.Add(new FormatRow("css-rgb", "CSS"));
        ExtraFormatRows.Add(new FormatRow("argb", "ARGB", "#AARRGGBB (convención XAML/Windows/Flutter)."));
        ExtraFormatRows.Add(new FormatRow("dart", "Flutter"));
        ExtraFormatRows.Add(new FormatRow("csharp-wpf", "C#"));
    }

    private void RefreshCurrentColor()
    {
        var c = Session.Current.Argb;
        var p = Session.Previous.Argb;
        var o = FormatOptions;
        CurrentBrush = BrushCache.Of(c);
        CurrentForeground = BrushCache.ContrastOf(c);
        PreviousBrush = BrushCache.Of(p);
        PreviousForeground = BrushCache.ContrastOf(p);
        CurrentHex = c.IsOpaque ? ColorFormats.Hex(c, o) : ColorFormats.ArgbHex(c, o);
        PreviousHex = p.IsOpaque ? ColorFormats.Hex(p, o) : ColorFormats.ArgbHex(p, o);
        HasAlpha = !c.IsOpaque;
        AlphaText = $"Alpha {c.A} ({ColorFormats.Pct(c.A / 255.0)}%)";
        HexInput = CurrentHex;
        HexInvalid = false;

        foreach (var row in FormatRows.Concat(ExtraFormatRows))
        {
            row.Value = row.Id switch
            {
                "hex" => ColorFormats.Hex(c, o),
                "rgb" => ColorFormats.Rgb(c),
                "hsl" => ColorFormats.Hsl(c),
                "hsv" => ColorFormats.Hsv(c),
                "cmyk" => ColorFormats.Cmyk(c),
                "oklch" => ColorFormats.Oklch(c),
                "css-rgb" => ColorFormats.CssRgb(c),
                "argb" => ColorFormats.ArgbHex(c, o),
                "dart" => ColorFormats.Dart(c),
                "csharp-wpf" => ColorFormats.CSharpWpf(c),
                _ => row.Value,
            };
        }
    }

    partial void OnRValueChanged(double value) => CommitRgb();
    partial void OnGValueChanged(double value) => CommitRgb();
    partial void OnBValueChanged(double value) => CommitRgb();

    private void CommitRgb()
    {
        if (_syncing) return;
        var cur = Session.Current.Argb;
        var c = new Argb(cur.A, (byte)Math.Clamp(RValue, 0, 255), (byte)Math.Clamp(GValue, 0, 255), (byte)Math.Clamp(BValue, 0, 255));
        if (c != cur) Session.Commit(c, ColorChangeSource.ManualInput);
    }

    /// <summary>Campo de texto principal: acepta HEX y cualquier formato soportado por el parser.</summary>
    [RelayCommand]
    private void ApplyHex(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == CurrentHex)
        {
            HexInvalid = false;
            OnPropertyChanged(nameof(HexInput));
            return;
        }
        if (ColorParser.TryParse(text, out var c))
        {
            HexInvalid = false;
            if (c != Session.Current.Argb) Session.Commit(c, ColorChangeSource.ManualInput);
            else OnPropertyChanged(nameof(HexInput)); // reescribe el texto normalizado
        }
        else
        {
            HexInvalid = true;
        }
    }

    [RelayCommand]
    private void RestorePrevious() => Session.RestorePrevious();

    [RelayCommand]
    private void CopyRow(FormatRow? row)
    {
        if (row is not null) Copy(row.Value, Session.Current.Argb);
    }

    [RelayCommand]
    private void CopyAs(string? formatterId) =>
        Copy(ColorFormatters.Get(formatterId).Format(Session.Current.Argb, FormatOptions), Session.Current.Argb);

    [RelayCommand]
    private void CopyAll() => Copy(ColorFormats.CopyAll(Session.Current.Argb, FormatOptions), Session.Current.Argb, "todos los formatos");

    [RelayCommand]
    private void CopyDefaultFormat() => CopyDefault(Session.Current.Argb);

    [RelayCommand]
    private void CopyHex() => Copy(ColorFormats.Hex(Session.Current.Argb, FormatOptions), Session.Current.Argb);

    [RelayCommand]
    private void CopyPreviousHex()
    {
        var p = Session.Previous.Argb;
        Copy(p.IsOpaque ? ColorFormats.Hex(p, FormatOptions) : ColorFormats.ArgbHex(p, FormatOptions), p);
    }

    [RelayCommand]
    private void AddCurrentColorToPalette()
    {
        var p = _palettes.EnsureActive();
        _palettes.AddColor(p.Id, Session.Current.Argb, null);
        ShowToast(Loc.F("toasts.addedToPaletteSimple", p.Name));
    }

    [RelayCommand]
    private void AddPreviousColorToPalette()
    {
        var p = _palettes.EnsureActive();
        _palettes.AddColor(p.Id, Session.Previous.Argb, null);
        ShowToast(Loc.F("toasts.addedToPaletteSimple", p.Name));
    }
}
