namespace ThatCapture;

/// <summary>
/// The result of a screen capture operation. Either <see cref="Ok"/> or <see cref="Err"/>.
/// </summary>
/// <remarks>
/// Use pattern matching to access the value — there is no shared property that gives you
/// the frame or the error without first discriminating which case you have:
/// <code>
/// var result = await capture.CaptureAreaAsync(x, y, width, height);
///
/// if (result is CaptureResult.Ok(var frame))
/// {
///     // use frame.Pixels, frame.Width, etc.
/// }
///
/// // Or exhaustively with a switch expression:
/// string label = result switch
/// {
///     CaptureResult.Ok(var frame) => $"captured {frame.Width}x{frame.Height}",
///     CaptureResult.Err(var err)  => $"failed: {err}",
/// };
/// </code>
/// </remarks>
public abstract record CaptureResult
{
    /// <summary>The capture succeeded. Contains the captured <see cref="CapturedFrame"/>.</summary>
    public sealed record Ok(CapturedFrame Frame) : CaptureResult;

    /// <summary>The capture failed. Contains a <see cref="CaptureError"/> describing why.</summary>
    public sealed record Err(CaptureError Error) : CaptureResult;
}
