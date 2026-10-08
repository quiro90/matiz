---
tags: [adr]
---
# ADR-001 WPF
**Contexto**: App de escritorio Windows con captura de píxeles multi-monitor.

**Decisión**: WPF sobre .NET 10.

**Alternativas descartadas**: WinUI 3 (empaquetado y overlays más complejos), Avalonia (multiplataforma innecesaria), WinForms (DPI mixto frágil, UI menos moderna).

**Consecuencias**: Interop Win32 directa, WriteableBitmap/RenderTargetBitmap sin dependencias; requiere estilos propios.

> Nota posterior: el objetivo pasa a incluir multiplataforma a futuro; ruta de migración en [[Multiplataforma]] (Core ya es portable).

Relacionado: [[ADR-007 Estilos propios]] · [[Matiz.App]]
