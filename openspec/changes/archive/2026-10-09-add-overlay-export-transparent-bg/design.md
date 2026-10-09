# Diseño: add-overlay-export-transparent-bg

## Context
- El botón "PNG" (barra inferior de la paleta activa) ejecuta `ExportActivePaletteImageCommand`, que llama a `Shell.ShowExportImage(PaletteExportModel.From(p))` y abre `ExportImageWindow` con `PaletteImageRenderer.Render(...)` (`PaletteImageOptions`: Horizontal, Scale, DarkBackground, ShowHsl, ShowCmyk). `Ctrl+E` (Services/Shortcuts.cs) apunta al mismo comando.
- `ExportImageWindow` ya usa el patrón `RadioButton` segmentado + `OnOptionChanged` → re-render en vivo. La ventana usa `ThemeService.ApplyTitleBar`.
- Los botones con desplegable de la app usan `Click="OpenMenu_Click"` + `ContextMenu` estático en `Window.Resources` (ver `CopyPaletteMenu`), posicionado en `PlacementMode.Bottom`.
- La tarjeta "Color actual" muestra "Anterior"/"Actual" desde `Session.Previous/Current.Argb` y tiene un `WrapPanel` con "Copiar todo", "Copiar como" y "Añadir a la paleta".
- `PaletteImageRenderer` rasteriza con `RenderTargetBitmap` (Pbgra32): si se omite el rectángulo de fondo, el PNG conserva alfa. Hay tests STA en `tests/Matiz.App.Tests/PaletteImageRendererTests.cs` que construyen `PaletteImageOptions` posicionalmente con 3 parámetros.

## Goals / Non-Goals

**Goals:**
- Desplegable con 2 opciones en el botón PNG; "Imagen" suma fondo transparente.
- Ventana nueva de superpuestos con renderer propio, formas cuadrado/círculo/triángulo, orden y escala, fondo claro/oscuro, valores por color, guardar PNG / copiar.
- Botón "Exportar superpuestos" para la dupla Anterior/Actual en la tarjeta de color actual.

**Non-Goals:**
- No persistir las opciones elegidas entre sesiones (cada ventana abre con sus valores por defecto).
- No tocar `Matiz.Core` ni formatos de texto de paletas; no hay exportación PDF/SVG de superpuestos.

## Decisions

1. **Ventana separada `ExportOverlayWindow` (no modo dentro de `ExportImageWindow`).**
   - Razón: las opciones difieren (forma/orden vs orientación) y mezclar dos renderers en una ventana complica el estado. Misma estética (preview izquierda, opciones derecha, tarjetas, título temático). El usuario confirmó ventana separada.
   - Alternativa descartada: `TabControl` de modos en la ventana existente.

2. **Renderer nuevo `OverlayImageRenderer` junto a `PaletteImageRenderer`; `PaletteImageOptions` solo gana `bool Transparent = false`.**
   - Razón: el layout es distinto (pila + listado); reusar la clase forzaría ramas en cada constante. La firma nueva es `(PaletteExportModel, OverlayImageOptions)`.
   - `Transparent` se agrega como parámetro opcional **después** de `DarkBackground` para no romper las llamadas posicionales de los tests existentes; sobre transparencia se omite el rectángulo de fondo y fg/borde quedan con estilo "claro" (`DarkBackground=false`), según lo acordado.
   - Alternativa descartada: enum `Background { Light, Dark, Transparent }` — rompería las construcciones posicionales de los tests sin aporte funcional.

3. **Geometría de la pila (incremento en %).**
   - Base fija (p. ej. capa mayor de 480 DIP); el inset entre capas `d = Base / (N + 1)` con piso `d ≥ 6 DIP`: con N=64 → d ≈ 7,4 DIP (todos los anillos visibles); con N=2 → d = 160 (capas claramente diferenciadas); N=1 → capa única sin anillos. Toda la pila centrada en su columna; la escala (1×–3×) multiplica el canvas completo, igual que hoy.
   - Borde por capa: mismo `Pen` alfa (30,0,0,0 / 40,255,255,255) que usa la exportación clásica; `EdgeMode.Aliased` para bordes limpios.
   - Triángulo: polígono isósceles inscripto en el cuadrado de la capa (apunta hacia arriba, centrado). Círculo: `Ellipse`. Cuadrado: `Rectangle`.
   - Alternativa descartada: % configurable por el usuario (más UI sin valor real; el auto-cálculo cumple la spec).

4. **Listado de valores a la derecha de la pila.** Misma tipografía y líneas (nombre, HEX, RGB + HSL/CMYK opcionales) que `PaletteImageRenderer`; el alto del canvas es el máximo entre la pila y el listado. Mantener el orden del listado = orden visual de la pila (respecto del orden elegido), para correlacionar capa y valor.

5. **Wiring MVVM mínimo.**
   - `IShell.ShowExportOverlay(PaletteExportModel)` implementado en `MainWindow` (`ExportOverlayWindow` con `Owner`, `ShowDialog`).
   - `MainViewModel.Palettes.cs`: `[RelayCommand(CanExecute = nameof(HasActiveColors))] ExportActivePaletteOverlay()`; notificar `CanExecuteChanged` junto al existente.
   - `MainViewModel.Current.cs`: `[RelayCommand] ExportCurrentPreviousOverlay()` que arma el modelo con `[Anterior, Actual]` (nombres localizados `color.previous` / `color.current`) — sin `CanExecute` (siempre disponible; si no hay "Anterior" aún, exporta solo la dupla tal cual esté).
   - Desplegable: `ContextMenu x:Key="ExportPaletteMenu"` en `Window.Resources` con 2 `MenuItem` → `ExportActivePaletteImageCommand` / `ExportActivePaletteOverlayCommand`; el botón mantiene icono + "PNG" + chevron y usa `OpenMenu_Click`.

6. **Localización.** Claves nuevas en `Strings.resx`/`Strings.es.resx`: `export.transparent`, `overlay.windowTitle`, `overlay.shape`, `overlay.shape.square/circle/triangle`, `overlay.order`, `overlay.order.forward/reverse`, `overlay.exportTooltip`, `overlay.prevCurrentTitle`. Reusar `export.savePng/copyImage/saveDialog/copyFailed/pngFilter` y `color.previous/current`.

## Risks / Trade-offs
- [Fondo transparente con texto claro legible] → Se acordó estilo claro (texto oscuro). Si el usuario exporta un PNG transparente para fondo oscuro, el texto podría fundirse; mitigación futura: acoplar selección de color de texto.
- [Triángulo inscripto deja esquinas de capa anteriores visibles fuera del triángulo] → Es el efecto deseado de superposición; se documenta en la preview, no se corrige.
- [Anillos mínimos con 64 colores a 1×] → Piso de 6 DIP garantiza visibilidad; a escala 1× el PNG crece verticalmente solo por el listado de valores, no por la pila.
- [Perf con 64 capas] → 64 formas + 64 bordes es trivial para `DrawingVisual`; sin riesgo real.

## Open Questions
- Ninguno que bloquee: la dupla Anterior/Actual se exporta siempre con "[Anterior, Actual]" aunque no exista color anterior (capa única) — comportamiento trivial aceptado por spec ("menos de dos colores → capa única").