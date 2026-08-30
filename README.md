# SyncPlay Refined

Jellyfin 10.11 plugin. Copy invite link sits in the SyncPlay menu, under Resume local playback. The other person opens that URL, logs in, and joins the group. Web client only. Everyone needs an account on the same server.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install SyncPlay Refined and restart.

Put [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) on the server first if you can. It injects the script in memory. The fallback writes `index.html`, and Docker often cannot.

Sideload from a build:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Release zips are in `dist/`. The catalog is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after saving.

**Only load for authenticated users.** On by default. The client script waits until a user is signed in.

**Replace Leave group with Disband group.** Off by default. Leave group becomes Disband group and removes everyone. Reload the web client after you save.

**Enable experimental features.** Off by default. Unfinished client work, on every web client on this server. Reload after saving. WIP code checks `SyncPlayRefinedDev.enabled()` or `SyncPlayRefinedDev.feature('name')`.

**Auto.** Default. Tries File Transformation, then JavaScript Injector, then a direct `index.html` patch.

**File Transformation.** Transforms `index.html` in memory.

**JavaScript Injector.** Registers a loader with that plugin.

**Direct index.html.** Writes a `<script>` tag into jellyfin-web's `index.html`. Docker often cannot write that file.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`, so the browser does not keep an old `client.js`. Settings also load from `/SyncPlayRefined/flags` on each page load.

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link (under Resume / Stop local playback)
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N` (UTC date, then a same-day counter). A push to `main` packs a new catalog entry. Other branches only compile. Prepend to `versions[]` in `manifest.json`. Keep old entries. `checksum` is the uppercase MD5 of the zip.
