# [YYMMDD] <Tên đợt> — Draft

> Tạo ở [01-brainstorm bước 2](../leader-stages/01-brainstorm.md), ngay sau khi chốt 6 ô. Cập nhật dần ở bước 3–7.
> **Workflow log**: [[YYMMDD]-workflow-log.md]([YYMMDD]-workflow-log.md)

---

## 1. Sáu ô — bằng lời User

| # | Ô | Lời User (trích dẫn) | Nguồn |
|---|---|---|---|
| 1 | Vì sao bây giờ | *"…"* | chat YYYY-MM-DD / CONCEPT.md / … |
| 2 | Sửa cái gì — ranh giới | *"…"* | |
| 3 | Sửa đến đâu là đủ | *"…"* | |
| 4 | Thứ tự | *"…"* | |
| 5 | Mốc deploy dev | *"…"* | |
| 6 | Đánh đổi chấp nhận được | *"…"* | |

> Ô nào chưa có lời User ⇒ để trống và hỏi tiếp. Không điền bằng suy đoán.

## 2. Khảo sát sơ bộ (Leader)

- Luồng/module liên quan: …
- Tài liệu hiện trạng đã đọc: …
- File/pattern tham khảo: …

## 3. Discovery (tóm tắt — chi tiết ở `[YYMMDD]-discovery.md`)

- Phát hiện làm đổi quyết định: …
- Chỗ va chạm với định hướng User + cách đã chốt: …
- Dứt khoát KHÔNG làm (User đã duyệt): …

## 4. Phương án

- Kỹ thuật (tối giản, tương xứng): …
- Dữ liệu / schema: …
- Đụng *Ràng buộc đặc thù* / *Risk gate riêng* nào của `CLAUDE.md`: …

## 5. Nghiệm thu & test 2 tầng

- Metric thành công: …
- Tầng 1 — smoke: tài khoản · setup · API · verify · dữ liệu test (giữ/dọn) · Reviewer có chạy lại smoke không
- Tầng 2 — End User: có/không · mốc nào

## 6. Tài liệu cần cập nhật

- [ ] `project-docs/present/…`
- [ ] `<module>/README.md`

## 7. Lộ trình

| Epic | Phase | Làm gì | Xong thì deploy dev được? | TDD? |
|---|---|---|---|---|

## 8. Nhánh code (ghi bởi `/prepare-code-branch`)

- Nhánh (cùng tên ở mọi repo bị đụng): `<prefix>/[YYMMDD]-<slug>`

| Repo | Tách từ | Commit gốc | Ghi chú |
|---|---|---|---|
| `api-service` | `main` | `…` | |
| `project-docs` | — | — | không tách nhánh, làm trên `main` |
