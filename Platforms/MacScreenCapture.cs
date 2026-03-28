using System.Runtime.InteropServices;

namespace ThatCapture.Platforms;

internal sealed class MacScreenCapture : IScreenCapture
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGWindowListCreateImage(CGRect screenBounds, uint listOption, uint windowID, uint imageOption);
    [DllImport(CoreGraphics)]
    private static extern nint CGImageGetWidth(IntPtr image);
    [DllImport(CoreGraphics)]
    private static extern nint CGImageGetHeight(IntPtr image);
    [DllImport(CoreGraphics)]
    private static extern nint CGImageGetBytesPerRow(IntPtr image);
    [DllImport(CoreGraphics)]
    private static extern IntPtr CGImageGetDataProvider(IntPtr image);
    [DllImport(CoreGraphics)]
    private static extern IntPtr CGDataProviderCopyData(IntPtr provider);
    [DllImport(CoreFoundation)]
    private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(CoreFoundation)]
    private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr cf);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect { public double X, Y, Width, Height; }

    private sealed class CFHandle : SafeHandle
    {
        public CFHandle(IntPtr ptr) : base(IntPtr.Zero, true) { SetHandle(ptr); }
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle() { CFRelease(handle); return true; }
    }

    // kCGWindowListOptionAll = 0, kCGNullWindowID = 0, kCGWindowImageDefault = 0
    public Task<CaptureResult> CaptureAreaAsync(int x, int y, int width, int height) =>
        Task.Run(() => Capture(x, y, width, height));

    private static CaptureResult Capture(int x, int y, int width, int height)
    {
        try
        {
            var rect = new CGRect { X = x, Y = y, Width = width, Height = height };
            using var image = new CFHandle(CGWindowListCreateImage(rect, 0, 0, 0));
            if (image.IsInvalid)
                return new CaptureResult.Err(new CaptureError.PermissionDenied());

            int imgWidth = (int)CGImageGetWidth(image.DangerousGetHandle());
            int imgHeight = (int)CGImageGetHeight(image.DangerousGetHandle());
            int stride = (int)CGImageGetBytesPerRow(image.DangerousGetHandle());

            var provider = CGImageGetDataProvider(image.DangerousGetHandle());
            using var data = new CFHandle(CGDataProviderCopyData(provider));
            if (data.IsInvalid)
                return new CaptureResult.Err(new CaptureError.CaptureFailed());

            int length = (int)CFDataGetLength(data.DangerousGetHandle());
            var ptr = CFDataGetBytePtr(data.DangerousGetHandle());
            var pixels = new byte[length];
            Marshal.Copy(ptr, pixels, 0, length);
            return new CaptureResult.Ok(new CapturedFrame(pixels, imgWidth, imgHeight, stride, PixelFormat.Bgra8888));
        }
        catch
        {
            return new CaptureResult.Err(new CaptureError.CaptureFailed());
        }
    }
}
