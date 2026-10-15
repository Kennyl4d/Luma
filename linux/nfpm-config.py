"""Generate nFPM configuration with explicit Linux ownership/modes on any build OS."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
publish=Path(sys.argv[1]).resolve()
output=Path(sys.argv[2]).resolve()
contents=[]
def file(src,dst,mode=0o644):
 contents.append({'src':str(Path(src).resolve()),'dst':dst,'file_info':{'mode':mode,'owner':'root','group':'root'}})
dirs=set()
for path in sorted(publish.rglob('*')):
 if path.is_file() and path.suffix not in ['.pdb']:
  rel=path.relative_to(publish).as_posix()
  file(path,'/usr/lib/luma/'+rel,0o755 if rel in ['luma','createdump'] else 0o644)
  parent=Path('/usr/lib/luma/'+rel).parent
  while str(parent).replace('\\','/') not in ['/usr/lib','/usr','/']:
   dirs.add(str(parent).replace('\\','/'));parent=parent.parent
file(root/'linux/packaging/luma','/usr/bin/luma',0o755)
file(root/'linux/packaging/luma.desktop','/usr/share/applications/luma.desktop')
file(root/'linux/packaging/luma-mime.xml','/usr/share/mime/packages/luma.xml')
file(root/'Luma/Assets/Luma.png','/usr/share/icons/hicolor/256x256/apps/luma.png')
file(root/'THIRD-PARTY-NOTICES.md','/usr/share/doc/luma/THIRD-PARTY-NOTICES.md')
file(root/'LICENSE','/usr/share/doc/luma/LICENSE')
for path in (root/'licenses').glob('*'):
 if path.is_file():file(path,'/usr/share/doc/luma/licenses/'+path.name)
dirs.update(['/usr/share/doc/luma','/usr/share/doc/luma/licenses'])
contents += [{'dst':path,'type':'dir','file_info':{'mode':0o755,'owner':'root','group':'root'}} for path in sorted(dirs)]
config={
 'name':'luma','arch':'amd64','platform':'linux','version':'0.3.4','release':'1',
 'maintainer':'KENNY','vendor':'KENNY','description':'Minimal image viewer with local text recognition\nLinux port of Luma 0.3.4. JPEG, PNG, animated GIF, HEIC/HEIF, WebP, AVIF, BMP/DIB, TIFF, ICO, SVG. Automatic localization in 14 languages.',
 'license':'GPL-3.0-only','section':'graphics','priority':'optional',
 'contents':contents,
 'scripts':{'postinstall':str(root/'linux/packaging/refresh-desktop.sh'),'postremove':str(root/'linux/packaging/refresh-desktop.sh')},
 'overrides':{
  'deb':{'depends':['libc6 (>= 2.28)','libgcc-s1','libstdc++6','libfontconfig1','libx11-6','libice6','libsm6','libgtk-3-0 | libgtk-3-0t64','libtesseract5','libssl3 | libssl3t64','libicu70 | libicu72 | libicu74 | libicu76 | libicu77 | libicu78'],'recommends':['xwayland','xdg-desktop-portal','xdg-desktop-portal-gtk | xdg-desktop-portal-kde','desktop-file-utils','shared-mime-info']},
  'rpm':{'depends':['glibc >= 2.28','libgcc','libstdc++','fontconfig','libX11','libICE','libSM','gtk3','openssl-libs','libicu','libtesseract.so.5()(64bit)'],'recommends':['xorg-x11-server-Xwayland','xdg-desktop-portal']},
  'archlinux':{'depends':['glibc','gcc-libs','fontconfig','libx11','libice','libsm','gtk3','openssl','icu','tesseract'],'suggests':['xorg-xwayland','xdg-desktop-portal','xdg-desktop-portal-gtk','xdg-desktop-portal-kde']}
 },
 'rpm':{'compression':'gzip:9','packager':'KENNY'},'archlinux':{'packager':'KENNY'},'deb':{'compression':'gzip'},
}
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(config,ensure_ascii=False,indent=2),encoding='utf-8')
