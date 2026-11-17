# 261114 — Epic 1 — Phase 2: Mockup Hộp thư tiếng Việt + màn app TV

📋 **[Epic 1](261114-epic1.md)** · **[Overview](261114-overview.md)**
**Revision**: Rev 1.1 — 2026-11-17 — SA PASS ([review](reviews/261114-epic1-phase2-sa-review.md)); Leader áp **cả 15 NOTE X1–X15** (bảng dưới — **ràng buộc**, đè lên thân doc). · Rev 1 — 2026-11-17

## Quyết định sau SA gate (Rev 1.1 — ràng buộc)

| # | Quyết định |
|---|---|
| X1 | Chuyển động **trung thực**: ≥ 2 ảnh danh sách với vệt chọn ở **dòng khác nhau** (ảnh tạm: tự dựng 2 biến thể; F2: User chụp ≥ 2 — thêm vào Prerequisites). Bấm ↓ ⇒ đổi ảnh nền **ngay**, lớp chữ cũ giữ thêm `độ trễ` rồi fade-out/fade-in. Thanh công cụ có chọn `độ trễ` 0 / 150 / 300 / 500 ms — số User chọn thành **metric Phase 3** |
| X2 | User **chỉ chốt khi xem trên TV**: (1) cắm cáp HDMI laptop → TV, mở mockup toàn màn, ngồi chỗ chơi. (2) (khuyên) lớp "Đã dịch" xuất PNG trong suốt để APK kiểm tra vẽ đè nguồn HDMI PS5 thật — thao tác thiết bị, User chạy. Nhận xét từ laptop **chưa tính là chốt** |
| X3 | Thêm trạng thái `Dịch một phần` (1–2 dòng còn tiếng Anh, có/không chấm chờ) và `Màn chưa hỗ trợ` (màn khác, không lớp phủ, một dòng giải thích) |
| X4 | **Cùng cột / cùng loại nhãn = cùng một cỡ** (theo ô dài nhất đang hiện); chỉ đoạn văn email auto-fit riêng; ưu tiên bản ngắn trước khi co. Mockup có cảnh cột người gửi/tiêu đề |
| X5 | Tắt lớp phủ theo thứ tự ưu tiên: **(1) tự động** — lớp phủ chỉ hiện khi app foreground là nguồn TV/HDMI (`UsageStatsManager`, quyền "Truy cập dữ liệu sử dụng") — mockup vẽ là hành vi mặc định, ghi "chờ spike đầu Phase 3"; **(2)** lõi ngừng gửi khi khung không phải FC 26 + `ttlMs`; **(3)** **mở app = tắt lớp phủ** (màn "Đã tắt lớp phủ — OK để bật lại") + hướng dẫn ghim app vào Yêu thích. Mockup **đếm số phím thật** của đường (3) |
| X6 | **Bỏ màn Cài đặt.** Màn "Đã kết nối" chỉ có **Tắt lớp phủ** · **Ghép cặp lại**. Kiểu che, cỡ chữ, chấm trạng thái chốt **một lần** khi duyệt ⇒ hằng số Phase 3 |
| X7 | 4 trạng thái chuẩn áp **khi có nghĩa**, không thì `N/A` + một câu lý do. Thêm `Lớp phủ đang bật — mở app ra` và `Quyền bị thu hồi / TV vừa cập nhật` |
| X8 | Mã ghép cặp **6–8 ký tự dễ đọc từ 3 m** (chữ số hoặc bộ không nhập nhằng `0/O`, `1/l`), chia nhóm, cỡ lớn. Thêm khung phụ "phía laptop" để thấy trọn luồng |
| X9 | Nhãn "**bản dịch minh hoạ**" trên mockup. Thêm vào danh sách quyết định User chốt: **định dạng ngày** và **tiền**, tên CLB/cầu thủ giữ nguyên |
| X10 | Dựng sớm được; **User chỉ chốt sau khi Phase 1 Bước 6 có kết quả** (R-A/R-B + mật độ màn TV). Gộp chung **một buổi** với Phase 1 Bước 6–7 (tối 2026-11-17) |
| X11 | Ảnh tạm: **không** logo EA/FC, huy hiệu CLB, ảnh cầu thủ, font Cruyff Sans; tên CLB/cầu thủ **hư cấu**. F2 nhúng base64 ⇒ mockup chứa ảnh EA ⇒ commit được khi repo private, xét lại khi public. ffmpeg chỉ xử lý ảnh/video User tự chụp |
| X12 | Font Be Vietnam Pro **nhúng base64** trong HTML (bỏ đường dẫn tương đối — `file://` có thể chặn) |
| X13 | Đường dẫn đủ: `project-docs/261114-fc26-vietsub-overlay/mockups/…`, `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase2/<vai>/` |
| X14 | `none` chỉ là **một ô đối chứng**; ghi chú trên mockup "`solid` ở đây khớp màu hơn thực tế — xem X2 (2)" |
| X15 | Hint màn app TV **suy ra từ trạng thái hiện tại** (CHƯA CẤP ⇒ luôn có hướng dẫn), không theo lịch sử thao tác; có cảnh "quay về từ cài đặt, quyền vẫn chưa có" |

---

## Hiện trạng

- Phase 1: code `engine/` + `tv-overlay/` (APK **kiểm tra**) xong, Reviewer PASS; phần trên thiết bị thật (Bước 6–7, fixture F1–F4) **chờ User ở nhà**.
- Viết Phase 2 **trước khi Phase 1 đóng** (lệch nhẹ trình tự cuốn chiếu — Leader quyết, ghi log): Phase 2 không phụ thuộc kết quả đo của Phase 1, chỉ phụ thuộc **ảnh nền thật F2** — được thay vào cuối Phase (Bước 4). User **chỉ duyệt** mockup khi đã có nền F2 thật.
- Đầu vào thiết kế đã có: discovery §5 (định hướng), §3.8 (khó khăn hiển thị trên màn FC 26), R2.7 (trạng thái mockup); [`present/backlog-and-techdebt.md`](../present/backlog-and-techdebt.md) mục 3 (UX chỉ-có-remote (a)–(e)); review 2b NB-R2 (focus bị kéo sau thao tác).
- Không có code sản phẩm nào đọc mockup — mockup là **hợp đồng** cho Phase 3 (trường dữ liệu `OverlayFrame`, trạng thái, kiểu che, luồng màn app TV).

## Mục tiêu

User nhìn mockup trên TV/màn lớn và nói được *"đúng kiểu tôi muốn"* về: **kiểu chữ Việt thay tại chỗ** trên Hộp thư Career Mode, và **màn app TV** dùng bằng remote. Chốt ở đây để Phase 3 không phải làm lại kiểu hiển thị (draft ô 5: "duyệt kiểu hiển thị sớm").

## Prerequisites

- ✅ Định hướng thiết kế (discovery §5) — vibe *bản địa · kín đáo · sắc nét · bình tĩnh*; font Be Vietnam Pro.
- ⏳ **F2** (ảnh nút Create của PS5) ≥ 3 ảnh: danh sách Hộp thư, 1 email dài, 1 email ngắn — **User**. **F4** (ảnh TV chụp từ chỗ ngồi) — **User**, chỉ dùng để chốt cỡ chữ, không commit (`_local/`).
- ⏳ Kết quả Bước 6.3 Phase 1 (HDR bật/tắt, chế độ ảnh TV) — ảnh hưởng chọn kiểu che; chưa có ⇒ mockup có cả 3 kiểu che để so.

## Files bắt buộc đọc trước khi làm

- [`261114-discovery.md`](261114-discovery.md) §3.8, §5, R2.7 — danh sách khó khăn + trạng thái phải có; không tự chế thêm trạng thái ngoài danh sách mà không ghi lý do.
- [`present/backlog-and-techdebt.md`](../present/backlog-and-techdebt.md) mục 3 — 5 vấn đề remote-only màn app TV **phải được giải** trong mockup.
- [`workflow/roles/ba-sa.md`](../../workflow/roles/ba-sa.md) §Bước 6 — hình thức mockup (prototype bấm được, thanh công cụ Màn × Trạng thái).
- [`tv-overlay/app/src/main/res/font/`](../../tv-overlay/app/src/main/res/font/) — Be Vietnam Pro Regular/SemiBold có sẵn (OFL); mockup nhúng **cùng font** (base64 hoặc `@font-face` tương đối tới file trong repo) để chữ y như APK.

## Kế hoạch

### Tổng quan

Hai file prototype HTML trong `mockups/` (worker dựng, Leader phê bình ≥ 1 vòng, SA soát trải nghiệm, rồi User duyệt):
1. `mockups/261114-mockup-inbox-overlay.html` — lớp chữ Việt vẽ đè trên ảnh Hộp thư FC 26.
2. `mockups/261114-mockup-tv-app.html` — màn app TV (sản phẩm, thay màn kiểm tra của Phase 1).

Khung hiển thị **16:9 cố định** (1920×1080 logic, co theo cửa sổ), điều khiển được bằng **bàn phím mũi tên + Enter + Backspace** để mô phỏng remote. Thanh công cụ nổi: chọn **Màn**, rồi **Trạng thái** của màn đó (hai lựa chọn tách riêng).

### Bước 1: Mockup lớp chữ Hộp thư

**Màn** (nền = ảnh tạm, rồi F2 thật ở Bước 4): (1) Danh sách Hộp thư · (2) Một email dài mở · (3) Một email ngắn mở.
**Trạng thái mỗi màn**:
* `EN gốc` — ảnh không có lớp phủ (đối chứng).
* `Đang chờ dịch` — giữ tiếng Anh (không vẽ gì), có hay không một chấm trạng thái nhỏ ở góc — **làm cả hai biến thể** để User chọn.
* `Đã dịch` — chữ Việt thay tại chỗ.
* `Đang chuyển động` — lớp chữ mờ dần (≈ 80 ms) khi màn cuộn/đổi mục, hiện lại (≈ 120–150 ms) khi đứng yên — chạy được bằng phím ↑↓ trên màn danh sách.
* `Mục đang chọn` — dòng được chọn đổi kiểu theo màu nhấn của game.
* `Overflow` — câu Việt dài nhất (≥ 1,4× tiếng Anh): auto-fit giảm cỡ tới mức tối thiểu rồi xuống 2 dòng; **không** dùng "…" cho nhãn.
* `Mất kết nối lõi` — lớp chữ tự xoá sau `ttlMs` (≈ 3 s, mô phỏng bằng nút).
**So sánh kiểu che** (một trạng thái riêng trên màn email): `solid` (mảng màu lấy mẫu + viền mờ 1–2 px) · `card` (thẻ bo góc nền tối ~80% đục) · `none` (chỉ chữ, có bóng) — đặt cạnh nhau trên **cùng một vùng chữ-trên-ảnh**.
**Bản dịch trong mockup**: do worker viết tay (không gọi LLM — risk gate), theo thuật ngữ bóng đá VN thông dụng; xưng hô đề xuất "HLV" ở nhãn, "ông" trong email trang trọng (discovery R2.8 Q7) — ghi chú ngay trên mockup để User chốt.
**Cỡ chữ**: cỡ tối thiểu đọc được trên TV 65" ở ~3 m — worker đề xuất số (px trên khung 1080) kèm lý do; chốt lại khi có F4.

### Bước 2: Mockup màn app TV (sản phẩm)

**Màn**: (1) Lần đầu — cấp quyền + miễn pin · (2) Chờ lõi kết nối (hiện IP:cổng + mã ghép cặp) · (3) Đã kết nối — đang dịch · (4) Lõi mất kết nối · (5) Cài đặt (cỡ chữ, kiểu che, bật/tắt chấm trạng thái).
**Bắt buộc giải** (backlog mục 3):
* (a) **Tắt lớp phủ nhanh khi đang ở HDMI**: đề xuất của Leader — lớp phủ **chỉ hiện khi lõi đang gửi** (tắt lõi trên PC = chữ tự biến mất sau `ttlMs`) **và** app có tile/biểu tượng trên màn Home để mở ra bấm "Tắt" trong 2 thao tác. Worker được đề xuất cách khác nếu tốt hơn, nêu lý do.
* (b) Đường **không cần máy tính** khi TV không có màn cấp quyền: nêu rõ trên màn khi nào **bắt buộc** cần máy tính (adb) và hướng dẫn ngắn; không giả vờ có đường remote-only nếu không có.
* (c) **Focus không bị dời** sau một thao tác của User (NB-R2) — chỉ đặt focus mặc định khi **mở màn**.
* (d) Màn quyền overlay là **danh sách chung** — hướng dẫn tìm dòng tên app.
* (e) Nút miễn pin mở **stub** trên Google TV — luồng: bấm → quay lại → nếu chưa miễn thì hiện hướng dẫn, không lặp vô tận.
**Trạng thái mỗi màn**: Loading · Empty (chưa có gì để hiện) · Error · Overflow (IP/mã dài, tên mạng dài) — đủ 4 trạng thái chuẩn.
**Focus**: luôn nhìn thấy (nền vàng #FFD400 như APK kiểm tra, hoặc phương án đẹp hơn có lý do).

### Bước 3: Phê bình + soát trải nghiệm (Leader + SA)

* Leader phê bình **≥ 1 vòng** (ghi `reviews/261114-epic1-phase2-leader-critique.md`), worker sửa.
* SA soát **tiêu chí 6 (trải nghiệm)**: người dùng khựng/bấm nhầm ở đâu, so Console Translator và phụ đề Netflix trên TV.

### Bước 4: Thay nền F2 thật + trình User

Khi có F2: thay ảnh tạm bằng F2 (đổi tên theo nội dung), chỉnh vị trí chữ Việt cho khớp chữ gốc trên ảnh thật, chạy lại phê bình nhanh. **Rồi mới** trình User (mở trên màn lớn/TV qua trình duyệt nếu được).

## Deliverables

- ✅ 2 file HTML trong `mockups/`, mở trực tiếp bằng trình duyệt, không cần server, không tải gì từ mạng ngoài trừ font nhúng từ repo.
- ✅ Mỗi màn có đủ trạng thái liệt kê; thanh công cụ Màn × Trạng thái; điều khiển bằng phím mô phỏng remote.
- ✅ Phê bình Leader + soát SA có file trong `reviews/`.
- ✅ Nền là **F2 thật** khi trình User; ảnh tạm được đánh dấu rõ "ẢNH TẠM" trên khung.
- ✅ Danh sách quyết định User cần chốt khi duyệt (kiểu che, chấm trạng thái, xưng hô, cỡ chữ) in ngay trong mockup.
- ❌ **Chưa làm**: code sản phẩm, giao thức `OverlayFrame` thật, gọi LLM, glossary chính thức, màn hiệu chỉnh 4 góc có UI.

## Blackbox Verify

Mockup là HTML tĩnh — không có API/DB.
### Chuẩn bị
- Không tài khoản. Trình duyệt Edge/Chrome trên laptop. Ảnh tạm: worker tự tạo (không lấy ảnh FC 26 từ web — bản quyền), dựng giống bố cục Hộp thư theo mô tả discovery.
### Các bước (`chưa kiểm`)
1. Mở từng file bằng `file://` — không lỗi console, không request mạng ra ngoài.
2. Duyệt mọi Màn × Trạng thái bằng thanh công cụ; chụp ảnh từng tổ hợp vào `playtest/epic1-phase2/<vai>/`.
3. Chỉ dùng phím (↑↓←→ Enter Backspace): đi hết luồng màn app TV từ "Lần đầu" tới "Đã kết nối" và tắt lớp phủ — focus luôn nhìn thấy, không bị dời sau thao tác.
4. Kiểm chữ Việt: font Be Vietnam Pro được dùng (không phải fallback) — DevTools *Rendered Fonts*.
**Reviewer chạy lại**: không (không có code sản phẩm); SA soát trải nghiệm thay thế.

## Bàn giao

Không có. Mockup là đầu vào cho Phase 3.
