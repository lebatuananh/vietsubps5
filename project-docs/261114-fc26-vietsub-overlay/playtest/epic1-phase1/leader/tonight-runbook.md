# Buổi tối 2026-11-17 — hướng dẫn cho User (Phase 1 Bước 6–7 + duyệt mockup Phase 2)

Thời lượng dự kiến: 45–60 phút. Laptop **cắm sạc**, cùng Wi-Fi với TV và PS5. Leader (Claude) chạy mọi lệnh trên laptop; bạn làm các bước trên TV/PS5/tay cầm.

## A. Chuẩn bị (5 phút)
1. **TV**: Cài đặt → Hệ thống → Giới thiệu → chụp màn hình (model + phiên bản Android TV) gửi Leader. Cần Android 10 trở lên.
2. **TV**: trong màn Giới thiệu, bấm dòng **Bản dựng** (Build) 7 lần ⇒ bật Tuỳ chọn nhà phát triển. Vào Tuỳ chọn nhà phát triển ⇒ bật **Gỡ lỗi USB** và **Gỡ lỗi ADB qua mạng** (tên có thể hơi khác). Ghi **IP của TV** (Cài đặt → Mạng) gửi Leader.
3. **PS5**: Cài đặt → Hệ thống → Remote Play → bật. (Nên bật luôn "Ẩn trạng thái kết nối Remote Play" nếu có.)
4. **Laptop**: cài **PS Remote Play** (playstation.com), đăng nhập tài khoản PSN của bạn — **Leader không chạm vào bước đăng nhập**.

## B. Cài app kiểm tra lên TV (10 phút) — R-A, R-B
5. Leader chạy `adb connect <IP TV>:5555` ⇒ **TV hỏi "Cho phép gỡ lỗi?"** ⇒ bạn chọn **Luôn cho phép** ⇒ OK.
6. Leader cài APK + chạy luôn lệnh miễn tối ưu pin (nút trong app trên Google TV thường không làm gì).
7. Trên TV mở app **VietSub — lớp phủ kiểm tra** (hàng ứng dụng). Nút vàng là nút đang chọn. Bấm **Cấp quyền hiển thị trên ứng dụng khác** ⇒ trong danh sách tìm dòng **VietSub — lớp phủ kiểm tra** ⇒ bật ⇒ Back. (Không thấy dòng đó ⇒ báo Leader, Leader chạy lệnh thay.)
8. Bấm **Bật lớp phủ** ⇒ chuyển sang **HDMI PS5**, vào FC 26.
9. **Chụp TV bằng điện thoại** (thẳng, cả màn) ⇒ gửi Leader. Ghi: 4 dấu góc vàng có nằm **đúng 4 góc hình game** không; chữ Việt mẫu đọc rõ từ chỗ ngồi không. Ghi thêm: PS5 có bật **HDR** không; **chế độ ảnh** TV (Game/…); PS5 xuất **4K hay 1080p, 60 hay 120 Hz**.
10. Lệch góc ⇒ Leader hướng dẫn chỉnh "Hiển thị toàn khung / Screen size" của Sony.
11. Chuyển HDMI → Home → HDMI; để TV chờ 1 phút rồi bật lại ⇒ lớp phủ còn không (chỉ ghi lại).

## C. Thử tay cầm + chụp PS Remote Play (15 phút) — Q1, R-C, R-D
12. **Đang cầm tay cầm chơi FC 26**, mở PS Remote Play trên laptop, kết nối PS5 **cùng tài khoản** ⇒ ghi: **tay cầm có bị ngắt không**; laptop có hiện hình khi **không cắm tay cầm** vào laptop không.
13. FC 26 để **tiếng Anh**, mở **Career Mode → Hộp thư (Inbox)**. Cửa sổ PS Remote Play để to, **không thu nhỏ, không kéo đổi cỡ** khi Leader chụp.
14. Bạn điều hướng trong game: danh sách thư (đầu / giữa / cuối), mở 3 email khác nhau (≥ 1 email dài), 1 lần dời vệt chọn sang dòng khác ⇒ mỗi màn **đứng yên vài giây** để Leader chụp bằng `engine capture --frames 1`.
15. Cùng lúc ở **một màn tĩnh**, chụp TV bằng điện thoại (để Leader so vị trí chữ giữa TV và laptop).

## D. Ảnh cho mockup (5 phút)
16. Trên PS5 (Cài đặt → Ghi & phát → Ảnh chụp → định dạng **PNG**), bấm **Create** chụp: danh sách Hộp thư (2 ảnh, vệt chọn ở 2 dòng khác nhau) + 1 email dài + 1 email ngắn ⇒ chép ra USB hoặc app PS ⇒ đưa vào laptop.

## E. Xem mockup trên TV (10–15 phút) — chốt kiểu hiển thị
17. **Cắm cáp HDMI từ laptop vào TV**, Leader mở mockup toàn màn hình (đã thay ảnh nền bằng ảnh bạn vừa chụp nếu kịp).
18. Ngồi đúng chỗ chơi, dùng phím mũi tên/Enter trên laptop (thay remote). Chốt: **kiểu che chữ** (solid / card), **có chấm "đang dịch" không**, **cỡ chữ nhỏ nhất**, **độ trễ chấp nhận được khi cuộn** (0/150/300/500 ms), **xưng hô** (HLV/ông/anh), **định dạng ngày và tiền**.

## Sau buổi tối
Leader: đo OCR trên ảnh thật (R-D), chốt Phase 1, sửa mockup theo ý bạn, rồi sang **Phase 3 — Hộp thư dịch thật trên TV** (cần **khoá API Claude** bạn tự đặt vào biến môi trường `ANTHROPIC_API_KEY` trên laptop — không gửi khoá qua chat).
