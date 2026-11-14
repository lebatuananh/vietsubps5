# Prompt LLM — cấu trúc chuẩn và cách verify

**Đọc trọn trước khi viết prompt mới hoặc sửa cấu trúc prompt cũ.**

Cơ chế cụ thể của dự án (prompt nằm ở đâu, load thế nào, có cache không, template engine gì) ⇒ `README.md` của module gọi LLM. File này chỉ nói **viết thế nào cho đúng** và **verify thế nào**.

---

## 🔴 Ba quy tắc bắt buộc

### 1. Prompt viết bằng tiếng Anh

Ngoại lệ duy nhất: phần **cố ý** yêu cầu LLM trả tiếng Việt (hoặc ngôn ngữ người dùng) — nêu rõ ràng buộc ngôn ngữ đó ngay trong prompt.

### 2. Vẫn phải mô tả nguyên văn đối tượng JSON

Kể cả khi đã khai báo structured output schema ở tầng code, prompt **vẫn phải mô tả đầy đủ cấu trúc JSON kèm ý nghĩa từng field**. Schema không thay được mô tả — thiếu thì output kém ổn định.

### 3. Hiểu cú pháp của template engine

Mustache (và nhiều engine khác) coi `{{...}}` là biến và **thay bằng chuỗi rỗng khi không có giá trị, không báo lỗi**. Muốn giữ nguyên văn placeholder cho LLM đọc thì đổi delimiter:

```
{{=<% %>=}}{{user.name}}<%={{ }}=%>
```

`{{x}}` escape HTML, `{{{x}}}` giữ nguyên — chọn đúng khi giá trị có ký tự đặc biệt.

---

## Khung cấu trúc

### `# Overview`
- **Vai trò (năng lực)** của AI.
- Giới thiệu ngắn về sản phẩm (lấy từ mục *Dự án* của `CLAUDE.md`).
- Nhiệm vụ sơ lược.

### `# Mechanics`
Các cơ chế nghiệp vụ **liên quan tới nhiệm vụ này** — chỉ cái thật sự liên quan. Nhồi toàn bộ nghiệp vụ vào mọi prompt làm loãng và tốn token.

### `# Context`
Thông tin của lượt gọi này: nhân vật nào, lịch sử, trạng thái hiện tại… — nơi đặt biến template.

### `# Task`
- `## Description` — mô tả chi tiết; nhiệm vụ có cấu trúc ⇒ **mô tả cấu trúc dữ liệu ở đây**.
- `## Rules` — **tối đa ~7** nguyên tắc. Quá 7 thì model bắt đầu bỏ sót ⇒ gom nhóm hoặc tách prompt.
- `## Examples` *(tuỳ chọn)* — input → output mong đợi; hiệu quả nhất với format đặc thù.
- `## Reasoning` — hướng dẫn model suy nghĩ theo trình tự trước khi trả lời.

---

## 🔴 Verify — không compiler nào bảo vệ prompt

Sửa prompt thì build **và** unit test đều xanh kể cả khi prompt hỏng hoàn toàn. Bắt buộc:

1. **Render template với dữ liệu thật → đọc output thật.**
   - *Đủ*: biến nào bị thay thành chuỗi rỗng (sai tên biến, quên escape)?
   - *Đúng*: mỗi giá trị có phải dữ liệu thật **của thực thể này**, hay bị hardcode/gán nhầm? Lớp này nguy hiểm hơn — prompt render hoàn hảo, chỉ nội dung sai.
2. **Gọi LLM thật ở liều nhỏ nhất → đọc kết quả**: JSON đúng cấu trúc không, nội dung theo nguyên tắc không.
3. ⚠️ **Prompt có bị cache không?** Nhiều module cache prompt in-memory sau lần load đầu ⇒ sửa file không có tác dụng cho tới khi **restart server**. Đây là lý do hay gặp nhất của *"prompt sửa rồi mà không ăn"*.
4. **File prompt có đang được dùng không?** Grep tên file trong code (≥ 2 biến thể cú pháp) trước khi sửa — sửa nhầm file chết thì không có gì xảy ra.

### Giới hạn độ dài — viết theo mục/câu, không theo ký tự

Model (nhất là khi tắt reasoning) không đếm được ký tự ⇒ trần kiểu *"≤ 1200 ký tự"* trong prompt bị vượt thường xuyên, còn validator cùng trần đó loại phần lớn output. Prompt nêu trần **đếm được** (số mục, mỗi mục một câu, số câu); validator chỉ chặn ở trần cứng **rộng hơn** mục tiêu (cỡ 2×).

Structured output ra `finish_reason = length` bất thường ⇒ đọc `content` thô trước khi nâng `max_tokens`: có provider ghi phần suy luận vào `content` khi bật reasoning. Thử tắt reasoning trước.

### Nội dung nhạy cảm / bị filter

Model và nhà cung cấp có lớp lọc nội dung riêng. Prompt chạm vùng nhạy cảm ⇒ khi verify, kiểm cả **tỷ lệ bị từ chối / bị cắt** và **dạng lỗi trả về** (refusal trong text, `finish_reason`, mã lỗi riêng của provider) — code phải map đúng các dạng đó, không coi refusal là output hợp lệ.

---

## Checklist trước khi báo xong một thay đổi prompt

- [ ] Prompt bằng tiếng Anh (trừ phần cố ý yêu cầu ngôn ngữ khác, đã nêu rõ).
- [ ] Đủ 4 section: Overview → Mechanics → Context → Task.
- [ ] ≤ ~7 rule.
- [ ] Trả JSON ⇒ đã mô tả nguyên văn cấu trúc + ý nghĩa từng field.
- [ ] Placeholder cần giữ nguyên văn đã escape/đổi delimiter.
- [ ] Đã xác nhận file prompt đang được load thật.
- [ ] Đã restart (nếu có cache), render thật, đọc output — không biến rỗng ngoài ý muốn, giá trị đúng thực thể.
- [ ] Đã gọi LLM thật ở liều nhỏ, output đúng cấu trúc và nguyên tắc.
- [ ] Giới hạn độ dài nêu theo mục/câu; validator có lề rộng hơn mục tiêu.
