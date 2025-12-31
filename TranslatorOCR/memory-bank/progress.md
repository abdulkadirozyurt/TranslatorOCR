# Progress

What works:
- Project scaffold, Avalonia UI, region selector, AppController, DI host.
- Windows screen capture implemented; macOS/Linux CLI fallbacks added.
- Tesseract OCR integrated with ImageSharp preprocessing.
- Settings persistence and UI for `tessdata` path + language.
- Unit tests for `AppController`; integration test that runs when `tessdata` is present.

Open / pending items:
- Replace obsolete `OpenFolderDialog` with `StorageProvider` API.
- Add native macOS/Linux capture implementations (native APIs) for better performance.
- Packaging and clear instructions for native Tesseract libraries across platforms.
- More integration tests and performance profiling when running against actual games.
