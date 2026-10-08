---
tags: [flujo, captura]
---
# Flujo interno de captura
1. `HotkeyService` recibe `WM_HOTKEY` (o botón) → `CaptureCommand`.
2. `ScreenPickerController.Start` → `MonitorSnapshot.CaptureAll()` (`BitBlt SRCCOPY|CAPTUREBLT` por monitor).
3. Una `OverlayWindow` por monitor, posicionada con `SetWindowPos` en **píxeles físicos**, muestra la instantánea 1:1.
4. Cada movimiento: `GetCursorPos` (físico) → píxel de la instantánea → lupa.
5. Confirmar → cerrar overlays, liberar memoria → callback → `Session.Commit(..., ScreenCapture)`.

Ver [[Multi-monitor y DPI]] · [[ADR-003 Captura congelada]]
