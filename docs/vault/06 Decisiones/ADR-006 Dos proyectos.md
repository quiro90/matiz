---
tags: [adr]
---
# ADR-006 Dos proyectos
**Contexto**: Arquitectura limpia sin sobreingeniería.

**Decisión**: `Matiz.Core` (lógica testeable) + `Matiz.App` (WPF/Win32) + tests.

**Alternativas descartadas**: Capas Domain/Application/Infrastructure en proyectos separados.

**Consecuencias**: Menos ceremonia; la persistencia vive en Core para testearla con carpetas temporales.

Relacionado: [[Arquitectura general]]
