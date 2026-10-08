## ADDED Requirements

### Requirement: Distribución como ejecutable autónomo
El sistema SHALL poder publicarse como un único ejecutable `Matiz.exe` para Windows x64 que no requiera tener .NET instalado, con el código de la aplicación y del framework precompilado a código máquina, sin cambios en el código fuente. También SHALL mantenerse una publicación ligera dependiente del .NET 10 Desktop Runtime.

#### Scenario: Equipo sin .NET
- **WHEN** se copia `publish\win-x64-native\Matiz.exe` a un Windows x64 sin .NET instalado y se ejecuta
- **THEN** la aplicación se abre y funciona igual que la versión dependiente del runtime

#### Scenario: Un solo archivo
- **WHEN** se ejecuta `.\build.ps1 native`
- **THEN** la carpeta `publish\win-x64-native` contiene únicamente `Matiz.exe`
