# Khuôn header workflow log — Direct Command

> Cách ghi từng dòng: [practices/workflow-log.md](../practices/workflow-log.md).
> Tạo ở [01-brainstorm bước 2](../leader-stages/01-brainstorm.md), ngay sau khi chốt 6 ô.

---

## Template

```markdown
# Workflow Log — Đợt [YYMMDD] <tên đợt>

**Mode**: Direct Command · level `narrow` | `general`
**Draft**: [[YYMMDD]-draft.md]([YYMMDD]-draft.md) · **Overview**: [[YYMMDD]-overview.md]([YYMMDD]-overview.md) · **Lessons**: [[YYMMDD]-lessons.md]([YYMMDD]-lessons.md) · **Sessions**: [agent-sessions.md](agent-sessions.md)

## Roles — ai đóng vai gì, chạy trên đâu

| Vai | Platform / model | Ghi chú |
|---|---|---|
| Team Leader | <vd claude-code / claude-opus-5-5> | |
| BA/SA | <TBD> | |
| Dev | <TBD> | |
| Reviewer | <TBD> | **Khác session Dev** — lý tưởng khác platform/pool |
| End User | <chỉ khi đợt đụng nội dung AI> | |

> Cập nhật platform/model thật khi gọi vai đó lần đầu — **ngoại lệ duy nhất** được sửa ngoài phần append.

## Ủy quyền & định hướng — ⚠️ KHÔNG ghi ở đây

Ghi ở Epic doc, mục **Định hướng User đã chốt**. Leader chỉ đọc đuôi log ⇒ header là vùng chết.

## Required Reading — cho mọi agent vào đợt

1. `CLAUDE.md`
2. `workflow/roles/<vai của bạn>.md` — **đọc trọn**
3. `project-docs/[YYMMDD]-<chủ-đề>/[YYMMDD]-draft.md` — 6 ô User đã chốt
4. `project-docs/[YYMMDD]-<chủ-đề>/[YYMMDD]-discovery*.md` — hiện trạng đã khảo sát
5. Phase doc được brief trỏ tới

---

<!-- Append-only, mới nhất ở cuối. Khuôn: YYYY-MM-DD HH:mm — <vai> — <platform>/<model> — <nội dung>
     Giờ lấy bằng lệnh hệ thống TRONG CÙNG lệnh ghi — không gõ tay. -->

YYYY-MM-DD HH:mm — Leader — <platform>/<model> — Mở đợt. Chốt 6 ô với User → [draft]([YYMMDD]-draft.md).
```
