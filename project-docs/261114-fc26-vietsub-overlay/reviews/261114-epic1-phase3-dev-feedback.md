DEV-FEEDBACK DONE
# 261114 — Epic 1 Phase 3 — Dev feedback (lượt 3a, gộp với code)

Đã đọc: `261114-epic1-phase3.md` Rev 1.2 (bảng *Quyết định sau SA gate* + thân doc), `reviews/261114-epic1-phase3-sa-review.md`, skill-ref C# (`README.md`). **Không có blocker** — em code luôn. Các chỗ doc không nói số/ hình dạng, em tự chọn (cấu hình được trong `tuning`, ghi lại ở *Lệch so với doc* của report):

| # | Chỗ phải đoán | Em chọn |
|---|---|---|
| F1 | `minFontPx` mặc định "theo mockup report" | **24** (mockup report dòng 87: "tối thiểu 24 px") |
| F2 | Ngưỡng số: `CellDiffThreshold`, `StableMs`, khung/giây | ô đổi khi |Δ xám TB| > 6/255; `StableMs` 250; ~4 khung/s; mọi hằng số trong `tuning` + `--verbose` |
| F3 | `sessionId` / `seq` | `sessionId` = GUID mới mỗi lần chạy; `seq` tăng từ 1 mỗi gói (kể cả nhịp tim) |
| F4 | `room` nếu không dò được vùng đồng nhất | bỏ `room` (null) — TV dùng luật cũ trong `box` |
| F5 | `group` đặt tên | `col-<n>` (cùng mép trái + cao dòng) và `row-<n>` (cùng hàng ngang); item thuộc cả hai ⇒ **`row-` thắng** khi nhãn hàng ngang ≥ 2 item, ngược lại `col-`; một item chỉ có **một** `group` (D2) |
| F6 | `alt` | `short_vi` của glossary/cache nếu có, ngược lại null |
| F7 | Seed `reviewed` | seed ghi `status:"machine"` (Dev không duyệt); cơ chế "seed `reviewed` thắng" vẫn có + test |
| F8 | `promptVersion`/`glossaryVersion` | hằng số `PromptVersion=1`, `glossaryVersion` lấy từ file glossary |
| F9 | Đếm ký tự LLM theo "ký tự nguồn" | tổng độ dài `sourceKey` các câu trong lô (không tính glossary/system) |
| F10 | `engine run` trên `--translator claude` khi không có khoá | cảnh báo 1 dòng + chạy như `cache` (đúng Rev 1.1/Prerequisites) |
| F11 | Ctrl+C | `Console.CancelKeyPress` ⇒ gửi `items:[]` rồi thoát mã 0 |

Ghi chú rủi ro (không chặn): (1) `Windows.Media.Ocr` không trả cỡ chữ ⇒ `fontPx` ước từ cao dòng, đúng như D6; (2) không có F1/F2/F3 thật ⇒ mọi ngưỡng chỉ kiểm bằng ảnh tổng hợp; (3) LLM thật **không** gọi ở lượt này (chỉ mapping + handler giả).
