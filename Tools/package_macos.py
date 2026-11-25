"""Package a built CourtX.app on Windows or macOS, preserving Unix semantics."""

import argparse
import hashlib
import os
from pathlib import Path
import plistlib
import shutil
import stat
import struct
from tempfile import TemporaryDirectory
import zipfile


def verify(archive, app_name, version):
    contents = app_name + "/Contents/"
    info = plistlib.loads(archive.read(contents + "Info.plist"))
    if info["CFBundlePackageType"] != "APPL" or info["CFBundleShortVersionString"] != version:
        raise ValueError("Unexpected application type or version")
    main = "MacOS/" + info["CFBundleExecutable"]
    for required in ("Resources/Data/level0", "Resources/Data/Managed/Assembly-CSharp.dll"):
        if archive.getinfo(contents + required).file_size == 0:
            raise ValueError(f"Empty game data at {required}")
    resources = plistlib.loads(archive.read(contents + "_CodeSignature/CodeResources"))
    native = [main]
    for path, record in resources["files2"].items():
        entry = archive.getinfo(contents + path)
        if "hash2" in record and hashlib.sha256(archive.read(entry)).digest() != record["hash2"]:
            raise ValueError(f"Resource hash mismatch for {path}")
        if "symlink" in record:
            if not stat.S_ISLNK(entry.external_attr >> 16) or archive.read(entry).decode() != record["symlink"]:
                raise ValueError(f"Invalid symbolic link for {path}")
        if "cdhash" in record:
            native.append(path)
    for path in native:
        entry = archive.getinfo(contents + path)
        with archive.open(entry) as binary:
            header = binary.read(48)
        magic, count = struct.unpack_from(">II", header)
        cpus = {struct.unpack_from(">I", header, offset)[0] for offset in (8, 28)}
        if (magic, count, cpus) != (0xCAFEBABE, 2, {0x01000007, 0x0100000C}):
            raise ValueError(f"Expected Intel x86_64 and Apple arm64 in {path}")
        if not (entry.external_attr >> 16) & stat.S_IXUSR:
            raise ValueError(f"Missing execute permission on {path}")
    if archive.testzip() is not None:
        raise ValueError("ZIP checksum verification failed")


def package(app, output, version):
    info = plistlib.loads((app / "Contents/Info.plist").read_bytes())
    binary = app / "Contents/MacOS" / info["CFBundleExecutable"]
    if output.is_relative_to(app):
        raise ValueError("Choose an output location outside the app")
    output.parent.mkdir(parents=True, exist_ok=True)
    notices = Path(__file__).resolve().parents[1] / "ThirdPartyNotices"
    with TemporaryDirectory(dir=output.parent) as stage:
        draft = Path(stage) / output.name
        with zipfile.ZipFile(draft, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for path in [app, *sorted(app.rglob("*"))]:
                is_link = path.is_symlink()
                is_dir = not is_link and path.is_dir()
                name = path.relative_to(app.parent).as_posix()
                entry = zipfile.ZipInfo(name + ("/" if is_dir else ""))
                entry.create_system = 3
                entry.compress_type = zipfile.ZIP_DEFLATED
                if is_link:
                    mode = stat.S_IFLNK | 0o777
                elif is_dir:
                    mode = stat.S_IFDIR | 0o755
                else:
                    executable = path == binary or path.suffix == ".dylib"
                    mode = stat.S_IFREG | (0o755 if executable else 0o644)
                entry.external_attr = (mode << 16) | (0x10 if is_dir else 0)
                if is_link:
                    archive.writestr(entry, os.readlink(path).replace("\\", "/").encode())
                elif is_dir:
                    archive.writestr(entry, b"")
                else:
                    with path.open("rb") as src, archive.open(entry, "w") as dst:
                        shutil.copyfileobj(src, dst)
            for notice in sorted(notices.glob("*.txt")):
                entry = zipfile.ZipInfo("ThirdPartyNotices/" + notice.name)
                entry.create_system = 3
                entry.external_attr = (stat.S_IFREG | 0o644) << 16
                entry.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(entry, notice.read_bytes())
        with zipfile.ZipFile(draft) as archive:
            verify(archive, app.name, version)
        os.replace(draft, output)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("app", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    package(args.app.absolute(), args.output.absolute(), args.version)
