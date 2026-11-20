param(
    [string]$SourceDir,
    [string]$AssetsDir
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if (-not $SourceDir -or -not (Test-Path -LiteralPath $SourceDir)) {
    Write-Host "LOGO: carpeta no encontrada ($SourceDir). Se conservan los assets actuales."
    exit 0
}

$image = Get-ChildItem -LiteralPath $SourceDir -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -match '^\.(png|jpe?g|bmp|gif)$' -and $_.Length -gt 0 } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $image) {
    Write-Host "LOGO: no hay imagenes en $SourceDir. Se conservan los assets actuales."
    exit 0
}

$pngPath = Join-Path $AssetsDir "app-logo.png"
$icoPath = Join-Path $AssetsDir "app-icon.ico"
New-Item -ItemType Directory -Force -Path $AssetsDir | Out-Null

if ((Test-Path -LiteralPath $pngPath) -and (Test-Path -LiteralPath $icoPath) -and
    (Get-Item -LiteralPath $pngPath).LastWriteTime -ge $image.LastWriteTime -and
    (Get-Item -LiteralPath $icoPath).LastWriteTime -ge $image.LastWriteTime) {
    Write-Host "LOGO: ya esta actualizado desde $($image.Name)."
    exit 0
}

$bmp = New-Object System.Drawing.Bitmap($image.FullName)
try {
    # Bounding box de los pixeles visibles, para recortar bordes transparentes.
    $minX = $bmp.Width; $minY = $bmp.Height; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            if ($bmp.GetPixel($x, $y).A -gt 8) {
                if ($x -lt $minX) { $minX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt 0) { throw "La imagen no tiene pixeles visibles: $($image.FullName)" }

    $cx = [int](($minX + $maxX) / 2)
    $cy = [int](($minY + $maxY) / 2)
    $side = [Math]::Max($maxX - $minX + 1, $maxY - $minY + 1)
    $side = [Math]::Min($side, [Math]::Min($bmp.Width, $bmp.Height))

    $left = [Math]::Max(0, [Math]::Min($bmp.Width - $side, $cx - [int]($side / 2)))
    $top = [Math]::Max(0, [Math]::Min($bmp.Height - $side, $cy - [int]($side / 2)))
    $cropRect = New-Object System.Drawing.Rectangle($left, $top, $side, $side)

    function New-SquareBitmap([int]$size) {
        $target = New-Object System.Drawing.Bitmap($size, $size)
        $g = [System.Drawing.Graphics]::FromImage($target)
        try {
            $g.Clear([System.Drawing.Color]::Transparent)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle(0, 0, $size, $size)), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
        } finally {
            $g.Dispose()
        }
        return $target
    }

    $square256 = New-SquareBitmap 256
    try {
        $square256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $square256.Dispose()
    }

    # ICO con entradas PNG (Windows Vista+).
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $images = @()
    foreach ($s in $sizes) {
        $b = New-SquareBitmap $s
        $ms = New-Object System.IO.MemoryStream
        try {
            $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $images += ,($ms.ToArray())
        } finally {
            $ms.Dispose()
            $b.Dispose()
        }
    }

    $fs = [System.IO.File]::Create($icoPath)
    try {
        $bw = New-Object System.IO.BinaryWriter($fs)
        $bw.Write([uint16]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]$sizes.Count)

        $offset = 6 + (16 * $sizes.Count)
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $s = $sizes[$i]
            $data = $images[$i]
            $bw.Write([byte]$(if ($s -eq 256) { 0 } else { $s }))
            $bw.Write([byte]$(if ($s -eq 256) { 0 } else { $s }))
            $bw.Write([byte]0)
            $bw.Write([byte]0)
            $bw.Write([uint16]1)
            $bw.Write([uint16]32)
            $bw.Write([uint32]$data.Length)
            $bw.Write([uint32]$offset)
            $offset += $data.Length
        }
        foreach ($data in $images) {
            $bw.Write($data)
        }
        $bw.Flush()
    } finally {
        $fs.Dispose()
    }

    Write-Host "LOGO: actualizado desde $($image.Name) -> app-logo.png / app-icon.ico"
    Write-Host "LOGO: recorte $cropRect"
} finally {
    $bmp.Dispose()
}
