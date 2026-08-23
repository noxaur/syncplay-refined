# SyncPlay Refined

Jellyfin 10.11 plugin. Adds **Copy invite link** under **Resume local playback** in the SyncPlay menu. Recipients who open the link join that group automatically.

Web client only. Everyone still needs a logged-in account on the same server. Native apps are out of scope.

## Install

This branch is the test catalog. In Jellyfin: Dashboard → Plugins → Repositories → add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/cursor/syncplay-invite-link/manifest.json
```

Then install **SyncPlay Refined** from the catalog and restart. Install [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) first if you can. It injects the script without touching `index.html` on disk, which is the usual Docker failure.

Sideload instead:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

The test zip on this branch is `dist/Jellyfin.Plugin.SyncPlayRefined_10.11.0_1.0.0.0.zip`.

## Config

Dashboard → Plugins → SyncPlay Refined. One setting: how the client script gets into jellyfin-web. Restart after saving.

- **Auto** (default): File Transformation if loaded, else JavaScript Injector if loaded, else a direct `index.html` patch
- **File Transformation**: in-memory `index.html` transform
- **JavaScript Injector**: registers a loader with that plugin
- **Direct index.html**: writes a `<script>` tag into jellyfin-web's `index.html`. Docker often cannot write that file

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link (under Resume / Stop local playback)
4. Send the URL. After login, the other client joins the group and resumes group playback

The link looks like:

```
https://host:8096/web/index.html?syncplayGroup=GUID#!/home
```

## Releases

`manifest.json` is the catalog history. Newest version first. Prepend on bump. Do not rewrite or drop old entries. Keep each changelog to one line.

This test branch's `sourceUrl` points at `dist/Jellyfin.Plugin.SyncPlayRefined_10.11.0_1.0.0.0.zip` on `cursor/syncplay-invite-link`. Fill `checksum` with the MD5 of that zip. Never fake hashes.

Version must stay in lockstep in `Jellyfin.Plugin.SyncPlayRefined.csproj`, `build.yaml`, and the new `manifest.json` entry.
