# automation/ — giá trị cụ thể cho test của dự án

Phương pháp chung (server local, chọn môi trường, dựng tài khoản): [`workflow/guidelines/automation.md`](../../workflow/guidelines/automation.md) — **đọc trước**. Test qua thiết bị Android/TV: [`workflow/guidelines/blackbox-android-adb.md`](../../workflow/guidelines/blackbox-android-adb.md).

| File | Chứa gì |
|---|---|
| [test-accounts.md](./test-accounts.md) | Tài khoản test — dự án này chưa có (xem file) |
| `scripts/` | Script tái dùng (xem README trong thư mục) |

## Thiết bị & mạng LAN

Dự án không có DB hay server. Môi trường test là **thiết bị thật trong LAN nhà User** + emulator.

| | |
|---|---|
| **Máy tính Windows (máy dev)** | Windows 11 · IP Wi-Fi LAN `192.168.1.30` (kiểm 2026-11-14 — có thể đổi, kiểm lại bằng `Get-NetIPAddress`). Cùng mạng `192.168.1.x` với TV và PS5 |
| **TV** | Sony 65" Google TV · IP: `<chưa ghi>` · gỡ lỗi qua mạng (`adb connect <ip>:5555`): `<chưa bật>` — **User bật** (Risk gate riêng). Bật xong ghi IP + ngày vào đây |
| **PS5** | IP: `<chưa ghi>` · Remote Play: `<chưa bật>` — **User bật** trong Cài đặt PS5 |
| **Cài đặt hình ảnh TV cho lớp phủ khớp toạ độ** | Tắt overscan / chọn chế độ hiển thị toàn khung (tên mục theo Sony — ghi lại khi chỉnh lần đầu) |
| **Emulator** | Android TV emulator — tên AVD, API level: ghi khi dựng lần đầu |

Giá trị local (IP, cổng LAN) ghi thẳng. 🔴 Khoá API dịch/LLM và mọi thông tin tài khoản PSN **không bao giờ** ghi ở đây.
