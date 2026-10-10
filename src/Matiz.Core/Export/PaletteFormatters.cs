using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Matiz.Core.Colors;
using Matiz.Core.Formatting;
using Matiz.Core.Localization;
using Matiz.Core.Palettes;

namespace Matiz.Core.Export;

/// <summary>Modelo común de exportación: sirve para paletas guardadas, generadas y escalas.</summary>
public sealed record PaletteExportModel(string Name, IReadOnlyList<PaletteExportColor> Colors)
{
    public static PaletteExportModel From(Palette p) =>
        new(p.Name, p.Colors.Select(c => new PaletteExportColor(c.Name, c.Color)).ToList());

    /// <summary>Mezcla cada color hacia su gris equivalente perceptual. amount 0 = modelo intacto; los nombres no cambian.</summary>
    public PaletteExportModel WithGrayMix(double amount)
    {
        if (amount <= 0) return this;
        return this with
        {
            Colors = Colors.Select(c => c with { Color = ColorMath.MixToGray(c.Color, amount) }).ToList()
        };
    }
}

public sealed record PaletteExportColor(string? Name, Argb Color);

public interface IPaletteFormatter
{
    string Id { get; }
    string DisplayName { get; }
    string Format(PaletteExportModel palette, FormatOptions options);
}

public sealed record PaletteFormatter(string Id, string DisplayName, Func<PaletteExportModel, FormatOptions, string> Formatter)
    : IPaletteFormatter
{
    public string Format(PaletteExportModel palette, FormatOptions options) => Formatter(palette, options);
}

/// <summary>Registro de formatos de exportación de paletas. Añadir uno nuevo = registrarlo aquí.</summary>
public static class PaletteFormatters
{
    private static readonly string Nl = "\n";

    public static IReadOnlyList<IPaletteFormatter> All { get; } =
    [
        new PaletteFormatter("css", "CSS variables", (p, o) => Css(p, o)),
        new PaletteFormatter("json", "JSON", (p, o) => Json(p, o)),
        new PaletteFormatter("dart", "Dart / Flutter", (p, _) => Dart(p)),
        new PaletteFormatter("csharp", "C# (WPF)", (p, _) => CSharp(p)),
        new PaletteFormatter("tailwind", "Tailwind v4 (@theme)", (p, o) => Tailwind(p, o)),
        new PaletteFormatter("hex-list", "Lista HEX", (p, o) => string.Join(Nl, p.Colors.Select(c => Hex(c.Color, o)))),
    ];

    public static IPaletteFormatter Get(string id) =>
        All.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static string Css(PaletteExportModel p, FormatOptions o)
    {
        var names = IdentifierNaming.Unique(p.Colors, IdentifierNaming.Kebab, "-");
        var sb = new StringBuilder(":root {").Append(Nl);
        for (var i = 0; i < p.Colors.Count; i++)
            sb.Append("  --").Append(names[i]).Append(": ").Append(CssValue(p.Colors[i].Color, o)).Append(';').Append(Nl);
        return sb.Append('}').ToString();
    }

    public static string Tailwind(PaletteExportModel p, FormatOptions o)
    {
        var names = IdentifierNaming.Unique(p.Colors, IdentifierNaming.Kebab, "-");
        var sb = new StringBuilder("@theme {").Append(Nl);
        for (var i = 0; i < p.Colors.Count; i++)
            sb.Append("  --color-").Append(names[i]).Append(": ").Append(CssValue(p.Colors[i].Color, o)).Append(';').Append(Nl);
        return sb.Append('}').ToString();
    }

    public static string Json(PaletteExportModel p, FormatOptions o)
    {
        var names = IdentifierNaming.Unique(p.Colors, n => n, " ");
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            w.WriteStartObject();
            for (var i = 0; i < p.Colors.Count; i++)
            {
                var c = p.Colors[i].Color;
                w.WriteString(names[i], c.IsOpaque ? Hex(c, o) : ColorFormats.ArgbHex(c, o));
            }
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(ms.ToArray()).Replace("\r\n", Nl);
    }

    public static string Dart(PaletteExportModel p)
    {
        var cls = IdentifierNaming.Pascal(p.Name, "Palette");
        var names = IdentifierNaming.Unique(p.Colors, n => IdentifierNaming.Camel(n), "");
        var sb = new StringBuilder("import 'package:flutter/material.dart';").Append(Nl).Append(Nl);
        sb.Append("class ").Append(cls).Append(" {").Append(Nl);
        sb.Append("  ").Append(cls).Append("._();").Append(Nl).Append(Nl);
        for (var i = 0; i < p.Colors.Count; i++)
            sb.Append("  static const Color ").Append(names[i]).Append(" = ").Append(ColorFormats.Dart(p.Colors[i].Color)).Append(';').Append(Nl);
        return sb.Append('}').ToString();
    }

    public static string CSharp(PaletteExportModel p)
    {
        var cls = IdentifierNaming.Pascal(p.Name, "Palette");
        var names = IdentifierNaming.Unique(p.Colors, n => IdentifierNaming.Pascal(n), "");
        var sb = new StringBuilder("using System.Windows.Media;").Append(Nl).Append(Nl);
        sb.Append("public static class ").Append(cls).Append(Nl).Append('{').Append(Nl);
        for (var i = 0; i < p.Colors.Count; i++)
            sb.Append("    public static readonly Color ").Append(names[i]).Append(" = ").Append(ColorFormats.CSharpArgb(p.Colors[i].Color)).Append(';').Append(Nl);
        return sb.Append('}').ToString();
    }

    private static string Hex(Argb c, FormatOptions o) => ColorFormats.Hex(c, o with { HexHash = true });

    private static string CssValue(Argb c, FormatOptions o) => c.IsOpaque ? Hex(c, o) : ColorFormats.CssRgb(c);
}

/// <summary>Conversión de nombres libres a identificadores válidos de cada lenguaje.</summary>
public static class IdentifierNaming
{
    /// <summary>
    /// Nombra cada color (los sin nombre como "Color N" según su posición) aplicando <paramref name="transform"/>
    /// y resuelve colisiones añadiendo sufijo numérico.
    /// </summary>
    public static string[] Unique(IReadOnlyList<PaletteExportColor> colors, Func<string, string> transform, string suffixSeparator)
    {
        var fallback = Texts.Current;
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new string[colors.Count];
        for (var i = 0; i < colors.Count; i++)
        {
            var raw = string.IsNullOrWhiteSpace(colors[i].Name) ? fallback.ColorNumberFallback(i + 1) : colors[i].Name!.Trim();
            var name = transform(raw);
            if (string.IsNullOrEmpty(name)) name = transform(fallback.ColorNumberFallback(i + 1));
            var candidate = name;
            for (var n = 2; !used.Add(candidate); n++) candidate = name + suffixSeparator + n;
            result[i] = candidate;
        }
        return result;
    }

    public static IReadOnlyList<string> Words(string text)
    {
        var normalized = RemoveDiacritics(text);
        var words = new List<string>();
        var sb = new StringBuilder();
        for (var i = 0; i < normalized.Length; i++)
        {
            var ch = normalized[i];
            if (!char.IsAsciiLetterOrDigit(ch))
            {
                Flush();
                continue;
            }
            // Separa camelCase: "primaryDark" → primary, Dark
            if (char.IsUpper(ch) && sb.Length > 0 && char.IsLower(sb[^1])) Flush();
            sb.Append(ch);
        }
        Flush();
        return words;

        void Flush()
        {
            if (sb.Length > 0) words.Add(sb.ToString());
            sb.Clear();
        }
    }

    public static string Kebab(string text) => string.Join("-", Words(text).Select(w => w.ToLowerInvariant()));

    public static string Camel(string text, string fallback = "color")
    {
        var pascal = Pascal(text, fallback);
        var first = pascal.TakeWhile(char.IsUpper).Count();
        // "Primary" → "primary"; "UIColor" → "uiColor"
        var lower = first <= 1 ? 1 : first == pascal.Length ? first : first - 1;
        return pascal[..lower].ToLowerInvariant() + pascal[lower..];
    }

    public static string Pascal(string text, string fallback = "Color")
    {
        var s = string.Concat(Words(text).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        if (s.Length == 0) s = fallback;
        if (char.IsDigit(s[0])) s = fallback + s;
        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
