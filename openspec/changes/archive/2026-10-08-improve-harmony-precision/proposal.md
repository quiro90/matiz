## Why

Al bajar el brillo, las armonías "se pierden": se generaban rotando el tono en OKLCH con croma fija, mientras la rueda es HSV. Resultado medido: una triádica de `#5246BC` caía en +136°/+275° de la rueda (no +120°/+240°) y la saturación saltaba de forma inconsistente según el brillo (p. ej. un verde al 70% producía armonías al 21% o al 51% según el brillo).

## What Changes

- Las armonías por rotación giran el tono **en la rueda** (HSV) el ángulo exacto y conservan la saturación del color base: los puntos forman siempre la figura geométrica exacta.
- Opción **"Luminosidad equilibrada"** (activa por defecto): ajusta solo el brillo de cada color para igualar la luminosidad perceptual (OKLab L) del base; como la posición en la rueda depende solo de H y S, la geometría se conserva.
- Las armonías se calculan desde las coordenadas continuas del selector (no desde el HEX redondeado), evitando derivas en colores muy oscuros.
- Los puntos de la rueda usan las coordenadas exactas de la armonía.

## Capabilities

### New Capabilities

### Modified Capabilities
- `palette-generation`: el requisito "Armonías en espacio perceptual" pasa a "Armonías geométricas con luminosidad equilibrada".

## Impact

`Matiz.Core/Generation/PaletteGenerator.cs`, `GeneratedColor`, `AppSettings` (nuevo ajuste), panel de armonías y puntos de la rueda en `Matiz.App`. Tests de generación.
