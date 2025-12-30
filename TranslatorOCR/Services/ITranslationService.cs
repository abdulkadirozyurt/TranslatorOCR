using System.Threading;
using System.Threading.Tasks;

namespace TranslatorOCR.Services;

public interface ITranslationService
{
    /// <summary>
    /// Translate the input text to the requested target language.
    /// </summary>
    Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken);
}
