using Matiz.Core.Colors;
using Matiz.Core.Export;
using Matiz.Core.Formatting;
using Matiz.Core.History;
using Matiz.Core.Palettes;
using Matiz.Core.Session;

namespace Matiz.Core.Tests;

public class SessionTests
{
    private static readonly Argb Violet = Argb.FromRgb(82, 70, 188);
    private static readonly Argb Lighter = Argb.FromRgb(0x67, 0x5B, 0xCE);

    [Fact]
    public void Previous_and_current_after_drag()
    {
        var s = new ColorSession(ColorState.FromArgb(Violet));
        for (var i = 0; i < 100; i++) s.SetPreview(new ColorState(246 + i * 0.01, 0.6, 0.8));
        s.Commit(ColorState.FromArgb(Lighter));
        Assert.Equal(Violet, s.Previous.Argb);
        Assert.Equal(Lighter, s.Current.Argb);
        Assert.Equal(1, s.UndoCount);

        s.RestorePrevious();
        Assert.Equal(Violet, s.Current.Argb);
        Assert.Equal(Lighter, s.Previous.Argb);
    }

    [Fact]
    public void Undo_reverts_whole_drag_and_redo_reapplies()
    {
        var s = new ColorSession(ColorState.FromArgb(Violet));
        s.SetPreview(new ColorState(10, 0.5, 0.5));
        s.SetPreview(new ColorState(20, 0.5, 0.5));
        s.Commit(new ColorState(30, 0.5, 0.5));
        Assert.True(s.Undo());
        Assert.Equal(Violet, s.Current.Argb);
        Assert.True(s.Redo());
        Assert.Equal(new ColorState(30, 0.5, 0.5).Argb, s.Current.Argb);
        s.Undo();
        s.Commit(Lighter, ColorChangeSource.ManualInput);
        Assert.False(s.CanRedo);
    }

    [Fact]
    public void Undo_stack_is_limited()
    {
        var s = new ColorSession(ColorState.FromArgb(Violet));
        for (var i = 0; i < 80; i++) s.Commit(Argb.FromRgb((byte)i, 0, 0), ColorChangeSource.ManualInput);
        Assert.Equal(ColorSession.UndoLimit, s.UndoCount);
    }

    [Fact]
    public void Gray_commit_keeps_hue()
    {
        var s = new ColorSession(ColorState.FromArgb(Violet));
        s.Commit(Argb.FromRgb(128, 128, 128), ColorChangeSource.ManualInput);
        Assert.Equal(ColorState.FromArgb(Violet).Hue, s.Current.Hue, 9);
    }
}

public class HistoryTests
{
    [Fact]
    public void Dedupes_moves_to_front_and_limits()
    {
        var h = new ColorHistory();
        for (var i = 0; i < 35; i++) h.Add(Argb.FromRgb((byte)i, 0, 0));
        Assert.Equal(ColorHistory.Capacity, h.Items.Count);
        Assert.Equal(Argb.FromRgb(34, 0, 0), h.Items[0]);
        Assert.DoesNotContain(Argb.FromRgb(0, 0, 0), h.Items);

        var fifth = h.Items[4];
        h.Add(fifth);
        Assert.Equal(fifth, h.Items[0]);
        Assert.Single(h.Items, c => c == fifth);
    }
}

public class PaletteServiceTests
{
    private DateTimeOffset _now = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    private PaletteService NewService() => new(new PaletteLibrary(), () => _now);

    [Fact]
    public void Create_with_default_unique_names()
    {
        var s = NewService();
        var a = s.Create();
        var b = s.Create();
        Assert.Equal("Untitled palette", a.Name);
        Assert.Equal("Untitled palette 2", b.Name);
        Assert.Equal(a.CreatedAt, a.ModifiedAt);
        Assert.Empty(a.Colors);
        Assert.Equal(b.Id, s.Active!.Id);
    }

    [Fact]
    public void Operations_update_modified_date()
    {
        var s = NewService();
        var p = s.Create("PuchiApp");
        _now = _now.AddMinutes(5);
        var c = s.AddColor(p.Id, Argb.FromRgb(82, 70, 188), "Primary")!;
        Assert.Equal(_now, p.ModifiedAt);

        s.RenameColor(p.Id, c.Id, "Brand");
        s.ReplaceColor(p.Id, c.Id, Argb.FromRgb(1, 2, 3));
        Assert.Equal("Brand", p.Colors[0].Name);
        Assert.Equal("#010203", p.Colors[0].Hex);
    }

    [Fact]
    public void Move_duplicate_delete_restore()
    {
        var s = NewService();
        var p = s.Create("PuchiApp");
        s.AddColors(p.Id, [(Argb.FromRgb(1, 0, 0), "A"), (Argb.FromRgb(2, 0, 0), "B"), (Argb.FromRgb(3, 0, 0), "C")]);
        s.MoveColor(p.Id, p.Colors[2].Id, 0);
        Assert.Equal(["C", "A", "B"], p.Colors.Select(c => c.Name!));

        var copy = s.Duplicate(p.Id)!;
        Assert.Equal("PuchiApp (copy)", copy.Name);
        Assert.NotEqual(p.Colors[0].Id, copy.Colors[0].Id);

        var removed = s.Delete(p.Id)!.Value;
        Assert.Null(s.Find(p.Id));
        s.Restore(removed.Palette, removed.Index);
        Assert.Equal(0, s.Palettes.ToList().IndexOf(p));
    }

    [Fact]
    public void Ensure_active_creates_palette_when_empty()
    {
        var s = NewService();
        var p = s.EnsureActive();
        Assert.Equal(PaletteService.DefaultName, p.Name);
        Assert.Same(p, s.Active);
    }

    [Fact]
    public void Alpha_is_kept()
    {
        var pc = PaletteColor.Create(new Argb(128, 82, 70, 188));
        Assert.Equal("#5246BC", pc.Hex);
        Assert.Equal((byte)128, pc.Alpha);
        Assert.Equal(new Argb(128, 82, 70, 188), pc.Color);
    }
}

public class ExportTests
{
    private static readonly PaletteExportModel Puchi = new("PuchiApp",
    [
        new("Primary", Argb.FromRgb(0x52, 0x46, 0xBC)),
        new("Secondary", Argb.FromRgb(0xFF, 0x8A, 0x00)),
    ]);

    [Fact]
    public void Css_variables()
    {
        Assert.Equal(":root {\n  --primary: #5246BC;\n  --secondary: #FF8A00;\n}", PaletteFormatters.Css(Puchi, FormatOptions.Default));
    }

    [Fact]
    public void Json_object()
    {
        var json = PaletteFormatters.Json(Puchi, FormatOptions.Default);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("#5246BC", doc.RootElement.GetProperty("Primary").GetString());
        Assert.Equal("#FF8A00", doc.RootElement.GetProperty("Secondary").GetString());
    }

    [Fact]
    public void Dart_class()
    {
        var dart = PaletteFormatters.Dart(Puchi);
        Assert.Contains("class PuchiApp {", dart);
        Assert.Contains("static const Color primary = Color(0xFF5246BC);", dart);
        Assert.Contains("static const Color secondary = Color(0xFFFF8A00);", dart);
    }

    [Fact]
    public void CSharp_class()
    {
        var cs = PaletteFormatters.CSharp(Puchi);
        Assert.Contains("public static class PuchiApp", cs);
        Assert.Contains("public static readonly Color Primary = Color.FromArgb(255, 82, 70, 188);", cs);
    }

    [Fact]
    public void Scale_steps_and_tailwind()
    {
        var scale = new PaletteExportModel("Primary",
            [new("Primary 50", Argb.FromRgb(250, 250, 255)), new("Primary 950", Argb.FromRgb(10, 10, 30))]);
        var css = PaletteFormatters.Css(scale, FormatOptions.Default);
        Assert.Contains("--primary-50:", css);
        Assert.Contains("--primary-950:", css);
        Assert.Contains("--color-primary-50:", PaletteFormatters.Tailwind(scale, FormatOptions.Default));
        Assert.Contains("primary50", PaletteFormatters.Dart(scale));
    }

    [Fact]
    public void Unnamed_and_duplicate_names()
    {
        var p = new PaletteExportModel("Mi paleta", [
            new(null, Argb.FromRgb(1, 1, 1)),
            new("Primary", Argb.FromRgb(2, 2, 2)),
            new("primary", Argb.FromRgb(3, 3, 3)),
        ]);
        var css = PaletteFormatters.Css(p, FormatOptions.Default);
        Assert.Contains("--color-1:", css);
        Assert.Contains("--primary:", css);
        Assert.Contains("--primary-2:", css);
        Assert.Contains("class MiPaleta", PaletteFormatters.Dart(p));
        Assert.Contains("color1", PaletteFormatters.Dart(p));
    }

    [Theory]
    [InlineData("Primary Dark", "primary-dark", "primaryDark", "PrimaryDark")]
    [InlineData("Señal ámbar", "senal-ambar", "senalAmbar", "SenalAmbar")]
    [InlineData("primaryDark", "primary-dark", "primaryDark", "PrimaryDark")]
    [InlineData("500", "500", "color500", "Color500")]
    public void Identifier_naming(string input, string kebab, string camel, string pascal)
    {
        Assert.Equal(kebab, IdentifierNaming.Kebab(input));
        Assert.Equal(camel, IdentifierNaming.Camel(input));
        Assert.Equal(pascal, IdentifierNaming.Pascal(input));
    }
}
