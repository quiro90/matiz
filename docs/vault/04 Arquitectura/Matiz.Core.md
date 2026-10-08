---
tags: [arquitectura, core]
---
# Matiz.Core
`net10.0`, sin WPF ni dependencias NuGet. 100% testeable.
- `Colors/` — `Argb`, `ColorState`, `ColorMath`, `WheelMapping` → [[Modelo canónico]]
- `Parsing/` — `ColorParser` → [[Parser de entradas]]
- `Formatting/` — `ColorFormats`, registro `ColorFormatters` → [[Formatos extensibles]]
- `Generation/` — `DesignScale`, `PaletteGenerator`, `DominantColors`
- `Session/` — `ColorSession` → [[Fuente única de verdad]]
- `Palettes/`, `Export/`, `History/`, `Settings/`, `Persistence/`
