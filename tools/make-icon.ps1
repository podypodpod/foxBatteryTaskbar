# Regenerates src/FoxBatteryTaskbar/app.ico (the exe / window icon).
# A green battery with a lightning bolt, rendered at each standard icon size and packed into one .ico.
# Run from the repo root:  powershell -ExecutionPolicy Bypass -File tools/make-icon.ps1
Add-Type -AssemblyName System.Drawing

$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$out = Join-Path $PSScriptRoot '..\src\FoxBatteryTaskbar\app.ico'

function New-IconPng([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Battery body (vertical) with a terminal nub on top
    $pad = [Math]::Max(1, [int]($s * 0.12))
    $nubW = [int]($s * 0.30); $nubH = [Math]::Max(1, [int]($s * 0.08))
    $bodyX = $pad + [int]($s * 0.08); $bodyW = $s - 2 * $bodyX
    $bodyY = $pad + $nubH; $bodyH = $s - $bodyY - $pad
    $stroke = [Math]::Max(1.0, $s / 14.0)

    $outline = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 30, 30, 30)), $stroke
    $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 60, 200, 90))
    $nub = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 30, 30))
    $bolt = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)

    $g.FillRectangle($nub, [int](($s - $nubW) / 2), $pad, $nubW, $nubH + 1)
    $g.FillRectangle($fill, $bodyX, $bodyY, $bodyW, $bodyH)
    $g.DrawRectangle($outline, $bodyX, $bodyY, $bodyW, $bodyH)

    # Lightning bolt
    $cx = $s / 2.0; $top = $bodyY + $bodyH * 0.15; $bot = $bodyY + $bodyH * 0.85; $mid = ($top + $bot) / 2; $w = $bodyW * 0.32
    $pts = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF ($cx + $w * 0.3), $top),
        (New-Object System.Drawing.PointF ($cx - $w), ($mid + $bodyH * 0.05)),
        (New-Object System.Drawing.PointF ($cx - $w * 0.05), ($mid + $bodyH * 0.05)),
        (New-Object System.Drawing.PointF ($cx - $w * 0.3), $bot),
        (New-Object System.Drawing.PointF ($cx + $w), ($mid - $bodyH * 0.05)),
        (New-Object System.Drawing.PointF ($cx + $w * 0.05), ($mid - $bodyH * 0.05)))
    $g.FillPolygon($bolt, $pts)

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,$ms.ToArray()
}

# ICO container: header, one directory entry per image, then the PNG payloads
$images = $sizes | ForEach-Object { ,(New-IconPng $_) }
$fs = [System.IO.File]::Create($out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $images[$i].Length
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))  # width (0 = 256)
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))  # height
    $bw.Write([byte]0); $bw.Write([byte]0)                   # palette, reserved
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)              # planes, bpp
    $bw.Write([UInt32]$len); $bw.Write([UInt32]$offset)
    $offset += $len
}
foreach ($img in $images) { $bw.Write($img) }
$bw.Dispose()
Write-Host "Wrote $out ($((Get-Item $out).Length) bytes, sizes: $($sizes -join ', '))"
