# ThatCapture [DRAFT]

Small cross-platform screen capture library for .NET

## Platform support

| Platform        | Support | Method                          | Dependency              |
|-----------------|---------|---------------------------------|-------------------------|
| Windows         |    ✅   | `Graphics.CopyFromScreen`       | `System.Drawing.Common` |
| macOS           |    ✅   | `CGWindowListCreateImage`       | CoreGraphics (system)   |
| Linux (X11)     |    ✅   | `XGetImage`                     | libX11 (system)         |
| Linux (Wayland) |    ✅   | `xdg-desktop-portal` Screenshot | `Tmds.DBus.Protocol`    |

> **Wayland note:** the first capture will show a system permission prompt from the desktop portal. This is a Wayland security requirement and cannot be bypassed. After the user accepts, subsequent calls proceed silently.

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

`ScreenCapture` automatically picks the right backend for the current platform. The active backend is exposed via the `Platform` property:

```csharp
Console.WriteLine(capture.Platform);
```

To override the auto-detected platform, pass a `CapturePlatform` value to the constructor:

```csharp
var capture = new ScreenCapture(CapturePlatform.X11);
```

The returned `CapturedFrame` always contains raw pixel data — no image library types in the API.

### Encoding to an image format

Extension methods are available to encode a captured frame to PNG, JPEG, or WebP:

```csharp
byte[] png  = frame.ToPng();
byte[] jpeg = frame.ToJpeg(); // default quality: 85
byte[] jpeg = frame.ToJpeg(quality: 60);
byte[] webp = frame.ToWebp(); // lossy by default
byte[] webp = frame.ToWebp(lossless: true);
```

To save directly to a file:

```csharp
await File.WriteAllBytesAsync("screenshot.png", frame.ToPng());
```

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

## To Do

- Improve error handling - handle error messages (& remove error descriptions in `CaptureError`)
- Cleanup some garbage and inconsistencies
- Do additional test loop on all systems
- Publish the package
