# Builds src/KyleReese/app.ico from tools/icon-source.jpg.
# Center-crops the source to a square, then high-quality resamples it into a
# multi-resolution icon (16-256px) packed as PNG frames.
# Run with Windows PowerShell 5.1 (has System.Drawing built in):
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\make-icon.ps1
param(
    [string]$Source = (Join-Path $PSScriptRoot 'icon-source.jpg'),
    [string]$Output = (Join-Path $PSScriptRoot '..\src\KyleReese\app.ico')
)

Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256

$src = [System.Drawing.Image]::FromFile([System.IO.Path]::GetFullPath($Source))

# Largest centered square that fits in the source.
$side = [Math]::Min($src.Width, $src.Height)
$srcX = [int](($src.Width - $side) / 2)
$srcY = [int](($src.Height - $side) / 2)
$cropRect = New-Object System.Drawing.Rectangle($srcX, $srcY, $side, $side)

function New-FramePng([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $destRect = New-Object System.Drawing.Rectangle(0, 0, $s, $s)
    $g.DrawImage($src, $destRect, $cropRect, [System.Drawing.GraphicsUnit]::Pixel)

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    return , $ms.ToArray()
}

$frames = foreach ($s in $sizes) { , (New-FramePng $s) }
$src.Dispose()

$out = New-Object System.IO.MemoryStream
$bw  = New-Object System.IO.BinaryWriter($out)

# ICONDIR
$bw.Write([uint16]0)             # reserved
$bw.Write([uint16]1)             # type = icon
$bw.Write([uint16]$sizes.Count)  # image count

# ICONDIRENTRY records
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $data = $frames[$i]
    $dim = [byte]($(if ($s -ge 256) { 0 } else { $s }))
    $bw.Write([byte]$dim)          # width
    $bw.Write([byte]$dim)          # height
    $bw.Write([byte]0)             # color count
    $bw.Write([byte]0)             # reserved
    $bw.Write([uint16]1)           # planes
    $bw.Write([uint16]32)          # bit count
    $bw.Write([uint32]$data.Length)
    $bw.Write([uint32]$offset)
    $offset += $data.Length
}

# Image data (PNG frames)
foreach ($data in $frames) { $bw.Write($data) }

$bw.Flush()
$target = [System.IO.Path]::GetFullPath($Output)
[System.IO.File]::WriteAllBytes($target, $out.ToArray())
$bw.Dispose(); $out.Dispose()

Write-Output "Wrote $target ($($sizes.Count) frames: $($sizes -join ', ')) from $Source"
