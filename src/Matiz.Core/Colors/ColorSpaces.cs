namespace Matiz.Core.Colors;

/// <summary>HSV/HSB. Hue en grados [0,360), Saturation y Value en [0,1].</summary>
public readonly record struct Hsv(double H, double S, double V);

/// <summary>HSL. Hue en grados [0,360), Saturation y Lightness en [0,1].</summary>
public readonly record struct Hsl(double H, double S, double L);

/// <summary>CMYK aproximado sin perfil. Todos los componentes en [0,1].</summary>
public readonly record struct Cmyk(double C, double M, double Y, double K);

/// <summary>Oklab (Björn Ottosson). L en [0,1].</summary>
public readonly record struct Oklab(double L, double A, double B);

/// <summary>OKLCH: forma polar de Oklab. Hue en grados [0,360).</summary>
public readonly record struct Oklch(double L, double C, double H);

/// <summary>sRGB lineal (sin función de transferencia), canales nominalmente en [0,1].</summary>
public readonly record struct LinearRgb(double R, double G, double B);
