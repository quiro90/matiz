---
tags: [adr]
---
# ADR-010 Ejecutable autónomo
**Contexto**: Se pidió compilar "nativo" sin tocar código ni lógica.

**Decisión**: Perfil `win-x64-native`: self-contained + single-file + ReadyToRun compuesto, sin trimming ni compresión. Se conserva la publicación ligera `win-x64`.

**Alternativas descartadas**: Native AOT (no compatible con WPF); trimming (WPF usa reflexión desde XAML, rompe en tiempo de ejecución); compresión por defecto (65 MB en lugar de 143 MB, pero ~0,2 s más de arranque; queda como opción).

**Consecuencias**: Un solo `Matiz.exe` que funciona sin instalar .NET y arranca en ~0,7 s; el archivo pesa ~143 MB.

Relacionado: [[Publicación]] · [[ADR-001 WPF]] · [[Multiplataforma]] · cambio `openspec/changes/archive/2026-10-08-add-standalone-distribution/`
