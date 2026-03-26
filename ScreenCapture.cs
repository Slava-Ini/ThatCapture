using System.Runtime.InteropServices;
using ThatCapture.Platforms;

namespace ThatCapture;

public sealed class ScreenCapture : IScreenCapture
{
    private readonly IScreenCapture _impl;

    public ScreenCapture()
    {
        _impl = CreatePlatformImpl();
    }

    public Task<CapturedFrame?> CaptureAreaAsync(int x, int y, int width, int height) =>
        _impl.CaptureAreaAsync(x, y, width, height);

    private static IScreenCapture CreatePlatformImpl()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsScreenCapture();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new MacScreenCapture();

        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "";

        return sessionType.Equals("wayland", StringComparison.OrdinalIgnoreCase)
            ? new WaylandScreenCapture()
            : new X11ScreenCapture();
    }
}
