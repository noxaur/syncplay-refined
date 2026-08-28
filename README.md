# SyncPlay Refined

Jellyfin 10.11 plugin. It puts Copy invite link in the SyncPlay menu, under Resume local playback. Open the link and you join that group. Web client only. You still need an account on the same server.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install SyncPlay Refined and restart. Put [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) on the server first if you can. That plugin injects the script in memory. Direct writes to `index.html` fail on a lot of Docker setups.

Sideload:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Zips live in `dist/`. The catalog is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after saving.

- **Only load for authenticated users** (default on). The client script waits until someone is signed in.
- **Replace Leave group with Disband group** (default off). Leave group becomes Disband group and kicks everyone. Reload the web client after saving.
- **Enable experimental features** (default off). Turns on unfinished client work for every web client on this server. Reload after saving. WIP code calls `SyncPlayRefinedDev.enabled()` or `SyncPlayRefinedDev.feature('name')`.
- **Auto** (default). Uses File Transformation and JavaScript Injector when they are installed. Otherwise it patches `index.html` on disk.
- **File Transformation.** In-memory `index.html` transform.
- **JavaScript Injector.** Registers a loader with that plugin.
- **Direct index.html.** Writes a `<script>` tag into jellyfin-web's `index.html`. Skip this on Docker unless the web dir is writable.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`, so the browser fetches the new `client.js`. Settings come from `/SyncPlayRefined/flags` on load.

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link (under Resume / Stop local playback)
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N` (UTC date, then a same-day counter). A push to `main` packs a new catalog entry. Other branches only compile. Prepend to `versions[]` in `manifest.json`. Do not drop old entries. `checksum` is the uppercase MD5 of the zip.
