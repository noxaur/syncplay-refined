#!/usr/bin/env python3
"""Pack the plugin zip and prepend a catalog entry. Versions are YYYY.M.D.N (UTC)."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import zipfile
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
PLUGIN = REPO / "Jellyfin.Plugin.SyncPlayRefined"
CSPROJ = PLUGIN / "Jellyfin.Plugin.SyncPlayRefined.csproj"
BUILD_YAML = REPO / "build.yaml"
MANIFEST = REPO / "manifest.json"
DIST = REPO / "dist"
VER_TAG = re.compile(r"<(AssemblyVersion|FileVersion)>[^<]+</\1>")


def next_calver(existing: list[str], day: datetime) -> str:
    prefix = f"{day.year}.{day.month}.{day.day}."
    n = 0
    for v in existing:
        if v.startswith(prefix):
            n = max(n, int(v.split(".")[3]) + 1)
    return f"{prefix}{n}"


def source_hash(plugin_dir: Path) -> str:
    # ponytail: source fingerprint, not the built DLL. Rebuilds with the same inputs skip.
    h = hashlib.sha256()
    files = [
        p
        for p in plugin_dir.rglob("*")
        if p.is_file() and not any(part in ("bin", "obj") for part in p.parts)
    ]
    for p in sorted(files, key=lambda x: x.as_posix().lower()):
        rel = p.relative_to(plugin_dir).as_posix().encode()
        text = p.read_text(encoding="utf-8")
        if p.suffix == ".csproj":
            text = VER_TAG.sub(lambda m: f"<{m.group(1)}>0.0.0.0</{m.group(1)}>", text)
        h.update(rel)
        h.update(b"\0")
        h.update(text.encode())
    return h.hexdigest()[:16]


def load_yaml_field(name: str) -> str:
    m = re.search(rf'^{name}:\s*"(.*)"\s*$', BUILD_YAML.read_text(), re.M)
    if not m:
        raise SystemExit(f"missing {name} in build.yaml")
    return m.group(1)


def stamp(version: str, changelog: str) -> None:
    csproj = VER_TAG.sub(lambda m: f"<{m.group(1)}>{version}</{m.group(1)}>", CSPROJ.read_text())
    CSPROJ.write_text(csproj)
    y = BUILD_YAML.read_text()
    y = re.sub(r'^version: ".*"$', f"version: {json.dumps(version)}", y, count=1, flags=re.M)
    y = re.sub(r'^changelog: ".*"$', f"changelog: {json.dumps(changelog)}", y, count=1, flags=re.M)
    BUILD_YAML.write_text(y)


def zip_name(version: str) -> str:
    abi = load_yaml_field("targetAbi").rsplit(".", 1)[0]
    return f"Jellyfin.Plugin.SyncPlayRefined_{abi}_{version}.zip"


def pack(
    *,
    dll: Path,
    version: str,
    changelog: str,
    timestamp: str,
    plugin_dir: Path,
    dest_dir: Path,
) -> tuple[Path, str]:
    dest_dir.mkdir(parents=True, exist_ok=True)
    meta = {
        "guid": load_yaml_field("guid"),
        "name": load_yaml_field("name"),
        "description": load_yaml_field("description"),
        "overview": load_yaml_field("overview"),
        "owner": load_yaml_field("owner"),
        "category": load_yaml_field("category"),
        "version": version,
        "changelog": changelog,
        "targetAbi": load_yaml_field("targetAbi"),
        "timestamp": timestamp,
        "sourceHash": source_hash(plugin_dir),
    }
    zpath = dest_dir / zip_name(version)
    with zipfile.ZipFile(zpath, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(dll, dll.name)
        zf.writestr("meta.json", json.dumps(meta, indent=2) + "\n")
    checksum = hashlib.md5(zpath.read_bytes()).hexdigest().upper()
    return zpath, checksum


def zip_source_hash(zpath: Path) -> str | None:
    with zipfile.ZipFile(zpath) as zf:
        meta = json.loads(zf.read("meta.json"))
    return meta.get("sourceHash")


def prepend_manifest(version: str, changelog: str, timestamp: str, checksum: str, zpath: Path) -> None:
    repo = os.environ.get("GITHUB_REPOSITORY", "noxaur/syncplay-refined")
    url = f"https://raw.githubusercontent.com/{repo}/main/dist/{zpath.name}"
    catalog = json.loads(MANIFEST.read_text())
    entry = {
        "version": version,
        "changelog": changelog,
        "targetAbi": load_yaml_field("targetAbi"),
        "sourceUrl": url,
        "checksum": checksum,
        "timestamp": timestamp,
    }
    catalog[0]["versions"].insert(0, entry)
    MANIFEST.write_text(json.dumps(catalog, indent=2) + "\n")


def newest_zip() -> Path | None:
    if not MANIFEST.exists():
        return None
    versions = json.loads(MANIFEST.read_text())[0]["versions"]
    if not versions:
        return None
    zpath = DIST / Path(versions[0]["sourceUrl"]).name
    return zpath if zpath.exists() else None


def dotnet_build(csproj: Path, version: str) -> Path:
    subprocess.check_call(
        [
            "dotnet",
            "build",
            str(csproj),
            "-c",
            "Release",
            f"-p:AssemblyVersion={version}",
            f"-p:FileVersion={version}",
        ]
    )
    return csproj.parent / "bin/Release/net9.0" / (csproj.stem + ".dll")


def existing_versions() -> list[str]:
    return [v["version"] for v in json.loads(MANIFEST.read_text())[0]["versions"]]


def self_test() -> None:
    day = datetime(2026, 8, 23, tzinfo=timezone.utc)
    assert next_calver([], day) == "2026.8.23.0"
    assert next_calver(["2026.8.23.0", "2026.8.23.1"], day) == "2026.8.23.2"
    assert next_calver(["2026.8.22.9", "1.0.0.1"], day) == "2026.8.23.0"
    jan = datetime(2026, 1, 5, tzinfo=timezone.utc)
    assert next_calver(["2026.1.5.0"], jan) == "2026.1.5.1"
    print("ok")


def cmd_pack(args: argparse.Namespace) -> None:
    zpath, checksum = pack(
        dll=Path(args.dll),
        version=args.version,
        changelog=args.changelog,
        timestamp=args.timestamp,
        plugin_dir=Path(args.plugin_dir),
        dest_dir=DIST,
    )
    print(f"{zpath.name} {checksum}")


def cmd_release(args: argparse.Namespace) -> None:
    current = source_hash(PLUGIN)
    prev = newest_zip()
    if prev is not None and zip_source_hash(prev) == current:
        print("unchanged")
        return
    now = datetime.now(timezone.utc)
    seen = existing_versions()
    version = args.version or next_calver(seen, now)
    if version in seen:
        raise SystemExit(f"{version} already in catalog")
    dest = DIST / zip_name(version)
    if dest.exists():
        raise SystemExit(f"{dest.name} already exists")
    changelog = args.changelog
    timestamp = args.timestamp or now.strftime("%Y-%m-%dT%H:%M:%SZ")
    stamp(version, changelog)
    dll = dotnet_build(CSPROJ, version)
    zpath, checksum = pack(
        dll=dll,
        version=version,
        changelog=changelog,
        timestamp=timestamp,
        plugin_dir=PLUGIN,
        dest_dir=DIST,
    )
    prepend_manifest(version, changelog, timestamp, checksum, zpath)
    print(version)


def main() -> None:
    p = argparse.ArgumentParser()
    sub = p.add_subparsers(dest="cmd", required=True)

    t = sub.add_parser("self-test")
    t.set_defaults(func=lambda _: self_test())

    pk = sub.add_parser("pack")
    pk.add_argument("--dll", required=True)
    pk.add_argument("--plugin-dir", required=True)
    pk.add_argument("--version", required=True)
    pk.add_argument("--changelog", required=True)
    pk.add_argument("--timestamp", required=True)
    pk.set_defaults(func=cmd_pack)

    rel = sub.add_parser("release")
    rel.add_argument("--changelog", required=True)
    rel.add_argument("--version")
    rel.add_argument("--timestamp")
    rel.set_defaults(func=cmd_release)

    args = p.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
