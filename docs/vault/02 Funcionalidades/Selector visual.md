---
tags: [funcionalidad, selector]
---
# Selector visual
- **Rueda**: ángulo = hue (0° rojo arriba, horario) · distancia al centro = saturación · centro blanco · dibujada a brillo 100%.
- **Brillo**: control vertical con el propio color, de 100% (arriba) a negro.
- **Enfoque Vivo ↔ Pastel**: redistribuye la saturación en el radio sin cambiar el color (`S = r^γ`).
- **Campos H / S / B** precisos (flechas ±1, Shift ±10, rueda del ratón).
- **Shift + arrastre**: ajuste fino (×0.25). Rueda enfocada: flechas cambian H/S.
- **Grises**: desde el centro de la rueda (saturación 0, cualquier brillo) y botón **Gris equivalente** (misma luminosidad OKLab).
- Con [[Armonías]] activas, la rueda muestra puntos secundarios.

Modelo → [[HSV en el selector]] · Render → [[Render de la rueda]] · Decisiones → [[ADR-004 Enfoque en lugar de zoom]], [[ADR-008 Brillo color a negro]]
