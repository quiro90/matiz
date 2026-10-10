---
tags: [funcionalidad, selector]
---
# Selector visual
- **Rueda**: ángulo = hue (0° rojo arriba, horario) · distancia al centro = saturación · centro blanco · dibujada a brillo 100%.
- **Columnas verticales a la derecha de la rueda** (para darle todo el alto disponible): **Brillo** (el propio color, de 100% arriba a negro) · **Enfoque Vivo (arriba) ↔ Pastel (abajo)** con botón ⟲ de restablecer debajo · y la columna de **Campos H / S / B** con **Gris equivalente**.
- **Enfoque**: redistribuye la saturación en el radio sin cambiar el color (`S = r^γ`).
- **Shift + arrastre**: ajuste fino (×0.25). Rueda enfocada: flechas cambian H/S.
- **Grises**: desde el centro de la rueda (saturación 0, cualquier brillo) y botón **Gris equivalente** (misma luminosidad OKLab).
- Con [[Armonías]] activas, la rueda muestra puntos secundarios.

Modelo → [[HSV en el selector]] · Render → [[Render de la rueda]] · Decisiones → [[ADR-004 Enfoque en lugar de zoom]], [[ADR-008 Brillo color a negro]]
