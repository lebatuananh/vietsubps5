---
name: brainstorm
description: Công đoạn đầu tiên của một đợt update — tiếp nhận yêu cầu từ User, hỏi từng câu một để lấp đủ 6 ô bắt buộc (vì sao · sửa cái gì · đủ đến đâu · thứ tự · mốc deploy dev · đánh đổi), tạo draft + workflow log, khảo sát sơ bộ, dẫn dắt discovery với BA/SA, chốt Epic/Phase và nhánh. Dùng khi User nêu một ý tưởng/nhu cầu mới cần phân tích trước khi viết tài liệu triển khai.
---

# Brainstorm — công đoạn 01

🔴 **Đọc trọn và làm theo**: [`workflow/leader-stages/01-brainstorm.md`](../../../workflow/leader-stages/01-brainstorm.md)

Quy trình đầy đủ nằm ở file đó, viết trung lập platform. Skill này chỉ là con trỏ + phần đặc thù Claude Code.

## Ba thứ không được quên

**1. Một câu mỗi lần, nạp bối cảnh trước khi hỏi.** Đọc `CLAUDE.md` + đợt liền trước, **tự điền ô đã rõ (kèm trích dẫn)**, chỉ hỏi ô thật sự thiếu. Hỏi bằng **text thường, tiếng Việt có dấu** — **không dùng `AskUserQuestion`**.

**2. Sáu ô phải đầy** trước khi sang bước 2 — chỉ User trả lời được:

```
[ ] 1. Vì sao bây giờ — điều gì đang đau, ai đang chịu
[ ] 2. Sửa cái gì — phạm vi, ranh giới tới đâu
[ ] 3. Sửa đến đâu là đủ — mức "đủ tốt" cho đợt này
[ ] 4. Thứ tự — cái gì làm trước, vì sao
[ ] 5. Mốc deploy dev — xong cái nào thì đẩy lên dev được
[ ] 6. Đánh đổi chấp nhận được
```

Ghi mỗi ô **bằng lời User, có trích dẫn**. Không hỏi đóng để lấp ô. Không hỏi thứ khảo sát ra được hoặc thứ là hệ quả của cái đã chốt.

**3. Tạo sớm** — ngay sau khi đủ 6 ô: thư mục `project-docs/[YYMMDD]-[chủ-đề]/`, `[YYMMDD]-draft.md`, `[YYMMDD]-workflow-log.md` (dòng mở màn bằng giờ hệ thống thật, lấy trong cùng lệnh ghi).

## Sau bước 3

Mở phiên discovery **≥ 2 lượt** với BA/SA theo [`02-discovery.md`](../../../workflow/leader-stages/02-discovery.md) — mỗi brief có **mục mở không khoanh vùng**. Gọi worker theo skill `/delegate`. Rồi quay lại bước 4–7 (bước 7 = nhánh code ⇒ skill `/prepare-code-branch`).

## Đặc thù Claude Code

- Khảo sát: `Grep`/`Glob`/`Read` trước, shell sau.
- Skill không được nạp lại sau compact ⇒ context bị cắt giữa chừng thì **đọc lại file công đoạn**; hook `SessionStart` đã nạp sẵn nội dung skill này.

Xong ⇒ `/prepare-code-branch` → `/write-implementation-docs`.
