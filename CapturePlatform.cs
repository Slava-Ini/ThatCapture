namespace ThatCapture;

/// <summary>Identifies the platform-specific backend used by <see cref="ScreenCapture"/>.</summary>
public enum CapturePlatform
{
    /// <summary>Windows via GDI+ (<c>System.Drawing</c>).</summary>
    Windows,
    /// <summary>macOS via CoreGraphics.</summary>
    MacOS,
    /// <summary>Linux X11 via libX11.</summary>
    X11,
    /// <summary>Linux Wayland via xdg-desktop-portal.</summary>
    Wayland,
}
