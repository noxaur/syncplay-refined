# AGENTS.md

## Cursor Cloud specific instructions

SyncPlay Refined is a **Jellyfin 10.11 server plugin** (C#, `net9.0`). There is no standalone app: the plugin's DLL is loaded by a Jellyfin server, and its client-side feature ("Copy invite link" in the SyncPlay menu, plus auto-join from a `?syncplayGroup=` URL) is injected into the Jellyfin web client. The startup update script installs the .NET 9 SDK and runs `dotnet restore`.

### Build / test / lint

- Test (fast, no SDK needed): `python3 scripts/release.py self-test` — unit checks for the CalVer logic in the release packer.
- Build: `dotnet build Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj -c Release`. These two are the only CI checks (see `.github/workflows/build.yml`).
- Lint: there is no dedicated linter. The project builds with `Nullable` enabled and CI expects 0 warnings, so treat build warnings as lint failures. `dotnet format Jellyfin.Plugin.SyncPlayRefined/Jellyfin.Plugin.SyncPlayRefined.csproj --verify-no-changes` is available and passes on a clean tree.
- `dotnet` installs to `/usr/share/dotnet` with a symlink at `/usr/local/bin/dotnet`, so it is already on `PATH`.

### Releases (do not run casually)

`scripts/release.py release` bumps the version, builds, zips into `dist/`, and prepends a `manifest.json` catalog entry. This is meant for CI on pushes to `main` only. Versions are `YYYY.M.D.N` (UTC). Don't hand-run it unless you are intentionally cutting a release.

### Running Jellyfin to test the plugin end-to-end

The `jellyfin` package (server + web + ffmpeg) is installed but **systemd is not PID 1** in this container, so `systemctl start jellyfin` does not work. Run the server manually as your own user with writable data/web dirs instead (the default `/var/lib/jellyfin` and `/usr/share/jellyfin/web` are root-owned):

1. Build the plugin (above), then copy `Jellyfin.Plugin.SyncPlayRefined/bin/Release/net9.0/Jellyfin.Plugin.SyncPlayRefined.dll` into `<datadir>/plugins/SyncPlayRefined/`.
2. Make a writable copy of the web client (`cp -r /usr/share/jellyfin/web <somewhere-you-own>`). This matters because with no File Transformation / JavaScript Injector plugin present, SyncPlay Refined falls back to the **Direct index.html** injection method, which writes a `<script>` tag into `web/index.html` and needs write access.
3. Run: `/usr/lib/jellyfin/bin/jellyfin --webdir=<web-copy> --ffmpeg=/usr/lib/jellyfin-ffmpeg/ffmpeg --datadir <datadir> --configdir <cfgdir> --logdir <logdir> --cachedir <cachedir>`. It listens on `0.0.0.0:8096`.
4. Confirm the plugin loaded: the log prints `SyncPlay Refined injection method: ...` and `Wrote script tag to .../index.html`; `curl http://127.0.0.1:8096/SyncPlayRefined/script` returns the client JS.

To exercise the feature in the browser: finish the first-run wizard (create an admin user; no media library is required), log in, click the SyncPlay icon in the header, "New group", then reopen the SyncPlay menu — "Copy invite link" appears there. Copying yields the current page URL with `?syncplayGroup=<groupId>` appended.
