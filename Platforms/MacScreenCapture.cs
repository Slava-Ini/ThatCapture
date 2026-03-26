using System.Runtime.InteropServices;

namespace ThatCapture.Platforms;

internal sealed class MacScreenCapture : IScreenCapture
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreGraphics)] private static extern IntPtr CGWindowListCreateImage(CGRect screenBounds, uint listOption, uint windowID, uint imageOption);
    [DllImport(CoreGraphics)] private static extern nint CGImageGetWidth(IntPtr image);
    [DllImport(CoreGraphics)] private static extern nint CGImageGetHeight(IntPtr image);
    [DllImport(CoreGraphics)] private static extern nint CGImageGetBytesPerRow(IntPtr image);
    [DllImport(CoreGraphics)] private static extern IntPtr CGImageGetDataProvider(IntPtr image);
    [DllImport(CoreGraphics)] private static extern IntPtr CGDataProviderCopyData(IntPtr provider);
    [DllImport(CoreFoundation)] private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(CoreFoundation)] private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(CoreFoundation)] private static extern void CFRelease(IntPtr cf);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect { public double X, Y, Width, Height; }

    // kCGWindowListOptionAll = 0, kCGNullWindowID = 0, kCGWindowImageDefault = 0
    public Task<CapturedFrame?> CaptureAreaAsync(int x, int y, int width, int height) =>
        Task.Run(() => Capture(x, y, width, height));

    private static CapturedFrame? Capture(int x, int y, int width, int height)
    {
        var rect = new CGRect { X = x, Y = y, Width = width, Height = height };
        var image = CGWindowListCreateImage(rect, 0, 0, 0);
        if (image == IntPtr.Zero) return null;

        try
        {
            int imgWidth = (int)CGImageGetWidth(image);
            int imgHeight = (int)CGImageGetHeight(image);
            int stride = (int)CGImageGetBytesPerRow(image);

            var provider = CGImageGetDataProvider(image);
            var data = CGDataProviderCopyData(provider);
            if (data == IntPtr.Zero) return null;

            try
            {
                int length = (int)CFDataGetLength(data);
                var ptr = CFDataGetBytePtr(data);
                var pixels = new byte[length];
                Marshal.Copy(ptr, pixels, 0, length);
                return new CapturedFrame(pixels, imgWidth, imgHeight, stride, PixelFormat.Bgra8888);
            }
            finally
            {
                CFRelease(data);
            }
        }
        finally
        {
            CFRelease(image);
        }
    }
}
