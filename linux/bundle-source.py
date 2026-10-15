from pathlib import Path
import zipfile
root=Path(__file__).resolve().parents[1]
output=root/'artifacts/linux/Luma-0.3.4-linux-source.zip'
files=[]
for folder in ['Luma.Linux','linux','licenses']:
 files.extend(path for path in (root/folder).rglob('*') if path.is_file() and not any(part in ['bin','obj','__pycache__'] for part in path.relative_to(root).parts))
for folder in ['Luma/Assets','Luma/Resources','Luma/DecoderConfig','Luma/tessdata']:
 files.extend(path for path in (root/folder).rglob('*') if path.is_file())
files += [root/path for path in ['Luma/Imaging/SvgInput.cs','scripts/Build-Linux.ps1','NuGet.Config','LICENSE','THIRD-PARTY-NOTICES.md','.gitignore','.gitattributes','.github/workflows/linux.yml']]
with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
 for path in sorted(set(files)):archive.write(path,path.relative_to(root).as_posix())
print(output.name,output.stat().st_size)
