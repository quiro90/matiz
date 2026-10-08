---
tags: [adr]
---
# ADR-008 Brillo color a negro
**Contexto**: Se pidió brillo 'blanco → color → negro' y rueda con centro blanco.

**Decisión**: Brillo = HSV V (color a 100% → negro); hacia blanco se va moviendo el punto al centro. Confirmado en revisión.

**Alternativas descartadas**: Brillo tipo HSL L (incompatible con centro blanco sin duplicar coordenadas).

**Consecuencias**: Una sola coordenada por color (fuente única de verdad).

Relacionado: [[HSV en el selector]] · [[Fuente única de verdad]]
