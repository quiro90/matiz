---
tags: [flujo, arquitectura]
---
# Flujo de un cambio de color
```mermaid
sequenceDiagram
  participant UI as Rueda / campo / captura
  participant VM as MainViewModel
  participant S as ColorSession
  UI->>VM: Hue/Saturation (arrastre)
  VM->>S: SetPreview(state)
  S-->>VM: Changed(Preview)
  VM->>VM: refresco coalescido (1 por frame)
  UI->>VM: soltar ratón
  VM->>S: Commit(state, source)
  S-->>VM: Changed(Commit) → Refresh + historial + guardar último color
```
- **Preview**: no entra en deshacer. **Commit**: actualiza anterior/actual y la pila de deshacer.
- Fuentes que añaden al [[Historial]]: captura, imagen, entrada manual.

Ver [[Fuente única de verdad]].
