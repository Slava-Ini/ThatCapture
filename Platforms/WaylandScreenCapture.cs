using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tmds.DBus.Protocol;

namespace ThatCapture.Platforms;

internal sealed class WaylandScreenCapture : IScreenCapture
{
    public Task<CaptureResult> CaptureAreaAsync(int x, int y, int width, int height) =>
        TakeScreenshotAsync(x, y, width, height);

    // ImageSharp is used internally here only to decode the PNG returned by xdg-desktop-portal.
    // It does not appear in the public API.
    private static CapturedFrame DecodeCropped(string filePath, int x, int y, int width, int height)
    {
        using var img = Image.Load<Bgra32>(filePath);
        img.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));

        int stride = width * 4;
        var pixels = new byte[stride * height];
        img.CopyPixelDataTo(MemoryMarshal.Cast<byte, Bgra32>(pixels.AsSpan()));

        return new CapturedFrame(pixels, width, height, stride, PixelFormat.Bgra8888);
    }

    private static string? ReadPortalResponse(Message message, object? _)
    {
        var reader = message.GetBodyReader();
        uint response = reader.ReadUInt32();
        if (response != 0) return null;

        string? uri = null;
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.DictEntry);
        while (reader.HasNext(arrayEnd))
        {
            reader.AlignStruct();
            string key = reader.ReadString();
            VariantValue value = reader.ReadVariantValue();
            if (key == "uri")
                uri = value.GetString();
        }
        return uri != null ? new Uri(uri).LocalPath : null;
    }

    private static void HandlePortalResponse(Exception? ex, string? path, object? _, object? state)
    {
        var tcs = (TaskCompletionSource<string?>)state!;
        if (tcs.Task.IsCompleted) return;
        if (ex != null)
            tcs.TrySetException(ex);
        else
            tcs.TrySetResult(path);
    }

    private static async Task<CaptureResult> TakeScreenshotAsync(int x, int y, int width, int height)
    {
        var sessionAddress = Address.Session;
        if (sessionAddress == null) return new CaptureResult.Err(new CaptureError.SessionBusUnavailable());

        try
        {
            using var connection = new Connection(sessionAddress);
            await connection.ConnectAsync();

            var uniqueName = connection.UniqueName;
            if (uniqueName == null) return new CaptureResult.Err(new CaptureError.SessionBusUnavailable());

            var token = $"tc{Random.Shared.Next(10000, 99999)}";
            var sender = uniqueName.TrimStart(':').Replace('.', '_');
            var requestPath = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";

            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            await connection.AddMatchAsync<string?>(
                new MatchRule { Type = MessageType.Signal, Path = requestPath, Interface = "org.freedesktop.portal.Request", Member = "Response" },
                ReadPortalResponse,
                HandlePortalResponse,
                ObserverFlags.None,
                readerState: null,
                handlerState: tcs
            );

            var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.portal.Desktop",
                path: "/org/freedesktop/portal/desktop",
                @interface: "org.freedesktop.portal.Screenshot",
                member: "Screenshot",
                signature: "sa{sv}"
            );
            writer.WriteString("");
            var arrayStart = writer.WriteArrayStart(DBusType.DictEntry);
            writer.WriteStructureStart();
            writer.WriteString("handle_token");
            writer.WriteVariantString(token);
            writer.WriteArrayEnd(arrayStart);
            connection.TrySendMessage(writer.CreateMessage());
            writer.Dispose();

            var path = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
            if (path == null) return new CaptureResult.Err(new CaptureError.PermissionDenied());

            try
            {
                return new CaptureResult.Ok(DecodeCropped(path, x, y, width, height));
            }
            finally
            {
                File.Delete(path);
            }
        }
        catch (TimeoutException)
        {
            return new CaptureResult.Err(new CaptureError.Timeout());
        }
        catch
        {
            return new CaptureResult.Err(new CaptureError.CaptureFailed());
        }
    }
}
