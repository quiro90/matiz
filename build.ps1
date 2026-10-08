<#
.SYNOPSIS
    Compila, ejecuta, testea y publica Matiz con un solo comando.

.EXAMPLE
    .\build.ps1              # compila (Release) y ejecuta los tests
    .\build.ps1 run          # compila y abre la app
    .\build.ps1 publish      # genera publish\win-x64\Matiz.exe (ReadyToRun, requiere .NET 10 Desktop Runtime)
    .\build.ps1 native       # genera publish\win-x64-native\Matiz.exe (un solo .exe, sin instalar .NET)
    .\build.ps1 msix         # genera publish\msix-store\*.msixupload para Partner Center (-Cert prueba local)
    .\build.ps1 all          # limpia, compila, testea y publica
    .\build.ps1 build -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('build', 'test', 'run', 'publish', 'native', 'clean', 'all', 'msix')]
    [string]$Task = 'test',

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [switch]$Cert
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$solution = Join-Path $root 'Matiz.sln'
$app = Join-Path $root 'src\Matiz.App\Matiz.App.csproj'

function Step([string]$text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

function Invoke-Dotnet([string[]]$arguments) {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($arguments[0]) falló (código $LASTEXITCODE)" }
}

function Assert-Sdk {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'No se encontró dotnet. Instala el .NET 10 SDK: https://dotnet.microsoft.com/download'
    }
    $sdks = & dotnet --list-sdks
    if (-not ($sdks -match '^10\.')) { throw "Se necesita el .NET 10 SDK. Instalados:`n$sdks" }
}

# Matiz.exe abierto bloquea los binarios: ofrece cerrarlo antes de compilar.
function Stop-RunningApp {
    $running = Get-Process -Name Matiz -ErrorAction SilentlyContinue
    if (-not $running) { return }
    $answer = Read-Host 'Matiz está abierto y bloquea la compilación. ¿Cerrarlo? (s/N)'
    if ($answer -notmatch '^[sSyY]') { throw 'Cierra Matiz y vuelve a intentarlo.' }
    $running | ForEach-Object { $_.CloseMainWindow() | Out-Null }   # cierre limpio: guarda paletas y ajustes
    $running | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
    Get-Process -Name Matiz -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Build { Step "Compilando ($Configuration)"; Invoke-Dotnet @('build', $solution, '-c', $Configuration, '-nologo') }
function Test { Step 'Ejecutando tests'; Invoke-Dotnet @('test', $solution, '-c', $Configuration, '--no-build', '-nologo') }
function Clean {
    Step 'Limpiando'
    Get-ChildItem $root -Recurse -Directory -Include bin, obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $root 'publish') -Recurse -Force -ErrorAction SilentlyContinue
}
function Publish {
    Step 'Publicando win-x64 (ReadyToRun)'
    Invoke-Dotnet @('publish', $app, '-p:PublishProfile=win-x64', '-nologo')
    $exe = Join-Path $root 'publish\win-x64\Matiz.exe'
    Write-Host "Listo: $exe" -ForegroundColor Green
}
function Native {
    Step 'Publicando ejecutable autónomo (self-contained, single-file, ReadyToRun compuesto)'
    Invoke-Dotnet @('publish', $app, '-p:PublishProfile=win-x64-native', '-nologo')
    $exe = Join-Path $root 'publish\win-x64-native\Matiz.exe'
    Write-Host ("Listo: $exe ({0:N0} MB, no requiere .NET instalado)" -f ((Get-Item $exe).Length / 1MB)) -ForegroundColor Green
}
function Find-MsixTools {
    $kits = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory -ErrorAction SilentlyContinue |
        Where-Object Name -match '^10\.0\.'
    if ($kits) {
        $kits = $kits | Sort-Object { [version]$_.Name } -Descending
        foreach ($kit in $kits) {
            $makeappx = Join-Path $kit.FullName 'x64\makeappx.exe'
            $signtool = Join-Path $kit.FullName 'x64\signtool.exe'
            if ((Test-Path $makeappx) -and (Test-Path $signtool)) { return @{ MakeAppx = $makeappx; Signtool = $signtool } }
        }
    }
    throw 'No se encontró makeappx.exe/signtool.exe x64 del Windows SDK. Instala un Windows 10/11 SDK (Visual Studio Installer → componente "Windows SDK", o winget Microsoft.WindowsSDK) y reintenta.'
}

function Msix {
    if (-not (Test-Path (Join-Path $root 'packaging\Matiz.Package\Package.appxmanifest'))) { throw 'Falta packaging\Matiz.Package\Package.appxmanifest.' }
    $tools = Find-MsixTools

    [xml]$manifest = Get-Content (Join-Path $root 'packaging\Matiz.Package\Package.appxmanifest')
    $identity = $manifest.Package.Identity
    if ($identity.Publisher -match 'TODO' -or $identity.Name -match 'TODO') {
        $err = "El manifiesto tiene identidad provisoria ($($identity.Name) / $($identity.Publisher)). "
        $err += 'Copia Package identity name y Publisher de https://partner.microsoft.com/dashboard (Aplicaciones de Windows → tu app → Product identity). '
        $err += 'Después pégalo en packaging\Matiz.Package\Package.appxmanifest.'
        throw $err
    }

    # La versión del paquete deriva de Directory.Build.props (fuente única de versión).
    $props = [xml](Get-Content (Join-Path $root 'Directory.Build.props'))
    $projVersion = ($props.GetElementsByTagName('Version') | Select-Object -First 1).InnerText.Trim()
    $parts = $projVersion -split '\.'
    while ($parts.Count -lt 4) { $parts += '0' }
    foreach ($part in $parts) {
        if ([int]$part -lt 0 -or [int]$part -gt 65535) { throw "Componente fuera de rango en la versión de Directory.Build.props: $projVersion (cada componente va de 0 a 65535)" }
    }
    $pkgVersion = $parts -join '.'
    if ($identity.Version -ne $pkgVersion) {
        throw "La versión del manifiesto ($($identity.Version)) no coincide con la del proyecto ($projVersion → $pkgVersion). Actualiza las dos juntas, en el mismo commit: Directory.Build.props <Version> y packaging\Matiz.Package\Package.appxmanifest <Identity Version>."
    }

    # Guarda de activos: todo lo declarado por el manifiesto debe existir.
    $visual = $manifest.Package.Applications.Application.VisualElements
    $tile = $visual.DefaultTile
    $assetRefs = @(
        $manifest.Package.Properties.Logo,
        $visual.Square150x150Logo,
        $visual.Square44x31Logo,
        $tile.Wide310x150Logo,
        $tile.Square310x310Logo
    ) | Where-Object { $_ }
    foreach ($ts in 16, 20, 24, 30, 32, 36, 48, 60, 64, 72, 80, 96, 256) {
        $assetRefs += ("Assets\Square44x44Logo.targetsize-{0}.png" -f $ts)
        $assetRefs += ("Assets\Square44x44Logo.targetsize-{0}_altform-unplated.png" -f $ts)
    }
    foreach ($ref in $assetRefs) {
        if (-not (Test-Path (Join-Path $root "packaging\Matiz.Package\$ref"))) {
            throw "Falta el activo de manifiesto '$ref'. Genera los PNG con packaging\Matiz.Package\Generate-Assets.ps1."
        }
    }

    Step "Publicando app self-contained ($Configuration, win-x64)"
    Invoke-Dotnet @('publish', $app, '-c', $Configuration, '-r', 'win-x64', '--self-contained', 'true', '-nologo')
    $tfmDir = Get-ChildItem (Join-Path $root "src\Matiz.App\bin\$Configuration") -Directory | Where-Object Name -like 'net*' | Select-Object -First 1
    if (-not $tfmDir) { throw 'No se encontró la salida de publish (bin\...\net*\win-x64\publish).' }
    $pub = Join-Path $tfmDir.FullName 'win-x64\publish'
    if (-not (Test-Path (Join-Path $pub 'Matiz.exe'))) { throw "El publish no generó Matiz.exe en $pub" }

    # Layout del paquete: contenido de la app + AppxManifest.xml + Assets.
    $pkgOut = Join-Path $root 'publish\msix-store'
    $layout = Join-Path $pkgOut 'layout'
    Remove-Item $pkgOut -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $layout | Out-Null
    Copy-Item (Join-Path $pub '*') $layout -Recurse -Force
    $manifestText = Get-Content (Join-Path $root 'packaging\Matiz.Package\Package.appxmanifest') -Raw
    [System.IO.File]::WriteAllText((Join-Path $layout 'AppxManifest.xml'), $manifestText, (New-Object System.Text.UTF8Encoding($true)))
    Copy-Item (Join-Path $root 'packaging\Matiz.Package\Assets') (Join-Path $layout 'Assets') -Recurse -Force

    Step 'Empaquetando MSIX (makeappx)'
    $baseName = '{0}_{1}_x64' -f $identity.Name, $pkgVersion
    $msix = Join-Path $pkgOut "$baseName.msix"
    & $tools.MakeAppx pack /d $layout /p $msix /o
    if ($LASTEXITCODE -ne 0) { throw "makeappx falló (código $LASTEXITCODE): revisa el manifiesto y los activos del layout" }

    if ($Cert) {
        Step 'Certificado de prueba + firma (sideload local)'
        $cert = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue | Where-Object Subject -eq $identity.Publisher | Sort-Object NotAfter -Descending | Select-Object -First 1
        if (-not $cert) {
            $thumb = & powershell -NoProfile -Command "New-SelfSignedCertificate -Subject '$($identity.Publisher)' -CertStoreLocation 'Cert:\CurrentUser\My' -Type CodeSigningCert -KeyUsage DigitalSignature | Select-Object -ExpandProperty Thumbprint"
            if (-not $thumb) { throw "New-SelfSignedCertificate falló; revisa el Publisher del manifiesto" }
            $cert = Get-Item ("Cert:\CurrentUser\My\" + $thumb)
        }
        $pfx = Join-Path $pkgOut 'Matiz-test.pfx'
        $certPwd = ConvertTo-SecureString -String 'Matiz-test' -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $certPwd | Out-Null
        & $tools.Signtool sign /fd SHA256 /a /f $pfx /p 'Matiz-test' $msix
        if ($LASTEXITCODE -ne 0) { throw "signtool falló (código $LASTEXITCODE)" }
        # Confiar el cert exige elevación (Root y Trusted People), pero solo si aún no se confía.
        $trusted = Get-ChildItem Cert:\LocalMachine\TrustedPeople, Cert:\LocalMachine\Root -ErrorAction SilentlyContinue | Where-Object Thumbprint -eq $cert.Thumbprint
        if (($trusted | Measure-Object).Count -ge 2) {
            Write-Host 'El cert de prueba ya es de confianza; omito la elevación.'
        }
        else {
        $importScript = "Import-PfxCertificate -FilePath '$pfx' -CertStoreLocation Cert:\LocalMachine\Root -Password (ConvertTo-SecureString 'Matiz-test' -AsPlainText -Force); Import-PfxCertificate -FilePath '$pfx' -CertStoreLocation Cert:\LocalMachine\TrustedPeople -Password (ConvertTo-SecureString 'Matiz-test' -AsPlainText -Force)"
        try {
            $elevated = Start-Process powershell -ArgumentList '-NoProfile', '-Command', $importScript -Verb RunAs -Wait -PassThru -ErrorAction Stop
            if ($elevated.ExitCode -ne 0) { Write-Host ('No se pudo importar el cert con esa elevación (código ' + $elevated.ExitCode + '). Confiá en Matiz-test.pfx manualmente para poder instalar el MSIX.') -ForegroundColor Yellow }
        }
        catch {
            Write-Host 'Se canceló la elevación: para instalar el paquete, importá Matiz-test.pfx manualmente (Root y TrustedPeople).' -ForegroundColor Yellow
        }
        }
    }

    Step 'Generando .msixupload'
    $zip = Join-Path $pkgOut 'upload.tmp.zip'
    Compress-Archive -Path $msix -DestinationPath $zip -Force
    $msixupload = Join-Path $pkgOut "$baseName.msixupload"
    Move-Item $zip $msixupload -Force

    Remove-Item $layout -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Listo: $msixupload" -ForegroundColor Green
}

function Run {
    $exe = Join-Path $root "src\Matiz.App\bin\$Configuration\net10.0-windows\Matiz.exe"
    Step "Abriendo $exe"
    Start-Process $exe
}

Push-Location $root
try {
    Assert-Sdk
    if ($Cert -and $Task -ne 'msix') { throw '-Cert solo se usa junto con la tarea msix.' }
    switch ($Task) {
        'build'   { Stop-RunningApp; Build }
        'test'    { Stop-RunningApp; Build; Test }
        'run'     { Stop-RunningApp; Build; Run }
        'publish' { Stop-RunningApp; Publish }
        'native'  { Stop-RunningApp; Native }
        'clean'   { Stop-RunningApp; Clean }
        'all'     { Stop-RunningApp; Clean; Build; Test; Publish; Native }
        'msix'    { Stop-RunningApp; Msix }
    }
    Write-Host "`nOK ($Task)" -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
