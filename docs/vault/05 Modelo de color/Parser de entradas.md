---
tags: [color, referencia]
---
# Parser de entradas
`ColorParser.TryParse` acepta (sin importar mayúsculas/espacios):
- `#5AC`, `5246bc`, `#5246BC`, `#805246BC` (ARGB), `0xFF5246BC`, `0x5246BC`
- `rgb(82, 70, 188)`, `rgba(…, 0.5)`, `rgb(82 70 188)`, `82 70 188`, `82,70,188`
- `hsl(246, 47%, 51%)`, `hsv(…)`, `hsb(…)`, con `deg`/`°`

Fuera de rango → inválido (el color no cambia). Lo usan el campo principal y `Ctrl+V`.
