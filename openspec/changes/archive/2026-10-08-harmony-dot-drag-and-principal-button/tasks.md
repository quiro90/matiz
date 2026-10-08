## 1. Núcleo (Matiz.Core): armonía con desfases

- [x] 1.1 Extender `PaletteGenerator.Harmony(ColorState, kind, anchor, balanceLightness)` con parámetro opcional `IReadOnlyList<(double HueDelta, double SatDelta)>? colorOffsets` (índice alineado al color generado; el del base se ignora): ángulo canónico + Δhue (normalizado), saturación del base + Δsat (clamp 0–1), regla de brillo vigente, coordenadas personalizadas en `WheelHue/WheelSaturation`. Verificación: tests nuevos en `Matiz.Core.Tests` — triádica de hue 246.1°/S 0.63 con Δ(10°, +0.05) en el índice de +120° produce hue exacto 246.1+130° (normalizado), S 0.68, sRGB válido y base intacta; `dotnet test`
- [x] 1.2 Test de compatibilidad: con `colorOffsets` nulo o todo cero, la salida es idéntica bit a bit al overload actual (requisito "Sin desfase"). Verificación: `dotnet test`

## 2. Estado y comandos en el ViewModel

- [x] 2.1 Añadir estado de desfases por punto alineado a `HarmonyOffsets(kind)` en `MainViewModel.GeneratedPalettes.cs`, con reinicio: cambio de tipo de armonía y todo cambio Confirmado de la sesión con origen distinto de `Picker` (botón "principal" = `Generated`, historial, captura, imagen, manual, grises, deshacer/rehacer). Los `Preview` (arrastre del principal) no reinician. Verificación: build OK; prueba manual — arrastrar punto, mover principal (el punto conserva desfase), usar "principal" en una tarjeta (todos los puntos vuelven a canónicos)
- [x] 2.2 Pasar el estado a `PaletteGenerator.Harmony` en `RefreshGenerated` y añadir comandos `SetWheelMarkerOffset(índice marcador, hue, sat)` (mapea índice de marcador → índice de tarjeta vía `_markerToSwatch`, calcula Δ contra la posición canónica) y `ResetWheelMarkerOffset(índice)` para el doble click. Verificación: build OK; en Armonías el color de la tarjeta cambia en vivo contra un desfase fijado desde el comando

## 3. Rueda (ColorWheel): arrastre y doble click de puntos

- [x] 3.1 En `ColorWheel.cs`: nuevo DP `MarkerDragCommand` (récord con índice + hue + sat de la posición del cursor) y `MarkerResetCommand` (reemplaza a `MarkerActivateCommand` para doble click); captura de mouse en down sobre punto con umbral de arrastre ~4–5 px; click simple sin arrastre → `MarkerClickCommand` (selección + toast, como hoy); posición ajustada para no salir del disco; prioridad de hit-test del marcador principal intacta. Verificación: build OK; comportamiento observable — click selecciona/copiar; arrastre mueve el punto y su tarjeta sin mover el principal ni el color actual; doble click reinicia el punto; arrastre iniciado sobre el principal mueve el color actual
- [x] 3.2 Enlazar los nuevos comandos en `MainWindow.xaml` (rueda). Verificación: build OK

## 4. Panel de tarjetas: click = copiar, botón "principal"

- [x] 4.1 En la plantilla de `SwatchItem` (`MainWindow.xaml`): el click del cuerpo pasa a `CopySwatchDefaultCommand` (formato principal) con tooltip nuevo "Copiar en el formato principal"; nuevo botón "principal" siempre visible abajo a la derecha (icono 18 px, tooltip "Usar como color principal", comando `UseSwatchCommand`); iconos hover de copiar/agregar desplazados para no superponerse; menú contextual mantiene su opción "Usar como color principal". Vale para las 5 pestañas (misma plantilla). Verificación: build OK; manual — click sobre tarjeta copia el formato principal con toast y agrega el color a Recientes sin mover rueda ni regenerar paletas; el botón convierte la tarjeta en color actual; en la pestaña Armonías el click en tarjeta ya no resetea los desfases a canónicos (solo el botón "principal" lo hace)
- [x] 4.2 Textos: reetiquetar la clave existente de "usar como color actual" a "Usar como color principal" (Usar como color principal) en `Strings.resx` (en) y `Strings.es.resx` (Tarjetas, Recientes y menús), y añadir la clave del tooltip de copia. Verificación: build OK; app arranca y muestra los textos en ambos idiomas

## 5. Verificación integral

- [x] 5.1 Suite completa y escenarios del spec: `dotnet build Matiz.sln` + `dotnet test`; recorrido manual de los escenarios de `specs/palette-generation/spec.md` (seguimiento rígido, reinicios por otra vía, saturación en gama, sin desfase = comportamiento actual, prioridad del principal, click tarjeta = copiar, botón principal = usar). Verificación: salida de tests verde y behaviors observables confirmados