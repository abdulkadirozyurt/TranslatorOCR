using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Ocr;

/// <summary>
/// Tesseract-based OCR service. Expects tessdata to be available.
/// It looks for `TESSDATA_PREFIX` environment variable or a local `tessdata` folder.
/// </summary>
public class TesseractOcrService : IOcrService, IDisposable
{
    private readonly string _tessDataPath;
    private readonly string _language;
    private TesseractEngine? _engine;

    public TesseractOcrService(TranslatorOCR.Services.ISettingsService settings)
    {
        _language = settings?.OcrLanguage ?? "eng";
        _tessDataPath = settings?.TessdataPath
                        ?? Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
                        ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    private void EnsureEngine()
    {
        if (_engine != null) return;
        if (!Directory.Exists(_tessDataPath))
            throw new DirectoryNotFoundException($"tessdata not found at '{_tessDataPath}'. Set TESSDATA_PREFIX or place tessdata there.");

        _engine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default);
    }

    public Task<string?> ReadTextAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        if (imageBytes == null || imageBytes.Length == 0) return Task.FromResult<string?>(null);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureEngine();

            // Preprocess image to improve OCR accuracy
            var pre = Preprocess(imageBytes);

            using var img = Pix.LoadFromMemory(pre);
            using var page = _engine!.Process(img);
            var text = page.GetText();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }, cancellationToken);
    }

    private byte[] Preprocess(byte[] imageBytes)
    {
        using var image = Image.Load<Rgba32>(imageBytes);

        // Convert to grayscale and enhance contrast
        image.Mutate(x =>
        {
            x.AutoOrient();
            x.Grayscale();
            x.Contrast(1.1f);
            x.GaussianSharpen(0.5f);

            // Upscale small images to improve OCR accuracy
            var targetWidth = image.Width < 800 ? image.Width * 2 : image.Width;
            var targetHeight = image.Height < 200 ? image.Height * 2 : image.Height;
            if (targetWidth != image.Width || targetHeight != image.Height)
            {
                x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(targetWidth, targetHeight),
                    Sampler = KnownResamplers.Lanczos3,
                    Mode = ResizeMode.Max
                });
            }
        });

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    public void Dispose()
    {
        _engine?.Dispose();
        _engine = null;
    }
}

