---
tags: [arquitectura, rendimiento]
---
# Render de la rueda
- `ColorWheel` genera un `WriteableBitmap` Pbgra32 en **píxeles físicos**, en paralelo por filas, con borde antialiasado.
- Caché por (tamaño en px, γ): mover el marcador o el brillo **no** lo regenera (`RenderCount` para diagnóstico).
- El marcador principal y los puntos de [[Armonías]] se dibujan encima en `OnRender`.
- El brillo usa un `LinearGradientBrush` de 2 paradas: reproduce exactamente la rampa de V.

Mapeo → [[HSV en el selector]].
