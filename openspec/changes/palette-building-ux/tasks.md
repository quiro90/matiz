# Palette Building UX — Tasks

Referencias: `proposal.md` (por qué/qué), `specs/` (requisitos), `design.md` (cómo — D1..D7). Verificación general de cada fase: `dotnet build Matiz.sln` sin errores y prueba manual de la app (`dotnet run --project src/Matiz.App`).

## 1. Captura de color secundario (overlay)

- [x] 1.1 En `ScreenPickerController`, el click derecho (y solo él) añade el color bajo el cursor como secundario vía el callback existente (nuevo modo de finalización `ConfirmSecondary()` con `CaptureResult.Kind`), sin copiar al portapapeles ni confirmar el principal; Esc sigue cancelando — verificar con Alt+C: click izq = principal actual (igual que hoy), click der = no cambia el principal, Esc = cancela. (D1)
- [x] 1.2 Si Shift está presionado al hacer click izq o der, el overlay NO se cierra (snapshot refrescado, sigue congelado): izq. actualiza el principal, der. añade otro secundario; sin Shift se conserva el cierre actual — verificar manualmente capturando 2-3 colores con Shift y cerrando luego con Esc o sin Shift. (D2)
- [x] 1.3 En `MainViewModel` (flujo `Capture()`), enrutar el secundario: `Session` pasa a Personalizado (vía `ConvertHarmonyToFree`/`AddFreePointFromColor`) con Δv propio del color capturado, respetando `MaxFreePoints = 64` (toast al llegar al límite) — verificar: Alt+C → click der → la paleta activa muestra el chip nuevo sin que cambie el color principal ni el portapapeles. (D3)
- [x] 1.4 Actualizar el tooltip del botón Capturar (resx `ui.capture.tooltip` es/en): documentar click izq = principal, click der = secundario (Personalizado), Shift = seguir capturando, Esc = cancelar, flechas/rueda igual que hoy — verificar: hover sobre el botón muestra el texto nuevo en es y en en. (D1/D2)

## 2. Botón "+" global y vía imagen

- [x] 2.1 En `MainWindow.xaml` (~459), quitar los `DataTriggers` de visibilidad de la fila del "+": `DockPanel` siempre visible, alineado arriba (altura constante en todas las pestañas, donde iría "Equilibrar"); los chips de Equilibrar/Borrar también quedan alineados arriba — verificar visualmente en las 6 pestañas que "+" está a la misma altura y visible. (D4)
- [x] 2.2 Visibilidad por pestaña: Armonías = Equilibrar + "+" + Borrar; Personalizado = Equilibrar + "+" + Borrar (se mantiene Equilibrar como hoy para que aplique a los puntos libres) ; Escala/Tints-Shades/Imagen = solo "+" (Equilibrar y Borrar ocultos) — verificar pestaña por pestaña. (D4)
- [x] 2.3 Extender `AddFreePoint` (VM): fuera de Armonías/Personalizado cambia a la pestaña Personalizado y añade con `NextAddPointPosition()` (opuesto al principal, con Δv=null) desde el color actual — verificar: en Escala con `#5246BC` pulsar "+" → pestaña Personalizado con el base + punto opuesto. (D4)
- [x] 2.4 En `ImageCanvas`, manejar `MouseRightButtonUp`: añadir como secundario el píxel bajo el cursor sobre la imagen original (`PixelUnder`, comando `AddImageSecondaryCommand`), con Δv propio (D3); sin imagen cargada no hace nada — verificar: cargar imagen → click der en 2 zonas → chips correctos y colores exactos vs la imagen. (D5)
- [x] 2.5 El "+" de la pestaña Imagen añade el color actualmente marcado (el principal marcado con click izq.); si no hay marcador válido cae al comportamiento genérico de 2.3 — verificar con y sin marcador. (D5)

## 3. Renombrar "Libre" → "Personalizado"

- [x] 3.1 Renombrar etiquetas en resx es: `gen.tab.free`="Personalizado", `gen.exportTitle.free`="Personalizado", y reescritas que mencionen "Libre" (p. ej. `gen.addPoint.tooltip`) — verificar con `grep` que ningún texto de UI queda con "Libre". (D7)
- [x] 3.2 Renombrar en resx en: `gen.tab.free`="Custom", `gen.exportTitle.free`="Custom" y reescritas equivalentes — verificar con `grep` en los resx en. (D7)
- [x] 3.3 Actualizar README.md y README.es.md (mención de "Libre"/modo Libre → "Personalizado") — verificar con `grep` que los READMEs no mencionan el nombre viejo fuera de notas históricas. (D7)
- [x] 3.4 Confirmar que enum `GeneratedTab.Free`, settings persistidos y paletas guardadas no cambian (solo etiquetas) — verificar: abrir la app con settings previos y ver que la pestaña activa y las paletas siguen funcionando. (D7)

## 4. Drag & drop evidente (paleta activa)

- [x] 4.1 Hint al hover: junto al tachito `X` de la cabecera de la muestra añadir un indicador de arrastre (icono Segoe MDL2 uE7C9, esquina superior izquierda) con tooltip del arrastre (`palette.drag.tooltip` nuevo es/en); `palette.color.tooltip` ahora distingue click/arrastrar/nombre — verificar: hover muestra tanto `X` como el hint; tooltip distingue ambas acciones. (D6)
- [x] 4.2 Feedback de arrastre: al arrastrar, la muestra de origen se atenúa (Opacity 0.45 mientras `DoDragDrop` está activo) y un separador vertical morado (`DropIndicatorAdorner`) señala entre qué muestras caerá; al soltar se restauran/ocultan — verificar arrastrando el 2° color hacia el 4° posición y observando separador + atenuación antes de soltar. (D6)
- [x] 4.3 Drop al final: `DragOver` sobre el `ScrollViewer` también marca el espacio a la derecha de la última muestra (hueco calculado por geometría); `Drop` sobre espacio vacío tras la última mueve a la última posición vía `MoveColor(src, count-1)` — verificar: arrastrar el 1er color al espacio vacío y confirmar que queda al final y persiste tras reiniciar la app. (D6)
- [x] 4.4 Regresión mínima del drag existente: umbral de arrastre (click sin drag sigue = usar color), mover izq/der del menú contextual, clamp de índices — verificar click (sin mover) usa el color como principal y sigue funcionando. (D6)

## 5. Verificación final

- [x] 5.1 `dotnet build Matiz.sln` compila sin warnings nuevos y `openspec validate "palette-building-ux"` pasa — comando y salida registrados.
- [ ] 5.2 Recorrido manual end-to-end: Alt+C (principal+2 secundarios con Shift) →"+" en Escala → click der en imagen → renames visibles → drag con feedback y persistencia — listar resultados observados contra los escenarios de specs.
- [ ] 5.3 `openspec archive "palette-building-ux"` tras confirmación del usuario (aplicar deltas a specs principales) — verificar `openspec list` sin changes activos y specs actualizados.