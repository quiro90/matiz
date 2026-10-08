<#
.SYNOPSIS
    Compila, ejecuta, testea y publica Matiz con un solo comando.

.EXAMPLE
    .\build.ps1              # compila (Release) y ejecuta los tests
    .\build.ps1 run          # compila y abre la app
    .\build.ps1 publish      # genera publish\win-x64\Matiz.exe (ReadyToRun, requiere .NET 10 Desktop Runtime)
    .\build.ps1 native       # genera publish\win-x64-native\Matiz.exe (un solo .exe, sin instalar .NET)
    .\build.ps1 all          # limpia, compila, testea y publica
    .\build.ps1 build -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('build', 'test', 'run', 'publish', 'native', 'clean', 'all')]
    [string]$Task = 'test',

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
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
function Run {
    $exe = Join-Path $root "src\Matiz.App\bin\$Configuration\net10.0-windows\Matiz.exe"
    Step "Abriendo $exe"
    Start-Process $exe
}

Push-Location $root
try {
    Assert-Sdk
    switch ($Task) {
        'build'   { Stop-RunningApp; Build }
        'test'    { Stop-RunningApp; Build; Test }
        'run'     { Stop-RunningApp; Build; Run }
        'publish' { Stop-RunningApp; Publish }
        'native'  { Stop-RunningApp; Native }
        'clean'   { Stop-RunningApp; Clean }
        'all'     { Stop-RunningApp; Clean; Build; Test; Publish; Native }
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
