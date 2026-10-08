---
tags: [arquitectura, export]
---
# Formatos extensibles
- `IColorFormatter` → registro `ColorFormatters.All` (color individual).
- `IPaletteFormatter` → registro `PaletteFormatters.All` (paletas/escalas), sobre `PaletteExportModel` (nombre + colores).
- La UI construye los menús "Copiar como" iterando los registros: **añadir un formato no toca la UI**.

Guía → [[Cómo añadir un formato]]
