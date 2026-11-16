namespace Engine.Geometry;

public static class Normalizer
{
    /// <summary>
    /// Đổi box pixel sang box 0..1 theo vùng nội dung. Kẹp HAI GÓC về [0,1] rồi tính lại w,h (≥ 0).
    /// Rect rỗng (w hoặc h ≤ 0) ⇒ <see cref="ArgumentException"/> có thông điệp.
    /// </summary>
    public static Box Normalize(Box boxPx, ContentRect contentRect)
    {
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
            throw new ArgumentException(
                $"Vùng nội dung rỗng ({contentRect.Width}x{contentRect.Height}); không thể chuẩn hoá toạ độ.",
                nameof(contentRect));

        double x1 = Clamp01((boxPx.X - contentRect.X) / contentRect.Width);
        double y1 = Clamp01((boxPx.Y - contentRect.Y) / contentRect.Height);
        double x2 = Clamp01((boxPx.X + boxPx.W - contentRect.X) / contentRect.Width);
        double y2 = Clamp01((boxPx.Y + boxPx.H - contentRect.Y) / contentRect.Height);
        return new Box(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));
    }

    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
}
