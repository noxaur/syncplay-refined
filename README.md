# SyncPlay Refined

Jellyfin 10.11 plugin. Adds **Copy invite link** to the SyncPlay menu, after Resume or Stop local playback. Recipients who open the link join that group. Web client only; everyone needs a logged-in account on the same server.

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

- **Only load for authenticated users** (default on): the client script waits until a user is signed in
- **Replace Leave group with Disband group** (default off): Leave group becomes Disband group and removes everyone. Reload the web client after saving
- **Enable experimental features** (default off): unfinished client features, every web client on this server. Reload the web client after saving
- **Injection method** (default Auto): File Transformation and JavaScript Injector when present, else a direct `index.html` patch. Docker often cannot write that file

After a plugin update, hard-refresh the web client once.

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link
4. Send the URL. After login, the other client joins the group. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N` (UTC date, then a same-day counter). A push to `main` packs a new catalog entry; other branches only compile.
