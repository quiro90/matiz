---
tags: [arquitectura, captura, dpi]
---
# Multi-monitor y DPI
- Proceso **Per-Monitor DPI Aware v2** (`app.manifest`).
- Una `OverlayWindow` **por monitor** (no una gigante: WPF solo tiene un DPI por ventana).
- Posición con `SetWindowPos` en píxeles físicos; imagen escalada `px / dpiScale` con `NearestNeighbor` → 1 px de instantánea = 1 px físico. Se reajusta en `DpiChanged`.
- Cursor siempre con `GetCursorPos` (físico): **nunca** se convierte DIP→píxel.
- Soporta coordenadas negativas, escalas distintas y monitores verticales.

Pendiente de prueba manual → [[Pendiente de verificar]].
