"""Deliver 1117, preserving local 1116 records outside the shareable ZIP."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import subprocess
import zipfile

source = Path(__file__).resolve().parents[1]
project = Path('E:/study documents/2026 Fall/AIAA 4435 Inter/CourtX')
release = project / 'Windows_FivePlayer_1117'
evidence = project / 'Preview/Playtests/FivePlayer-20261117'


def sha256(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def copy_history():
    previous = project / 'Windows_FivePlayer_1116/Replays5v5'
    destination = release / 'Replays5v5'
    if not previous.is_dir():
        raise FileNotFoundError(previous)
    hashes = {p.relative_to(previous).as_posix(): sha256(p)
              for p in sorted(previous.rglob('*')) if p.is_file()}
    if not hashes:
        raise ValueError('No 1116 history was found')
    for name, digest in hashes.items():
        new = destination / name
        new.parent.mkdir(parents=True, exist_ok=True)
        if not new.exists():
            shutil.copy2(previous / name, new)
        if sha256(new) != digest or sha256(previous / name) != digest:
            raise ValueError(f'History verification failed for {name}')
    evidence.mkdir(parents=True, exist_ok=True)
    (evidence / 'copied-history-sha256.json').write_text(json.dumps(hashes, indent=2) + '\n', encoding='utf-8')
    print(f'History verified: {len(hashes)} files match 1116 SHA256 hashes.')


def package():
    if not (release / 'CourtX.exe').is_file():
        raise FileNotFoundError('Build the 1117 production player before packaging')
    assembly = (release / 'CourtX_Data/Managed/Assembly-CSharp.dll').read_bytes()
    if b'FiveCourtGame' not in assembly or b'FivePlayerPlaytest' in assembly:
        raise ValueError('Expected the five-player production assembly without the QA harness')
    copy_history()
    shutil.copyfile(source / 'docs/FIVE_PLAYER_GUIDE_ZH.md', release / '使用说明.md')
    shutil.copyfile(source / 'docs/verification/five-player-20261117/RESULTS.md', release / '验证记录.md')
    revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=source, text=True).strip()
    (release / '版本.txt').write_text(
        f'CourtX 5v5 | 2026-11-17 | 2D 战术板与分组回放\nSource commit: {revision}\n'
        'Unity 2022.3.62f3c1 | Windows x64 | offline\n'
        '运行 CourtX.exe；分享请复制整个文件夹。\n'
        'Q 直接传球 · E 要球 · C 再 call · V 回放 · Esc 暂停\n'
        '真实回合保存在 Replays5v5；暂停布防不推进时间。\n', encoding='utf-8')
    files = {}
    for root in ('CourtX_Data', 'MonoBleedingEdge'):
        for path in sorted((release / root).rglob('*')):
            if path.is_file():
                files[path.relative_to(release).as_posix()] = sha256(path)
    for name in ('CourtX.exe', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', '版本.txt', '使用说明.md', '验证记录.md'):
        files[name] = sha256(release / name)
    manifest = release / 'SHA256SUMS.txt'
    manifest.write_text(''.join(f'{digest}  {name}\n' for name, digest in sorted(files.items())), encoding='utf-8')
    archive = project / 'CourtX_FivePlayer_20261117.zip'
    staging = archive.with_suffix('.zip.tmp')
    prefix = release.name + '/'
    with zipfile.ZipFile(staging, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as bundle:
        for name in [*sorted(files), manifest.name]:
            bundle.write(release / name, prefix + name)
    with zipfile.ZipFile(staging) as bundle:
        expected = {prefix + name for name in [*files, manifest.name]}
        if bundle.testzip() is not None or set(bundle.namelist()) != expected:
            raise ValueError('Archive contents do not match verified production files')
        if any(set(Path(name).parts) & {'Replays5v5', 'QA-SyntheticSessions', 'QA'} for name in bundle.namelist()):
            raise ValueError('Archive includes practice or QA data')
        for name, digest in files.items():
            with bundle.open(prefix + name) as member:
                if hashlib.file_digest(member, 'sha256').hexdigest() != digest:
                    raise ValueError(f'Archive member failed SHA256 verification: {name}')
    staging.replace(archive)
    print(f'Package verified: {archive} ({archive.stat().st_size / 1024**2:.1f} MiB)')
    print(f'Delivery files: {len(files)} + checksum manifest; SHA256 {sha256(archive)}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--copy-history-only', action='store_true', help='copy/verify old records for read-only QA')
    if parser.parse_args().copy_history_only:
        copy_history()
    else:
        package()
