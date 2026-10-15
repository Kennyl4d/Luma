param([switch]$Portable)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$localSdk = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$sdk = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { 'dotnet' }
$version = ([xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Luma\Luma.csproj') -Raw)).Project.PropertyGroup.Version
$savedEnvironment = @{}
foreach ($name in 'DOTNET_CLI_HOME','DOTNET_CLI_TELEMETRY_OPTOUT','APPDATA','LOCALAPPDATA') { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:APPDATA = Join-Path $projectRoot '.tools\profile'
$env:LOCALAPPDATA = Join-Path $projectRoot '.tools\local'
New-Item -ItemType Directory -Force $env:APPDATA,$env:LOCALAPPDATA | Out-Null
& $sdk restore (Join-Path $projectRoot 'Luma\Luma.csproj') --configfile (Join-Path $projectRoot 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'Ripristino dipendenze fallito.' }
if ($Portable) {
    $config = Join-Path $projectRoot 'NuGet.Config'
    & $sdk publish (Join-Path $projectRoot 'Luma\Luma.csproj') -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -p:PublishSingleFile=false "-p:RestoreConfigFile=$config" -o (Join-Path $projectRoot 'artifacts\Luma')
    if ($LASTEXITCODE -ne 0) { throw 'Pubblicazione fallita.' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install.ps1'),(Join-Path $PSScriptRoot 'Uninstall.ps1') -Destination (Join-Path $projectRoot 'artifacts\Luma')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md'),(Join-Path $projectRoot 'THIRD-PARTY-NOTICES.md'),(Join-Path $projectRoot 'BUILD-STATUS.md') -Destination (Join-Path $projectRoot 'artifacts\Luma')
    if (Test-Path -LiteralPath (Join-Path $projectRoot 'licenses')) { Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses') -Destination (Join-Path $projectRoot 'artifacts\Luma') -Recurse -Force }
    $package = Join-Path $projectRoot 'artifacts\Luma'
    $files = Get-ChildItem -LiteralPath $package -File -Recurse | Where-Object Name -ne 'installed-files.txt' | ForEach-Object { $_.FullName.Substring($package.Length + 1) }
    @($files; 'installed-files.txt') | Set-Content -LiteralPath (Join-Path $package 'installed-files.txt') -Encoding UTF8
    Compress-Archive -Path (Join-Path $projectRoot 'artifacts\Luma') -DestinationPath (Join-Path $projectRoot "artifacts\Luma-$version-win-x64.zip") -Force
} else {
    & $sdk build (Join-Path $projectRoot 'Luma\Luma.csproj') -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Compilazione fallita.' }
}
} finally {
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
}
