using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ThatCapture.Platforms;

[SupportedOSPlatform("windows")]
internal sealed class WindowsScreenCapture : IScreenCapture
{
    public Task<CapturedFrame?> CaptureAreaAsync(int x, int y, int width, int height) =>
        Task.Run(() => Capture(x, y, width, height));

    private static CapturedFrame? Capture(int x, int y, int width, int height)
    {
        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            bitmap.PixelFormat);
        try
        {
            int stride = bmpData.Stride;
            var pixels = new byte[stride * height];
            Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
            return new CapturedFrame(pixels, width, height, stride, PixelFormat.Bgra8888);
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }
}
