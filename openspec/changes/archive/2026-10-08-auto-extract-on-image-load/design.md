## Context

El extraído de dominantes ya existe (`MainViewModel.Image.cs` → `ExtractColors`, `DominantColors.Extract`, determinista, async con `Task.Run`, activa la pestaña "Extraídas" y toast). Las tres vías de carga de imagen (Ctrl+O → `LoadImageFile`, drag&drop en code-behind, paste/portapapeles) convergen en `ShowImage(BitmapSource, string?)`. Sin tests de UI en el proyecto; sin cambios de algoritmo.

## Goals / Non-Goals

**Goals:**

- Extracción automática de una vez al cargar cualquier imagen.
- Etiqueta "Cantidad de colores:" junto al campo numérico.

**Non-Goals:**

- No se re-extrae automáticamente al cambiar la cantidad (el usuario re-pulsa "Extraer colores").
- No se cambia `DominantColors`, el determinismo ni las acciones de tarjetas.
- No se persiste `ExtractCount` entre sesiones (sigue partiendo en 6).

## Decisions

### D1. Gancho único en `ShowImage()`
Es el punto de convergencia de las tres vías (diálogo, drag&drop, portapapeles): cualquier carga nueva dispara la extracción automática ahí, una sola vez por imagen. Volver al selector (`CloseImage`) y reabrir la misma imagen pasa por `ShowImage` de nuevo → re-extracción; comportamiento esperado y barato (determinista).

### D2. Refactor mínimo del VM
Separar el cuerpo de `[RelayCommand] ExtractColors()` en `ExtractColorsCoreAsync()` (misma lógica: guard `ImageSource is not { } || IsExtracting`, conversión Bgra32, `Task.Run`, `_extracted`, `HasExtracted`, `GeneratedTab`, `RefreshGenerated`, toast). El comando queda en `ExtractColors() => await ExtractColorsCoreAsync()`; `ShowImage` dispara `ExtractColorsCoreAsync()` fire-and-forget (`_ = ...`) al final. El guard `IsExtracting` evita corridas concurrentes.

### D3. Caso borde aceptado: cargar otra imagen durante una extracción
La extracción en curso usa el `BitmapSource` capturado al empezar, y la nueva corrida automática se salta por `IsExtracting`. Resultado: la pestaña muestra la extracción de la imagen anterior hasta un "Extraer colores" manual. Caso raro (extracciones < 1 s); se documenta y no se agrega cancelación.

### D4. Etiqueta en XAML + clave resx nueva
`TextBlock Text="{loc:Loc image.extractCount.label}" VerticalAlignment="Center" Margin="0,0,6,0"` antes del `NumericBox` en el `StackPanel` (Grid.Column=2 de la cabecera de imagen). Clave nueva `image.extractCount.label`: "Cantidad de colores:" (es) / "Number of colors:" (en). El botón mantiene `image.extract` ("Extraer colores") y sus tooltips/automation existentes sin cambios.

## Risks / Trade-offs

- Toast automático al cargar ("N colores extraídos") añade un aviso sin acción del usuario: se conserva como confirmación visual y desaparece solo.
- `ShowImage` gana un side-effect async: se mantiene fire-and-forget con guards para no bloquear el flujo de carga ni duplicar corridas.

## Migration Plan

No aplica (no hay datos ni formato persistido involucrado).

## Open Questions

Ninguna — alcance confirmado con el usuario (etiqueta sí; texto del botón sin cambios).