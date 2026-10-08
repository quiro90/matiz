## Why

Al programar o diseñar, obtener un color exacto de la pantalla, entenderlo en los formatos que usa el código y derivar una paleta coherente requiere hoy combinar varias herramientas (picker de PowerToys, webs de paletas, conversores, editores gráficos). Matiz unifica ese flujo en una utilidad de escritorio pequeña y rápida: **ALT+C → click → color capturado → valores → variantes → copiar → seguir trabajando**.

El repositorio está vacío (no hay estructura previa que conservar), por lo que este cambio define el producto completo de la v1 y su plan por fases antes de escribir código.

## What Changes

- Nueva aplicación WPF (.NET 10) con una única ventana compacta, tema claro/oscuro y UI neutra para que el color sea el protagonista.
- **Color actual** como única fuente de verdad: todos los controles (rueda, brillo, inputs, captura, paletas, historial) leen y escriben el mismo estado; cambios en vivo, sin botón "Aplicar"; comparación color anterior/actual y deshacer/rehacer.
- **Modelo de color** preciso y documentado: sRGB 8 bits + alpha como valor canónico; HEX, RGB, HSL, HSV, CMYK (aproximado, sin perfil) y OKLCH; reglas de redondeo explícitas; parser tolerante para pegar colores en cualquier formato soportado.
- **Selector visual**: rueda bidimensional (ángulo = hue, radio = saturación, centro blanco), control de brillo que muestra el propio color, distribución radial ajustable (pastel ↔ vivo), entradas numéricas precisas y escala de grises.
- **Captura de pantalla** con hotkey global (ALT+C por defecto, configurable), overlay por monitor, lupa con cuadrícula de píxeles, soporte multi-monitor y DPI mixto.
- **Generación de paletas** desde el color actual: armonías (complementaria, análoga, complementaria dividida, triádica, tetrádica), monocromática, tints/shades, neutros y **Design Scale 50–950** calculada en OKLCH.
- **Paletas guardadas** en JSON local con CRUD completo, reordenamiento, duplicado y paleta activa siempre visible.
- **Exportación**: copiar color/paleta/escala como CSS, JSON, Dart/Flutter, C#, Tailwind (sistema de formatos extensible) y exportar paleta como imagen PNG.
- **Image picker**: abrir/arrastrar/pegar una imagen y tomar colores con lupa; extracción de colores dominantes como entrega posterior dentro de la fase.
- **Historial** de colores recientes (30, persistente).
- Atajos de teclado, ajustes persistentes, modo "siempre visible" e instancia única.

## Capabilities

### New Capabilities
- `color-model`: representación interna del color, conversiones entre espacios, precisión y redondeo, alpha, CMYK aproximado y parsing de entradas de texto.
- `current-color`: estado único del color actual, sincronización de todas las vistas, color anterior/actual, deshacer/rehacer y entrada manual.
- `visual-picker`: rueda hue/saturación, control de brillo, distribución radial (enfoque pastel/vivo), entradas numéricas y escala de grises.
- `screen-picker`: modo de captura de píxeles de cualquier monitor con overlay, lupa, teclado y hotkey global.
- `color-clipboard`: visualización de formatos, copia individual, "copiar todo", fragmentos de código por color y formato por defecto.
- `palette-generation`: armonías, monocromática, tints/shades, neutros y Design Scale perceptual.
- `saved-palettes`: modelo de paleta, persistencia local y operaciones de gestión.
- `palette-export`: exportación de paletas/escalas a formatos de código extensibles y a imagen PNG.
- `image-picker`: carga de imágenes, selección de colores con lupa y extracción de colores dominantes.
- `color-history`: colores recientes con política de inserción, límite y persistencia.
- `app-shell`: ventana principal, layout, temas, atajos, ajustes, instancia única y modo siempre visible.

### Modified Capabilities
(ninguna: proyecto nuevo)

## Impact

- Código nuevo: solución `Matiz.slnx` con `src/Matiz.Core`, `src/Matiz.App`, `tests/Matiz.Core.Tests`.
- Dependencias NuGet: `CommunityToolkit.Mvvm` (App), `xunit` + `Microsoft.NET.Test.Sdk` (tests). Ninguna librería de UI de terceros.
- Integración Windows vía P/Invoke (user32/gdi32): `RegisterHotKey`, `BitBlt`, `EnumDisplayMonitors`, `GetDpiForMonitor`, `SetWindowPos`, `GetCursorPos`/`SetCursorPos`. Manifest con Per-Monitor DPI Aware v2.
- Datos de usuario en `%APPDATA%\Matiz\` (`palettes.json`, `history.json`, `settings.json`).
- No hay APIs, servicios remotos ni telemetría.
