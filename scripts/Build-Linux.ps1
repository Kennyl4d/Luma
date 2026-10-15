param([string]$Nfpm, [string]$Python='python', [switch]$SkipPublish)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Nfpm){$Nfpm=Join-Path $root '.tools\nfpm\2.48.0\nfpm.exe'}
$sdk=Join-Path $root '.tools\dotnet\dotnet.exe'
if(!(Test-Path $sdk)){$sdk='dotnet'}
$publish=Join-Path $root 'artifacts\linux\publish'
$packages=Join-Path $root 'artifacts\linux\packages'
New-Item -ItemType Directory -Force $packages | Out-Null
$saved=@{}
foreach($name in 'DOTNET_CLI_HOME','APPDATA','LOCALAPPDATA'){$saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try{
 $env:DOTNET_CLI_HOME=Join-Path $root '.tools\cli';$env:APPDATA=Join-Path $root '.tools\profile';$env:LOCALAPPDATA=Join-Path $root '.tools\local'
 if(!$SkipPublish){& $sdk publish (Join-Path $root 'Luma.Linux\Luma.Linux.csproj') -c Release -r linux-x64 --self-contained true -p:PublishReadyToRun=true "-p:RestoreConfigFile=$(Join-Path $root 'NuGet.Config')" -o $publish;if($LASTEXITCODE -ne 0){throw 'Linux publish failed'}}
 $config=Join-Path $root 'artifacts\linux\nfpm.json'
 & $Python (Join-Path $root 'linux\nfpm-config.py') $publish $config
 if($LASTEXITCODE -ne 0){throw 'nFPM configuration failed'}
 foreach($format in 'deb','rpm','archlinux'){& $Nfpm package --config $config --packager $format --target $packages;if($LASTEXITCODE -ne 0){throw "Package failed: $format"}}
 $checksums=Get-ChildItem $packages -File | Where-Object Name -ne 'SHA256SUMS' | ForEach-Object { (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.Name }
 $checksums | Set-Content (Join-Path $packages 'SHA256SUMS') -Encoding ascii
}finally{foreach($name in $saved.Keys){[Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')}}
