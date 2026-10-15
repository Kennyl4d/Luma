# Original vector artwork rasterized at each Windows icon size.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'Luma\Assets'
New-Item -ItemType Directory -Force $assetRoot | Out-Null
$images = foreach ($size in 16,24,32,48,64,128,256) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform(($size / 256.0), ($size / 256.0))
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc(12,12,64,64,180,90); $path.AddArc(180,12,64,64,270,90)
    $path.AddArc(180,180,64,64,0,90); $path.AddArc(12,180,64,64,90,90); $path.CloseFigure()
    $gradient = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(0,0), [Drawing.Point]::new(256,256), [Drawing.Color]::FromArgb(255,35,187,187), [Drawing.Color]::FromArgb(255,20,79,143))
    $graphics.FillPath($gradient, $path)
    $sun = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,255,211,109))
    $graphics.FillEllipse($sun,166,48,38,38)
    $mountain = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,241,252,255))
    $points = [Drawing.PointF[]]@([Drawing.PointF]::new(39,196),[Drawing.PointF]::new(103,85),[Drawing.PointF]::new(145,150),[Drawing.PointF]::new(171,115),[Drawing.PointF]::new(221,196))
    $graphics.FillPolygon($mountain,$points)
    $shade = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,163,225,237))
    $graphics.FillPolygon($shade,[Drawing.PointF[]]@([Drawing.PointF]::new(103,85),[Drawing.PointF]::new(115,166),[Drawing.PointF]::new(145,150)))
    $stream = [IO.MemoryStream]::new()
    $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    if ($size -eq 256) { $bitmap.Save((Join-Path $assetRoot 'Luma.png'),[Drawing.Imaging.ImageFormat]::Png) }
    [PSCustomObject]@{ Size=$size; Bytes=$stream.ToArray() }
    $stream.Dispose(); $shade.Dispose(); $mountain.Dispose(); $sun.Dispose(); $gradient.Dispose(); $path.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
$file = [IO.File]::Create((Join-Path $assetRoot 'Luma.ico'))
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
    $offset = 6 + 16 * $images.Count
    foreach ($item in $images) {
        $dimension = if ($item.Size -eq 256) { 0 } else { $item.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
        $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$item.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $item.Bytes.Length
    }
    foreach ($item in $images) { $writer.Write([byte[]]$item.Bytes) }
} finally { $writer.Dispose(); $file.Dispose() }
