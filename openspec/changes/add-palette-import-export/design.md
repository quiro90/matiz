## Context
Ver proposal.md (Why/What). Estado relevante del código:
- El DTO de biblioteca (`Palette`, `PaletteColor` en `src/Matiz.Core/Palettes/Palette.cs`) ya expone todo lo que el archivo debe llevar: nombre, descripción, fechas y colores con nombre individual + hex + alfa. La serialización del repositorio usa un contexto source-gen (`MatizJsonContext` en `Persistence/Repositories.cs`) con camelCase y omisión de nulos.
- `JsonStore.WriteAtomic` (`Persistence/JsonStore.cs`) define la escritura atómica vigente (tmp + `File.Replace`), y `PaletteService.Create`/`Duplicate` el patrón de nombre único con sufijo (`UniqueName`).
- El preview truncado está en `Matiz.App/ViewModels/Items.cs` (`Preview = Model.Colors.Take(12)`); el panel Biblioteca (`Views/MainWindow.xaml`, bloque "Biblioteca de paletas", ~370px) usa `ItemsControl` con `StackPanel` horizontal.
- La app ya usa diálogos nativos `Microsoft.Win32` (ventanas de exportación de imagen) y es instancia única por mutex con canal de reactivación (`App.xaml.cs`).
- Límite vigente: `PaletteService.MaxColors = 64`; paletas externas mayores se conservan intactas (spec saved-palettes).

## Goals / Non-Goals
**Goals:**
- Un formato de archivo standalone, legible y versionado, exportable hoy y compatible con futuras versiones (guarda de esquema).
- Import idempotente respecto de la biblioteca existente: nunca pisa, siempre agrega.
- Preview de la lista sin truncado, con envoltura a filas.
- Doble click del SO funciona tanto con app cerrada como abierta.

**Non-Goals:**
- No se toca el esquema de `palettes.json` ni la migración existente.
- No se exporta/importa ningún otro estado (historial, rueda, generación): solo paletas.
- Sin asociación de archivos fuera del paquete MSIX (portable/dev no registra extensión).
- Sin ícono dedicado para el tipo de archivo (reutiliza logo del paquete; puede agregarse después).

## Decisions

### 1. DTO propio `PaletteFile` en Core, serializado con el contexto source-gen existente
- Nuevo `src/Matiz.Core/Palettes/PaletteFile.cs`: `SchemaVersion` (constante 1), `Name`, `Description`, `CreatedAt`, `ModifiedAt`, `Colors` (lista de `PaletteColor` reutilizada tal cual: serializa `id/name/hex/alpha`). `From(Palette)` y `ToPalette()`.
- `ToPalette()` regenera ids (paleta y colores) para nunca duplicar ids internos; conserva nombre/descripción/fechas/hex/alfa.
- Registro en `MatizJsonContext` (`[JsonSerializable(typeof(PaletteFile))]`): mismas opciones que el resto de la persistencia (AOT/trim-friendly, camelCase, omiss null).
- Alternativa rechazada: serializar `Palette` completa — arrastra ids internos y acopla el archivo al esquema de la biblioteca.

### 2. Guardado y lectura como pares estáticos de `PaletteFile`
- `PaletteFile.Write(path)`: JSON con indentación + escritura atómica propia (~6 líneas, mismo patrón de `WriteAtomic`: `.tmp` + `File.Replace`). No se reutiliza `JsonStore`: su ctor está atado a un documento de datos con debounce, y un archivo de export no necesita timer.
- `PaletteFile.Read(path)`: parse con el contexto, `schemaVersion > vigente` → `NotSupportedException` (mismo criterio que `PaletteRepository.Migrate`), JSON vacío/nulo → `JsonException`. Las excepciones viajan al App que muestra toast.

### 3. Importación en `PaletteService.Import(PaletteFile, ...)`
- Mismo flujo que `Create` con `UniqueName` (sufijo automático si el nombre existe, que es la respuesta del usuario) pero con fechas y colores del archivo. `CreatedAt`/`ModifiedAt` con valor default → "ahora" (defensa contra archivos incompletos).
- No pasa por `AddColors` (que aplica el límite) porque importar es restaurar un archivo externo: se asigna la lista completa a `Colors` y luego `Touch` → autoguardado con debounce normal.
- `Library.ActivePaletteId` apunta a la nueva paleta (queda marcada).

### 4. Comandos y diálogos en el VM de la app
- `ExportPalette` (CanExecute `HasActiveColors`, coherente con `ExportActivePaletteImage`) y `ImportPalette` (sin gating) en `MainViewModel.Palettes.cs`; dialogs `Microsoft.Win32` directos, como en `ExportImageWindow`.
- Filtro y título localizados en resx (`dialogs.exportPalette.*`, `dialogs.importPalette.*`). Nombre sugerido: nombre de la paleta sanitizado contra `Path.GetInvalidFileNameChars()`.
- `ImportPaletteFile(string path)` método público también usado por App (doble click). Toasts: exportación/importación con nombre; error compartido con mensaje del motivo.
- `ExportPaletteCommand.NotifyCanExecuteChanged()` junto a los demás en `SyncPalettes`.

### 5. Panel Biblioteca
- Preview: `ItemsPanel` → `WrapPanel` y `Preview` sin `Take(12)` (todos los colores; sin cambios de tamaño 16px): ~17 por fila y envuelve.
- Fila de botones nueva (row adicional del grid) `HorizontalAlignment="Right"`: "Exportar" con `Visibility` atado a `SelectedPalette` (NotEmptyToVis) y "Importar" siempre visible a la derecha (solo Importar cuando la lista está vacía).

### 6. Doble click del SO por argumento + entregador de pendiente
- Manifest MSIX: `uap:Extension Category="windows.fileTypeAssociation"` con `.mpalette` (fullTrust: la ruta llega como argumento de línea de comando).
- `App.OnStartup(e)`: recoge args con extensión `.mpalette` (case-insensitive) existentes. Instancia nueva (primera) → importa tras inicializar VM/ventana (y también procesa un pendiente dejado por un crash). Instancia duplicada → escribe la ruta en `%APPDATA%\Matiz\import.mpalette.pending` antes de señalar el evento `Activate`; el listener de la instancia viva lee el pendiente, importa (Dispatcher) y borra el archivo. Recién arrancada la app valida `File.Exists`.
- Alternativa rechazada: named pipe para pasar args al vivo (más interop por el mismo resultado).

## Risks / Trade-offs
- [Preview de 64 colores → 4 filas por item] → Aceptado por decisión del usuario ("mostremos en filas aunque sea el máximo"); la lista scrollea; fila 1 siempre llena.
- [Asociación requiere actualizar el paquete instalado] → En dev/portable no hay doble-click: los flujos por botón cubren el uso diario; la asociación se activa al publicar la store/MSIX actualizada.
- [Archivo pendiente escribe antes de señalar] → Si la instancia viva muere antes de consumir, el siguiente arranque procesa el pendiente (convergencia); solo se usa la primera línea del archivo.
- [Import >64 salta el límite intencionalmente] → Igual tratamiento que palettes.json externas (leer sí, añadir no), definido en spec.
- [Escritura atómica duplicada fuera de JsonStore] → ~6 líneas, probadas con test de roundtrip; mantener igual que `WriteAtomic`.

## Migration Plan
Sin migración de datos: formato nuevo standalone, la biblioteca no cambia de esquema. Rollback trivial (revertir commits; `.mpalette` dejado en el disco sigue siendo JSON legible).

## Open Questions
(Ninguna; decisiones del usuario tomadas en la conversación: `.mpalette` con JSON, import siempre nueva con sufijo en colisión, Import visible aun sin paleta marcada, preview en filas sin truncar. Doble click marcado como "genial si es simple": wiring elegido es simple y va incluido.)