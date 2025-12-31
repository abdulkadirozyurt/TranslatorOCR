#if NET
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TranslatorOCR.Models;
using TR = TranslatorOCR.Models.Region;
using System.Runtime.Versioning;
using TranslatorOCR.Services;

namespace TranslatorOCR.Infrastructure.ScreenCapture;

    [SupportedOSPlatform("windows")]
    public class WindowsScreenCaptureService : IScreenCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr ptr);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, System.Int32 dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    private const int SRCCOPY = 0x00CC0020;

    public Task<byte[]?> CaptureRegionAsync(TR region, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("WindowsScreenCaptureService is only supported on Windows");

        var desktopWnd = GetDesktopWindow();
        var desktopDc = GetWindowDC(desktopWnd);
        var memDc = CreateCompatibleDC(desktopDc);
        var bmp = CreateCompatibleBitmap(desktopDc, region.Width, region.Height);
        var oldBitmap = SelectObject(memDc, bmp);

        try
        {
            BitBlt(memDc, 0, 0, region.Width, region.Height, desktopDc, region.X, region.Y, SRCCOPY);

            using var image = Image.FromHbitmap(bmp);
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            return Task.FromResult<byte[]?>(ms.ToArray());
        }
        finally
        {
            SelectObject(memDc, oldBitmap);
            DeleteObject(bmp);
            DeleteDC(memDc);
        }
    }
}
#endif
