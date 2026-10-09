## 1. Core: reglas de límite

- [x] 1.1 `PaletteService`: añadir `public const int MaxColors = 64`; `AddColor` devuelve `null` si la paleta tiene 64 colores; `AddColors` pasa a all-or-nothing (validar total ≤ 64 antes de añadir, devolver la cantidad añadida). Verificar con tests nuevos en `Matiz.Core.Tests` (añadir al límite devuelve null/0; 67+70 externa se conserva)
- [x] 1.2 `DominantColors.Extract` (Core): cap interno `(count, 1, 32)` → `(count, 1, 64)` con constante `MaxCount = 64`. Verificar: test de `GenerationTests` con count=64 (extrae 64) y count=1 (extrae 1)
- [x] 1.3 Extender el modelo de puntos libres de `PaletteGenerator.FreePoints` a tripleta `(HueDelta, SatDelta, ValueDelta)` con `ValueDelta = null`: `null` mantiene la regla vigente (ValueForLightness si Equilibrar, base.V si no); con valor, `v = clamp(base.V + Δv)`. Actualizar la firma de `Generate` y sus llamadores. Verificar: `FreePointsTests` actualizado a tripletas verde

## 2. App: VM de paleta activa (rueda)

- [x] 2.1 `MainViewModel.GeneratedPalettes.cs`: `MaxFreePoints` 16 → 64. Verificar: compila y `CanAddFreePoint` refleja 64
- [x] 2.2 `AddFreePointAt` (click derecho en rueda): al límite mostrar toast `toasts.maxPaletteColors` ("Máximo 64 colores por paleta") y no añadir. Verificar manual: click derecho con 64 puntos → toast, contador queda 64
- [x] 2.3 `SetWheelMarkerOffset` (arrastre) preserva el `Δv` existente del punto; `ConvertHarmonyToFree` y "+" crean `ValueDelta = null`; `RemoveFreePoint` (promoción) recalcula `Δv'` desde los colores generados reales cuando existían `Δv` explícitos. Verificar: arrastrar un punto cargado mantiene su brillo, y compila `FreePointsTests`
- [x] 2.4 `ExtractColorsCoreAsync` (`MainViewModel.Image.cs`): clamp `Math.Clamp(ExtractCount, 3, 10)` → `(1, 64)`. Verificar: compila + test de extracción con 1 y 64

## 3. App: comando "Recargar"

- [x] 3.1 `MainViewModel.Palettes.cs`: comando `ReloadPaletteToFree` con `CanExecute = HasActiveColors`; al pulsar muestra `ShowToast(toasts.reloadPaletteWarning, actionLabel: "Recargar", LoadPaletteToFree, 5)`. Verificar manual: sin paleta activa → deshabilitado; con paleta → toast con acción
- [x] 3.2 `LoadPaletteToFree`: evalúa `_palettes.Active`; `Session.Commit(Colors[0].Color, ColorChangeSource.Palette)`; para el resto calcula offset triple con `Δv = hsv.V − base.V`; `_freeOffsets` reemplazado por completo; pestaña `GeneratedTab.Free`; cierra `IsLibraryOpen` (e `IsImageMode` si aplica); toast `toasts.paletteLoaded` opcional. Verificar manual: colores de tarjetas idénticos (HEX igual) a los de la paleta; deshacer vuelve al color anterior
- [x] 3.3 Cablear en todos los lugares que listan comandos de paleta (`MainWindow.xaml` CommandBindings si los hay, menú de paletas). Verificar: botón habilitado/deshabilitado según `HasActiveColors`

## 4. UI (XAML) y Localización

- [x] 4.1 `MainWindow.xaml` imagen: `NumericBox` de n.º de colores `Minimum="1"` `Maximum="64"` (línea ~227). Verificar manual: intentar 0 o 65 marca inválido y no lanza extracción
- [x] 4.2 `MainWindow.xaml` panel Biblioteca (~682): botón "Recargar" junto a "Nueva", mismo estilo que Nueva/Duplicar/Borrar, `Command=ReloadPaletteToFree` con tooltip. Verificar manual: visible junto a Nueva
- [x] 4.3 `Strings.resx` + `Strings.es.resx` (sección library, ~380): añadir `library.reload` ("Recargar"), `library.reload.tooltip` ("Cargar la paleta en la rueda cromática"), `toasts.reloadPaletteWarning` ("Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales"), `toasts.maxPaletteColors` ("Máximo 64 colores por paleta"), `toasts.paletteLoaded` ("Paleta «{0}» cargada en la rueda"). Verificar: la app muestra los textos en es y en (invariant) en

## 5. Tests

- [x] 5.1 Actualizar `FreePointsTests` a tripletas y cubrir: Δv null con Equilibrar on/off, Δv explícito (brillo exacto e inmune a Equilibrar), mezcla null/explícito. Verificar: `dotnet test tests/Matiz.Core.Tests` verde
- [x] 5.2 Tests nuevos de límite: `PaletteService.AddColor`/`AddColors` al límite y all-or-nothing; `DominantColors.Extract` con count 1 y 64. Verificar: `dotnet test tests/Matiz.Core.Tests` verde
- [x] 5.3 `dotnet build` completo sin warnings (TreatWarningsAsErrors) y suite completa verde

## 6. Documentación y versión

- [x] 6.1 Docs vault: `02 Funcionalidades\Puntos libres.md` (16 → 64), `06 Decisiones\ADR-011 Puntos libres.md` (Δbrillo), `02 Funcionalidades\Image picker.md` (1–64, default 6), `02 Funcionalidades\Paletas guardadas.md` (límite + sección Recargar). Verificar: coherentes con specs
- [x] 6.2 Versión 1.0.7: `Directory.Build.props` (Version + InformationalVersion), `packaging/Matiz.Package/Package.appxmanifest`, `docs/vault/08 Desarrollo/Publicación.md`. Verificar: `git grep "1\.0\.6"` sin restos relevantes
- [x] 6.3 Tras `openspec archive palette-limits-64-and-reload`: retitular en `openspec/specs/free-points/spec.md` el escenario "Límite de 16 puntos" → "Límite de 64 puntos" (el título se conserva por validación del delta; content ya describe 64). Verificar: `openspec list` muestra specs actualizadas y el cambio archivado
- [x] 6.4 Commit con trailer Co-authored-by. Verificar: historial limpio con ambos pasos

## 7. Verificación final

- [ ] 7.1 Recorrido manual completo: importar imagen (default 6, rango 1–64) → añadir hasta 64 con "+" y click derecho (aviso al límite) → guardar paleta de 64 → Recargar desde el menú (aviso → colores exactos en tarjetas, primer color principal, Biblioteca cerrada) → Equilibrar no altera los Δbrillo → deshacer funciona. Verificar: comportamiento como en specs/deltas