# Buổi chạy thật đầu tiên — Phase 3 (chữ Việt thật lên TV)

Thay cho phần B của [runbook Phase 1](../../epic1-phase1/leader/tonight-runbook.md). Phần A (thông tin TV, bật gỡ lỗi, bật Remote Play, cài PS Remote Play) giữ nguyên. Leader (Claude) chạy mọi lệnh trên laptop; **bạn** làm các bước trên TV/PS5/tay cầm. Laptop cắm sạc, **không khoá màn hình** khi đang chơi (máy khoá ⇒ không chụp được cửa sổ PS Remote Play).

## 0. Trước khi bắt đầu (bạn)
- (Nên có) **Khoá API Claude**: tự đặt biến môi trường người dùng `ANTHROPIC_API_KEY` trên laptop rồi mở lại terminal. **Không gửi khoá qua chat.** Không có khoá ⇒ chỉ hiện những câu có sẵn trong bộ dịch mẫu (~200 nhãn/câu), còn lại giữ tiếng Anh.

## 1. Cài app lên TV (10 phút)
1. Leader: `adb connect <IP TV>:5555` ⇒ TV hỏi "Cho phép gỡ lỗi?" ⇒ bạn chọn **Luôn cho phép**.
2. Leader kiểm `ro.build.version.sdk` ≥ 29, cài `tv-overlay/app/build/outputs/apk/debug/app-debug.apk` (bản debug — bản release chưa ký; dùng cá nhân không sao), rồi chạy luôn:
   `adb shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow` và `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`.
3. Bạn mở app **VietSub** trên TV ⇒ màn **Chờ máy tính** hiện `IP:47820` và **mã 8 ký tự** ⇒ đọc mã cho Leader.
4. Leader: `engine pair --tv <IP TV> --code <mã>` ⇒ TV chuyển sang **Đã kết nối**.
5. Bạn bấm **Home** để rời app. *(Nhớ: mở app VietSub = tắt chữ; muốn bật lại: ← rồi OK, rồi Home.)*

## 2. Khớp góc (5 phút)
6. Chuyển TV sang **HDMI PS5**. Leader chạy `engine pattern --send true --seconds 60` ⇒ 4 dấu góc phải nằm đúng 4 góc hình game. Lệch ⇒ chỉnh "Screen size / Hiển thị toàn khung" của Sony theo Leader.
7. Để TV ở HDMI 1 phút rồi chạy lại `pattern` ⇒ còn hiện = app không bị TV giết.

## 3. Thử dịch thật (chỉ khi có khoá)
8. Mở PS Remote Play trên laptop, kết nối PS5, FC 26 để **tiếng Anh**, vào **Career Mode → Hộp thư**. Cửa sổ để to, **không thu nhỏ/kéo cỡ**.
9. Leader chụp 1 khung (`engine capture --frames 1`) rồi chạy `engine warm --translator claude --yes true` **trên đúng một ảnh** ⇒ đọc dòng `[llm]`. Có "tắt backend LLM cho phiên này" ⇒ dừng, kiểm khoá. (Chi phí cỡ vài cent.)

## 4. Chơi (10–20 phút)
10. Leader chạy `engine run --verbose true`. Bạn ngồi chỗ chơi, ở Hộp thư: cuộn danh sách, mở 3 email, quay lại, để yên.
11. Nói ngay cho Leader nếu: chữ **to/nhỏ** hơn chữ game · mảng che **trông như miếng dán** · chữ **chớp** khi đứng yên · chữ **sai chỗ**. Leader chỉnh `engine.local.json` (`fontScale`, `minFontPx`, `coverMode`, `holdMs`, ngưỡng `tuning`) — **không cần cài lại app**.
12. Vào một trận ngắn ⇒ chữ **không** được hiện giữa trận. Ra menu khác (Squad, Transfers) ⇒ có dịch nhưng chưa cam kết đẹp.
13. Kết thúc: Ctrl+C engine ⇒ chữ trên TV tự xoá trong vài giây.

## Bình thường ở bản này (không phải lỗi)
- Ngày (`12 Nov`), tên cầu thủ/CLB, câu chưa dịch kịp ⇒ **giữ tiếng Anh** (không vẽ bản thô). Câu mới chờ AI vài giây rồi mới hiện; lần sau hiện ngay (đã lưu).
- Cột hẹp có nhãn dài ⇒ cả cột co nhỏ cho đều.
- Đổi mã trên TV ("Ghép cặp lại") ⇒ phải chạy lại `engine pair`.
- Trần 100.000 ký tự/ngày ⇒ engine in số đã dùng mỗi 10 phút và khi thoát.

## Sau buổi
Leader: ghi số đo (góc, cỡ chữ, nhấp nháy, trễ), lưu ảnh chụp TV, chỉnh mặc định theo lựa chọn của bạn, cập nhật `automation/README.md` (model/IP TV).
