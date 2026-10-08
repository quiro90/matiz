---
tags: [producto, backlog, arquitectura]
---
# Multiplataforma (futuro)
**Hoy Matiz solo compila para Windows**: la UI es WPF y la captura usa Win32 ([[ADR-001 WPF]]).

## Qué ya es multiplataforma
- **`Matiz.Core` (net10.0, sin WPF)**: color, conversiones, parser, formatos, escala, armonías, extracción, paletas, exportación, persistencia, sesión/deshacer. No usa APIs de Windows, así que debería compilar y pasar sus tests en Linux y macOS sin cambios (no verificado aún). Es la mitad del proyecto y toda la lógica.
- Los view models dependen poco de WPF (CommunityToolkit.Mvvm es multiplataforma).

## Qué habría que portar
| Pieza | Hoy (Windows) | Ruta multiplataforma |
|---|---|---|
| UI | WPF | **Avalonia UI** (XAML + MVVM muy parecido a WPF; admite Native AOT) |
| Rueda, brillo, lupa, visor | `FrameworkElement` + `WriteableBitmap` | Controles Avalonia equivalentes (`WriteableBitmap` existe) |
| Captura de pantalla | `BitBlt` + overlay por monitor | macOS: ScreenCaptureKit / `CGWindowListCreateImage` (pide permiso de grabación de pantalla). Linux X11: `XGetImage`. Linux Wayland: portal `org.freedesktop.portal.Screenshot` (tiene `PickColor`); los overlays libres no están permitidos |
| Atajo global | `RegisterHotKey` | macOS: Carbon `RegisterEventHotKey` / CGEventTap (permiso de accesibilidad). X11: `XGrabKey`. Wayland: portal `GlobalShortcuts` (no todos los escritorios) |
| Portapapeles, diálogos, temas | APIs WPF/Win32 | APIs de Avalonia |
| Barra de título | `WindowChrome` | `ExtendClientAreaToDecorationsHint` de Avalonia |

## Enfoque sugerido
1. Separar una interfaz `IScreenColorPicker` / `IGlobalHotkey` por plataforma.
2. Nuevo proyecto `Matiz.Desktop` (Avalonia) reutilizando `Matiz.Core` y los view models.
3. Windows primero (paridad), luego macOS, luego Linux X11 y Wayland (la captura libre está más restringida).

Riesgo principal: Wayland limita la captura y los atajos globales por diseño de seguridad.
Ver [[Alcance y backlog]].
