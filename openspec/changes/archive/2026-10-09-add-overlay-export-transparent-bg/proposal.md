# Cambio: add-overlay-export-transparent-bg

## Why
Hoy el botón "PNG" de la paleta activa exporta una única imagen clásica (bloques + valores). Quien usa la paleta necesita además una forma de apreciar cómo quedan los colores **superpuestos** uno sobre otro (sin combinarlos, como capas apiladas) y llevársela a PNG; tanto para paletas completas (hasta 64 colores) como para la dupla "Anterior/Actual" del color actual. De paso, el PNG clásico debería admitir fondo transparente.

## What Changes
- El botón "PNG" de la paleta activa pasa a abrir un desplegable con 2 opciones:
  - **"Imagen"** (mismo logo): abre la misma ventana de exportación de hoy, que ahora suma la opción de fondo **"Transparente"** (PNG con canal alfa; sobre transparencia el texto y bordes usan el estilo del modo claro).
  - **"Exportar superpuestos"** (nuevo): abre una **ventana nueva** con vista previa y opciones similares a la exportación de imagen (título, valores por color, fondo claro/oscuro, escala 1×/2×/3×, guardar PNG, copiar imagen) pero renderizando los colores **apilados en capas superpuestas**: forma a elegir entre **cuadrado, círculo o triángulo**, tamaño incremental en % para que **cada capa muestre un borde visible** (64 colores → anillos angostos pero visibles; menos colores → capas visiblemente más anchas). Cada capa es **opaca**: los colores no se combinan ni mezclan.
  - La ventana de superpuestos permite elegir el **orden** de los colores: primero→último o último→primero.
- En la tarjeta "Color actual", debajo de "Copiar todo" (fila de botones superior), nuevo botón **"Exportar superpuestos"** (solo esa opción) que abre la misma ventana de superpuestos con los dos colores "Anterior" y "Actual".
- Textos nuevos localizados (es/en). El atajo `Ctrl+E` sigue abriendo la exportación "Imagen".

## Capabilities

### New Capabilities
- (ninguna)

### Modified Capabilities
- `palette-export`: entra por desplegable (Imagen / Exportar superpuestos); la exportación PNG clásica suma fondo transparente; nuevo modo de exportación "superpuestos" (formas, orden, capa incremental con borde visible, valores y fondo).
- `current-color`: la dupla "Anterior/Actual" del color actual se puede exportar como superpuestos desde su tarjeta.

## Impact
- **Matiz.App**:
  - `Views/MainWindow.xaml`: menú del botón PNG + nuevo botón en la tarjeta de color actual.
  - `ViewModels/IShell.cs` y `Views/MainWindow.xaml.cs`: nueva `ShowExportOverlay(...)`.
  - `ViewModels/MainViewModel.Palettes.cs` y `MainViewModel.Current.cs`: comandos `ExportActivePaletteOverlay` y `ExportCurrentPreviousOverlay`.
  - `Imaging/PaletteImageRenderer.cs`: opción de fondo transparente.
  - Nuevos: `Views/ExportOverlayWindow.xaml(.cs)` y `Imaging/OverlayImageRenderer.cs`.
  - `Localization/Strings.resx` y `Strings.es.resx`: claves nuevas.
- **Tests**: `tests/Matiz.App.Tests` — pruebas del fondo transparente y del renderer de superpuestos.
- Sin cambios en `Matiz.Core`, persistencia ni empaquetado.