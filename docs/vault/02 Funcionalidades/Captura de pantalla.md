---
tags: [funcionalidad, captura]
---
# Captura de pantalla
Toma el **píxel real** visible en cualquier monitor.

- Activación: botón **Capturar** (barra superior) o atajo global **Alt+C** (configurable en [[Ajustes]]).
- Overlay por monitor con cursor de cruz y **lupa** 11×11 (7–21) con cuadrícula, píxel central marcado, HEX y RGB.
- Click / Enter confirma y cierra · **Shift+click / Shift+Enter** confirma y sigue en modo continuo · **click derecho** confirma el píxel como **color secundario** ([[Puntos libres]]) y cierra · Esc cancela · flechas 1 px (Shift 10 px) · rueda = aumento.
- El tooltip del botón Capturar explica el atajo (Alt+C) y el click derecho para el secundario.
- Al confirmar: color actual + [[Historial]] + copia en el formato principal (si "Copiar al capturar") + trae la ventana al frente (si "Mostrar tras capturar").

Por dentro → [[Flujo interno de captura]] · [[Multi-monitor y DPI]] · [[ADR-003 Captura congelada]]
Limitaciones → [[Riesgos y limitaciones]]
Código: `src/Matiz.App/ScreenCapture/`
