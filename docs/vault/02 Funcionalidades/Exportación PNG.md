---
tags: [funcionalidad, export]
---
# Exportación PNG
Botón **PNG** en la paleta activa (`Ctrl+E`).
- Título + bloques grandes + nombre, HEX, RGB (HSL y CMYK opcionales).
- Horizontal (hasta 6 por fila) o vertical · escala 1×/2×/3× · fondo claro/oscuro · vista previa · copiar imagen.
- Bloques sin antialiasing: el centro de cada bloque tiene **exactamente** el color (test automático).

Código: `src/Matiz.App/Imaging/PaletteImageRenderer.cs`
