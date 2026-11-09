"""Package the independently built reactive player; never touches the stable players."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import zipfile
import argparse

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--recap", action="store_true", help="Package the separate recap playback build")
args = parser.parse_args()
variant = "Recap" if args.recap else "Reactive"
date = "20261110" if args.recap else "20261109"
app = root / f"Windows_{variant}"
out = root / f"Preview/Playtests/{variant}-{date}"
assert (app / f"CourtX {variant}.exe").is_file(), "Build the selected player first"
if args.recap:
    assert b"RecapPlayerReview" not in (app / "CourtX Recap_Data/Managed/Assembly-CSharp.dll").read_bytes(), "QA recorder leaked into release"
out.mkdir(parents=True, exist_ok=True)
(app / "Documentation").mkdir(exist_ok=True)
(app / "ThirdPartyNotices").mkdir(exist_ok=True)
for name in ("reactive-user-guide.md", "reactive-integration.md", "reactive-sources.md"):
    shutil.copy2(root / "docs" / name, app / "Documentation" / name)
if args.recap:
    shutil.copy2(root / "docs/recap-user-guide.md", app / "Documentation/recap-user-guide.md")
shutil.copy2(root / "ThirdPartyNotices/BasketWorld-MIT.txt", app / "ThirdPartyNotices/BasketWorld-MIT.txt")
shutil.copytree(root / "docs/verification/reactive-20261109/examples", app / "Examples", dirs_exist_ok=True)
revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
(app / "BuildInfo.json").write_text(json.dumps({
    "sourceRevision": revision, "branch": subprocess.check_output(["git", "branch", "--show-current"], cwd=root, text=True).strip(),
    "unity": "2022.3.62f3c1", "reviewMode": "local rules; optional provider unconfigured",
    "stableTag": "v0.3-tactics-stable-20261109",
}, ensure_ascii=False, indent=2), encoding="utf-8")
archive = out / f"CourtX-{variant}-{date}.zip"
files = sorted(p for p in app.rglob("*") if p.is_file())
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for path in files:
        z.write(path, path.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None, "Archive CRC check failed"
    for path in files:
        assert hashlib.sha256(z.read(path.relative_to(root).as_posix())).digest() == hashlib.sha256(path.read_bytes()).digest(), path
digest = hashlib.sha256(archive.read_bytes()).hexdigest().upper()
manifest = {"file": archive.name, "sha256": digest, "bytes": archive.stat().st_size,
            "entries": len(files), "sourceRevision": revision, "allArchivedFilesMatchBuild": True}
(out / "package-verification.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps(manifest, indent=2))
