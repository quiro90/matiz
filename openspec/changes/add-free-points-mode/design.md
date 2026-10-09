## Context

Ver proposal.md — Why. Implementación actual relevante (leída del código):

- `MainViewModel.GeneratedPalettes.cs`: enum `GeneratedTab { Scale, Harmony, TintsShades, Neutrals, Extracted }`; `RefreshGenerated()` regenera `GeneratedColors` según pestaña; `RefreshWheelMarkers()` arma los `WheelMarker` (solo en Harmony, salta el base) y mantiene `_markerToSwatch`; `_harmonyOffsets[(Δhue,Δsat)]`, `HasHarmonyOffsets`, comandos `SetWheelMarkerOffset`, `ResetWheelMarkerOffset`, `ResetHarmonyOffsets`; `OnHarmonyKindChanged` y `OnSessionChanged` (MainViewModel.cs:109-110) limpian los desfases.
- `ColorWheel.cs`: `Markers`, `MarkerClickCommand`, `MarkerDragCommand`, `MarkerResetCommand` (doble click); captura de mouse (`_markerCapture`) reentrante con umbral de arrastre; `HitMarker` da prioridad al marcador principal; no tiene manejo de botón derecho.
- `PaletteGenerator.cs`: `Harmony(ColorState, kind, anchor, balance, colorOffsets)` gira el hue HSV el ángulo canónico (+ desfase opcional) y ajusta el brillo con `ValueForLightness` ("Equilibrar"); `HarmonyOffsets(kind)` devuelve los ángulos (incluye el 0 del base); Monocromática deriva de Design Scale (posiciones en rueda coincidentes).
- `MainWindow.xaml`: fila de pestañas con `RadioButton`/`EnumEquals`; fila de opciones de Armonía (`DockPanel`) con ListBox de tipos, switch "Equilibrar" y botón "Restaurar"; plantilla de tarjetas compartida con botones Copiar/Agregar (abajo izquierda) y la X por hover de la paleta activa como patrón de referencia (`IsMouseOver` + `BoolToVis`).
- Nada de esto se persiste: los desfases viven solo en memoria del VM.

## Goals / Non-Goals

**Goals:**
- Modo Libre con estado relativo (Δhue/Δsat) que siga al principal por el solo hecho de re-derivarse en cada refresh.
- Reutilizar al máximo el pipeline existente de armonías (generación, rueda, tarjetas): los puntos libres son "una armonía sin figura canónica".
- Eliminar limpiamente la maquinaria de desfases de armonía (estado, comandos, bindings, claves de localización).

**Non-Goals:**
- Persistir el conjunto libre entre sesiones (sigue la política de los desfases: solo sesión).
- Deshacer acciones sobre puntos (undo/redo sigue cubriendo solo el color actual).
- Menus contextuales nuevos en la rueda o el wheel-marker (el click derecho añade directo, sin menú).
- Cambiar la generación canónica de armonías ("Armonías en espacio perceptual" queda intacto).

## Decisions

1. **Representación relativa del conjunto libre.** `List<(double HueDelta, double SatDelta)> _freeOffsets` respecto de `Session.Current` (hue/sat continuos, hue normalizado 0–360, Δsat en [−1, 1] con clamp a [0,1] al derivar). El principal no se guarda: es siempre `Session.Current` y su tarjeta es la base. *Alternativa descartada*: guardar posiciones absolutas — obligaría a re-anclar a mano en todos los orígenes de cambio del color (rueda, brillo, campos, historial, captura, undo); relativo da gratis la traslación rígida exigida.

2. **Generación en Core:** `PaletteGenerator.FreePoints(ColorState baseState, IReadOnlyList<(double HueDelta, double SatDelta)> offsets, bool balanceLightness)`: base + un `GeneratedColor` por offset (hue = baseHue+Δhue, sat = clamp(baseSat+Δsat, 0, 1), brillo = `ValueForLightness` si "Equilibrar" o baseV si no; siempre `WheelHue`/`WheelSaturation` exactas). Etiquetas: "1", "2", … (números consecutivos en el orden del conjunto). Se reutiliza `GeneratedColor` (con `IsBase=false`), así las tarjetas, la rueda y la exportación funcionan sin cambios estructurales. Tests en Core: posición relativa (246.1° → 6.1°), ΔL < 0.01 con Equilibrar, mismo V sin Equilibrar, clamp de saturación, sRGB válido, lista vacía → solo base.

3. **Máximo de secundarios: 16** (constant `MaxFreePoints = 16` en el VM; el generador no la conoce). Decisión del usuario (16 u opciones ~"no infinito"); con 16 sigue siendo una fila usable (la Escala ya muestra 11). "CanExecute" del "+" y guardia en click derecho bloquean el 17.º.

4. **Punto nuevo con "+": opuesto si no hay secundarios, si no al lado del último**: (Δhue 180°, Δsat 0) cuando el conjunto no tiene aún puntos; en caso contrario hue +30° por paso hasta quedar separado (≥10°) de todo punto existente y del principal, conservando el Δsat del último. Así varios "+" consecutivos producen puntos visibles y movibles en vez de apilarse en el mismo hue. Alternativas descartadas: mismo lugar que el principal — un punto nuevo queda tapado bajo el marcador principal y parece "no haber pasado nada"; siempre opuesto — en Libre se apilan todos en 180° y en Complementaria solapa con el punto canónico. (*Ajuste tras implementación, feedback del usuario*.)

5. **Conversión Armonías → Libre reemplaza el conjunto.** Acciones que parten de una armonía ("+" o arrastre de punto, o click derecho estando en Armonías) definen el conjunto libre desde la figura actual: Δhue = ángulo canónico de cada punto (Δsat 0), y en el arrastre el punto movido toma el Δ derivado de la posición final absoluta; el punto nuevo del "+" va opuesto al principal. Se **reemplaza** el conjunto libre previo para que Libre siempre muestre la figura que el usuario estaba viendo en Armonías. *Alternativa descartada*: sumar los puntos previos — Libre mostraría una mezcla confusa (p. ej. una triádica + 7 puntos viejos) y crece sin control. En Monocromática la conversión lleva solo el principal (sus colores comparten posición en la rueda: llevarlos apilariaría 4 puntos en el mismo sitio).

6. **Señalización del arrastre en Armonías:** el primer evento de arrastre de `SetWheelMarkerOffset` ejecuta la conversión y cambia `GeneratedTab = Free`. El orden de los marcadores se conserva (sin base, índices estables en `_markerToSwatch`), de modo que la captura de mouse del `ColorWheel` (sobre el control, no sobre los marcadores) sobrevive al cambio de pestaña y los eventos siguientes del mismo arrastre ya los recibe el comando de puntos libres. `MarkerResetCommand` (doble click) se elimina del control y de sus bindings.

7. **Quitar el principal promueve al siguiente conservando posiciones absolutas.** Al borrar la base con k secundarios: Δ'_i = normalize(Δhue_i − Δhue_k) y Δsat'_i = Δsat_i − Δsat_k, y `Session.Commit(color_del_punto_k, ColorChangeSource.Generated)` — cambio confirmado como cualquier "usar como principal". *Alternativa descartada*: conservar los Δ relativos (la figura entera rotaría al nuevo ancla; sorprende al usuario que "borrar" mueva el resto). Borrar un secundario solo quita su entrada. `ColorChangeSource.Generated` además coincide semánticamente ("color de una pestaña generada").

8. **Click derecho en `ColorWheel`:** nuevo `MouseRightButtonDown` → proyecta el cursor a coordenadas de rueda (clamp al disco, mismo cálculo que `ExecuteMarkerDrag`) → comando `AddPointAt(hue, sat)` del VM, salvo si hay una captura/arrastre en curso. Sin menú contextual ni estado adicional. Si el conjunto ya tiene 16 secundarios, el VM ignora el comando. Funciona en todas las pestañas (la rueda no cambia de pestaña al hacer click izquierdo; el botón derecho siempre significa "añadir punto libre").

9. **UI de la fila de opciones:** un solo `DockPanel` visible en Armonías y Libre: `HarmonyList` (solo Armonías), switch "Equilibrar" (ambas), botón "+" (`ChipButton`, icono `E710`, tooltip "Añadir punto") a la derecha —desaparece "Restaurar". El orden de pestañas cambia solo en XAML (RadioButtons); el enum puede seguir en cualquier orden pero se documenta el orden de UI: Escala · Tints/Shades · Neutros · Armonías · Libre · Extraídas (condicional).

10. **"−" en tarjetas de Libre:** en la plantilla compartida de tarjetas, `SmallIconButton` (icono `E738`) en la esquina inferior derecha, visible por hover (`IsMouseOver` + `BoolToVis`, patrón de la X de la paleta activa) y solo cuando `GeneratedTab == Free` (trigger sobre el DataContext del ItemsControl) y cuando el conjunto tiene ≥ 2 colores. El comando recibe el `SwatchItem` y el VM resuelve el índice con `GeneratedColors.IndexOf` (patrón de `AddSwatchToPalette`); índice 0 = principal → promoción, índice > 0 → quita directa.

11. **Selección en Libre** reutiliza `_selectedHarmonyIndex`: el toast de punto seleccionado muestra "Libre {n} · {hex}" (sin nombre de armonía cuando la pestaña no es Harmony).

12. **Limpieza:** se borran `_harmonyOffsets`, `HasHarmonyOffsets`, `ClearHarmonyOffsets`, `ResetHarmonyOffsets`, `ResetWheelMarkerOffset` y el reseteo en `OnSessionChanged`/`OnHarmonyKindChanged`; claves `harmony.reset` fuera de las cadenas (`Texts.Core`). Nuevas claves es/en: `gen.tab.free`, `gen.addPoint` (+ tooltip), `swatch.removePoint`, `gen.exportTitle.free`.

## Risks / Trade-offs

- [Cambio de pestaña a mitad del arrastre (Armonías → Libre)] → el orden de marcadores se preserva y la captura vive en el control; verificación manual del drag continuo (tarea 2.2). Riesgo residual: refresco visual del conjunto de marcadores durante la captura — aceptable (una sola recalibración de posición).
- [Quitar la base con undo disponible desincroniza el conjunto: deshacer restaura el color principal pero no los puntos] → documentado en la spec ("quitar no es deshacible; undo cubre solo el color actual"); los Δ recomputados se mantienen, así el desorden es acotado.
- [17 tarjetas en una fila (UniformGrid)] → tarjetas angostas, HEX recortado igual que hoy en Escala con 11; se acepta (fila única es la convención del panel).
- [Borrar la maquinaria de desfases toca varios call sites] → un task dedicado de limpieza con build + tests green como verificación.
- [Click derecho accidental cambia de pestaña] → comportamiento pedido explícitamente ("al añadir ya se pasa a libre automáticamente"); ningún daño: volver a la pestaña anterior es un click.

## Migration Plan

Sin migración de datos: nada persiste (los desfases viejos tampoco persistían). Rollback = revertir el commit.

## Open Questions

Ninguna material: el límite (16) y los defaults (punto nuevo opuesto —o junto al último si ya hay puntos—, "−" por hover, "Equilibrar" en Libre, Extraídas tras Libre) los confirmó el usuario; la semántica de reemplazo al convertir desde Armonías queda documentada como decisión 5 — reversible en revisión si el usuario prefiere acumular.