using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Application;
using TranslatorOCR.Models;
using TranslatorOCR.Services;
using Xunit;

namespace TranslatorOCR.Tests
{
    public class AppControllerSmokeTests
    {
        [Fact]
        public async Task ProcessOnceAsync_ShowsTranslatedText_WhenOcrReturnsText()
        {
            var capture = new FakeCapture();
            var ocr = new FakeOcr();
            var translator = new FakeTranslator();
            var overlay = new FakeOverlay();

            var controller = new AppController(capture, ocr, translator, overlay);
            var region = new Region(0, 0, 100, 50);

            var result = await controller.ProcessOnceAsync(region, "en", CancellationToken.None);

            Assert.True(result);
            Assert.Equal("SAMPLE_TR", overlay.LastShown);
        }

        private class FakeCapture : IScreenCaptureService
        {
            public Task<byte[]?> CaptureRegionAsync(Region region, CancellationToken cancellationToken)
            {
                return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
            }
        }

        private class FakeOcr : IOcrService
        {
            public Task<string?> ReadTextAsync(byte[] imageBytes, CancellationToken cancellationToken)
            {
                return Task.FromResult<string?>("SAMPLE");
            }
        }

        private class FakeTranslator : ITranslationService
        {
            public Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken)
            {
                return Task.FromResult<string?>($"{text}_TR");
            }
        }

        private class FakeOverlay : IOverlayService
        {
            public string? LastShown;
            public Task ShowTextAsync(string text, CancellationToken cancellationToken)
            {
                LastShown = text;
                return Task.CompletedTask;
            }

            public Task HideAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task TempHideAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task TempShowAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
