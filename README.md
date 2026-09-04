# SyncPlay Refined

Jellyfin 10.11 plugin. Adds **Copy invite link** under **Resume local playback** in the SyncPlay menu. Open the link and you join that group. Web client only. You need a logged-in account on the same server.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install **SyncPlay Refined** and restart. Install [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) first if you run Docker. That plugin injects the script in memory. Writing `index.html` on disk often fails in Docker.

Sideload:

1. `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`
2. Copy `Jellyfin.Plugin.SyncPlayRefined.dll` to `<jellyfin-config>/plugins/SyncPlayRefined/`
3. Restart Jellyfin

Release zips sit in `dist/`. The catalog is `manifest.json`.

## Config

Dashboard → Plugins → SyncPlay Refined. Restart after you save.

- **Only load for authenticated users.** On by default. The client script waits until a user is signed in.
- **Replace Leave group with Disband group.** Off by default. The Leave group button becomes Disband group and removes everyone. Reload the web client after you save.
- **Enable experimental features.** Off by default. Unfinished client features load on every web client on this server. Reload the web client after you save. Gate WIP with `SyncPlayRefinedDev.enabled()` or `SyncPlayRefinedDev.feature('name')`.
- **Auto.** Default. Uses File Transformation or JavaScript Injector when either is present. Otherwise patches `index.html` on disk.
- **File Transformation.** Transforms `index.html` in memory.
- **JavaScript Injector.** Registers a loader with that plugin.
- **Direct index.html.** Writes a `<script>` tag into jellyfin-web's `index.html`. Docker often cannot write that file.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`, so a cached `client.js` does not survive the version bump. Settings come from `/SyncPlayRefined/flags` on load.

## Use

1. Join or create a SyncPlay group.
2. Open the SyncPlay menu.
3. Copy invite link. It sits under Resume local playback and Stop local playback.
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N`. UTC date, then a same-day counter. A push to `main` prepends a catalog entry. Other branches only compile. Prepend to `versions[]` in `manifest.json`. Keep old entries. `checksum` is the uppercase MD5 of the zip.
