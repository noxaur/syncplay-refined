# SyncPlay Refined

Jellyfin 10.11 **web** plugin. Adds **Copy invite link** to the SyncPlay menu (under Resume / Stop local playback). Recipients who open the link join that group after login. Optional: **Leave group** becomes **Disband group** and kicks everyone. All clients need an account on the same server.

## Install

Dashboard → Plugins → Repositories. Add:

```text
https://raw.githubusercontent.com/noxaur/syncplay-refined/refs/heads/main/manifest.json
```

Install **SyncPlay Refined** and restart.

Install [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) first if you can. Auto then injects the script in memory. Without it, the plugin may write a `<script>` tag into `web/index.html`, which Docker often cannot.

Sideload: build `Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj` (`Release`), copy the DLL to `<jellyfin-config>/plugins/SyncPlayRefined/`, restart. Zips are in `dist/`.

## Use

1. Join or create a SyncPlay group.
2. Open the SyncPlay menu → **Copy invite link**.
3. Send the URL. After login, the other client joins and resumes group playback. The link is the current page with `?syncplayGroup=` on the query string.

## Config

Dashboard → Plugins → SyncPlay Refined. After a plugin update, hard-refresh the web client once.

- **Only load for authenticated users** (default on): wait until someone is signed in. Restart after saving.
- **Replace Leave group with Disband group** (default off): Leave becomes Disband; it removes everyone. Reload the web client.
- **Enable experimental features** (default off): unfinished client features, every web client on this server. Reload the web client.
- **Script injection method** (default Auto): File Transformation and JavaScript Injector when present, else a direct `index.html` patch. Restart after saving.

Forced methods: File Transformation (in-memory), JavaScript Injector (registers a loader), Direct index.html (writes the file).
