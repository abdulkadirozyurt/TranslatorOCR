using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Translation
{
    /// <summary>
    /// Lightweight translator using the unofficial Google Translate HTTP endpoint.
    /// This does not require an API key but may be rate-limited or change.
    /// It's intended as a pragmatic replacement for the Python `deep_translator` usage.
    /// </summary>
    public class GoogleTranslateService : ITranslationService
    {
        private readonly HttpClient _http = new HttpClient();

        public async Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (string.IsNullOrWhiteSpace(targetLanguage)) targetLanguage = "en";

            try
            {
                // Build URL for unofficial translate endpoint
                var q = Uri.EscapeDataString(text);
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={Uri.EscapeDataString(targetLanguage)}&dt=t&q={q}";

                using var resp = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;

                var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

                // The response is a nested array. We extract concatenated translated segments.
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
                var sb = new StringBuilder();
                var top = doc.RootElement[0];
                if (top.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in top.EnumerateArray())
                    {
                        if (part.ValueKind == JsonValueKind.Array && part.GetArrayLength() > 0)
                        {
                            var seg = part[0].GetString();
                            if (!string.IsNullOrEmpty(seg)) sb.Append(seg);
                        }
                    }
                }

                var result = sb.ToString().Trim();
                return string.IsNullOrWhiteSpace(result) ? null : result;
            }
            catch
            {
                return null;
            }
        }
    }
}
