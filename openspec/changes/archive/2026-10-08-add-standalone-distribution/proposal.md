## Why

Se pidió compilar "nativo". Native AOT no es compatible con WPF (reflexión/XAML/COM), así que la opción sin cambiar código es un ejecutable autónomo precompilado: un único `Matiz.exe` que no requiere instalar .NET y arranca rápido.

## What Changes

- Nuevo perfil de publicación `win-x64-native`: self-contained, single-file, ReadyToRun compuesto (app + framework precompilados), sin trimming.
- Tarea `.\build.ps1 native` (incluida en `all`).
- Se mantiene el perfil `win-x64` (framework-dependent, ~1,4 MB).

## Capabilities

### New Capabilities

### Modified Capabilities
- `app-shell`: se añade el requisito de distribución como ejecutable autónomo.

## Impact

`src/Matiz.App/Properties/PublishProfiles/win-x64-native.pubxml`, `build.ps1`, documentación. Sin cambios de código ni de lógica.
