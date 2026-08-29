# SyncPlay Refined

Jellyfin 10.11 plugin. It puts "Copy invite link" under "Resume local playback" in the SyncPlay menu. Open the link while logged into the same server and you join that group. Web client only. No anonymous guests.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install SyncPlay Refined and restart.

Put [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) on the server first if you can. That plugin injects the script in memory. Direct writes to `index.html` fail on a lot of Docker setups.

Sideload from a local build:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Release zips are in `dist/`. The catalog file is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after you save.

**Only load for authenticated users.** Default on. The client script waits until someone is signed in.

**Replace Leave group with Disband group.** Default off. The Leave group button becomes Disband group and kicks everyone, not just you. Reload the web client after saving.

**Enable experimental features.** Default off. Unfinished client work, served to every web client on this server. Reload after saving. WIP code checks `SyncPlayRefinedDev.enabled()` or `SyncPlayRefinedDev.feature('name')`.

**Auto.** Default. Uses File Transformation or JavaScript Injector when either is installed. Otherwise it patches `index.html` on disk.

**File Transformation.** In-memory `index.html` transform.

**JavaScript Injector.** Registers a loader with that plugin.

**Direct index.html.** Writes a `<script>` tag into jellyfin-web's `index.html`. Docker images often mount that file read-only, so this one is the last resort.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`, so the browser does not keep an old `client.js`. Toggles also reload from `/SyncPlayRefined/flags` on page load.

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link (under Resume / Stop local playback)
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N` (UTC date, then a same-day counter). A push to `main` packs a new catalog entry. Other branches only compile. Prepend to `versions[]` in `manifest.json`. Leave old entries in place. `checksum` is the uppercase MD5 of the zip.
