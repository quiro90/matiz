---
tags: [funcionalidad, imagen]
---
# Image picker
- Abrir (`Ctrl+O`), arrastrar archivo o **pegar** una imagen (`Ctrl+V`). PNG, JPG, BMP, GIF, TIFF, WebP.
- Sustituye a la rueda; **← Selector** o `Esc` para volver.
- Rueda = zoom en el cursor · arrastrar = mover · click = toma el píxel **de la imagen original** (no del render) + lupa · **click derecho** = añade un punto secundario ([[Puntos libres]]) con el píxel de la imagen.
- **Personalizar** (botón junto al "+", solo en "De la imagen"): carga los extraídos como puntos del conjunto Personalizado — reemplaza lo que hubiera; con 2+ colores en Personalizado avisa y pide confirmación ([[Puntos libres]]).
- **Extraer colores** (1–64, con etiqueta "Cantidad de colores:"): k-means++ en OKLab, determinista → pestaña "De la imagen" en [[Paletas generadas]]. **Al cargar una imagen se extrae solo** con la cantidad vigente (6 por defecto); el botón re-extrae, ej. tras cambiar la cantidad. Límite compartido de 64 colores con [[Paletas guardadas]].

Flujo → [[Flujo color desde imagen]]
