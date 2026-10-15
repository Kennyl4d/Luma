$ErrorActionPreference = 'Stop'
$installation = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$registration = Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Luma' -ErrorAction SilentlyContinue
if (!$registration -or [IO.Path]::GetFullPath($registration.InstallLocation).TrimEnd('\') -ne $installation) { throw 'La cartella non corrisponde all’installazione registrata.' }
if (!(Test-Path -LiteralPath (Join-Path $installation 'Luma.exe'))) { throw 'Luma.exe non trovato: rimozione annullata.' }
# Delete only files shipped by the application; retain unknown user files.
$manifest = Join-Path $installation 'installed-files.txt'
if (!(Test-Path -LiteralPath $manifest)) { throw 'Manifest dei file installati non trovato.' }
foreach ($extension in '.jpg','.jpeg','.png','.gif','.heic','.heif','.webp','.avif','.bmp','.dib','.tif','.tiff','.ico','.svg') {
    Remove-ItemProperty "HKCU:\Software\Classes\$extension\OpenWithProgids" -Name 'Luma.Image' -ErrorAction SilentlyContinue
}
Remove-Item 'HKCU:\Software\Classes\Luma.Image','HKCU:\Software\Luma','HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Luma' -Recurse -ErrorAction SilentlyContinue
Remove-ItemProperty 'HKCU:\Software\RegisteredApplications' -Name Luma -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'Luma.lnk') -ErrorAction SilentlyContinue
foreach ($relative in Get-Content -LiteralPath $manifest) {
    $target = [IO.Path]::GetFullPath((Join-Path $installation $relative))
    if (!$target.StartsWith($installation + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Percorso fuori dalla cartella di installazione.' }
    Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue
}
Write-Host 'Luma disinstallato. Eventuali cartelle vuote o file personali sono stati conservati.'
