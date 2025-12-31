# System Patterns

Architecture overview:
- Clean Architecture layers: UI (Avalonia) -> Application (`AppController`) -> Domain (services interfaces) -> Infrastructure (capture, OCR, translation, overlay).
- DI-host provides platform-specific implementations at startup.

Key patterns and decisions:
- Single `AppController` orchestrates capture→OCR→translate→overlay; unit-tested via fakes.
- Services expressed as interfaces: `IScreenCaptureService`, `IOcrService`, `ITranslationService`, `IOverlayService`, `ISettingsService`.
- Platform adaptations via DI registration in `Program.Main`.

Error handling and resiliency:
- AppController swallows non-fatal exceptions during the loop to maintain responsiveness; logging hooks can be added.
- Tesseract engine initialization validates `tessdata` path and surfaces a clear exception if missing.
