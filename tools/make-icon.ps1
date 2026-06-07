# Generates src/KyleReese/app.ico — a red stop-sign octagon with a white "stop" glyph.
# Run with Windows PowerShell 5.1 (has System.Drawing built in):
#   powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256
$red   = [System.Drawing.Color]::FromArgb(255, 211, 47, 47)   # stop-sign red
$white = [System.Drawing.Color]::White

function New-FramePng([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Flat-top regular octagon (stop sign), inscribed with a small margin.
    $cx = $s / 2.0; $cy = $s / 2.0
    $r  = ($s / 2.0) * 0.96
    $pts = New-Object 'System.Drawing.PointF[]' 8
    for ($i = 0; $i -lt 8; $i++) {
        $ang = [Math]::PI / 180.0 * (22.5 + 45.0 * $i)
        $pts[$i] = New-Object System.Drawing.PointF(
            [float]($cx + $r * [Math]::Cos($ang)),
            [float]($cy + $r * [Math]::Sin($ang)))
    }

    $brush = New-Object System.Drawing.SolidBrush($red)
    $g.FillPolygon($brush, $pts)

    # White rim.
    $penW = [Math]::Max(1.0, $s * 0.06)
    $pen  = New-Object System.Drawing.Pen($white, [float]$penW)
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPolygon($pen, $pts)

    # White "stop" square in the centre.
    $side = $s * 0.40
    $rect = New-Object System.Drawing.RectangleF(
        [float]($cx - $side / 2.0), [float]($cy - $side / 2.0), [float]$side, [float]$side)
    $wbrush = New-Object System.Drawing.SolidBrush($white)
    $g.FillRectangle($wbrush, $rect)

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)

    $brush.Dispose(); $pen.Dispose(); $wbrush.Dispose(); $g.Dispose(); $bmp.Dispose()
    return , $ms.ToArray()
}

$frames = foreach ($s in $sizes) { , (New-FramePng $s) }

$out = New-Object System.IO.MemoryStream
$bw  = New-Object System.IO.BinaryWriter($out)

# ICONDIR
$bw.Write([uint16]0)              # reserved
$bw.Write([uint16]1)              # type = icon
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
$target = Join-Path $PSScriptRoot '..\src\KyleReese\app.ico'
$target = [System.IO.Path]::GetFullPath($target)
[System.IO.File]::WriteAllBytes($target, $out.ToArray())
$bw.Dispose(); $out.Dispose()

Write-Output "Wrote $target ($($sizes.Count) frames: $($sizes -join ', '))"
