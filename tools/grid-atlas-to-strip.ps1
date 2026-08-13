param(
    [Parameter(Mandatory = $true)] [string] $InputPath,
    [Parameter(Mandatory = $true)] [string] $OutputPath,
    [Parameter(Mandatory = $true)] [int] $Columns,
    [Parameter(Mandatory = $true)] [int] $Rows,
    [int] $HorizontalGuard = 0
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $InputPath))
try {
    $cellWidth = [int][Math]::Ceiling($source.Width / $Columns)
    $cellHeight = [int][Math]::Ceiling($source.Height / $Rows)
    $slotSize = [Math]::Max($cellWidth, $cellHeight)
    $strip = New-Object System.Drawing.Bitmap ($slotSize * $Columns * $Rows), $slotSize, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($strip)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor

            for ($row = 0; $row -lt $Rows; $row++) {
                for ($column = 0; $column -lt $Columns; $column++) {
                    $index = $row * $Columns + $column
                    $left = [int][Math]::Round($column * $source.Width / $Columns)
                    $top = [int][Math]::Round($row * $source.Height / $Rows)
                    $right = [int][Math]::Round(($column + 1) * $source.Width / $Columns)
                    $bottom = [int][Math]::Round(($row + 1) * $source.Height / $Rows)
                    $left += $HorizontalGuard
                    $right -= $HorizontalGuard
                    $width = $right - $left
                    $height = $bottom - $top
                    $destinationX = $index * $slotSize + [int](($slotSize - $width) / 2)
                    $destinationY = $slotSize - $height
                    $destination = New-Object System.Drawing.Rectangle $destinationX, $destinationY, $width, $height
                    $sourceRect = New-Object System.Drawing.Rectangle $left, $top, $width, $height
                    $graphics.DrawImage($source, $destination, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
                }
            }
        }
        finally {
            $graphics.Dispose()
        }

        $parent = Split-Path -Parent $OutputPath
        if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
        $strip.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $strip.Dispose()
    }
}
finally {
    $source.Dispose()
}
