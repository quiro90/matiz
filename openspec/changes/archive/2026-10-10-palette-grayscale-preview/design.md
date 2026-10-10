## Context

Ver proposal.md (Why). Estado actual relevante:

- La barra "Paleta activa" (`MainWindow.xaml` ~L602) es una fila de 3 columnas: botón biblioteca (col 0), muestras con scroll (col 1, altura ~62px: muestra 38 + campo de nombre), y Copiar + Exportar centrados verticalmente (col 2, ~28px por botón). La tarjeta puede crecer ~20px sin tocar la ventana, pero el usuario pidió explícitamente **no agrandar la barra**.
- La mezcla a gris ya existe: `ColorMath.GrayEquivalent(Argb)` (gris SRGB con el Oklab L del color; el mismo del botón "Gris equivalente" del picker).
- Los 3 caminos de exportación de la paleta activa toman los datos de `PaletteExportModel.From(Palette)` (record `PaletteExportModel(Name, IReadOnlyList<PaletteExportColor>)`, en `Matiz.Core/Export/PaletteFormatters.cs`): `CopyActivePaletteAs` (formatos del registro), `ExportActivePaletteImage` (PNG) y `ExportActivePaletteOverlay`. Los renderers de imagen ya pintan lo que el modelo trae (bloques + textos HEX/RGB).
- Las muestras de la paleta activa son `PaletteColorItem` (`Items.cs`): `Brush`/`Foreground` fijados en el ctor y `Hex` derivado de `Color`; los bindings de la vista apuntan a esos campos.
- La app ya usa popups flotantes para acciones de columna derecha (Copiar/Exportar abren `ContextMenu` vía `OpenMenu_Click`), y ya tiene plantillas propias de `Slider` (horizontal y vertical) en `Themes/Controls.xaml`.
- Límite de 64 colores por paleta: refrescar ≤64 ítems por tick de arrastre es despreciable.

## Goals / Non-Goals

**Goals:**
- Modo de vista continuo 0–100 %, arrastrable, refrescando en caliente las ≤64 muestras.
- Fidelidad total al final de la escala: a 100 % cada color coincide bit a bit con `GrayEquivalent`.
- WYSIWYG en exportación reutilizando el único punto de verdad (`PaletteExportModel`) para no duplicar lógica en cada formato.
- La barra inferior conserva su altura exacta de hoy (requisito del usuario).

**Non-Goals:**
- No persistir el porcentaje en ajustes globales ni como estado separado por vista: el único lugar del valor es la paleta (`grayPercent`); paletas no activas conservan su propio valor hasta que se marquen.
- No alterar colores guardados, Recientes ni las paletas generadas (las fechas de la paleta tampoco: el porcentaje no las mueve).
- No aplicar la mezcla a las acciones individuales por color (menú contextual de una muestra opera sobre el color original).
- No agregar atajos de teclado nuevos ni aplicar el modo a otras vistas (futuras).

## Decisions

1. **Mezcla por canal en sRGB hacia el gris equivalente.** `MixToGray(Argb, amount)` interpola linealmente cada canal RGB (manteniendo alfa del original) entre el color y `GrayEquivalent(c)`. Alternativas: interpolación perceptual en Oklab (más cara, diferencias apenas perceptibles en un preview) o aplicar solo estados binarios (descartado por el usuario). A 0 % es identidad exacta y a 100 % es exactamente `GrayEquivalent`, que satisface los escenarios del spec.

2. **Presentación con campos visibles conmutables en `PaletteColorItem`.** Se agregan `DisplayBrush`, `DisplayForeground` y `DisplayHex` (ObservableProperty) que la vista enlaza en lugar de `Brush`/`Foreground`/`Hex`; el ViewModel recalcula esos campos en cada ítem cuando cambia el porcentaje y al reconstruir la lista (`RefreshActivePalette` pasa el porcentaje vigente). Alternativas: converter global + `MultiBinding` (refresh implícito difícil de controlar) o reconstruir ítems al mover el slider (churn innecesario, perdería estado de renombrado en curso). El arrastre y los menús siguen operando con ítems reales: la vista gris no interfiere con drag & drop.

3. **Export WYSIWYG transformando el modelo una sola vez.** Función única (Core, testeable) que mapea `PaletteExportModel` → `PaletteExportModel` con `Colors = … with { Color = MixToGray(…) }` aprovechando records; los 3 call sites la invocan condicionada al porcentaje > 0. Los formatters y renderers no cambian: reciben colores ya mezclados y por eso muestran HEX/RGB grises sin lógica nueva. Los nombres jamás se transforman (exigencia del spec).

4. **Popup sobre botón compacto.** Bajo Copiar/Exportar (que suben a la parte superior de su columna con `VerticalAlignment="Top"`) va un botón compacto "Escala gris" (misma familia visual que los SmallIconButton, ~20px); al pulsarlo abre un `Popup` con `Placement="Top"`, `StaysOpen=False` sobre la columna — flota sobre las muestras, no modifica la altura de la barra. Contenido: título, Slider 0–100 (plantilla propia existente), porcentaje con dígitos y un acceso mini de restablecer (0 %). El botón marca estado activo (trigger por valor > 0). Alternativas descartadas: fila expandible en línea (crecería la barra, veto del usuario), `ContextMenu` como contenedor (hosting de slider incómodo por foco/selección de menú) y ToggleButton+Popup anclado (mismo resultado con más plantilla; basta `Button` + handler code-behind siguiendo el patrón `OpenMenu_Click`).

5. **Actualización en vivo del porcentaje.** El Slider enlaza a `GrayScalePercent` (int 0–100 en `MainViewModel.Palettes`) con `UpdateSourceTrigger=PropertyChanged`; su setter refresca los ítems visibles y `NotifyCanExecuteChanged` de las órdenes que dependan de presentación (los de export no cambian su habilitación: siempre exportan el estado actual, sea 0 o no).

6. **Localización.** Textos nuevos ("Escala gris", "Escala de grises", "Restablecer", tooltip del modo) en `Strings.resx`/`Strings.es.resx`, siguiendo el patrón `loc:Loc` existente.

7. **Persistencia del porcentaje por paleta.** `Palette.GrayPercent` (`int?`, null = 0 %) es el único lugar del valor: `PaletteService.SetGrayPercent(id, v)` acota 0–100, normaliza 0→null, no toca `ModifiedAt` (estado de vista, no de datos), no-op si el valor no cambia, y emite `Changed` → autoguardado (`JsonStore.ScheduleSave` ya hace debounce de 300 ms, así que el arrastre escribe al asentarse). Durante el arrastre la vista ya se refresca vía `ApplyGrayMix`; para que el `Changed` del servicio no reconstruya ítems en cada tick, la VM marca `_applyingGray` y el handler en `MainViewModel` se salta el `SyncPalettes` (solo guarda). La restauración va unificada por `SyncPalettes` (única ruta de carga/cambio/importación): lee `Active.GrayPercent` en `GrayScalePercent` con guarda de igualdad para evitar bucles. `PaletteFile` incorpora `GrayPercent` opcional mapeado en `From`/`ToPalette`, sin cambio de `schemaVersion` (System.Text.Json omite el campo desconocido de un archivo nuevo al leerlo una app vieja, y lo trata como null al revés). Recargar hacia la rueda carga los colores como se ven (misma mezcla) y no altera la paleta: conserva su porcentaje y sus datos intactos (el acceso es el botón compacto "Recargar colores" en la barra Paleta activa, junto a "Escala gris"; se retiró de la Biblioteca); crear y duplicar tampoco arrastra porcentaje; deshacer recarga/eliminar restaura el valor que la paleta tenía. Alternativas descartadas: persistir solo en ajustes (no viaja con la paleta ni a `.mpalette`) o grabar por tick con rebuild completo (churn de ítems innecesario).

## Risks / Trade-offs

- [Arrastrar el slider recalcula ≤64 muestras por tick] → costo mínimo; sin virtualización necesaria. Medido conceptualmente: creación de ≤64 brushes y strings; el `BrushCache` evita duplicar brushes para el mismo color.
- [Confusión: el HEX mostrado sobre la muestra gris no es el almacenado] → con el modo activo el texto visible acompaña al color visible (WYSIWYG), y el menú contextual copia el original; se documenta en el tooltip del modo.
- [Usuario espera "accesibilidad" en mezclas intermedias] → las mezclas intermedias son un preview estético; solo 0 % y 100 % tienen garantía de coincidencia con el gris perceptual exacto. Se aclara en el tooltip.
- [Popup puede quedar tapado por bordes de pantalla] → `Placement="Top"` con `StaysOpen=False`; altura del popup acotada (~80px), sin overflow horizontal (ancho ~180px).
- [Estado del botón/exports desincronizado entre pestañas] → el porcentaje es global de la sesión y aplica a la paleta activa que se esté viendo; al cambiar de paleta marcada el refresco de ítems reusa el porcentaje vigente (coberturado por "Coherencia en caliente").
- [Escrituras de `palettes.json` durante el arrastre del slider] → `ScheduleSave` debouncea (300 ms): se escribe el valor asentado, no cada tick; archivos atómicos (tmp+replace) como siempre.
- [Porcentaje "pegado" a una paleta confunde al importar paletas viejas] → archivos sin `grayPercent` importan/restauran como 0 %; el campo es opcional y visible en el slider, nunca silencioso sobre los colores.

## Migration Plan

Sin migración: `palettes.json` y ajustes no cambian de esquema. Rollback revert: desactivar la feature eliminando UI y transformación; datos sin efecto.

## Open Questions

Ninguna: control, layout, alcance de export y persistencia (por paleta en `palettes.json`/`.mpalette`, restauración automática, reset al crear/duplicar y acceso de recarga en la barra Paleta activa) quedaron resueltos con el usuario.