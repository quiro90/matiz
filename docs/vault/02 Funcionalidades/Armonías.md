---
tags: [funcionalidad, paletas]
---
# Armonías
Complementaria (2) · Análoga (5, ±30°/±60°) · Complementaria dividida (3, ±150°) · Triádica (3) · Tetrádica (4) · Monocromática (5 pasos de la [[Design Scale]]).

- **Geometría exacta**: giran el tono **en la rueda** (HSV) el ángulo exacto y conservan la saturación del base → los puntos forman siempre la figura (triángulo, cruz…), con cualquier brillo.
- **Luminosidad equilibrada** (switch, activo por defecto): ajusta solo el brillo de cada color para igualar la luminosidad percibida (OKLab L) del base; si no se alcanza, usa brillo 100%. La posición en la rueda no cambia. Desactivado: todos con el mismo brillo HSV.
- Se calculan desde las coordenadas continuas del selector (no del HEX), estables en colores muy oscuros.
- **En la rueda**: cada color aparece como punto fino con líneas tenues al centro.
  - Click en un punto: lo marca (y su tarjeta) y ofrece **Copiar**; no cambia el color actual.
  - Doble click: pasa a ser el color actual.

Decisión → [[ADR-009 Armonías geométricas]]
