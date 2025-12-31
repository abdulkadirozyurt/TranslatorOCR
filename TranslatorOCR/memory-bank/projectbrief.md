# Project Brief

One-sentence summary: Build a cross-platform, low-latency OCR-based game subtitle translator (TranslatorOCR) using .NET/Avalonia, Tesseract OCR, and a modular, testable architecture following SOLID and Clean Architecture principles.

Primary goals:
- Replace an existing Python translator with a responsive, maintainable .NET application.
- Provide cross-platform screen capture, OCR (Tesseract), translation, and non-intrusive overlay display.
- Follow TDD and SOLID, keep domain logic testable and independent of platform-specific implementations.

Constraints and non-goals:
- Native packaging and installers are out of scope for initial implementation (document native deps instead).
- The app should be lightweight; avoid heavy background processing; tune OCR frequency and preprocessing.
