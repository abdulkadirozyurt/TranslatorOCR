using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Translation;

/// <summary>
/// Lightweight mock translator for development and tests. Replace with API client.
/// </summary>
public class MockTranslationService : ITranslationService
{
    public Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text)) return Task.FromResult<string?>(null);
        return Task.FromResult<string?>($"{text} [{targetLanguage}]");
    }
}
