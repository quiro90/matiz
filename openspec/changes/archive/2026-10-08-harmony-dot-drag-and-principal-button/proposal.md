## Why

Hoy un click accidental sobre cualquier tarjeta del panel de paletas convertidas convierte ese color en el **color actual**: la rueda se mueve, el brillo cambia y toda la armonía se regenera, destruyendo el trabajo en curso. Además, los puntos de la armonía en la rueda cromática no son ajustables: solo siguen al marcador principal, sin forma de personalizar un color de la armonía desde la propia rueda.

## What Changes

- **Puntos de armonía arrastrables en la rueda** (solo pestaña Armonías): arrastrar un punto secundario ajusta su hue y saturación de forma independiente, **sin mover el marcador principal**; la tarjeta correspondiente del panel inferior se actualiza en vivo. La rueda mantiene **exactamente** los puntos definidos por el tipo de armonía (un ajuste nunca agrega ni quita puntos).
- **Desfases personalizados**: cada punto arrastrado conserva su desfase respecto de la posición canónica y sigue a la armonía cuando el principal se mueve (armonía personalizada). Los desfases vuelven a la posición canónica cuando el color principal cambia por otra vía (botón "principal", historial, captura, entrada manual, deshacer) o al cambiar el tipo de armonía. Doble click sobre un punto reinicia ese punto a su posición canónica **sin** cambiar el color principal.
- **Click sobre la tarjeta ya no cambia el color actual**: pasa a **copiar en el formato principal** (y, por la política ya existente de historial, el color queda registrado en Recientes). Aplica a todas las pestañas del panel (Escala, Armonías, Tints/Shades, Neutros, Extraídas).
- **Botoncito "principal" siempre visible** abajo a la derecha de cada tarjeta (icono con tooltip "Usar como color principal"): la **única** vía desde el panel para convertir un color generado en el color actual.
- Los mini-swatches de Recientes conservan su comportamiento actual (click = usar como color actual).
- Los puntos de armonía en la rueda siguen mostrándose **solo** en la pestaña Armonías: en las demás pestañas los colores comparten hue/saturación (varían en brillo/gris) y los puntos se superpondrían en la misma posición.

## Capabilities

### New Capabilities

(ninguna)

### Modified Capabilities

- `palette-generation`: se modifican los requisitos "Armonías visibles en la rueda" (arrastre con desfase, doble click reinicia el punto) y "Presentación y acciones de tarjetas" (click copia en formato principal; nuevo botón "principal" como única vía para usar el color); se agrega el requisito nuevo "Desfases personalizados de la armonía".

## Impact

- **Matiz.App**:
  - `Controls/ColorWheel.cs`: hit-test y arrastre de puntos secundarios (distinguir click / doble click / arrastre), nuevos comandos de drag y reset; el hit-test del marcador principal conserva prioridad.
  - `ViewModels/MainViewModel.GeneratedPalettes.cs`: estado de desfases por punto (por tipo de armonía, en vivo), comandos para arrastrar/reiniciar punto; `UseSwatch` queda solo para el botón "principal" e historial.
  - `Views/MainWindow.xaml`: plantilla de tarjeta — click del cuerpo copia en formato principal, botoncito "principal" siempre visible abajo a la derecha, tooltips actualizados.
  - `Localization/Strings*.resx`: nuevo texto del botón "principal" y tooltip de copia.
- **Matiz.Core**:
  - `Generation/PaletteGenerator.cs`: overload de `Harmony(...)` que acepta desfases por punto (hue/saturación) y los aplica a las coordenadas de rueda y al color resultante.
- **Tests** (`Matiz.Core.Tests`): casos de armonía con desfases (conserva saturación/croma, clamp a gama, seguimiento rígido al mover el principal).
- Sin nuevas dependencias. Los desfases son estado en memoria de la sesión (no se persisten en %APPDATA%\Matiz).