# Active Context

Current focus:
- Stabilize cross-platform screen capture (Windows native; macOS/Linux CLI fallbacks added).
- Ensure Tesseract integration is configurable via `tessdata` path and language settings.

Recent changes:
- Added `ISettingsService` and a `SettingsService` persisting to `%AppData%/TranslatorOCR/settings.json`.
- Added UI controls for tessdata path and language, plus a Browse button.
- Implemented macOS and Linux CLI-based capture fallbacks and updated DI registration.
- Added an integration test that generates a simple image and runs OCR when `tessdata` is available.

Next steps (short-term):
- Replace `OpenFolderDialog` usage with `StorageProvider` in Avalonia (address obsolete API warning).
- Add packaging docs for native Tesseract dependencies per platform.
