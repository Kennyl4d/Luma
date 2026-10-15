#!/usr/bin/env bash
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
publish="${1:?Publish directory required}"
stage="${2:?Stage directory required}"
mkdir -p "$stage/usr/lib/luma" "$stage/usr/bin" "$stage/usr/share/applications" "$stage/usr/share/icons/hicolor/256x256/apps" "$stage/usr/share/mime/packages" "$stage/usr/share/doc/luma/licenses"
cp -a "$publish/." "$stage/usr/lib/luma/"
chmod 755 "$stage/usr/lib/luma/luma"
install -m755 "$root/linux/packaging/luma" "$stage/usr/bin/luma"
install -m644 "$root/linux/packaging/luma.desktop" "$stage/usr/share/applications/luma.desktop"
install -m644 "$root/linux/packaging/luma-mime.xml" "$stage/usr/share/mime/packages/luma.xml"
install -m644 "$root/Luma/Assets/Luma.png" "$stage/usr/share/icons/hicolor/256x256/apps/luma.png"
install -m644 "$root/THIRD-PARTY-NOTICES.md" "$stage/usr/share/doc/luma/THIRD-PARTY-NOTICES.md"
install -m644 "$root/LICENSE" "$stage/usr/share/doc/luma/LICENSE"
cp -a "$root/licenses/." "$stage/usr/share/doc/luma/licenses/"
find "$stage" -type d -exec chmod 755 {} +
