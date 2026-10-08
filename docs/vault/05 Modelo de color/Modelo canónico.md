---
tags: [color]
---
# Modelo canónico
- Valor canónico: **sRGB 8 bits + alpha** (`Argb`). Es lo que se muestra, copia y guarda.
- Redondeo: `round(x × 255)`, punto medio *away from zero* (127.5 → 128).
- El selector trabaja con `ColorState` continuo; el `Argb` se deriva → [[HSV en el selector]].
- Generación perceptual → [[OKLCH en la generación]].

Ver [[Conversiones y redondeo]] · [[ADR-002 HSV + OKLCH]]
