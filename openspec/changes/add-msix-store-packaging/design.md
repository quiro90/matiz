## Context

Ver `proposal.md`. Estado relevante:

- Build actual: `build.ps1` con tareas `build/test/run/publish/native/clean/all`, todo con CLI `dotnet` sobre `Matiz.sln`. `Build` y `Test` compilan **la solución** con dotnet (constraint: dotnet CLI no puede compilar `.wapproj`).
- La Store firma el paquete; Partner Center (cuenta individual, ya disponible) exige: identidad (Name + Publisher CN), versionado `x.y.z.w`, set de visual assets, `.msixupload` sin firma propia.
- VS 2026 Enterprise instalado, pero VERIFICADO durante apply: no tiene el workload de packaging UWP/MSIX (faltan los targets `Microsoft.DesktopBridge` que exige un `.wapproj`); sí está instalado Windows SDK 10.0.26100 con `makeappx.exe`/`signtool.exe` en `C:\Program Files (x86)\Windows Kits\10\bin`.
- Valores de identidad de Partner Center: entregados por el usuario durante la implementación: `Name="JuanQuiroga.Matiz"`, `Publisher="CN=C7BB1DDD-DF8A-49EF-8538-8D09EF4F231B"`, `PublisherDisplayName` `Juan Quiroga`. La guarda anti-`TODO` queda como red de seguridad ante futuros valores provisorios.

## Goals / Non-Goals

**Goals:**
- Un comando (`.\build.ps1 msix`) que produce el `.msixupload` listo para Partner Center.
- No tocar código de la app ni el flujo dotnet existente.
- Fuente única de versión (`Directory.Build.props`) con verificación de consistencia contra el manifiesto.
- Set de íconos del paquete completo y generado de forma reproducible.

**Non-Goals:**
- Migración de datos portable → paquete; cambio de nombre del mutex; MSIX firmado para instalación externa; arm64; CI automático; WACK automatizado.

## Decisions

1. **Empaquetado con `dotnet publish` + MakeAppx/Signtool del Windows SDK (decisión actualizada durante apply).** El diseño original usaba un `.wapproj`; la verificación del entorno mostró que VS 2026 no tiene el workload de packaging (sin targets `Microsoft.DesktopBridge`), mientras que el Windows SDK 10.0.26100 ya instalado provee `makeappx.exe` y `signtool.exe`. El flujo queda: `dotnet publish` self-contained → layout con `AppxManifest.xml` → `makeappx pack` → (opcional) `signtool sign` → `.msixupload` sin firma para Partner Center, que firma el paquete al subirlo. Alternativa descartada tras evaluar el entorno: instalar el workload UWP (~3 GB) solo para compilar un proyecto auxiliar.
2. **Ubicación `packaging/Matiz.Package/`.** El manifiesto, activos y scripts viven fuera de `src/` para señalar que no es código de la app. No se crea `.wapproj`, por lo que no se agrega nada a `Matiz.sln` ni a `Matiz.slnx`; todo el empaquetado queda centralizado en la tarea `msix` de `build.ps1`.
3. **Payload self-contained vía `dotnet publish`.** `dotnet publish` con `-r win-x64 -p:SelfContained=true` sobre el proyecto de app; sin single-file ni R2R dentro del paquete (el MSIX ya agrupa archivos; la extracción a TEMP del single-file es inútil aquí). Alternativa: R2R dentro del paquete - diferida, gana arranque a cambio de ~30% más tamaño; se puede activar después sin cambiar specs.
4. **Paquete sin firma + `.msixupload` + opción `-Cert`.** `makeappx pack` genera el `.msix` sin firma (Partner Center firma al recibir; acepta subir `.msix` o `.msixupload`). El artefacto canónico es el `.msixupload` (zip con el `.msix` dentro). **Opcional** para prueba local: parámetro `-Cert` que crea un cert autofirmado con subject = Publisher CN del manifiesto, lo registra en Trusted People/Root y firma con `signtool`; por defecto no se genera cert ni firma.
5. **Versión: fuente única + verificación, sin magia.** `Directory.Build.props` `<Version>1.0.1</Version>` sigue siendo la verdad. `msix` deriva `1.0.1.0` (major.minor.patch del `<Version>`, revisión 0 si el manifiesto no la especifica) y compara con el atributo `Version` + `Identity` del `Package.appxmanifest`: si difieren, falla mostrando ambos. Se descartó el stamping automático del manifiesto en build (mutaría un archivo trackeado en cada build); el bump de versión es un paso manual de release documentado (una línea en `Directory.Build.props` + una en el manifiesto).
6. **Guarda de identidad.** El manifiesto lleva la identidad real de Partner Center (`JuanQuiroga.Matiz` + `CN=C7BB1DDD-DF8A-49EF-8538-8D09EF4F231B`); `msix` valida que no queden valores provisorios (`TODO` en Publisher/Name) y aborta con instrucciones exactas si los hay. Así nadie sube un paquete sin identidad.
7. **Activos reproducibles.** Script PowerShell (`packaging/Matiz.Package/Generate-Assets.ps1`) con `System.Drawing` que lee la imagen 256x256 (medida durante apply: `src/Matiz.App/Assets/matiz.png`) y escala a `Square44x44Logo` 44x44 con variantes `targetsize-16/20/24/30/32/36/48/60/64/72/80/96/256` + `_altform-unplated` para el ícono de barra de tareas, además de 150x150, 310x150, 310x310 y StoreLogo 50x50, con fondo transparente. El escalado del 256 → 310 introduce suavizado leve: aceptable para la v1; se reemplaza con arte de mayor resolución si se hace el release gráfico. La tarea `msix` re-valida que todos los activos declarados existan (requirement del spec).
8. **Perfiles portable intactos.** `Publish`/`Native` no cambian nada internamente; solo se agrega `msix` a `all`? No: `all` queda como estaba (clean build test publish native) y `msix` es una tarea aparte, porque exige MSBuild/VS y no debe hacer fallar `all` en máquinas solo-dotnet.

## Risks / Trade-offs

- [Ruta de makeappx/signtool dependiente del SDK instalado] → `msix` localiza ambos en `C:\Program Files (x86)\Windows Kits\10\bin\<mayor versión>\x64` y falla con instrucciones si no hay Windows 10/11 SDK ("instalar Windows SDK desde Visual Studio Installer o winget Microsoft.WindowsSDK").
- [Mutex `Matiz.{usuario}` compartido entre portable y paquete] → si ambas versiones están instaladas en la misma máquina, solo una instancia puede "poseer" el atajo: la segunda se comporta como ya activa. Fuera de scope ya declarado; se documenta en el README (usar una de las dos).
- [Ícono 256px escalado a 310] → leve suavizado; mitigado más adelante con arte HD.
- [Reserva del nombre "Matiz" en Store puede estar tomada] → la visualización puede seguir siendo "Matiz"; si el identity name está ocupado, se reserva una variante (p. ej. `MatizQuiroga` o sufijo numérico) y va al manifiesto; no afecta specs.
- [Tamaño del paquete self-contained (~140-160 MB)] → dentro del límite de Store (2 GB); aceptado.

## Migration Plan

1. Agregar `packaging/Matiz.Package` (manifiesto + script de activos) — sin cambios en las soluciones.
2. Identidad real de Partner Center ya pegada en el manifiesto (reserva: `JuanQuiroga.Matiz`).
3. Generar activos y correr `.\build.ps1 msix` → `.msixupload`.
4. Subida manual a Partner Center (pasos 1-a-1 en tasks.md).
- **Rollback**: eliminar la carpeta `packaging/`, la referencia en `Matiz.slnx` y la tarea `msix`; cero impacto en la app ni en las distribuciones portables.

## Open Questions

- Ninguno bloqueante: los dos datos que faltan (Package Identity Name exacto y Publisher CN) los entrega el usuario en el paso 1 de la implementación.