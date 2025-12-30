using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Models;

namespace TranslatorOCR.Services;

public interface IScreenCaptureService
{
    /// <summary>
    /// Capture the specified region and return raw image bytes (platform-specific format).
    /// Return null if capture failed or nothing to capture.
    /// </summary>
    Task<byte[]?> CaptureRegionAsync(Region region, CancellationToken cancellationToken);
}
