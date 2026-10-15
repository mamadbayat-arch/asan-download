# Asan Download / آسان دانلود

This is a modified distribution of **Gopeed v1.9.3** by GopeedLab and its contributors.

Upstream: https://github.com/GopeedLab/gopeed

Pinned upstream commit: `a5cd53f94c18ac65add684b1113fa5f0b47cc4da`.

Upstream copyright notices and the full GNU GPL version 3 in `LICENSE` are preserved. Changes to this Gopeed-derived application are distributed under GPL version 3. Source for this modified application must accompany or be made available with any distributed binary. No endorsement by GopeedLab is implied.

Modifications by MehrNoor, October 2026: Asan branding, Persian defaults and theme, a separate persistent Windows profile, access to legacy download history, explicit browser-download confirmation, a compatible browser host with independent identity, and Windows build packaging. Brand names and logos identify this fork and do not change upstream attribution.

Application source: `gopeed/` in the `codex/gopeed-based` branch of https://github.com/mamadbayat-arch/asan-download . The corresponding source archive is included with build artifacts. Dependencies retain their respective licenses.

The original C# application in the repository root is a separate application and is not silently replaced by this preview.

Default download safety: downloaded archives are kept as files; automatic extraction and deletion are disabled on the initial migration. A later explicit user preference can change these settings. Browser startup and history import never create download tasks.
