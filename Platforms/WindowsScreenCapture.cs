using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ThatCapture.Platforms;

[SupportedOSPlatform("windows")]
internal sealed class WindowsScreenCapture : IScreenCapture
{
    public Task<CaptureResult> CaptureAreaAsync(int x, int y, int width, int height) =>
        Task.Run(() => Capture(x, y, width, height));

    private sealed class LockedBits(Bitmap bitmap, BitmapData data) : IDisposable
    {
        public BitmapData Data => data;
        public void Dispose() => bitmap.UnlockBits(data);
    }

    private static CaptureResult Capture(int x, int y, int width, int height)
    {
        try
        {
            using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            using var locked = new LockedBits(bitmap, bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat));
            int stride = locked.Data.Stride;
            var pixels = new byte[stride * height];
            Marshal.Copy(locked.Data.Scan0, pixels, 0, pixels.Length);
            return new CaptureResult.Ok(new CapturedFrame(pixels, width, height, stride, PixelFormat.Bgra8888));
        }
        catch (Exception ex)
        {
            return new CaptureResult.Err(new CaptureError.CaptureFailed(ex.Message));
        }
    }
}
