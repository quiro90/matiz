# Matiz

**English** · [Español](README.es.md)

A small, fast Windows color tool for developers and designers: pick any pixel on screen, understand it in every format, build palettes and copy them straight into your code.

**Alt+C → click any pixel → copied → scale / harmonies → copy as code → back to work.**

![Matiz](docs/capture.png)

## Features

- **Screen picker**: global hotkey (`Alt+C`, configurable), magnifier with pixel grid, multi-monitor and per-monitor DPI aware, pixel-accurate.
- **Visual picker**: hue/saturation wheel (white center), brightness bar showing the color itself, Vivid ↔ Pastel focus, precise H/S/B inputs and a gray scale.
- **Every format**: HEX, RGB, HSL, HSV, CMYK (approx.), OKLCH, plus code snippets (CSS, C#/WPF, XAML, Flutter/Dart, ARGB). Paste any format to set a color.
- **Design scale 50–950** generated in OKLCH, ready for design systems.
- **Harmonies** (complementary, analogous, split, triadic, tetradic, monochromatic) drawn on the wheel, with balanced lightness.
- **Tints / shades, neutrals** and **dominant colors from images** (open, drop or paste a screenshot).
- **Saved palettes** with autosave, reordering and undo; export as **CSS variables, JSON, Dart, C#, Tailwind** or a **PNG** image.
- Recent colors, undo/redo, light/dark theme, always-on-top, keyboard shortcuts.

> The UI is currently in Spanish.

## Requirements

- Windows 10/11 x64.
- To build: [.NET 10 SDK](https://dotnet.microsoft.com/download).
- To run: nothing (standalone build) or the .NET 10 Desktop Runtime (light build).

## Build

Open `Matiz.sln` in Visual Studio, or use the script:

```powershell
.\build.ps1            # build (Release) + tests
.\build.ps1 run        # build and launch
.\build.ps1 publish    # publish\win-x64\Matiz.exe         (light, ~1.4 MB, needs .NET 10 Desktop Runtime)
.\build.ps1 native     # publish\win-x64-native\Matiz.exe  (single precompiled .exe, ~143 MB, no .NET needed)
.\build.ps1 all        # clean + build + tests + both publishes
```

User data lives in `%APPDATA%\Matiz\` (palettes, history, settings).

## Project

- `src/Matiz.Core` — color model, conversions, palette generation, export, persistence (no UI, cross-platform).
- `src/Matiz.App` — WPF app (MVVM), custom controls, screen capture.
- `tests/` — xUnit (139 tests).
- `openspec/` — spec-driven specifications.
- `docs/vault/` — full documentation as an Obsidian vault (start at `00 Inicio`).

Windows only for now (WPF). A path to Linux/macOS via Avalonia is documented in `docs/vault/01 Producto/Multiplataforma.md`.
