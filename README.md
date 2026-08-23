# SyncPlay Refined

Jellyfin 10.11 plugin. Adds **Copy invite link** under **Resume local playback** in the SyncPlay menu. Recipients who open the link join that group automatically. Web client only; everyone needs a logged-in account on the same server.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install **SyncPlay Refined** and restart. Install [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) first if you can. It injects the script without writing `index.html`, which Docker often cannot.

Sideload:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Zips live in `dist/`. The catalog is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after saving.

- **Auto** (default): File Transformation if loaded, else JavaScript Injector if loaded, else a direct `index.html` patch
- **File Transformation**: in-memory `index.html` transform
- **JavaScript Injector**: registers a loader with that plugin
- **Direct index.html**: writes a `<script>` tag into jellyfin-web's `index.html`. Docker often cannot write that file

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link (under Resume / Stop local playback)
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N` (UTC date, then a same-day counter). A push to `main` packs a new catalog entry; other branches only compile. Prepend to `versions[]` in `manifest.json`. Do not drop old entries. `checksum` is the uppercase MD5 of the zip.
