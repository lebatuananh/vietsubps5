using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Engine;

/// <summary>Đọc/ghi PNG bằng Windows.Graphics.Imaging (có sẵn qua TFM, không thêm gói).</summary>
public static class ImageIo
{
    public sealed record Bgra(byte[] Pixels, int Width, int Height)
    {
        public int Stride => Width * 4;
    }

    public static async Task WritePngAsync(string path, byte[] bgra, int width, int height)
    {
        using var buffer = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, buffer);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
            (uint)width, (uint)height, 96, 96, bgra);
        await encoder.FlushAsync();
        buffer.Seek(0);
        var reader = new DataReader(buffer.GetInputStreamAt(0));
        await reader.LoadAsync((uint)buffer.Size);
        var bytes = new byte[buffer.Size];
        reader.ReadBytes(bytes);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllBytesAsync(path, bytes);
    }

    /// <summary>Giải mã PNG sang BGRA8, phóng/thu bằng <paramref name="scale"/> (Fant) nếu ≠ 1.</summary>
    public static async Task<Bgra> ReadBgraAsync(string path, double scale = 1.0)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        using var stream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(stream.GetOutputStreamAt(0));
        writer.WriteBytes(bytes);
        await writer.StoreAsync();
        await writer.FlushAsync();
        writer.DetachStream();

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var transform = new BitmapTransform();
        uint w = decoder.PixelWidth, h = decoder.PixelHeight;
        if (Math.Abs(scale - 1.0) > 1e-9)
        {
            w = (uint)Math.Max(1, Math.Round(decoder.PixelWidth * scale));
            h = (uint)Math.Max(1, Math.Round(decoder.PixelHeight * scale));
            transform.ScaledWidth = w;
            transform.ScaledHeight = h;
            transform.InterpolationMode = BitmapInterpolationMode.Fant;
        }
        var provider = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform,
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
        return new Bgra(provider.DetachPixelData(), (int)w, (int)h);
    }

    public static async Task<(int Width, int Height)> ReadSizeAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        using var stream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(stream.GetOutputStreamAt(0));
        writer.WriteBytes(bytes);
        await writer.StoreAsync();
        writer.DetachStream();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        return ((int)decoder.PixelWidth, (int)decoder.PixelHeight);
    }
}
