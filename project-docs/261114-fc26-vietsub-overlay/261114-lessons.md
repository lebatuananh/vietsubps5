# Lessons — Đợt 261114 Dịch FC 26 (Career Mode) lên TV

Tiêu chí ghi: **"nếu biết trước thì đã không gặp phải"**. Ghi **ngay khi gặp**, không dồn cuối đợt.
Quy tắc: [workflow/practices/lessons.md](../../workflow/practices/lessons.md)

## Đã chuyển thành rule

| Lesson | Chuyển vào đâu | Dự án / Template / Cả hai | Ngày |
|---|---|---|---|

> Điền khi đóng đợt — xem bảng hai trục ở [06-close §4](../../workflow/leader-stages/06-close.md).

---

### Leader suy kiến trúc sản phẩm của người khác từ lời mô tả trải nghiệm của User, rồi nói lại với User như bằng chứng

**Bối cảnh**: brainstorm ô 3 — User kể đã dùng Console Translator trên TV: *"Vẫn phải cài app trên TV sau đó không cần làm gì cả lần sau cứ thể là chơi"*.
**Quan sát**: Leader kết luận "app đó chạy ngầm trên TV và vẽ chữ đè… bằng chứng thực tế TV cho vẽ đè" và ngầm hiểu cả lõi chạy trên TV. Discovery lượt 1 (SA) đọc tài liệu của chính sản phẩm đó: app TV **chỉ hiển thị**, lõi chạy trên điện thoại/iPhone/PC. Leader phải đính chính với User.
**Nguyên nhân**: Leader verify phần dễ (TV cho vẽ đè — đúng) rồi ghi công cho mệnh đề rộng hơn (cả sản phẩm chạy trên TV) mà không đọc nguồn — đúng lỗi "verify nhầm mệnh đề" ở team-leader §2b, nhưng xảy ra ngay trong lời nói với User, trước discovery.
**Cách xử lý**: draft §2 có mục đính chính; lesson này; ô 3 "không cần bật lại" được tách thành đích của lát X thay vì lát 1.
**Phạm vi**: quy trình (mọi dự án) — khi User dẫn một sản phẩm khác làm chuẩn, Leader chỉ ghi lời User + đánh dấu `assumption` về cơ chế của sản phẩm đó cho tới khi discovery đọc nguồn; không nói lại với User như sự thật.

### Kiểm thư mục cài đặt ngoài repo bị sandbox chặn với worker

**Bối cảnh**: discovery lượt 1, SA muốn kiểm PS Remote Play đã cài chưa (`Get-ChildItem 'C:\Program Files\Sony'`).
**Quan sát**: "Claude Code may only access files in the allowed working directories for this session" ⇒ SA để `unknown`. Lượt 2 SA cũng không chạy được `dotnet --list-sdks` (ngoài allow-rule).
**Nguyên nhân**: worker Claude Code chạy với cwd = repo, sandbox giới hạn đường dẫn; allow-rule của brief hẹp.
**Cách xử lý**: Leader tự probe các fact máy (chỉ đọc) và ghi log; brief sau liệt kê sẵn lệnh probe máy cần thiết hoặc để Leader làm.
**Phạm vi**: profile Claude Code làm worker — fact máy ngoài repo do Leader probe.

### Kiểm "font đủ dấu" bằng Paint.hasGlyph cho kết quả đúng giả

**Bối cảnh**: Phase 1 lượt 2b, Leader chốt (Rev 2.2, 1.2 #8) log `Paint.hasGlyph()` của typeface Be Vietnam Pro làm bằng chứng đủ dải U+1EA0–U+1EF9.
**Quan sát**: `hasGlyph` trả `true` cho cả `日`/`한` — ký tự không có trong font (Dev đối chứng).
**Nguyên nhân**: `Paint.hasGlyph` xét cả chuỗi font fallback của hệ thống, không chỉ typeface được gán — đúng loại lỗi `CLAUDE.md` cảnh báo (font thiếu dải ⇒ lấy font hệ thống từng chữ).
**Cách xử lý**: test JVM đọc thẳng bảng `cmap` của file TTF nhúng (`FontCoverageTest`), có đối chứng âm.
**Phạm vi**: dự án (mọi chỗ kiểm font tiếng Việt) — kiểm thuộc tính font phải có **đối chứng âm**; ghi vào README `tv-overlay/` và rule ở 06-close.

### "Bọc ActivityNotFoundException" không đủ cho intent cài đặt trên Android TV

**Bối cảnh**: Phase 1 lượt 2b, N7 (Leader) — nút "Bỏ tối ưu pin" bọc `try/catch ActivityNotFoundException`, lỗi thì hiện lệnh adb dự phòng.
**Quan sát**: trên Google TV, `ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` resolve tới `com.android.tv.settings/.EmptyStubActivity` — mở rồi đóng ngay, không ném lỗi ⇒ User không thấy gì. Dev smoke bằng `appops`/`dumpsys` thay vì bấm nút nên không lộ; Reviewer bấm bằng `input keyevent` mới lộ.
**Nguyên nhân**: giả định "không có màn ⇒ ném ActivityNotFoundException"; TvSettings dùng stub.
**Cách xử lý**: kiểm **kết quả** sau khi quay lại (`onResume` vẫn chưa được cấp ⇒ hiện dự phòng) thay vì chỉ bắt lỗi khởi chạy; smoke phải bấm nút thật bằng phím remote.
**Phạm vi**: dự án (mọi màn cài đặt trên TV) + quy trình: smoke UI không được thay thao tác người dùng bằng lệnh tắt (`appops`, `am start --ez`) cho đúng thứ đang kiểm.
