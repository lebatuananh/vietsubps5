# 261114 — Epic 1 Phase 2 — Leader phê bình mockup (vòng 1)

**Ngày**: 2026-11-17 · **Đối tượng**: `mockups/261114-mockup-inbox-overlay.html`, `mockups/261114-mockup-tv-app.html` theo [mockup report](261114-epic1-phase2-mockup-report.md). Leader xem ảnh `playtest/epic1-phase2/sa/01, 06, 16, 30, 44`.

**Nhận xét chung**: bám đủ X1–X15, định hướng *bản địa · kín đáo* đạt ở màn danh sách (06): cùng cột cùng cỡ, màu chữ bám chữ gốc, vệt chọn giữ chữ tối — nhìn như game có sẵn tiếng Việt. Màn app TV (30, 44) rõ ràng, focus vàng nổi, 2 hành động. Đi phím có số thật. Tốt.

## Phải sửa (vòng này)

| # | Chỗ | Vấn đề | Yêu cầu |
|---|---|---|---|
| C1 | `16-inbox-email-long-translated.jpg` — thẻ tiêu đề + dòng phụ trên ảnh | **Chữ tiếng Anh gốc lộ ra ngoài thẻ che**: sau "…Marco Velden" còn "len"; quanh "ĐÃ NHẬN ĐỀ NGHỊ…" thấy bóng ký tự gốc. Thẻ che đang co theo **câu Việt**, không theo **vùng chữ gốc** ⇒ đúng kiểu "méo xệch" User cấm | **Quy tắc che**: vùng che = **hợp** của bbox chữ gốc (từ OCR) và bbox chữ Việt, cộng lề (≥ 6 px / 1080). Áp cho cả `solid` và `card`. Kiểm mọi trạng thái của 3 màn (list, email dài, email ngắn) không còn pixel chữ gốc nào ló ra; ghi quy tắc này vào bảng quyết định như **ràng buộc cho Phase 3** (không phải lựa chọn của User) |
| C2 | `06-inbox-list-translated.jpg` — hàng tab | "CHUYỂN NHƯỢNG" dài hơn nên gần dính "ĐỘI HÌNH" (khe ≈ 20 px trong khi các khe khác ≈ 60 px) | **Quy tắc nhãn hàng ngang**: khe tối thiểu giữa hai nhãn liền nhau; thiếu ⇒ dùng `short_vi` trước (vd "MUA BÁN" hoặc "CHUYỂN NHƯỢNG" co theo nhóm tab), rồi mới co cỡ **cả nhóm tab**. Ghi vào bảng quyết định là ràng buộc Phase 3 |
| C3 | `30-tv-setup-none.jpg` — dòng `adb` | Lệnh `adb` mono cỡ nhỏ (~16 px) — không đọc được từ 3 m, và người dùng remote không cần đọc | Gom các dòng "Người cài (cần máy tính)" vào **một khối thu gọn** "Hướng dẫn cho người cài" (mở bằng một nút), cỡ ≥ 20 px khi mở; mặc định ẩn |

## Ghi nhận, không sửa

* Câu dịch minh hoạ ổn cho mục đích duyệt hiển thị; xưng hô "ông/HLV" để User chốt (đã có trong bảng quyết định).
* Màn "Đã kết nối" còn khoảng trống lớn bên trái dưới — chấp nhận (TV, ít chữ là tốt).

## Sau vòng sửa

SA cập nhật mockup + chụp lại các ảnh bị ảnh hưởng (prefix `r2-`), ghi một mục "Vòng 2" vào mockup report. Leader xem lại ảnh C1/C2/C3; đạt ⇒ sẵn sàng thay F2 và trình User trên TV.
