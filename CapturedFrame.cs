namespace ThatCapture;

public enum PixelFormat { Bgra8888, Rgba8888 }

public sealed class CapturedFrame(byte[] pixels, int width, int height, int stride, PixelFormat format)
{
    public byte[] Pixels { get; } = pixels;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int Stride { get; } = stride;
    public PixelFormat Format { get; } = format;
}
