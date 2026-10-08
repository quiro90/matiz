---
tags: [referencia]
---
# Constantes
| Qué | Valor | Dónde |
|---|---|---|
| Deshacer | 50 commits | `ColorSession.UndoLimit` |
| Historial | 30 colores | `ColorHistory.Capacity` |
| L de referencia de la escala | 0.975, 0.945, 0.89, 0.82, 0.72, 0.63, 0.55, 0.47, 0.39, 0.32, 0.24 | `DesignScale.ReferenceL` |
| ΔL mínimo | 0.02 | `DesignScale.MinStep` |
| Caída de croma | 0.6 | `DesignScale.ChromaFalloff` |
| Croma de neutros | min(0.015, C·0.12) | `PaletteGenerator.Neutrals` |
| Armonías: equilibrio de L | búsqueda binaria de V (40 iteraciones), tope V = 100% | `PaletteGenerator.ValueForLightness` |
| Enfoque γ | 2^(1.25·k) | `WheelMapping` |
| Lupa | 11 (7–21) | `ScreenPickerController` |
| Ajuste fino de la rueda | ×0.25 | `ColorWheel` |
| Guardado diferido | 300 ms | `JsonStore` |
| Extracción | ≤128 px, k-means++ semilla 1234 | `DominantColors` |
