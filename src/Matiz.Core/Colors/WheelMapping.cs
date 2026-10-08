namespace Matiz.Core.Colors;

/// <summary>
/// Mapeo entre la posición en la rueda y (hue, saturación HSV).
/// Ángulo: 0° (rojo) arriba, creciente en sentido horario. Radio normalizado r ∈ [0,1].
/// Distribución radial: S = r^γ. γ = 1 lineal; γ &gt; 1 dedica más radio a saturaciones bajas (pastel);
/// γ &lt; 1 a saturaciones altas (vivo).
/// </summary>
public static class WheelMapping
{
    public const double MaxFocusExponent = 1.25;

    /// <summary>Convierte el control "Enfoque" k ∈ [-1, 1] (−1 vivo, 0 lineal, +1 pastel) en γ.</summary>
    public static double GammaFromFocus(double focus) =>
        Math.Pow(2, MaxFocusExponent * Math.Clamp(focus, -1, 1));

    /// <summary>
    /// Posición relativa al centro (dx a la derecha, dy hacia abajo, en las mismas unidades que <paramref name="radius"/>)
    /// → hue y saturación. Fuera de la rueda la saturación se limita a 1.
    /// </summary>
    public static (double Hue, double Saturation) FromPoint(double dx, double dy, double radius, double gamma)
    {
        if (radius <= 0) return (0, 0);
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var r = Math.Min(dist / radius, 1);
        var hue = dist < 1e-9 ? double.NaN : ColorMath.NormalizeHue(Math.Atan2(dx, -dy) * 180 / Math.PI);
        return (hue, Math.Pow(r, gamma));
    }

    /// <summary>Hue y saturación → desplazamiento normalizado (radio 1) desde el centro.</summary>
    public static (double X, double Y) ToPoint(double hue, double saturation, double gamma)
    {
        var r = Math.Pow(Math.Clamp(saturation, 0, 1), 1 / gamma);
        var rad = hue * Math.PI / 180;
        return (r * Math.Sin(rad), -r * Math.Cos(rad));
    }
}
