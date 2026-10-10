# Native Windows vector drawing, matching Assets/MonitorDesk.svg. No external dependencies.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'src/MonitorDesk/Assets/MonitorDesk.ico'
function Render-Icon([int]$size) {
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new($size / 64.0, $size / 64.0))
    $brushes = [Windows.Media.BrushConverter]::new()
    $dark = $brushes.ConvertFromString('#171e2d')
    $page = $brushes.ConvertFromString('#0d111b')
    $accent = $brushes.ConvertFromString('#8be5ce')
    $warm = $brushes.ConvertFromString('#ffd08a')
    $pen = [Windows.Media.Pen]::new($accent, 4)
    $pen.StartLineCap = $pen.EndLineCap = [Windows.Media.PenLineCap]::Round
    $drawing.DrawRoundedRectangle($dark, $null, [Windows.Rect]::new(2,2,60,60),14,14)
    $drawing.DrawRoundedRectangle($page, $pen, [Windows.Rect]::new(10,15,44,31),5,5)
    $drawing.DrawEllipse($warm,$null,[Windows.Point]::new(40,28),6,6)
    $drawing.DrawLine($pen,[Windows.Point]::new(22,36),[Windows.Point]::new(32,36))
    $drawing.DrawLine($pen,[Windows.Point]::new(32,46),[Windows.Point]::new(32,53))
    $drawing.DrawLine($pen,[Windows.Point]::new(22,54),[Windows.Point]::new(42,54))
    $drawing.Pop(); $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.MemoryStream]::new()
    $encoder.Save($stream)
    $bytes = $stream.ToArray(); $stream.Dispose()
    return ,$bytes
}
$sizes = @(16,20,24,32,40,48,64,128,256)
$frames = @($sizes | ForEach-Object { ,(Render-Icon $_) })
$file = [IO.File]::Create($output)
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $sizeByte = if ($sizes[$i] -eq 256) {0} else {$sizes[$i]}
        $writer.Write([byte]$sizeByte); $writer.Write([byte]$sizeByte)
        $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
} finally { $writer.Dispose(); $file.Dispose() }
$preview = Join-Path $root 'artifacts/icon-preview.png'
[IO.Directory]::CreateDirectory((Split-Path $preview -Parent)) | Out-Null
[IO.File]::WriteAllBytes($preview,(Render-Icon 512))
Write-Host "Created $output (nine sizes)"
