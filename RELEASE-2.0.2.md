# آسان دانلود ۲.۰.۲

User-facing branding is Asan Download throughout the Windows app, browser extension and new landing page. Copyright, GPL source and upstream provenance remain available in the dedicated licenses screen and source distribution. Internal source module names and the existing profile path are retained for compatibility; the download database is not moved or deleted.

- Replace the old login title, tray donation link and notification identity. Use the product logo and consistent Material file icons. Windows engine bundle uses `asan_engine.dll`.
- Replace dormant upstream executable-updater code with product-owned release notification and explicit installation from the product site. This preview does not install updates automatically and never invokes the incompatible 1.x updater.
- Remove upstream extension-store requests and installation telemetry. Provide an independent catalogue endpoint, installed-extension management and manual repository installation. The new catalogue is initially empty: newly published third-party extensions may require a newer engine API than this fork supports.
- Handle catalogue failures without uncaught errors, retain loaded results, offer retry and prevent an older search response from replacing the latest search.
- Distinguish browser integration from engine extensions in the Persian UI and installation guide.
- Resolve a browser-origin link before acknowledging transfer. HTTP failures and slow origin responses return browser fallback without creating a delayed failed task or cancelling the browser prematurely.
- Fix timer reads before first start, repeated pause/start, paused idle time, completion/error freeze and concurrent access. Use monotonic elapsed time for valid duration and average speed.
- Show the app after interactive installation. Existing history, download folder and user preferences are preserved.

Validation: browser startup/confirmation/fallback regressions; race-enabled Go host and timer tests; Flutter Persian typography, translations, responsive light/dark layouts, history, statistics, confirmation and release/catalogue regression tests; packaged Windows end-to-end download with byte hash, host ping, resolve error, restart persistence, missing file history and stale startup links.

Known limits: browser cookies are not forwarded; authenticated downloads may need the browser. Browser bundle supports Chrome and Edge. No guarantee of speeds beyond the user's connection or origin-server limits. Native Windows screenshots from widget tests cover responsive layout; they are not full desktop interaction tests.
