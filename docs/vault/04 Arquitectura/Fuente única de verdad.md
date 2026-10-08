---
tags: [arquitectura, estado]
---
# Fuente única de verdad
`ColorSession` (Core) guarda **el** color actual como `ColorState` (H, S, V continuos + alpha).
- Todas las vistas leen de ella y escriben con `SetPreview` / `Commit`.
- `Previous` = color antes del último commit. Deshacer/rehacer: 50 commits.
- `ColorState.FromArgb(c, previous)` conserva hue/saturación en grises y negro → bajar el brillo a 0 y volver recupera el color.
- El VM evita bucles con `_syncing` y agrupa los previews por frame (`CompositionTarget.Rendering`).

Ver [[Flujo de un cambio de color]].
