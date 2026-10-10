## Why

Al revisar contraste, preparar versiones para impresión/accesibilidad o entregar una versión "sin color", el usuario necesita ver y exportar la paleta activa en escala de grises sin destruir su trabajo ni duplicar la paleta a mano. Hoy eso no existe: no hay forma de previsualizar "cada color, su gris equivalente" ni de copiar/exportar esa vista; habría que rehacer la paleta con valores de gris calculados uno por uno.

## What Changes

- Nuevo modo de vista temporal ("en caliente") en la tarjeta Paleta activa, controlado por una barra deslizante continua de **0 a 100 %** ("Escala de grises"): a medida que se arrastra, cada muestra mezcla su color original con su gris equivalente perceptual (`ColorMath.GrayEquivalent`, mismo gris del botón "Gris equivalente"; Oklab L). A 0 % la paleta se ve exactamente igual que hoy; a 100 % cada muestra muestra su gris equivalente individual.
- Las muestras cambian en vivo (relleno y texto HEX mostrado) según el valor del modo; los colores almacenados **no** se modifican: al volver a 0 % la paleta vuelve exactamente a su estado previo.
- Ajustes de layout en la barra de Paleta activa **sin agrandar su altura**: los botones Copiar/Exportar suben a la parte superior de su columna y debajo se agrega un botón compacto "Escala gris" que, al pulsarlo, abre un panel flotante (popup) con la barra deslizante 0–100 %.
- Exportación WYSIWYG: con el modo en un valor > 0, **toda** exportación de la paleta activa (Copiar como: CSS/JSON/Dart/C#/…, Exportar a imagen PNG y Exportar overlay) entrega los colores mezclados con su gris y muestra los valores resultantes; a 0 % todo sale idéntico a como sale hoy.
- El valor del modo vive solo durante la sesión: no se persiste en ajustes, no afecta a Recientes, a las paletas generadas ni a las acciones individuales por color (contexto de una muestra copia el color original).

## Capabilities

### New Capabilities
- `palette-grayscale-preview`: modo de vista temporal 0–100 % que muestra la paleta activa con cada color mezclado hacia su gris equivalente (sin alterar datos), con acceso por botón compacto que expande la barra flotante y exportación que refleja el valor actual.

### Modified Capabilities
- `palette-export`: nueva exigencia — toda exportación de la paleta activa (formatos de código, imagen PNG y overlay) SHALL reflejar el estado actual del modo de vista en escala de grises (WYSIWYG).

## Impact

- `src/Matiz.App/Views/MainWindow.xaml` — tarjeta Paleta activa: columna derecha (botones arriba + botón compacto "Escala gris"), popup flotante con slider; bindings de presentación muestran el valor visible.
- `src/Matiz.App/ViewModels/MainViewModel.Palettes.cs` — estado `GrayScalePercent` (0–100) y refresco en caliente de los ítems; transformación aplicada en los tres caminos de exportación (Copiar como, Exportar imagen, Exportar overlay).
- `src/Matiz.App/ViewModels/Items.cs` — `PaletteColorItem`: campos de presentación (`DisplayBrush`/`DisplayForeground`/`DisplayHex`) calculados desde el porcentaje.
- `src/Matiz.Core/Colors/ColorMath.cs` + tests — mezcla hacia gris equivalente (interpolación por canal hacia `GrayEquivalent`, alfa del color original).
- `packaging`/persistencia sin cambios: `palettes.json` no cambia de esquema; sin cambios breaking.
- `Localization/Strings.resx` + `Strings.es.resx` — textos nuevos (botón, popup, tooltip).