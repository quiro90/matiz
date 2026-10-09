---
tags: [referencia, persistencia]
---
# Esquema palettes.json
```json
{
  "schemaVersion": 1,
  "activePaletteId": "guid",
  "palettes": [{
    "id": "guid", "name": "PuchiApp", "description": "opcional",
    "createdAt": "2026-10-08T12:00:00+00:00", "modifiedAt": "…",
    "colors": [
      { "id": "guid", "name": "Primary", "hex": "#5246BC" },
      { "id": "guid", "name": "Overlay", "hex": "#000000", "alpha": 128 }
    ]
  }]
}
```
`hex` siempre `#RRGGBB`; `alpha` solo si ≠ 255. Ver [[Persistencia JSON]].

Los archivos de exportación **`.mpalette`** ([[Paletas guardadas]]) siguen la misma estructura de paleta: JSON standalone con `schemaVersion`, nombre, descripción, fechas y colores (`hex` + `alpha` opcional). El doble click del SO o "Importar" los agrega siempre como paleta nueva.
