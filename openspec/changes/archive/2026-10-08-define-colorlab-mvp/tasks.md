<!--
Plan por fases. Cada fase termina en un entregable usable y en una tarea final "Aceptación de fase"
que verifica los criterios de aceptación. Hito MVP núcleo = fin de fase 3 (ALT+C → click → copiar).
v1.0 = fin de fase 6. Fases 7–8 completan el alcance de este cambio.
Cambios respecto a la propuesta original de 10 fases:
- Clipboard básico pasa a fase 1 (copiar es parte del flujo central desde el primer día).
- Historial se une a la captura (fase 3) porque se alimenta de ella; su persistencia va con la fase 5.
- Formatos de código y exportar imagen se agrupan (fase 6): comparten el modelo de exportación.
- Shortcuts y configuración no esperan al final: cada fase añade sus atajos; la fase 8 solo pule.
-->

## 1. Fundamentos: solución, modelo de color y panel de color actual

- [x] 1.1 Crear `Matiz.slnx` con `src/Matiz.Core` (net10.0), `src/Matiz.App` (net10.0-windows, WPF) y `tests/Matiz.Core.Tests` (xUnit), `Directory.Build.props` (nullable, warnings as errors, LangVersion latest) y `.gitignore`; verificar con `dotnet build` y `dotnet test` en verde
- [x] 1.2 Añadir `app.manifest` con PerMonitorV2 y verificar con un log de `GetDpiForWindow` en dos monitores con escalas distintas
- [x] 1.3 Implementar `Argb` y `ColorState` (HSV + alpha) con redondeo canónico; verificar con tests de redondeo (127.5 → 128) y de construcción
- [x] 1.4 Implementar conversiones RGB↔HSV, RGB↔HSL, RGB→CMYK, RGB↔OKLab/OKLCH; verificar con tests de valores de referencia (`#5246BC` → HSV 246/63/74, HSL 246/47/51, CMYK 56/63/0/26; blanco, negro, grises, primarios) y test de ida y vuelta exhaustivo HEX↔HSV/HSL
- [x] 1.5 Implementar preservación de hue/saturación para colores acromáticos (`ColorState.FromArgb(argb, previous)`); verificar con tests de los escenarios de `current-color`
- [x] 1.6 Implementar `ColorParser` con todos los formatos de `color-model` y rechazo de entradas inválidas; verificar con tests por formato y por caso de error
- [x] 1.7 Implementar `IColorFormatter` + registro (HEX, RGB, HSL, HSV, CMYK, OKLCH, CSS rgb/hsl/oklch, C# FromRgb/FromArgb, XAML, Dart, ARGB entero, "Copiar todo") con opciones de HEX; verificar con tests de salida exacta
- [x] 1.8 Crear `MainWindow` con layout de zonas (D11) vacías, tokens de tema claro/oscuro y `ThemeService` (incluye spike de `ThemeMode` Fluent y decisión documentada); verificar cambiando de tema en caliente
- [x] 1.9 Implementar `CurrentColorService` (preview/commit, anterior/actual, deshacer/rehacer 50, coalescencia por frame); verificar con tests unitarios del servicio (no requiere UI)
- [x] 1.10 Implementar panel de color actual: muestras anterior/actual, formatos con copiar, "Copiar todo", "Copiar como ▾", campos HEX y R/G/B validados, `Ctrl+C`, `Ctrl+Shift+C`, `Ctrl+V`, `Ctrl+Z/Y`; verificar manualmente con `#5246BC`
- [x] 1.11 Implementar `ClipboardService` con reintentos y toast "Copiado"; verificar bloqueando el portapapeles desde un script de prueba
- [ ] 1.12 Aceptación de fase 1: la app abre en < 1,5 s; escribir/pegar `#5246BC`, `rgb(82,70,188)` o `0xFF5246BC` muestra todos los formatos correctos; cada copiar deja el texto exacto en el portapapeles; Ctrl+Z/Y y "Anterior" funcionan; temas claro/oscuro; `dotnet test` en verde

## 2. Selector visual

- [x] 2.1 Implementar mapeo rueda↔(H,S) con enfoque `γ` en Core (`WheelMapping`); verificar con tests de ida y vuelta posición↔color y de "0–30% ocupa > 50% del radio en Pastel"
- [x] 2.2 Implementar control `ColorWheel` (bitmap en caché por tamaño/DPI/γ, borde antialiasado, marcador de doble anillo, click/arrastre, Shift fino, límite en el borde); verificar que el bitmap no se regenera al arrastrar (contador de regeneraciones en debug)
- [x] 2.3 Implementar `BrightnessSlider` con degradado HSV(H,S,1)→negro; verificar escenario "B 37% → `#29235E`"
- [x] 2.4 Implementar slider "Enfoque" Vivo↔Pastel; verificar que cambiarlo no altera el color y mueve el marcador
- [x] 2.5 Implementar campos H (1 decimal) / S / B con validación, rueda del ratón y flechas (±1/±10); verificar escenarios de `visual-picker`
- [x] 2.6 Implementar `GrayStrip` (100→0) y acción "Gris equivalente" (OKLab L); verificar click en 50 → `#808080` y test de gris equivalente ±0.005
- [x] 2.7 Conectar todo al `CurrentColorService` (preview durante arrastre, commit al soltar); verificar que la entrada HEX mueve marcador y brillo y que deshacer revierte un arrastre completo
- [ ] 2.8 Aceptación de fase 2: rueda y brillo fluidos a la tasa de refresco del monitor con la ventana maximizada en 4K; centro blanco, borde puro, 120° borde = `#00FF00`; bajar brillo a 0 y volver recupera el color exacto; ningún estado del selector diverge del color actual

## 3. Captura de pantalla e historial (hito MVP núcleo)

- [x] 3.1 Spike: enumerar monitores (rect físico, DPI), capturar cada uno con `BitBlt`+`CAPTUREBLT` y volcar a PNG; verificar con 2 monitores de DPI distinto y uno en coordenadas negativas
- [x] 3.2 Implementar `OverlayWindow` por monitor (topmost, sin bordes, posicionada con `SetWindowPos` en físico, imagen 1:1 con NearestNeighbor, cursor cruz); verificar que la instantánea coincide píxel a píxel con el escritorio
- [x] 3.3 Implementar `Magnifier` (N×N, 7–21, cuadrícula, píxel central, HEX/RGB, reposicionamiento en bordes) leyendo `GetCursorPos`; verificar en las cuatro esquinas de cada monitor
- [x] 3.4 Implementar confirmar (click/Enter), cancelar (Esc/click derecho), flechas ±1/±10 con `SetCursorPos`, rueda = aumento; verificar escenarios de `screen-picker`
- [x] 3.5 Implementar `HotkeyService` (`RegisterHotKey` Alt+C, aviso si falla) y botón "Capturar"; verificar desde otra app con Matiz minimizado y con el atajo ocupado por otra app
- [x] 3.6 Implementar acciones post-captura: commit, copiar al capturar, mostrar ventana tras capturar (ajustes en memoria por ahora); verificar flujo ALT+C → click → pegar en un editor
- [x] 3.7 Implementar `ColorHistory` en Core (política de inserción, dedupe, límite 30) y fila "Recientes" en la UI; verificar con tests de Core y manualmente
- [ ] 3.8 Aceptación de fase 3 (MVP núcleo): matriz manual multi-monitor (100%+150%, monitor a la izquierda y encima del principal, vertical) capturando un `#5246BC` de referencia en cada uno devuelve exactamente `#5246BC`; ALT+C → click → `Ctrl+V` en otra app pega el HEX; Esc no cambia nada; la lupa responde sin retraso perceptible

## 4. Generación de paletas

- [x] 4.1 Implementar ajuste a gama sRGB en OKLCH (búsqueda binaria de croma); verificar con tests de colores fuera de gama
- [x] 4.2 Implementar armonías (complementaria, análoga, dividida, triádica, tetrádica) y monocromática; verificar con tests de rotación de hue y |ΔL| < 0.01
- [x] 4.3 Implementar tints/shades sRGB; verificar Tint 50% `#A9A3DE` y Shade 50% `#29235E`
- [x] 4.4 Implementar Design Scale (curva de referencia, anclaje 500/automático, campana de croma, ancla exacta) y Neutros; verificar con tests de L estrictamente decreciente, ΔL ≥ 0.02, en gama, ancla exacta, y golden tests para `#5246BC`, `#FACC15`, `#22C55E`, `#000000`, `#FFFFFF`
- [x] 4.5 Implementar panel de paletas generadas (pestañas Escala/Armonías▾/Tints-Shades/Neutros, tarjetas con HEX/RGB, click = color actual, copiar HEX/RGB) recalculado en vivo; verificar arrastrando la rueda
- [x] 4.8 Mostrar los colores de la armonía como puntos en la rueda (click selecciona y ofrece copiar, doble click usa); verificar escenarios "Armonías visibles en la rueda"
- [x] 4.6 Revisión visual de la Design Scale con 10 colores variados y ajuste de constantes si hace falta; registrar las constantes finales en `design.md`
- [x] 4.7 Aceptación de fase 4: todas las paletas se actualizan en vivo durante el arrastre sin caída de fluidez; la escala de `#5246BC` tiene el base en 500 exacto y 11 pasos distinguibles; todos los tests de generación en verde

## 5. Paletas guardadas y persistencia

- [x] 5.1 Implementar `JsonStore<T>` (escritura atómica tmp+replace+bak, debounce, recuperación de corruptos, migraciones por `schemaVersion`); verificar con tests en directorio temporal (corrupción, archivo inexistente, migración v0→v1)
- [x] 5.2 Implementar modelo `Palette`/`PaletteColor` y `PaletteService` (crear, renombrar, descripción, duplicar, eliminar, agregar, renombrar color, reemplazar, eliminar color, mover, fechas); verificar con tests unitarios
- [x] 5.3 Implementar `PaletteRepository`, `HistoryRepository`, `SettingsRepository`; verificar persistencia tras reiniciar (tests + manual)
- [x] 5.4 Implementar barra de paleta activa (muestras con nombre, `+`/`Ctrl+S`, click = color actual, menú contextual renombrar/reemplazar/eliminar/mover, arrastrar y soltar); verificar escenarios de `saved-palettes`
- [x] 5.5 Implementar panel lateral "Biblioteca de paletas" (lista, crear `Ctrl+N`, renombrar, duplicar, eliminar con deshacer, seleccionar activa); verificar manualmente
- [x] 5.6 Implementar "Agregar a paleta" y "Agregar todo" (con prefijo de nombre para escalas) desde paletas generadas; verificar "Primary 50…950"
- [x] 5.7 Persistir ajustes e historial (último color, tema, enfoque, posición de ventana con corrección si el monitor no existe); verificar desconectando un monitor
- [ ] 5.8 Aceptación de fase 5: crear "PuchiApp" con 4 colores nombrados, reordenar, duplicar y reiniciar conserva todo; matar el proceso durante un guardado no corrompe `palettes.json`; un archivo corrupto se recupera con aviso

## 6. Exportación: código e imagen

- [x] 6.1 Implementar `IdentifierNaming` (kebab/camel/Pascal, colisiones, nombres vacíos → `colorN`); verificar con tests
- [x] 6.2 Implementar `IPaletteFormatter` + registro: CSS variables, JSON, Dart/Flutter, C#, Tailwind; verificar con tests de salida exacta de los escenarios de `palette-export`
- [x] 6.3 Añadir "Copiar como ▾" a paleta activa, biblioteca y paletas generadas/escala; verificar pegando el resultado en un archivo .css/.dart/.cs y compilando/validando
- [x] 6.4 Implementar `PaletteImageRenderer` (horizontal/vertical, 1×/2×/3×, fondo claro/oscuro, HSL/CMYK opcionales) y diálogo con vista previa (`Ctrl+E`); verificar dimensiones 2× = 2 × 1× y píxel exacto en el centro de cada bloque (test que renderiza y lee el PNG)
- [ ] 6.5 Aceptación de fase 6 (v1.0): flujo completo "capturar → escala → agregar todo → copiar como CSS → exportar PNG" sin errores; los fragmentos copiados son código válido en su lenguaje

## 7. Image picker

- [x] 7.1 Implementar carga de imagen (diálogo `Ctrl+O`, arrastrar y soltar, `Ctrl+V` con imagen) vía WIC, con error para formatos no soportados; verificar con PNG, JPG, WebP y un .txt
- [x] 7.2 Implementar visor con ajuste, zoom con rueda, desplazamiento, lupa sobre píxeles originales y click = color actual + historial; verificar el escenario de imagen al 25%
- [x] 7.3 Implementar `DominantColors` (k-means++ en OKLab, semilla fija, downscale ≤128 px) en Core y acción "Extraer colores" (3–10) asíncrona; verificar con tests de determinismo y con una imagen 6000×4000 (< 1 s, UI fluida)
- [ ] 7.4 Aceptación de fase 7: pegar una captura, tomar 3 colores y extraer 6 dominantes que aparecen como paleta generada con todas sus acciones

## 8. Pulido, ajustes y distribución

- [x] 8.1 Implementar panel de ajustes completo (tema, siempre visible, atajo global con captura de combinación y validación, formato por defecto, opciones HEX, copiar/mostrar tras capturar, anclaje de escala); verificar que cada ajuste persiste y se aplica sin reiniciar
- [x] 8.2 Centralizar los atajos de ventana en una tabla de comandos (preparado para hacerlos configurables) y verificar que no interfieren con la edición en campos de texto
- [x] 8.3 Implementar instancia única (mutex + activación de la ventana existente) y "Siempre visible"; verificar lanzando dos veces
- [ ] 8.4 Revisión de accesibilidad: orden de tabulación, foco visible, nombres accesibles, tooltips; verificar con Narrador y solo teclado
- [x] 8.8 Barra de título integrada (WindowChrome) con Minimizar/Cerrar en la barra superior y posición izquierda/derecha configurable; verificar arrastre, snap, redimensionado y persistencia del ajuste
- [x] 8.5 Ayuda breve del modo captura (limitaciones HDR/pantalla completa/contenido protegido); verificar que es accesible desde la UI
- [ ] 8.6 Perfil de publicación framework-dependent win-x64 con ReadyToRun; verificar arranque en frío < 1,5 s en una máquina limpia con .NET 10 Desktop Runtime
- [ ] 8.7 Aceptación de fase 8: recorrido completo del flujo de `proposal.md` en tema claro y oscuro, con DPI mixto, solo con teclado donde aplique; `openspec validate define-colorlab-mvp --strict` sin errores
