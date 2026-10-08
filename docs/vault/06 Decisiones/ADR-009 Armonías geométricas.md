---
tags: [adr]
---
# ADR-009 Armonías geométricas
**Contexto**: Las armonías giraban el tono en OKLCH con croma fija, pero la rueda es HSV: una triádica de `#5246BC` caía en +136°/+275° de la rueda y la saturación saltaba según el brillo ("se pierde la armonía al bajar el brillo").

**Decisión**: Girar el tono **en la rueda (HSV)** el ángulo exacto y conservar la saturación; con **Luminosidad equilibrada** (por defecto) ajustar solo el brillo de cada color para igualar la OKLab L del base (búsqueda binaria; si no se alcanza, brillo 100%). Calcular desde las coordenadas continuas del selector.

**Alternativas descartadas**: Mantener la rotación OKLCH (figura no geométrica en la rueda, saturación inestable); rotación HSV pura sin equilibrar (los tonos claros/oscuros pesan distinto; queda como opción al desactivar el switch).

**Consecuencias**: La figura en la rueda es exacta y estable al mover el brillo; la luminosidad percibida se iguala cuando es alcanzable. Supera a [[ADR-002 HSV + OKLCH]] solo en lo que respecta a las armonías.

Relacionado: [[Armonías]] · [[OKLCH en la generación]] · cambio `openspec/changes/archive/2026-10-08-improve-harmony-precision/`
