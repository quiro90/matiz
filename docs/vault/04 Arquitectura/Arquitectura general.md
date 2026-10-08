---
tags: [arquitectura]
---
# Arquitectura general
```mermaid
flowchart TB
  subgraph App[Matiz.App · WPF]
    V[Views XAML] --> VM[MainViewModel]
    C[Controles: rueda, brillo, grises, lupa, visor]
    SC[ScreenCapture]
    SV[Services: portapapeles, tema, hotkey]
  end
  subgraph Core[Matiz.Core · sin WPF]
    M[Colors] --- P[Parsing / Formatting]
    G[Generation] --- PA[Palettes / Export]
    PS[Persistence] --- SE[Session / History / Settings]
  end
  VM --> Core
```
- Dos proyectos + tests → [[ADR-006 Dos proyectos]].
- MVVM con CommunityToolkit.Mvvm; composición manual en `App.xaml.cs` (sin contenedor DI).

Detalles: [[Matiz.Core]] · [[Matiz.App]] · [[Mapa del código]]
