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

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, ulong planeMask, int format);
    [DllImport("libX11.so.6")]
    private static extern int XDestroyImage(IntPtr ximage);
    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    private sealed class XDisplayHandle : SafeHandle
    {
        public XDisplayHandle() : base(IntPtr.Zero, true) { SetHandle(XOpenDisplay(IntPtr.Zero)); }
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle() { XCloseDisplay(handle); return true; }
    }

    private sealed class XImageHandle : SafeHandle
    {
        public XImageHandle(IntPtr ptr) : base(IntPtr.Zero, true) { SetHandle(ptr); }
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle() { XDestroyImage(handle); return true; }
    }

    public Task<CaptureResult> CaptureAreaAsync(int x, int y, int width, int height) =>
        Task.Run(() => Capture(x, y, width, height));

    private static CaptureResult Capture(int x, int y, int width, int height)
    {
        try
        {
            using var display = new XDisplayHandle();
            if (display.IsInvalid)
                return new CaptureResult.Err(new CaptureError.CaptureFailed());

            var root = XDefaultRootWindow(display.DangerousGetHandle());
            using var ximage = new XImageHandle(XGetImage(display.DangerousGetHandle(), root, x, y, (uint)width, (uint)height, AllPlanes, ZPixmap));
            if (ximage.IsInvalid)
                return new CaptureResult.Err(new CaptureError.CaptureFailed());

            return XImageToFrame(ximage.DangerousGetHandle(), width, height);
        }
        catch (Exception ex)
        {
            return new CaptureResult.Err(new CaptureError.CaptureFailed(ex.Message));
        }
    }

    private static CaptureResult XImageToFrame(IntPtr ximage, int width, int height)
    {
        var dataPtr = Marshal.ReadIntPtr(ximage, DataOffset);
        var bytesPerLine = Marshal.ReadInt32(ximage, BytesPerLineOffset);
        var bitsPerPixel = Marshal.ReadInt32(ximage, BitsPerPixelOffset);

        if (bitsPerPixel != 32 || dataPtr == IntPtr.Zero)
            return new CaptureResult.Err(new CaptureError.CaptureFailed());

        int stride = width * 4;
        var pixels = new byte[stride * height];
        var row = new byte[width * 4];

        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(IntPtr.Add(dataPtr, y * bytesPerLine), row, 0, width * 4);
            Buffer.BlockCopy(row, 0, pixels, y * stride, width * 4);
        }

        return new CaptureResult.Ok(new CapturedFrame(pixels, width, height, stride, PixelFormat.Bgra8888));
    }
}

