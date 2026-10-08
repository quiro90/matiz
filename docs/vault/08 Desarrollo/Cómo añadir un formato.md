---
tags: [desarrollo, export]
---
# Cómo añadir un formato
**Color individual** — en `ColorFormatters.All`:
```csharp
new ColorFormatter("swift", "Swift UIColor",
    (c, _) => $"UIColor(red: {c.R}/255, green: {c.G}/255, blue: {c.B}/255, alpha: 1)"),
```
**Paleta** — en `PaletteFormatters.All` con un `Func<PaletteExportModel, FormatOptions, string>`; usa `IdentifierNaming` para nombres válidos.

Añade un test de salida exacta. La UI lo muestra sola. Ver [[Formatos extensibles]].
