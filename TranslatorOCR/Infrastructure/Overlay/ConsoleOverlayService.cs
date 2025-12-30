using System;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.Overlay;

/// <summary>
/// Very small overlay implementation that writes to console during development.
/// Replace with Avalonia overlay window for production UI.
/// </summary>
public class ConsoleOverlayService : IOverlayService
{
    public Task HideAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[Overlay] Hide");
        return Task.CompletedTask;
    }

    public Task ShowTextAsync(string text, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Overlay] {text}");
        return Task.CompletedTask;
    }
}
