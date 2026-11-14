# Blackbox Testing Guidelines

**Mục đích**: test end-to-end bằng cách gọi API/UI thật và verify hành vi hệ thống.

**Khác unit test** (logic thuần có mock): [tdd](./tdd.md). **Cách làm thực tế** (server local, DB, domain, tài khoản): [automation](./automation.md) — 🔴 đọc trước mọi lượt blackbox.

---

## Blackbox smoke sau khi code — BẮT BUỘC với Phase có API/UI

Definition of done **không dừng ở unit test pass**:

```
1. Unit test xanh (nếu Phase có logic thuần)
2. Blackbox smoke: setup theo automation → gọi API/UI thật luồng chính → verify 3 lớp
```

Smoke thì **nhẹ về phạm vi** (luồng chính, không phải QA đầy đủ) — nhưng **phần setup không nhẹ**: vẫn đi qua luồng chuẩn bị tài khoản/dữ liệu chuẩn.

Phase doc loại này phải có section **"Blackbox Verify"** ([03-write-docs §4](../leader-stages/03-write-docs.md)).

---

## 🔴 Công cụ harness được gọi tên trong kế hoạch: mở đọc ĐẦU RA của nó

Mỗi công cụ mà bước blackbox gọi tên (script đăng nhập, sinh danh tính, dựng dữ liệu…) ⇒ trước khi viết bước dựa vào nó, **mở đọc chỗ nó xuất ra** (hoặc chạy thử một lần). Đừng nhớ *"harness này ký credential"* rồi mặc định nó đưa ra credential dùng được ở mọi đường — script có thể chỉ in đường dẫn hoặc ID. Nhãn `chưa kiểm` chỉ nói *kết quả chưa biết*, không che được một bước **bất khả thi**.

## 🔴 Chiến lược phạm vi — hai vế ngang nhau

**Vế 1 — ngoài scope Phase thì đừng chạy thật.** Thiết kế smoke theo câu hỏi *"Phase này đụng code gì, và cách RẺ NHẤT chứng minh nó đúng là gì?"* — không mặc định gọi endpoint chạy trọn pipeline.

- Phần **ngoài scope** chứng minh "không bị đụng" bằng unit test + đọc code, **không** sinh ảnh/gọi LLM thật tốn tiền.
- Phần **đã pass ở Phase trước** không chạy lại, trừ khi Phase này sửa code chung với nó.
- Full pipeline chạy **một lần** ở Phase cuối Epic, khi cần chứng minh cả chuỗi.
- Thao tác tốn tiền/thời gian mà **không phải chính tính năng đang kiểm** (dựng dữ liệu nền) ⇒ dùng liều nhỏ nhất (vd sinh 1 ảnh).

> Ca thật: smoke gọi một endpoint luôn chạy full ~23 ảnh, mỗi ảnh 1–3 phút ⇒ 30–60 phút; nhóm ảnh ngoài scope hoàn toàn vẫn bị sinh thật, tốn tiền mà không chứng minh được gì.

**Vế 2 — trong scope Phase thì chạy quy mô thật tới khi chắc tính năng tốt.** "Tối giản" = chọn công cụ rẻ nhất **cho từng case**, **không phải bỏ bớt case**, cũng **không** cắt liều của chính tính năng tới mức không thử được nó.

> Ca thật: blackbox dùng fixture 1 node vì chắc chắn tái hiện một ca lỗi — nhưng 1 node **không thử được** chính tính năng: vòng lặp nhiều node, thứ tự, guard tổ tiên, hiển thị tiến trình nhiều mục. User: *"smoke test và blackbox không cần cố với liều nhỏ nhất. Quan trọng là phải đảm bảo tính năng hoạt động tốt."*

Câu tự hỏi: ***"Nếu chỉ chạy đúng thế này, mình có dám nói với User là tính năng chạy tốt không?"*** Chưa dám thì chưa đủ.

### Bản đồ case → công cụ đỡ

Trong Phase doc, lập bảng: **mỗi case một thứ đỡ nó** (unit test · `grep` · compiler · smoke thật), không case nào trống.

- 🔴 **Kiểm chứng trước khi viện dẫn**: grep xác nhận test case tồn tại thật **trước** khi dựa vào nó để cắt smoke.
- Cảnh giác case **compiler không đỡ được** — điển hình là asset load bằng string runtime (`loadPrompt('tên_file')`): xoá nhầm file thì build, unit test (đã mock loader), smoke đều xanh mà production nổ. Loại này cần guard riêng (vd đối chiếu mọi tên được load với file thật).

---

## Verify là ba lớp

`200 OK` **không** phải là verify.

| Lớp | Kiểm gì |
|---|---|
| **Response** | Status, shape JSON, giá trị field quan trọng |
| **Database** | Query lại bảng bị ảnh hưởng — ghi đúng chưa, có thừa/thiếu bản ghi không |
| **Log** | Luồng chạy qua đúng nhánh code mong đợi, không exception bị nuốt |

Luồng bất đồng bộ (sinh ảnh, job nền) ⇒ response trả về **trước khi** việc xong — đợi và kiểm kết quả cuối ở DB, không kết luận từ response.

UI ⇒ thêm lớp **trực quan + network**: DOM sau render, số/thứ tự request thật, console, ảnh đặt tên tường minh. 🔴 Cách làm đầy đủ — dựng danh tính, SDK giả, mock chọn lọc, report: [blackbox-ui-playwright](./blackbox-ui-playwright.md).

---

## Dữ liệu test độc lập với người code

Khi review/test lại, **không dùng chính dữ liệu Dev đã dùng**. Logic phân nhánh theo identity/type/role/status ⇒ case phải **đổi đúng khoá đó**. Chạy lại đúng fixture của Dev không bao giờ bắt được lớp lỗi hardcode.

---

## Blackbox Android điều khiển bằng `adb`

🔴 **Sổ tay đầy đủ: [blackbox-android-adb.md](./blackbox-android-adb.md)** — vòng đời thiết bị/emulator, cài APK, dump bounds + tap, seed/đọc state, bắt traffic, dọn dẹp, và các bẫy. Ba rule hay quên nhất:

- 🔴 **Toạ độ chạm lấy từ `uiautomator dump`**, không ước từ ảnh chụp; dump lại sau mỗi thay đổi layout. "Chạm không ăn" ⇒ dump bounds trước khi kết luận bug.
- 🔴 **Bắt traffic bằng app VPN**: dừng capture xong phải kiểm không còn `tun0` (`adb shell ip addr`), hoặc force-stop app VPN — VPN treo làm app mất mạng, trông như lỗi backend.
- 🔴 **Dọn emulator: kill đúng cây tiến trình, đừng tin `adb emu kill`** — và kiểm lại sau 10–15 phút vì nó có thể tự bật lại.

---

## Những thứ unit test không bắt được — blackbox phải bắt

1. Đường đi thật qua hệ thống: controller → DB → response; job nền; dịch vụ ngoài.
2. Prompt LLM — phải render, đọc output thật, gọi LLM thật.
3. Tương thích client đã phát hành — response đổi shape thì client không compile-error, chỉ chết lúc chạy.
4. Idempotency và retry của listener/webhook — gửi lại hai lần có nhân đôi dữ liệu không.
5. Chữ ký/xác thực phụ thuộc thời gian (vd `auth_date`) — harness ký bằng chính đồng hồ máy nên không bao giờ thấy lệch; chỉ client thật mới lộ.
