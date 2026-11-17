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

Mã thoát: `0` ok · `1` tham số/dữ liệu (gồm `--hwnd` không phải số/0xHEX, thư mục `--input`/`--ocr`/`--golden` không tồn tại) · `2` không tìm thấy cửa sổ hoặc **≥ 2 cửa sổ khớp** (in danh sách, dùng `--hwnd`) · `3` cửa sổ thu nhỏ · `4` không nhận được khung nào sau `--timeout-ms` · `5` thiếu gói OCR `en-US` · `6` lỗi hệ thống/interop (gồm **cửa sổ đổi kích thước giữa lúc chụp**: khung không còn khớp frame pool ⇒ báo lỗi, không trả ảnh cắt cụt — chỉ lệnh `capture`; vòng `run` (Phase 3) **tạo lại frame pool** thay vì exit 6).

`--no-crop true` (chẩn đoán): giữ nguyên khung WGC gồm thanh tiêu đề — dùng để phân biệt "pixel đọc sai" với "cửa sổ nguồn đen".

## TFM và gói NuGet

- TFM `net10.0-windows10.0.22621.0` (cả `Engine` và `Engine.Tests`; project test tham chiếu project Exe nên phải cùng TFM). .NET 8 đã hết hỗ trợ 2026-11-10. WinRT (`Windows.Graphics.Capture`, `Windows.Media.Ocr`, `Windows.Graphics.Imaging`) lấy qua TFM (gói `Microsoft.Windows.SDK.NET.Ref`, tự restore từ NuGet; giấy phép theo https://aka.ms/WinSDKLicenseURL) — **không** thêm CsWinRT riêng.
- Giấy phép (đọc từ `.nuspec` đã restore):

| Gói | Dùng cho | Giấy phép |
|---|---|---|
| `Vortice.Direct3D11` 3.8.3, `Vortice.DXGI` 3.8.3 (kéo theo `Vortice.DirectX`, `Vortice.Mathematics`, `SharpGen.Runtime`, `SharpGen.Runtime.COM`) | Thiết bị D3D11 + staging texture để lấy pixel từ khung WGC | MIT |
| `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2 | Test | Apache-2.0 |
| `Microsoft.NET.Test.Sdk` 18.12.0 | Test | MIT |
| `Anthropic` 13.9.0 (SDK C# chính thức; kéo theo `Microsoft.Extensions.AI.Abstractions` 10.5.1, `System.Net.ServerSentEvents` 10.0.1, `System.Text.Json` 10.0.6, `System.Collections.Immutable`, `System.Diagnostics.DiagnosticSource`, `System.Threading.Tasks.Extensions`) | Gọi LLM dịch (Phase 3) | **Apache-2.0** (đọc `.nuspec`; không copyleft) |

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


---

# Phase 3 — chữ Việt thật lên TV

Giao thức PC ↔ TV (nguồn chân lý): [`../tv-overlay/README.md` mục *Giao thức*](../tv-overlay/README.md). Engine là **client**, TV là server cổng **47820**.

## Lệnh mới

| Lệnh | Việc |
|---|---|
| `pair --tv <ip> --code <XXXX-XXXX> [--port 47820]` | Ghi `engine.local.json` (host/port/mã đã chuẩn hoá — **mã không in ra**) rồi thử hello. Mã sai bảng chữ ⇒ exit 1 (không ghi); không kết nối được ⇒ exit 2; TV từ chối ⇒ exit 3 (cấu hình **đã ghi** — chạy lại `pair` với mã đúng) |
| `pattern [--send true] [--seconds 60]` | 4 item góc + nhãn 10%…90% để kiểm khớp góc trên TV thật. Không `--send` ⇒ in gói ra stdout |
| `replay --input <dir PNG> [--translator claude\|cache\|fake] [--send true] [--hold-ms 4000] [--out <dir>] [--verbose true]` | Cùng pipeline với `run`, nguồn = thư mục PNG (đồng hồ ảo; `--send` thì chạy thời gian thật). Ghi `<ảnh>.frame.json` (gói cuối đã gửi) + `misses.json`; in "gọi backend / HTTP thật / ký tự LLM" |
| `run [--window-title "PS Remote Play"] [--hwnd H] [--translator …] [--no-send true] [--verbose true] [--max-seconds N]` | Vòng chạy thật: WGC ~4 khung/s → chữ ký ô → Động/Đứng yên → OCR → bố cục → dịch → gửi TV. Cửa sổ thu nhỏ/không thấy ⇒ gửi `items:[]`, thử lại mỗi 2 s, **không thoát**. Ctrl+C (hoặc `--max-seconds`) ⇒ gửi `items:[]` rồi thoát 0 |
| `warm --input <dir PNG> [--translator claude\|fake] [--yes true]` | OCR + dịch các câu chưa có, chỉ ghi cache. **Tốn tiền** ⇒ in số câu + ký tự dự kiến, thoát 1 nếu thiếu `--yes` |
| `cache-export --out <file.tsv>` | Bảng `status, source, vi, short_vi, provider, attempts, last_error` (+ glossary) cho End User đọc |

`--translator`: `cache` (mặc định — chỉ glossary + cache, **0 gọi mạng**) · `fake` (`[VI] ` + gốc, cho test/smoke, 0 mạng) · `claude` (cần `ANTHROPIC_API_KEY`; **thiếu khoá ⇒ cảnh báo một dòng và chạy như `cache`**, không exception). Khoá chỉ đọc từ biến môi trường, **không bao giờ** log/ghi file.

## File dữ liệu (đường dẫn gốc = biến môi trường `VIETSUB_HOME`, mặc định `%LOCALAPPDATA%\VietSub`)

Test và smoke **bắt buộc** đặt `VIETSUB_HOME` = thư mục tạm. Xoá thư mục = về như mới.

| Đường dẫn | Nội dung |
|---|---|
| `engine.local.json` | `tv` {host, port, token} · `display` {coverMode, minFontPx, holdMs, fontScale} · `tuning` (mọi ngưỡng; mặc định = hằng số có tên trong `TuningConfig`). Thiếu khoá ⇒ mặc định |
| `cache/fc26.json` | Cache người dùng (`TranslationEntry`: `status` machine\|reviewed\|rejected, `attempts`, `lastError`, `promptVersion`, `glossaryVersion`…). Ghi nguyên tử (temp + rename). Chỉ ghi phần người dùng, không chép lại seed |
| `usage/YYYY-MM-DD.json` | `{"chars": n}` — ký tự **nguồn** đã gửi LLM trong ngày (giờ máy) |
| (repo) `engine/data/fc26/glossary.json`, `seed-cache.json` | Glossary (129 mục, có `short_vi`) + cache hạt giống (67 câu/nhãn Hộp thư & Career Mode, `status: machine` — **bản nháp Dev, chưa End User duyệt**). Copy ra thư mục output khi build |

`display`: `coverMode` `auto` (mặc định — độ lệch màu vành > `coverUniformThreshold` ⇒ `card`, ngược lại `solid`) \| `solid` \| `card` \| `none` · `minFontPx` 24 · `holdMs` 0 (TV áp) · `fontScale` **1,0** (**chỉ engine** nhân vào `fontPx`; 0,78 cũ làm chữ Việt chỉ ~56% chữ gốc). `coverMode` lạ ⇒ từ chối khi nạp cấu hình.

## Hành vi cần biết

- **Động/Đứng yên (N3)**: chữ ký = trung bình xám 16×9 ô trên contentRect; ô đổi khi |Δ| > `cellDiffThreshold` (6). Ô động liên tục ≥ 3 s ⇒ **ô nền** bị bỏ khỏi phép so (**B3**: với ô nền động có item đang vẽ, chỉ so điểm ảnh **trong bbox chữ** của item — nền chạy quanh chữ đứng yên không gỡ item ⇒ không nhấp nháy). Đổi ⇒ gỡ **ngay** item giao ô đổi và gửi gói; đứng yên ≥ `stableMs` (250) ⇒ OCR/dịch/gửi. Cùng chữ ký ⇒ không OCR lại, gửi lại gói cũ (seq mới) mỗi 1 s.
- **Chế độ trận (N4, NB7)**: chuyển động **kéo dài** — ≥ `matchModeMinBeats` (3) nhịp lấy mẫu có > 50% ô đổi trong `matchModeSustainMs` (3 s) ⇒ gửi `items:[]`, không OCR/dịch tới khi đứng yên liên tục ≥ 1,5 s. Một lần đổi trang toàn màn (menu → menu) chỉ là 1 nhịp nên không tính (hiện lại sau `stableMs`, không phải 1,5 s). Dòng **chỉ chữ hoa ≤ 3 từ hoặc ≤ 4 ký tự** không gọi LLM (chỉ glossary/cache).
- **Bố cục (D6, N1)**: tách dòng OCR thành đoạn khi khe giữa hai từ > `splitGapFactor` (1,2) × cao dòng; bỏ dòng ≤ 1 chữ cái/chỉ số/token icon; gộp đoạn văn (cùng mép trái ±0,6% bề rộng, cao lệch ≤ 35% — NB8, email hai dòng OCR 23/29 px, khe dọc ≤ 0,9× cao); `group`: `col-n` (cùng mép trái, ≥ 2 thành viên) → `row-n` (≥ 2 nhãn cùng hàng, khe ≤ 8% bề rộng) → `solo-n`. **Cùng nhóm ⇒ `fontPx` = trung vị** (hộp OCR dao động theo nét chữ).
- **Dịch (D8)**: chuẩn hoá khoảng trắng/dấu nháy; che biến thành slot `{N1}`… (số, tiền, %, ngày, giờ, `OVR`); tra glossary (không phân biệt hoa/thường) → cache (seed → người dùng) → LLM. Slot bản dịch phải **đúng bằng** slot nguồn. Câu gốc VIẾT HOA ⇒ bản dịch viết hoa. **Câu chưa có bản dịch không được vẽ.**
- **`maxLines` (Rev 1.4)**: nhãn một dòng gửi `1`; đoạn văn (gộp ≥ 2 dòng) gửi số dòng gốc + 2 (tối đa 6) — TV dựa vào đó để co cỡ trước khi thêm dòng.
- **`id` (B1 review 3a)**: băm(ô lưới + câu chuẩn hoá + số thứ tự lần xuất hiện của cặp (ô, câu) theo thứ tự đọc) ⇒ duy nhất trong gói, ổn định giữa các khung. Hộp bbox gửi TV được **nới `tuning.boxPadPx` (3 px/1080) mỗi phía** vì hộp OCR hay hụt vài px so với nét chữ (C1: không điểm ảnh gốc nào được ló ra — `room` bắt đầu từ hộp đã nới).
- **Lỗi API không tạm thời (B2)**: 400/401/403/404… = lỗi **cấu hình/ xác thực**, không phải "lô bị bỏ": **tắt backend LLM cả phiên** (log đúng một dòng `[llm] tắt backend…`), **không** tách đôi, **không** `attempts`, **hoàn** ký tự đã giữ chỗ — tối đa **1 request**. 429/5xx = tạm thời **và hoàn ký tự** (không tính tiền; NB3); 408/409/timeout = tạm thời, **giữ** ký tự. SDK `MaxRetries = 0` (NB2: engine tự lùi 30 s).
- **Một câu bị loại chỉ thử lại ở phiên sau (NB1)**; `usage/*.json` ghi nguyên tử, file hỏng/ sai kiểu ⇒ **coi như đã chạm trần** (fail-closed, NB4); `--translator fake` **không ghi cache người dùng, không trừ trần** (NB5); `Save` có khoá và dịch nền chạy một hàng đợi (NB6); `run` bắt mọi ngoại lệ lạ trong vòng, log rồi thử lại sau 2 s (NB13). `--no-send`/`--send`/`--yes`/`--verbose` nhận cả dạng trần lẫn `true` (NB11).
- **Chống gọi trả phí lặp (B1)**: câu bị loại (slot sai/thiếu) hoặc lô bị bỏ (`refusal`/`max_tokens`/parse lỗi) ⇒ `attempts++` **ghi vào cache qua phiên**; tổng ≤ 2 lần, sau đó chỉ thử lại khi `promptVersion`/`glossaryVersion` đổi. Lô bị bỏ ⇒ **tách đôi** rồi thử lại; `attempts` chỉ tăng cho câu ở lô đơn lẻ. Tập `inFlight` theo `sourceKey` (màn quay về không gửi lại). Lỗi mạng/429/5xx **không** tính `attempts`, lùi 30 s. Trần 100.000 ký tự nguồn/ngày (`tuning.dailyCapChars`) — chạm trần ⇒ ngừng gọi LLM tới hết ngày, cảnh báo một lần.
- **LLM (D9, N10)**: `claude-sonnet-5-5`, `OutputConfig` {Effort Low, JSON schema `{items:[{id, vi, short_vi?}]}` — mọi object `additionalProperties:false`, `short_vi` không `required`}, không prefill/temperature, `MaxTokens` 8000, `System` = hướng dẫn + glossary ổn định với `CacheControlEphemeral`; lô ≤ 40 câu; `StopReason` `refusal`/`max_tokens` ⇒ bỏ lô. Tách phần thuần (`ClaudePrompt`, `ClaudeBackend` + `IClaudeTransport` giả trong test) khỏi phần SDK (`AnthropicSdkTransport`) — **lượt 3a chưa gọi API thật**.
- **`room` (N5)**: hộp lớn nhất quanh bbox mà nền còn đồng nhất (độ lệch màu ≤ 24) với màu che, ≤ `roomMaxFactor` (2×) mỗi chiều, không lấn bbox item khác.
- **Đổi cỡ cửa sổ khi `run`**: tạo lại frame pool (`CaptureSession.Recreations`), log "đã tạo lại frame pool"; `capture` giữ exit 6. NB8 (giải phóng RCW/thiết bị) và NB9 (kiểm HRESULT DWM/`GetClientRect`/`ClientToScreen`) đã sửa.

## Bẫy mới (Phase 3)

- **Phiên Windows bị khoá/ngắt kết nối ⇒ WGC không phát khung nào** (`quser` hiện `Disc`, `LogonUI` chạy): cả `capture` lẫn `run` báo exit 4 / "không nhận được khung" dù mã đúng. Khi chạy `run` thật: đừng khoá máy, đừng để màn tắt (xem backlog lát X).
- Listener/ client chỉ dùng loopback trong smoke; `VIETSUB_HOME` luôn thư mục tạm.
- Mã ghép cặp không có `0`, `1`, `2`, `5`, `6`, `8`, `B`, `G`, `I`, `L`, `O`, `Q`, `S`, `Z` (bảng chữ cái `ACDEFHJKMNPRTUVWXY3479`).
