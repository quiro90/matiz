---
tags: [arquitectura, persistencia]
---
# Persistencia JSON
`JsonStore<T>`: un documento por archivo en `%APPDATA%\Matiz\` (o `MATIZ_DATA_DIR`).
- Escritura atómica (`.tmp` + `File.Replace` con `.bak`).
- Guardado diferido 300 ms; serializa en el hilo de UI y escribe en segundo plano.
- Recuperación de archivos corruptos y migraciones por `schemaVersion` (v0 = array suelto → v1).
- `System.Text.Json` con source generation (`MatizJsonContext`).

Esquemas → [[Esquema palettes.json]] · [[Esquema settings.json]] · Flujo → [[Flujo de persistencia]]
