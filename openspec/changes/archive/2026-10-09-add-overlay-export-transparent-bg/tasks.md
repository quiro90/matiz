# Tareas: add-overlay-export-transparent-bg

## 1. Renderer clásico: fondo transparente

- [x] 1.1 Agregar `bool Transparent = false` a `PaletteImageOptions` (después de `DarkBackground`) y en `PaletteImageRenderer.Render` omitir el rectángulo de fondo cuando aplique, conservando estilo claro para texto/bordes; verificar `dotnet test tests/Matiz.App.Tests` sigue en verde
- [x] 1.2 Agregar test STA: con `Transparent = true` las zonas de fondo del PNG decodificado tienen alfa 0 y los bloques conservan su color exacto

## 2. Renderer de superpuestos

- [x] 2.1 Crear `Imaging/OverlayImageRenderer.cs` con `OverlayImageOptions` (DarkBackground, Scale, Shape {Square,Circle,Triangle}, Reverse, ShowHsl, ShowCmyk) y `LayoutSize/Render` (pila incremental con inset = base/(N+1), piso 6 DIP; capas opacas; borde por capa; listado de valores a la derecha); verificar que compila y renderiza sin excepción para N = 1, 2, 8 y 64
- [x] 2.2 Test STA: pila de 8 en cuadrado, orden primero→último — el punto interior de la capa k coincide con su color y anillos decrecientes son distinguibles; test de orden invertido (último color como capa mayor); test de forma círculo; verificar `dotnet test` verde
- [x] 2.3 Reusar `PaletteImageRenderer.SavePng` para guardar (sin duplicar encoder); verificar guardado/copia manual desde la ventana

## 3. Ventana de superpuestos y wiring

- [x] 3.1 Crear `Views/ExportOverlayWindow.xaml(.cs)` con preview + opciones (FORMA segmentado, ORDEN segmentado, TAMAÑO 1×/2×/3×, FONDO claro/oscuro, INFORMACIÓN HSL/CMYK, Guardar PNG/Copiar imagen/Cerrar) con re-render en vivo y `theme.ApplyTitleBar`; verificar apertura con paleta real desde la app
- [x] 3.2 Agregar `ShowExportOverlay` a `IShell` e implementarlo en `MainWindow`; agregar `ExportActivePaletteOverlayCommand` en `MainViewModel.Palettes.cs` (CanExecute = HasActiveColors + NotifyCanExecuteChanged en el mismo lugar que el comando existente); verificar Ctrl+E no cambió y que la ventana abre asociada a `MainWindow`

## 4. UI: desplegable del botón PNG y botón Anterior/Actual

- [x] 4.1 Definir `ContextMenu x:Key="ExportPaletteMenu"` (2 items: "Imagen", "Exportar superpuestos") en `Window.Resources` y convertir el botón PNG en desplegable con `OpenMenu_Click`; verificar manualmente las dos entradas y que "Imagen" abre la ventana clásica con la opción Transparente en el segmento FONDO
- [x] 4.2 En `MainViewModel.Current.cs` agregar `ExportCurrentPreviousOverlayCommand` (modelo `[Anterior, Actual]` con `color.previous`/`color.current`) y el botón "Exportar superpuestos" en el `WrapPanel` de la tarjeta de color actual; verificar que abre la ventana con la dupla correcta
- [x] 4.3 Agregar claves de localización nuevas a `Strings.resx` y `Strings.es.resx` (`export.transparent`, `overlay.*`, tooltip del botón) y verificar que la UI en es/en muestra textos correctos

## 5. Validación final

- [x] 5.1 `dotnet build` de la solución y `dotnet test` completo en verde; smoke test manual: exportar PNG transparente, superpuestos de paleta (formas/órdenes), superpuestos de Anterior/Actual y guardar/copiar
- [x] 5.2 Validar el change: `openspec validate add-overlay-export-transparent-bg --strict`

## 6. Correcciones tras prueba del usuario

- [x] 6.1 Corregir el desplegable del botón PNG: los `MenuItem` enlazaban sin el prefijo `DataContext.` al usar `RelativeSource AncestorType=ContextMenu` (el `Command` quedaba nulo y no ejecutaba nada); aplicar el patrón de los menús existentes y registrar regresión manual de ambas entradas
- [x] 6.2 Agregar la opción "Transparente" al segmento FONDO de la ventana de superpuestos (renderer: sin rectángulo de fondo, PNG con alfa y estilo claro para texto/bordes), con test STA y delta de spec actualizado
- [x] 6.3 Renombrar etiquetas: botón "PNG" → "Exportar"; ítems del desplegable "Imagen" → "Paleta" y "Exportar superpuestos" → "Superpuestos"; claves `palette.export`/`palette.exportImageItem`/`palette.exportOverlayItem` en es/en (se retira `export.image`, sin usos); delta de spec actualizado