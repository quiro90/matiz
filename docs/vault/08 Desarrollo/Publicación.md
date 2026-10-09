---
tags: [desarrollo]
---
# Publicación
Dos variantes, sin cambios de código:

| Variante | Comando | Salida | Tamaño | Requiere | Arranque* |
|---|---|---|---|---|---|
| **Ligera** | `.\build.ps1 publish` | `publish\win-x64\Matiz.exe` (+ DLLs) | ~1,4 MB | .NET 10 Desktop Runtime | ~0,75 s |
| **Autónoma ("nativa")** | `.\build.ps1 native` | `publish\win-x64-native\Matiz.exe` (un solo archivo) | ~143 MB | nada | ~0,7 s |

\* Medido con la caché de Windows caliente. En frío la primera apertura es más lenta (~2,5 s la ligera, ~1,6 s la autónoma).

## Autónoma: qué hace el perfil `win-x64-native`
- `SelfContained` — incluye el runtime de .NET.
- `PublishSingleFile` + `IncludeNativeLibrariesForSelfExtract` — un único `.exe`.
- `PublishReadyToRun` + `PublishReadyToRunComposite` — app **y** framework precompilados juntos a código máquina (casi sin JIT al arrancar).
- Sin trimming y sin símbolos.
- Opción: `-p:EnableCompressionInSingleFile=true` → ~65 MB, pero ~0,9 s de arranque (descomprime al abrir).

## ¿Por qué no Native AOT?
WPF no es compatible con Native AOT (usa reflexión desde XAML, COM e interop dinámica). ReadyToRun compuesto + self-contained es lo más cercano sin reescribir la UI → [[ADR-010 Ejecutable autónomo]]. Si en el futuro se migra a Avalonia, Native AOT sí es posible ([[Multiplataforma]]).

## Versión e icono
Versión actual: **v1.0.9** (`Directory.Build.props`: `Version` 1.0.9, `InformationalVersion` 1.0.9; `packaging/Matiz.Package/Package.appxmanifest` también la lleva). Icono: `src/Matiz.App/Assets/matiz.ico` (círculo degradado de la barra superior, 16–256 px).

Instalador / MSIX / auto-actualización: fuera del alcance actual ([[Alcance y backlog]]).
