## Why

Los límites actuales coartan el uso de paletas grandes: los puntos secundarios de la rueda están limitados a 16 y la cantidad de colores extraída de una imagen a 3–10, justo cuando el usuario quiere trabajar con paletas extensas (design systems, extracción detallada). Además, una paleta guardada que hoy no tenga ningún límite carece de regla explícita. Por último, no existe forma de volver a editar una paleta guardada en la rueda cromática: una vez creada, los puntos solo se pueden reconstruir a mano.

## What Changes

- Límite de puntos secundarios en la rueda (modo Libre, desde armonías, botón "+" o click derecho): **16 → 64** (semántica de secundarios: principal + 64 secundarios). Al alcanzar el límite, el click derecho avisa con un toast "Máximo 64 colores; no se añadieron más" y no añade nada; el botón "+" queda deshabilitado como hoy.
- Extracción de colores de imagen: cantidad **por defecto 6** (sin cambio), **mínimo 1**, **máximo 64** (antes 3–10). El cap interno de `DominantColors.Extract` sube a 64 en consecuencia.
- Paleta activa (y cualquier paleta guardada): **máximo 64 colores por paleta** (regla nueva, duro, aplicado en todos los flujos: "+", "Añadir todo", recargar, cambiar de paleta activa o seleccionar colores). Al intentar añadir más se advierte con toast "Máximo 64 colores por paleta" y no se añade nada por encima del límite.
- Botón **"Recargar"** en el menú de paletas (panel Biblioteca, junto a "Nueva"): advierte —"Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." con toast + botón de confirmación (patrón actual de toast con acción)— y al confirmar carga la paleta marcada (activa) en la rueda en modo **Libre**: primer color = principal (color actual), resto = puntos secundarios (Δhue/Δsat relativos al primero). Deshabilitado sin paleta activa con colores. Al confirmar cierra el panel de Biblioteca para mostrar la rueda.
- Versión nueva: **1.0.7**.

## Capabilities

### New Capabilities

- (ninguna)

### Modified Capabilities

- `free-points`: el límite de puntos secundarios sube de 16 a 64 y el click derecho ahora advierte con toast al alcanzar el límite en lugar de fallar en silencio.
- `image-picker`: el rango de la cantidad de colores extraídos pasa de 3–10 a 1–64 (por defecto 6 sin cambio).
- `saved-palettes`: nueva regla de máximo 64 colores por paleta aplicada en todo flujo de añadir (con aviso), y nuevo método "Recargar" que carga la paleta marcada en la rueda cromática como Libre con aviso previo y confirmación por toast.

## Impact

- **Código**:
  - `Matiz.App/ViewModels/MainViewModel.GeneratedPalettes.cs` — `MaxFreePoints` 16 → 64; aviso en `AddFreePointAt` al límite; nuevo flujo "Recargar" (comando + toast de confirmación + carga de offsets).
  - `Matiz.App/ViewModels/MainViewModel.Image.cs` — clamp de `ExtractCount` a (1, 64).
  - `Matiz.App/Views/MainWindow.xaml` — `NumericBox` de extracción `Minimum=1 Maximum=64`; botón "Recargar" en el panel Biblioteca.
  - `Matiz.Core/Generation/DominantColors.cs` — cap interno de extracción 32 → 64.
  - `Matiz.Core/Palettes/PaletteService.cs` — const `MaxColors = 64`; `AddColor`/`AddColors` respetan el límite y permiten a la capa UI saber si se añadió o la paleta está llena.
  - `Matiz.App/Localization/Strings.resx` / `Strings.es.resx` — claves nuevas: "Recargar", tooltip, aviso de confirmación, toast de carga, toast de límite.
- **Docs**: `Publicación.md` (v1.0.7), `Puntos libres`, `ADR-011 Puntos libres`, `Image picker`, `Paletas guardadas` (nueva sección "Recargar"), specs `free-points` / `image-picker` / `saved-palettes` sincronizadas al archivar.
- **Empaquetado**: `Directory.Build.props` (Version/InformationalVersion 1.0.7) y `packaging/Matiz.Package/Package.appxmanifest`.
- **Compatibilidad**: paletas guardadas con más de 64 colores (imposible con el límite nuevo pero posibles de archivos externos o versiones previas) se mantienen tal cual: el límite solo aplica al *añadir nuevos* colores; ninguna paleta existente se recorta ni se bloquea para lectura/exportación. El límite de la rueda (64 secundarios) también aplica solo al añadir: set cargados desde paletas no exceden 63 secundarios (paleta de máx. 64 colores) y el actual no se recorta.