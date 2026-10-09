using Matiz.Core.Colors;

namespace Matiz.App.ScreenCapture;

/// <summary>Qué captura el modo captura: color principal o punto secundario del conjunto Personalizado.</summary>
public enum CaptureResultKind
{
    Principal,
    Secondary,
}

/// <summary>
/// Resultado de una acción del modo captura. <see cref="Continue"/> indica que el usuario tenía Shift presionado
/// y el overlay sigue abierto para capturar varios colores seguidos.
/// </summary>
public sealed record CaptureResult(Argb Color, CaptureResultKind Kind, bool Continue);