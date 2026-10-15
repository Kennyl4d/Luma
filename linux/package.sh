#!/usr/bin/env bash
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
publish="${1:?Publish directory required}"; output="${2:?Output directory required}"
version="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$root/Luma.Linux/Luma.Linux.csproj")"
mkdir -p "$output"; output="$(cd "$output" && pwd)"
work="$(mktemp -d)"; trap 'rm -rf -- "$work"' EXIT
bash "$root/linux/stage.sh" "$publish" "$work/stage"
mkdir -p "$work/stage/DEBIAN"
size="$(du -sk "$work/stage/usr" | cut -f1)"
cat > "$work/stage/DEBIAN/control" <<EOF
Package: luma
Version: $version-1
Architecture: amd64
Maintainer: KENNY
Section: graphics
Priority: optional
Installed-Size: $size
Depends: libc6 (>= 2.28), libgcc-s1, libstdc++6, libfontconfig1, libx11-6, libice6, libsm6, libgtk-3-0 | libgtk-3-0t64, libtesseract5, libssl3 | libssl3t64, libicu72 | libicu74 | libicu76 | libicu77 | libicu78 | libicu70
Recommends: xwayland, xdg-desktop-portal, xdg-desktop-portal-gtk | xdg-desktop-portal-kde, desktop-file-utils, shared-mime-info
Description: Minimal image viewer with local text recognition
 JPEG, PNG, animated GIF, HEIC/HEIF, WebP, AVIF, BMP/DIB, TIFF, ICO, SVG.
 Automatic localization in 14 languages. Author: KENNY.
EOF
install -m755 "$root/linux/packaging/refresh-desktop.sh" "$work/stage/DEBIAN/postinst"
install -m755 "$root/linux/packaging/refresh-desktop.sh" "$work/stage/DEBIAN/postrm"
dpkg-deb --root-owner-group --build "$work/stage" "$output/luma_${version}-1_amd64.deb"
mkdir -p "$work/rpm"/{BUILD,BUILDROOT,RPMS,SOURCES,SPECS,SRPMS}
bash "$root/linux/stage.sh" "$publish" "$work/rpm-stage"
cat > "$work/rpm/SPECS/luma.spec" <<EOF
Name: luma
Version: $version
Release: 1
Summary: Minimal image viewer with local text recognition
License: GPL-3.0-only
BuildArch: x86_64
AutoReqProv: no
Requires: glibc >= 2.28, libgcc, libstdc++, fontconfig, libX11, libICE, libSM, gtk3, openssl-libs, libicu
Requires: libtesseract.so.5()(64bit)
Recommends: xorg-x11-server-Xwayland, xdg-desktop-portal
%description
Luma 0.3.4 Linux port. Image formats, animated GIF, zoom, fullscreen,
rotation, 14 automatic languages, system theme and offline text selection.
Author: KENNY.
%global __os_install_post %{nil}
%global _build_id_links none
%install
mkdir -p %{buildroot}
cp -a "$work/rpm-stage/." %{buildroot}/
%files
/usr/lib/luma
/usr/bin/luma
/usr/share/applications/luma.desktop
/usr/share/icons/hicolor/256x256/apps/luma.png
/usr/share/mime/packages/luma.xml
/usr/share/doc/luma
%post
$(cat "$root/linux/packaging/refresh-desktop.sh")
%postun
$(cat "$root/linux/packaging/refresh-desktop.sh")
EOF
rpmbuild --define "_topdir $work/rpm" --define "_buildrootdir $work/rpm/BUILDROOT" -bb "$work/rpm/SPECS/luma.spec"
cp "$work/rpm/RPMS/x86_64/"*.rpm "$output/"
