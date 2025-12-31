#if NET
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Models;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.ScreenCapture;

[SupportedOSPlatform("macos")]
public class MacScreenCaptureService : IScreenCaptureService
{
    /// <summary>
    /// Uses the system `screencapture` tool to capture a region and output PNG to stdout.
    /// Requires macOS built-in `screencapture`.
    /// </summary>
    public async Task<byte[]?> CaptureRegionAsync(Region region, CancellationToken cancellationToken)
    {
        // Arguments: -x suppresses sound, -R x,y,w,h, - (output to stdout)
        var args = $"-x -R {region.X},{region.Y},{region.Width},{region.Height} -";

        var psi = new ProcessStartInfo("screencapture", args)
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
            if (proc.ExitCode != 0) return null;
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
#endif
