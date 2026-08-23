#!/usr/bin/env python3
"""Mirrors Disband.MemberNames and the Leave URL rewrite. Keep in sync with Disband.cs and Web/client.js."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CLIENT = ROOT / "Jellyfin.Plugin.SyncPlayRefined" / "Web" / "client.js"
DISBAND = ROOT / "Jellyfin.Plugin.SyncPlayRefined" / "Disband.cs"


def member_names(groups: list[list[str]], user_name: str | None) -> set[str]:
    names: dict[str, str] = {}
    if not user_name:
        return set()
    needle = user_name.lower()
    for participants in groups:
        if not any(p.lower() == needle for p in participants):
            continue
        for p in participants:
            names.setdefault(p.lower(), p)
    return set(names.values())


def rewrite_leave_url(url: str | None, enabled: bool, get_url=None) -> str | None:
    if not enabled or url is None:
        return url
    if not re.search(r"/SyncPlay/Leave(?:\?|$)", url, re.I):
        return url
    if get_url:
        return get_url("SyncPlayRefined/Disband")
    return re.sub(r"/SyncPlay/Leave", "/SyncPlayRefined/Disband", url, count=1, flags=re.I)


def main() -> None:
    assert member_names([["Alice", "Bob"], ["Eve"]], "alice") == {"Alice", "Bob"}
    assert member_names([["Alice", "Bob"]], "carol") == set()
    assert member_names([["Alice"], ["Alice", "Dan"]], "ALICE") == {"Alice", "Dan"}
    assert member_names([], "Alice") == set()
    assert member_names([["Alice"]], None) == set()

    assert rewrite_leave_url("/SyncPlay/Leave", True) == "/SyncPlayRefined/Disband"
    assert rewrite_leave_url("http://h/SyncPlay/Leave?x=1", True) == "http://h/SyncPlayRefined/Disband?x=1"
    assert rewrite_leave_url("/SyncPlay/Leave", False) == "/SyncPlay/Leave"
    assert rewrite_leave_url("/SyncPlay/List", True) == "/SyncPlay/List"
    assert rewrite_leave_url("/SyncPlay/Leave", True, lambda _: "/j/SyncPlayRefined/Disband") == "/j/SyncPlayRefined/Disband"

    js = CLIENT.read_text()
    assert r"/\/SyncPlay\/Leave(?:\?|$)/i" in js
    assert "Disband group" in js
    assert "SyncPlayRefined/Disband" in js

    cs = DISBAND.read_text()
    assert "OrdinalIgnoreCase" in cs
    assert "Participants" in cs
    print("ok")


if __name__ == "__main__":
    main()
