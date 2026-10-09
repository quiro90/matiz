# Matiz

[English](README.md) · **Español**

Una herramienta de color pequeña y rápida para Windows, pensada para desarrolladores y diseñadores: captura cualquier píxel de la pantalla, entiéndelo en todos los formatos, arma paletas y cópialas directo a tu código.

**Alt+C → click en cualquier píxel → copiado → escala / armonías → copiar como código → seguir trabajando.**

![Matiz](docs/capture.png)
![Matiz](docs/capture2.png)

## Funcionalidades

- **Captura de pantalla**: atajo global (`Alt+C`, configurable), lupa con cuadrícula de píxeles, multi-monitor y DPI por monitor, precisión de píxel. Click izquierdo toma el color principal, click derecho lo añade como punto secundario del conjunto Personalizado (hasta 64 colores), y con **Shift** presionado la lupa sigue abierta para capturar varios colores seguidos (Esc cancela).
- **Selector visual**: rueda de tono/saturación (centro blanco), barra de brillo con el propio color, enfoque Vivo ↔ Pastel, campos H/S/B precisos y escala de grises.
- **Todos los formatos**: HEX, RGB, HSL, HSV, CMYK (aprox.), OKLCH y fragmentos de código (CSS, C#/WPF, XAML, Flutter/Dart, ARGB). Pega cualquier formato para fijar un color.
- **Escala 50–950** generada en OKLCH, lista para design systems.
- **Armonías** (complementaria, análoga, dividida, triádica, tetrádica, monocromática) dibujadas en la rueda, con luminosidad equilibrada.
- **Puntos libres**: hasta 64 colores secundarios añadidos con click derecho en la rueda, click derecho al capturar o sobre la imagen (o el botón "+", visible en todas las pestañas y siempre a la misma altura); siguen rígidamente al color principal y personalizar una armonía la convierte a Personalizado en vez de romperla.
- **Tints / shades, neutros** y **colores dominantes de imágenes** (abrir, arrastrar o pegar una captura); en "De la imagen" el botón **"Personalizar"** carga los extraídos de una vez como puntos del conjunto Personalizado (con aviso si reemplaza una paleta ya armada).
- **Paletas guardadas** (hasta 64 colores por paleta) con autoguardado, reordenamiento (también arrastrando las tarjetas) y deshacer; **Recargar** vuelve a poner una paleta guardada en la rueda (modo Personalizado) para editarla; exportación a **variables CSS, JSON, Dart, C#, Tailwind** o **imagen PNG**.
- Colores recientes, deshacer/rehacer, tema claro/oscuro, siempre visible y atajos de teclado.

## Requisitos

- Windows 10/11 x64.
- Para compilar: [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Para ejecutar: nada (versión autónoma) o .NET 10 Desktop Runtime (versión ligera).

## Compilar

Abre `Matiz.sln` en Visual Studio o usa el script:

```powershell
.\build.ps1            # compila (Release) + tests
.\build.ps1 run        # compila y abre la app
.\build.ps1 publish    # publish\win-x64\Matiz.exe         (ligera, ~1,4 MB, requiere .NET 10 Desktop Runtime)
.\build.ps1 native     # publish\win-x64-native\Matiz.exe  (un solo .exe precompilado, ~143 MB, sin instalar .NET)
.\build.ps1 all        # limpia + compila + tests + ambas publicaciones
```

Los datos de usuario se guardan en `%APPDATA%\Matiz\` (paletas, historial, ajustes).

## Publicar en Microsoft Store

El empaquetado MSIX no usa Visual Studio ni `.wapproj`: `.\build.ps1 msix` hace `dotnet publish` self-contained `win-x64`, arma el layout y genera el paquete con `makeappx` del Windows SDK (necesita un **Windows 10/11 SDK** instalado).

1. **Reservar la app**: en [Partner Center](https://partner.microsoft.com/dashboard) → *Aplicaciones de Windows* → reserva el nombre (ya reservado: `JuanQuiroga.Matiz`).
2. **Identidad del paquete**: en la página *Product identity* de la app, copiá **Package identity name** y **Publisher** y pegalos en `packaging/Matiz.Package/Package.appxmanifest` (`Identity Name` / `Identity Publisher`; ya cargados con `JuanQuiroga.Matiz` / `CN=C7BB1DDD-DF8A-49EF-8538-8D09EF4F231B`).
3. **Bump de versión** (si aplica): subila en **`Directory.Build.props` → `<Version>`** y en el manifiesto → `<Identity Version>` **en el mismo commit** (`1.0.2` → `1.0.2.0`); el build falla si quedan destonadas. La versión de Store debe ir sumando (jamás bajar o repetir una ya enviada).
4. **Activos**: si faltan, regenerá los PNG con `packaging/Matiz.Package/Generate-Assets.ps1`.
5. **Prueba local (sideload)**, opcional: `.\build.ps1 msix -Cert` crea un cert de prueba e instala el MSIX firmado (pide UAC para confiar en el cert); doble click en el `.msix` generado.
6. **Generar el paquete**: `.\build.ps1 msix` → `publish/msix-store/JuanQuiroga.Matiz_<versión>_x64.msixupload`.
7. **Completar el envío** en Partner Center: descripción corta/larga, screenshots, ícono 300x300 del listado, URL de política de privacidad (link al README del repo), clasificación de edad, precios, y subí el `.msixupload`.
8. **Al aprobar la Store**: creá el release de GitHub con `publish/win-x64-native/Matiz.exe` (nota qué versión corresponde a la Store).

## Proyecto

- `src/Matiz.Core` — modelo de color, conversiones, generación de paletas, exportación, persistencia (sin UI, multiplataforma).
- `src/Matiz.App` — app WPF (MVVM), controles propios, captura de pantalla.
- `tests/` — xUnit (163 tests).
- `openspec/` — especificaciones (spec-driven).
- `docs/vault/` — documentación completa como vault de Obsidian (empieza por `00 Inicio`).

Por ahora solo Windows (WPF). La ruta a Linux/macOS con Avalonia está documentada en `docs/vault/01 Producto/Multiplataforma.md`.

## Licencia

Apache 2.0 con la [Commons Clause](https://commonsclause.com/): libre para copiar, modificar, compilar y distribuir; la venta del software queda reservada al autor. Ver [LICENSE](LICENSE).

## Apoyar el proyecto

Considerá donar si te es útil:

<a href="https://cafecito.app/juanquiroga"><img src="https://cdn.cafecito.app/imgs/buttons/button_2.png" alt="Invitame un café en cafecito.app"></a> <a href="https://www.paypal.com/ncp/payment/PJXDUSBHSE8DE"><img src="https://www.paypalobjects.com/es_ES/i/btn/btn_donateCC_LG.gif" alt="Doná con PayPal"></a>
