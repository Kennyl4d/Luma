<h1 align="center">Luma</h1>

<p align="center">
  <img width="112" height="112" alt="Luma" src="https://github.com/user-attachments/assets/0cc01265-5a03-4634-bfef-1842ff27d2e7" />
</p>

<p align="center">
  <strong>A lightweight image viewer for Windows and Linux.</strong><br>
  Open your images quickly, with a clean interface and no unnecessary features.
</p>

<p align="center">
  <a href="../../releases/tag/windows-v0.3.4"><img alt="Download for Windows" src="https://img.shields.io/badge/Download-Windows-0078D4?style=flat-square"></a>
  <a href="../../releases/tag/linux-v0.3.4"><img alt="Download for Linux — prerelease" src="https://img.shields.io/badge/Download-Linux%20%28prerelease%29-FCC624?style=flat-square"></a>
  <a href="../../actions/workflows/windows.yml"><img alt="Windows build" src="https://img.shields.io/github/actions/workflow/status/Kennyl4d/Luma/windows.yml?branch=main&label=Windows%20build&style=flat-square"></a>
  <a href="LICENSE"><img alt="License: GPL v3.0" src="https://img.shields.io/badge/License-GPL--3.0-blue?style=flat-square"></a>
</p>

<p align="center">
Inspired by the simplicity of the classic Windows Photo Viewer, Luma combines essential viewing tools with modern image formats and offline text recognition.
</p>

<img width="820" height="567" alt="Luma image viewer screenshot" src="https://github.com/user-attachments/assets/8b82f24b-27e2-4963-a575-433f43eeb0be" />

## Downloads

Download installers and packages from the [Releases page](../../releases).

| Platform | Available downloads |
|---|---|
| Windows 10 / 11 x64 | EXE installer, MSI installer, portable ZIP |
| Linux x86_64 | DEB, RPM, Arch Linux `.pkg.tar.zst` |

**Windows requirements:** Windows 10 version 1607 (build 14393) or later, or Windows 11, 64-bit. This is the package's configured minimum; execution on Windows 10, particularly older builds and editions, has not yet been validated. The .NET runtime is included.

**Linux status:** the Linux port is currently a prerelease. Packages have been generated, but functionality, installation, and performance still require testing on Linux desktops.

## Features

- Minimal interface with light, dark, and system themes.
- Previous and next image navigation.
- Zoom, fit to window, and actual-size viewing.
- Fullscreen mode.
- Image rotation without modifying the original file.
- Animated GIF playback.
- Automatic interface language selection.
- Offline text recognition with text selection and copying directly from images.
- File associations for opening images with Luma.
- Application information and a custom icon.

## Supported formats

JPEG · PNG · GIF · HEIC · HEIF · WebP · AVIF · BMP · DIB · TIF · TIFF · ICO · SVG

GIF animation is supported. TIFF displays the first page, SVG rendering is static, and other supported formats display a single image or frame.

## Languages

Luma automatically follows the system language, with English as the fallback.

Available translations:

English, Italian, Czech, German, Spanish, French, Japanese, Korean, Polish, Brazilian Portuguese, Russian, Turkish, Simplified Chinese, and Traditional Chinese.

## Installation

### Windows

Download and run the EXE installer from the Releases page.

For portable use, download the Windows portable ZIP, extract it, and launch `Luma.exe`. Keep the extracted files together.

To make Luma your default image viewer, choose it in Windows default app settings or through **Open with**.

### Linux

Download the package for your distribution. Open a terminal in the folder containing the downloaded file and run the appropriate command below. These commands install a **local package**; no Luma package repository is currently provided. Required dependencies may be downloaded from your distribution's repositories.

**Debian / Ubuntu**

```sh
sudo apt install ./luma_0.3.4-1_amd64.deb
```

**Fedora**

```sh
sudo dnf install ./luma-0.3.4-1.x86_64.rpm
```

**Arch Linux**

```sh
sudo pacman -U ./luma-0.3.4-1-x86_64.pkg.tar.zst
```

Linux packages target modern glibc-based x86_64 systems. On Wayland desktops, Luma currently uses XWayland.

To update Luma, download a newer package from Releases and install it using the same method.

## Text recognition and privacy

Text recognition runs locally on your computer. Images are not uploaded to an online OCR service.

Recognized words can be selected directly on the image and copied with **Ctrl+C** or the context menu. Hold **Shift** to pan the image over recognized text.

Text recognition is not available for animated GIFs.

## Source code and builds

Windows and Linux source archives are available in this repository.

The project uses:

- **Windows:** C# / .NET and WPF.
- **Linux:** C# / .NET and Avalonia.
- **Image decoding:** Magick.NET / ImageMagick, alongside platform-specific decoding.
- **Text recognition:** Tesseract OCR.

GitHub Actions workflows are provided for building Windows and Linux packages.

## Feedback

Found a problem? Open an [issue](../../issues) and include:

- Your operating system and Luma version.
- The image format involved.
- Steps to reproduce the problem.
- The error message or a screenshot, if available.

Only attach images you are comfortable sharing.

## License

Luma's own code is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).

Third-party libraries, models, and other components retain their respective licenses. Their notices and license files are included in the source archives and distribution packages.

## Author

**KENNY**
