namespace Engine.Geometry;

/// <summary>Dò viền đen/gần đen (letterbox/pillarbox) của hình stream. Chỉ nhận ảnh ĐÃ cắt về vùng client.</summary>
public static class ContentRectDetector
{
    /// <summary>Độ sáng (luma 0..255) tối đa còn tính là "đen". Video limited-range có thể ra ~16 thay vì 0.</summary>
    public const int BlackLumaMax = 24;

    /// <summary>Hàng/cột là nội dung khi ít nhất tỉ lệ này (phần trăm) điểm ảnh sáng hơn <see cref="BlackLumaMax"/>.</summary>
    public const int MinBrightPercent = 1;

    /// <summary>Dung sai tỉ lệ khung hình so với 16:9 (±1,5%).</summary>
    public const double AspectTolerance = 0.015;

    /// <param name="bgra">Điểm ảnh BGRA 8 bit.</param>
    /// <param name="stride">Số byte mỗi hàng (có thể lớn hơn width*4).</param>
    public static ContentDetection Detect(ReadOnlySpan<byte> bgra, int width, int height, int stride)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException($"Kích thước ảnh không hợp lệ: {width}x{height}.");
        if (stride < width * 4)
            throw new ArgumentException($"stride {stride} nhỏ hơn width*4 ({width * 4}).");
        if (bgra.Length < (long)stride * (height - 1) + width * 4)
            throw new ArgumentException("Mảng điểm ảnh ngắn hơn kích thước khai báo.");

        var whole = new ContentRect(0, 0, width, height);

        // Bước 1: hàng nội dung = ≥ MinBrightPercent% điểm sáng (trên toàn bề ngang).
        int top = -1, bottom = -1;
        for (int y = 0; y < height; y++)
        {
            int bright = 0;
            int row = y * stride;
            for (int x = 0; x < width; x++)
                if (Luma(bgra, row + x * 4) > BlackLumaMax) bright++;
            if (bright * 100 >= width * MinBrightPercent)
            {
                if (top < 0) top = y;
                bottom = y;
            }
        }
        if (top < 0) return new ContentDetection(whole, false); // toàn đen

        // Bước 2: cột nội dung, chỉ xét trong dải hàng đã tìm được.
        int rows = bottom - top + 1;
        int left = -1, right = -1;
        for (int x = 0; x < width; x++)
        {
            int bright = 0;
            for (int y = top; y <= bottom; y++)
                if (Luma(bgra, y * stride + x * 4) > BlackLumaMax) bright++;
            if (bright * 100 >= rows * MinBrightPercent)
            {
                if (left < 0) left = x;
                right = x;
            }
        }
        if (left < 0) return new ContentDetection(whole, false);

        var rect = new ContentRect(left, top, right - left + 1, rows);
        bool sixteenNine = IsSixteenByNine(rect.Width, rect.Height);
        if (sixteenNine) return new ContentDetection(rect, true);

        // NB2 — dự phòng: màn game tối, không có letterbox thật ⇒ rect co về vùng chữ. Nếu cả client
        // (ảnh đã cắt client) gần 16:9 và rect KHÔNG giống viền đen thật (vắt hết một chiều, hai lề đối
        // xứng) ⇒ coi cả client là nội dung. Viền đen đối xứng thật vẫn giữ nguyên (confident=false).
        if (IsSixteenByNine(width, height) && !LooksLikeBars(rect, width, height))
            return new ContentDetection(whole, true, FallbackClient);
        return new ContentDetection(rect, false);
    }

    public const string FallbackClient = "client";

    private static bool IsSixteenByNine(int w, int h)
        => h > 0 && Math.Abs((double)w / h - 16.0 / 9.0) / (16.0 / 9.0) <= AspectTolerance;

    /// <summary>Letterbox/pillarbox thật: rect vắt hết chiều cao (hoặc rộng) và hai lề còn lại đối xứng.</summary>
    private static bool LooksLikeBars(ContentRect r, int w, int h)
    {
        int right = w - (r.X + r.Width), bottom = h - (r.Y + r.Height);
        bool fullHeight = r.Y <= 1 && bottom <= 1;
        bool fullWidth = r.X <= 1 && right <= 1;
        int tolX = Math.Max(2, w / 200), tolY = Math.Max(2, h / 200);
        return (fullHeight && Math.Abs(r.X - right) <= tolX) || (fullWidth && Math.Abs(r.Y - bottom) <= tolY);
    }

    /// <summary>Luma nguyên 0..255 từ BGRA (hệ số Rec.709 làm tròn /256).</summary>
    private static int Luma(ReadOnlySpan<byte> p, int i)
        => (18 * p[i] + 183 * p[i + 1] + 54 * p[i + 2]) >> 8;
}
