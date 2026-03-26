namespace ThatCapture;

/// <summary>Pixel format of the raw data in a <see cref="CapturedFrame"/>.</summary>
public enum PixelFormat
{
    /// <summary>4 bytes per pixel: Blue, Green, Red, Alpha.</summary>
    Bgra8888,
    /// <summary>4 bytes per pixel: Red, Green, Blue, Alpha.</summary>
    Rgba8888,
}

/// <summary>
/// Raw pixel data captured from the screen.
/// </summary>
/// <remarks>
/// Pixel data is always tightly owned — no unmanaged memory, no pooling.
/// <see cref="Stride"/> may be larger than <c>Width * 4</c> due to row padding,
/// so always use <see cref="Stride"/> when stepping between rows:
/// <code>
/// for (int y = 0; y &lt; frame.Height; y++)
/// {
///     var row = frame.Pixels.AsSpan(y * frame.Stride, frame.Width * 4);
/// }
/// </code>
/// </remarks>
public sealed class CapturedFrame(byte[] pixels, int width, int height, int stride, PixelFormat format)
{
    /// <summary>Raw pixel bytes in <see cref="Format"/> order.</summary>
    public byte[] Pixels { get; } = pixels;

    /// <summary>Width of the captured area in pixels.</summary>
    public int Width { get; } = width;

    /// <summary>Height of the captured area in pixels.</summary>
    public int Height { get; } = height;

    /// <summary>Number of bytes per row, including any padding.</summary>
    public int Stride { get; } = stride;

    /// <summary>Pixel format of <see cref="Pixels"/>.</summary>
    public PixelFormat Format { get; } = format;
}
