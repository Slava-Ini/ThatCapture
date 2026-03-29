using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace ThatCapture;

/// <summary>
/// Extension methods for encoding a <see cref="CapturedFrame"/> to common image formats.
/// </summary>
/// <remarks>
/// All methods require <see cref="PixelFormat.Bgra8888"/> pixel data,
/// which is what every platform implementation currently produces.
/// </remarks>
public static class CapturedFrameExtensions
{
    /// <summary>Encodes the frame as a PNG byte array (lossless).</summary>
    public static byte[] ToPng(this CapturedFrame frame)
    {
        using var image = ToImage(frame);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms, new PngEncoder { CompressionLevel = PngCompressionLevel.DefaultCompression });
        return ms.ToArray();
    }

    /// <summary>Encodes the frame as a JPEG byte array.</summary>
    /// <param name="quality">
    /// JPEG quality, 1–100. Higher means better quality and larger file.
    /// Defaults to 85, a good balance for screen content.
    /// </param>
    public static byte[] ToJpeg(this CapturedFrame frame, int quality = 85)
    {
        using var image = ToImage(frame);
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = quality });
        return ms.ToArray();
    }

    /// <summary>Encodes the frame as a WebP byte array.</summary>
    /// <param name="lossless">
    /// When <c>true</c>, produces a lossless WebP (larger, pixel-perfect).
    /// When <c>false</c> (default), produces a lossy WebP (smaller, near-lossless at high quality).
    /// </param>
    /// <param name="quality">
    /// Quality for lossy encoding, 1–100. Ignored when <paramref name="lossless"/> is <c>true</c>.
    /// Defaults to 85.
    /// </param>
    public static byte[] ToWebp(this CapturedFrame frame, bool lossless = false, int quality = 85)
    {
        using var image = ToImage(frame);
        using var ms = new MemoryStream();
        image.SaveAsWebp(ms, new WebpEncoder
        {
            FileFormat = lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
            Quality = quality,
        });
        return ms.ToArray();
    }

    private static Image<Bgra32> ToImage(CapturedFrame frame)
    {
        if (frame.Format != PixelFormat.Bgra8888)
            throw new NotSupportedException($"Pixel format {frame.Format} is not supported for encoding.");

        int tightStride = frame.Width * 4;

        if (frame.Stride == tightStride)
        {
            return Image.LoadPixelData<Bgra32>(
                MemoryMarshal.Cast<byte, Bgra32>(frame.Pixels.AsSpan()),
                frame.Width, frame.Height);
        }

        // Stride has row padding — copy each row into the image individually.
        var image = new Image<Bgra32>(frame.Width, frame.Height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < frame.Height; y++)
            {
                var src = MemoryMarshal.Cast<byte, Bgra32>(
                    frame.Pixels.AsSpan(y * frame.Stride, tightStride));
                src.CopyTo(accessor.GetRowSpan(y));
            }
        });
        return image;
    }
}
