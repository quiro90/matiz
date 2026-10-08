---
tags: [desarrollo]
---
# Compilar y ejecutar
**Lo más fácil**: `.\build.ps1` en la raíz (`build`, `test`, `run`, `publish`, `native`, `clean`, `all`; `-Configuration Debug`). Si Matiz está abierto, ofrece cerrarlo.
Soluciones: `Matiz.sln` (cualquier Visual Studio) y `Matiz.slnx` (formato nuevo).

```bash
dotnet run --project src/Matiz.App
dotnet build Matiz.slnx -c Release
```
- Requiere .NET 10 SDK en Windows.
- `MATIZ_DATA_DIR=<carpeta>` para usar datos aislados en pruebas.
- Instancia única: si ya hay una abierta, la nueva solo la trae al frente.
