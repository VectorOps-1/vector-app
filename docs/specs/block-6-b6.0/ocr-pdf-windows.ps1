param(
    [Parameter(Mandatory = $true)]
    [string]$PdfPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$PopplerPath = "C:\Users\odend\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\poppler\Library\bin\pdftoppm.exe",

    [int]$Dpi = 180
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PdfPath)) {
    throw "PDF not found: $PdfPath"
}

if (-not (Test-Path -LiteralPath $PopplerPath)) {
    throw "pdftoppm not found: $PopplerPath"
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$tempDirectory = Join-Path $env:TEMP ("acuityops-b60-ocr-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
$null = [Windows.Media.Ocr.OcrEngine, Windows.Media.Ocr, ContentType = WindowsRuntime]

function Wait-WinRtOperation {
    param(
        [Parameter(Mandatory = $true)]$Operation,
        [Parameter(Mandatory = $true)][Type]$ResultType
    )

    $method = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object {
            $_.Name -eq "AsTask" -and
            $_.IsGenericMethod -and
            $_.GetParameters().Count -eq 1
        } |
        Select-Object -First 1

    $task = $method.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}

try {
    $imagePrefix = Join-Path $tempDirectory "page"
    & $PopplerPath -png -r $Dpi $PdfPath $imagePrefix
    if ($LASTEXITCODE -ne 0) {
        throw "pdftoppm failed with exit code $LASTEXITCODE"
    }

    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
    if ($null -eq $engine) {
        throw "Windows OCR engine is unavailable."
    }

    $output = [System.Text.StringBuilder]::new()
    $pages = Get-ChildItem -LiteralPath $tempDirectory -Filter "page-*.png" | Sort-Object Name
    foreach ($page in $pages) {
        $pageNumber = [int]([regex]::Match($page.BaseName, "(\d+)$").Groups[1].Value)
        $file = Wait-WinRtOperation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($page.FullName)) ([Windows.Storage.StorageFile])
        $stream = Wait-WinRtOperation ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
        $decoder = Wait-WinRtOperation ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
        $bitmap = Wait-WinRtOperation ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
        $result = Wait-WinRtOperation ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])

        [void]$output.AppendLine("===== PDF PAGE $pageNumber =====")
        [void]$output.AppendLine($result.Text)
        [void]$output.AppendLine()

        $bitmap.Dispose()
        $stream.Dispose()
    }

    [IO.File]::WriteAllText($OutputPath, $output.ToString(), [Text.UTF8Encoding]::new($false))
}
finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}

Get-FileHash -Algorithm SHA256 -LiteralPath $OutputPath |
    Select-Object Path, Hash
