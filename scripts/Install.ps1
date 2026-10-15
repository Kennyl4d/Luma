param([string]$Destination = (Join-Path $env:LOCALAPPDATA 'Programs\Luma'))
$ErrorActionPreference = 'Stop'
$sourceExe = Join-Path $PSScriptRoot 'Luma.exe'
if (!(Test-Path -LiteralPath $sourceExe)) { throw 'Esegui Install.ps1 dalla cartella del pacchetto pubblicato.' }
$Destination = [IO.Path]::GetFullPath($Destination)
if ($Destination.TrimEnd('\') -eq $PSScriptRoot.TrimEnd('\') -or $Destination.StartsWith($PSScriptRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Scegli una cartella esterna a quella del pacchetto.' }
$manifest = Join-Path $PSScriptRoot 'installed-files.txt'
if (!(Test-Path -LiteralPath $manifest)) { throw 'Manifest del pacchetto non trovato.' }
$packageFiles = foreach ($relative in Get-Content -LiteralPath $manifest) {
    $source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $relative))
    $target = [IO.Path]::GetFullPath((Join-Path $Destination $relative))
    if (!$source.StartsWith($PSScriptRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or !$target.StartsWith($Destination.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Percorso non valido nel manifest.' }
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw "File del pacchetto mancante: $relative" }
    [PSCustomObject]@{ Source = $source; Target = $target }
}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
foreach ($file in $packageFiles) {
    New-Item -ItemType Directory -Path (Split-Path $file.Target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.Source -Destination $file.Target -Force
}
$exe = Join-Path $Destination 'Luma.exe'
$extensions = '.jpg','.jpeg','.png','.gif','.heic','.heif','.webp','.avif','.bmp','.dib','.tif','.tiff','.ico','.svg'
$classes = 'HKCU:\Software\Classes'
$progId = 'Luma.Image'
New-Item "$classes\$progId\shell\open\command" -Force | Out-Null
Set-Item "$classes\$progId" -Value 'Immagine Luma'
Set-Item "$classes\$progId\shell\open\command" -Value ('"' + $exe + '" "%1"')
New-Item "$classes\$progId\DefaultIcon" -Force | Out-Null
Set-Item "$classes\$progId\DefaultIcon" -Value ('"' + $exe + '",0')
$capabilities = 'HKCU:\Software\Luma\Capabilities'
New-Item "$capabilities\FileAssociations" -Force | Out-Null
New-ItemProperty $capabilities -Name ApplicationName -Value 'Luma' -PropertyType String -Force | Out-Null
New-ItemProperty $capabilities -Name ApplicationDescription -Value 'Visualizzatore immagini essenziale' -PropertyType String -Force | Out-Null
foreach ($extension in $extensions) {
    New-Item "$classes\$extension\OpenWithProgids" -Force | Out-Null
    New-ItemProperty "$classes\$extension\OpenWithProgids" -Name $progId -Value '' -PropertyType String -Force | Out-Null
    New-ItemProperty "$capabilities\FileAssociations" -Name $extension -Value $progId -PropertyType String -Force | Out-Null
}
New-Item 'HKCU:\Software\RegisteredApplications' -Force | Out-Null
New-ItemProperty 'HKCU:\Software\RegisteredApplications' -Name Luma -Value 'Software\Luma\Capabilities' -PropertyType String -Force | Out-Null
$uninstall = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Luma'
New-Item $uninstall -Force | Out-Null
New-ItemProperty $uninstall -Name DisplayName -Value Luma -Force | Out-Null
New-ItemProperty $uninstall -Name DisplayVersion -Value '0.3.4' -Force | Out-Null
New-ItemProperty $uninstall -Name InstallLocation -Value $Destination -Force | Out-Null
New-ItemProperty $uninstall -Name UninstallString -Value ('powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $Destination 'Uninstall.ps1') + '"') -Force | Out-Null
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'Luma.lnk'))
$shortcut.TargetPath = $exe; $shortcut.WorkingDirectory = $Destination; $shortcut.IconLocation = "$exe,0"; $shortcut.Save()
Write-Host "Installato in $Destination. In App predefinite, cerca Luma e scegli i formati."
Start-Process 'ms-settings:defaultapps'
