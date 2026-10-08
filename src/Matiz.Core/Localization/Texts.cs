using Matiz.Core.Generation;
using Matiz.Core.Palettes;

namespace Matiz.Core.Localization;

/// <summary>
/// Textos generados por el modelo (etiquetas de paletas, nombres por defecto…).
/// La raíz de composición (Matiz.App) lo inicializa con su catálogo localizable; por defecto, inglés.
/// </summary>
public interface ITexts
{
    string HarmonyName(HarmonyKind kind);
    string Tint(int percent);
    string Shade(int percent);
    string Base { get; }
    string ColorNumberFallback(int index);
    string UntitledPalette { get; }
    string CopySuffix { get; }
}

public static class Texts
{
    public static ITexts Current { get; set; } = new EnglishTexts();
}

/// <summary>Catálogo por defecto (inglés), usado cuando la app no inicializa ninguno (tests, uso directo de Core).</summary>
public sealed class EnglishTexts : ITexts
{
    public string HarmonyName(HarmonyKind kind) => kind switch
    {
        HarmonyKind.Complementary => "Complementary",
        HarmonyKind.Analogous => "Analogous",
        HarmonyKind.SplitComplementary => "Split-complementary",
        HarmonyKind.Triadic => "Triadic",
        HarmonyKind.Tetradic => "Tetradic",
        HarmonyKind.Monochromatic => "Monochromatic",
        _ => kind.ToString(),
    };

    public string Tint(int percent) => $"Tint {percent}%";
    public string Shade(int percent) => $"Shade {percent}%";
    public string Base => "Base";
    public string ColorNumberFallback(int index) => $"Color {index}";
    public string UntitledPalette => "Untitled palette";
    public string CopySuffix => "(copy)";
}