# Proposal: palette-building-ux

## Why

Armar una paleta desde la pantalla es hoy un flujo de varios pasos: capturar reemplaza el color actual, añadir un secundario exige volver a la rueda o al "+", y el único lugar donde se puede añadir sin la rueda es la pestaña Armonías/Libre. Además, el reordenamiento por arrastre de la paleta activa existe pero es invisible: ni cursor, ni indicadores, ni guía visual durante el arrastre. Este cambio agiliza el flujo "veo un color → lo capturo → armo la paleta" y hace evidente lo que ya funcionaba.

## What Changes

- **Captura de color secundario**: en el modo captura (Alt+C o botón "Capturar"), el click derecho captura el color como punto secundario del conjunto libre (misma regla que el "+" de Armonías o el click derecho en la rueda) sin modificar el color actual; el click izquierdo sigue confirmando el color principal. Con `Shift` presionado el overlay no se cierra: `Shift`+click izquierdo actualiza el principal (los secundarios lo siguen rígidamente) y `Shift`+click derecho añade otro secundario, permitiendo armar una paleta de varios clics seguidos. El tooltip del botón "Capturar" documenta esta función.
- **Botón "+" en todas las pestañas**: el "+" (Añadir punto) pasa a estar siempre visible en la fila del "Equilibrar" —misma altura en todas las pestañas (Escala 50–950, Tints/Shades, Neutros, Armonías, Libre/Personalizado y De la imagen), alineado arriba— y añade un punto secundario pasando automáticamente a la pestaña Libre. "Borrar" y "Equilibrar" siguen visibles solo en Armonías/Libre. "BREAKING": ninguno.
- **Secundarios en modo imagen**: con una imagen abierta, el click derecho sobre la imagen marca el píxel como punto secundario (misma regla que la rueda) y el visible "+" añade el color actual marcado; en modo imagen la rueda no está disponible, así que esta vía cubre esa falta.
- **Renombrar "Libre" → "Personalizado"**: solo etiquetas de UI y documentación (pestaña, título de exportación, tooltips, specs, READMEs); en inglés `Free` → `Custom`. No cambian identificadores de código, ni el formato de datos, ni la persistencia.
- **Drag & drop evidente en la paleta activa**: al pasar el mouse sobre una muestra se indica que se puede mover (tooltip junto al tachito X); durante el arrastre la muestra original se atenúa y un separador vertical señala el punto de inserción; soltar en el espacio vacío al final mueve el color a la última posición.

## Capabilities

### New Capabilities

<!-- Ninguna: no se introducen capacidades nuevas -->

### Modified Capabilities

- `screen-picker`: nuevas interacciones del modo captura (click derecho = secundario, `Shift` = overlay persistente) y documentación en el tooltip del botón "Capturar".
- `free-points`: el botón "+" queda visible y operativo en todas las pestañas con la fila a altura constante; nueva vía de añadir secundarios desde el modo captura (click derecho en overlay) y desde el modo imagen (click derecho sobre la imagen y "+" con el color actual); renombre de etiqueta "Libre" → "Personalizado".
- `saved-palettes`: el reordenamiento por arrastre gana visibilidad (tooltip/hint al pasar el mouse, muestra atenuada y separador de inserción durante el arrastre, soltar al final en el espacio vacío); el comportamiento de orden persistente ya especificado no cambia.

## Impact

- **Código (Matiz.App)**: `ScreenPickerController`/`OverlayWindow` (resultados `Principal`/`Secundario`/`Continuar`, click derecho, `Shift`), `Views/MainWindow.xaml` (fila de la barra de generadas: triggers, alineación y altura constante; paleta activa: visuales de arrastre), `Views/MainWindow.xaml.cs` (arrastre: estados, separador de inserción, drop en espacio vacío), `Controls/ImageCanvas.cs` (click derecho con píxel), `ViewModels/MainViewModel*.cs` (callback de captura con acción, `AddFreePoint` desde cualquier pestaña, nuevo comando de punto desde imagen), `Localization/Strings.es.resx` y `Strings.resx` (etiquetas y tooltips).
- **Código (Matiz.Core)**: sin cambios (los puntos libres, el límite de 64 y la persistencia de orden ya existen).
- **Specs**: deltas de `screen-picker`, `free-points`, `saved-palettes`.
- **Docs**: README.md y README.es.md (rename "Libre" → "Personalizado" y nuevas interacciones).
- **Riesgos**: interacción del click derecho con el cancelado (Esc queda como única cancelación); el "+" deshabilitado al límite de 64 sigue con su toast; la fila de armonías con chips envueltos puede desplazar la parte inferior de la tarjeta si no se fija la alineación del "+".