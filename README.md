# SyncPlay Refined

A Jellyfin 10.11 plugin for the web client. It puts Copy invite link under Resume local playback in the SyncPlay menu. Open that URL while logged into the same server and you join the group. Native apps get nothing from this. Guests get nothing either. Everyone has to have an account.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install SyncPlay Refined and restart.

Put [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) on the server first if you can. That plugin injects the script in memory. Direct writes to `index.html` fail on a lot of Docker setups, and Auto will try them anyway if nothing else is installed.

To build and drop the DLL in by hand:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Release zips sit in `dist/`. The plugin catalog is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after saving.

**Only load for authenticated users.** On by default. The client script waits until someone is signed in.

**Replace Leave group with Disband group.** Off by default. Leave group becomes Disband group and kicks everyone, not just you. Reload the web client after you save this.

**Enable experimental features.** Off by default. Turns on unfinished client work for every web client on this server. Reload after saving. From the console, `SyncPlayRefinedDev.enabled()` and `SyncPlayRefinedDev.feature('name')` tell you what is live.

**Script injection method.** Auto uses File Transformation and JavaScript Injector when those plugins exist, then falls back to patching `index.html`. File Transformation transforms `index.html` in memory. JavaScript Injector registers a loader with that plugin. Direct index.html writes a `<script>` tag into jellyfin-web's `index.html`, which Docker often cannot touch.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`, so a stale `client.js` should not survive that. Toggles also come from `/SyncPlayRefined/flags` on load.

## Use

Join or create a SyncPlay group, open the SyncPlay menu, copy the invite link under Resume / Stop local playback, and send it. After login, the other client joins and resumes group playback. The link is whatever page you were on, plus `?syncplayGroup=` on the query string.

## Releases

Versions look like `YYYY.M.D.N`. UTC date, then a same-day counter. A push to `main` prepends a new entry to `versions[]` in `manifest.json`. Other branches only compile. Keep old entries. `checksum` is the uppercase MD5 of the zip.
