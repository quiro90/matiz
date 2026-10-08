---
tags: [adr]
---
# ADR-003 Captura congelada
**Contexto**: Capturar el píxel real en cualquier monitor con lupa.

**Decisión**: Instantánea por monitor al activar + overlay por monitor.

**Alternativas descartadas**: Modo live con hook global de ratón (frágil, vigilado por antivirus, foco complejo).

**Consecuencias**: Determinista y sin hooks; el contenido animado queda congelado durante la captura.

Relacionado: [[Flujo interno de captura]] · [[Multi-monitor y DPI]]
