namespace Matiz.Core.Colors;

/// <summary>
/// Estado del selector: coordenadas HSV continuas + alpha. Es la representación del
/// "Color Actual"; el valor canónico <see cref="Argb"/> se deriva de él.
/// Conserva hue/saturación aunque el color sea acromático (gris o negro).
/// </summary>
public readonly record struct ColorState
{
    public ColorState(double hue, double saturation, double value, byte alpha = 255)
    {
        Hue = ColorMath.NormalizeHue(hue);
        Saturation = Math.Clamp(saturation, 0, 1);
        Value = Math.Clamp(value, 0, 1);
        Alpha = alpha;
        Argb = ColorMath.FromHsv(Hue, Saturation, Value, alpha);
    }

    public double Hue { get; }
    public double Saturation { get; }
    public double Value { get; }
    public byte Alpha { get; }
    public Argb Argb { get; }

    public ColorState WithHue(double hue) => new(hue, Saturation, Value, Alpha);
    public ColorState WithSaturation(double s) => new(Hue, s, Value, Alpha);
    public ColorState WithValue(double v) => new(Hue, Saturation, v, Alpha);
    public ColorState WithHueSaturation(double hue, double s) => new(hue, s, Value, Alpha);

    /// <summary>
    /// Crea el estado para un color canónico. Si el color es acromático conserva el hue
    /// del estado previo; si es negro conserva también la saturación previa.
    /// El <see cref="Argb"/> resultante es siempre exactamente <paramref name="color"/>.
    /// </summary>
    public static ColorState FromArgb(Argb color, ColorState? previous = null)
    {
        var hsv = ColorMath.ToHsv(color);
        var hue = hsv.H;
        var sat = hsv.S;
        if (previous is { } p)
        {
            if (hsv.S <= 0 || hsv.V <= 0) hue = p.Hue;
            if (hsv.V <= 0) sat = p.Saturation;
        }
        var state = new ColorState(hue, sat, hsv.V, color.A);
        // Garantía de ida y vuelta exacta (protege ante cualquier borde numérico).
        return state.Argb == color ? state : new ColorState(hsv.H, hsv.S, hsv.V, color.A);
    }
}
