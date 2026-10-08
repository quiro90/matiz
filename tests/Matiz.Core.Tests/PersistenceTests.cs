using Matiz.Core.Colors;
using Matiz.Core.History;
using Matiz.Core.Palettes;
using Matiz.Core.Persistence;
using Matiz.Core.Settings;

namespace Matiz.Core.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "matiz-tests-" + Guid.NewGuid().ToString("N"));
    private DataPaths Paths => new(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Missing_file_returns_empty_library()
    {
        using var store = PaletteRepository.Create(Paths);
        var result = store.Load();
        Assert.Empty(result.Value.Palettes);
        Assert.Null(result.RecoveredCorruptFile);
    }

    [Fact]
    public void Palettes_round_trip_with_order_names_and_dates()
    {
        var lib = new PaletteLibrary();
        var svc = new PaletteService(lib);
        var p = svc.Create("PuchiApp");
        svc.AddColors(p.Id, [(Argb.FromRgb(82, 70, 188), "Primary"), (new Argb(128, 255, 138, 0), "Secondary"), (Argb.FromRgb(34, 197, 94), null)]);

        using (var store = PaletteRepository.Create(Paths)) store.Save(lib);

        using var store2 = PaletteRepository.Create(Paths);
        var loaded = store2.Load().Value;
        var lp = Assert.Single(loaded.Palettes);
        Assert.Equal("PuchiApp", lp.Name);
        Assert.Equal(p.CreatedAt, lp.CreatedAt);
        Assert.Equal(p.ModifiedAt, lp.ModifiedAt);
        Assert.Equal(["Primary", "Secondary", null], lp.Colors.Select(c => c.Name));
        Assert.Equal(new Argb(128, 255, 138, 0), lp.Colors[1].Color);
        Assert.Equal(p.Id, loaded.ActivePaletteId);

        var json = File.ReadAllText(Paths.Palettes);
        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"hex\": \"#5246BC\"", json);
    }

    [Fact]
    public void Corrupt_file_is_renamed_and_empty_library_returned()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Paths.Palettes, "{ esto no es json");
        using var store = PaletteRepository.Create(Paths);
        var result = store.Load();
        Assert.Empty(result.Value.Palettes);
        Assert.NotNull(result.RecoveredCorruptFile);
        Assert.True(File.Exists(result.RecoveredCorruptFile));
        Assert.False(File.Exists(Paths.Palettes));
        Assert.Matches(@"palettes\.corrupt-\d{8}-\d{6}\.json$", result.RecoveredCorruptFile);
    }

    [Fact]
    public void Migrates_v0_array_format()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Paths.Palettes, """
            [ { "id": "7f9c1b8e-0000-0000-0000-000000000001", "name": "Vieja", "colors": [ { "hex": "#5246BC", "name": "Primary" } ] } ]
            """);
        using var store = PaletteRepository.Create(Paths);
        var lib = store.Load().Value;
        Assert.Equal(PaletteLibrary.CurrentSchemaVersion, lib.SchemaVersion);
        Assert.Equal("Vieja", Assert.Single(lib.Palettes).Name);
        Assert.Equal(Argb.FromRgb(82, 70, 188), lib.Palettes[0].Colors[0].Color);
    }

    [Fact]
    public void Atomic_save_replaces_and_keeps_backup()
    {
        using var store = PaletteRepository.Create(Paths);
        store.Save(new PaletteLibrary());
        var lib = new PaletteLibrary();
        new PaletteService(lib).Create("Nueva");
        store.Save(lib);
        Assert.True(File.Exists(Paths.Palettes + ".bak"));
        Assert.False(File.Exists(Paths.Palettes + ".tmp"));
        Assert.Contains("Nueva", File.ReadAllText(Paths.Palettes));
    }

    [Fact]
    public async Task Debounced_save_writes_after_delay_and_on_flush()
    {
        using var store = PaletteRepository.Create(Paths, TimeSpan.FromMilliseconds(50));
        var lib = new PaletteLibrary();
        new PaletteService(lib).Create("Diferida");
        store.ScheduleSave(lib);
        for (var i = 0; i < 40 && !File.Exists(Paths.Palettes); i++) await Task.Delay(25);
        Assert.Contains("Diferida", File.ReadAllText(Paths.Palettes));
    }

    [Fact]
    public void History_and_settings_round_trip()
    {
        var history = new ColorHistory([Argb.FromRgb(1, 2, 3), new Argb(128, 4, 5, 6)]);
        using (var hs = HistoryRepository.Create(Paths)) hs.Save(HistoryData.From(history));
        using (var hs = HistoryRepository.Create(Paths))
            Assert.Equal(history.Items, hs.Load().Value.ToHistory().Items);

        var settings = new AppSettings { Theme = ThemePreference.Dark, WheelFocus = 0.5, LastHue = 246, Window = new WindowPlacement { Left = -1920, Top = 0, Width = 1000, Height = 680 } };
        using (var ss = SettingsRepository.Create(Paths)) ss.Save(settings);
        using (var ss = SettingsRepository.Create(Paths))
        {
            var loaded = ss.Load().Value;
            Assert.Equal(ThemePreference.Dark, loaded.Theme);
            Assert.Equal(0.5, loaded.WheelFocus);
            Assert.Equal(-1920, loaded.Window!.Left);
            Assert.Contains("\"theme\": \"Dark\"", File.ReadAllText(Paths.Settings));
        }
    }
}
