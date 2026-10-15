#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $projectRoot 'Luma\tessdata'
New-Item -ItemType Directory -Force $destination | Out-Null
$commit = '87416418657359cb625c412a48b6e1d6d41c29bd'
$languages = 'ces','deu','eng','fra','ita','jpn','kor','pol','por','rus','spa','tur','chi_sim','chi_tra'
$manifest = foreach ($language in $languages) {
    $file = Join-Path $destination "$language.traineddata"
    if (!(Test-Path -LiteralPath $file)) { Invoke-WebRequest "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/$commit/$language.traineddata" -OutFile $file }
    Write-Host "Modello OCR: $language"
    [PSCustomObject]@{ File="$language.traineddata"; SourceCommit=$commit; SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash }
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'sources.json') -Encoding utf8
Invoke-WebRequest "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/$commit/LICENSE" -OutFile (Join-Path $projectRoot 'licenses\Tesseract-models-LICENSE.txt')
