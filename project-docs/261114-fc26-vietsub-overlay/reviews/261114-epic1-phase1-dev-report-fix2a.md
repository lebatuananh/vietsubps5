DEV-FIX2A DONE
# 261114 — Epic 1 Phase 1 — Dev report fix2a (vòng sửa sau review 2a)

Chỉ sửa NB2, NB3, NB6, NB4, NB7, NB10, NB5 theo Rev 2.3/2.4. Không làm NB8, NB9, tái tạo frame pool. Chỉ sửa `engine/` + `engine/README.md` + report này; evidence ở `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/dev/` (`<dev>/`). Không TV/PS5/LAN/adb/emulator, git chỉ đọc; cửa sổ chụp chỉ là `ffplay` do em bật (PID đã ghi, đã dừng, `tasklist` cuối lượt không còn `ffplay`).

## 1. Từng NB: đổi gì (file:dòng)

| NB | Thay đổi | Vị trí |
|---|---|---|
| NB2 | Dự phòng `fallback:"client"`: rect không 16:9 mà cả ảnh (đã cắt client) gần 16:9 **và** rect không giống viền đen thật ⇒ cả ảnh, `confident=true`. Không có dự phòng ⇒ `confident=false`, `fallback=null`, và `ocr` chuẩn hoá theo **cả ảnh**. Trường `fallback` trong `capture.json`/`ocr.json` | `Geometry/ContentRectDetector.cs:67` (+ `:78` `LooksLikeBars`), `Geometry/Geometry.cs:16` (`RectForNormalization`), `Ocr/OcrRunner.cs:45` (+ dòng chuẩn hoá), `Commands.cs:22,87,91` |
| NB3 | (a) tính bội số: golden k lần ⇒ cần ≥ k lần xuất hiện riêng biệt, không chồng lấn, ≤ 3 dòng | `Ocr/OcrScorer.cs:41` gọi `:82` `CountDistinctOccurrences` (thay `ContainsOnWordBoundary`, đã xoá vì hết dùng) |
| NB6 | (b) đa tập hợp, không xét thứ tự (thay LCS, đã xoá) | `Ocr/OcrScorer.cs:52` |
| NB4 | `frame.ContentSize` ≠ kích thước pool ⇒ `EngineException` exit 6 (không trả ảnh cắt cụt) | `Capture/WindowCapturer.cs:49,86,133-139` |
| NB7 | `--hwnd` dùng `TryParse` ⇒ exit 1; `ocr-score` kiểm `--ocr`/`--golden` tồn tại ⇒ exit 1 | `Commands.cs:43-45`, `:134-135` |
| NB10 | Bỏ `AllowUnsafeBlocks` | `Engine.csproj` (grep = 0) |
| NB5 | README ghi `capturedAtUtc` là thời điểm lấy mẫu | `engine/README.md` (phần schema) |

README cập nhật cho mọi đổi hành vi: mã thoát (exit 1 mở rộng; exit 6 thêm "đổi cỡ giữa lúc chụp"), `fallback`, định nghĩa (a)/(b), ngữ nghĩa `contentRect` trong `ocr.json` (= rect đã dùng), bỏ ghi chú "(b) thấp giả".

## 2. TDD

Trước: **21** test. Sau: **28** (+7): Detector +3 (nền tối đồng màu/ fallback; client không 16:9 ⇒ không fallback; `RectForNormalization`), Scorer +4 (`Board`×2 ⇒ 1/2; `Board`×2 hai lần riêng biệt/ cùng dòng/ chỉ một lần; (b) golden theo hàng vs OCR theo cột = 7/7; (b) đa tập hợp `Nov`×2 vs `Nov`). Test hiện có sửa: `ContentNotSixteenByNine` thêm `Assert.Null(r.Fallback)`. Số kỳ vọng (a)/(b) của test cũ **không đổi** (hàng gộp 5/5; sai 1 ký tự 2/3 vẫn đúng theo định nghĩa đa tập hợp).

**RED** (`<dev>/10-fix-tdd-red.txt`; runtime assertion fail, chưa có code): `Failed: 5, Passed: 23, Total: 28` — 5 test fail: `DarkUniformScreen_…_FallsBackToWholeClient`, `RectForNormalization_UsesWholeImage_WhenNotConfident`, `DuplicateGoldenSegment_SecondOccurrenceMisread_OnlyOneOfTwoMatches`, `DuplicateGoldenSegment_TwoDistinctOccurrences_BothMatch`, `WordCoverage_IsOrderIndependent_ColumnMajorOcrVsRowMajorGolden`. (Test `…_IsMultiset…` và `…ClientNotSixteenByNine…` xanh ngay từ đầu — là ca đối chứng, không chứng minh gì cho red.)

**GREEN** (`<dev>/11-fix-tdd-green.txt`, chạy lại cuối lượt): `Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28`. `dotnet build engine`: **0 Warning(s), 0 Error(s)**. Không sửa test để xanh.

## 3. Smoke (ảnh TỔNG HỢP — không phải R-C/R-D)

Thêm ảnh tối `synthetic/synth-dark.png` (nền `0x0b0b0b`, chỉ một dòng chữ ở giữa, mép trống) + golden. Script `fix-smoke.ps1` (ignored).

| Smoke | Kết quả | Evidence |
|---|---|---|
| 3 `capture` inbox (ffplay software renderer) | client 1920×1080, `meanLuma=33,4`, `confident=true fallback=none`, exit 0 | `<dev>/12-fix-smoke.txt`, `<dev>/fix-cap-synth-inbox-list/` |
| 3 `capture` **ảnh tối** (NB2) | `meanLuma=10,1`, `contentRect=(0,0,1920,1080) confident=True fallback=client` — **cả rect** (2a trên ảnh kiểu này co về vùng chữ) | `<dev>/12-fix-smoke.txt`, `<dev>/fix-cap-synth-dark/capture.json` |
| 5 `ocr`+`ocr-score` ×1 và ×2 (3 ảnh) | (a) 26/26 = 100%; (b) 85/85 = **100%** (2a: 76,8% — (b) thấp giả đã hết; riêng `synth-inbox-list` 63,5% → 100%). `synth-dark.ocr.json`: `fallback:"client"`, `box01.x = 648/1920 = 0,3375` (chuẩn hoá theo cả ảnh, đúng) | `<dev>/13-fix-smoke5-ocr-score.txt`, `<dev>/ocr-fix-x1/`, `<dev>/ocr-fix-x2/` |
| NB7 `--hwnd zz12` | exit **1** + thông điệp (2a: unhandled) | `<dev>/12-fix-smoke.txt` |
| NB7 `ocr-score` thư mục không có | exit **1** + thông điệp (lần chạy "missing golden dir" tình cờ trúng nhánh `--ocr` trước vì `ocr-fix-x1` lúc đó chưa tồn tại — nhánh `--golden` kiểm bằng đọc code + unit đường dẫn tương tự, **chưa chạy riêng**) | cùng file |

Số 100% vẫn chỉ chứng minh đường chạy trên ảnh sạch.

## 4. Chưa chứng minh được

- **NB4** (exit 6 khi đổi cỡ): chỉ đọc code + build; chưa phóng cửa sổ giữa lúc chụp để thấy exit 6. Cũng chưa biết `ContentSize` có luôn bằng `item.Size` ở khung đầu của PS Remote Play (nếu app đổi cỡ stream lúc kết nối, có thể báo exit 6 oan).
- **NB2 trên màn thật**: fallback chỉ thử với ảnh dựng; `LooksLikeBars` (vắt hết một chiều + hai lề lệch ≤ 0,5%) là ngưỡng em đặt, chưa thấy viền stream thật (không đối xứng tuyệt đối, bị nhiễu).
- NB3/NB6 chỉ trên golden dựng; chưa thấy F1 (nhiều đoạn trùng "Today", "Board" thật).
- Nhánh `--golden` không tồn tại của `ocr-score` chưa chạy riêng (xem trên).
- Mọi mục "chưa chứng minh" của 2a (PS Remote Play thật, R-D, scale > 100%) vẫn nguyên.

## 5. Lệch so với Rev 2.4

1. **Luật dự phòng NB2 áp dụng có điều kiện thêm "rect không giống viền đen thật"** (`LooksLikeBars`). Lý do ở mục 6.
2. `ocr.json.contentRect` = rect **đã dùng** để chuẩn hoá (cả ảnh nếu không confident) chứ không phải rect dò được; `capture.json.contentRect` vẫn là rect dò được (cùng `fallback`). Ghi README.
3. Xoá `ContainsOnWordBoundary`/`LcsLength` (code chết sau NB3/NB6).

## 6. Câu hỏi mở — Rev 2.4 có mâu thuẫn với code / làm chụp thật tệ hơn không?

1. **Mâu thuẫn trực tiếp với bảng TDD sẵn có.** Luật chữ nghĩa của NB2 ("không confident mà client gần 16:9 ⇒ cả client") áp lên ca bảng "Không ra 16:9: nội dung 1500×1080 sau khi bỏ viền ⇒ rect thật, `confident=false`": ảnh đó là 1920×1080 (client **chính nó** 16:9) ⇒ luật chữ nghĩa sẽ trả cả ảnh, `confident=true` và làm test này đỏ. Em giữ ca bảng bằng điều kiện thêm `LooksLikeBars` (viền đen **đối xứng** vắt hết một chiều vẫn là viền thật). Leader/SA cần xác nhận điều kiện này, hoặc chọn bỏ ca bảng cũ.
2. **Hệ quả thực tế**: PS Remote Play ở cửa sổ có client 16:9 mà stream bị pillarbox **không đối xứng** hoặc kèm một mép sáng sẽ bị dò thành rect hẹp, `LooksLikeBars` = false ⇒ fallback **cả client** ⇒ `confident=true` giả, và toạ độ chuẩn hoá tính cả phần viền đen. Trong khi trước đây ít nhất `confident=false` báo hiệu. Với màn game tối thật, fallback đúng; với viền lệch, nó che mất cảnh báo. Đề xuất giữ `fallback:"client"` hiện diện trong JSON và cho Bước 7 **luôn xem PNG khi `fallback` ≠ null** (không chỉ khi `confident=false`).
3. **Chuẩn hoá theo cả ảnh khi không confident** (client không 16:9): nếu cửa sổ Remote Play không 16:9 và stream 16:9 nằm trong đó, `box01` tính theo cả client sẽ sai cùng mức viền — Phase 3 phải có đường khác (rect do người cấu hình). Phase 1 chấp nhận được.
4. NB4 exit 6 hợp lý cho Phase 1 nhưng với Remote Play (stream có thể đổi độ phân giải theo mạng — discovery D6) cửa sổ không đổi cỡ nhưng nội dung khung đổi: chưa biết `ContentSize` có đổi theo không. Nếu có, `--frames` > 1 sẽ thỉnh thoảng exit 6; dùng `--frames 1` như Rev 2.3 dặn.

## 7. `git status --short` (cuối lượt)

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
?? project-docs/261114-fc26-vietsub-overlay/_workers/…   (của Leader, có từ trước)
?? project-docs/261114-fc26-vietsub-overlay/playtest/
?? project-docs/261114-fc26-vietsub-overlay/reviews/
```
Phần của em: `M .gitignore` (lượt 2a), `?? engine/`, `?? …/playtest/`, report trong `reviews/`; các `M`/`??` còn lại của Leader.
