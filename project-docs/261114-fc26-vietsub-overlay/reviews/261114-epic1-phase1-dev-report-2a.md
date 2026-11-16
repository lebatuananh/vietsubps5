DEV-2A DONE
# 261114 — Epic 1 Phase 1 — Dev report 2a (Bước 1–3, `engine/`)

**Phạm vi**: Bước 1, 2, 3 (`engine/`) theo Rev 2.2. **Không** làm `tv-overlay/` (lượt 2b), không sửa `CLAUDE.md`/`automation/README.md`/doc đợt, không emulator/TV/PS5, không lệnh git ghi. Đường evidence (đã commit được, script đã bị `project-docs/.gitignore` loại): `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/dev/` — viết tắt `<dev>/` dưới đây.

## 1. Đã làm gì (file)

| Nhóm | File |
|---|---|
| Khung | `engine/Engine.slnx` · `engine/src/Engine/Engine.csproj` (`net10.0-windows10.0.22621.0`) · `engine/tests/Engine.Tests/Engine.Tests.csproj` + `GlobalUsings.cs` |
| Bước 2 (thuần) | `Geometry/Geometry.cs` (`ContentRect`, `Box`, `ContentDetection`) · `Geometry/ContentRectDetector.cs` · `Geometry/Normalizer.cs` · `Ocr/OcrScorer.cs` |
| Bước 3 | `Capture/NativeMethods.cs` (P/Invoke + `IGraphicsCaptureItemInterop`, `IDirect3DDxgiInterfaceAccess`) · `Capture/WindowFinder.cs` · `Capture/WindowCapturer.cs` (Vortice D3D11 + WGC `CreateForWindow` + `CreateFreeThreaded`) · `ImageIo.cs` (PNG qua `Windows.Graphics.Imaging`) · `Ocr/OcrRunner.cs` · `Cli.cs` (parse tay) · `Commands.cs` · `Program.cs` (DPI ở dòng đầu `Main`) |
| Test | `ContentRectDetectorTests.cs` · `NormalizerTests.cs` · `OcrScorerTests.cs` |
| Cấu hình/ docs | `engine/README.md` · root `.gitignore` (+ `*.local.json`, `engine/tests/fixtures/**/_local/`) · `.gitkeep` ở `f1-remoteplay/ f2-ps5/ f3-frames/ golden/` · thư mục `_local/f3-video`, `_local/f4-tv-photo` (ignored, chỉ tồn tại ở máy này) |
| Evidence/ tooling (không commit script) | `<dev>/*.txt`, `cap*/`, `ocr-*/`, `synthetic/`, `08-box-check-x2.png`; script `make-synthetic.ps1`, `smoke34.ps1`, `smoke-errors.ps1`, `ffplay-variants*.ps1` |

NuGet: `Vortice.Direct3D11` + `Vortice.DXGI` 3.8.3 (MIT, kéo `Vortice.DirectX`, `Vortice.Mathematics`, `SharpGen.Runtime[.COM]` — MIT); `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2 (Apache-2.0); `Microsoft.NET.Test.Sdk` 18.12.0 (MIT). Giấy phép đọc từ `.nuspec`, đã ghi vào `engine/README.md`. Mạng chỉ dùng cho NuGet.org (restore).

## 2. Số test trước/ sau

| | Trước | Sau |
|---|---|---|
| Baseline (trước khi code) | **0** (chưa có module) · HEAD `8101fc74`, nhánh `features/261114-fc26-vietsub-overlay` | — |
| Red (stub) | — | **21 fail / 0 pass** (21 `NotImplementedException`) |
| Green | — | **21 pass / 0 fail** |

21 case = `DetectContentRect` 6 + `Normalize` 5 (3 `Fact` + 1 `Theory` 2 dòng) + `ScoreOcr` 10 (8 theo bảng + 2 ca biên từ: bắt đầu giữa từ `room`/`Boardroom` không khớp; dấu câu là biên `Today`/`Today,` khớp). Bảng TDD 18 case của doc: đủ cả; thêm 3.

### Red (runtime, không phải lỗi biên dịch) — `<dev>/01-tdd-red.txt`
```
Failed!  - Failed:    21, Passed:     0, Skipped:     0, Total:    21, Duration: 146 ms - Engine.Tests.dll (net10.0)
(21 dòng "System.NotImplementedException : The method or operation is not implemented.")
```
Ví dụ: `Engine.Tests.NormalizerTests.EmptyRect_ThrowsWithMessage_NoDivideByZero(w: 0, h: 1080)` — `Expected: typeof(System.ArgumentException) / Actual: typeof(System.NotImplementedException)`.

### Green — `<dev>/02-tdd-green.txt`, chạy lại cuối lượt
```
Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21, Duration: 402 ms - Engine.Tests.dll (net10.0)
```
Không sửa test cho xanh; không nới assertion. Ngưỡng `DetectContentRect` (G3) đặt **trước** khi viết test và không đổi sau đó.

## 3. Smoke (Blackbox Verify bước 1–5, ảnh **tổng hợp** — KHÔNG phải R-C, KHÔNG phải R-D)

| Bước | Kết quả | Evidence |
|---|---|---|
| 1 `dotnet --list-sdks` có `10.0.x` | **PASS** — `8.0.121`, `10.0.404` | `<dev>/03-smoke-step1-2.txt` |
| 2 `dotnet build engine` + `dotnet test engine` | **PASS** — 0 warning/0 error; 21/21 | cùng file; cuối lượt chạy lại 21/21 |
| 3 `capture` cửa sổ thay thế, 3 PNG | **PASS (có bẫy, xem dưới)** — `ffplay` `VietSubTest` do em bật: cửa sổ 1936×1119 → khung WGC 1922×1112 → client **1920×1080 tại (1,31)**; 3 PNG 1920×1080, **không có thanh tiêu đề**, nội dung đúng (đã xem ảnh), `meanLuma=33,4`; `capture.json` có rect client + rect nội dung (0,0,1920,1080), `confident=true`; DPI `PerMonitorV2` hiệu lực | `<dev>/04-smoke3-4-capture.txt`, `<dev>/cap/` |
| 4 Thu nhỏ rồi chụp | **PASS** — exit `3`, "Cửa sổ đang thu nhỏ…", **0,7 s**, không treo | cùng file |
| (thêm) đường lỗi/ `--hwnd` | **PASS** — không khớp ⇒ exit 2; 2 cửa sổ khớp ⇒ exit 2 + liệt kê HWND/tiêu đề/kích thước; `--hwnd` chọn đúng 1 ⇒ exit 0 | `<dev>/05-smoke-capture-errors.txt` |
| 5 `ocr` + `ocr-score`, 2 biến thể | **PASS về đường chạy** (xem bảng số dưới); box01 kiểm bằng mắt: 19 hộp vẽ lên ảnh nằm đúng chữ | `<dev>/06-smoke5-ocr-score.txt`, `<dev>/ocr-x1|x2/`, `<dev>/08-box-check-x2.png` |
| (thêm) OCR trên ảnh chụp WGC | 19 dòng/ảnh, giống ảnh gốc | `<dev>/07-ocr-on-capture.txt` |

**Bẫy ở bước 3 (đã gặp, đã chẩn đoán)**: lần đầu cả 3 PNG **đen hoàn toàn** (`meanLuma=0,0`). Chẩn đoán: `--no-crop true` cho thấy thanh tiêu đề hiện đúng, còn client đen ⇒ copy pixel và cắt client **đúng**, nguồn đen. `ffplay` mặc định dùng renderer GPU, WGC chụp ra đen trên máy này; `SDL_RENDER_DRIVER=software` ⇒ ảnh đúng. Evidence: `<dev>/cap-nocrop/`, `<dev>/cap-v1-loop0/` (đen), `<dev>/cap-w2-mp4-renderer-sw/` (đúng). Ghi vào README.

### Số OCR — ảnh tổng hợp, **không phải R-D** (2 ảnh 1920×1080 vẽ bằng ffmpeg, golden = đúng chuỗi đã vẽ)

| Biến thể | (a) đoạn khớp | (b) từ đúng |
|---|---|---|
| ×1 (`ocr-x1`) | 25/25 = **100%** | 63/82 = 76,8% |
| ×2 (`ocr-x2`) | 25/25 = **100%** | 63/82 = 76,8% |

Riêng `synth-inbox-list`: (a) 19/19, (b) 33/52 = 63,5%; `synth-email`: (a) 6/6, (b) 30/30. **Ý nghĩa**: chỉ chứng minh đường chạy + toạ độ; ảnh sạch nên ×1 = ×2 không cho biết gì về ảnh stream nén.

**Phát hiện**: (b) thấp giả trên ảnh OCR hoàn hảo. Windows OCR trả dòng **theo cột** (cột người gửi hết rồi mới cột tiêu đề, cột ngày) còn golden xếp **theo hàng** (quyết định D2) ⇒ dãy con chung theo thứ tự bị gãy dù 19/19 đoạn khớp. Xem mục 6.

## 4. Chưa chứng minh được

1. **Cửa sổ PS Remote Play thật**: WGC có chụp ra hình (không đen) không; có thanh tiêu đề tự vẽ trong vùng client không; offset client đúng không; tiêu đề cửa sổ thật. Mọi thứ ở bước 3 mới kiểm với **một** cửa sổ Win32 chuẩn trên **một** màn hình, **một** mức DPI. Cửa sổ GPU-render có thể ra đen (đã gặp với `ffplay`) — chưa biết PS Remote Play có thế không (R-C).
2. **R-D**: chưa có F1 ⇒ không có số OCR thật. 100% trên ảnh tổng hợp **không** nói gì về chữ FC 26 đã nén.
3. **Ngưỡng `DetectContentRect`** (`BlackLumaMax=24`, `MinBrightPercent=1`, `AspectTolerance=0.015`): kiểm bằng ảnh dựng trong test; viền "đen" thật của stream (limited-range, nhiễu nén, viền không thuần đen) chưa thấy. Cần chỉnh/ xác nhận khi có F1 — đang `chưa kiểm`.
4. `confident` cho cửa sổ không 16:9 thật (vd Remote Play ở cửa sổ 4:3, hoặc client có thêm thanh) chưa thấy ngoài ảnh dựng.
5. Chưa kiểm: HD color/ định dạng điểm ảnh ≠ BGRA8; khung WGC đổi kích thước giữa chừng (frame pool **không** được tạo lại — dùng `ContentSize` từ góc trên-trái); cửa sổ bị che một phần/ trên màn hình thứ hai/ scale khác; cửa sổ có đổ bóng Windows 10.
6. Nhánh `OcrEngine.TryCreateFromLanguage == null` (exit 5): chỉ đọc code, máy có `en-US` (đã chạy OCR thật).
7. `Normalize` kẹp hai góc (G5) — chưa biết Phase 3 có cần ngữ nghĩa khác không.
8. `Engine.Tests` chỉ phủ 3 hàm thuần; `capture`, `ocr`, `ocr-score` (đọc/ghi file, CLI) chỉ kiểm bằng smoke.

## 5. Lệch so với doc (và lý do)

| # | Lệch | Lý do |
|---|---|---|
| 1 | Smoke 3 chạy `ffplay` với `SDL_RENDER_DRIVER=software` | `ffplay` mặc định ra đen dưới WGC (mục 3). Không đổi sang WinForms |
| 2 | Thêm cờ `--no-crop true` (chẩn đoán) | Cần để phân biệt lỗi copy pixel với nguồn đen; ghi README. Không dùng khi chụp F1 |
| 3 | Smoke 5 chạy trên `<dev>/synthetic/`, không phải `.../f1-remoteplay` | G8: F1 chưa có (thư mục chỉ có `.gitkeep`) |
| 4 | 21 test thay vì 18 | +2 ca biên từ (Rev 2.2 "ca biên từ thêm"), +1 dòng `Theory` rỗng-rect (w=0, h=0) |
| 5 | `capture.json`: `contentRect`/`confident` tính từ **khung đầu**; thêm `clientOffset`, `dpiPerMonitorV2`, `meanLuma` từng khung | Chẩn đoán; schema G6 + G12. Không phải contract |
| 6 | `xunit.runner.visualstudio` 2.8.2 (không bản mới nhất) | Khớp xUnit v2 |
| 7 | `--frames N` lấy N mẫu từ khung mới nhất (G1) ⇒ cảnh đứng yên cho **N ảnh giống hệt** (đã thấy: 3 PNG cùng 108468 byte) | Đúng quyết định G1; hệ quả xem mục 6 |
| 8 | Không dùng app manifest cho DPI | Rev 2.2: gọi `SetProcessDpiAwarenessContext` ở dòng đầu `Main`; log `hiệu lực=True` |

## 6. Câu hỏi mở — quyết định của Leader còn thiếu/ sai trong thực tế

1. **D2 (golden xếp theo hàng) làm chỉ số (b) vô nghĩa với danh sách nhiều cột.** Bằng chứng: OCR đọc 19/19 đoạn đúng, (a)=100%, nhưng (b)=63,5% vì Windows OCR trả dòng theo cột (`<dev>/06-smoke5-ocr-score.txt`, thứ tự dòng trong `ocr-x1/synth-inbox-list.ocr.json`). Với F1 thật (Hộp thư 3 cột) (b) sẽ thấp giả tương tự và dễ bị đọc như "OCR kém". Đề xuất: (b) tính theo **độ phủ từ không phụ thuộc thứ tự** (đa tập hợp), hoặc sắp dòng OCR theo (hàng, x) trước khi LCS, hoặc bỏ (b). Em chưa đổi — đây là định nghĩa chỉ số do Leader/SA chốt.
2. **Bước 7: `--frames 5` trên màn tĩnh cho 5 ảnh y hệt** (đã thấy với 3 khung). "Danh sách Hộp thư (đầu/giữa/cuối), 3 email, …" ⇒ nếu mỗi trạng thái chụp `--frames 5` thì fixture F1 phình gấp 5 mà không thêm thông tin, và golden/ Reviewer soát ×5. Nên dùng `--frames 1` cho mỗi trạng thái màn, hoặc chỉ giữ ảnh khác nhau.
3. **Tên file F1 va chạm**: mỗi lần `capture` ghi `frame-001.png…` vào thư mục `--out` của nó. "Chép các PNG đạt vào thư mục F1" từ nhiều lần chạy ⇒ trùng tên; mà `ocr-score` ghép golden theo **tên không đuôi** (`<ảnh>.golden.json`). Cần quy ước đổi tên ngay khi chép (ví dụ `inbox-list-top.png`, `email-contract.png`) **trước** khi viết golden. Doc chưa nói.
4. **Quyết định "ffplay là cửa sổ thay thế, không cần WinForms" suýt sai**: `ffplay` mặc định chụp ra đen; chỉ chạy được nhờ renderer phần mềm. Quan trọng hơn: nếu PS Remote Play cũng render GPU kiểu đó, R-C có thể ra đen vì lý do **không phải lỗi engine** — Bước 7 nên có sẵn nhánh chẩn đoán (`meanLuma`, `--no-crop`) và cách xử lý (cài đặt render/ phần cứng của app) trước khi kết luận "engine không chụp được".
5. **Mơ hồ chưa quyết**: `PS Remote Play` có thể khớp ≥ 2 cửa sổ (G2 sẽ liệt kê và dừng) — Leader cần sẵn `--hwnd` khi chạy Bước 7; tiêu đề thật của cửa sổ chưa ai biết.
6. Không thấy quyết định nào khác sai trong thực tế ở lượt 2a.

## 7. Dọn dẹp

- Tiến trình em bật: 3 phiên `ffplay` ×1–2 cửa sổ (PID đã ghi trong log từng smoke) — đã `Stop-Process` đúng PID; `tasklist` cuối lượt không còn `ffplay.exe`, không còn `Engine.exe`. Không bật emulator/server/daemon Gradle.
- Dữ liệu để lại: toàn bộ `<dev>/` (ảnh/ JSON evidence, ảnh tổng hợp + golden tổng hợp). File tạm `%TEMP%\vietsub-synth-test.mp4` đã xoá.
- Cửa sổ chụp: **chỉ** `ffplay` `VietSubTest` do em bật; không chụp/ động vào cửa sổ nào khác.

## 8. `git status --short` (cuối lượt)

```
 M .gitignore
 M project-docs/261114-fc26-vietsub-overlay/261114-draft.md
 M project-docs/261114-fc26-vietsub-overlay/261114-workflow-log.md
 M project-docs/261114-fc26-vietsub-overlay/agent-sessions.md
 M project-docs/automation/README.md
 M project-docs/overview-project.md
?? engine/
?? project-docs/261114-fc26-vietsub-overlay/261114-epic1-phase1.md
?? project-docs/261114-fc26-vietsub-overlay/261114-epic1.md
?? project-docs/261114-fc26-vietsub-overlay/261114-lessons.md
?? project-docs/261114-fc26-vietsub-overlay/261114-overview.md
?? project-docs/261114-fc26-vietsub-overlay/_workers/briefs/…  (brief của Leader, có từ trước)
?? project-docs/261114-fc26-vietsub-overlay/_workers/runs/…    (script chạy worker, có từ trước)
?? project-docs/261114-fc26-vietsub-overlay/playtest/
?? project-docs/261114-fc26-vietsub-overlay/reviews/
```
(Các `M`/`??` ở `project-docs/…` ngoài `playtest/`, `reviews/` là của Leader từ trước lượt này; phần của em: `M .gitignore`, `?? engine/`, `?? …/playtest/`, và file report này trong `reviews/`.) `git ls-files --others --exclude-standard engine` — 24 file nguồn/ test/ README/ `.gitkeep`; `bin/ obj/` và `_local/` bị ignore (`git check-ignore -v` xác nhận).
