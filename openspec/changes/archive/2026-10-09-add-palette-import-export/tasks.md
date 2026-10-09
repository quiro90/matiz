# Tareas: add-palette-import-export

## Fase 1 — Core: formato `.mpalette` e importación
- [x] 1.1. `PaletteFile` en `src/Matiz.Core/Palettes/PaletteFile.cs`: DTO (`SchemaVersion` 1, Name, Description, CreatedAt, ModifiedAt, Colors), `From(Palette)`, `ToPalette()` (ids nuevos), `Write(path)` atómico (`.tmp` + `File.Replace`), `Read(path)` con validación de `schemaVersion`. Verificar: proyecto compila.
- [x] 1.2. Registrar `[JsonSerializable(typeof(PaletteFile))]` en `MatizJsonContext` (`Persistence/Repositories.cs`). Verificar: build del Core completo.
- [x] 1.3. `PaletteService.Import(PaletteFile)`: paleta nueva con `UniqueName` (sufijo si existiera), fechas del archivo (default → ahora), colores completos sin límite, ids nuevos, queda activa, `Changed` → autoguardado. Verificar: tests de 1.4.
- [x] 1.4. Tests xUnit en `tests/Matiz.Core.Tests` (nuevo `PaletteFileTests.cs`): roundtrip completo (nombre/descripción/3 colores con nombres/orden/fechas/alfa 128 conservado); JSON inválido → `JsonException`; `schemaVersion` futura → `NotSupportedException`; `Import` crea nueva sin pisar la existente (colisión → "Nombre 2"); import con 70 colores se conserva completa; nombre vacío → "Paleta sin título". Verificar: `dotnet test tests/Matiz.Core.Tests`.

## Fase 2 — App: preview completo, botones y comandos
- [x] 2.1. `Items.cs`: `Preview` = todos los colores (quitar `Take(12)`). Verificar: build; al ejecutar, paleta de 13+ colores muestra todos en la lista.
- [x] 2.2. `MainWindow.xaml`: `ItemsPanel` del preview → `WrapPanel`. Verificar: paleta de 20 colores envuelve a 2 filas, sin scrollbar horizontal.
- [x] 2.3. Fila de botones "Exportar | Importar" (Importar a la derecha, siempre visible; Exportar solo con paleta marcada) debajo de la descripción, nueva row del grid. Verificar: sin paletas se ve solo "Importar"; con paleta se ven ambos.
- [x] 2.4. Comandos en `MainViewModel.Palettes.cs`: `ExportPalette` (CanExecute `HasActiveColors`, SaveFileDialog, write + toast, nombre sugerido sanitizado) e `ImportPalette` (OpenFileDialog → `ImportPaletteFile`); `ImportPaletteFile(string)` público con manejo de errores → toast; `NotifyCanExecuteChanged` de `ExportPaletteCommand` en `SyncPalettes`. Verificar: build; export/import manual de una paleta y re-import produce "Nombre 2".
- [x] 2.5. Localización: `library.title` → "Paletas de Colores"/"Color Palettes"; claves `library.export`, `library.import` (+tooltips, automation), `dialogs.exportPalette.*`, `dialogs.importPalette.*`, `toasts.paletteExported`, `toasts.paletteImported`, `toasts.paletteFileError` en `Strings.resx` y `Strings.es.resx`. Verificar: UI en ES y EN (cambiar idioma en Ajustes).

## Fase 3 — Doble click del SO
- [x] 3.1. `App.xaml.cs`: detección de args `.mpalette` (case-insensitive) al arrancar; instancia nueva importa tras init (y procesa pendiente previo); instancia duplicada escribe `%APPDATA%\Matiz\import.mpalette.pending` antes de señalar `Activate`; listener de la instancia viva lee pendiente → `ImportPaletteFile` (Dispatcher) y borra el archivo. Verificar: app cerrada, segunda instancia con arg → la viva importa y no abre ventana nueva.
- [x] 3.2. `packaging/Matiz.Package/Package.appxmanifest`: `fileTypeAssociation` con `.mpalette`. Verificar: cambia el manifiesto y no rompe el resto (build del proyecto de paquete no requiere; valida el XML).

## Fase 4 — Validación final
- [x] 4.1. `dotnet build` de la solución + `dotnet test tests/Matiz.Core.Tests` (y tests de App si aplica). Verificar: sin errores/regresiones.
- [x] 4.2. Recorrido manual rápido (entregable final): exportar "PuchiApp" → importar (quedan ambas), revisar preview con 13/20 colores, doble click del `.mpalette` con app corriendo, título "Paletas de Colores". Verificar: comportamiento según specs del cambio. *(Confirmado por el usuario: "está perfecto")*