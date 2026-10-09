## Why

La aplicación ya existe y funciona, pero no tiene empaquetado formal: los perfiles actuales de `dotnet publish` generan una carpeta portable y un `Matiz.exe` single-file, sin identidad de paquete ni manifiesto MSIX. Para distribuir vía Microsoft Store se necesita un paquete MSIX con identidad de Partner Center, payload self-contained (la Store no instala el .NET Desktop Runtime por el usuario) y el set completo de íconos que exige el manifiesto. La distribución directa como `.exe` portable para releases de GitHub se mantiene como está.

## What Changes

- Carpeta de empaquetado `packaging/Matiz.Package/` con el `Package.appxmanifest` (identidad de Partner Center, full trust, x64) y el script de íconos.
- `Package.appxmanifest` con identidad (Package Identity Name y Publisher CN de Partner Center), `runFullTrust`, Windows mínimo Win10 y arquitectura x64.
- Payload **self-contained**: el paquete se construye con el runtime .NET incluido; `build.ps1 publish/native` siguen generando las distribuciones directas sin cambios.
- Tarea nueva `.\build.ps1 msix`: `dotnet publish` self-contained + `makeappx`/`signtool` del Windows SDK ya instalado, y genera el `.msixupload` listo para subir a Partner Center, sin firma propia (la Store firma).
- Set de activos de icono MSIX/Store (44x44, 150x150, 310x150, 310x310, StoreLogo) generado desde el arte actual.
- Esquema de versión canónico `x.y.z.w` derivado de `Directory.Build.props` (`1.0.1` → `1.0.1.0`), sin romper la cadena visible "v1.01"; verificación de consistencia en la tarea de empaquetado.
- Documentación breve del proceso de publicación (reservar nombre, listar, subir).

**Fuera de alcance**: migración de datos de la instalación portable hacia el paquete (`%\APPDATA%` → `LocalCache`), cambio del nombre del mutex de instancia única, MSIX firmado para instalación externa, arm64, pipelines de CI.

## Capabilities

### New Capabilities
- `app-packaging`: empaquetado, identidad y distribución de la app (paquete MSIX para Store y distribuciones portables directas).

### Modified Capabilities
<!-- ninguna -->

## Impact

- **Nuevos archivos**: `packaging/Matiz.Package/Package.appxmanifest`, `packaging/Matiz.Package/Generate-Assets.ps1`, `packaging/Matiz.Package/Assets/*` (PNG del set), doc de publicación.
- **Modificados**: `build.ps1` (tarea `msix`), `Directory.Build.props` (sin cambios de versión, solo verificación), README (apartado de publicación). Las soluciones (`Matiz.sln` / `Matiz.slnx`) no cambian.
- **Sin cambios**: código fuente de `Matiz.Core` y `Matiz.App`, persistencia, hotkeys, captura. Las distribuciones portables existentes (`publish\win-x64`, `publish\win-x64-native`) quedan intactas.
- La app ya cumple los requisitos de compatibilidad para paquete full-trust (P/Invoke user32/gdi32/shcore/dwmapi, `RegisterHotKey`, captura por `BitBlt`, datos solo en `%APPDATA%`, `asInvoker`).