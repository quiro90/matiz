---
tags: [desarrollo, tests]
---
# Tests
```bash
dotnet test Matiz.slnx
```
- `tests/Matiz.Core.Tests` (128): conversiones (incluye ida y vuelta exhaustiva de 16,7 M colores), parser, formatos, sesión/deshacer, escala/armonías/neutros, extracción, paletas, exportación, persistencia (corrupción, migración, escritura atómica).
- `tests/Matiz.App.Tests` (9): PNG con píxel exacto y tamaño 2×, atajos.
- La UI no se testea exhaustivamente: ver [[Pendiente de verificar]].
