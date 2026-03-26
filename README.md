# ThatCapture [DRAFT]

Small cross-platform screen capture library for .NET.

## Usage

```csharp
var capture = new ScreenCapture();
var result = await capture.CaptureAreaAsync(x, y, width, height);

if (result is CaptureResult.Ok(var frame))
{
    // frame.Pixels  — raw bytes
    // frame.Width   — pixels wide
    // frame.Height  — pixels tall
    // frame.Stride  — bytes per row (may include padding — always use Stride, not Width * 4)
    // frame.Format  — e.g. PixelFormat.Bgra8888
}
```

`ScreenCapture` automatically picks the right implementation for the current platform. The returned `CapturedFrame` always contains raw pixel data — no image library types in the API.

## Error handling

`CaptureAreaAsync` returns a `CaptureResult`, which is either `Ok` or `Err`. Match on it to handle failures:

```csharp
switch (result)
{
    case CaptureResult.Ok(var frame):
        // use frame
        break;
    case CaptureResult.Err(var err):
        var message = err switch
        {
            CaptureError.PermissionDenied      => "screen recording permission was denied",
            CaptureError.SessionBusUnavailable => "D-Bus session bus is unavailable",
            CaptureError.Timeout               => "portal request timed out",
            CaptureError.CaptureFailed         => "native capture call failed",
        };
        break;
}
```

## Platform support

| Platform        | Support | Method                          | Dependency              |
|-----------------|---------|---------------------------------|-------------------------|
| Windows         | ✅      | `Graphics.CopyFromScreen`       | `System.Drawing.Common` |
| macOS           | ✅      | `CGWindowListCreateImage`       | CoreGraphics (system)   |
| Linux (X11)     | ✅      | `XGetImage`                     | libX11 (system)         |
| Linux (Wayland) | ✅      | `xdg-desktop-portal` Screenshot | `Tmds.DBus.Protocol`    |

> **Wayland note:** the first capture will show a system permission prompt from the desktop portal. This is a Wayland security requirement and cannot be bypassed. After the user accepts, subsequent calls proceed silently.
