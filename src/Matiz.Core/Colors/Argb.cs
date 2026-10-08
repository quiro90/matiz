namespace Matiz.Core.Colors;

/// <summary>
/// Valor canónico de un color: sRGB de 8 bits por canal más alpha de 8 bits.
/// Todo lo que se muestra, copia o guarda se deriva de este valor.
/// </summary>
public readonly record struct Argb(byte A, byte R, byte G, byte B)
{
    public static Argb FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>Convierte un canal en [0,1] al byte canónico: round(x × 255), punto medio away-from-zero.</summary>
    public static byte ToByte(double unit)
    {
        if (double.IsNaN(unit)) return 0;
        var v = Math.Round(unit * 255.0, MidpointRounding.AwayFromZero);
        return (byte)Math.Clamp(v, 0, 255);
    }

    public static Argb FromUnit(double r, double g, double b, byte a = 255) =>
        new(a, ToByte(r), ToByte(g), ToByte(b));

    public static Argb FromUInt32(uint argb) =>
        new((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    public uint ToUInt32() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

    public bool IsOpaque => A == 255;

    public double RUnit => R / 255.0;
    public double GUnit => G / 255.0;
    public double BUnit => B / 255.0;

    public Argb WithAlpha(byte a) => this with { A = a };

    public Argb Opaque => this with { A = 255 };

    /// <summary>#RRGGBB en mayúsculas (sin alpha).</summary>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>#AARRGGBB en mayúsculas.</summary>
    public string ToArgbHex() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    public override string ToString() => IsOpaque ? ToHex() : ToArgbHex();
}
