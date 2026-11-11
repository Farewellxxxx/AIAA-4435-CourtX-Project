"""Package the visual-polish player without changing older releases or user rounds."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import zipfile

root = Path(__file__).resolve().parents[1]
app = root / "Windows_LearningPolish"
out = root / "Preview/Playtests/Polish-20261111"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


assert (app / "CourtX Learning.exe").is_file(), "Build the polish player first"
assembly = app / "CourtX Learning_Data/Managed/Assembly-CSharp.dll"
binary = assembly.read_bytes()
assert all(name not in binary for name in (b"PolishPlayerReview", b"LearningPlayerReview", b"RecapPlayerReview"))
assert b"LearningHubUI" in binary
assert not subprocess.check_output(["git", "diff", "HEAD", "--", "Unity/Assets"], cwd=root)
revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
branch = subprocess.check_output(["git", "branch", "--show-current"], cwd=root, text=True).strip()
assert branch == "feature/learning-visual-polish"

old_specs = [
    ("Learning-20261111/CourtX-Learning-20261111.zip", "12E332E99CEDC136DFC5FC665A1155354D0AF01BD6C7A1C7936419EC240A517E"),
    ("Recap-20261110/CourtX-Recap-20261110.zip", "6D84FA90EF27D9B08E1C620306A618402419ADC7C5393D1D3CFC14AB2A815F60"),
    ("Reactive-20261109/CourtX-Reactive-20261109.zip", "14EE6EF40B4C2524415BDCFD6C7679B0D6357AA586114F4A54AB7F7C71F7BF52"),
]
preserved = {}
for name, expected in old_specs:
    archive = root / "Preview/Playtests" / name
    assert sha(archive) == expected, "Older archive changed: " + name
    count = 0
    with zipfile.ZipFile(archive) as z:
        for info in z.infolist():
            if info.is_dir():
                continue
            file = root / info.filename
            assert file.is_file() and file.read_bytes() == z.read(info), "Older build changed: " + info.filename
            count += 1
    preserved[name] = {"sha256": expected, "filesMatched": count}
assert sha(root / "Windows_Tactics/CourtX Tactics_Data/Managed/Assembly-CSharp.dll") == "084CE09D4B26B2110E54F1588FC0E05BF6C24E4930D364D2BAD85D082F097264"
assert sha(root / "Windows_Learning/CourtX Learning_Data/Managed/Assembly-CSharp.dll") == "8CCF92C1157EC8FEC950B4A83E4F2F6B2176F5C25B08F6D9338CA0D060220967"

rounds = json.loads((out / "user-replay-preservation.json").read_text(encoding="utf-8-sig"))
(app / "Replays").mkdir(exist_ok=True)
for item in rounds:
    original = Path(item["Path"])
    assert sha(original) == item["Hash"], "Original user round changed"
    target = app / "Replays" / original.name
    if target.exists():
        assert sha(target) == item["Hash"], "Conflicting round in new build: " + target.name
    else:
        shutil.copy2(original, target)
    assert sha(target) == item["Hash"]

(app / "Documentation").mkdir(exist_ok=True)
(app / "ThirdPartyNotices").mkdir(exist_ok=True)
for name in ("LEARNING_POLISH.md", "LEARNING_GUIDE_ZH.md", "LEARNING_INTEGRATION.md", "reactive-sources.md"):
    shutil.copy2(root / "docs" / name, app / "Documentation" / name)
shutil.copy2(root / "ThirdPartyNotices/BasketWorld-MIT.txt", app / "ThirdPartyNotices/BasketWorld-MIT.txt")
(app / "BuildInfo.json").write_text(json.dumps({"sourceRevision": revision, "branch": branch,
    "unity": "2022.3.62f3c1", "stableTag": "v0.5-learning-stable-20261111",
    "mode": "offline local rules; optional LLM provider unconfigured", "assemblySha256": sha(assembly)}, indent=2), encoding="utf-8")

# User replay continuity is local only. Do not publish personal rounds as default history.
files = sorted(p for p in app.rglob("*") if p.is_file() and "Replays" not in p.relative_to(app).parts and p.suffix.lower() != ".log")
archive = out / "CourtX-LearningPolish-20261111.zip"
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for path in files:
        z.write(path, path.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for path in files:
        assert z.read(path.relative_to(root).as_posix()) == path.read_bytes()
manifest = {"file": archive.name, "sha256": sha(archive), "bytes": archive.stat().st_size,
    "entries": len(files), "sourceRevision": revision, "assemblySha256": sha(assembly),
    "qaInstrumentationAbsent": True, "allArchivedFilesMatchBuild": True,
    "preservedUserFiles": len(rounds), "preserved": preserved}
(out / "package-verification.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps(manifest, indent=2))
