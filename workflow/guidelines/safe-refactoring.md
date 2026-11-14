# Safe Refactoring Guidelines

**Mục đích**: thứ tự thay đổi để refactor không bùng nổ lỗi runtime. Bổ sung cho [tdd](./tdd.md): TDD lo "test che lưng", file này lo "đổi theo thứ tự nào".

---

## Triết lý

Refactor an toàn: **tại mọi bước trung gian, hệ thống vẫn chạy — hoặc fail lúc compile, không bao giờ fail lúc runtime.** Không phải "đập ra rồi lắp lại", mà là chuỗi bước nhỏ, mỗi bước giữ codebase xanh.

---

## 1. XOÁ / THU HẸP: từ ngoài vào trong

Toàn hệ thống là **một** call stack, từ ngoài cùng (client, route, controller) vào sâu (service → repository → **database**). Database không phải trục riêng — nó là **tầng sâu nhất**.

```
Xoá:  Client → Controller → Service → Repository → DB column
      (ngoài, xoá trước) ────────────────────────► (trong, xoá sau)
```

Xoá một cột:
1. Ngừng **ghi** cột.
2. Ngừng **đọc** cột (mapping response, type/interface).
3. Cập nhật schema ORM + generate.
4. **Cuối cùng** mới `DROP COLUMN`.

Làm ngược = mọi query chạm cột đã mất nổ **đồng loạt ở runtime** — loại lỗi đắt nhất, khó khoanh vùng nhất.

⚠️ **"Tầng ngoài cùng" gồm cả client đã phát hành** (mobile app, extension…) — bản cũ trên máy người dùng vẫn gọi API. Bước 1–2 phải tính client cũ còn sống bao lâu (§4).

## 2. THÊM / MỞ RỘNG: từ trong ra ngoài

```
Thêm:  DB column → Repository → Service → Controller → Client
       (trong, thêm trước) ───────────────────────► (ngoài, thêm sau)
```

Thêm cột + apply SQL + generate **trước**, rồi mới viết code đọc nó.

## 3. Để compiler làm lưới an toàn

Xoá một producer trong khi còn consumer ⇒ typecheck báo lỗi **ngay lúc build** — fail an toàn. Chạy build/typecheck sau khi gỡ producer: pass nghĩa là không còn consumer sót trong code có kiểu.

Ba vùng compiler **không** bảo vệ:
- **DB** — ORM chỉ biết schema lúc generate, không biết trạng thái cột thật.
- **Prompt template / asset load bằng string** — đổi tên biến trong code mà quên template ⇒ template engine thay bằng chuỗi rỗng, im lặng.
- **Ranh giới giữa module / client đã phát hành** — không có compile chung.

## 4. Expand → Migrate → Contract

Thay đổi không xong trong một bước mà vẫn giữ hệ thống chạy (đổi tên cột đang dùng, đổi kiểu, đổi nghĩa field):

1. **Expand** — thêm cái mới song song cái cũ.
2. **Migrate** — chuyển consumer sang cái mới (ngoài vào trong), backfill nếu cần.
3. **Contract** — gỡ cái cũ khi không còn ai dùng.

Có client đã phát hành ⇒ pha Contract thường phải đợi qua vài bản release. Không chắc bản cũ còn đọc field đó ⇒ **giữ lại** — một cột thừa rẻ hơn nhiều so với một đợt crash hàng loạt.

---

## Checklist kế hoạch refactor

- [ ] Đã liệt kê **đủ** consumer (grep toàn repo, gồm prompt template và client)?
- [ ] Đúng chiều (xoá: ngoài→trong · thêm: trong→ngoài)?
- [ ] `DROP COLUMN` ở bước cuối?
- [ ] Có test khoá hành vi hiện tại (characterization test) **trước** khi refactor?
- [ ] Bước nào chạm client đã phát hành? Đã tính tương thích ngược?
- [ ] Mỗi bước trung gian hệ thống vẫn chạy?
- [ ] Có build/typecheck giữa các bước để bắt consumer sót?
