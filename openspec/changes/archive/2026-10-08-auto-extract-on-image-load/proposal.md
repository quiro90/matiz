## Why

Hoy al cargar una imagen (diálogo, drag&drop, portapapeles) el usuario solo la ve: para obtener colores predominantes debe pulsar "Extraer colores" manualmente. Es el flujo más común (pegar captura → mirar paleta) y se puede automatizar sin perder el control manual. Además el campo de cantidad (3–10, por defecto 6) carece de etiqueta y solo se entiende por el tooltip.

## What Changes

- Al cargar una imagen, la extracción de colores predominantes SHALL ejecutarse una vez de forma automática con la cantidad vigente (por defecto 6), sin bloquear la UI y con la misma determinismo/desempeño de siempre; el botón "Extraer colores" sigue disponible para re-extraer (por ejemplo tras cambiar la cantidad).
- UI: se agrega la etiqueta "Cantidad de colores:" a la izquierda del NumericBox, quedando `Cantidad de colores: [6] [Extraer colores]`.
- Sin cambios en el algoritmo de extracción (`DominantColors`), en el determinismo ni en las acciones de las tarjetas resultantes.

## Capabilities

### New Capabilities

(ninguna)

### Modified Capabilities

- `image-picker`: "Extraer colores principales" — se documenta la extracción automática al cargar la imagen y la etiqueta del campo de cantidad; se conservan los escenarios existentes (Determinismo, Imagen grande) y se agregan dos (extracción automática al cargar; la cantidad ajustada se respeta).

## Impact

- Código: `src\Matiz.App\ViewModels\MainViewModel.Image.cs` (refactor mínimo: separar `ExtractColorsCoreAsync()` del comando y dispararla desde `ShowImage()`), `src\Matiz.App\Views\MainWindow.xaml` (TextBlock de etiqueta antes del NumericBox), `src\Matiz.App\Localization\Strings.resx` + `Strings.es.resx` (nueva clave `image.extractCount.label`).
- Specs: `image-picker` (delta MODIFIED). Nada en Core ni tests de Core (sin cambios de algoritmo).