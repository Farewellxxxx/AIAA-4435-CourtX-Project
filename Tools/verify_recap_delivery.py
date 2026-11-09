"""Check preserved release files against their original archive and the new package."""
from pathlib import Path
import hashlib
import json
import zipfile

root = Path(__file__).resolve().parents[1]
old = root / "Preview/Playtests/Reactive-20261109/CourtX-Reactive-20261109.zip"
assert hashlib.sha256(old.read_bytes()).hexdigest().upper() == "14EE6EF40B4C2524415BDCFD6C7679B0D6357AA586114F4A54AB7F7C71F7BF52"
count = 0
with zipfile.ZipFile(old) as z:
    for info in z.infolist():
        if info.is_dir():
            continue
        file = root / info.filename
        assert file.is_file() and hashlib.sha256(file.read_bytes()).digest() == hashlib.sha256(z.read(info)).digest(), info.filename
        count += 1
old_tactical = root / "Windows_Tactics/CourtX Tactics_Data/Managed/Assembly-CSharp.dll"
assert hashlib.sha256(old_tactical.read_bytes()).hexdigest().upper() == "084CE09D4B26B2110E54F1588FC0E05BF6C24E4930D364D2BAD85D082F097264"
out = root / "Preview/Playtests/Recap-20261110"
manifest = json.loads((out / "package-verification.json").read_text())
assert hashlib.sha256((out / manifest["file"]).read_bytes()).hexdigest().upper() == manifest["sha256"]
result = {"oldReactiveArchiveUnchanged": True, "oldReactiveFilesMatched": count,
          "oldTacticalAssemblyUnchanged": True, "newArchiveHashVerified": True}
(out / "stable-preservation.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps(result, indent=2))
