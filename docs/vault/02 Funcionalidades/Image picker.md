---
tags: [funcionalidad, imagen]
---
# Image picker
- Abrir (`Ctrl+O`), arrastrar archivo o **pegar** una imagen (`Ctrl+V`). PNG, JPG, BMP, GIF, TIFF, WebP.
- Sustituye a la rueda; **← Selector** o `Esc` para volver.
- Rueda = zoom en el cursor · arrastrar = mover · click = toma el píxel **de la imagen original** (no del render) + lupa.
- **Extraer colores** (3–10, con etiqueta "Cantidad de colores:"): k-means++ en OKLab, determinista → pestaña "De la imagen" en [[Paletas generadas]]. **Al cargar una imagen se extrae solo** con la cantidad vigente (6 por defecto); el botón re-extrae, ej. tras cambiar la cantidad.

Flujo → [[Flujo color desde imagen]]
