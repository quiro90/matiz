## Purpose

Define el comportamiento de empaquetado y distribución de Matiz: generación de un paquete MSIX self-contained para Microsoft Store y de las distribuciones portables directas, con identidad y versión de paquete consistentes.

## ADDED Requirements

### Requirement: Paquete MSIX para Microsoft Store
El sistema SHALL poder generarse como paquete MSIX full trust para Windows x64 con payload self-contained (incluye el runtime .NET 10, no requiere .NET instalado en el equipo destino) y sin firma propia, listo para subir a Microsoft Partner Center.

#### Scenario: Generación del paquete para Store
- **WHEN** se ejecuta `.\build.ps1 msix`
- **THEN** la carpeta `publish\msix-store` contiene un `.msixupload` sin firma de desarrollador, con el `Matiz.exe` de la app y el runtime incluido dentro del paquete

#### Scenario: Instalación en equipo sin .NET
- **WHEN** el paquete MSIX instalado en un Windows 10/11 x64 sin .NET Desktop Runtime y se usa el atajo global Alt+C sobre la pantalla
- **THEN** se captura el color del píxel y la app funciona igual que la versión portable

### Requirement: Identidad y versión del paquete
El manifiesto del paquete SHALL usar la identidad de Partner Center (Package Identity Name y Publisher CN) y una versión con formato `x.y.z.w` (cada componente entre 0 y 65535) derivada de la versión del proyecto. Al empaquetar, la versión del manifiesto SHALL coincidir con la derivada de `Directory.Build.props` (`1.0.1` → `1.0.1.0`), sin cambiar la cadena visible "v1.01".

#### Scenario: Versión consistente al empaquetar
- **WHEN** la versión del proyecto es `1.0.1` y el manifiesto declara `Version="1.0.1.0"`
- **THEN** la tarea de empaquetado se completa y la app empaquetada muestra "Matiz v1.01"

#### Scenario: Versión desincronizada detectada
- **WHEN** `Directory.Build.props` declara `1.0.1` y el manifiesto declara `Version="1.0.0.1"`
- **THEN** `.\build.ps1 msix` falla antes de compilar con un mensaje que muestra los dos valores y cómo corregirlos

### Requirement: Activos visuales del paquete
El manifiesto del paquete SHALL declarar el set de visual assets requerido por el MSIX: Square44x44Logo, Square150x150Logo, Wide310x150Logo, Square310x310Logo y StoreLogo. La tarea de empaquetado SHALL fallar si falta alguno de los activos declarados.

#### Scenario: Icono instalado correctamente
- **WHEN** todos los PNG declarados existen en `packaging/Matiz.Package/Assets`
- **WHEN** se instala el paquete en un Windows 10/11 x64
- **THEN** el icono de Matiz aparece en el menú Inicio y en la lista de aplicaciones instaladas, sin activos sin resolver en el manifiesto

#### Scenario: Detección de activo faltante
- **WHEN** se elimina un PNG declarado del manifiesto y se intenta ejecutar `.\build.ps1 msix`
- **THEN** la tarea de empaquetado falla con un error que identifica el activo faltante

### Requirement: Distribución portable directa sin cambios
El sistema SHALL continuar generando las distribuciones directas con el mismo comportamiento actual: `publish\win-x64` (framework-dependent con ReadyToRun) y `publish\win-x64-native\Matiz.exe` (single-file, self-contained).

#### Scenario: Release de GitHub sigue funcionando
- **WHEN** se ejecuta `.\build.ps1 native`
- **THEN** la carpeta `publish\win-x64-native` contiene únicamente `Matiz.exe`, ejecutable en x64 sin .NET instalado y sin identidad de paquete

#### Scenario: Empaquetado no interfiere
- **WHEN** se termina de generar el `.msixupload` y se ejecuta `.\build.ps1 publish`
- **THEN** se genera `publish\win-x64\Matiz.exe` igual que antes de agregar el empaquetado MSIX