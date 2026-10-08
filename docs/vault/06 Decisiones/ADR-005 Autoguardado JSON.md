---
tags: [adr]
---
# ADR-005 Autoguardado JSON
**Contexto**: Persistir paletas, historial y ajustes localmente.

**Decisión**: JSON legible, autoguardado con escritura atómica y versión de esquema.

**Alternativas descartadas**: SQLite/LiteDB (sin beneficio a este volumen), botón Guardar.

**Consecuencias**: `Ctrl+S` pasa a ser 'agregar color a la paleta activa'.

Relacionado: [[Persistencia JSON]] · [[Esquema palettes.json]]
