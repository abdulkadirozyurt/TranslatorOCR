using System;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Translation;

/// <summary>
/// Placeholder removed: mock translators are not allowed.
/// If you see this class, replace usages with a real translator implementation.
/// </summary>
public class MockTranslationService : ITranslationService
{
    public Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("MockTranslationService is removed. Use GoogleTranslateService or a real API client instead.");
    }
}
