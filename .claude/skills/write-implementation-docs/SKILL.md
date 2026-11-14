---
name: write-implementation-docs
description: Viết tài liệu triển khai 3 cấp (Overview → Epic → Phase) cuốn chiếu theo vertical slicing, rồi đưa Phase doc qua SA thẩm định (gate bắt buộc) trước khi thi công. Dùng sau khi brainstorm + discovery xong. Chỉ viết tài liệu, không code.
---

# Viết tài liệu triển khai — công đoạn 03

🔴 **Đọc trọn và làm theo**: [`workflow/leader-stages/03-write-docs.md`](../../../workflow/leader-stages/03-write-docs.md)

## Nguyên tắc tối thượng

1. **Cuốn chiếu** — chỉ viết Overview + Epic đang làm + Phase sắp làm. Không viết trước toàn bộ.
2. **Chỉ viết tài liệu, KHÔNG code** — trừ khi User nói rõ "code đi".
3. **Không chép source code hoàn chỉnh vào doc** — chỉ hướng dẫn từng bước.

## Tên file — trong thư mục đợt

`[YYMMDD]-overview.md` · `[YYMMDD]-epic[X].md` · `[YYMMDD]-epic[X]-phase[Y].md` · `[YYMMDD].sql` (một file cả đợt) · `mockups/[YYMMDD]-mockup-<màn>.html` · báo cáo quanh Phase ở `reviews/`

Template: `workflow/templates/{overview,epic,phase}.md`.

## Phase doc bắt buộc có

- **Files bắt buộc đọc trước khi code** (kèm lý do từng file) — nếu có test/logic.
- **TDD** đã chốt — không kế hoạch nước đôi.
- **Blackbox Verify** — quy trình tư duy tường minh; dòng chưa chạy thử ⇒ `chưa kiểm`.
- **Mockup `.html`** đủ Loading/Empty/Error/Overflow — nếu đụng UI.
- **Bàn giao** — nếu đụng DB / env / client đã phát hành (nguyên liệu cho gói bàn giao ở `05-handover`).
- **Decision Check** — nếu chạm risk gate.

## 🔴 Gate: SA thẩm định Phase doc

```
Leader viết Phase doc → giao SA (skill /delegate, brief có mục mở)
  → SA soi 6 tiêu chí: khớp định hướng · khớp code thật · đủ cho Dev · đủ cho smoke · khớp ràng buộc CLAUDE.md · trải nghiệm (nếu có UI)
  → BLOCKER ⇒ sửa (Rev x) ⇒ SA duyệt lại delta
  → PASS ⇒ ghi log
```

**SA PASS ⇒ báo User một đoạn tóm tắt rồi đi thẳng kickoff/giao Dev** — không dừng chờ User đọc lại doc. **Vẫn dừng chờ User** khi Phase có va chạm định hướng chưa chốt hoặc chạm risk gate.

Lần đầu của đợt ⇒ `/kickoff-code-workflow`. Các Phase sau ⇒ thẳng [`04-implement.md`](../../../workflow/leader-stages/04-implement.md).
