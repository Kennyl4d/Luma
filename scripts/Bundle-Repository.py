"""Stage explicit project sources for GitHub, without caches or build output."""
from pathlib import Path
import shutil
import zipfile

root = Path(__file__).resolve().parents[1]
destination = root / 'artifacts/github-ready/Luma'
if destination.exists():
    raise SystemExit('Destination already exists; choose a new staging location before rerunning.')
destination.mkdir(parents=True)
files = []
for name in ['Luma', 'Luma.Linux', 'scripts', 'installer', 'linux', 'licenses', '.github', 'docs']:
    files.extend(path for path in (root / name).rglob('*')
                 if path.is_file() and not any(part in {'bin', 'obj', '__pycache__', '.vs'}
                                              for part in path.relative_to(root).parts))
files.extend(root / name for name in ['README.md', 'LICENSE', 'NuGet.Config',
             'THIRD-PARTY-NOTICES.md', 'BUILD-STATUS.md', '.gitignore', '.gitattributes'])
for source in sorted(set(files)):
    target = destination / source.relative_to(root)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, target)
archive_path = destination.parent / 'Luma-GitHub-ready.zip'
with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for path in sorted(destination.rglob('*')):
        if path.is_file():
            archive.write(path, path.relative_to(destination.parent).as_posix())
print(f'{len(set(files))} files staged; archive: {archive_path.stat().st_size} bytes')
for folder in sorted(destination.iterdir()):
    if folder.is_dir():
        print(folder.name, sum(path.is_file() for path in folder.rglob('*')))
