---
tags: [adr]
---
# ADR-002 HSV + OKLCH
**Contexto**: Se pedía rueda con centro blanco, brillo separado y escalas perceptuales.

**Decisión**: HSV para interactuar, OKLCH para generar, sRGB 8 bits como valor canónico.

**Alternativas descartadas**: HSL (centro gris), HSLuv/OKHSV (números desconocidos para el usuario), solo HSV (escalas pobres).

**Consecuencias**: Números familiares en el selector y escalas de calidad; dos modelos a mantener (aislados en Core).

> Actualización: las armonías giran ahora en la rueda HSV con luminosidad equilibrada → [[ADR-009 Armonías geométricas]].

Relacionado: [[HSV en el selector]] · [[OKLCH en la generación]]
