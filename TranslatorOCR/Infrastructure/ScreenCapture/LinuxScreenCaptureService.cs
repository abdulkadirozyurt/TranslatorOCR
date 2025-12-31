#if NET
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Models;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.ScreenCapture;

[SupportedOSPlatform("linux")]
public class LinuxScreenCaptureService : IScreenCaptureService
{
    /// <summary>
    /// Tries to use common Linux screenshot tools to capture to stdout:
    /// - Prefer `grim` (Wayland) if available: `grim -` with geometry option via `slurp` not implemented here.
    /// - Fallback to ImageMagick `import` if available.
    /// </summary>
    public async Task<byte[]?> CaptureRegionAsync(Region region, CancellationToken cancellationToken)
    {
        // Try ImageMagick 'import' first (widely available)
        var importArgs = $"-silent -window root -crop {region.Width}x{region.Height}+{region.X}+{region.Y} png:-";

        var psi = new ProcessStartInfo("import", importArgs)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var proc = Process.Start(psi)!;
            using var ms = new MemoryStream();
            await proc.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (proc.ExitCode == 0) return ms.ToArray();
        }
        catch
        {
        }

        // If 'import' is not available, try 'grim' (Wayland) without slurp; needs external handling in complex setups.
        var grimArgs = $"-"; // grim can accept - for stdout with optional -g geometry, but varies.
        psi = new ProcessStartInfo("grim", grimArgs)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var proc = Process.Start(psi)!;
            using var ms = new MemoryStream();
            await proc.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            await proc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (proc.ExitCode == 0) return ms.ToArray();
        }
        catch
        {
        }

        return null;
    }
}
#endif
