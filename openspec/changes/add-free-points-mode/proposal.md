## Why

Hoy la armonía se "rompe" al arrastrar un punto (quedan desfases personalizados con un botón "Restaurar" para volver) y no hay forma de tener puntos de color adicionales fuera de las armonías geométricas: si el usuario quiere su propia combinación libre de 2 a N colores, no puede construirla. Se pide separar ambos mundos: Armonías siempre muestra la figura canónica exacta (ya no se rompe), y un nuevo modo "Libre" permite armar paletas personalizadas añadiendo puntos secundarios libremente desde cualquier pestaña.

## What Changes

- **Orden de pestañas**: Armonías pasa al final, después de Neutros, y se añade la pestaña "Libre" como última (Extraídas queda visible tras Libre solo cuando hay colores extraídos). Orden final: Escala · Tints/Shades · Neutros · Armonías · Libre · Extraídas (condicional).
- **Modo Libre (pestaña "Libre")**: parte de un color principal (el color actual) y admite hasta **16 puntos secundarios** con posición relativa (Δhue/Δsat) respecto del principal. Por defecto solo tiene el principal. Al mover el principal, los puntos siguen conservando sus posiciones relativas (traslación rígida). Se muestra igual que Armonías: puntos en la rueda + tarjetas, y aplica la opción "Equilibrar".
- **Añadir puntos secundarios**:
  - **Click derecho en la rueda** (desde cualquier pestaña: Escala, Tints/Shades, Neutros, Armonías, Libre): añade un punto secundario en esa posición y pasa automáticamente a "Libre".
  - **Botón "+" discreto** junto a "Equilibrar", abajo a la derecha de los colores: visible en Armonías y Libre; crea el punto opuesto al principal (180°, misma saturación) y pasa automáticamente a "Libre". Se deshabilita al llegar a 16 puntos.
  - Añadir en Armonías **convierte la armonía actual en puntos libres** (base + puntos con sus posiciones canónicas + el nuevo punto).
- **Armonía ya no se rompe**:
  - Arrastrar un punto de la armonía **pasa directamente a Libre** con todos los puntos conservando su posición (el arrastrado en la posición de destino, los demás en la canónica).
  - Se elimina el botón "Restaurar" (y con él los desfases personalizados y el doble click que reiniciaba el punto).
  - En Monocromática los puntos siguen sin ser arrastrables (sus posiciones coinciden con el principal); "+" sí está disponible.
- **Borrar puntos en Libre**: botón "−" en la esquina inferior derecha de la tarjeta, visible al pasar el mouse (como la X de la paleta activa). Debe sobrevivir al menos 1 color (queda como principal): borrar un secundario lo elimina sin cambiar el color actual; borrar el principal promueve automáticamente a principal el siguiente punto ( cambia el color actual a ese color y los demás puntos quedan en sus posiciones absolutas).
- **Comportamiento inalterado en Armonías**: figura canónica exacta, click en punto lo selecciona y ofrece copiar, prioridad del marcador principal, puntos ocultos en otras pestañas; los desfases personalizados ya no existen.

## Capabilities

### New Capabilities
- `free-points`: modo Libre — principal + hasta 16 puntos secundarios de posición relativa, añadidos por click derecho en la rueda o botón "+", arrastrables, borrables con "−" (con promoción de principal), en sesión (sin persistir entre sesiones).

### Modified Capabilities
- `palette-generation`: la pestaña Armonías ya no admite desfases personalizados (se elimina "Restaurar", doble click de reinicio y reseteo ante cambios por otra vía); arrastrar un punto o añadir uno pasa directamente a Libre; nuevo orden de pestañas con "Libre" al final; la regla de tarjetas que prohíbe controles por hover admite la excepción del botón "−" en Libre; "Equilibrar" también gobierna Libre.

## Impact

- **`Matiz.App/ViewModels/MainViewModel.GeneratedPalettes.cs`**: enum `GeneratedTab` (reordenar + `Free`), estado de puntos libres (efectivos Δhue/Δsat + promoción), comandos nuevos (añadir punto, añadir por click derecho, borrar punto, arrastre de punto libre), se eliminan desfases de armonía (`_harmonyOffsets`, `HasHarmonyOffsets`, `ResetHarmonyOffsets`, `ResetWheelMarkerOffset`), conversión Armonía→Libre en drag y en "+".
- **`MainViewModel.cs`**: quitar el reseteo de desfases en `OnSessionChanged`.
- **`Matiz.App/Controls/ColorWheel.cs`**: soporte de click derecho (comando de añadir punto en esa posición) y reutilización del drag/click/deselección para puntos libres.
- **`Matiz.App/Views/MainWindow.xaml`**: reordenar pestañas, añadir "Libre", botón "+", "Equilibrar" visible también en Libre, quitar botón "Restaurar", botón "−" en la plantilla de tarjetas (solo Libre, por hover).
- **`Matiz.Core/Generation/PaletteGenerator.cs`**: generación de puntos libres desde desfases (misma regla de brillo/gama que armonías).
- **`Matiz.App/Localization`** y **`Matiz.Core/Localization/Texts`**: claves nuevas (pestaña "Libre", "+", "−", toasts) y limpieza de `harmony.reset`.
- **`Matiz.Core.Tests`**: tests de generación de puntos libres (offsets, Equilibrar, gama sRGB, límites de saturación).