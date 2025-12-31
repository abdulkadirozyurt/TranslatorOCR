using System;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Models;
using TranslatorOCR.Services;

namespace TranslatorOCR.Application;

/// <summary>
/// Coordinates capture → OCR → translate → overlay display. Keeps business logic out of UI.
/// Designed for testability: ProcessOnceAsync performs a single pipeline step (TDD-friendly).
/// </summary>
public class AppController
{
    private readonly IScreenCaptureService _capture;
    private readonly IOcrService _ocr;
    private readonly ITranslationService _translator;
    private readonly IOverlayService _overlay;

    public AppController(IScreenCaptureService capture, IOcrService ocr, ITranslationService translator, IOverlayService overlay)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _ocr = ocr ?? throw new ArgumentNullException(nameof(ocr));
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
    }

    /// <summary>
    /// Process a single capture→OCR→translate→display cycle. Returns true if text displayed.
    /// </summary>
    public async Task<bool> ProcessOnceAsync(Region region, string targetLanguage, CancellationToken cancellationToken)
    {
        if (region is null) throw new ArgumentNullException(nameof(region));
        if (string.IsNullOrWhiteSpace(targetLanguage)) targetLanguage = "en";

        // Temporarily hide overlay to avoid capturing it
        try
        {
            await _overlay.TempHideAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignore overlay temp-hide errors
        }

        var bytes = await _capture.CaptureRegionAsync(region, cancellationToken).ConfigureAwait(false);
        if (bytes == null || bytes.Length == 0)
        {
            await _overlay.HideAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        var text = await _ocr.ReadTextAsync(bytes, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text))
        {
            await _overlay.HideAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        var translated = await _translator.TranslateAsync(text, targetLanguage, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(translated)) translated = text;

        try
        {
            await _overlay.ShowTextAsync(translated, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // restore overlay visibility if it was temporarily hidden
            try { await _overlay.TempShowAsync(cancellationToken).ConfigureAwait(false); } catch { }
        }
        return true;
    }

    /// <summary>
    /// Run the loop until cancellation. Each iteration waits the given interval.
    /// </summary>
    public async Task StartLoopAsync(Region region, TimeSpan interval, string targetLanguage, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(region, targetLanguage, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Swallow to keep loop resilient; real implementation should log.
            }

            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }
}
