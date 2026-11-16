using System.Text;
using static Engine.Capture.NativeMethods;

namespace Engine.Capture;

public sealed record WindowInfo(IntPtr Hwnd, string Title, int Width, int Height)
{
    public override string ToString() => $"HWND=0x{Hwnd.ToInt64():X} \"{Title}\" {Width}x{Height}";
}

public static class WindowFinder
{
    /// <summary>Cửa sổ top-level nhìn thấy (không cloaked) có tiêu đề chứa <paramref name="titlePart"/> (không phân biệt hoa thường).</summary>
    public static List<WindowInfo> FindByTitle(string titlePart)
    {
        var found = new List<WindowInfo>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsCloaked(hwnd)) return true;
            var title = GetTitle(hwnd);
            if (title.Length == 0 || title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) < 0) return true;
            GetWindowRect(hwnd, out var r);
            found.Add(new WindowInfo(hwnd, title, r.Width, r.Height));
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static WindowInfo? FromHwnd(IntPtr hwnd)
    {
        if (!IsWindow(hwnd)) return null;
        GetWindowRect(hwnd, out var r);
        return new WindowInfo(hwnd, GetTitle(hwnd), r.Width, r.Height);
    }

    private static bool IsCloaked(IntPtr hwnd)
        => DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;

    private static string GetTitle(IntPtr hwnd)
    {
        int len = GetWindowTextLengthW(hwnd);
        if (len <= 0) return "";
        var buf = new char[len + 1];
        int n = GetWindowTextW(hwnd, buf, buf.Length);
        return new string(buf, 0, n);
    }
}
