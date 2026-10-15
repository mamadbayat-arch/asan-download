> **نسخهٔ جدید بر پایه Gopeed:** سورس در `gopeed/` و راهنمای ساخت ویندوز در [GOPEED-FORK.md](GOPEED-FORK.md) است. با `BuildGopeed.ps1` ساخته می‌شود. توضیحات زیر مربوط به نسخهٔ قبلی C# هستند.

# آسان دانلود · Asan Download

Persian Windows download manager with a browser extension and a small self-hosted website/admin service.

- Desktop app: **1.8.2**, C# / Windows Forms / .NET Framework.
- Chrome and Microsoft Edge extension: **1.7.1**, Manifest V3.
- Website: https://downloader.hamtavar.ir/

## Features

- Up to 16 download connections, adaptive chunk scheduling, retries, pause and resume.
- One default download folder, automatic filenames and collision-safe destinations.
- Persistent completed-download history with independent records, atomic writes and recovery backups.
- Persian completion dates, active download duration and average speed; clickable MB/s ↔ Mb/s display.
- Confirmation before browser downloads; automatic interception requires a recent real click or keyboard action. Browser-startup downloads do not generate unsolicited prompts.
- Light/dark interface, embedded Kalameh fonts and Hamtavar branding.
- Windows startup option, browser native messaging, signed automatic updates, centrally managed advertisements and issue reporting.

## Build and run

Use Windows 10/11 with .NET Framework 4.8, PowerShell and Node.js 22 or later. The build uses the C# compiler included with Windows and does not require Visual Studio.

```powershell
./Build.ps1
./MehrDownload.exe
```

To install for the current Windows user, run `Install.cmd` after building. The native messaging host and extension files are installed with the app.

In Chrome/Edge, enable Developer mode on the extensions page and choose **Load unpacked** for the installed `BrowserExtension` folder. After updating extension files, click **Reload** and refresh the download webpage. The extension folder also includes a Persian illustrated guide.

Downloads requiring browser cookies or login, streaming media and webpage URLs are not supported as direct downloads. Download speed remains subject to the network and source server. Resume requires compatible server range/validator support.

## Tests

```powershell
./Test.ps1
./TestUpdates.ps1
node BrowserExtension/test-background.mjs
node server/test.mjs
```

These cover download integrity, pause/resume, history persistence and recovery, Persian dates, UI behavior, signed-update validation and rollback, browser consent/fallback and unwanted startup requests.

`AdaptiveTests.cs` and `AdaptiveServer.mjs` provide the controlled slow-connection scheduling benchmark. `NativeSmoke.mjs ping` checks the installed native host without starting a download.

## Website and administration

```powershell
cd server
# Set ADMIN_PASSWORD to a unique password of at least 12 characters.
# Optional: ADMIN_USERNAME, DATA_DIR, PUBLIC_ORIGIN, PORT, NODE_ENV.
node setup-admin.mjs
npm start
```

The default port is 4080. Set `PUBLIC_ORIGIN` to the deployed HTTPS origin. Admin pages are `/admin` and `/admin/reports`; runtime data is stored in `server/data` unless `DATA_DIR` is configured. Supply secrets through the environment; do not commit them.

## Release builds

`BuildRelease.ps1` creates the Windows bundle, extension ZIP, update executable and signed release manifest. It requires the existing private publisher key `UPDATE-SIGNING-KEY.xml`, which is intentionally excluded from Git. `Assets/UpdatePublicKey.xml` is the public verification key used by installed clients. Keep the matching private key in secure backup; a new key will not sign updates accepted by existing clients.

```powershell
./BuildRelease.ps1
```

Release files are generated in `server/public/downloads` and the manifest in `server/public/release.json`. Generated executables, archives, signing secrets, credentials, local histories and test output are excluded from the repository.

## Source layout

| Path | Purpose |
| --- | --- |
| `App.cs`, `Glass.cs` | Windows interface and installer |
| `Engine.cs` | Download engine and resume state |
| `HistoryStore.cs` | Durable completed-download records |
| `Updates.cs` | Signed updates and rollback |
| `Integration.cs`, `NativeHost.cs` | Windows/browser integration |
| `BrowserExtension/` | Extension, guide and regression tests |
| `Ads.cs`, `Reports.cs` | Central ads and issue reporting |
| `server/` | Node.js website, admin and public APIs |
| `Assets/` | Embedded fonts, branding and public verification key |

Brand assets and bundled fonts retain their existing rights; this repository does not grant a new license for them.
