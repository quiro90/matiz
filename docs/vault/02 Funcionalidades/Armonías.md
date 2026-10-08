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
  - Arrastrar un punto: ajusta su posición de forma independiente (tono y saturación); su tarjeta cambia en vivo.
  - Doble click: restablece su posición canónica (antes era usar el color).
  - Usarlo como color principal: click sobre el cuerpo de su tarjeta — conserva el desfase y la figura se traslada al nuevo base.
  - **Restaurar armonía**: botón junto a "Luminosidad equilibrada", visible solo con desfases activos; restablece todos los puntos (equivale al doble click sobre cada uno). No es deshacible. El desfase se reinicia además desde el historial, captura, imagen, entrada manual, deshacer/rehacer y al cambiar el tipo de armonía.

Decisión → [[ADR-009 Armonías geométricas]]
