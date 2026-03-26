using System.Runtime.InteropServices;

namespace ThatCapture.Platforms;

internal sealed class X11ScreenCapture : IScreenCapture
{
    // XImage struct field offsets on 64-bit Linux
    private const int DataOffset = 16;         // char* data
    private const int BytesPerLineOffset = 44; // int bytes_per_line
    private const int BitsPerPixelOffset = 48; // int bits_per_pixel

    private const int ZPixmap = 2;
    private const ulong AllPlanes = ulong.MaxValue;

    [DllImport("libX11.so.6")] private static extern IntPtr XOpenDisplay(IntPtr display);
    [DllImport("libX11.so.6")] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")] private static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, ulong planeMask, int format);
    [DllImport("libX11.so.6")] private static extern int XDestroyImage(IntPtr ximage);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(IntPtr display);

    public Task<CapturedFrame?> CaptureAreaAsync(int x, int y, int width, int height) =>
        Task.Run(() => Capture(x, y, width, height));

    private static CapturedFrame? Capture(int x, int y, int width, int height)
    {
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero) return null;
        try
        {
            var root = XDefaultRootWindow(display);
            var ximage = XGetImage(display, root, x, y, (uint)width, (uint)height, AllPlanes, ZPixmap);
            if (ximage == IntPtr.Zero) return null;
            try
            {
                return XImageToFrame(ximage, width, height);
            }
            finally
            {
                XDestroyImage(ximage);
            }
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    private static CapturedFrame? XImageToFrame(IntPtr ximage, int width, int height)
    {
        var dataPtr = Marshal.ReadIntPtr(ximage, DataOffset);
        var bytesPerLine = Marshal.ReadInt32(ximage, BytesPerLineOffset);
        var bitsPerPixel = Marshal.ReadInt32(ximage, BitsPerPixelOffset);

        if (bitsPerPixel != 32 || dataPtr == IntPtr.Zero) return null;

        int stride = width * 4;
        var pixels = new byte[stride * height];
        var row = new byte[width * 4];

        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(IntPtr.Add(dataPtr, y * bytesPerLine), row, 0, width * 4);
            Buffer.BlockCopy(row, 0, pixels, y * stride, width * 4);
        }

        return new CapturedFrame(pixels, width, height, stride, PixelFormat.Bgra8888);
    }
}
