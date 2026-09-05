# SyncPlay Refined

Jellyfin 10.11 plugin for the web client. Copy invite link sits in the SyncPlay menu, under Resume local playback. Open that URL after login and you join the group. Native apps do not get this. Everyone still needs an account on the same server.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install SyncPlay Refined and restart. Install [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) first if you run Docker. It injects the script in memory. Direct `index.html` writes need a writable web dir, which Docker often does not give you.

Sideload:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Zips live in `dist/`. The catalog is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after saving.

**Only load for authenticated users.** On by default. The script waits until someone is signed in.

**Replace Leave group with Disband group.** Off by default. Leave group becomes Disband group and kicks the whole group. Reload the web client after you save.

**Enable experimental features.** Off by default. Unfinished client work, on for every web client on this server. Reload after saving. WIP code should call `SyncPlayRefinedDev.enabled()` or `SyncPlayRefinedDev.feature('name')`.

**Script injection.** Auto is the default and the one you want. It uses File Transformation and JavaScript Injector when those plugins exist, then falls back to writing a `<script>` tag into jellyfin-web's `index.html`. File Transformation rewrites `index.html` in memory. JavaScript Injector registers a loader with that plugin. Pick Direct index.html only if you know the web dir is writable.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`, so a stale `client.js` should drop. Settings also reload from `/SyncPlayRefined/flags` on page load.

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link, under Resume local playback or Stop local playback
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N`. UTC date, then a same-day counter. A push to `main` packs a new catalog entry. Other branches only compile. Prepend to `versions[]` in `manifest.json`. Do not drop old entries. `checksum` is the uppercase MD5 of the zip.
