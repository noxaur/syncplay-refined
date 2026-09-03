# SyncPlay Refined

Jellyfin 10.11 plugin. Adds **Copy invite link** to the in-group SyncPlay menu (after Resume or Stop local playback). Recipients who open the link join that group after login. Web client only; everyone needs an account on the same server.

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

Dashboard → Plugins → SyncPlay Refined.

- **Only load for authenticated users** (default on): the client script waits until a user is signed in. Restart after saving.
- **Replace Leave group with Disband group** (default off): Leave group becomes Disband group and removes everyone. Reload the web client after saving.
- **Enable experimental features** (default off): unfinished client features, every web client on this server. Reload the web client after saving.
- **Script injection method** (default Auto): File Transformation and JavaScript Injector when present, else a direct `index.html` patch. You can force File Transformation, JavaScript Injector, or Direct index.html. Restart after saving. Direct writes a `<script>` tag into jellyfin-web's `index.html`; Docker often cannot.

After a plugin update, hard-refresh the web client once. The script URL is `/SyncPlayRefined/script?v=<plugin-version>`. Settings also refresh from `/SyncPlayRefined/flags` on load.

## Use

1. Join or create a SyncPlay group
2. Open the SyncPlay menu
3. Copy invite link
4. Send the URL. After login, the other client joins the group and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Releases

Versions are `YYYY.M.D.N` (UTC date, then a same-day counter). A push to `main` prepends a catalog entry. Keep old `manifest.json` versions.
