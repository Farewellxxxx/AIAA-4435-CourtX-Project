"""Package the already built Windows player; never include local practice records."""
from pathlib import Path
import hashlib
import shutil
import subprocess
import zipfile

source = Path(__file__).resolve().parents[1]
project = Path('E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX')
release = project / 'Windows_FivePlayer_1116'
assert (release / 'CourtX.exe').is_file()
assembly = (release / 'CourtX_Data/Managed/Assembly-CSharp.dll').read_bytes()
assert b'FiveCourtGame' in assembly and b'FivePlayerPlaytest' not in assembly
shutil.copyfile(source / 'docs/FIVE_PLAYER_GUIDE_ZH.md', release / '使用说明.md')
shutil.copyfile(source / 'docs/verification/five-player-20261116/RESULTS.md', release / '验证记录.md')
revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=source, text=True).strip()
(release / '版本.txt').write_text(
    f'CourtX 5v5 | 2026-11-16 | 暂停布防版\nSource commit: {revision}\n'
    'Unity 2022.3.62f3c1 | Windows x64 | offline\n'
    '运行 CourtX.exe；分享请复制整个文件夹。\n'
    'Q 直接传球 · E 要球 · C 再 call · V 回放 · Esc 暂停\n'
    '真实回合保存在 Replays5v5；暂停布防不推进时间。\n', encoding='utf-8')
files = sorted(p for p in release.rglob('*') if p.is_file()
               and 'Replays5v5' not in p.relative_to(release).parts
               and p.name != 'SHA256SUMS.txt')
manifest = release / 'SHA256SUMS.txt'
manifest.write_text('\n'.join(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.relative_to(release).as_posix()}'
                              for p in files) + '\n', encoding='utf-8')
archive = project / 'CourtX_FivePlayer_20261116.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as bundle:
    for path in [*files, manifest]:
        bundle.write(path, Path('Windows_FivePlayer_1116') / path.relative_to(release))
with zipfile.ZipFile(archive) as bundle:
    assert bundle.testzip() is None
    assert not any('Replays5v5/' in name for name in bundle.namelist())
print(f'Package verified: {archive} ({archive.stat().st_size / 1024**2:.1f} MiB)')
print(f'Runtime files: {len(files)}; SHA256 {hashlib.sha256(archive.read_bytes()).hexdigest()}')
