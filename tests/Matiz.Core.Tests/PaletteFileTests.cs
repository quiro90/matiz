using System.Text.Json;
using Matiz.Core.Colors;
using Matiz.Core.Palettes;
using Matiz.Core.Persistence;

namespace Matiz.Core.Tests;

/// <summary>Formato de archivo de paleta (.mpalette): roundtrip, validaciones e importación.</summary>
public sealed class PaletteFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "matiz-tests-" + Guid.NewGuid().ToString("N"));
    private string PathOf(string name) => System.IO.Path.Combine(_dir, name);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static Palette SamplePalette()
    {
        var svc = new PaletteService(new PaletteLibrary());
        var p = svc.Create("PuchiApp", activate: false);
        svc.SetDescription(p.Id, "Colores de la app");
        svc.AddColors(p.Id,
        [
            (Argb.FromRgb(82, 70, 188), "Primary"),
            (new Argb(128, 255, 138, 0), "Secondary"),
            (Argb.FromRgb(34, 197, 94), null),
        ]);
        return p;
    }

    [Fact]
    public void Round_trip_preserves_name_description_colors_order_dates_and_alpha()
    {
        var p = SamplePalette();
        Directory.CreateDirectory(_dir);
        var path = PathOf("PuchiApp.mpalette");
        PaletteFile.From(p).Write(path);

        var file = PaletteFile.Read(path);
        Assert.Equal(PaletteFile.CurrentSchemaVersion, file.SchemaVersion);
        Assert.Equal("PuchiApp", file.Name);
        Assert.Equal("Colores de la app", file.Description);
        Assert.Equal(p.CreatedAt, file.CreatedAt);
        Assert.Equal(p.ModifiedAt, file.ModifiedAt);
        Assert.Equal(p.Colors.Select(c => (c.Name, c.Color)), file.Colors.Select(c => (c.Name, c.Color)));
        Assert.Equal(new Argb(128, 255, 138, 0), file.Colors[1].Color);

        var json = File.ReadAllText(path);
        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"name\": \"PuchiApp\"", json);
        Assert.Contains("\"hex\": \"#5246BC\"", json);
        Assert.Contains("\"alpha\": 128", json);
        Assert.DoesNotContain("grayPercent", json); // null = 0 %: el campo se omite (compatibilidad hacia atrás)
    }

    [Fact]
    public void Round_trip_preserves_gray_percent()
    {
        var svc = new PaletteService(new PaletteLibrary());
        var p = svc.Import(new PaletteFile
        {
            Name = "Gris",
            GrayPercent = 60,
            Colors = [new PaletteColor { Hex = "#5246BC", Name = "Primary" }],
        });
        Directory.CreateDirectory(_dir);
        var path = PathOf("Gris.mpalette");
        PaletteFile.From(p).Write(path);

        var json = File.ReadAllText(path);
        Assert.Contains("\"grayPercent\": 60", json);

        var file = PaletteFile.Read(path);
        Assert.Equal(60, file.GrayPercent);
        var reimported = svc.Import(file);
        Assert.Equal(60, reimported.GrayPercent);
    }

    [Fact]
    public void Old_file_without_gray_percent_reads_as_zero()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf("vieja.mpalette"), """{"schemaVersion": 1, "name": "Vieja", "colors": [{"hex": "#5246BC"}]}""");
        var file = PaletteFile.Read(PathOf("vieja.mpalette"));
        Assert.Null(file.GrayPercent);
    }

    [Fact]
    public void Invalid_json_fails()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf("roto.mpalette"), "{ esto no es json");
        Assert.ThrowsAny<JsonException>(() => PaletteFile.Read(PathOf("roto.mpalette")));
    }

    [Fact]
    public void Future_schema_version_is_rejected()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf("futura.mpalette"), """{"schemaVersion": 99, "name": "Futura"}""");
        var ex = Assert.Throws<NotSupportedException>(() => PaletteFile.Read(PathOf("futura.mpalette")));
        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public void Import_always_creates_new_palette_and_never_touches_existing()
    {
        var svc = new PaletteService(new PaletteLibrary());
        var existing = svc.Create("PuchiApp", activate: false);
        svc.SetDescription(existing.Id, "Colores de la app");
        svc.AddColors(existing.Id,
        [
            (Argb.FromRgb(82, 70, 188), "Primary"),
            (new Argb(128, 255, 138, 0), "Secondary"),
            (Argb.FromRgb(34, 197, 94), null),
        ]);
        var file = new PaletteFile { Name = "PuchiApp", Description = "Importada", Colors = [new PaletteColor { Hex = "#E24347", Name = "Red" }] };
        var createdAt = DateTimeOffset.Parse("2026-10-09T10:00:00Z");
        file.CreatedAt = createdAt;
        file.ModifiedAt = createdAt.AddMinutes(5);

        var imported = svc.Import(file);

        // Nunca pisa: la existente sigue intacta con sus mismos datos.
        Assert.Equal(existing.Id, svc.Find(existing.Id)!.Id);
        Assert.Equal(3, svc.Find(existing.Id)!.Colors.Count);
        // Nueva con sufijo " 2", colores/descripción/fechas del archivo, ids nuevos.
        Assert.Equal("PuchiApp 2", imported.Name);
        Assert.Equal("Importada", imported.Description);
        Assert.Equal(new Argb(255, 226, 67, 71), Assert.Single(imported.Colors).Color);
        Assert.Equal("Red", imported.Colors[0].Name);
        Assert.Equal(createdAt, imported.CreatedAt);
        Assert.Equal(createdAt.AddMinutes(5), imported.ModifiedAt);
        Assert.NotEqual(imported.Colors[0].Id, svc.Find(existing.Id)!.Colors[0].Id);
        Assert.Equal(imported.Id, svc.Library.ActivePaletteId);
    }

    [Fact]
    public void Import_preserves_more_than_64_colors()
    {
        var svc = new PaletteService(new PaletteLibrary());
        List<PaletteColor> many = [.. Enumerable.Range(0, 70).Select(i => PaletteColor.Create(Argb.FromRgb((byte)i, 0, 0)))];
        var imported = svc.Import(new PaletteFile { Name = "Grande", Colors = many });
        Assert.Equal(70, imported.Colors.Count);
    }

    [Fact]
    public void Import_without_name_uses_default_name()
    {
        var svc = new PaletteService(new PaletteLibrary());
        var imported = svc.Import(new PaletteFile { Colors = [] });
        Assert.Equal(PaletteService.DefaultName, imported.Name);
    }
}