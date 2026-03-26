namespace ThatCapture;

public interface IScreenCapture
{
    Task<CaptureResult> CaptureAreaAsync(int x, int y, int width, int height);
}
