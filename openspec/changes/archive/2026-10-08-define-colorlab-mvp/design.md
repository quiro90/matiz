## Context

Repositorio vacío: no hay código, convenciones ni estructura previa que conservar. Entorno: Windows 11, .NET SDK 10.0.401, OpenSpec 1.11. Motivación y alcance en `proposal.md`; requisitos de comportamiento en `specs/`. Este documento fija las decisiones técnicas y explica por qué.

Restricciones que condicionan el diseño:
- Captura de píxeles de todo el escritorio virtual con DPI mixto → integración Win32 inevitable.
- Interacción en tiempo real (rueda, brillo, paletas en vivo) → cálculos baratos, render en caché.
- "Herramienta pequeña": mínimas dependencias, mínimas capas, nada de frameworks de UI externos.

## Goals / Non-Goals

**Goals:**
- Núcleo de color puro, sin UI, 100% testeable (conversiones, parsing, formatos, generación, persistencia).
- Una sola fuente de verdad para el color actual con sincronización trivial de todas las vistas.
- Screen picker con precisión de píxel físico en cualquier combinación de monitores/DPI.
- Formatos de exportación extensibles por registro.

**Non-Goals:**
- Gestión de color ICC, soft-proofing o CMYK de impresión.
- Edición de imágenes, capas, degradados, herramientas de dibujo.
- Sincronización en la nube, cuentas, colaboración.
- Portabilidad a macOS/Linux.
- Inyección de dependencias con contenedor, plugins dinámicos, base de datos.

## Decisions

### D1. WPF sobre WinUI 3, Avalonia o WinForms

| Opción | A favor | En contra |
|---|---|---|
| **WPF (.NET 10)** ✅ | Maduro, sin empaquetado MSIX obligatorio, `WriteableBitmap` y `DrawingVisual`/`RenderTargetBitmap` (rueda y exportar PNG sin dependencias), interop Win32 directa (`HwndSource`, P/Invoke), Per-Monitor DPI v2 soportado, ventanas transparentes/topmost sencillas, arranque rápido | Look por defecto anticuado → requiere estilos propios |
| WinUI 3 | Look Fluent nativo | Windows App SDK como dependencia grande, despliegue más complejo, overlays transparentes multi-monitor más difíciles, ciclo de desarrollo más lento |
| Avalonia | Moderno, multiplataforma | La multiplataforma no aporta (la captura es Win32 de todos modos), dependencia grande |
| WinForms | Ligero, GDI+ | Peor composición/antialiasing, DPI mixto más frágil, UI moderna costosa |

**Decisión: WPF.** Estilos propios ligeros (un `ResourceDictionary` de tokens de color + estilos de controles). En la fase 1 se hará un spike breve del tema Fluent integrado de WPF (`ThemeMode`, .NET 9+); se adopta solo si su estado de soporte y apariencia encajan con la UI neutra; si no, estilos propios.

### D2. Modelo de color: HSV para la interacción, OKLCH para la generación, sRGB 8 bits como canónico

Se evaluaron los modelos frente a los 5 criterios pedidos:

| Modelo | Rueda intuitiva (centro blanco) | Coherencia visual | Escalas útiles | Valores familiares | Coste |
|---|---|---|---|---|---|
| **HSV/HSB** | ✅ centro blanco con V=100%, radio = mezcla con blanco (tint), V = mezcla con negro (shade) | ❌ hue no uniforme (verde enorme, amarillo/cian estrechos), luminosidad percibida varía con el hue | ❌ | ✅ es lo que muestran todas las herramientas | trivial |
| HSL | ❌ centro gris al 50% de L; para centro blanco hay que fijar L=100% y la rueda queda blanca | ❌ igual que HSV | ❌ (L=50% amarillo ≠ L=50% azul) | ✅ | trivial |
| HSLuv | ✅ L perceptual | ✅ | ✅ | ❌ valores desconocidos para el usuario, S relativo a la gama hace saltos raros | medio |
| **OKLCH** | ❌ como rueda: gama sRGB irregular (huecos fuera de gama) | ✅ L perceptualmente uniforme, hue estable | ✅ estándar moderno (CSS Color 4, Tailwind v4) | ➖ creciente en CSS | bajo (~60 líneas) |
| OKHSV/OKHSL | ✅ forma de HSV con hue perceptual | ✅ | ✅ | ❌ los números no coinciden con HSV "normal" | alto (cálculo de cúspide de gama) |

**Decisión:**
1. **Valor canónico:** sRGB 8 bits + alpha (`Argb`). Es lo que produce la pantalla, lo que se copia y lo que se guarda. Ninguna conversión acumula error porque todo se deriva de él o del estado del selector.
2. **Interacción del selector (rueda + brillo + campos H/S/B): HSV.** Razones: (a) cumple exactamente el modelo visual pedido: centro blanco, radio = saturación, brillo independiente; (b) se descompone en las dos operaciones de diseño básicas: **acercarse al centro = tint (hacia blanco)**, **bajar brillo = shade (hacia negro)**, lo que hace la interacción predecible; (c) los números H/S/B que ve el usuario son los mismos que en cualquier otra herramienta y que el formato HSV copiado. No se elige por facilidad sino porque es el único modelo que cumple a la vez "centro blanco" y "valores reconocibles".
3. **Generación (Design Scale, neutros, armonías, monocromática, gris equivalente): OKLCH/OKLab.** Es donde la uniformidad perceptual importa de verdad: pasos de luminosidad homogéneos entre hues y rotaciones de hue que conservan el peso visual.
4. **Tints/Shades: mezcla sRGB** (definición Sass/Bootstrap), deliberadamente "convencional" para que coincida con lo que el usuario obtiene en otras herramientas. La alternativa perceptual es la Design Scale.
5. **Futuro opcional:** "Rueda perceptual" (OKHSV) como modo alternativo; el mapeo rueda↔color está aislado tras una interfaz (`IWheelColorSpace`) para que sea un cambio local.

Consecuencia (confirmada en la revisión, Q1): el control de brillo va del **color a brillo 100% → negro**, no de blanco → color → negro. Ir hacia el blanco se hace moviendo el punto hacia el centro de la rueda. Un control "blanco → color → negro" (L de HSL) es incompatible con una rueda de centro blanco sin introducir dos coordenadas para el mismo color (rompería la fuente única de verdad). La Design Scale, siempre visible debajo, cubre visualmente el recorrido blanco→color→negro y es clicable.

### D3. Estado del color actual

`ColorState` (en Core) es inmutable: `{ double Hue, double Saturation, double Value, byte Alpha }` + `Argb` derivado y cacheado. Un único `CurrentColorService` (App) expone `Current`, `Previous`, `SetPreview(state)`, `Commit(state, source)`, `Undo()`, `Redo()` y el evento `Changed(ColorState, ChangeKind)`.
- Las vistas nunca guardan su propio color; leen `Current` y llaman a `SetPreview`/`Commit`.
- `FromArgb(argb, previousState)` preserva hue/saturación del estado previo cuando el color entrante es acromático (spec `current-color`).
- Los arrastres llaman a `SetPreview` en cada movimiento y a `Commit` al soltar → la pila de deshacer solo recibe confirmaciones.
- Coalescencia: las notificaciones de cambio se agrupan por frame (`CompositionTarget.Rendering`) para que un ratón de 1000 Hz no recalcule la UI 1000 veces por segundo.

### D4. Arquitectura y estructura de la solución

```
Matiz.slnx
├─ src/Matiz.Core            (net10.0, sin WPF, sin dependencias NuGet)
│  ├─ Color/        Argb, ColorState, Hsv, Hsl, Cmyk, Oklab, Oklch, ColorMath (conversiones), Gamut
│  ├─ Parsing/      ColorParser (todas las entradas de color-model)
│  ├─ Formatting/   IColorFormatter + registro (HEX, rgb(), hsl(), oklch(), C#, XAML, Dart, ARGB, "Copiar todo")
│  ├─ Generation/   Harmonies, TintShade, DesignScale, Neutrals, DominantColors (k-means)
│  ├─ Palettes/     Palette, PaletteColor, PaletteService (operaciones puras)
│  ├─ Export/       IPaletteFormatter + registro (CSS, JSON, Dart, C#, Tailwind), IdentifierNaming
│  └─ Persistence/  JsonStore<T> (escritura atómica, versión, recuperación), PaletteRepository, HistoryRepository, SettingsRepository
├─ src/Matiz.App             (net10.0-windows, WPF, CommunityToolkit.Mvvm)
│  ├─ App.xaml(.cs)             raíz de composición manual (sin contenedor DI), instancia única
│  ├─ Services/     CurrentColorService, ClipboardService, ThemeService, HotkeyService, DialogService, ToastService
│  ├─ ViewModels/   MainViewModel, PickerViewModel, CurrentColorViewModel, GeneratedPalettesViewModel,
│  │                ActivePaletteViewModel, PaletteLibraryViewModel, HistoryViewModel, SettingsViewModel, ImagePickerViewModel
│  ├─ Views/        MainWindow + UserControls por zona, paneles laterales
│  ├─ Controls/     ColorWheel, BrightnessSlider, GrayStrip, Swatch, Magnifier (controles custom, sin MVVM interno)
│  ├─ ScreenCapture/ ScreenPickerController, MonitorInfo, DesktopSnapshot, OverlayWindow
│  ├─ Imaging/      PaletteImageRenderer (DrawingVisual → PNG), ImageLoader (WIC)
│  ├─ Interop/      NativeMethods (LibraryImport: user32/gdi32/shcore)
│  └─ Themes/       Tokens.Light.xaml, Tokens.Dark.xaml, Controls.xaml
└─ tests/Matiz.Core.Tests    (xUnit)
```

Por qué solo dos proyectos: Core contiene todo lo que es lógica y se puede testear sin Windows; App contiene todo lo que depende de WPF/Win32. Separar "Infrastructure", "Application", "Domain" en proyectos distintos añadiría ceremonia sin beneficio a este tamaño. La persistencia vive en Core porque solo usa `System.IO`/`System.Text.Json` y así se testea con directorios temporales.

Dependencias: `CommunityToolkit.Mvvm` (generadores de `ObservableProperty`/`RelayCommand`, elimina boilerplate, ~sin coste en runtime); `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`. P/Invoke manual con `LibraryImport` (≈15 funciones) en lugar de CsWin32 para no añadir generadores. `System.Text.Json` con source generation. Sin `System.Drawing`.

### D5. Screen picker: instantánea congelada + un overlay por monitor

Alternativas:
- **A. Live (estilo PowerToys):** ventana-lupa que sigue al cursor + hook global de ratón `WH_MOUSE_LL` para interceptar el click + `BitBlt` de una región pequeña en cada movimiento, excluyendo la lupa con `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)`. Ventaja: contenido animado sigue vivo. Inconvenientes: hook global (más frágil, algunos antivirus lo vigilan, se desactiva si el hilo tarda >~300 ms), gestión de foco compleja, el click debe "tragarse" para que no llegue a la app de debajo.
- **B. Congelada (estilo ShareX/Snipping):** ✅ al activar se copia cada monitor con `BitBlt(..., SRCCOPY | CAPTUREBLT)` en coordenadas físicas; se abre una `OverlayWindow` topmost sin bordes por monitor que muestra esa instantánea 1:1 y procesa ratón/teclado de forma normal.

**Decisión: B.** Es determinista, no requiere hooks globales, la lupa se dibuja desde un bitmap en memoria (instantánea, sin E/S por movimiento), facilita las flechas píxel a píxel y es testeable. Coste aceptado: el contenido animado queda congelado en el instante de activación (para elegir un color suele ser una ventaja).

Detalles DPI/multi-monitor:
- Manifest `PerMonitorV2` → todas las APIs devuelven píxeles físicos.
- `EnumDisplayMonitors` + `GetMonitorInfo` (rectángulo físico, puede ser negativo) + `GetDpiForMonitor` por monitor.
- Cada `OverlayWindow` se posiciona con `SetWindowPos` en coordenadas físicas tras crear su `HWND` (evita la conversión DIP de `Left/Top`, ambigua con DPI mixto). Su contenido es un `Image` con tamaño `pixelWidth × 96 / dpi` y `BitmapScalingMode=NearestNeighbor`, de forma que 1 píxel de la instantánea = 1 píxel físico. Se gestiona `DpiChanged`.
- La posición del cursor se lee siempre con `GetCursorPos` (físico) y se indexa directamente en la instantánea del monitor: nunca se convierte DIP→píxel, eliminando errores de redondeo.
- Flechas: `SetCursorPos` ±1/±10 físicos.
- Lupa: `CroppedBitmap` de N×N centrado (N impar, 11 por defecto, 7–21 con la rueda del ratón) escalado con vecino más cercano + cuadrícula y recuadro del píxel central dibujados encima; panel con HEX/RGB y muestra. Se reposiciona por cuadrante para no salir del monitor.
- Memoria: ~33 MB por monitor 4K; las instantáneas se liberan al cerrar el modo.
- Hotkey: `RegisterHotKey` sobre un `HwndSource` oculto; `MOD_ALT | MOD_NOREPEAT`, `'C'`. Si falla → aviso + configurable.

### D6. Rueda cromática

- Control custom `ColorWheel` (FrameworkElement): capa 1 = `WriteableBitmap` con el mapa H/S a V=100% en píxeles físicos del DPI actual, generado en un bucle sobre filas (`Parallel.For` si el tamaño lo justifica, ~400×400 = 160 k píxeles → pocos ms); capa 2 = marcador dibujado en `OnRender` (anillo doble blanco/negro para contraste universal).
- Se regenera solo al cambiar tamaño, DPI o enfoque (`γ`). Mover el marcador no toca el bitmap.
- Borde antialiasado calculando cobertura alfa en el último píxel del radio.
- Mapeo: `hue = (atan2(dx, -dy) en grados + 360) mod 360` (0° arriba, sentido horario); `r = min(dist/R, 1)`; `S = r^γ`; inverso para posicionar el marcador `r = S^(1/γ)`.
- **Enfoque (amplitud):** un slider `k ∈ [-1, 1]`, `γ = 2^(1.25·k)` → `γ ∈ [0.42, 2.38]`; `k=0` lineal; `k>0` "Pastel" (más radio para S baja); `k<0` "Vivo".
- **Zoom de la rueda: no se implementa como control.** El requisito "zoom manteniendo centro=0% y borde=100%" equivale a hacer la rueda más grande; se cubre haciendo que la rueda escale con la ventana, con arrastre fino con `Shift` (×0.25), flechas y campos numéricos. Un zoom con desplazamiento (pan) haría perder de vista el centro y complica la UI con poco beneficio sobre el enfoque. Queda en backlog.
- `BrightnessSlider`: `LinearGradientBrush` de 2 paradas `HSV(H,S,1)` → negro. Como V escala linealmente los canales sRGB codificados, el degradado de 2 paradas en interpolación sRGB es exactamente la rampa de V.
- `GrayStrip`: degradado blanco→negro con marcas 100…0.

### D7. Algoritmo de la Design Scale

1. Convertir el base a OKLCH `(L₀, C₀, h₀)`.
2. Curva de referencia de L (inspirada en Tailwind v4, ajustable con tests "golden"):
   `50:0.975, 100:0.945, 200:0.89, 300:0.82, 400:0.72, 500:0.63, 600:0.55, 700:0.47, 800:0.39, 900:0.32, 950:0.24`.
3. Anclaje: paso `a` = 500 (por defecto) o el de L de referencia más cercana (automático). Se remapea la curva por tramos lineales para que `L(a) = L₀`, conservando los extremos (si `L₀` es más extremo que el siguiente paso, los extremos se desplazan para mantener ΔL ≥ 0.02).
4. Croma: `C(paso) = C₀ · f(L)`, con `f` en campana que reduce croma en los extremos (p. ej. `f = 1 − (|L − L₀| / span)^2 · 0.6`, ajustable).
5. Hue constante `h₀`.
6. Ajuste a gama: búsqueda binaria de la máxima C ≤ objetivo que cae en sRGB [0,1] (L y h fijos).
7. El paso ancla se fuerza exactamente al ARGB del base.

Neutros: mismos L de referencia, `C = min(0.02, C₀·0.1)`, hue `h₀`.
Monocromática: 5 pasos tomados de la escala (100, 300, ancla, 700, 900).
Armonías: rotación de `h` en OKLCH, mismo L y C, con ajuste a gama del paso 6.
Dominantes (fase 7): redimensionar a ≤ 128 px del lado mayor, k-means en OKLab con k-means++ y semilla fija, 10 iteraciones, ordenar por población; en `Task.Run`.

### D8. Modelo de datos y persistencia

`palettes.json`:
```json
{
  "schemaVersion": 1,
  "activePaletteId": "7f9c…",
  "palettes": [
    {
      "id": "7f9c…",
      "name": "PuchiApp",
      "description": "Colores de la app",
      "createdAt": "2026-10-08T12:00:00Z",
      "modifiedAt": "2026-10-08T12:30:00Z",
      "colors": [
        { "id": "a1…", "name": "Primary", "hex": "#5246BC" },
        { "id": "b2…", "name": "Secondary", "hex": "#FF8A00", "alpha": 255 }
      ]
    }
  ]
}
```
- `hex` siempre `#RRGGBB` (legible y sin ambigüedad de orden ARGB/RGBA); `alpha` opcional, omitido si 255.
- `history.json`: `{ "schemaVersion": 1, "colors": ["#5246BC", …] }`. `settings.json`: objeto plano con versión.
- Un archivo para todas las paletas: volumen pequeño (cientos de colores), operación atómica simple. SQLite/LiteDB no aportan nada aquí.
- `JsonStore<T>`: escribe en `*.tmp` y `File.Replace` (con `.bak`), serializa en un hilo de fondo con *debounce* de ~300 ms; lectura con recuperación (renombra corruptos) y migraciones por `schemaVersion`.

### D9. Formatos extensibles

```csharp
interface IColorFormatter   { string Id; string DisplayName; string Format(Argb c, FormatOptions o); }
interface IPaletteFormatter { string Id; string DisplayName; string Format(PaletteExportModel p, FormatOptions o); }
```
Registros estáticos en Core (`ColorFormatters.All`, `PaletteFormatters.All`); la UI construye los menús "Copiar como" iterando el registro. `PaletteExportModel` = nombre + lista `(nombre, Argb)` → sirve igual para paletas guardadas, generadas y escalas. `IdentifierNaming` produce kebab/camel/Pascal y resuelve colisiones.

### D10. Exportar imagen

`PaletteImageRenderer` compone con `DrawingVisual` (bloques, `FormattedText`), renderiza con `RenderTargetBitmap` a 96 DPI × escala y codifica con `PngBitmapEncoder`. Bloques rellenados con `SolidColorBrush` sin antialiasing en el interior (garantiza píxel exacto, spec `palette-export`). Diálogo de vista previa = el mismo renderer a escala 1× dentro de un panel.

### D11. Interfaz

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ● Matiz         [◎ Capturar  Alt+C] [🖼 Imagen]            [📌] [◐] [⚙]   │
├───────────────────────────────────────────┬──────────────────────────────────┤
│                                    ┌──┐   │  ┌────────┬─────────────────────┐│
│            ╭──────────╮            │  │   │  │Anterior│       Actual        ││
│         ╭──╯          ╰──╮         │B │   │  └────────┴─────────────────────┘│
│        │    RUEDA H + S   │        │r │   │  [ #5246BC                  ] ⧉  │
│        │    (centro blanco)│       │i │   │  RGB   82, 70, 188           ⧉  │
│         ╰──╮     ◎    ╭──╯         │l │   │  HSL   246°, 47%, 51%        ⧉  │
│            ╰──────────╯            │l │   │  HSV   246°, 63%, 74%        ⧉  │
│                                    └──┘   │  CMYK* 56%, 63%, 0%, 26%     ⧉  │
│  Enfoque  Vivo ───────●─────── Pastel     │  [Copiar todo] [Copiar como ▾]  │
│  H [246.1]°   S [63] %   B [74] %         │  [Gris equivalente] [Más ▾]     │
│  Grises 100 ░░░░▒▒▒▒▓▓▓▓████ 0            │                                  │
├───────────────────────────────────────────┴──────────────────────────────────┤
│ Recientes  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■ ■                                    │
├──────────────────────────────────────────────────────────────────────────────┤
│ [Escala] Armonías▾  Tints/Shades  Neutros          [+ Agregar todo] [⧉ ▾]    │
│  50  100  200  300  400 [500] 600  700  800  900  950                        │
│  ██  ██   ██   ██   ██  [██]  ██   ██   ██   ██   ██                         │
├──────────────────────────────────────────────────────────────────────────────┤
│ Paleta ▾ PuchiApp │ ■Primary ■Secondary ■Success ■Warning  [+]   [⧉ ▾] [PNG] │
└──────────────────────────────────────────────────────────────────────────────┘
```
- Jerarquía alineada con las prioridades: seleccionar (izquierda, la zona más grande) → entender/copiar (derecha) → generar (centro-abajo) → guardar/exportar (barra inferior).
- La pestaña por defecto del panel generado es **Escala** (la más útil para design systems y, de paso, muestra el recorrido blanco→color→negro). Armonías es un desplegable con los 5 tipos + Monocromática para no saturar de pestañas.
- "Paleta ▾" abre un panel lateral con la biblioteca de paletas (buscar, crear, renombrar, duplicar, eliminar, descripción). ⚙ abre el panel lateral de ajustes. "Imagen" sustituye la zona de la rueda por el visor de imagen con botón "← Selector".
- Superficies: grises neutros (oscuro `#1E1E1E/#2A2A2A`, claro `#F5F5F5/#FFFFFF`), un único acento neutro; el color actual es el único elemento cromático grande.
- Notificaciones: toast discreto inferior ("Copiado #5246BC").

## Risks / Trade-offs

- [Overlay incorrecto con DPI mixto / coordenadas negativas] → overlay por monitor posicionado en píxeles físicos, cursor leído con `GetCursorPos`, spike al inicio de la fase 3 y matriz de pruebas manual (100%+150%, monitor a la izquierda/encima, vertical, 3 monitores).
- [`Alt+C` choca con aceleradores de menú de otras apps (p. ej. menús con "&C")] → mientras Matiz corre, el atajo global gana; es configurable y se avisa si no se puede registrar. Alternativa sugerida en ajustes: `Ctrl+Alt+C` o `Win+Shift+C`.
- [HDR / contenido protegido / pantalla completa exclusiva: valores de `BitBlt` no fieles o negros] → limitación documentada; DXGI Desktop Duplication queda en backlog.
- [Contenido animado congelado en la captura] → aceptado (ver D5); si molesta, el modo live es una evolución local de `ScreenPickerController`.
- [Memoria con varios monitores 4K/8K] → captura por monitor, liberación inmediata; ~33 MB/4K.
- [Calibración subjetiva de la Design Scale] → curva y campana de croma en constantes, tests golden con colores de referencia, revisión visual en fase 4.
- [Pérdida de hue en grises/negro] → `ColorState` HSV preserva coordenadas (D3) + tests.
- [Deriva por redondeo entre formatos] → canónico 8 bits + tests de ida y vuelta para los 16,7 M de colores en HEX↔HSV/HSL (pocos segundos) y muestreo aleatorio para OKLCH.
- [Portapapeles bloqueado (`CLIPBRD_E_CANT_OPEN`)] → reintentos cortos + toast de error.
- [Rendimiento del render WPF con la UI actualizándose por movimiento] → coalescencia por frame (D3), bitmap de rueda en caché, paletas = funciones puras de microsegundos.
- [Crecimiento de alcance] → backlog explícito (abajo) y fases con entregables cerrados.
- [Tema Fluent integrado de WPF aún experimental] → estilos propios mínimos como plan base.

## Migration Plan

No aplica (producto nuevo). Distribución inicial: `dotnet publish` framework-dependent win-x64 con ReadyToRun (arranque rápido); instalador/MSIX y auto-actualización quedan fuera del MVP.

## Backlog (fuera del MVP, documentado para no perderlo)

Zoom/pan de la rueda; rueda perceptual OKHSV; slider de alpha en UI; checker de contraste WCAG (texto blanco/negro sobre el color) — pequeño y de alto valor, candidato a v1.1; icono en bandeja/minimizar a bandeja; atajos de ventana configurables; importar/exportar paletas (.json, .ase, .gpl); SCSS/JS/Material como formatos; captura DXGI/HDR; modo captura "live".

## Notas de implementación (aplicadas durante el desarrollo)

- **Tema:** no se adoptó el tema Fluent integrado de WPF (`ThemeMode`, marcado como experimental WPF0001 y con estilos que tiñen superficies); se usan tokens propios `Themes/Dark.xaml` / `Light.xaml` + `Controls.xaml`, barra de título oscura vía `DwmSetWindowAttribute`.
- **Interop:** `DllImport` en lugar de `LibraryImport` (el callback de `EnumDisplayMonitors` necesita delegado; mantener un único estilo es más simple).
- **Campo HEX = campo libre:** el campo principal acepta HEX y cualquier formato del parser (rgb(), hsl(), 0xAARRGGBB…), cubriendo el "campo pegar/escribir" sin un control extra.
- **Design Scale:** si "Base en 500" es geométricamente imposible (no caben ΔL ≥ 0.02 por paso, p. ej. blanco o negro puros) se usa el anclaje automático. Constantes finales: curva `0.975, 0.945, 0.89, 0.82, 0.72, 0.63, 0.55, 0.47, 0.39, 0.32, 0.24`, caída de croma 0.6 en los extremos.
- **Neutros:** usan la curva de referencia (no la remapeada al color base) para que un base claro no produzca neutros casi blancos; croma `min(0.015, C·0.12)`.
- **Atajos:** tabla central `Services/Shortcuts.cs`; `Ctrl+Shift+Z` también rehace.
- **Diagnóstico:** excepciones no controladas se registran en `error.log` de la carpeta de datos. `MATIZ_DATA_DIR` redefine la carpeta de datos (pruebas).

## Decisiones confirmadas en la revisión

- **Q1. Brillo:** confirmado. La rueda se dibuja siempre a brillo 100%; el control de brillo va del color al 100% hasta negro (0%). Ir hacia blanco = mover el punto hacia el centro.
- **Q2. Atajos:** `Ctrl+S` = agregar color actual a la paleta activa (las paletas se guardan solas). Además, botón "Capturar" destacado como acción principal de la barra superior para capturar en cualquier punto del escritorio (spec `screen-picker`).
- **Q3. Tras capturar:** se copia automáticamente en el **formato principal** configurado (por defecto HEX), el mismo de `Ctrl+C`. "Mostrar ventana tras capturar" activo por defecto, desactivable.
- **Q4. Design Scale:** se mantiene "Base en 500" por defecto (pedido original) con opción "Automático"; el algoritmo garantiza ΔL ≥ 0.02 entre pasos incluso con bases extremas.
- **Q5. HEX de 8 dígitos:** `#AARRGGBB`.
- **Q6. Nombre:** **Matiz** — en español "matiz" es literalmente *hue* y también *nuance*; corto, pronunciable en inglés, y describe la herramienta. Proyectos `Matiz.Core`, `Matiz.App`, `Matiz.Core.Tests`; datos en `%APPDATA%\Matiz`. Antes de publicar conviene comprobar que el nombre no colisiona con otra app en Microsoft Store/winget.

Los valores numéricos de los ejemplos originales son ilustrativos; las specs usan valores calculados con las fórmulas definidas en `color-model` y los tests son la referencia.
