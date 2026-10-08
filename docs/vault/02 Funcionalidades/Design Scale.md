---
tags: [funcionalidad, paletas, oklch]
---
# Design Scale
Escala 50, 100 … 900, 950 para design systems, calculada en **OKLCH** ([[OKLCH en la generación]]).

1. L de referencia (estilo Tailwind v4) → ver [[Constantes]].
2. Ancla: **Base en 500** (por defecto) o **Automático** (paso de L más cercana). Si 500 es imposible (blanco/negro puros) usa automático.
3. Remapeo garantizando **ΔL ≥ 0.02** entre pasos.
4. Croma del base reducida hacia los extremos (caída 0.6), hue constante.
5. Ajuste a gama sRGB reduciendo solo croma. El ancla es **exactamente** el color base.

Verificado por tests: L estrictamente decreciente para cualquier entrada ([[Tests]]).
