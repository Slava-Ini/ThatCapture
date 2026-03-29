using System.Runtime.InteropServices;
using ThatCapture.Platforms;

namespace ThatCapture;

public sealed class ScreenCapture : IScreenCapture
{
    private readonly IScreenCapture _impl;

    /// <summary>The platform backend currently in use.</summary>
    public CapturePlatform Platform { get; }

    /// <summary>
    /// Creates a <see cref="ScreenCapture"/> using the auto-detected platform backend.
    /// Pass <paramref name="platform"/> to override the detected platform.
    /// </summary>
    public ScreenCapture(CapturePlatform? platform = null)
    {
        Platform = platform ?? DetectPlatform();
        _impl = CreateImpl(Platform);
    }

    public Task<CaptureResult> CaptureAreaAsync(int x, int y, int width, int height) =>
        _impl.CaptureAreaAsync(x, y, width, height);

    private static CapturePlatform DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return CapturePlatform.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return CapturePlatform.MacOS;

        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "";

        return sessionType.Equals("wayland", StringComparison.OrdinalIgnoreCase)
            ? CapturePlatform.Wayland
            : CapturePlatform.X11;
    }

    private static IScreenCapture CreateImpl(CapturePlatform platform) => platform switch
    {
        CapturePlatform.Windows => new WindowsScreenCapture(),
        CapturePlatform.MacOS => new MacScreenCapture(),
        CapturePlatform.X11 => new X11ScreenCapture(),
        CapturePlatform.Wayland => new WaylandScreenCapture(),
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
    };
}
