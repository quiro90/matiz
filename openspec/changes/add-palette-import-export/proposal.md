## Why
La biblioteca de paletas permite crear, duplicar, eliminar y recargar paletas, pero no hay forma de llevar una paleta a un archivo ni de recuperarla: si se borra, si se cambia de máquina o si alguien quiere compartir una paleta ("armala y te la paso"), los datos quedan atrapados en `palettes.json`. Además el mini-preview de la lista trunca los colores en 12 (`Colors.Take(12)`), y una paleta de 13+ colores se ve incompleta.

## What Changes
- Título del panel Biblioteca: "Paletas" pasa a "Paletas de Colores" (EN: "Color Palettes").
- En la zona inferior del panel (editor de nombre y descripción de la paleta marcada) se agregan dos botones, siempre con "Importar" a la derecha:
  - **Exportar**: guarda la paleta marcada completa (nombre, descripción, colores con su nombre individual, orden y fechas) en un archivo `.mpalette` (JSON, versión de esquema), vía diálogo "Guardar como".
  - **Importar**: abre un archivo `.mpalette` y restaura la paleta **siempre como paleta nueva**: conserva nombre/descripción/colores/del archivo (fechas incluidas) pero genera ids nuevos y nunca pisa una paleta existente. Si el nombre ya existe se agrega el sufijo automático ("Nombre 2"), igual que el resto de la app. Queda como paleta marcada.
- Sin paleta marcada (lista vacía), la zona inferior muestra solo "Importar", de modo que importar siempre es alcanzable.
- El preview de cada paleta muestra **todos** sus colores (hasta 64) en un `WrapPanel`: la primera fila muestra el máximo que entra (~17), y con más colores envuelve a una segunda fila en lugar de truncar.
- Doble click del SO sobre un `.mpalette` abre Matiz e importa la paleta: asociación de archivos declarada en el manifest MSIX y manejo de argumentos; si Matiz ya está corriendo, la nueva instancia le entrega el archivo a la instancia activa (pendiente de importación) y cierra.

## Capabilities
### New Capabilities
- `palette-file`: formato de archivo de paleta `.mpalette` (JSON versionado), requisitos de Exportar/Importar (siempre nueva, sin pisar, fechas y colores con nombre) y apertura por doble click del SO (asociación + delegación a instancia viva).

### Modified Capabilities
- `saved-palettes`: presentación del panel Biblioteca (título "Paletas de Colores"), botones Exportar/Importar junto al editor de nombre/descripción de la paleta marcada (Importar visible aun sin paleta marcada) y preview de la lista sin truncar en 12 (envuelve a 2 filas si hace falta).

## Impact
- **Matiz.Core**:
  - Nuevo `Palettes/PaletteFile.cs`: DTO con `From`/`ToPalette`, guardado atómico y lectura con validación de `schemaVersion`.
  - `Palettes/PaletteService.cs`: nuevo `Import(PaletteFile)`.
  - `Persistence/Repositories.cs`: registro `[JsonSerializable(typeof(PaletteFile))]` en `MatizJsonContext`.
- **Matiz.App**:
  - `Views/MainWindow.xaml`: panel Biblioteca (WrapPanel del preview + fila de botones bajo la descripción).
  - `ViewModels/MainViewModel.Palettes.cs`: comandos `ExportPalette` / `ImportPalette` y método público `ImportPaletteFile`.
  - `ViewModels/Items.cs`: `Preview` sin `Take(12)`.
  - `App.xaml.cs`: importación por argumento de línea de comando + entrega a instancia viva (archivo pendiente).
  - `Localization/Strings.resx` y `Strings.es.resx`: claves nuevas.
- **Empaquetado**: `packaging/Matiz.Package/Package.appxmanifest` — `fileTypeAssociation` para `.mpalette`.
- **Tests**: `tests/Matiz.Core.Tests` — ida y vuelta del archivo, importación (nueva, colisión de nombre, >64 colores, versiones/fechas por defecto).