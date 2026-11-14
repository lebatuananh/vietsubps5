# project-docs/ — tài liệu của DỰ ÁN

🔴 **Dự án là monorepo** — thư mục này nằm chung repo với code. Tài liệu đợt commit trên nhánh đợt cùng code; sửa ngoài đợt (vd `automation/`) commit thẳng `main` ([CLAUDE.md — Cấu trúc repo](../CLAUDE.md#cấu-trúc-repo)).

Thư mục này chỉ chứa thứ **đặc thù dự án**. Quy trình làm việc chung nằm ở [`../workflow/`](../workflow/README.md) (repo wrapper).

| Thư mục / file | Chứa gì | Ai cập nhật, khi nào |
|---|---|---|
| [`present/`](./present/) | **Hiện trạng** hệ thống: luồng nghiệp vụ, cơ chế, nợ kỹ thuật | Cuối mỗi Phase/đợt đụng tới luồng đó |
| [`automation/`](./automation/) | Giá trị cụ thể cho test: DB dev, tài khoản test, script tái dùng | Khi dựng tài khoản mới, khi môi trường đổi, khi gặp bẫy |
| [`overview-project.md`](./overview-project.md) | Danh sách các đợt update | Mở đợt / bàn giao / đóng đợt |
| `[YYMMDD]-[chủ-đề]/` | Tài liệu một đợt: draft · discovery · overview → epic → phase · SQL · mockup · log · lessons · sessions · review · handover | Suốt đợt |

## Quy ước

- Thư mục đợt: `[YYMMDD]-[chủ-đề]` — `YYMMDD` = ngày bắt đầu, trùng với tên nhánh git của đợt ở các repo module.
- File trong đợt đều mang prefix `[YYMMDD]-` (trừ `agent-sessions.md` và các thư mục con).
- Root thư mục đợt chỉ chứa tài liệu chính (draft · discovery · overview · epic · phase · SQL · workflow-log · lessons · agent-sessions · handover); còn lại vào thư mục con: `reviews/` (SA review, dev feedback/report, review) · `mockups/` · `playtest/<epic-phase>/<vai>/` · `spikes/` · `ux/` · `_workers/{briefs,runs,streams}/` (`streams/` gitignore).
- Mỗi đợt có **một file SQL duy nhất** `[YYMMDD].sql` gom mọi script (schema + data), comment phân tách theo Phase.
- Tài liệu `present/` mô tả **hiện trạng**, không chứa kế hoạch. Kế hoạch sống trong thư mục đợt.
- Quy ước code của module **không** ở đây — ở `README.md` của module.
