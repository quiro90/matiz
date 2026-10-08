# Genera los visual assets MSIX declarados en Package.appxmanifest.
# Fuente: imagen cuadrada 256px (src/Matiz.App/Assets/matiz.png). Salida con fondo transparente.
param(
    [string]$Source = (Join-Path $PSScriptRoot "..\..\src\Matiz.App\Assets\matiz.png"),
    [string]$OutDir = (Join-Path $PSScriptRoot "Assets")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Source)) { throw "No se encontro la imagen fuente: $Source" }

Add-Type -AssemblyName System.Drawing

$srcImg = [System.Drawing.Image]::FromFile((Resolve-Path $Source).Path)

function New-Png([int]$Size, [string]$Path) {
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    try {
        $gr = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $gr.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $gr.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $gr.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $gr.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $gr.Clear([System.Drawing.Color]::Transparent)
            $gr.DrawImage($script:srcImg, 0, 0, $Size, $Size)
        }
        finally { $gr.Dispose() }
        $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bmp.Dispose() }
}

function New-PngWide([int]$W, [int]$H, [int]$Art, [string]$Path) {
    $bmp = New-Object System.Drawing.Bitmap($W, $H)
    try {
        $gr = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $gr.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $gr.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $gr.Clear([System.Drawing.Color]::Transparent)
            $ox = [int](($W - $Art) / 2)
            $oy = [int](($H - $Art) / 2)
            $gr.DrawImage($script:srcImg, $ox, $oy, $Art, $Art)
        }
        finally { $gr.Dispose() }
        $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bmp.Dispose() }
}

if ($srcImg.Width -ne $srcImg.Height) { throw "La imagen fuente debe ser cuadrada (es $($srcImg.Width)x$($srcImg.Height)): $Source" }
if ($srcImg.Width -lt 256) { throw "La imagen fuente debe ser de al menos 256px (es $($srcImg.Width)x$($srcImg.Height)): $Source" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path

New-Png 44 (Join-Path $OutDir "Square44x44Logo.png")
foreach ($ts in 16, 20, 24, 30, 32, 36, 48, 60, 64, 72, 80, 96, 256) {
    $plate = Join-Path $OutDir ("Square44x44Logo.targetsize-{0}.png" -f $ts)
    New-Png $ts $plate
    Copy-Item $plate (Join-Path $OutDir ("Square44x44Logo.targetsize-{0}_altform-unplated.png" -f $ts)) -Force
}
New-Png 150 (Join-Path $OutDir "Square150x150Logo.png")
New-PngWide 310 150 150 (Join-Path $OutDir "Wide310x150Logo.png")
New-Png 310 (Join-Path $OutDir "Square310x310Logo.png")
New-Png 50 (Join-Path $OutDir "StoreLogo.png")

Write-Host ("Assets generados en {0}" -f $OutDir)