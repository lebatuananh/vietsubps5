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
/// Chụp một cửa sổ bằng Windows Graphics Capture (interop CreateForWindow — KHÔNG dùng TryCreateFromWindowId),
/// frame pool CreateFreeThreaded (console app không có DispatcherQueue). Trả ảnh ĐÃ cắt về vùng client.
/// </summary>
public static class WindowCapturer
{
    private static readonly Guid IidGraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid IidD3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    public static List<CapturedFrame> Capture(WindowInfo win, int frames, int intervalMs, int timeoutMs, bool cropClient = true)
    {
        if (!GraphicsCaptureSession.IsSupported())
            throw new EngineException(6, "Windows Graphics Capture không được hỗ trợ trên máy này.");
        if (IsIconic(win.Hwnd))
            throw new EngineException(3, $"Cửa sổ đang thu nhỏ (minimized) — WGC không có khung mới. Mở lại cửa sổ: {win}");

        var item = CreateItem(win.Hwnd);
        CreateDevice(out var d3dDevice, out var d3dContext);
        using var _dev = d3dDevice;
        using var _ctx = d3dContext;
        var winrtDevice = CreateWinRtDevice(d3dDevice);

        var poolSize = item.Size;
        using var pool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 3, poolSize);
        using var session = pool.CreateCaptureSession(item);
        session.IsCursorCaptureEnabled = false;
        session.StartCapture();

        var result = new List<CapturedFrame>();
        Direct3D11CaptureFrame? latest = null;
        try
        {
            var sw = Stopwatch.StartNew();
            void Drain()
            {
                Direct3D11CaptureFrame? f;
                while ((f = pool.TryGetNextFrame()) != null) { latest?.Dispose(); latest = f; }
            }

            while (true)
            {
                Drain();
                if (latest != null) break;
                if (sw.ElapsedMilliseconds > timeoutMs)
                    throw new EngineException(4,
                        $"Không nhận được khung nào sau {timeoutMs} ms từ {win}. " +
                        (IsIconic(win.Hwnd) ? "Cửa sổ đang thu nhỏ." : "Cửa sổ có thể không vẽ lại (nội dung đứng yên bị che/ tắt)."));
                Thread.Sleep(10);
            }

            for (int i = 0; i < frames; i++)
            {
                if (i > 0)
                {
                    var until = Stopwatch.StartNew();
                    while (until.ElapsedMilliseconds < intervalMs) { Drain(); Thread.Sleep(10); }
                }
                Drain();
                result.Add(ReadFrame(win, latest!, d3dDevice, d3dContext, cropClient, poolSize));
            }
        }
        finally
        {
            latest?.Dispose();
        }
        return result;
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
        Check(interop.CreateForWindow(hwnd, in IidGraphicsCaptureItem, out var itemPtr), "IGraphicsCaptureItemInterop.CreateForWindow");
        try { return MarshalInspectable<GraphicsCaptureItem>.FromAbi(itemPtr); }
        finally { Marshal.Release(itemPtr); }
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

    private static CapturedFrame ReadFrame(WindowInfo win, Direct3D11CaptureFrame frame, ID3D11Device dev, ID3D11DeviceContext ctx, bool cropClient, Windows.Graphics.SizeInt32 poolSize)
    {
        int fw = frame.ContentSize.Width, fh = frame.ContentSize.Height;
        // NB4: cửa sổ đổi cỡ giữa lúc chụp ⇒ khung không còn khớp pool (pool không được tạo lại ở Phase 1) ⇒ báo lỗi, không trả ảnh cắt cụt.
        if (fw != poolSize.Width || fh != poolSize.Height)
            throw new EngineException(6,
                $"Cửa sổ đổi kích thước giữa lúc chụp (khung {fw}x{fh}, pool {poolSize.Width}x{poolSize.Height}). " +
                "Không đổi cỡ/ phóng to cửa sổ khi đang chụp; chạy lại.");

        // Surface -> ID3D11Texture2D
        IntPtr surfPtr = MarshalInterface<IDirect3DSurface>.FromManaged(frame.Surface);
        IntPtr accPtr = IntPtr.Zero, texPtr = IntPtr.Zero;
        try
        {
            var accIid = typeof(IDirect3DDxgiInterfaceAccess).GUID;
            Check(Marshal.QueryInterface(surfPtr, in accIid, out accPtr), "QueryInterface(IDirect3DDxgiInterfaceAccess)");
            var access = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(accPtr);
            Check(access.GetInterface(in IidD3D11Texture2D, out texPtr), "IDirect3DDxgiInterfaceAccess.GetInterface");
        }
        finally
        {
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
        DwmGetWindowAttribute(win.Hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frameBounds, Marshal.SizeOf<RECT>());
        GetClientRect(win.Hwnd, out RECT client);
        var origin = new POINT { X = 0, Y = 0 };
        ClientToScreen(win.Hwnd, ref origin);
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
