using Matiz.Core.Generation;
using Matiz.Core.Localization;

namespace Matiz.App.Localization;

/// <summary>Catálogo ITexts (Matiz.Core) apoyado en el servicio localizable: resuelve en caliente al idioma activo.</summary>
public sealed class LocalizationTexts : ITexts
{
    public static LocalizationTexts Instance { get; } = new();

    public string HarmonyName(HarmonyKind kind) => kind switch
    {
        HarmonyKind.Complementary => Loc.T("harmony.complementary"),
        HarmonyKind.Analogous => Loc.T("harmony.analogous"),
        HarmonyKind.SplitComplementary => Loc.T("harmony.split"),
        HarmonyKind.Triadic => Loc.T("harmony.triadic"),
        HarmonyKind.Tetradic => Loc.T("harmony.tetradic"),
        HarmonyKind.Monochromatic => Loc.T("harmony.monochromatic"),
        _ => kind.ToString(),
    };

    public string Tint(int percent) => Loc.F("harmony.tint", percent);
    public string Shade(int percent) => Loc.F("harmony.shade", percent);
    public string Base => Loc.T("harmony.base");
    public string ColorNumberFallback(int index) => Loc.F("harmony.colorFallback", index);
    public string UntitledPalette => Loc.T("palette.untitled");
    public string CopySuffix => Loc.T("palette.copySuffix");
}