using System;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Models;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.ScreenCapture;

/// <summary>
/// Minimal cross-platform stub for screen capture. Replace with real implementation (Skia/Platform APIs).
/// </summary>
public class ScreenCaptureService : IScreenCaptureService
{
    public Task<byte[]?> CaptureRegionAsync(Region region, CancellationToken cancellationToken)
    {
        // TODO: Implement platform-specific capture. For now return empty to indicate nothing.
        return Task.FromResult<byte[]?>(Array.Empty<byte>());
    }
}
