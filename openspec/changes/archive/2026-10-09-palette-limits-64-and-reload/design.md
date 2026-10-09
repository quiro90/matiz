## Context

Estado actual (ver proposal.md): `MainViewModel.GeneratedPalettes.cs` limita los secundarios de la rueda a `MaxFreePoints = 16`; la extracción de imagen clampa `ExtractCount` a (3, 10) en el XAML (`NumericBox` Minimum=3 Maximum=10) y en `ExtractColorsCoreAsync`, con cap interno adicional de 32 en `DominantColors.Extract`; `PaletteService.AddColor/AddColors` no tienen límite. El modo Libre se describe como desfases `(Δhue, Δsat)` relativos al color actual (`PaletteGenerator.FreePoints`), y su brillo se deriva siempre de la regla "Equilibrar" o del brillo del principal. La confirmación de acciones destructivas en la app usa el patrón de toast con acción (p. ej. "Deshacer" en borrar paleta); no hay diálogos modales de confirmación en flujos principales.

## Goals / Non-Goals

**Goals:**
- Un único número de regla (64) reutilizable en rueda, extracción y paletas guardadas.
- "Recargar" con fidelidad cromática: las tarjetas cargadas reproducen los colores exactos de la paleta.
- Cero cambios de persistencia: el conjunto libre no se guarda entre sesiones y el esquema de `palettes.json` no cambia.

**Non-Goals:**
- No se rediseña la colocación del "+" (los pasos de 30° siguen igual; con figuras de 60+ puntos pueden agotarse los 12 pasos y caer en el fallback, aceptado).
- No se persiste el conjunto Libre entre sesiones ni se exporta a JSON ese estado.
- No se recortan paletas existentes por encima de 64 (ver propuesta).

## Decisions

### 1. Constantes: 64 con semántica de secundarios
- `MaxFreePoints` 16 → 64 en `MainViewModel.GeneratedPalettes.cs` (secundarios; el principal no cuenta: 65 colores totales, igual semántica que el 16 vigente).
- `PaletteService` (Core) incorpora `public const int MaxColors = 64` y lo aplica en `AddColor` y `AddColors`. La regla vive en Core para que cualquier llamada (App, tests, futuras UIs) la respete; el VM la refleja en toasts. Alternativa rechazada: limitar solo en el VM (reglas duplicadas y hueco para llamadas futuras).
- `DominantColors.Extract`: su clamp interno (1, 32) sube a (1, 64) con constante `MaxCount = 64`; el llamador y el `NumericBox` imponen además el rango 1–64 (defensa en profundidad; k-means con k=64 sobre muestras ≤128 px es trivial en coste).

### 2. Aviso al límite según el flujo
- "+" al límite: deshabilitado (como hoy, sin evento).
- Click derecho en rueda al límite: hoy falla en silencio (`AddFreePointAt` retorna); pasa a mostrar el toast `toasts.maxPaletteColors` ("Máximo 64 colores por paleta") y no añade. Se usa el mismo texto global de regla en rueda y paletas para un único mensaje mental.
- Extracción: el `NumericBox` (1–64) y el clamp Rechazan fuera de rango marcando `IsInvalid`; no se añade toast porque la UX del control ya impide el valor.

### 3. API de añadir colores a paleta con resultado explícito
- `AddColor` devuelve `null` si la paleta no ha sido encontrada o está llena (antes solo lo devolvía si no la encuentra; el llamador distingue por `p.Colors.Count` o un nuevo booleano de retorno).
- `AddColors` pasa a devolver `int` con la cantidad añadida (ahora devuelve `void`): 0 significa bloqueo total.
- El VM muestra `toasts.maxPaletteColors` cuando el resultado indica bloqueo. "Añadir todo" es todo-o-nada (decisión del usuario: sin recorte parcial): el VM pre-evalúa `p.Colors.Count + n <= 64` antes de llamar y no llama si no cabe. Alternativa rechazada: añadir hasta el límite y avisar cuántos quedaron fuera (opción "recortar", descartada por el usuario).

### 4. Δbrillo propio en los puntos libres (fidelidad de "Recargar")
Los desfases libres pasan de `(Δhue, Δsat)` a `(Δhue, Δsat, Δv)` con `Δv` (`double?`):
- `null` = comportamiento actual exacto (puntos de armonía, "+", arrastre, click derecho: regla "Equilibrar" o brillo del base). Todos los flujos vigentes crean `null`; el modelo no cambia observablemente para ellos.
- `Δv` explícito = el punto reproduce su brillo propio: `v = clamp(base.V + Δv)` y "Equilibrar" no lo toca. Solo "Recargar" crea `Δv` no nulos.
- Ripple contenido: `PaletteGenerator.FreePoints` (firma con tripleta), `ConvertHarmonyToFree`, `SetWheelMarkerOffset` (preserva el `Δv` existente al arrastrar), `RemoveFreePoint` (promoción recalcula `Δv` desde los colores generados reales: `Δv' = V(gen[i]) − V(promovido)`, de modo que tras promover a un punto la regla y los explícitos convergen en colores exactos), `ClearFreePoints`/`RemoveWheelMarker` sin cambios de semántica, y `FreePointsTests` actualizado a tripletas. Alternativas rechazadas: no reproducir el brillo (las tarjetas cargarían con brillo recalculado, "no carga mis colores"), ni ampliar `ColorState` (el color actual no participa: solo las tarjetas generadas).

### 5. Flow "Recargar" (VM, comando `ReloadPaletteToFree`)
1. `CanExecute` = paleta activa con ≥1 color (`HasActiveColors`). Sin paleta: deshabilitado, sin aviso.
2. Al pulsar: `ShowToast(toasts.reloadPaletteWarning, "Recargar", LoadPaletteToFree, seconds: 5)` — patrón vigente de toast con acción (el temporizador ya extiende a 6 s mínimo cuando hay acción). Modalidad no interactiva evitada a propósito (consistente con el resto de la app).
3. `LoadPaletteToFree`: evalúa la paleta activa al momento de confirmar (`_palettes.Active`);
   - base = `Colors[0].Color`: `Session.Commit(baseColor, ColorChangeSource.Palette)` (color actual exacto, va a deshacer);
   - para el resto: `hsv = ColorMath.ToHsv(c.Color)`; offset = `(NormalizeHue(hsv.H − base.Hue), hsv.S − base.Sat, hsv.V − base.V)` — la suma `base.V + Δv` reproduce bit a bit el ARGB de la paleta (el brillo de la tarjeta es exacto);
   - `_freeOffsets.Clear()` + rangos → pestaña `GeneratedTab.Free` (si ya estaba, `RefreshGenerated()`); cierra `IsLibraryOpen` (y `IsImageMode` si estaba abierto, para que se vea la rueda);
   - toast final `toasts.paletteLoaded` ("Paleta «{0}» cargada en la rueda") opcional de confirmación.
- El aviso menciona "se perderán las selecciones actuales" porque el conjunto libre previo se reemplaza (las paletas guardadas nunca se pierden); la promoción de "quitar principal" sigue funcionando tras la carga.

### 6. Versionado 1.0.7
Mismo patrón que 1.0.6: `Directory.Build.props` (`Version`, `InformationalVersion`), `packaging/Matiz.Package/Package.appxmanifest` y `docs/vault/08 Desarrollo/Publicación.md`.

## Risks / Trade-offs

- [Tripleta de desfases toca varios puntos del VM] → Mitigado: `null` preserva el comportamiento vigente línea a línea; tests de Free_points actualizados con tripletas explícitas.
- [Δv explícito hace que "Equilibrar" no actúe sobre puntos cargados] → Documentado en spec/docs (es el precio de la fidelidad); "Borrar" y armonías siguen creando puntos bajo regla.
- [Paleta externa >64 colores habilitada a medias (leer sí, añadir no)] → Definido explícitamente en la spec: el límite solo aplica a añadir.
- [Aviso "Recargar" como toast puede perderse si caduca] → 6 s mínimos con acción; repetir el click vuelve a mostrar el aviso (idempotente).
- [Promoción del principal recalcula Δv] → Solo afecta conjuntos cargados (Δv explícito) o mixtos; la conversión usa colores generados reales, no reglas nuevas.

## Migration Plan

Sin migración de datos (no cambia esquema ni formato). Rollback trivial.

## Open Questions

(Ninguna; las decisiones del usuario quedaron definidas en la conversación: toast con confirmación, cerrar panel tras recargar, bloqueo duro de 64 con único texto de aviso, y semántica del límite en la rueda de secundarios)