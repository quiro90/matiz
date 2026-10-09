# Palette Building UX — Design

## Contexto

Matiz ya tiene toda la maquinaria de puntos secundarios (`_freeOffsets`, `MaxFreePoints = 64`, `AddFreePointAt`, `NextAddPointPosition`, `ConvertHarmonyToFree`, promoción del principal) y de reordenamiento de la paleta activa (`MovePaletteColor` → `PaletteService.MoveColor`). Este change reutiliza esos mecanismos y añade *vías de acceso* nuevas, en lugar de lógica nueva de generación.

- `ScreenPickerController` (overlay por monitor, snapshot congelado): click izquierdo / Enter / Espacio = `Confirm()`, click derecho = `Cancel()`, Esc = cancela, flechas = nudge, rueda = zoom. La confirmación invoca `Action<Argb?>` y `MainViewModel.Capture()` hace `Session.Commit(c, ColorChangeSource.ScreenCapture)` + portapapeles opcional.
- Fila del "+" (~`MainWindow.xaml` 459): `DockPanel` visible SOLO en Armonías/Libre vía `DataTriggers`; "Equilibrar" (Switch), "+" (`AddFreePointCommand`), "Borrar" (`ClearFreePointsCommand`).
- `ImageCanvas` usa click izquierdo para tomar el píxel (`PickCommand`/`PixelAt`); el click derecho no está usado → vía libre para el secundario.
- Drag & drop de la paleta activa ya funciona (`MainWindow.xaml` 632 + code-behind ~170–200) pero es invisible para el usuario.
- "Libre" existe solo como texto (resx es/en, READMEs); el enum `GeneratedTab.Free` y la persistencia no cambian.

## Goals / Non-Goals

**Goals**
- Tres vías para añadir un secundario: rueda (ya existe, click derecho), modo Captura (nuevo: click derecho, ojo Shift), imagen (nuevo: click derecho) — todas respetando el límite `MaxFreePoints`.
- Fila del "+" visible y a la misma altura en todas las pestañas.
- Renombrar la etiqueta "Libre" → "Personalizado" en toda la UI y docs.
- Hacer evidente el reordenamiento por arrastre de la paleta activa (hint, guía visual, drop al final).

**Non-Goals**
- No cambiar lógica de armonías ni de generación de paletas (Escala/Tints/Neutros).
- No cambiar persistencia ni el enum `GeneratedTab.Free` (solo etiquetas).
- No rediseñar el overlay de captura ni el layout general.

## Decisiones

### D1. Gatillo del secundario en captura: click derecho (no izquierdo)
El overlay recicla el click derecho, que hoy solo cancela; con esto las vías quedan coherentes con la rueda y la imagen (click derecho = secundario, click izquierdo = principal/confirmar). El usuario lo eligió explícitamente tras ver que en la rueda el secundario es derecho. Alternativas descartadas: hotkey adicional, doble click (conflictuaría con precisión).
- El click derecho añade el color bajo el cursor como secundario (pasando a Personalizado si hace falta, mismo camino que `AddFreePointAt`) **sin** copiarlo al portapapeles ni confirmar sobre el color principal.
- Esc sigue siendo la única cancelación; el tooltip del botón Capturar documenta las tres vías.

### D2. Shift = modo continuo
Si Shift está presionado (con click izquierdo o derecho), el overlay NO se cierra: sigue congelado para capturar varios colores seguidos (izq. actualiza el principal respetando los secundarios ya capturados; der. añade más secundarios). Sin Shift se conserva el comportamiento actual de cerrar tras confirmar. Implementación: `ScreenPickerController` consulta `Keyboard.Modifiers` en el momento del click y decide si llama a `Finish()` o mantiene el overlay vivo (refrescando el snapshot).

### D3. Los capturados guardan brillo propio
Un secundario capturado (overlay o imagen) se guarda con Δv propio = al brillo exacto del píxel capturado (igual que ya hace `LoadPaletteToFree`), para reproducir el color exacto. Los añadidos desde la rueda o con "+" siguen sin Δv (regla "Equilibrar"). Riesgo: el Δv queda atado al color actual de la sesión; aceptado porque `LoadPaletteToFree` ya opera así y es la semántica existente.

### D4. Fila del "+" siempre visible, altura fija arriba
Se eliminan los `DataTriggers` de visibilidad de la fila: el `DockPanel` queda siempre visible, alineado arriba (misma altura en todas las pestañas, "donde iría Equilibrar"), y los chips/lista envueltos debajo. En Armonías se sigue mostrando "Equilibrar"; en Personalizado "+" y "Borrar"; en Escala/Tints/Neutros/Imagen solo "+". `AddFreePoint` extiende su semántica: fuera de Armonías/Personalizado cambia a la pestaña Personalizado y añade con `NextAddPointPosition()` desde el color actual. Alternativa descartada: floating button (cambiaria de altura con el wrap de chips, justo lo que evita el pedido).

### D5. Vía imagen: click derecho sobre la imagen
`ImageCanvas` añade manejo de `MouseRightButtonUp`: pixel del color bajo el cursor sobre la imagen original (`PixelAt/PixelUnder`) → añade como secundario (con Δv propio según D3). El "+" de la pestaña imagen añade el color actualmente marcado en la imagen (el principal marcado con click izquierdo). Sin marcador válido, "+" mantiene el comportamiento genérico de D4.

### D6. Drag & drop evidente (paleta activa)
Reutilizar los handlers existentes; el gap es feedback y el drop al final:
1. **Hint al hover**: en la cabecera de la muestra, junto al tachito `X` (ya visible al hover), añadir otro elemento al hover con flechas/indicación de arrastre, y tooltip que distinga "click: usar / arrastrar: mover".
2. **Durante el drag**: la muestra de origen se atenúa (opacidad/tamaño) y un separador vertical de inserción (adorner simple en el `ItemsControl`/`StackPanel`) marca en todo momento entre qué muestras caerá.
3. **Drop al final**: `DragOver` también marca válidos los píxeles a la derecha de la última muestra; `Drop` sin índice = mover a la última posición (`MoveColor(i, count-1)`), que el clamp existente ya tolera.

### D7. Renombre solo de textos
"Libre" → "Personalizado" únicamente en: `Strings.es.resx` (`gen.tab.free`, `gen.addPoint.tooltip`, `gen.exportTitle.free`), `Strings.es-*.resx` si existen los mismos, `Strings.en.resx` (`Free` → `Custom`), tooltips que mencionen la pestaña, y READMEs (es/en). Enum y settings persistidos no cambian → sin migración de datos. El Purpose del spec principal se editó directo en `openspec/specs/free-points/spec.md`.

## Riesgos / Trade-offs

- [Click derecho en captura era "cancelar" familiar] → mitigación: Esc sigue cancelando y el tooltip del botón Capturar documenta la nueva vía desde el 1er uso.
- [Modo continuo con Shift puede dejar el overlay vivo más de lo esperado] → mitigación: Esc cancela en cualquier momento; sin Shift el comportamiento es idéntico al actual.
- [Fila fija del "+" en la pestaña imagen compite con el alto disponible de la imagen] → mitigación: la fila usa altura constante mínima y la imagen ya convive con la barra de tabs.
- [Separador de inserción en drag es code-behind (adorner) y no MVVM puro] → mitigación: mantener un solo elemento `Rectangle` reutilizable, oculto fuera del drag; sin nuevas dependencias.
- [El drop al final usa coordenadas del panel] → mitigación: calcular con `TranslatePoint` sobre el `ItemsPresenter` solo cuando hay espacio vacío (items con wrap corto); clamp de `MoveColor` evita índices inválidos.

## Migration Plan

Cambio puro de app (C#/WPF + resx + README). Sin datos nuevos: `GeneratedTab.Free` se persiste sin cambios; paletas y settings existentes siguen funcionando. Rollback: `git revert` del commit de implementación (cada fase es separable).

## Open Questions

Ninguno — las decisiones de gatillo, Shift, brillo propio, posición del "+" y feedback de drag fueron confirmadas con el usuario durante la exploración.