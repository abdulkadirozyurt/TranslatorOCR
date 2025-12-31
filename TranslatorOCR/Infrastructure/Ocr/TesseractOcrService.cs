using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Common 2-letter to Tesseract 3-letter language code mapping.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "eng",
        ["de"] = "deu",
        ["fr"] = "fra",
        ["es"] = "spa",
        ["it"] = "ita",
        ["pt"] = "por",
        ["ru"] = "rus",
        ["zh"] = "chi_sim",
        ["ja"] = "jpn",
        ["ko"] = "kor",
        ["ar"] = "ara",
        ["tr"] = "tur",
        ["pl"] = "pol",
        ["nl"] = "nld",
        ["sv"] = "swe",
        ["da"] = "dan",
        ["fi"] = "fin",
        ["no"] = "nor",
        ["cs"] = "ces",
        ["el"] = "ell",
        ["he"] = "heb",
        ["hi"] = "hin",
        ["hu"] = "hun",
        ["id"] = "ind",
        ["th"] = "tha",
        ["uk"] = "ukr",
        ["vi"] = "vie",
    };

    public TesseractOcrService(TranslatorOCR.Services.ISettingsService settings)
    {
        var rawLang = settings?.OcrLanguage ?? "eng";
        _language = NormalizeLanguageCode(rawLang);
        
        var configured = settings?.TessdataPath;
        var env = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
        var defaultPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

        if (!string.IsNullOrWhiteSpace(configured))
            _tessDataPath = configured!;
        else if (!string.IsNullOrWhiteSpace(env))
            _tessDataPath = env!;
        else
            _tessDataPath = FindTessdataFolder() ?? defaultPath;
    }

    /// <summary>
    /// Normalizes language codes to Tesseract 3-letter format.
    /// </summary>
    private static string NormalizeLanguageCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "eng";
        
        code = code.Trim();
        
        // If it's a 2-letter code, map it to 3-letter
        if (LanguageCodeMap.TryGetValue(code, out var tessCode))
            return tessCode;
        
        // Already a 3-letter code or unknown, return as-is
        return code;
    }

    private void EnsureEngine()
    {
        if (_engine != null) return;
        if (!Directory.Exists(_tessDataPath))
        {
            var attempted = new[]
            {
                _tessDataPath,
                Path.Combine(AppContext.BaseDirectory, "tessdata"),
                Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? string.Empty
            };

            throw new DirectoryNotFoundException($"tessdata not found. Attempted: '{string.Join("', '", attempted)}'. Set TESSDATA_PREFIX, set `TessdataPath` in settings, or place a `tessdata` folder in the application directory.");
        }

        try
        {
            _engine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default);
        }
        catch (Tesseract.TesseractException tex)
        {
            // Collect diagnostics to help the user fix common issues
            string[] trainedFiles = Array.Empty<string>();
            try { trainedFiles = Directory.GetFiles(_tessDataPath, "*.traineddata"); } catch { }

            bool languageFilePresent = false;
            try { languageFilePresent = File.Exists(Path.Combine(_tessDataPath, _language + ".traineddata")); } catch { }

            string[] dlls = Array.Empty<string>();
            try { dlls = Directory.GetFiles(AppContext.BaseDirectory, "*.dll"); } catch { }

            var env = Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? string.Empty;

            var details = $"Tesseract initialization failed: {tex.Message}\n" +
                          $"tessdata path: '{_tessDataPath}'\n" +
                          $"TESSDATA_PREFIX: '{env}'\n" +
                          $"Language requested: '{_language}' (traineddata present: {languageFilePresent})\n" +
                          $"Traineddata files (sample): {string.Join(", ", trainedFiles.Length > 0 ? trainedFiles[..Math.Min(10, trainedFiles.Length)] : new string[]{"(none)"})}\n" +
                          $"DLLs in app folder (sample): {string.Join(", ", dlls.Length > 0 ? dlls[..Math.Min(10, dlls.Length)] : new string[]{"(none)"})}\n" +
                          "Common fixes: ensure the tessdata folder contains the language .traineddata files, confirm native tesseract/leptonica DLLs are available for the app's bitness, and install the Visual C++ Redistributable. See https://github.com/charlesw/tesseract/wiki/Error-1 for details.";

            throw new InvalidOperationException(details, tex);
        }
    }

    private static string? FindTessdataFolder()
    {
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "tessdata");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
        }
        catch
        {
            // ignore and fallback to defaults
        }

        return null;
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

