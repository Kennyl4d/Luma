#requires -Version 7.0
param([string]$Compiler, [switch]$RebuildApp, [string]$Revision = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if ($RebuildApp) { & (Join-Path $PSScriptRoot 'Build.ps1') -Portable }
$version = ([xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Luma\Luma.csproj') -Raw)).Project.PropertyGroup.Version
if (!$Compiler) {
    $Compiler = Join-Path $projectRoot '.tools\wix5\wix.exe'
    if (!(Test-Path -LiteralPath $Compiler)) { $Compiler = (Get-Command wix.exe -ErrorAction Stop).Source }
}
$payload = Join-Path $projectRoot 'artifacts\Luma'
$exe = Join-Path $payload 'Luma.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Pubblica prima Luma con scripts\Build.ps1 -Portable.' }
$publishedVersion = (Get-Item -LiteralPath $exe).VersionInfo.ProductVersion
if (($publishedVersion -split '\+', 2)[0] -ne $version) { throw "La versione pubblicata ($publishedVersion) non corrisponde ai sorgenti ($version). Usa -RebuildApp." }
$generated = Join-Path $projectRoot "artifacts\installer-build\$version"
New-Item -ItemType Directory -Force $generated | Out-Null
function Escape-Xml([string]$value) { return [Security.SecurityElement]::Escape($value) }
function Identifier([string]$value) {
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant()))
    return 'P' + [Convert]::ToHexString($hash).Substring(0,24)
}
function ComponentGuid([string]$value) {
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes('Luma.MSI.Payload.' + $value.ToLowerInvariant()))
    return [Guid]::new([byte[]]$hash[0..15]).ToString()
}
$ignored = @('Install.ps1','Uninstall.ps1','installed-files.txt','README.md','BUILD-STATUS.md','Luma.pdb')
$guideCatalog = Get-Content (Join-Path $projectRoot 'Luma\Resources\Strings.json') -Raw | ConvertFrom-Json
$guide = [Collections.Generic.List[string]]::new()
foreach ($language in $guideCatalog.languages.psobject.Properties) {
    $strings = @{}; for ($i=0; $i -lt $guideCatalog.keys.Count; $i++) { $strings[$guideCatalog.keys[$i]] = $language.Value[$i] }
    $guide.Add("Luma $version — $($language.Name)"); $guide.Add("$($strings.Author): KENNY")
    $guide.Add($strings.Description); $guide.Add('JPEG · PNG · GIF · HEIC/HEIF · WebP · AVIF · BMP/DIB · TIFF · ICO · SVG')
    foreach ($key in 'OpenTip','PreviousTip','NextTip','ZoomOutTip','ZoomInTip','FitTip','ActualTip','RotateTip','FullscreenTip','Privacy','SelectionTip') { $guide.Add($strings[$key]) }
    $guide.Add('')
}
$guide | Set-Content (Join-Path $projectRoot 'installer\Guida.txt') -Encoding utf8
$files = @(Get-ChildItem -LiteralPath $payload -File -Recurse | Where-Object { $_.Name -notin $ignored })
$files += Get-Item -LiteralPath (Join-Path $projectRoot 'installer\Guida.txt')
$xml = [Text.StringBuilder]::new('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"><Fragment>')
$components = [Collections.Generic.List[string]]::new()
foreach ($file in $files | Sort-Object FullName) {
    $relative = if ($file.Name -eq 'Guida.txt') { 'Guida.txt' } else { [IO.Path]::GetRelativePath($payload,$file.FullName) }
    $directory = [IO.Path]::GetDirectoryName($relative)
    $id = Identifier $relative
    $guid = ComponentGuid $relative
    $components.Add($id)
    $directoryAttribute = if ($directory) { ' Subdirectory="' + (Escape-Xml $directory) + '"' } else { '' }
    [void]$xml.Append('<DirectoryRef Id="INSTALLFOLDER"><Component Id="' + $id + '" Guid="' + $guid + '" Bitness="always64"' + $directoryAttribute + '>')
    [void]$xml.Append('<File Id="F' + $id + '" Source="' + (Escape-Xml $file.FullName) + '" Name="' + (Escape-Xml $file.Name) + '"/>')
    [void]$xml.Append('<RegistryValue Root="HKCU" Key="Software\Luma\Installer\Components" Name="' + $id + '" Type="integer" Value="1" KeyPath="yes"/>')
    if ($directory) { [void]$xml.Append('<RemoveFolder Id="R' + $id + '" On="uninstall"/>') }
    [void]$xml.Append('</Component></DirectoryRef>')
}
[void]$xml.Append('<ComponentGroup Id="LumaPayload">')
foreach ($id in $components) { [void]$xml.Append('<ComponentRef Id="' + $id + '"/>') }
[void]$xml.Append('</ComponentGroup></Fragment></Wix>')
$payloadSource = Join-Path $generated 'Payload.wxs'
$xml.ToString() | Set-Content -LiteralPath $payloadSource -Encoding utf8
$suffix = if ($Revision) { "-$Revision" } else { '' }
$output = Join-Path $projectRoot "artifacts\Luma-Setup-$version-x64$suffix.msi"
$languages = @(Get-Content (Join-Path $projectRoot 'installer\languages.json') -Raw | ConvertFrom-Json)
$productCode = [Guid]::NewGuid().ToString()
$transformDirectory = Join-Path $generated ('transforms-' + $productCode)
New-Item -ItemType Directory -Force $transformDirectory | Out-Null
$cabCache = Join-Path $generated 'cabs'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $csc)) { throw 'Compilatore .NET Framework 4.x x64 di Windows non trovato.' }
$saved = @{}
foreach ($name in 'DOTNET_ROOT','DOTNET_ROLL_FORWARD','DOTNET_CLI_HOME','APPDATA','LOCALAPPDATA') { $saved[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
try {
    $localDotnetRoot = Join-Path $projectRoot '.tools\dotnet'
    if (Test-Path (Join-Path $localDotnetRoot 'dotnet.exe')) { $env:DOTNET_ROOT = $localDotnetRoot }
    $env:DOTNET_ROLL_FORWARD = 'Major'
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\cli'; $env:APPDATA = Join-Path $projectRoot '.tools\profile'; $env:LOCALAPPDATA = Join-Path $projectRoot '.tools\local'
    Push-Location $projectRoot
    try {
        foreach ($language in $languages) {
            $culture = $language.culture
            $work = Join-Path $generated $culture
            New-Item -ItemType Directory -Force $work | Out-Null
            $localization = [xml](Get-Content (Join-Path $projectRoot "installer\localization\WixUI_$($language.source).wxl") -Raw)
            $localization.WixLocalization.SetAttribute('Culture', $culture)
            $localization.WixLocalization.RemoveAttribute('ExtensionDefaultCulture')
            $keys = 'SetupDescription','NewerVersion','WindowsRequired','ImageDescription','ViewerDescription','SetupFailed'
            for ($i=0; $i -lt $keys.Count; $i++) {
                $node = $localization.CreateElement('String','http://wixtoolset.org/schemas/v4/wxl')
                $node.SetAttribute('Id',$keys[$i]); $node.SetAttribute('Value',$language.strings[$i])
                [void]$localization.DocumentElement.AppendChild($node)
            }
            $node = $localization.CreateElement('String','http://wixtoolset.org/schemas/v4/wxl')
            $node.SetAttribute('Id','LanguageId'); $node.SetAttribute('Value',[string]$language.id)
            [void]$localization.DocumentElement.AppendChild($node)
            $locPath = Join-Path $work 'Luma.wxl'; $localization.Save($locPath)
            $localizedMsi = if ($language.key -eq 'en') { $output } else { Join-Path $work 'Luma.msi' }
            Write-Host "Installer: $culture"
            & $Compiler build (Join-Path $projectRoot 'installer\Luma.wxs') $payloadSource -arch x64 -ext WixToolset.UI.wixext -culture $culture -loc $locPath -d "AppVersion=$version" -d "ProjectRoot=$projectRoot" -d "ProductCode=$productCode" -cc $cabCache -pdbtype none -o $localizedMsi -intermediateFolder $work
            if ($LASTEXITCODE -ne 0) { throw "Creazione MSI fallita: $culture" }
            if ($language.key -ne 'en') {
                & $Compiler msi transform $output $localizedMsi -t language -serr f -o (Join-Path $transformDirectory "Luma_$culture.mst") -intermediateFolder $work
                if ($LASTEXITCODE -ne 0) { throw "Creazione traduzione MSI fallita: $culture" }
            }
        }
        $helper = Join-Path $generated 'InstallerTools.exe'
        & $csc /nologo /target:exe /platform:x64 "/out:$helper" (Join-Path $projectRoot 'installer\InstallerTools.cs')
        if ($LASTEXITCODE -ne 0) { throw 'Compilazione helper MSI fallita.' }
        & $helper $output $transformDirectory (($languages.id) -join ',')
        if ($LASTEXITCODE -ne 0) { throw 'Inserimento traduzioni MSI fallito.' }
        $setup = Join-Path $projectRoot "artifacts\Luma-Setup-$version-x64$suffix.exe"
        $assemblyInfo = Join-Path $generated 'SetupVersion.cs'
        "using System.Reflection; [assembly: AssemblyVersion(`"$version.0`")] [assembly: AssemblyFileVersion(`"$version.0`")] [assembly: AssemblyProduct(`"Luma Setup`")] [assembly: AssemblyCompany(`"KENNY`")]" | Set-Content $assemblyInfo -Encoding utf8
        & $csc /nologo /target:winexe /platform:x64 /optimize+ /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/win32manifest:$(Join-Path $projectRoot 'installer\Setup.manifest')" "/win32icon:$(Join-Path $projectRoot 'Luma\Assets\Luma.ico')" "/resource:$output,Luma.msi" "/resource:$(Join-Path $projectRoot 'installer\languages.json'),Languages.json" "/out:$setup" (Join-Path $projectRoot 'installer\Setup.cs') $assemblyInfo
        if ($LASTEXITCODE -ne 0) { throw 'Compilazione Setup multilingue fallita.' }
        $setupHash = Get-FileHash -LiteralPath $setup -Algorithm SHA256
        ($setupHash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($setup)) | Set-Content ($setup + '.sha256') -Encoding ascii
    } finally { Pop-Location }
} finally { foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name,$saved[$name],'Process') } }
$hash = Get-FileHash -LiteralPath $output -Algorithm SHA256
($hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($output)) | Set-Content -LiteralPath ($output + '.sha256') -Encoding ascii
Write-Host "Installer creato: $output"
