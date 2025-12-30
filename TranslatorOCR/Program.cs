using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace TranslatorOCR
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Build a minimal Host to provide DI to the Avalonia app.
            var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register default implementations (can be overridden later)
                    services.AddSingleton<TranslatorOCR.Services.IScreenCaptureService, TranslatorOCR.Infrastructure.ScreenCapture.ScreenCaptureService>();
                    services.AddSingleton<TranslatorOCR.Services.IOcrService, TranslatorOCR.Infrastructure.Ocr.TesseractOcrService>();
                    services.AddSingleton<TranslatorOCR.Services.ITranslationService, TranslatorOCR.Infrastructure.Translation.MockTranslationService>();
                    services.AddSingleton<TranslatorOCR.Services.IOverlayService, TranslatorOCR.Infrastructure.Overlay.AvaloniaOverlayService>();
                    services.AddSingleton<TranslatorOCR.Application.AppController>();
                })
                .Build();

            // expose the service provider to the Avalonia app
            App.Services = host.Services;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
