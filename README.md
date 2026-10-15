# Luma

A lightweight image viewer for Windows and Linux, built around a simple idea: open your images quickly, with a clean interface and no unnecessary features.

Inspired by the simplicity of the classic Windows Photo Viewer, Luma combines essential viewing tools with modern image formats and offline text recognition.


## Downloads

Download installers and packages from the [Releases page](https://github.com/Yisus89/Luma/releases).

| Platform | Available downloads |
|---|---|
| Windows x64 | EXE installer, MSI installer, portable ZIP |
| Linux x86_64 | DEB, RPM, Arch Linux `.pkg.tar.zst` |

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

Download the package for your distribution and install it using its package manager.

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

Found a problem? Open an [issue](https://github.com/Yisus89/Luma/issues) and include:

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
