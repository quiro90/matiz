---
tags: [arquitectura, app]
---
# Matiz.App
`net10.0-windows`, WPF, ensamblado `Matiz.exe`.
- `Views/` — `MainWindow` (una ventana, paneles laterales, sin barra de título del sistema: `WindowChrome` con Minimizar/Cerrar integrados en la barra superior), `ExportImageWindow`.
- `ViewModels/` — `MainViewModel` dividido en parciales por zona (Picker, Current, GeneratedPalettes, Palettes, Settings, Image).
- `Controls/` — `ColorWheel`, `BrightnessSlider`, `GrayStrip`, `NumericBox`, `ImageCanvas`, `MagnifierRenderer`.
- `Localization/` — i18n: resx (inglés por defecto + `es`), extensión `{loc:Loc key}` en XAML y servicio que recarga los textos al cambiar de idioma en caliente.
- `ScreenCapture/` — [[Multi-monitor y DPI]].
- `Services/` — portapapeles con reintentos, [[Temas]], hotkey global, atajos, carga de imágenes.
- `Interop/NativeMethods.cs` — P/Invoke user32/gdi32/shcore/dwmapi.
- Instancia única (mutex + evento) y `error.log` para excepciones no controladas.
