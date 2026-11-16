# 261114 Dịch FC 26 (Career Mode) lên TV — Overview

**Ngày tạo**: 2026-11-15 · **Trạng thái**: 🔄 đang làm
**Nhánh**: `features/261114-fc26-vietsub-overlay` (monorepo) · **Module bị đụng**: `engine/` (mới), `tv-overlay/` (mới), `project-docs/`, `CLAUDE.md`
**Draft**: [261114-draft.md](261114-draft.md) · **Discovery**: [261114-discovery.md](261114-discovery.md) · **Log**: [261114-workflow-log.md](261114-workflow-log.md) · **Lessons**: [261114-lessons.md](261114-lessons.md)

---

## Vấn đề

- FC 26 trên PS5 không có tiếng Việt. User chơi **Career Mode, offline** là chính và muốn *"dịch được hết… để tôi trải nghiệm chơi cho hoàn hảo"*, chữ Việt *"chuẩn và đẹp… không bị méo xệch"*, hiện **thẳng trên TV** (draft ô 1–3).
- Đợt này đưa Career Mode lên tiếng Việt theo từng nhóm màn; mỗi nhóm xong là một lần cài lên TV để User chơi thử (ô 5).
- Phụ thuộc: app TV vẽ đè + lõi PC (lấy hình → OCR → dịch → gửi) là xương sống, mọi nhóm màn đi qua. Các nhóm màn sau độc lập với nhau nhưng tuần tự theo thứ tự User duyệt.

## Hiện trạng

Repo chưa có code (đợt đầu). Discovery Round 1 + 2 ([261114-discovery.md](261114-discovery.md)) đã chốt:
- Lớp phủ trên Google TV khả thi: `SYSTEM_ALERT_WINDOW` + miễn tối ưu pin (Console Translator đã chạy trên chính TV của User — chỉ phần vẽ, lõi của nó ở thiết bị khác).
- Lõi chạy trên laptop Windows của User; lát đầu lấy hình từ app **PS Remote Play chính thức** bằng Windows.Graphics.Capture; lát "không cần bật gì" về sau dùng chiaki-ng chạy riêng (duyệt riêng).
- Máy: Windows 11 build 26100, OCR `en-US` có sẵn, chỉ có .NET SDK 8.0.121 (cần .NET 10), chưa có `adb`/Java.

## Các Epic

### Epic 1: Hộp thư Career Mode tiếng Việt trên TV
📋 **[Chi tiết](261114-epic1.md)** — dựng hai module, khử rủi ro trên TV thật, chốt kiểu hiển thị bằng mockup trên ảnh thật, rồi đưa màn Hộp thư lên TV đẹp/chuẩn — lần cài thử chính thức đầu tiên.

### Epic 2: Career Hub, menu, Đội hình & chỉ số cầu thủ
📋 *(viết khi Epic 1 xong)*

### Epic 3: "Không cần bật gì" — lõi tự chạy + tự kết nối PS5
📋 *(viết khi Epic 2 xong; cần User duyệt AGPL + tài khoản PSN)*

### Epic 4+: Chuyển nhượng → Sự kiện → Tin tức → Tập luyện → Manager Live
📋 *(viết lần lượt)*

## Thứ tự

```
Epic 1 → Epic 2 → Epic 3 ("không cần bật gì", đặt sớm sau 2–3 nhóm màn — User duyệt) → Epic 4+
```

## Trạng thái

- 🔄 Epic 1: Phase 1 (chuẩn bị & khử rủi ro) — code `engine/` + `tv-overlay/` xong, Reviewer PASS (2a, 2b); **chờ User ở nhà**: U3–U7, Bước 6 (TV thật), Bước 7 (PS Remote Play thật), golden F1 + số R-D
- 📋 Epic 2+: chưa mở

## Tổng kết *(viết khi đóng đợt)*
