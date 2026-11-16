# Backlog, nợ kỹ thuật & bẫy môi trường

**Đọc trước mọi đợt.** Gồm cả **backlog** (việc User đã chốt để làm sau) lẫn **nợ kỹ thuật / bẫy môi trường**. Mỗi mục: hiện tượng · vì sao còn đó · hệ quả nếu quên · cách sống chung (hoặc kế hoạch trả).

> Mục nào là **chủ ý thiết kế** chứ không phải nợ ⇒ ghi rõ, để không ai "sửa" nhầm.

---

## 1. `engine/` chụp liên tục chưa giải phóng COM và không chịu được đổi cỡ cửa sổ

**Module**: `engine` · **Phát hiện**: đợt 261114 (review 2a NB4, NB8, NB9)
**Hiện tượng**: RCW `IDirect3DDxgiInterfaceAccess`/interop và `winrtDevice` không được giải phóng tường minh; giá trị trả về của `DwmGetWindowAttribute`/`GetClientRect` không kiểm; frame pool không tạo lại khi cửa sổ đổi cỡ (hiện báo exit 6 và dừng).
**Vì sao còn đó**: Phase 1 chỉ chụp ≤ 5 khung (spike khử rủi ro).
**Hệ quả nếu quên**: Phase 3 chụp liên tục ⇒ rò bộ nhớ/handle; User kéo cửa sổ PS Remote Play ⇒ lõi dừng.
**Cách sống chung**: Phase 1 dùng `--frames 1`, không đổi cỡ cửa sổ khi chụp. **Trả ở Phase 3**.

## 2. Lớp phủ vẽ đè lên mọi nguồn, không chỉ HDMI PS5

**Module**: `tv-overlay` · **Phát hiện**: đợt 261114 (discovery §3.2)
**Hiện tượng**: `TYPE_APPLICATION_OVERLAY` nằm trên mọi thứ đang hiển thị — chuyển sang YouTube/truyền hình mà lớp phủ còn bật thì chữ vẫn hiện.
**Vì sao còn đó**: chưa có API công khai chắc chắn để biết nguồn TV đang hiện.
**Hệ quả nếu quên**: người nhà thấy chữ dịch FC 26 đè lên chương trình khác.
**Cách sống chung**: APK kiểm tra tự tắt sau 30 phút (`START_NOT_STICKY`). Phase 3: lõi chỉ gửi khi khung Remote Play là FC 26 + gói có `ttlMs` tự xoá.

## 3. UX chỉ-có-remote của app TV — đầu vào bắt buộc cho mockup Phase 2

**Module**: `tv-overlay` · **Phát hiện**: đợt 261114 (dev report fix2b §5, re-review 2b)
**Hiện tượng**: (a) không có cách tắt lớp phủ nhanh khi đang ở HDMI; (b) lệnh dự phòng là `adb` cần máy tính; (c) focus bị kéo sang "việc tiếp theo" ngay sau thao tác của User; (d) màn quyền overlay của Google TV là danh sách chung, phải cuộn tìm app; (e) nút "Bỏ tối ưu pin" trên Google TV mở stub tự đóng.
**Vì sao còn đó**: màn hiện tại là công cụ kiểm tra Phase 1, được miễn mockup có điều kiện.
**Hệ quả nếu quên**: sản phẩm thật dùng bằng remote sẽ kẹt.
**Cách sống chung**: **Phase 2 mockup phải giải (a)–(e)**; màn kiểm tra bị thay hoàn toàn ở Phase 3.

## 4. `Paint.hasGlyph` không dùng được để kiểm font đủ dấu

**Module**: `tv-overlay` · **Phát hiện**: đợt 261114 (dev report 2b §6.1)
**Hiện tượng**: `hasGlyph` trả `true` cả với ký tự không có trong font (xét font fallback hệ thống).
**Vì sao còn đó**: chủ ý — không phải nợ; ghi để không ai dùng lại.
**Hệ quả nếu quên**: báo "đủ dấu" giả, chữ Việt lẫn font hệ thống trên TV.
**Cách sống chung**: kiểm bằng test đọc bảng `cmap` của file TTF (`FontCoverageTest`), luôn kèm đối chứng âm.

<!-- Khuôn một mục:

## <N>. <Tiêu đề — nêu thẳng vấn đề>

**Module**: … · **Phát hiện**: đợt [YYMMDD]
**Hiện tượng**: …
**Vì sao còn đó**: chủ ý thiết kế / chưa có thời gian / chờ …
**Hệ quả nếu quên**: …
**Cách sống chung**: …
-->
