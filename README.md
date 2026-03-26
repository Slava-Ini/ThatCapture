# ThatCapture [DRAFT]

Small cross-platform screen capture library for .NET.

## Usage

```csharp
var capture = new ScreenCapture();
CapturedFrame? frame = await capture.CaptureAreaAsync(x, y, width, height);

if (frame != null)
{
    // frame.Pixels  — raw bytes
    // frame.Width   — pixels wide
    // frame.Height  — pixels tall
    // frame.Stride  — bytes per row
    // frame.Format  — e.g. PixelFormat.Bgra8888
}
```

`ScreenCapture` automatically picks the right implementation for the current platform. The returned `CapturedFrame` always contains raw pixel data — no image library types in the API.

## Platform support

| Platform        | Support | Method                          | Dependency              |
|-----------------|---------|---------------------------------|-------------------------|
| Windows         | ✅      | `Graphics.CopyFromScreen`       | `System.Drawing.Common` |
| macOS           | ✅      | `CGWindowListCreateImage`       | CoreGraphics (system)   |
| Linux (X11)     | ✅      | `XGetImage`                     | libX11 (system)         |
| Linux (Wayland) | ✅      | `xdg-desktop-portal` Screenshot | `Tmds.DBus.Protocol`    |

> **Wayland note:** the first capture will show a system permission prompt from the desktop portal. This is a Wayland security requirement and cannot be bypassed. After the user accepts, subsequent calls proceed silently.
