"""Package the independently built reactive player; never touches the stable players."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import zipfile

root = Path(__file__).resolve().parents[1]
app = root / "Windows_Reactive"
out = root / "Preview/Playtests/Reactive-20261109"
assert (app / "CourtX Reactive.exe").is_file(), "Build the reactive player first"
out.mkdir(parents=True, exist_ok=True)
(app / "Documentation").mkdir(exist_ok=True)
(app / "ThirdPartyNotices").mkdir(exist_ok=True)
for name in ("reactive-user-guide.md", "reactive-integration.md", "reactive-sources.md"):
    shutil.copy2(root / "docs" / name, app / "Documentation" / name)
shutil.copy2(root / "ThirdPartyNotices/BasketWorld-MIT.txt", app / "ThirdPartyNotices/BasketWorld-MIT.txt")
shutil.copytree(root / "docs/verification/reactive-20261109/examples", app / "Examples", dirs_exist_ok=True)
revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
(app / "BuildInfo.json").write_text(json.dumps({
    "sourceRevision": revision, "branch": "feature/reactive-tactics-review",
    "unity": "2022.3.62f3c1", "reviewMode": "local rules; optional provider unconfigured",
    "stableTag": "v0.3-tactics-stable-20261109",
}, ensure_ascii=False, indent=2), encoding="utf-8")
archive = out / "CourtX-Reactive-20261109.zip"
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
