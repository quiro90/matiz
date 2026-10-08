## Context

Hoy `ColorWheel` dibuja los puntos de la armonía (`WheelMarker`) como elementos no interactivos para arrastre: `MarkerClickCommand` (seleccionar + toast copiar) y `MarkerActivateCommand` (doble click → convertir en color actual, sin arrastre posible). El panel de tarjetas (`MainWindow.xaml`, plantilla de `SwatchItem`) enlaza el click del cuerpo al comando `UseSwatchCommand`, que hace `Session.Commit(...)` y por tanto mueve rueda, brillo y regenera toda la armonía. La posición de cada punto sale de `PaletteGenerator.Harmony`, que gira el hue del base según `HarmonyOffsets(kind)` y conserva la saturación del base; las coordenadas exactas viajan en `GeneratedColor.WheelHue/WheelSaturation`.

## Goals / Non-Goals

**Goals:**
- Arrastrar un punto de armonía lo ajusta con un desfase (hue, saturación) respecto de su posición canónica, en vivo, sin tocar el color actual.
- El desfase sobrevive al movimiento del principal (rueda/brillo) y se reinicia cuando corresponde según el spec.
- El panel pasa a ser "solo lectura + acciones explícitas": click copia en formato principal; único camino al color actual es el botón "principal" (o su equivalente de menú).
- Comportamiento del panel uniforme en las 5 pestañas generadas, sin cambios en Recientes.

**Non-Goals:**
- Puntos de armonía en pestañas distintas de Armonías (se superpondrían: comparten hue/saturación).
- Persistir desfases entre sesiones ni historial de desfases (estado efímero en memoria).
- Editar un color de la armonía desde el panel inferior (solo desde la rueda).
- Arrastre de puntos por teclado (los puntos no son enfocables; el teclado mueve el principal como hoy).

## Decisions

1. **Desfases como delta respecto de la posición canónica, en coordenadas de rueda (Δhue en grados, Δsaturación en fracción).** Alternativa considerada: guardar el punto absoluto (hue, sat) por índice. Con delta, mover el principal no requiere recalcular nada extra: `PaletteGenerator.Harmony` aplica `ángulo canónico + Δhue` y `saturación del base + Δsat` (clamp 0–1) y las coordenadas personalizadas viajan ya en `WheelHue/WheelSaturation`. Con punto absoluto habría que recanonizar en cada frame de arrastre del principal para conservar el desfase (más frágil).

2. **El generador (Matiz.Core) aplica los desfases**, no el ViewModel. `Harmony(ColorState baseState, HarmonyKind kind, ScaleAnchorMode anchor, bool balanceLightness, IReadOnlyList<(double HueDelta, double SatDelta)>? colorOffsets = null)` — el índice del par corresponde al índice del color generado (el del base se ignora). Con desfases nulos o todos cero, la salida es bit a bit la de hoy (requisito "Sin desfase es comportamiento actual"). Testeable en `Matiz.Core.Tests` sin WPF. Alternativa: post-proceso en el VM (duplicaría la lógica de brillo/gama fuera de Core).

3. **Estado de desfases en el VM**: un arreglo `(double dHue, double dSat)[]` alineado a `HarmonyOffsets(kind).Count`, vacío/nulo ⇒ sin desfases. Se reinicializa (todo cero) cuando: (a) el tipo de armonía cambia, (b) la sesión reporta un cambio **Confirmado con origen distinto de `Picker`** (botón "principal" = `Generated`, historial, captura, imagen, manual, grises, deshacer/rehacer, "anterior"), (c) cambio de "Luminosidad equilibrada" **no** reinicia (solo cambia la regla de brillo). Los `Preview` (arrastre del principal) no reinician. Alternativa: reiniciar también con el toggle de brillo — se descarta porque el desfase es una decisión geométrica del usuario, ajena al brillo.

4. **Interacción en la rueda (ColorWheel) con captura de mouse y umbral de arrastre (~4–5 px)**:
   - `MouseLeftButtonDown` sobre un punto: captura; si `ClickCount ≥ 2` → comando de reinicio del punto (reemplaza al actual `MarkerActivateCommand`, que ya no convierte en color actual). Si es click simple, queda en "candidato a arrastre".
   - `MouseMove` sin umbral superado: nada. Superado: marca arrastre del índice capturado y, por cada movimiento, ejecuta un comando nuevo `MarkerDragCommand` con un récord `(índice, hue, sat)` derivado de `WheelMapping.FromPoint`, con la posición ajustada para no salir del disco (mismo clamping que usa hoy el arrastre del principal con su punto virtual).
   - `MouseLeftButtonUp`: si hubo desplazamiento menor al umbral → `MarkerClickCommand` (seleccionar + toast copiar, como hoy); si fue arrastre → nada más (sin commit, sin toast).
   - El hit-test conserva la prioridad del marcador principal (`MainHitRadius`) y el índice queda capturado al presionar (el punto se mueve bajo el cursor durante el arrastre).

5. **Tarjeta: click del cuerpo → `CopySwatchDefaultCommand`** (formato principal del usuario, que además registra en Recientes por la política vigente de `color-history`); tooltip nuevo "Copiar en el formato principal". El botón "principal" (18 px, siempre visible, abajo a la derecha de la muestra; los iconos de copiar/agregar que hoy aparecen en hover se desplazan para no superponerse) usa el comando existente `UseSwatchCommand` con tooltip nuevo "Usar como color principal"; el menú contextual de la tarjeta mantiene su opción equivalente con el mismo texto. Se reutiliza una única clave de texto renombrada para "usar como color principal" (tarjetas, Recientes y menús) en `Strings.resx` / `Strings.es.resx`. Alternativa: pill con texto "Principal" — se descarta por espacio (tarjeta de 58 px) y ruido visual en 5 pestañas.

6. **Los desfases no entran ni en deshacer ni en persistencia**: la pila de deshacer registra cambios del color actual (`ColorSession`), que el arrastre de puntos no toca; `%APPDATA%\Matiz` no cambia. Reinicio completo al abrir la app (nuevos desfases nulos).

## Risks / Trade-offs

- [Arrastre de punto vs. click de selección] → umbral de arrastre pequeño pero suficiente (~4–5 px) y estado "índice capturado desde el down"; el click simple sigue funcionando igual que hoy.
- [Doble click ahora reinicia puntos: quien esperaba convertir en color actual] → la vía deliberada existe y queda documentada en el botón "principal" de la tarjeta; el toast de selección del click simple ya muestra el valor y "Copiar".
- [Figura de la armonía deja de ser canónica con desfases] → es deliberado y solo visible tras un ajuste manual; con desfases a cero la geometría exacta del spec se mantiene sin cambios.
- [Rendimiento en arrastre de punto] → mismo patrón que el arrastre del principal: `RefreshGenerated` actualiza los contenedores en sitio y el bitmap de la rueda no se regenera (solo `InvalidateVisual`).
- [Brillo personalizado durante drag de punto] → el brillo se recalcula con la regla vigente ("Luminosidad equilibrada" busca por búsqueda binaria, ~40 iteraciones por color y frame: despreciable, igual que hoy en cada frame de arrastre del principal).