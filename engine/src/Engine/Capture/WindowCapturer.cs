using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine.Geometry;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using static Engine.Capture.NativeMethods;

namespace Engine.Capture;

/// <summary>Lỗi có mã thoát riêng (exit ≠ 0) và thông điệp rõ.</summary>
public sealed class EngineException(int exitCode, string message) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}

public sealed record CapturedFrame(
    byte[] Pixels, int Width, int Height,
    DateTime CapturedAtUtc,
    int WgcWidth, int WgcHeight,
    ContentRect ClientRectInFrame,
    int ClientOffsetX, int ClientOffsetY);

/// <summary>
/// Phiên chụp liên tục một cửa sổ bằng Windows Graphics Capture (interop CreateForWindow — KHÔNG dùng TryCreateFromWindowId),
/// frame pool CreateFreeThreaded (console app không có DispatcherQueue). Trả ảnh ĐÃ cắt về vùng client.
/// Phase 3: <c>recreateOnResize</c> = true (vòng <c>run</c>) ⇒ cửa sổ đổi cỡ thì tạo lại frame pool thay vì exit 6;
/// false (lệnh <c>capture</c>) giữ hành vi cũ exit 6. NB8: giải phóng RCW/ thiết bị; NB9: kiểm HRESULT/ kết quả DWM, GetClientRect.
/// </summary>
public sealed class CaptureSession : IDisposable
{
    private static readonly Guid IidGraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid IidD3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    private readonly WindowInfo _win;
    private readonly bool _cropClient, _recreateOnResize;
    private readonly GraphicsCaptureItem _item;
    private readonly ID3D11Device _dev;
    private readonly ID3D11DeviceContext _ctx;
    private readonly IDirect3DDevice _winrtDevice;
    private readonly Direct3D11CaptureFramePool _pool;
    private readonly GraphicsCaptureSession _session;
    private Windows.Graphics.SizeInt32 _poolSize;
    private Direct3D11CaptureFrame? _latest;

    public int Recreations { get; private set; }

    public CaptureSession(WindowInfo win, bool cropClient = true, bool recreateOnResize = false)
    {
        _win = win; _cropClient = cropClient; _recreateOnResize = recreateOnResize;
        if (!GraphicsCaptureSession.IsSupported())
            throw new EngineException(6, "Windows Graphics Capture không được hỗ trợ trên máy này.");
        if (IsIconic(win.Hwnd))
            throw new EngineException(3, $"Cửa sổ đang thu nhỏ (minimized) — WGC không có khung mới. Mở lại cửa sổ: {win}");

        _item = CreateItem(win.Hwnd);
        CreateDevice(out _dev, out _ctx);
        _winrtDevice = CreateWinRtDevice(_dev);
        _poolSize = _item.Size;
        _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(_winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 3, _poolSize);
        _session = _pool.CreateCaptureSession(_item);
        _session.IsCursorCaptureEnabled = false;
        _session.StartCapture();
    }

    private void Drain()
    {
        Direct3D11CaptureFrame? f;
        while ((f = _pool.TryGetNextFrame()) != null) { _latest?.Dispose(); _latest = f; }
    }

    /// <summary>Khung mới nhất (đã cắt client). Chờ tới <paramref name="timeoutMs"/> nếu chưa có khung nào (exit 4).</summary>
    public CapturedFrame Grab(int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            Drain();
            if (_latest != null)
            {
                int fw = _latest.ContentSize.Width, fh = _latest.ContentSize.Height;
                if (fw == _poolSize.Width && fh == _poolSize.Height) break;
                if (!_recreateOnResize)
                    // NB4 (capture): không trả ảnh cắt cụt.
                    throw new EngineException(6,
                        $"Cửa sổ đổi kích thước giữa lúc chụp (khung {fw}x{fh}, pool {_poolSize.Width}x{_poolSize.Height}). " +
                        "Không đổi cỡ/ phóng to cửa sổ khi đang chụp; chạy lại.");
                // Phase 3 (run): tạo lại frame pool theo cỡ mới, bỏ khung cũ, chờ khung kế.
                _pool.Recreate(_winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 3, _latest.ContentSize);
                _poolSize = _latest.ContentSize;
                _latest.Dispose(); _latest = null;
                Recreations++;
            }
            if (sw.ElapsedMilliseconds > timeoutMs)
                throw new EngineException(4,
                    $"Không nhận được khung nào sau {timeoutMs} ms từ {_win}. " +
                    (IsIconic(_win.Hwnd) ? "Cửa sổ đang thu nhỏ." : "Cửa sổ có thể không vẽ lại (nội dung đứng yên bị che/ tắt)."));
            Thread.Sleep(10);
        }
        return ReadFrame(_win, _latest!, _dev, _ctx, _cropClient);
    }

    public void Dispose()
    {
        _latest?.Dispose(); _latest = null;
        _session.Dispose();
        _pool.Dispose();
        (_winrtDevice as IDisposable)?.Dispose();
        _ctx.Dispose();
        _dev.Dispose();
    }

    // ---------------------------------------------------------------- interop

    private static GraphicsCaptureItem CreateItem(IntPtr hwnd)
    {
        const string cls = "Windows.Graphics.Capture.GraphicsCaptureItem";
        Check(WindowsCreateString(cls, cls.Length, out var hs), "WindowsCreateString");
        IntPtr factory;
        try
        {
            var iid = typeof(IGraphicsCaptureItemInterop).GUID;
            Check(RoGetActivationFactory(hs, in iid, out factory), "RoGetActivationFactory(GraphicsCaptureItem)");
        }
        finally { WindowsDeleteString(hs); }

        var interop = (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factory);
        Marshal.Release(factory);
        try
        {
            Check(interop.CreateForWindow(hwnd, in IidGraphicsCaptureItem, out var itemPtr), "IGraphicsCaptureItemInterop.CreateForWindow");
            try { return MarshalInspectable<GraphicsCaptureItem>.FromAbi(itemPtr); }
            finally { Marshal.Release(itemPtr); }
        }
        finally { Marshal.ReleaseComObject(interop); } // NB8
    }

    private static void CreateDevice(out ID3D11Device device, out ID3D11DeviceContext context)
    {
        var hr = Vortice.Direct3D11.D3D11.D3D11CreateDevice(
            null!, Vortice.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport,
            null!, out device!, out context!);
        if (hr.Failure) throw new EngineException(6, $"D3D11CreateDevice thất bại: {hr}");
    }

    private static IDirect3DDevice CreateWinRtDevice(ID3D11Device d3d)
    {
        using var dxgi = d3d.QueryInterface<IDXGIDevice>();
        Check(CreateDirect3D11DeviceFromDXGIDevice(dxgi.NativePointer, out var ptr), "CreateDirect3D11DeviceFromDXGIDevice");
        try { return MarshalInterface<IDirect3DDevice>.FromAbi(ptr); }
        finally { Marshal.Release(ptr); }
    }

    private static CapturedFrame ReadFrame(WindowInfo win, Direct3D11CaptureFrame frame, ID3D11Device dev, ID3D11DeviceContext ctx, bool cropClient)
    {
        int fw = frame.ContentSize.Width, fh = frame.ContentSize.Height;

        // Surface -> ID3D11Texture2D
        IntPtr surfPtr = MarshalInterface<IDirect3DSurface>.FromManaged(frame.Surface);
        IntPtr accPtr = IntPtr.Zero, texPtr = IntPtr.Zero;
        IDirect3DDxgiInterfaceAccess? access = null;
        try
        {
            var accIid = typeof(IDirect3DDxgiInterfaceAccess).GUID;
            Check(Marshal.QueryInterface(surfPtr, in accIid, out accPtr), "QueryInterface(IDirect3DDxgiInterfaceAccess)");
            access = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(accPtr);
            Check(access.GetInterface(in IidD3D11Texture2D, out texPtr), "IDirect3DDxgiInterfaceAccess.GetInterface");
        }
        finally
        {
            if (access != null) Marshal.ReleaseComObject(access); // NB8
            if (accPtr != IntPtr.Zero) Marshal.Release(accPtr);
            MarshalInterface<IDirect3DSurface>.DisposeAbi(surfPtr);
        }

        using var tex = new ID3D11Texture2D(texPtr);
        var desc = tex.Description;
        desc.Usage = ResourceUsage.Staging;
        desc.BindFlags = BindFlags.None;
        desc.CPUAccessFlags = CpuAccessFlags.Read;
        desc.MiscFlags = ResourceOptionFlags.None;
        using var staging = dev.CreateTexture2D(desc);
        ctx.CopyResource(staging, tex);

        int texW = (int)desc.Width, texH = (int)desc.Height;
        fw = Math.Min(fw, texW); fh = Math.Min(fh, texH);
        var full = new byte[fw * fh * 4];
        var map = ctx.Map(staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            for (int y = 0; y < fh; y++)
                Marshal.Copy(map.DataPointer + y * (int)map.RowPitch, full, y * fw * 4, fw * 4);
        }
        finally { ctx.Unmap(staging, 0); }

        // Cắt về vùng client: offset client trong khung WGC = ClientToScreen(0,0) - EXTENDED_FRAME_BOUNDS.TopLeft
        // NB9: kiểm kết quả mọi lệnh gọi (cửa sổ đóng giữa chừng ⇒ lỗi rõ thay vì offset sai).
        int hr = DwmGetWindowAttribute(win.Hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frameBounds, Marshal.SizeOf<RECT>());
        if (hr != 0) throw new EngineException(6, $"DwmGetWindowAttribute(EXTENDED_FRAME_BOUNDS) thất bại (HRESULT 0x{hr:X8}) — cửa sổ đã đóng?");
        if (!GetClientRect(win.Hwnd, out RECT client)) throw new EngineException(6, "GetClientRect thất bại — cửa sổ đã đóng?");
        var origin = new POINT { X = 0, Y = 0 };
        if (!ClientToScreen(win.Hwnd, ref origin)) throw new EngineException(6, "ClientToScreen thất bại — cửa sổ đã đóng?");
        int offX = origin.X - frameBounds.Left, offY = origin.Y - frameBounds.Top;
        int x0 = Math.Max(0, offX), y0 = Math.Max(0, offY);
        int x1 = Math.Min(fw, offX + client.Width), y1 = Math.Min(fh, offY + client.Height);
        if (!cropClient) { x0 = 0; y0 = 0; x1 = fw; y1 = fh; } // chẩn đoán: giữ nguyên khung WGC (gồm thanh tiêu đề)
        if (x1 <= x0 || y1 <= y0)
            throw new EngineException(6, $"Vùng client rỗng sau khi cắt (offset {offX},{offY}, client {client.Width}x{client.Height}, khung {fw}x{fh}).");

        int cw = x1 - x0, ch = y1 - y0;
        var cropped = new byte[cw * ch * 4];
        for (int y = 0; y < ch; y++)
            Buffer.BlockCopy(full, ((y0 + y) * fw + x0) * 4, cropped, y * cw * 4, cw * 4);

        return new CapturedFrame(cropped, cw, ch, DateTime.UtcNow, fw, fh,
            new ContentRect(x0, y0, cw, ch), offX, offY);
    }

    private static void Check(int hr, string what)
    {
        if (hr < 0) throw new EngineException(6, $"{what} thất bại (HRESULT 0x{hr:X8}).");
    }
}

/// <summary>Lệnh <c>capture</c>: lấy mẫu N khung từ <see cref="CaptureSession"/> (giữ exit 6 khi đổi cỡ).</summary>
public static class WindowCapturer
{
    public static List<CapturedFrame> Capture(WindowInfo win, int frames, int intervalMs, int timeoutMs, bool cropClient = true)
    {
        using var session = new CaptureSession(win, cropClient, recreateOnResize: false);
        var result = new List<CapturedFrame>();
        for (int i = 0; i < frames; i++)
        {
            if (i > 0) Thread.Sleep(intervalMs);
            result.Add(session.Grab(timeoutMs));
        }
        return result;
    }
}
