---
tags: [desarrollo, spec]
---
# OpenSpec
Desarrollo spec-driven.
- **Specs vigentes**: `openspec/specs/<capacidad>/spec.md` (11 capacidades, 58 requisitos): fuente de verdad del comportamiento.
- **Cambios archivados** (`openspec/changes/archive/`):
  - `2026-10-08-define-colorlab-mvp` — producto inicial (propuesta, diseño, tareas).
  - `2026-10-08-improve-harmony-precision` — armonías geométricas con luminosidad equilibrada ([[ADR-009 Armonías geométricas]]).
  - `2026-10-08-add-standalone-distribution` — ejecutable autónomo ([[ADR-010 Ejecutable autónomo]]).
- `proposal.md` (por qué / qué) · `design.md` (cómo, decisiones) · `tasks.md` (fases) · `specs/<capacidad>/spec.md` (requisitos con escenarios).
- Nuevos cambios: `/opsx:propose "<idea>"` → `/opsx:apply` → `/opsx:archive` (sincroniza las specs).
- Validar: `openspec validate --specs --strict`.
- Las ADR de este vault (p. ej. [[ADR-002 HSV + OKLCH]]) resumen `design.md`.
