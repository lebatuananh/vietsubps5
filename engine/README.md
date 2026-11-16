# engine/ — lõi xử lý (Windows)

Chỉ ghi thứ **đọc code rồi chạy thẳng vẫn dễ vấp** (CLAUDE.md). Phase 1 = spike có số đo: chụp cửa sổ → cắt client → OCR → chấm. Chưa có kết nối TV, dịch, cache, LLM.

## Build / chạy / test

Chạy **từ root repo**:

```
dotnet build engine
dotnet test engine
dotnet run --project engine/src/Engine -- <lệnh> [tham số]     # cách gọi chuẩn; "engine <lệnh>" trong doc là viết tắt của dòng này
```

| Lệnh | Việc |
|---|---|
| `capture --window-title <chuỗi con> \| --hwnd <số\|0xHEX> --out <dir> [--frames N=1] [--interval-ms 500] [--timeout-ms 5000]` | Chụp cửa sổ bằng WGC, **cắt về vùng client**, ghi `frame-NNN.png` + `capture.json` |
| `ocr --input <dir PNG> --out <dir> [--scale 1\|2]` | `Windows.Media.Ocr` `en-US` → `<ảnh>.ocr.json` (`--scale 2` = phóng 2× trước OCR) |
| `ocr-score --ocr <dir> --golden <dir>` | So `*.ocr.json` với `*.golden.json` (cùng tên không đuôi); in (a) (b) từng ảnh + tổng |

Mã thoát: `0` ok · `1` tham số/dữ liệu (gồm `--hwnd` không phải số/0xHEX, thư mục `--input`/`--ocr`/`--golden` không tồn tại) · `2` không tìm thấy cửa sổ hoặc **≥ 2 cửa sổ khớp** (in danh sách, dùng `--hwnd`) · `3` cửa sổ thu nhỏ · `4` không nhận được khung nào sau `--timeout-ms` · `5` thiếu gói OCR `en-US` · `6` lỗi hệ thống/interop (gồm **cửa sổ đổi kích thước giữa lúc chụp**: khung không còn khớp frame pool ⇒ báo lỗi, không trả ảnh cắt cụt — pool chưa được tạo lại, nợ Phase 3).

`--no-crop true` (chẩn đoán): giữ nguyên khung WGC gồm thanh tiêu đề — dùng để phân biệt "pixel đọc sai" với "cửa sổ nguồn đen".

## TFM và gói NuGet

- TFM `net10.0-windows10.0.22621.0` (cả `Engine` và `Engine.Tests`; project test tham chiếu project Exe nên phải cùng TFM). .NET 8 đã hết hỗ trợ 2026-11-10. WinRT (`Windows.Graphics.Capture`, `Windows.Media.Ocr`, `Windows.Graphics.Imaging`) lấy qua TFM (gói `Microsoft.Windows.SDK.NET.Ref`, tự restore từ NuGet; giấy phép theo https://aka.ms/WinSDKLicenseURL) — **không** thêm CsWinRT riêng.
- Giấy phép (đọc từ `.nuspec` đã restore):

| Gói | Dùng cho | Giấy phép |
|---|---|---|
| `Vortice.Direct3D11` 3.8.3, `Vortice.DXGI` 3.8.3 (kéo theo `Vortice.DirectX`, `Vortice.Mathematics`, `SharpGen.Runtime`, `SharpGen.Runtime.COM`) | Thiết bị D3D11 + staging texture để lấy pixel từ khung WGC | MIT |
| `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2 | Test | Apache-2.0 |
| `Microsoft.NET.Test.Sdk` 18.12.0 | Test | MIT |

- **xUnit v2 cố ý** (không v3): v3 chạy trên Microsoft.Testing.Platform, `dotnet test` của .NET 10 cần bật chế độ riêng qua `global.json`.
- Interop WinRT tự khai báo (`Capture/NativeMethods.cs`): `IGraphicsCaptureItemInterop`, `IDirect3DDxgiInterfaceAccess`, `RoGetActivationFactory`, `CreateDirect3D11DeviceFromDXGIDevice`. **Không** dùng `GraphicsCaptureItem.TryCreateFromWindowId` (cần package manifest).

## Bẫy đã gặp / đã biết

- **DPI**: `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)` ở dòng đầu `Main` (log `DPI PerMonitorV2: set=… hiệu lực=…`). Manifest nhúng trong apphost không đáng tin khi chạy qua `dotnet run`. Không khai ⇒ `GetClientRect`/`ClientToScreen` ảo hoá, lệch khung WGC.
- **WGC chỉ phát khung khi nội dung đổi** ⇒ `--frames N` = lấy mẫu N lần cách `--interval-ms` từ **khung mới nhất**; engine giữ khung mới nhất bằng cách rút cạn frame pool (`CreateFreeThreaded` + polling `TryGetNextFrame`, không dùng `FrameArrived`). Console app không có `DispatcherQueue` ⇒ `Create` thường thì không có sự kiện.
- **Cửa sổ thu nhỏ** ⇒ không có khung: engine báo exit 3 ngay (không treo). Viền vàng khi đang chụp là bình thường. HD color có thể không phải BGRA8 (khung đang yêu cầu `B8G8R8A8UIntNormalized`).
- **Khung WGC theo cửa sổ gồm cả thanh tiêu đề/viền**; khung = `DWMWA_EXTENDED_FRAME_BOUNDS` (không gồm vùng đổ bóng vô hình của `GetWindowRect`: cửa sổ 1936×1119 → khung 1922×1112). Offset client = `ClientToScreen(0,0) − EXTENDED_FRAME_BOUNDS.TopLeft` (đã kiểm với cửa sổ Win32 chuẩn: client 1920×1080 tại (1,31)). **Cửa sổ tự vẽ thanh tiêu đề bên trong vùng client (kiểu Photos, có thể cả PS Remote Play) vẫn còn thanh đó sau khi cắt** — chưa kiểm với PS Remote Play.
- **Cửa sổ dùng renderer GPU có thể bị WGC chụp ra ĐEN dù thanh tiêu đề vẫn hiện** (đã gặp: `ffplay` mặc định trên máy dev — meanLuma 0,0). `SDL_RENDER_DRIVER=software` cho ảnh đúng. Khi `meanLuma` ≈ 0 hãy nghi nguồn, không nghi engine (so với `--no-crop true`).
- **`ocr`**: `Windows.Media.Ocr` cần gói `en-US`; thiếu ⇒ exit 5 + lệnh cài. Ảnh lớn hơn `OcrEngine.MaxImageDimension` bị thu nhỏ, toạ độ quy về ảnh gốc, `scaleApplied` ghi lại.
- **Thứ tự dòng OCR theo CỘT, không theo hàng** (danh sách 3 cột): vì thế (b) đã đổi sang đa tập hợp, không xét thứ tự (xem *Định nghĩa*).
- **PowerShell 5.1**: `>` làm hỏng nhị phân (PNG); script `.ps1` đọc bằng ANSI nên ký tự không ASCII (€, dấu tiếng Việt) trong file script bị hỏng — dùng `[char]0x20AC` hoặc file UTF-8 có BOM.
- `_local/` (F3 video, F4 ảnh TV) bị gitignore ⇒ **không có trên bản clone mới** — tạo lại thư mục `engine/tests/fixtures/fc26/inbox/_local/{f3-video,f4-tv-photo}` khi cần.

## Định nghĩa

- `confident` (`DetectContentRect`) = "tìm được rect hợp lý **và** gần 16:9" (dung sai ±1,5%) — không phải "phát hiện đáng tin". Cửa sổ 4:3 thật cho rect đúng nhưng `confident=false`. Hằng số (`BlackLumaMax=24`, `MinBrightPercent=1`, `AspectTolerance=0.015`) đặt trước, **chưa chỉnh bằng dữ liệu thật** — chỉnh sau khi có F1.
- **Dự phòng `fallback:"client"`** (NB2): rect dò được không 16:9 mà cả ảnh (đã cắt client) gần 16:9 **và** rect không giống viền đen thật (viền thật = vắt hết một chiều và hai lề đối xứng) ⇒ coi cả client là nội dung, `confident=true`, `fallback="client"` — để màn game tối (chữ ở giữa, không chi tiết ở mép) không bị co rect. Viền đen đối xứng thật (vd pillarbox 1500×1080 trong ảnh 1920×1080) vẫn `confident=false`. Không confident và không có dự phòng ⇒ `fallback=null`, **`ocr` chuẩn hoá theo cả ảnh** (không theo rect không confident). `confident=false` ⇒ xem PNG trước khi kết luận.
- "golden" = **ground truth chép tay** (không phải kết quả OCR ghi lại).
- `ScoreOcr`: chuẩn hoá khoảng trắng, giữ hoa/thường và dấu câu. (a) đoạn golden khớp = chuỗi con liên tục **theo biên từ** (ký tự liền kề không phải `char.IsLetterOrDigit`, hoặc đầu/cuối chuỗi) trải trên ≤ 3 dòng OCR liền nhau, tính **bội số**: đoạn golden xuất hiện k lần cần ≥ k lần xuất hiện **riêng biệt, không chồng lấn** trong văn bản OCR (dòng nối bằng 1 dấu cách) — `Board`×2 mà OCR đọc lần hai `B0ard` ⇒ 1/2. (b) độ phủ từ theo **đa tập hợp**, không xét thứ tự: mỗi từ golden khớp tối đa một lần với một từ OCR của cùng ảnh (chữ hoa/thường, dấu câu giữ nguyên) / tổng từ golden. Golden rỗng ⇒ "không xác định".

## Schema output chẩn đoán (Phase 1 — Phase 3 **không** đọc như contract; giao thức PC → TV là `OverlayFrame`, chốt ở Phase 3)

`capture.json`:
```json
{ "window": {"hwnd":"0x…","title":"…","width":0,"height":0},
  "dpiPerMonitorV2": true,
  "wgcFrameSize": {"width":0,"height":0},
  "clientRect": {"x":0,"y":0,"width":0,"height":0},   // trong khung WGC
  "clientOffset": {"x":0,"y":0},
  "contentRect": {"x":0,"y":0,"width":0,"height":0},  // trong ảnh đã cắt client, từ khung đầu
  "confident": true, "fallback": null,    // "client" khi dùng dự phòng NB2
  "meanLuma": 0.0,
  "frames": [{"file":"frame-001.png","capturedAtUtc":"…","meanLuma":0.0}] }
```
`capturedAtUtc` = thời điểm **lấy mẫu** (`DateTime.UtcNow` lúc đọc), **không** phải thời điểm WGC tạo khung: màn tĩnh cho N mốc giờ khác nhau nhưng là **cùng một khung** (PNG giống hệt).

`<ảnh>.ocr.json`: `{ image, imageSize{width,height}, contentRect (rect ĐÃ DÙNG để chuẩn hoá: cả ảnh nếu không confident), confident, fallback, scaleApplied, variant("scale1"|"scale2"), lines:[{ text, boxPx{x,y,w,h}, box01{x,y,w,h} }] }` — `boxPx` theo ảnh **gốc đã cắt client** (kể cả biến thể 2×), `box01` chuẩn hoá theo `contentRect`.
`<ảnh>.golden.json`: mảng chuỗi, xếp theo **thứ tự đọc** (trên xuống dưới; trong hàng, trái sang phải).

## Fixture

`engine/tests/fixtures/fc26/inbox/`: `f1-remoteplay/` (PNG từ `engine capture`, ≤ 3 MB/ảnh) · `golden/` · `f2-ps5/` (≤ 5 MB/ảnh; 4K ⇒ chuyển 1080p) · `f3-frames/` · `_local/f3-video/`, `_local/f4-tv-photo/` (**không commit**). Ảnh tổng hợp của Phase 1 nằm ở `project-docs/…/playtest/epic1-phase1/dev/synthetic/` — không phải fixture FC 26.
