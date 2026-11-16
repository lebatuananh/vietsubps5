namespace Engine.Geometry;

/// <summary>Hình chữ nhật theo pixel nguyên (gốc trên-trái).</summary>
public readonly record struct ContentRect(int X, int Y, int Width, int Height);

/// <summary>Hộp chữ nhật theo số thực (pixel hoặc chuẩn hoá 0..1 tuỳ ngữ cảnh).</summary>
public readonly record struct Box(double X, double Y, double W, double H);

/// <summary>
/// Kết quả dò viền. <see cref="Confident"/> = "tìm được rect hợp lý VÀ gần 16:9"
/// (không phải "phát hiện đáng tin": cửa sổ 4:3 thật vẫn cho rect đúng nhưng Confident=false).
/// </summary>
public readonly record struct ContentDetection(ContentRect Rect, bool Confident, string? Fallback = null)
{
    /// <summary>Rect dùng để chuẩn hoá toạ độ: rect không confident thì KHÔNG dùng — dùng cả ảnh.</summary>
    public ContentRect RectForNormalization(int imageWidth, int imageHeight)
        => Confident ? Rect : new ContentRect(0, 0, imageWidth, imageHeight);
}
