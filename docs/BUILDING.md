# Build Luma from source

The repository contains both platforms as regular files. Source ZIP files in the repository root are no longer build inputs.

## Layout

- `Luma/`: Windows WPF application and shared resources, models, decoder configuration, and SVG input filtering.
- `Luma.Linux/`: Linux Avalonia application. References shared files in `Luma/`; keep both directories.
- `installer/`: multilingual Windows installer sources.
- `linux/`: Linux package recipes and integration files.
- `scripts/`: build and packaging scripts.
- `licenses/`: third-party license texts. `LICENSE` covers Luma's own code (GPL-3.0-only).
- `.github/workflows/`: manually triggered Windows and Linux builds.

## GitHub Actions

Run **Luma Windows packages** or **Luma Linux packages** from Actions, on `main`.

Windows artifacts contain EXE, MSI, portable ZIP, and installer checksums. Linux artifacts contain DEB/RPM and the Arch package separately. Upload the chosen files to the same release. Workflows do not publish releases automatically and do not sign binaries. They are independent of release tag names.

The Windows version metadata fix is in `Luma.csproj`; the installer accepts informational build metadata while still checking the app version. No workflow-time project editing is required.

The Linux workflow builds packages on remote Linux runners. It does not install WSL or a VM on your Windows computer. Package generation does not replace desktop runtime testing.

## Local Windows build

Install .NET SDK 10 and PowerShell 7. The .NET 8 runtime may also be used by WiX 5. Run:

```powershell
./scripts/Build.ps1 -Portable
```

For the installer, install WiX 5.0.2 and its matching UI extension in the environment used by `Build-Installer.ps1`. The workflow shows the exact local tool path and environment variables. Then run:

```powershell
./scripts/Build-Installer.ps1
```

Output is in `artifacts/`, excluded from version control. Restore dependencies from the configured NuGet feed; do not commit `.tools`, `.nuget`, `bin`, or `obj`.

## Local Linux build

Install .NET SDK 10 and native package tools for the target distribution.

```sh
dotnet publish Luma.Linux/Luma.Linux.csproj -c Release -r linux-x64 --self-contained true -p:PublishReadyToRun=true -p:RestoreConfigFile=NuGet.Config -o artifacts/linux/publish
bash linux/package.sh "$PWD/artifacts/linux/publish" "$PWD/artifacts/linux/packages"
```

The script above needs both `dpkg-deb` and `rpmbuild`. The Arch recipe is run on Arch as a non-root user; the GitHub workflow stages its inputs. The alternative nFPM build is described in `linux/README.md`.

## Release history and licensing

This source consolidation does not modify the existing `v0.3.4` tag, its source snapshots, or previously uploaded installers. Future builds include the selected GPL text and updated package metadata. Previously generated packages do not change retroactively.

Third-party code and binary components retain their licenses and redistribution conditions. The application license does not replace those conditions.
