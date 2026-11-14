---
name: prepare-code-branch
description: Chuẩn bị nhánh code cho một đợt update — xác định repo bị đụng, kiểm hiện trạng từng repo, đề xuất tên nhánh theo chuẩn và đưa khối lệnh cho User tạo. Chạy sau khi brainstorm đã chốt Epic/Phase, trước /write-implementation-docs. AI không tự tạo nhánh.
---

# Chuẩn bị nhánh code

Nền tảng: [`workflow/guidelines/git.md`](../../../workflow/guidelines/git.md) — đọc nếu chưa nắm. Đây là Bước 7 của [`01-brainstorm`](../../../workflow/leader-stages/01-brainstorm.md).

```
/brainstorm → /prepare-code-branch → /write-implementation-docs → /kickoff-code-workflow
```

Vì sao đứng ở đây: brainstorm Bước 6 đã chốt Epic/Phase ⇒ **đã biết đụng repo nào**; viết tài liệu và code thì cần nhánh sẵn sàng, để mọi commit của đợt nằm đúng chỗ ngay từ dòng đầu.

## 🔴 Nguyên tắc tối thượng

**AI KHÔNG tự chạy lệnh tạo nhánh** — trừ khi User ủy quyền rõ cho đúng việc này trong đợt này (ghi nguyên văn vào log). AI **khảo sát + đề xuất + đưa lệnh sẵn**, rồi **chờ User xác nhận**.

---

## Bước 1 — Xác định repo bị đụng

Đọc `project-docs/[YYMMDD]-*/[YYMMDD]-draft.md`, lấy danh sách module từ Epic/Phase đã chốt. Danh sách repo + nhánh chuẩn của dự án: `CLAUDE.md` mục *Cấu trúc repo*.

⚠️ **Không đoán từ tên đợt.** Một đợt "thêm màn quản trị" có thể đụng cả backend lẫn admin. Draft chưa nói rõ ⇒ hỏi User.

`project-docs` luôn bị đụng (mọi đợt viết tài liệu) nhưng là ngoại lệ — **không tách nhánh**.

## Bước 2 — Khảo sát hiện trạng từng repo

`git status` ở root không thấy gì trong module ⇒ kiểm riêng từng repo:

```bash
for d in <các repo bị đụng> project-docs; do
  printf "%-18s %-36s %s\n" "$d" "$(git -C "$d" branch --show-current)" "$(git -C "$d" status --short | wc -l) file đổi"
done
```

Báo từng repo: đang ở nhánh nào · có thay đổi chưa commit không · nhánh chuẩn có cần `git pull` không (`git -C <repo> fetch` rồi so `HEAD` với `origin/<nhánh chuẩn>`).

🔴 **Repo còn thay đổi chưa commit** ⇒ báo User xử lý trước, không tạo nhánh đè lên.

## Bước 3 — Đề xuất tên nhánh

Một tên duy nhất cho mọi repo của đợt: `<features|enhancements|bugs>/<YYMMDD>-<slug>`

- `YYMMDD` **lấy đúng từ thư mục đợt** trong `project-docs/`.
- `slug` ngắn, gạch nối, tiếng Anh — không lặp lại tiền tố.

## Bước 4 — Đưa lệnh cho User

Một dòng mỗi repo; repo có nhánh chuẩn khác `main` (theo `CLAUDE.md`) thì tách từ nhánh đó:

```bash
git -C api-service checkout main && git -C api-service pull && git -C api-service checkout -b <tên-nhánh>
git -C admin-console checkout main && git -C admin-console pull && git -C admin-console checkout -b <tên-nhánh>
# project-docs: không tách nhánh, làm thẳng trên main
```

Kèm ghi chú: repo nào tách từ đâu, repo nào bỏ qua và vì sao.

## Bước 5 — Verify sau khi User tạo xong

Chạy lại lệnh khảo sát ở Bước 2. Mọi repo trong danh sách phải đứng đúng nhánh mới (trừ `project-docs` ở `main`). Còn repo ở nhánh chuẩn mà đợt sẽ sửa ⇒ **báo User, chưa sang viết tài liệu**.

## Bước 6 — Ghi vào draft

Mục *Nhánh code* trong draft: tên nhánh · bảng repo (nhánh tách từ đâu · commit gốc `git -C <repo> rev-parse --short HEAD`) · repo ngoại lệ. `/kickoff-code-workflow` đọc lại mục này để verify tiền đề.

Xong ⇒ `/write-implementation-docs`.
