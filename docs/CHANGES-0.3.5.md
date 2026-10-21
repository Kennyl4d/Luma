# Luma 0.3.5

- Mouse-wheel zoom preserves the point under the pointer, including rotated images. Toolbar and keyboard zoom use the viewport center. Scroll offsets are clamped at image edges.
- OCR starts disabled for every image. The OCR toolbar button enables it for the current still image; clicking again cancels recognition and removes the selectable text overlay. Navigation resets it to off. Animated GIFs keep OCR disabled.
- About includes a manual **Check for updates** action. It queries the latest stable public GitHub release, compares numeric versions, and offers a browser link when a newer release exists. No automatic check or installation is performed. Missing releases, inaccessible repositories, connection failures, API errors and timeouts are handled.
- Removed the 128 MiB file-size and 40 MP image limits in both loaders. Removed the decoder's fixed 192 MiB memory allocation caps. Actual memory availability and decoder/platform capabilities still apply. Existing animation limits and SVG input filtering remain.
- New OCR, update, and insufficient-memory messages are translated into all 14 interface languages. Windows and Linux share the update-check implementation and translation catalog.
- Application and packaging version is 0.3.5.

## Validation

Builds and self-contained publication succeeded for Windows x64 and Linux x64 with .NET 10.0.402.

- Update and localization regression tests: 76 assertions, plus a live GitHub API check.
- WPF viewer regression tests: 36 assertions. Generated 54 MP / 154 MiB BMP loaded through Windows decoding, and 54 MP TIFF loaded through ImageMagick. Zoom-in and zoom-out anchors were checked at 0, 90, 180 and 270 degrees, along with OCR defaults/reset/clearing.
- Avalonia viewer regression tests: 39 assertions using the headless platform on Windows. Generated 54 MP / 154 MiB BMP loaded through the Linux application's ImageMagick loader. Zoom and OCR state were checked, and the About UI loaded successfully.

Linux desktop runtime testing is still required on a Linux machine, including native OCR, file dialogs, browser launch, and installation of distribution packages. The headless tests validate the Avalonia layout and application code, not an actual Linux desktop session.

## Reproduce tests

From the repository root with .NET SDK 10 installed:

```sh
dotnet run --project tests/UpdateChecks -c Release
dotnet run --project tests/LinuxViewer -c Release
```

On Windows:

```powershell
dotnet run --project tests/WindowsViewer -c Release
```

Pass `-- --live` to UpdateChecks to include the optional network check. Viewer tests generate temporary images, including a roughly 154 MiB BMP; allow sufficient temporary disk space and memory. WPF tests position their windows outside the desktop and remove their image fixtures afterward.
