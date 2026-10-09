---
tags: [funcionalidad, paletas]
---
# Paletas guardadas
- Paleta = nombre, descripción, fechas de creación/modificación, colores (nombre opcional + valor). Máximo **64 colores** por paleta (límite compartido con la rueda y la imagen).
- **Paleta activa** siempre visible abajo (área con scroll): click usa el color, **arrastrar reordena** — arrastre evidente, se arrastra por su cuerpo, el origen se atenúa, un separador marca el hueco de destino y hay una franja al final ("soltar aquí")—, nombre editable en el sitio, **tachito X al pasar el mouse** elimina el color, click derecho (copiar, reemplazar con actual, mover, eliminar con deshacer).
- **Biblioteca** ("Paletas de Colores", botón de la paleta activa): crear (`Ctrl+N`), duplicar, **Recargar**, eliminar (con deshacer), renombrar, descripción. **Recargar** muestra un aviso ("se perderán las selecciones actuales") y al confirmar carga la paleta marcada en la rueda en modo Personalizado: primer color como principal, los demás como puntos con su hue/sat/brillo exactos; cierra Biblioteca e imagen y se puede deshacer.
- **Exportar / Importar** (debajo del nombre y descripción): exportar guarda la paleta marcada en un archivo **`.mpalette`** (JSON con nombre, descripción, fechas y colores); guardar como siempre crea una paleta nueva (nombre repetido → sufijo " 2"). Doble click del OS sobre un `.mpalette` abre Matiz e importa la paleta (con la app corriendo, se delega a la instancia viva). El preview de cada paleta muestra **todos** sus colores en filas que envuelven.
- `Ctrl+S` / **+ Paleta** (zona superior del color) agrega el color actual (crea una paleta si no hay).
- **Autoguardado** → [[Persistencia JSON]] · formato → [[Esquema palettes.json]] · [[ADR-005 Autoguardado JSON]]
