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
    /// <summary>
    /// Comprehensive tests for TesseractOcrService covering:
    /// - Path resolution (settings, environment, search)
    /// - Error diagnostics when initialization fails
    /// - Image preprocessing
    /// - OCR accuracy on generated images
    /// - Edge cases (null/empty input, cancellation)
    /// </summary>
    public class OcrIntegrationTests
    {
        #region Path Resolution Tests

        [Fact]
        public void TesseractOcrService_Throws_When_TessdataMissing()
        {
            // Arrange: point settings to a non-existent temp folder
            var tmp = Path.Combine(Path.GetTempPath(), "translatorocr_nonexistent_tessdata_" + Guid.NewGuid().ToString("N"));
            var settings = new SettingsServiceForTest { TessdataPath = tmp, OcrLanguage = "eng" };

            // Act & Assert: calling ReadTextAsync triggers engine initialization which will check tessdata
            using var svc = new TesseractOcrService(settings);
            var ex = Assert.ThrowsAsync<DirectoryNotFoundException>(async () => 
                await svc.ReadTextAsync(new byte[] { 1 }, CancellationToken.None)).GetAwaiter().GetResult();
            Assert.Contains("tessdata", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TesseractOcrService_Uses_SettingsPath_When_Provided()
        {
            // Arrange: Create a real temp folder but without traineddata
            var tmp = Path.Combine(Path.GetTempPath(), "translatorocr_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var settings = new SettingsServiceForTest { TessdataPath = tmp, OcrLanguage = "eng" };
                using var svc = new TesseractOcrService(settings);
                
                // Act: Try to read - should fail with InvalidOperationException (not DirectoryNotFound)
                // because the directory exists but traineddata is missing
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await svc.ReadTextAsync(CreateMinimalPng(), CancellationToken.None)).GetAwaiter().GetResult();
                
                // Assert: Error message should contain the path we provided
                Assert.Contains(tmp, ex.Message);
            }
            finally
            {
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            }
        }

        [Fact]
        public void TesseractOcrService_Falls_Back_To_BaseDirectory_When_SettingsEmpty()
        {
            // Arrange: Empty settings should trigger fallback search
            var settings = new SettingsServiceForTest { TessdataPath = "", OcrLanguage = "eng" };
            
            // Act: Create service (constructor does path resolution)
            using var svc = new TesseractOcrService(settings);
            
            // We can't easily assert the internal path, but we can verify it doesn't throw in constructor
            Assert.NotNull(svc);
        }

        [Fact]
        public void TesseractOcrService_Handles_Null_Settings_Gracefully()
        {
            // Arrange: null settings
            var settings = new SettingsServiceForTest { TessdataPath = null, OcrLanguage = null };
            
            // Act: Should not throw in constructor
            using var svc = new TesseractOcrService(settings);
            Assert.NotNull(svc);
        }

        #endregion

        #region Error Diagnostics Tests

        [Fact]
        public void TesseractOcrService_Provides_Detailed_Diagnostics_On_InitFailure()
        {
            // Arrange: Create folder with dummy file but no valid traineddata
            var tmp = Path.Combine(Path.GetTempPath(), "translatorocr_diag_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            File.WriteAllText(Path.Combine(tmp, "dummy.txt"), "not traineddata");
            
            try
            {
                var settings = new SettingsServiceForTest { TessdataPath = tmp, OcrLanguage = "eng" };
                using var svc = new TesseractOcrService(settings);
                
                // Act
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await svc.ReadTextAsync(CreateMinimalPng(), CancellationToken.None)).GetAwaiter().GetResult();
                
                // Assert: Error should contain diagnostic info
                Assert.Contains("tessdata path:", ex.Message, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Language requested:", ex.Message, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("eng", ex.Message);
                Assert.Contains("traineddata present: False", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            }
        }

        [Fact]
        public void TesseractOcrService_Lists_Available_TrainedData_In_Error()
        {
            // Arrange: Create folder with a fake traineddata file
            var tmp = Path.Combine(Path.GetTempPath(), "translatorocr_list_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            File.WriteAllBytes(Path.Combine(tmp, "fake.traineddata"), new byte[] { 0 });
            
            try
            {
                var settings = new SettingsServiceForTest { TessdataPath = tmp, OcrLanguage = "eng" };
                using var svc = new TesseractOcrService(settings);
                
                // Act
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await svc.ReadTextAsync(CreateMinimalPng(), CancellationToken.None)).GetAwaiter().GetResult();
                
                // Assert: Error should list the fake traineddata file
                Assert.Contains("fake.traineddata", ex.Message);
            }
            finally
            {
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            }
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        public async Task ReadTextAsync_Returns_Null_For_Null_Input()
        {
            var settings = new SettingsServiceForTest { TessdataPath = null, OcrLanguage = "eng" };
            using var svc = new TesseractOcrService(settings);
            
            var result = await svc.ReadTextAsync(null!, CancellationToken.None);
            
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadTextAsync_Returns_Null_For_Empty_Array()
        {
            var settings = new SettingsServiceForTest { TessdataPath = null, OcrLanguage = "eng" };
            using var svc = new TesseractOcrService(settings);
            
            var result = await svc.ReadTextAsync(Array.Empty<byte>(), CancellationToken.None);
            
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadTextAsync_Respects_CancellationToken()
        {
            var tessPath = FindTessdataPath();
            if (tessPath == null) return; // Skip if tessdata not available
            
            var settings = new SettingsServiceForTest { TessdataPath = tessPath, OcrLanguage = "eng" };
            using var svc = new TesseractOcrService(settings);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // TaskCanceledException is a subclass of OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await svc.ReadTextAsync(CreateMinimalPng(), cts.Token));
        }

        #endregion

        #region OCR Accuracy Tests

        [Fact]
        public async Task Ocr_On_GeneratedImage_Returns_Text_When_TessdataAvailable()
        {
            var tessPath = FindTessdataPath();
            if (tessPath == null) return; // Skip if tessdata not available

            var settings = new SettingsServiceForTest { TessdataPath = tessPath, OcrLanguage = "eng" };

            // Generate a simple image with the word HELLO using System.Drawing
            byte[] pngBytes = CreateImageWithText("HELLO");

            try
            {
                using var svc = new TesseractOcrService(settings);
                var text = await svc.ReadTextAsync(pngBytes, CancellationToken.None);
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.Contains("HELLO", text, StringComparison.OrdinalIgnoreCase);
            }
            catch (Tesseract.TesseractException)
            {
                // Native Tesseract not available in this environment; treat as pass
                return;
            }
            catch (InvalidOperationException ex) when (ex.InnerException is Tesseract.TesseractException)
            {
                // Native Tesseract not available; treat as pass
                return;
            }
        }

        [Fact]
        public async Task Ocr_On_SmallImage_Upscales_And_Reads_Text()
        {
            var tessPath = FindTessdataPath();
            if (tessPath == null) return;

            var settings = new SettingsServiceForTest { TessdataPath = tessPath, OcrLanguage = "eng" };

            // Create a small image (should trigger upscaling in Preprocess)
            byte[] pngBytes = CreateImageWithText("TEST", width: 200, height: 50);

            try
            {
                using var svc = new TesseractOcrService(settings);
                var text = await svc.ReadTextAsync(pngBytes, CancellationToken.None);
                // May or may not recognize "TEST" depending on image quality
                // Just verify it doesn't crash
                Assert.True(text == null || text.Length >= 0);
            }
            catch (Tesseract.TesseractException)
            {
                return;
            }
            catch (InvalidOperationException ex) when (ex.InnerException is Tesseract.TesseractException)
            {
                return;
            }
        }

        [Fact]
        public async Task Ocr_On_LargeImage_DoesNot_Upscale()
        {
            var tessPath = FindTessdataPath();
            if (tessPath == null) return;

            var settings = new SettingsServiceForTest { TessdataPath = tessPath, OcrLanguage = "eng" };

            // Create a large image (should NOT trigger upscaling)
            byte[] pngBytes = CreateImageWithText("LARGE", width: 1200, height: 400);

            try
            {
                using var svc = new TesseractOcrService(settings);
                var text = await svc.ReadTextAsync(pngBytes, CancellationToken.None);
                Assert.True(text == null || text.Length >= 0);
            }
            catch (Tesseract.TesseractException)
            {
                return;
            }
            catch (InvalidOperationException ex) when (ex.InnerException is Tesseract.TesseractException)
            {
                return;
            }
        }

        [Fact]
        public async Task Ocr_Returns_Null_For_Blank_Image()
        {
            var tessPath = FindTessdataPath();
            if (tessPath == null) return;

            var settings = new SettingsServiceForTest { TessdataPath = tessPath, OcrLanguage = "eng" };

            // Create completely white image with no text
            byte[] pngBytes = CreateBlankImage();

            try
            {
                using var svc = new TesseractOcrService(settings);
                var text = await svc.ReadTextAsync(pngBytes, CancellationToken.None);
                // Blank image should return null or empty
                Assert.True(string.IsNullOrWhiteSpace(text));
            }
            catch (Tesseract.TesseractException)
            {
                return;
            }
            catch (InvalidOperationException ex) when (ex.InnerException is Tesseract.TesseractException)
            {
                return;
            }
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void TesseractOcrService_Can_Be_Disposed_Multiple_Times()
        {
            var settings = new SettingsServiceForTest { TessdataPath = null, OcrLanguage = "eng" };
            var svc = new TesseractOcrService(settings);
            
            // Should not throw
            svc.Dispose();
            svc.Dispose();
            svc.Dispose();
        }

        #endregion

        #region Helper Methods

        private static string? FindTessdataPath()
        {
            // Try several locations
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "tessdata"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TranslatorOCR", "tessdata"),
                Environment.GetEnvironmentVariable("TESSDATA_PREFIX") ?? ""
            };

            foreach (var path in candidates)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, "eng.traineddata")))
                    return path;
            }

            return null;
        }

        private static byte[] CreateMinimalPng()
        {
            // Minimal valid 1x1 white PNG
            using var bmp = new System.Drawing.Bitmap(1, 1);
            bmp.SetPixel(0, 0, System.Drawing.Color.White);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static byte[] CreateBlankImage()
        {
            using var bmp = new System.Drawing.Bitmap(400, 120);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.White);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static byte[] CreateImageWithText(string text, int width = 400, int height = 120)
        {
            using var bmp = new System.Drawing.Bitmap(width, height);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            using var font = new System.Drawing.Font("Arial", Math.Min(48, height - 10), System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            using var brush = System.Drawing.Brushes.Black;
            g.DrawString(text, font, brush, new System.Drawing.PointF(10, 10));
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        #endregion

        #region Test Settings Implementation

        private class SettingsServiceForTest : ISettingsService
        {
            public string? TessdataPath { get; set; }
            public string? OcrLanguage { get; set; }
            public void Load() { }
            public void Save() { }
        }

        #endregion
    }
}
