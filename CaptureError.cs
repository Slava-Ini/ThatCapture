namespace ThatCapture;

/// <summary>
/// Describes why a screen capture operation failed.
/// </summary>
/// <remarks>
/// Returned inside <see cref="CaptureResult.Err"/>. Match on the specific subtype
/// to handle each failure case:
/// <code>
/// if (result is CaptureResult.Err(var err))
/// {
///     switch (err)
///     {
///         case CaptureError.PermissionDenied:
///             // prompt the user to grant screen recording access
///             break;
///         case CaptureError.Timeout:
///             // the portal dialog was left open too long
///             break;
///         default:
///             // unexpected platform failure
///             break;
///     }
/// }
/// </code>
/// </remarks>
public abstract record CaptureError
{
    /// <summary>
    /// The D-Bus session bus is not available. Only occurs on Wayland.
    /// Usually means <c>DBUS_SESSION_BUS_ADDRESS</c> is not set or the bus is not running.
    /// </summary>
    public record SessionBusUnavailable() : CaptureError;

    /// <summary>
    /// The OS or portal denied the screenshot request.
    /// On Wayland, the user dismissed or rejected the portal permission prompt.
    /// On macOS, screen recording permission has not been granted.
    /// </summary>
    public record PermissionDenied() : CaptureError;

    /// <summary>
    /// The native display or image handle could not be obtained.
    /// On X11, <c>XOpenDisplay</c> or <c>XGetImage</c> failed.
    /// On macOS, <c>CGWindowListCreateImage</c> returned no data.
    /// </summary>
    public record CaptureFailed() : CaptureError;

    /// <summary>
    /// The Wayland portal request timed out after 60 seconds without a response.
    /// Only occurs on Wayland.
    /// </summary>
    public record Timeout() : CaptureError;
}
