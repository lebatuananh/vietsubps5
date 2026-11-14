# TDD Guidelines — Unit test

**Mục đích**: quy định cách viết unit test cho business logic. Tài liệu **nguyên tắc** — không mô tả hạ tầng test của từng module (lệnh, framework nằm trong `README.md` module).

**Phạm vi**: unit test cho logic thuần. Test qua API/UI thật thuộc [blackbox-testing](./blackbox-testing.md).

---

## Quy tắc bắt buộc

**Mọi Phase có business logic thuần đều phải có unit test.**

"Logic thuần" = test độc lập được, không cần DB/network/LLM thật:
- Pure function, transformation (map entity → response, parse/format).
- Threshold, điều kiện, state machine (tính level, xét điều kiện mở khoá, chọn nhánh).
- Validation rule, tính toán (giá, điểm kinh nghiệm, giới hạn theo ngày).

**Không** bắt buộc unit test cho: code chỉ ghép I/O, config, wrapper mỏng quanh SDK ngoài.

### ⛔ Cấm giấu pure logic trong controller

"Controller chỉ ghép I/O" **không phải cửa thoát**. Controller có slice, filter, transform, so ngưỡng, chọn nhánh nghiệp vụ ⇒ **bắt buộc extract thành pure function + unit test**.

🔴 Không để inline để né test. Không viết kế hoạch nước đôi *"nếu tách hàm thì unit test, nếu inline thì dựa smoke"* — Leader (người viết Phase doc) phải **chốt**, vì đó chính là cách bug lọt.

> Ca thật: logic cắt mảng tin nhắn inline trong controller có case biên `findIndex = -1` → mảng rỗng → 500. Không unit test nên lọt tới tận blackbox.

**Blackbox là lưới cuối, không phải lưới đầu.** Nhận ra pure logic: hỏi được *"với input X thì kết quả phải là Y"* mà không nhắc HTTP, DB hay LLM.

---

## Ba nguyên tắc cốt lõi

### 1. Red phải là runtime failure, không phải compile error

Trong ngôn ngữ có kiểu, hàm chưa tồn tại thì không compile — bạn chưa vào vòng TDD. **Stub `throw new Error('not implemented')` là bước đầu của TDD**, không phải bước phụ.

⇒ Quy trình đúng: **Stub → Test (Red) → Implement (Green)**.

### 2. Test khó viết là code smell — refactor code, đừng refactor test

Phải mock quá nhiều thứ để test một hàm ⇒ hàm đang làm quá nhiều việc. Extract logic thành pure function trước — pure function không cần mock.

### 3. Hiểu scope và lifetime của mock trước khi viết test dựa vào nó

Module auto-mock ở top-level thì `mockRestore()` không lấy lại được implementation thật; describe block cần hành vi thật ⇒ dùng `requireActual` (Jest) / `importActual` (Vitest). "Mock chạy" ≠ "test đang test đúng thứ".

---

## Quy trình

### Tính năng mới

```
1. Stub (throw 'not implemented')  → compile được
2. Viết unit test                  → chạy → confirm FAIL (Red)
3. Implement                       → chạy → confirm PASS (Green)
4. Refactor nếu cần                → chạy → vẫn PASS
```

**Bước 2 fail là bắt buộc** — test pass ngay khi chưa implement là test sai thứ.

### Refactor code đã có (chưa có test)

```
1. Viết test khoá hành vi HIỆN TẠI (characterization test — khác blackbox smoke) → PASS (chốt baseline)
2. Refactor                              → vẫn PASS
3. Unit test cho helper vừa extract      → PASS
```

Bước 3 fail ở refactor ⇒ hoặc refactor sai, hoặc hành vi cũ là bug — cả hai đều dừng lại xem xét, **không sửa test cho xanh**.

---

## Quy ước

- **Colocate**: `foo.ts` cạnh `foo.test.ts`, không để thư mục `__tests__` riêng.
- **Tên test mô tả hành vi**: ✅ `returns null when profile has no avatar` · ❌ `test buildProfile`.
- **Arrange → Act → Assert**, mỗi test **một** hành vi.
- **Fixture helper** tạo test data ngắn gọn, không lặp object trong từng case.

### Case phải phủ

| Loại | Ví dụ |
|---|---|
| Happy path | Input hợp lệ thông thường |
| Biên | 0, rỗng, 1 phần tử, max, vừa đủ / vừa thiếu ngưỡng |
| Null/undefined | Field optional vắng mặt |
| Sai | Input không hợp lệ → ném đúng error class |
| Trim/case | Nếu logic có `toLowerCase`/`trim` |
| Trùng lặp / thứ tự | Input trùng; kết quả phụ thuộc thứ tự |

---

## Mock cái gì

**Mock ở biên hệ thống** (chỗ chạm ra ngoài process): DB/repository, HTTP client, LLM client, queue, storage, thời gian, random.

**Không mock** chính logic đang test và các pure helper nó gọi — mock helper thuần thì test chỉ còn kiểm "hàm có gọi hàm kia không".

Unit test chạy được **không cần DB, network, server**, cùng kết quả ở mọi máy. Không đọc `.env` thật, không gọi API thật.

---

## Integration test nhiều mock — checklist TRƯỚC khi viết dòng test đầu tiên

1. **Trace toàn bộ execution path** của method: mỗi dependency được gọi — module nào, method nào, **field nào của return value được truy cập ngay sau đó** ⇒ mock phải trả đúng các field đó.
2. **Đọc đủ type definition** của mọi object cần stub — kể cả field không bắt buộc theo type nhưng method có truy cập (thiếu là crash runtime).
3. **Đọc hàm dựng biến template** nếu test đụng prompt ⇒ biết context có những key gì.
4. **Xác nhận ngữ nghĩa render của template engine** (vd Mustache `{{x}}` escape HTML, `{{{x}}}` giữ nguyên).

Phase doc yêu cầu integration test phải có section **"Files bắt buộc đọc trước khi code"** liệt kê đúng các file trên. Thiếu ⇒ worker đoán và crash.

---

## Definition of Done liên quan tới test

Phase **chưa xong** nếu:

- [ ] Logic thuần mới/sửa chưa có unit test phủ happy path + biên + null.
- [ ] Test suite chưa xanh **toàn bộ** (không chỉ test mới).
- [ ] Test mới chưa từng được thấy fail trước khi implement.

**So với baseline**: chạy test trước khi bắt đầu, ghi lại. Sau khi xong, số pass/fail phải giải thích được. "Vốn đã fail từ trước" là câu hợp lệ — nhưng phải **biết**, không đoán.

### 🔴 Không sửa test để làm nó xanh

Test fail ⇒ mặc định **code sai**. Chỉ sửa test khi chứng minh được kỳ vọng viết sai từ đầu — và nói rõ trong báo cáo. **Tuyệt đối không**: nới assertion, `skip` test đang fail, hardcode giá trị cho khớp fixture — cả ba là **blocking defect**.

---

## Giới hạn của unit test

Unit test xanh **không** chứng minh tính năng chạy. Hai vùng nó không bảo vệ:

1. **Đường đi thật qua hệ thống** (controller → DB → response, job nền, dịch vụ ngoài) ⇒ [blackbox](./blackbox-testing.md).
2. **Prompt LLM** — sửa prompt thì build và unit test đều xanh kể cả khi prompt hỏng hoàn toàn ⇒ render + gọi thật ([prompt-llm](./prompt-llm.md)).

⇒ Phase có API/web: DoD gồm **cả** unit test xanh **và** blackbox smoke luồng chính.
