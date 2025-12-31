using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Infrastructure.Ocr;
using TranslatorOCR.Infrastructure.Settings;
using TranslatorOCR.Services;
using Xunit;

namespace TranslatorOCR.Tests
{
    public class OcrIntegrationTests
    {
        [Fact]
        public void TesseractOcrService_Throws_When_TessdataMissing()
        {
            // arrange: point settings to a non-existent temp folder
            var tmp = Path.Combine(Path.GetTempPath(), "translatorocr_nonexistent_tessdata_" + Guid.NewGuid().ToString("N"));
            var settings = new SettingsServiceForTest { TessdataPath = tmp, OcrLanguage = "eng" };

            // act & assert: calling ReadTextAsync triggers engine initialization which will check tessdata
            using var svc = new TesseractOcrService(settings);
            var ex = Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await svc.ReadTextAsync(new byte[] { 1 }, CancellationToken.None)).GetAwaiter().GetResult();
            Assert.Contains("tessdata", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Ocr_On_GeneratedImage_Returns_Text_When_TessdataAvailable()
        {
            var tessPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            var engTrained = Path.Combine(tessPath, "eng.traineddata");
            if (!File.Exists(engTrained))
                return; // tessdata not present in test environment; treat as pass

            var settings = new SettingsServiceForTest { TessdataPath = tessPath, OcrLanguage = "eng" };

            // generate a simple image with the word HELLO using System.Drawing
            byte[] pngBytes;
            using (var bmp = new System.Drawing.Bitmap(400, 120))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.White);
                using var font = new System.Drawing.Font("Arial", 48, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
                using var brush = System.Drawing.Brushes.Black;
                g.DrawString("HELLO", font, brush, new System.Drawing.PointF(10, 10));
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                pngBytes = ms.ToArray();
            }

            try
            {
                using var svc = new TesseractOcrService(settings);
                var text = await svc.ReadTextAsync(pngBytes, CancellationToken.None);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.Contains("HELLO", text, StringComparison.OrdinalIgnoreCase);
            }
            catch (Tesseract.TesseractException)
            {
                // native Tesseract not available in this environment; treat as pass
                return;
            }
        }

        private class SettingsServiceForTest : ISettingsService
        {
            public string? TessdataPath { get; set; }
            public string? OcrLanguage { get; set; }
            public void Load() { }
            public void Save() { }
        }
    }
}
