# [YYMMDD] — Epic <N> — Phase <M>: <Tên>

📋 **[Epic <N>]([YYMMDD]-epic<N>.md)** · **[Overview]([YYMMDD]-overview.md)**
**Revision**: Rev 1 — YYYY-MM-DD *(ghi Rev 2, 2.1… mỗi lần sửa theo SA/Dev, kèm tóm tắt đổi gì)*

---

## Hiện trạng

Rà soát kỹ thuật liên quan Phase này (từ discovery + verify của Leader):
- Cấu trúc code hiện tại, file/pattern liên quan, dependency.
- 🔴 **Claim của BA/SA đã verify là SAI** (nếu có): … — để Dev không đi "sửa" thứ đang đúng.

## Prerequisites

- ✅ <deliverable Phase trước>
- ✅ <setup/config cần có>

## Files bắt buộc đọc trước khi code

> 🔴 Bắt buộc với Phase có unit test / logic. Mỗi file **kèm lý do cụ thể** — thiếu thì worker đoán sai và mất nhiều vòng.

- [`<module>/src/…`](…) — vì sao: hàm trả shape gì / field nào nullable / template dùng biến gì
- [`<module>/…/Domain/X.cs`](…) + cấu hình trong DbContext (entity `X`) — field nào nullable, tên cột

## Kế hoạch

### Tổng quan

1–2 đoạn: Phase làm gì, deliverable là gì, xong thì deploy dev được chưa.

### Bước 1: <Tên> (code)

**Files**: …
**Thay đổi**: … — theo pattern `…` (file mẫu: …)
**Logic**: cách tiếp cận (không chép code) · business rule · edge case

### Bước 2: <Tên> (manual — SQL / cấu hình)

**Action**: … · **Target**: … · **SQL**: phần Phase <M> trong [`[YYMMDD].sql`]([YYMMDD].sql)
**Verify**: …

## Deliverables

Phase xong khi:
- ✅ <kết quả kiểm được 1>
- ✅ <kết quả kiểm được 2>
- ❌ <dứt khoát CHƯA làm ở Phase này>

---

## TDD Checklist (nếu Phase có logic thuần)

> ⛔ Pure logic trong controller (slice/filter/transform/threshold) **bắt buộc** extract thành pure function + unit test. Không để inline, không kế hoạch nước đôi. Xem [tdd](../../workflow/guidelines/tdd.md).

- [ ] Stub → viết test → **FAIL** → implement → **PASS**
- [ ] Refactor: test khoá hành vi hiện tại (characterization test) **trước** → refactor → vẫn PASS

**Unit test spec** — mỗi pure function:

| Case | Input | Expected |
|---|---|---|
| Happy path | | |
| Rỗng / null | | |
| Biên | | |

---

> Phase có màn / route / dialog UI mới ⇒ thêm hàng TDD **render** từng màn với mỗi trạng thái hiển thị chính (component test). Unit test logic không thay được bước này.

## Blackbox Verify (bắt buộc nếu Phase test được qua API/UI)

> Quy trình tư duy tường minh — không chung chung, không cầm tay ID cụ thể. Mỗi vai tự chuẩn bị tài khoản/data **riêng**. Dòng chưa chạy thử lần nào ⇒ ghi `chưa kiểm`. Xem [blackbox-testing](../../workflow/guidelines/blackbox-testing.md) · [automation](../../workflow/guidelines/automation.md).

### Chuẩn bị (vai tự làm)
- **Tài khoản**: đọc `project-docs/automation/test-accounts.md` → kiểm hiện trạng → thiếu thì dựng.
- **Data cần có**: <mô tả trạng thái — vd "tài khoản đã onboarding + 2 ghi chú + 1 bản tóm tắt AI">
- **Môi trường**: mặc định localhost. Bước nào cần đọc đối chiếu domain dev (User đã yêu cầu) ⇒ ghi rõ; không ghi dữ liệu lên dev.
- 🔴 **Dữ liệu test**: tạo ra gì · **có cần dọn không** — mặc định **để lại** và ghi vào `test-accounts.md`; chỉ dọn khi có lý do cụ thể (lẫn vào dữ liệu thật hoặc dùng chung, chiếm ràng buộc unique, làm sai số liệu, chứa dữ liệu nhạy cảm…) · cần dọn ⇒ viết thành **một bước trong *Các bước*** (dọn gì, theo đối tượng nào, kiểm lại thế nào).

### Các bước
1. <làm gì> — [local|dev] — inspect: <query/kiểm gì>
2. … *(nghi vấn ở bước nào ⇒ DỪNG, báo Leader, không đoán)*

**Reviewer chạy lại smoke**: không *(mặc định — Reviewer soi code, test/build và evidence của Dev)* | có — bước nào, vì sao *(Leader + SA chốt lúc thảo luận kế hoạch; vd luồng tiền, tích hợp ngoài, lỗi chỉ lộ khi chạy thật mà evidence của Dev không chứng minh được)*

### Bản đồ case → công cụ đỡ

| Case | Đỡ bằng (unit test / grep / compiler / smoke thật) | Trạng thái |
|---|---|---|

### Verify (3 lớp)
- [ ] Response: …
- [ ] DB: bảng/cột nào → giá trị mong đợi
- [ ] Log: dấu hiệu đúng nhánh
- [ ] Không được xảy ra: … (regression)
- [ ] UI (theo [blackbox-ui-playwright](../../workflow/guidelines/blackbox-ui-playwright.md)): network (số/thứ tự request) · 4 trạng thái mockup · thao tác "sai" · ảnh `NN-<mô-tả>.png` ở `project-docs/[YYMMDD]-…/playtest/epic<N>-phase<M>/<vai>/` · ghi rõ hàng nào mock, ≥ 1 luồng chính chạy thật

### Sau khi test — bắt buộc
- [ ] Cập nhật `test-accounts.md` (tài khoản, ID, trạng thái, bẫy đã vấp, giữ/xoá).
- [ ] Dọn dữ liệu test **chỉ khi kế hoạch có bước dọn**, làm đúng bước đó; không có ⇒ để lại, ghi vào `test-accounts.md`.
- [ ] Tắt server của mình, port trống.
- [ ] Bẫy môi trường / nợ kỹ thuật mới ⇒ `present/backlog-and-techdebt.md` hoặc README module.

---

## Bàn giao (điền nếu Phase đụng DB, env, hoặc client đã phát hành)

- **SQL**: có/không — phần nào trong `[YYMMDD].sql` · ⏱️ chạy **trước** (thêm) / **sau** (xoá) khi cập nhật image
- **Env mới**: `TÊN_BIẾN` — module · mục đích · giá trị local (KHÔNG secret production) · build-time hay runtime
- **Client**: có cần phát hành lại? người dùng bản cũ bị gì?
- **Kiểm sau deploy (cho dev/tester)**: gọi gì / nhìn gì để biết bản mới đã lên
