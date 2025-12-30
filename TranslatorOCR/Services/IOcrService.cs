using System.Threading;
using System.Threading.Tasks;

namespace TranslatorOCR.Services;

public interface IOcrService
{
    /// <summary>
    /// Extract text from raw image bytes. Returns null or empty if nothing recognized.
    /// </summary>
    Task<string?> ReadTextAsync(byte[] imageBytes, CancellationToken cancellationToken);
}
