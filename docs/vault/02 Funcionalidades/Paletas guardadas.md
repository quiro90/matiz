---
tags: [funcionalidad, paletas]
---
# Paletas guardadas
- Paleta = nombre, descripción, fechas de creación/modificación, colores (nombre opcional + valor). Máximo **64 colores** por paleta (límite compartido con la rueda y la imagen).
- **Paleta activa** siempre visible abajo: click usa el color, arrastrar reordena, nombre editable en el sitio, **tachito X al pasar el mouse** elimina el color, click derecho (copiar, reemplazar con actual, mover, eliminar con deshacer).
- **Biblioteca** (botón de la paleta activa): crear (`Ctrl+N`), duplicar, **Recargar**, eliminar (con deshacer), renombrar, descripción. **Recargar** muestra un aviso ("se perderán las selecciones actuales") y al confirmar carga la paleta marcada en la rueda en modo Libre: primer color como principal, los demás como puntos con su hue/sat/brillo exactos; cierra Biblioteca e imagen y se puede deshacer.
- `Ctrl+S` / **+ Paleta** (zona superior del color) agrega el color actual (crea una paleta si no hay).
- **Autoguardado** → [[Persistencia JSON]] · formato → [[Esquema palettes.json]] · [[ADR-005 Autoguardado JSON]]
