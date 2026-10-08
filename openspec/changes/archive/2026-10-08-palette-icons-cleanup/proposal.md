## Why

Quedaron tres detalles de UX a pulir tras la change de tarjetas y armonías: la cabecera de "Paleta activa" tiene un botón "+" duplicado (ya existe "+ Paleta" en la zona superior del color actual), borrar un color de la paleta activa exige abrir el menú contextual y los iconos de las tarjetas generadas quedaron con visibilidad invertida respecto de lo que el usuario prefiere (estrella de "usar como principal" siempre visible en vez de un ojo solo al pasar el mouse).

## What Changes

- Se elimina el botón "+" de la cabecera de "Paleta activa"; la vía para agregar el color actual queda en el botón "+ Paleta" de la zona superior (o `Ctrl+S`). No cambia ningún comando ni atajo, solo se saca la duplicación.
- Cada color de la "Paleta activa" muestra un tachito (X) discreto de borrado al pasar el mouse, con la misma acción que la opción "Eliminar" del menú contextual.
- Tarjetas generadas (todas las pestañas: escalas, armonías, tints, etc.): los iconos "Copiar HEX" y "Agregar a paleta" pasan a estar SIEMPRE visibles; "Usar como color principal" cambia el icono de estrella a ojo y se muestra SOLO al pasar el mouse sobre la tarjeta.

## Capabilities

### New Capabilities

(ninguna)

### Modified Capabilities

- `saved-palettes`: "Operaciones de gestión" — agregar el color actual se documenta vía botón "+ Paleta" (ya sin botón duplicado en la cabecera de la paleta activa) y eliminar un color suma la vía del tachito de borrado al pasar el mouse sobre la muestra.
- `palette-generation`: "Presentación y acciones de tarjetas" — visibilidad de los iconos de acción (copiar y agregar siempre visibles; usar como principal, ojo, solo al hover).

## Impact

- Código: `src\Matiz.App\Views\MainWindow.xaml` (cabecera de Paleta activa, plantilla `PaletteColorItem`, plantilla `SwatchItem`); `src\Matiz.App\Localization\Strings.resx` y `Strings.es.resx` (claves `palette.addCurrent.*` quedan fuera de uso al quitar el botón). Sin cambios en Core ni en ViewModels (comandos existentes reutilizados: `AddCurrentToPaletteCommand`, `RemovePaletteColorCommand`, `CopySwatchHexCommand`, `AddSwatchToPaletteCommand`, `UseSwatchCommand`).
- Specs: `saved-palettes` y `palette-generation` (deltas MODIFIED).