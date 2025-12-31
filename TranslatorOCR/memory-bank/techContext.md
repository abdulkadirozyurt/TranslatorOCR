# Tech Context

Languages and frameworks:
- .NET 9.0, C# 11, Avalonia UI 11.x for cross-platform desktop UI.
- Tesseract (NuGet `Tesseract`) for OCR native bindings.
- SixLabors.ImageSharp for preprocessing images.

Dependencies and native requirements:
- Tesseract native runtime and `tessdata` traineddata files are required at runtime. `tessdata` may be placed in AppContext.BaseDirectory/tessdata or configured via Settings.
- Windows: `System.Drawing.Common` is used for Windows capture helper; macOS/Linux use CLI fallbacks (`screencapture`, `import`, `grim`) as interim solutions.

Development setup:
- `dotnet build` and `dotnet test` verify compile and unit/integration tests (integration test skips/returns early when native deps are missing).
- Tests: xUnit test project targeting net10.0.
