namespace ThatCapture;

public interface IScreenCapture
{
    Task<CapturedFrame?> CaptureAreaAsync(int x, int y, int width, int height);
}
