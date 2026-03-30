using ThatCapture;

const int CaptureX = 0;
const int CaptureY = 0;
const int CaptureWidth = 320;
const int CaptureHeight = 240;
const string OutDir = "out";


Directory.CreateDirectory(OutDir);

var capture = new ScreenCapture();
Console.WriteLine($"Platform: {capture.Platform}");

var result = await capture.CaptureAreaAsync(CaptureX, CaptureY, CaptureWidth, CaptureHeight);

if (result is not CaptureResult.Ok(var frame))
{
    var err = ((CaptureResult.Err)result).Error;
    Console.WriteLine($"FAIL  capture failed: {err}");
    return;
}

Console.WriteLine($"Frame:    {frame.Width}x{frame.Height}, stride={frame.Stride}, format={frame.Format}");
Console.WriteLine();

await RunTest("PNG  (default)", () => frame.ToPng(), Path.Combine(OutDir, "out.png"));
await RunTest("JPEG q=85 (default)", () => frame.ToJpeg(), Path.Combine(OutDir, "out_q85.jpg"));
await RunTest("JPEG q=60", () => frame.ToJpeg(quality: 60), Path.Combine(OutDir, "out_q60.jpg"));
await RunTest("WebP lossy  (default)", () => frame.ToWebp(), Path.Combine(OutDir, "out_lossy.webp"));
await RunTest("WebP lossless", () => frame.ToWebp(lossless: true), Path.Combine(OutDir, "out_lossless.webp"));

Console.WriteLine();
Console.WriteLine($"Output files written to: {Path.GetFullPath(OutDir)}");

static async Task RunTest(string label, Func<byte[]> encode, string path)
{
    try
    {
        var bytes = encode();
        await File.WriteAllBytesAsync(path, bytes);
        Console.WriteLine($"  OK    {label,-28}  {bytes.Length,8:N0} bytes  →  {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  {label,-28}  {ex.Message}");
    }
}
