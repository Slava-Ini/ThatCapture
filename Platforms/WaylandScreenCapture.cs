using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tmds.DBus.Protocol;

namespace ThatCapture.Platforms;

internal sealed class WaylandScreenCapture : IScreenCapture
{
    public async Task<CapturedFrame?> CaptureAreaAsync(int x, int y, int width, int height)
    {
        var filePath = await TakeScreenshotAsync();
        if (filePath == null) return null;

        try
        {
            return DecodeCropped(filePath, x, y, width, height);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    // ImageSharp is used internally here only to decode the PNG returned by xdg-desktop-portal.
    // It does not appear in the public API.
    private static CapturedFrame? DecodeCropped(string filePath, int x, int y, int width, int height)
    {
        using var img = Image.Load<Bgra32>(filePath);
        img.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));

        int stride = width * 4;
        var pixels = new byte[stride * height];
        img.CopyPixelDataTo(MemoryMarshal.Cast<byte, Bgra32>(pixels.AsSpan()));

        return new CapturedFrame(pixels, width, height, stride, PixelFormat.Bgra8888);
    }

    private static async Task<string?> TakeScreenshotAsync()
    {
        using var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();

        var token = $"tc{Random.Shared.Next(10000, 99999)}";
        var sender = connection.UniqueName!.TrimStart(':').Replace('.', '_');
        var requestPath = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await connection.AddMatchAsync<string?>(
            new MatchRule { Type = MessageType.Signal, Path = requestPath, Interface = "org.freedesktop.portal.Request", Member = "Response" },
            static (Message message, object? _) =>
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
            },
            static (Exception? ex, string? path, object? _, object? state) =>
            {
                var tcs = (TaskCompletionSource<string?>)state!;
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetResult(ex != null ? null : path);
            },
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

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
    }
}
