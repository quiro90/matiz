using Matiz.Core.Formatting;
using Matiz.Core.Generation;

namespace Matiz.Core.Settings;

public enum ThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>Contenido de settings.json.</summary>
public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public ThemePreference Theme { get; set; } = ThemePreference.System;
    public bool AlwaysOnTop { get; set; }
    /// <summary>Botones Minimizar/Cerrar a la izquierda de la barra superior (por defecto, a la derecha).</summary>
    public bool WindowButtonsOnLeft { get; set; }
    public string CaptureHotkey { get; set; } = "Alt+C";

    /// <summary>Formato principal: usado por Ctrl+C, copiar al capturar y click en muestras.</summary>
    public string DefaultFormatId { get; set; } = ColorFormatters.HexId;
    public bool HexUppercase { get; set; } = true;
    public bool HexHash { get; set; } = true;

    public bool CopyOnCapture { get; set; } = true;
    public bool ShowAfterCapture { get; set; } = true;

    /// <summary>Enfoque de la rueda: −1 vivo, 0 lineal, +1 pastel.</summary>
    public double WheelFocus { get; set; }
    public ScaleAnchorMode ScaleAnchor { get; set; } = ScaleAnchorMode.Fixed500;
    public string ScalePrefix { get; set; } = "Primary";
    /// <summary>Armonías con luminosidad equilibrada (ajusta el brillo de cada color al L perceptual del base).</summary>
    public bool HarmonyBalanceLightness { get; set; } = true;

    public string LastColor { get; set; } = "#5246BC";
    public double? LastHue { get; set; }

    /// <summary>Rectángulo de la ventana en píxeles físicos (pantalla virtual).</summary>
    public WindowPlacement? Window { get; set; }

    public FormatOptions ToFormatOptions() => new(HexUppercase, HexHash);
}

public sealed class WindowPlacement
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Maximized { get; set; }
}
