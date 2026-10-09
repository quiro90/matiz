---
tags: [adr]
---
# ADR-011 Puntos libres
**Contexto**: La personalización de posiciones se hacía "rompiendo" una armonía (desfases de los puntos + botón "Restaurar armonía"), lo que dejaba estado ambiguo —no se sabía si se estaba viendo la armonía o una personalización— y hacía perder la figura canónica. No había forma de acumular colores propios partiendo de cualquier pestaña.

**Decisión**: separar en dos modos. **Armonías** siempre canónicas (no se rompen: arrastrar o añadir las **convierte** a Libre). **Libre**: hasta 16 puntos secundarios como desfases hue/sat relativos al principal —siguen rígidamente al principal al moverlo—, con conversión automática al arrastrar, "+" o click derecho (la conversión reemplaza el conjunto), borrado por tarjeta ("−" al hover) y promoción del primer secundario si se quita el principal (posiciones absolutas intactas). El punto del "+" es opuesto al principal si no hay secundarios y, si los hay, al lado del último (hue +30° por paso hasta posición libre ≥10°) para que varios "+" consecutivos no se solapen.

**Alternativas descartadas**: mantener el "romper armonía" in-place (estado ambiguo, botón de restauración y reseteo por múltiples fuentes); acumular puntos personalizados dentro de la pestaña Armonías (mezcla figura + personalizaciones, crece sin control).

**Consecuencias**: se eliminó toda la maquinaria de desfases de armonía (`_harmonyOffsets`/`ResetHarmonyOffsets`/doble click); Libre usa desfases relativos (el seguimiento del principal es gratis) y no persiste entre sesiones; el "+" produce puntos visibles uno al lado del otro.

Relacionado: [[ADR-009 Armonías geométricas]] · [[Puntos libres]] · cambio `openspec/changes/archive/2026-10-09-add-free-points-mode/`