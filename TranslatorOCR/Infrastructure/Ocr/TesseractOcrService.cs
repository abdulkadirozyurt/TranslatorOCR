using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Ocr;

/// <summary>
/// Stub OCR service. Wire up Tesseract or other engine for production.
/// </summary>
public class TesseractOcrService : IOcrService
{
    public Task<string?> ReadTextAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        // TODO: integrate Tesseract.NET or other OCR engine. Return sample placeholder for now.
        return Task.FromResult<string?>(string.Empty);
    }
}
