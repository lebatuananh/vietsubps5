REVIEW-RE2A DONE
# 261114 — Epic 1 Phase 1 — Review lượt 2a (Bước 1–3, `engine/`)

Reviewer: session độc lập, không viết code này. Nguồn chân lý: [Phase doc](../261114-epic1-phase1.md) Rev 2.2 + 2 dòng Rev 2.3. Đối chiếu với [dev report 2a](261114-epic1-phase1-dev-report-2a.md). Evidence của Reviewer: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/reviewer/` (viết tắt `<rv>/`). Mọi ảnh và golden đều do Reviewer tự tạo (PIL, Arial) và chụp từ cửa sổ do Reviewer tự bật. **Không** dùng output của Dev làm evidence.

## Verdict

**Verdict: PASS** — không có finding blocking.

Blocking: **không có**.

Lý do PASS: mọi quyết định BINDING trong phạm vi 2a (G1–G9, G12, 1.2 #4/#5, 1.3, D3D11, xUnit, kẹt interop) đều có code tương ứng (`file:line` ở bảng dưới). Build sạch (0 warning, build lại toàn bộ). 21/21 test xanh. Smoke 3, 4, 5 tái lập được bằng dữ liệu riêng của Reviewer. Các lỗi tìm được đều **không** làm hỏng số đo R-C/R-D của Phase 1, nhưng có 3 điểm (NB1–NB3) Leader nên xử lý **trước Bước 7**, vì chúng có thể làm kết luận về PS Remote Play thật bị sai.

## Đối chiếu phạm vi

- Yêu cầu BINDING/kế hoạch trong phạm vi 2a: 20. Đã có code: 20. Riêng (b) theo Rev 2.3 là follow-up đã lên lịch, **không** phải lỗi của Dev (xem NB6).
- File trong diff: 24 file (`git ls-files --others --exclude-standard engine`, `<rv>/09-gitignore-check.txt`) và `.gitignore` root. Không file nào nằm ngoài phạm vi. Không đụng `tv-overlay/`.

### Bảng quyết định ↔ code

| # | Quyết định | Kết quả | Chỗ trong code |
|---|---|---|---|
| G1 | `--frames N` / `--interval-ms` 500 / `--timeout-ms` 5000; không có khung ⇒ exit ≠ 0; `capturedAtUtc` mỗi khung | OK (có lưu ý NB5) | `Commands.cs:31-34` (mặc định), `WindowCapturer.cs:66-75` (timeout ⇒ exit 4), `:77-86` (lấy mẫu từ khung mới nhất), `:189` (thời điểm) |
| G2 | Chỉ cửa sổ visible + không cloaked; ≠ 1 khớp ⇒ liệt kê, exit ≠ 0; `--hwnd` | OK | `WindowFinder.cs:19`, `Commands.cs:50-55`, `Commands.cs:37-45` |
| G3 | Hằng số có tên; `confident` = rect hợp lý **và** gần 16:9; ghi README | OK | `ContentRectDetector.cs:7,10,13,60-64`; `Geometry.cs:9-12`; `README.md:53` |
| G4 | Biên từ = `!char.IsLetterOrDigit` hoặc đầu/cuối chuỗi | OK | `OcrScorer.cs:76-90` |
| G5 | Kẹp hai góc rồi tính lại w,h ≥ 0 | OK | `Normalizer.cs:16-20` |
| G6 | Schema 3 file trong README; không phải contract; golden = ground truth chép tay | OK | `README.md:51-71` |
| G7 | Quy tắc golden F1 | Chưa áp dụng (chưa có F1) | — |
| G8 | Ảnh tổng hợp ở `dev/synthetic/`, ghi rõ "không phải R-D" | OK | `dev/06-smoke5-ocr-score.txt` dòng 1, `README.md:75` |
| G9 | `engine/Engine.slnx`, `src/Engine`, `tests/Engine.Tests`; `ffplay` làm cửa sổ thay thế | OK | `Engine.slnx:1-8` |
| G12 | `meanLuma` trong `capture.json` | OK | `Commands.cs:22,71,85` |
| 1.2 #5 | DPI ở **dòng đầu** `Main`, có log | OK (nhưng chưa từng được thử ở scale > 100%, xem NB1) | `Program.cs:20-25` |
| N2 | `CreateFreeThreaded` | OK | `WindowCapturer.cs:49-50` |
| R2.3 | Interop `CreateForWindow`, không dùng `TryCreateFromWindowId` | OK — grep chỉ thấy tên này trong comment `WindowCapturer.cs:28` | `NativeMethods.cs:61-66`, `WindowCapturer.cs:97-114` |
| N1 | Cắt client **trước** `DetectContentRect` | OK | `WindowCapturer.cs:172-190` cắt, sau đó mới `Commands.cs:77` gọi `Detect` |
| N3/Bước 3 | Offset = `ClientToScreen` − `EXTENDED_FRAME_BOUNDS` | OK | `WindowCapturer.cs:173-177` |
| Bước 3 | OCR `en-US`, `null` ⇒ exit 5; ảnh > `MaxImageDimension` ⇒ thu nhỏ, toạ độ quy về ảnh gốc | OK | `OcrRunner.cs:30-35`, `:45-51`, `:71` |
| D3D11 | Vortice.Direct3D11 + DXGI (MIT); tự khai báo interop WinRT | OK | `Engine.csproj:15-16`; giấy phép đã kiểm từ `.nuspec`: Vortice.* MIT, SharpGen.Runtime[.COM] 2.4.2-beta MIT, xunit 2.9.3 Apache-2.0, runner 2.8.2 Apache-2.0, Test.Sdk 18.12.0 MIT. Khớp `README.md:30-34` |
| xUnit | v2 2.9.x | OK | `Engine.Tests.csproj:18-19` |
| Bước 1 | TFM `net10.0-windows10.0.22621.0` + lý do; cách gọi chuẩn; bẫy WGC/DPI | OK | `Engine.csproj:5`; `README.md:9-13, 27, 41-49` |
| D6/N11/N18 | `.gitignore`: `*.local.json`, `engine/tests/fixtures/**/_local/`; `bin/ obj/` | OK | `.gitignore:17, 27-28, 34` (`git check-ignore -v` trong `<rv>/09-gitignore-check.txt`) |
| 1.3 | `.gitkeep` ở 4 thư mục fixture được commit; README nói `_local/` không có trên bản clone | OK | `README.md:49` |
| Kẹt interop | Không dùng `GraphicsCapturePicker` | OK | grep không thấy |

### TDD

- Bảng doc có 18 case cho phần engine (6 + 4 + 8). Code có 21 test. Đủ cả 18 case; thêm 2 ca biên từ (`OcrScorerTests.cs:29-41`) và tách ca rect rỗng thành 2 dòng `Theory` (`NormalizerTests.cs:48-56`). Số liệu khớp bảng: letterbox (0,60,1920,1080) ±1 (`ContentRectDetectorTests.cs:45-47`), pillarbox (40,0,1920,1080) (`:54-56`), sai 1 ký tự ⇒ (a) 0/1, (b) 2/3 và có dòng OCR gần nhất (`OcrScorerTests.cs:53-63`), không ghép quá 3 dòng (`:80-87`), golden rỗng ⇒ `null` (`:90-97`).
- Không thấy assertion nào bị nới. Ca "không viền" so khớp đúng tuyệt đối (`tol 0`, `ContentRectDetectorTests.cs:38`). Ca "sai 1 ký tự" còn thêm một dòng OCR gây nhiễu (`"Squad Hub"`) để chứng minh chọn đúng dòng gần nhất. Đây là bản chặt hơn doc.
- Bằng chứng red: `dev/01-tdd-red.txt` có 21 test fail do `NotImplementedException` khi chạy, kèm stack trỏ về thân stub, ví dụ `OcrScorer.cs:line 27`. Không chứng minh được thứ tự red→green từ mtime (cả hai file đều ghi 07:35), nhưng nội dung là output chạy thật.
- Ca "Mép nội dung tối" khớp đúng chữ của bảng: có chi tiết sáng rải rác, khoảng 2%/cột (`ContentRectDetectorTests.cs:61-77`). Bảng này **quá lỏng** so với màn game thật (xem NB2). Đây là lỗi của doc, không phải của Dev.

## Finding

Mọi finding đều **non-blocking**. Thứ tự theo mức độ ảnh hưởng tới Bước 7 / Phase 3.

| # | Mức độ | Chỗ nào | Vấn đề | Nguyên nhân | Chấp nhận được? |
|---|---|---|---|---|---|
| NB1 | non-blocking — **làm trước Bước 7** | `Program.cs:20` · Phase doc dòng 104, 288 | **Đường DPI chưa từng được thử thật.** Màn hình đang ở **100%**: `GetDpiForSystem 96 screen 2880 1800` (`<rv>/12-dpi-check.txt`). Thanh taskbar 48 px vật lý (vùng làm việc khi maximize: WGC 2880×1752) cũng khớp với 100%. Registry `AppliedDPI` ghi 144 — giá trị cũ, không phải scale đang hiệu lực. Log `hiệu lực=True` của Dev đúng, nhưng ở 100% thì toạ độ không bị ảo hoá. Vì vậy smoke 3 (của Dev lẫn của Reviewer) **không chứng minh** offset client đúng ở scale > 100%, trái với bản đồ case ở dòng 288. Phase doc dòng 104 ghi "laptop 2880×1800 chạy scale > 100%" — nay không còn đúng | Doc giả định theo probe cũ; Dev không kiểm scale hiện tại | Chấp nhận cho 2a. Leader: (1) nếu User chơi Remote Play ở scale > 100% thì Bước 7 phải so offset client (`capture.json` `clientOffset`) với ảnh `--no-crop true`; hoặc (2) xin User tạm bật 150% cho một lần smoke 3 (đổi cài đặt máy User ⇒ User quyết). Mình **không** đổi scale |
| NB2 | non-blocking — **làm trước Bước 7** | `ContentRectDetector.cs:28-64`, `OcrRunner.cs:72` · Phase doc dòng 116, 118, 175, 210 | **Màn game tối, không có letterbox ⇒ rect nội dung co lại về vùng chữ.** Ảnh `rv-dark-edge` 1920×1080 (nền luma ≈ 11, 260 px dưới cùng trống): engine trả `ContentRect { X = 300, Y = 148, Width = 705, Height = 578 } confident=False`, trong khi thật là cả ảnh. Mọi `box01` lệch **0,155–0,312** (`<rv>/04-box01-check.txt`, ảnh `<rv>/box-check-rv-dark-edge-scale1.png`). `OcrRunner.cs:72` vẫn chuẩn hoá theo rect này kể cả khi `Confident=false`: `lines.Add(new OcrLineModel(line.Text, boxPx, Normalizer.Normalize(boxPx, detection.Rect)));`. Hệ quả cho **Bước 7**: tiêu chí "`capture.json` có rect nội dung ~16:9" (dòng 175) có thể **trượt trên Hộp thư FC 26 nền tối dù ảnh chụp hoàn toàn đúng** ⇒ dễ kết luận nhầm R-C hỏng | Bước 2 viết "Ưu tiên kết quả gần 16:9" (dòng 116) nhưng bảng TDD (dòng 210) chỉ đòi ca có chi tiết sáng rải rác; code đúng theo bảng | Chấp nhận cho 2a (G3 cho phép chỉnh sau khi có F1). Leader nên (a) báo trước cho người chạy Bước 7: `confident=false` trên màn tối **không** có nghĩa R-C hỏng — hãy xem PNG; (b) thêm vào doc một quy tắc dự phòng, ví dụ: không confident mà client gần 16:9 ⇒ dùng cả client; hoặc nới rect về 16:9 trong phạm vi client; kèm ca TDD "nền tối đồng màu, không chi tiết ở mép"; (c) ở Phase 3 không chuẩn hoá theo rect không confident |
| NB3 | non-blocking — doc | `OcrScorer.cs:40-46` · Phase doc dòng 130 | **Chỉ số chính (a) bị thổi phồng khi golden có đoạn trùng.** Một dòng OCR được dùng cho mọi đoạn golden giống nhau. Ví dụ dựng tay qua CLI thật: golden có `"Board"` 2 lần, OCR đọc lần hai thành `"B0ard"` ⇒ vẫn `đoạn 6/6 (a)=100,0%` (`<rv>/10-duplicate-segment-demo.txt`). Hộp thư FC 26 lặp người gửi ("Board"…) và ngày ("Today") rất nhiều ⇒ đọc sai ở một hàng bị hàng khác che mất | Doc dòng 130 cố ý cho dùng lại ("Mỗi dòng OCR được dùng cho nhiều đoạn golden") để phủ trường hợp hàng bị gộp, nhưng không tính tới đoạn trùng | Code đúng theo doc. Đề xuất Leader/SA sửa định nghĩa (a) cho đồng bộ với (b) mới (Rev 2.3): đoạn golden xuất hiện k lần cần ≥ k lần xuất hiện **riêng biệt** trong văn bản OCR. Sửa trước khi lấy số R-D |
| NB4 | non-blocking (Phase 1) / **phải sửa trước Phase 3** | `WindowCapturer.cs:49-50, 134, 162` | **Cửa sổ đổi kích thước giữa lúc chụp ⇒ ảnh bị cắt cụt mà không báo.** Pool tạo một lần theo `item.Size` (`winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 3, item.Size);`) rồi `fw = Math.Min(fw, texW); fh = Math.Min(fh, texH);`. Ca D: cửa sổ do mình bật, phóng từ 976×579 lên 1716×1039 giữa 3 khung ⇒ `frame-003.png: 961x541`, chỉ có góc trên-trái của cửa sổ mới, **exit 0**, `contentRect … confident=True` (`<rv>/05-smoke-capture.txt` ca D, `<rv>/07-resize-frame1-vs-frame3.png`) | Dev đã nêu ở dev report §4.5; spike Phase 1 chấp nhận được | Chấp nhận cho Phase 1 (`--frames 1`, User không kéo cửa sổ). Sửa: khi `frame.ContentSize` ≠ kích thước pool ⇒ `pool.Recreate(...)` rồi lấy khung mới (hoặc ít nhất báo exit 6 / cảnh báo). Leader dặn User không đổi cỡ cửa sổ khi đang chụp ở Bước 7 |
| NB5 | non-blocking | `WindowCapturer.cs:189` | `capturedAtUtc` là thời điểm **đọc mẫu** (`DateTime.UtcNow`), không phải thời điểm WGC tạo khung (`frame.SystemRelativeTime`). Màn tĩnh ⇒ 3 khung có 3 mốc giờ khác nhau nhưng là **cùng một khung**: 3 PNG cùng md5 `2e3da939…` ở ca A | Chọn đơn giản | Chấp nhận. Ghi rõ nghĩa trong README, hoặc thêm `frameTime` |
| NB6 | follow-up đã lên lịch (không phải lỗi Dev) | `OcrScorer.cs:48-51`, `README.md:47,55` | (b) vẫn dùng LCS theo thứ tự: `int wordsMatched = LcsLength(goldenWords, ocrWords);`. Rev 2.3 đã đổi sang đa tập hợp. Reviewer tái lập độc lập hiện tượng "(b) thấp giả": ảnh của mình `rv-letterbox-messages` cho (a) 16/16 nhưng (b) 34/54 = 63,0% (`<rv>/03-smoke5-ocr-score.txt`) | Rev 2.3 ra sau lượt code | Dev sửa ở vòng sau: code, test ca "hàng gộp" và "sai 1 ký tự" theo số mới, `README.md:47,55` |
| NB7 | non-blocking | `Commands.cs:40-42, 131`; `Program.cs:44-53` | Lỗi dữ liệu đầu vào thường gặp **thoát bằng unhandled exception** (exit `-532462766` kèm stack trace) thay vì exit 1 như bảng mã thoát ở `README.md:21`: `--hwnd zz12` ⇒ `System.FormatException` (`long.Parse`); `ocr-score --ocr <thư mục không có>` ⇒ `DirectoryNotFoundException` (`<rv>/05-smoke-capture.txt` ca H1, H2). Exit vẫn ≠ 0 và nguyên nhân vẫn được giữ — **không** phải lỗi "broad catch nuốt lỗi". Các lỗi lạ khác (COM/WinRT, ví dụ `CreateCaptureSession`) cũng thoát như vậy, không phải exit 6 | Chỉ bắt `EngineException`/`ArgumentException` | Chấp nhận. Sửa rẻ: `long.TryParse` ⇒ `ArgumentException`; kiểm `Directory.Exists(ocrDir)` như `OcrAsync:109` |
| NB8 | non-blocking (Phase 3) | `WindowCapturer.cs:109, 143, 47` | Vòng đời COM: RCW `access` (`var access = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(accPtr);`) và RCW `interop` không được `Marshal.ReleaseComObject`. Chúng giữ tham chiếu tới surface/factory cho tới khi GC chạy finalizer. `winrtDevice` (`IDirect3DDevice`, là `IClosable`) không được dispose. Mọi chỗ khác đúng: `ID3D11Texture2D(texPtr)` nhận đúng 1 ref và được dispose; `FromAbi` + `Marshal.Release` cân đối; thứ tự dispose session → pool → context → device đúng (`using var` ở `:45-51`); `latest` dispose trong `finally` (`:88-91`) | Spike ngắn | Không ảnh hưởng ≤ 5 khung. Phase 3 chụp liên tục ⇒ thêm `ReleaseComObject` trong `finally` và dispose `winrtDevice` |
| NB9 | non-blocking | `WindowCapturer.cs:173-176` | Không kiểm giá trị trả về của `DwmGetWindowAttribute`/`GetClientRect`/`ClientToScreen` (`DwmGetWindowAttribute(win.Hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frameBounds, Marshal.SizeOf<RECT>());`). Cửa sổ đóng giữa chừng ⇒ bounds = 0 ⇒ offset sai. Thường sẽ rơi vào nhánh "Vùng client rỗng" (exit 6), nhưng không chắc. Hình học cũng đọc lúc `ReadFrame`, không phải lúc WGC tạo khung | Spike | Chấp nhận; kiểm HRESULT ⇒ exit 6 |
| NB10 | non-blocking | `Engine.csproj:8` | `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` nhưng không có khối `unsafe` nào (grep không thấy). Mọi thao tác con trỏ đi qua `Marshal.Copy` với `map.DataPointer + y * (int)map.RowPitch` — đúng (tôn trọng `RowPitch`, 4 byte/điểm ảnh khớp với BGRA8 đã ép ở pool) | — | Bỏ cờ cho gọn |

Đã kiểm và **không** thành finding:
- Định dạng điểm ảnh: pool ép `B8G8R8A8UIntNormalized` (`WindowCapturer.cs:50`) ⇒ WGC giao BGRA8. Phép copy theo `RowPitch` đúng.
- Cửa sổ tự khớp với console chạy engine (`cmd /c` trong conhost): **không tái lập được**, chỉ ra `LỖI (2)` (`<rv>/08-selfmatch-console.txt`).
- Đường dẫn: `--out` tương đối theo thư mục hiện tại (root repo), đúng cách gọi chuẩn. `ocr-score` ghép theo tên không đuôi; golden thiếu ⇒ in "bỏ qua" (`<rv>/11-ocr-on-capture.txt`).

## Test · build · smoke

- **Test**: 21/21 pass (`Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21`) — `<rv>/01-build-test.txt`. Baseline: 0 (chưa có module). Khớp con số Dev báo.
- **Build**: `dotnet build engine` exit 0, **0 warning**; `--no-incremental` cũng 0 warning (`<rv>/01b-rebuild-noincremental.txt`). SDK `10.0.404`.
- **Evidence của Dev**: đã mở `dev/01-tdd-red.txt` (21 fail do `NotImplementedException`), `02-tdd-green.txt` (21 pass), `04-smoke3-4-capture.txt` (client 1920×1080 tại (1,31), exit 3 cho cửa sổ thu nhỏ), `05-smoke-capture-errors.txt` (exit 2 ×2, `--hwnd` exit 0), `06-smoke5-ocr-score.txt` (25/25, (b) 63/82), `cap/capture.json`. Tất cả khớp report. Không thấy số liệu nào lệch.
- **DPI hiện tại** (lệnh Python `ctypes`, process PerMonitorV2): `GetDpiForSystem 96 screen 2880 1800` ⇒ scale 100% (NB1, `<rv>/12-dpi-check.txt`).
- **Smoke lại: có** — Phase doc dòng 281 (bước 2, 3, 5; bước 6, 8, 9 thuộc `tv-overlay`, ngoài 2a). Môi trường: laptop dev. Cửa sổ do Reviewer bật (`ffplay`, `SDL_RENDER_DRIVER=software`; một form WinForms), tiêu đề `RvwCap*`. Mọi PID đã ghi trong `<rv>/05-smoke-capture.txt` và dừng đúng PID. `tasklist` cuối lượt không còn `ffplay.exe`/`Engine.exe`. **Không** chụp hay đụng cửa sổ nào khác.

| Bước / ca | Dữ liệu (của Reviewer) | Kết quả |
|---|---|---|
| 2 build+test | — | xanh, 21/21 |
| 3 (ca A) | `ffplay` hiện ảnh letterbox 1920×1200 (viền luma 16, kiểu limited-range) | exit 0; cửa sổ 1936×1239 → WGC 1922×1232 → client 1920×1200 tại (1,31); 3 PNG **không có thanh tiêu đề**, nội dung đúng (`<rv>/02-smoke3-capA-and-capB.png`); `contentRect (0,60,1920,1080) confident=True` |
| 3 (ca B — **đổi khoá: tỉ lệ cửa sổ**) | cửa sổ 1200×1000 hiện ảnh pillarbox 2000×1080 (2 lớp viền) | `contentRect (24,176,1152,648) confident=True` — đúng tuyệt đối về hình học |
| 3 (ca C — nguồn chuyển động) | `lavfi testsrc2` | 3 khung **khác nhau** (3 md5 khác), lấy mẫu cách 700 ms — G1 hoạt động khi nội dung đổi |
| 3 (ca D — đổi cỡ) | phóng cửa sổ giữa lúc chụp | ảnh cắt cụt mà vẫn exit 0 ⇒ NB4 |
| 3 (ca E — maximize) | cửa sổ phóng to toàn màn (rect −8…2888) | WGC 2880×1752, client (0,23) 2880×1729, không sót thanh tiêu đề, letterbox `(0,54,2880,1620)` đúng (`<rv>/06-maximized-half.png`) |
| 3 (ca F) | form WinForms (PowerShell 5.1) | chụp đúng; nhưng form vẫn **DPI-aware** và màn ở 100% ⇒ **không** thử được cửa sổ DPI-unaware (xem Chưa kiểm được) |
| 4 (ca G) | thu nhỏ cửa sổ của mình | exit 3, 0,7 s, thông điệp rõ |
| Đường lỗi (ca H) | — | `--window-title` thiếu giá trị ⇒ exit 1; `ocr --input` không tồn tại ⇒ exit 1; `--hwnd zz12` và `ocr-score` với thư mục không có ⇒ unhandled (NB7) |
| 5 ocr ×1/×2 + ocr-score | 3 ảnh riêng: danh sách 3 cột trong letterbox, email trong pillarbox (có chữ 20 px, `60%`, `(or decline)`), màn tối | ×1 và ×2 đều: (a) **27/27 = 100%**, (b) 101/121 = 83,5% (`<rv>/03-smoke5-ocr-score.txt`). **Ảnh tổng hợp — không phải R-D** |
| 5 box01 (kiểm bằng số, mạnh hơn nhìn mắt) | so `boxPx` với hộp mực chữ mình vẽ, và so `box01` với rect nội dung **thật** | letterbox/pillarbox: góc trên-trái lệch ≤ 3 px, sai số `box01` = 0,000 ở cả ×1 và ×2 (toạ độ quy về ảnh gốc đúng). Màn tối: lệch tới 0,312 (NB2) — `<rv>/04-box01-check.txt`, ảnh `<rv>/box-check-*.png` |
| OCR trên ảnh WGC thật | `cap-A/frame-001.png` | 16/16 đoạn, khớp với OCR trên ảnh gốc (`<rv>/11-ocr-on-capture.txt`) |

- **`.gitignore`**: `bin/`, `obj/` (`.gitignore:27-28`), `_local/` (`:34`), `*.local.json` (`:17`), script `*.ps1`/`*.py` trong `playtest/` (`project-docs/.gitignore:25,27`) đều đã bị ignore. Phần `playtest/` sẽ commit (của Dev và Reviewer) tổng cộng khoảng 3,1 MB; file lớn nhất 424 KB (`<rv>/cap-E/frame-001.png`). Không có gì riêng tư. Kết quả `git check-ignore -v` ở `<rv>/09-gitignore-check.txt`.
- **Sự cố của chính Reviewer (đã dọn)**: lần chạy smoke đầu tiên, script của mình bị trùng biến PowerShell (`$r`/`$R` không phân biệt hoa thường), nên ghi nhầm thư mục `RvW+RECT/` ở **root repo**. Mình đã xem nội dung (chỉ là output chụp của mình), xoá đi, rồi chạy lại toàn bộ. `git status` không còn thư mục này.

## Chưa kiểm được

1. **Cửa sổ DPI-unaware và scale > 100%**: màn đang 100%, `ffplay` và form PowerShell đều DPI-aware. Offset client ở scale 125/150% (và với app không khai DPI) **chưa ai chứng minh** (NB1). Mình không đổi cài đặt hiển thị của User.
2. **PS Remote Play thật** (R-C): thanh tiêu đề tự vẽ trong client, khung đen do render GPU, HDR — ngoài vai Reviewer.
3. **Golden F1 (N12)**: chưa có F1 ⇒ chưa soát được ≥ 30% golden. Số R-D chưa tồn tại.
4. **Màn hình thứ hai, cửa sổ bị che một phần hoặc nằm ngoài màn, HDR bật trên Windows**: chưa dựng.
5. **Nhánh `TryCreateFromLanguage == null`** (exit 5): chỉ đọc code (`OcrRunner.cs:30-35`), máy có `en-US`.
6. **Rò rỉ COM theo thời gian** (NB8): chỉ đọc code, chưa đo bằng một vòng chụp dài.
7. Thứ tự red→green: chỉ dựa vào nội dung `dev/01-tdd-red.txt`; mtime không chứng minh được thứ tự.

## Câu hỏi mở — Leader chưa hỏi gì, và Phase doc sai chỗ nào sau khi đã có code

1. **Doc dòng 104 / 288 đã lỗi thời**: máy đang ở 100%, nên "smoke 3 kiểm DPI" là không thật (NB1). Bước 7 cần ghi scale (dòng Rev 2.3 ở dòng 41 đã yêu cầu) **và** so offset bằng `--no-crop true` nếu scale ≠ 100%.
2. **Tiêu chí R-C ở Bước 7 (dòng 175) có thể cho kết quả âm tính giả** trên màn Hộp thư tối (NB2). Cần nói rõ: `confident=false` ⇒ xem PNG trước khi kết luận.
3. **Định nghĩa (a) ở dòng 130** không tính đoạn trùng (NB3) — nên sửa cùng đợt với (b) Rev 2.3, **trước** khi chạy `ocr-score` trên F1.
4. **Bước 2, dòng 116 ("Ưu tiên kết quả gần 16:9")** không có ca TDD tương ứng, và code không làm. Doc cần chốt: chỉ gắn cờ (như code hiện tại) hay có quy tắc dự phòng (NB2-b).
5. Brief của Leader không yêu cầu kiểm **đổi cỡ / maximize / tỉ lệ cửa sổ khác 16:9**. Mình đã kiểm: maximize và tỉ lệ khác đều đúng; đổi cỡ thì hỏng mà không báo (NB4). Cần ghi vào nợ kỹ thuật cho Phase 3.

---

## Re-review fix 2a

**Verdict: PASS** — không có finding blocking.

Phạm vi: chỉ phần delta theo dòng Rev 2.4 của Phase doc (dòng 41) và (b) Rev 2.3, đối chiếu với [dev report fix2a](261114-epic1-phase1-dev-report-fix2a.md). Evidence mới có tiền tố `re-` trong `<rv>/`. Mọi dữ liệu là của Reviewer (cùng ảnh `rv-dark-edge.png` và bộ `dup-demo` như review 2a, cộng 4 ảnh thăm dò hồi quy mới), cửa sổ là `ffplay` do Reviewer bật. PID ghi trong `<rv>/re-04-capture-nb4.txt`, đã dừng hết; `tasklist` cuối lượt không còn `ffplay.exe`/`Engine.exe`.

### Build · test

- `dotnet build engine --no-incremental`: `0 Warning(s)`, `0 Error(s)`. `dotnet test engine`: `Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28` (`<rv>/re-01-build-test.txt`). Trước vòng sửa là 21; thêm 7 test mới, khớp dev report §2.
- Test mới khớp đúng ca Rev 2.4: "nền tối đồng màu, chữ ở giữa" (`ContentRectDetectorTests.cs:112-120`); "Board ×2, lần hai B0ard ⇒ 1/2" (`OcrScorerTests.cs`, `DuplicateGoldenSegment_SecondOccurrenceMisread_OnlyOneOfTwoMatches` dòng 90, khẳng định `SegmentRate` 0.5 ở dòng 96). Các số kỳ vọng cũ (hàng gộp 5/5, sai 1 ký tự 2/3) không đổi.

### Từng NB

| NB | Đã kiểm (code) | Chạy lại (dữ liệu của Reviewer) | Rủi ro hồi quy của chính bản sửa |
|---|---|---|---|
| NB2 | `ContentRectDetector.cs:61-69` (dự phòng), `:78-85` `LooksLikeBars`; `Geometry.cs:103-108` `RectForNormalization`; `OcrRunner.cs` chuẩn hoá theo `rectUsed` (`Normalizer.Normalize(boxPx, rectUsed)`); `fallback` được ghi vào `capture.json`/`ocr.json` (`Commands.cs` `det.Rect, det.Confident, det.Fallback,`) | `rv-dark-edge.png`: `content=ContentRect { X = 0, Y = 0, Width = 1920, Height = 1080 } confident=True`, `fallback client`; **sai số `box01` tệ nhất = 0** (review 2a: 0,312) — `<rv>/re-02-nb2-nb3-nb7.txt`, `<rv>/re-03-nb2-box01.txt` | **Remote Play có letterbox thật không bị ảnh hưởng.** Rect 16:9 trả sớm, trước khi xét dự phòng (`:62`). Đã thử: P3 letterbox với viền nhiễu luma 18 ⇒ `(0,60,1920,1080) True None`; review 2a ca A/B/E chạy lại A, E vẫn đúng (`re-04`). **Hồi quy đã thấy (dev §6.2 đã dự đoán):** P2 — client 16:9 với pillarbox **lệch** (200/280 px) ⇒ `True client`: `confident=true` giả, che mất cảnh báo trước đây. P1 pillarbox 4:3 đối xứng ⇒ `False None`, đúng. P4 màn tối có cột chữ chạm cả mép trên lẫn mép dưới ⇒ dự phòng đúng. Với Remote Play (stream luôn 16:9) thì pillarbox lệch trong client 16:9 khó xảy ra ⇒ **non-blocking**. Hệ quả cho Bước 7: xem mục Rủi ro mới bên dưới |
| NB3 | `OcrScorer.cs:146-154` (đếm theo bội số), `:190-217` `CountDistinctOccurrences` (biên từ, không chồng lấn, ≤ 3 dòng) | `dup-demo` (cùng input như review 2a): **`đoạn 5/6 (a)=83,3%`** (2a: 6/6). Bộ ảnh tổng hợp đầy đủ: vẫn 27/27 — `re-02` | **Ca hàng gộp không vỡ:** mỗi đoạn golden **khác nhau** đếm độc lập trên cùng văn bản, nên một dòng gộp vẫn khớp đủ 3 đoạn. Test `MergedRow…` xanh; ca "2 lần trong cùng một dòng" ⇒ 2 (test `DuplicateGoldenSegment_TwoDistinctOccurrences_BothMatch`). Lỗi nhỏ, chỉ ảnh hưởng thông tin hiển thị: với đoạn trùng không khớp, "dòng OCR gần nhất" in ra chính lần khớp đã dùng (`KHÔNG KHỚP: "Board"  ⇐ dòng OCR gần nhất: "Board"`), nên người đọc phải tự tìm `B0ard` — non-blocking |
| NB6 | `OcrScorer.cs:156-162` (đa tập hợp; LCS đã xoá) | Bộ tổng hợp: (b) **121/121** (2a: 101/121, thấp giả). `dup-demo`: (b) **7/8** | **Vẫn bắt được lỗi đọc sai thật:** `B0ard` không được tính (7/8); ca "sai 1 ký tự" vẫn 2/3; mỗi từ OCR chỉ dùng một lần (test `Nov`×2 ⇒ 1). Cái mất đi là khả năng phát hiện sai thứ tự — đúng như Rev 2.3 chủ ý |
| NB4 | `WindowCapturer.cs:49-51` (`poolSize`), kiểm trong `ReadFrame` (`if (fw != poolSize.Width \|\| fh != poolSize.Height)` ⇒ exit 6) | **Đã chạy thật** (Dev chưa chạy): ca D phóng cửa sổ của mình giữa lúc chụp ⇒ **exit 6**, `LỖI (6): Cửa sổ đổi kích thước giữa lúc chụp (khung 1702x1032, pool 962x572). Không đổi cỡ/ phóng to cửa sổ khi đang chụp; chạy lại.` Thư mục `re-cap-D` **không được tạo** ⇒ không có PNG cắt cụt — `re-04` | **Không báo oan** ở các ca A (3 khung tĩnh), C (nguồn chuyển động, 3 khung), E (maximize). Hai rủi ro còn lại: (1) chỉ cần một khung lệch là bỏ **cả** N khung, kể cả khung tốt trước đó — chấp nhận được vì Bước 7 dùng `--frames 1`; (2) chưa biết PS Remote Play có đổi `ContentSize` khi stream đổi độ phân giải hoặc lúc kết nối không — nếu có sẽ ra exit 6 thay vì ảnh. Không phải lỗi im lặng nên non-blocking |
| NB5 | `README.md:72` | — | Không có |
| NB7 | `Commands.cs` `long.TryParse` ⇒ `ArgumentException`; kiểm `Directory.Exists` cho `--ocr`/`--golden` | `--hwnd zz12` ⇒ exit 1 kèm thông điệp. `--ocr` không tồn tại ⇒ exit 1. **`--golden` không tồn tại ⇒ exit 1** (nhánh Dev chưa chạy riêng — nay đã chạy) — `re-02` | Không có |
| NB10 | `Engine.csproj` không còn `AllowUnsafeBlocks`; grep `unsafe` trong `src` = 0 | Build `--no-incremental` 0 warning | Không có |

### Những thứ KHÔNG được làm (nợ Phase 3) và không có thay đổi lạc phạm vi

- **NB8**: không có `ReleaseComObject`; RCW `access` vẫn giữ nguyên như 2a (grep = 0); `winrtDevice` không được dispose — **chưa làm**, đúng như kế hoạch.
- **NB9**: giá trị trả về của `DwmGetWindowAttribute(win.Hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, …)` và `GetClientRect` vẫn không kiểm (`WindowCapturer.cs:179-180`) — **chưa làm**.
- **Tái tạo frame pool**: grep `Recreate` = 0 — **chưa làm** (NB4 chỉ báo lỗi).
- File không thuộc danh sách sửa có số dòng **y hệt** lúc review 2a: `WindowFinder.cs` 47, `NativeMethods.cs` 73, `ImageIo.cs` 69, `Program.cs` 55, `Cli.cs` 56, `Normalizer.cs` 24, `NormalizerTests.cs` 57, `Engine.Tests.csproj` 25, `Engine.slnx` 8. `Program.cs:20-25` (DPI) và `MeanLuma` (`Commands.cs:95-104`) đọc lại vẫn như cũ. Các thay đổi trong `WindowCapturer.cs`, `Commands.cs`, `OcrRunner.cs`, `Geometry.cs`, `ContentRectDetector.cs`, `OcrScorer.cs`, README đều gắn với một NB trong danh sách. Không thấy refactor ngoài phạm vi. (Không có git diff vì file còn untracked — đối chiếu bằng cách đọc lại.)
- Lệch so với Rev 2.4 mà Dev tự khai (điều kiện thêm `LooksLikeBars`; `ocr.json.contentRect` = rect đã dùng): **chấp nhận được**. Luật Rev 2.4 nếu áp đúng từng chữ sẽ làm vỡ ca TDD cũ "Không ra 16:9". `LooksLikeBars` giữ được ca đó, và ca P1 của mình cho thấy nó hoạt động. Leader/SA nên xác nhận vào doc (dev §6.1).

### Rủi ro mới Leader cần biết trước khi User chụp cửa sổ PS Remote Play thật

1. **Quy tắc Bước 7 "`confident=false` ⇒ xem PNG" (Rev 2.4) nay không đủ.** Có dự phòng thì `confident=true` vẫn có thể sai (ca P2). Đề xuất sửa quy tắc Bước 7 thành: **xem PNG khi `confident=false` HOẶC `fallback` ≠ `null`**. Lưu ý thêm: với Hộp thư FC 26 nền tối, kết quả gần như chắc chắn sẽ là `fallback:"client"`.
2. **`ocr.json.contentRect` đã đổi nghĩa** thành "rect đã dùng" (cả ảnh khi không confident). Muốn biết rect dò được thật thì phải xem `capture.json`. Ai đọc `ocr.json` để kiểm R-C sẽ không thấy viền — R-C phải được kiểm bằng `capture.json` + PNG.
3. **NB4 có thể ra exit 6 ngay khung đầu** nếu PS Remote Play đổi `ContentSize` (đổi độ phân giải stream, vừa khôi phục cửa sổ, vừa kết nối). Chưa từng xảy ra với 5 cửa sổ thử, nhưng chưa thử với Remote Play. Nếu gặp: chờ stream ổn định rồi chạy lại `--frames 1`; **không** coi là engine không chụp được.
4. Phần "Chưa kiểm được" của review 2a vẫn nguyên: scale > 100% / DPI-unaware, PS Remote Play thật, golden F1.
