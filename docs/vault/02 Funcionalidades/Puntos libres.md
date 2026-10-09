---
tags: [funcionalidad, paletas]
---
# Puntos libres
Modo de [[Paletas generadas]] para armar paletas personalizadas: el color actual es el **principal** (tarjeta base) y se le suman hasta **16 puntos secundarios** definidos por su posición relativa en la rueda (Δhue/Δsat).

- **Añadir**: click derecho sobre la [[Selector visual|rueda]] desde cualquier pestaña, o el botón **"+"** junto a "Luminosidad equilibrada" (Armonías y Libre). El primer punto es opuesto al principal (180°); los siguientes se colocan **al lado del último** (avanza el hue hasta no solaparse). Cualquiera pasa automáticamente a Libre.
- Desde **Armonías**: arrastrar un punto, click derecho o "+" **convierte la armonía actual** en conjunto libre (ángulos canónicos + el punto nuevo) y reemplaza los puntos previos — las armonías ya no se "rompen".
- **Seguimiento rígido**: al cambiar el principal por cualquier vía (rueda, brillo, campos, historial, captura, imagen, manual, undo/redo) los puntos se trasladan con él conservando sus ángulos. La saturación se limita al disco de la rueda.
- **Luminosidad equilibrada** funciona igual que en [[Armonías]] (switch común).
- **Quitar**: al pasar el mouse, la tarjeta muestra "−" abajo a la derecha; el **doble click** sobre un punto en la rueda también lo elimina (desde Armonías pasa a Libre con la figura menos ese punto). El botón **"Borrar"** —chip junto a "+", visible solo en Libre con puntos— quita **todos** los secundarios de una vez y conserva el principal. Quitar un secundario lo elimina sin tocar los demás; quitar el principal promueve al primer secundario (su color pasa a ser el actual como cambio confirmado y el resto conserva sus posiciones absolutas). Siempre queda al menos un color.
- Etiquetas por número (1, 2, …); al agregar/exportar se nombran `Prefijo N` (p. ej. `Primary 1`). Borrar puntos no es deshacible y el conjunto no persiste entre sesiones.

Decisión → [[ADR-011 Puntos libres]] · cambio `openspec/changes/archive/2026-10-09-add-free-points-mode/`