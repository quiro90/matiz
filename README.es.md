# Matiz

[English](README.md) · **Español**

Una herramienta de color pequeña y rápida para Windows, pensada para desarrolladores y diseñadores: captura cualquier píxel de la pantalla, entiéndelo en todos los formatos, arma paletas y cópialas directo a tu código.

**Alt+C → click en cualquier píxel → copiado → escala / armonías → copiar como código → seguir trabajando.**

![Matiz](docs/capture.png)

## Funcionalidades

- **Captura de pantalla**: atajo global (`Alt+C`, configurable), lupa con cuadrícula de píxeles, multi-monitor y DPI por monitor, precisión de píxel.
- **Selector visual**: rueda de tono/saturación (centro blanco), barra de brillo con el propio color, enfoque Vivo ↔ Pastel, campos H/S/B precisos y escala de grises.
- **Todos los formatos**: HEX, RGB, HSL, HSV, CMYK (aprox.), OKLCH y fragmentos de código (CSS, C#/WPF, XAML, Flutter/Dart, ARGB). Pega cualquier formato para fijar un color.
- **Escala 50–950** generada en OKLCH, lista para design systems.
- **Armonías** (complementaria, análoga, dividida, triádica, tetrádica, monocromática) dibujadas en la rueda, con luminosidad equilibrada.
- **Tints / shades, neutros** y **colores dominantes de imágenes** (abrir, arrastrar o pegar una captura).
- **Paletas guardadas** con autoguardado, reordenamiento y deshacer; exportación a **variables CSS, JSON, Dart, C#, Tailwind** o **imagen PNG**.
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

## Proyecto

- `src/Matiz.Core` — modelo de color, conversiones, generación de paletas, exportación, persistencia (sin UI, multiplataforma).
- `src/Matiz.App` — app WPF (MVVM), controles propios, captura de pantalla.
- `tests/` — xUnit (139 tests).
- `openspec/` — especificaciones (spec-driven).
- `docs/vault/` — documentación completa como vault de Obsidian (empieza por `00 Inicio`).

Por ahora solo Windows (WPF). La ruta a Linux/macOS con Avalonia está documentada en `docs/vault/01 Producto/Multiplataforma.md`.
