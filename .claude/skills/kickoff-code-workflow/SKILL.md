---
name: kickoff-code-workflow
description: Khởi động thi công một đợt update sau khi Phase doc đầu tiên đã có SA verdict PASS — verify tiền đề (thư mục đợt, doc, nhánh, working tree), setup permission, probe platform worker (quyền, browser, loại nội dung), tạo file lessons + sổ session, ghi mốc thi công vào workflow log. AI đóng vai Team Leader, không tự code.
---

# Kickoff thi công

Chạy **một lần mỗi đợt**, sau khi Phase 1 Epic 1 đã qua SA gate ([`03-write-docs.md`](../../../workflow/leader-stages/03-write-docs.md)). Rồi sang [`04-implement.md`](../../../workflow/leader-stages/04-implement.md).

## ⚠️ Nguyên tắc tối thượng

1. **Không tự code.** Skill này chỉ setup luồng.
2. **Một đợt — một workflow log.** Log đã tạo từ brainstorm ⇒ **append tiếp**, không tạo mới đè lịch sử.
3. **Không tự tạo nhánh / commit.** Thao tác git thuộc User trừ khi được ủy quyền rõ.

---

## Bước 1 — Verify tiền đề

1. Thư mục `project-docs/<đợt>/` có: `draft` · `discovery` · tài liệu triển khai theo cấu trúc đã chọn ở [03 — Chọn cấu trúc](../../../workflow/leader-stages/03-write-docs.md#chọn-cấu-trúc) (3 cấp: `overview` · `epic1` · `epic1-phase1` — single document: `implementation` — draft-only: kế hoạch nằm trong draft) · SA review PASS của đúng tài liệu sẽ thi công.
2. `<đợt>-workflow-log.md` **đã tồn tại** (từ brainstorm). Chưa có ⇒ tạo theo `workflow/templates/workflow-log-header.md`, **không ghi đè**.
3. Nhánh — **ở từng repo bị đụng** (danh sách trong draft, mục nhánh do `/prepare-code-branch` ghi): `git -C <repo> branch --show-current` đúng `<prefix>/<YYMMDD>-<slug>` của đợt · `git -C <repo> status --short` sạch. Repo nào còn ở nhánh chuẩn mà đợt sẽ sửa ⇒ **báo User, dừng**. `project-docs` ở `main` là đúng chuẩn.
4. **Environment preflight** — ghi vào log: nhánh + commit **từng repo** · `DATABASE_URL` trỏ DB nào (che credential, đối chiếu mục *Môi trường* của `CLAUDE.md`) · port các module · baseline test từng module bị đụng.

## Bước 2 — Permission

Kiểm `.claude/settings.local.json` đủ wildcard cho Leader làm việc không bị hỏi từng lệnh (thêm nếu thiếu, cần User duyệt nếu harness chặn):

- `PowerShell(*)` · `Bash(powershell:*)`
- `Read(<root project>\**)` · `Write(<root project>\**)` · `Edit(<root project>\**)`

## Bước 3 — Probe platform worker

Platform dự kiến dùng = theo `workflow/agent-platform/worker-policy.local.md` (chưa có ⇒ cùng User lập từ `worker-policy.sample.md` trước, hook chặn gọi worker khi thiếu). Với **mỗi platform đó** (profile ở `workflow/agent-platform/`; chưa có profile ⇒ probe đủ ba mục dưới rồi viết profile):

- CLI **đổi version** kể từ lần probe ghi trong profile ⇒ probe lại: chạy shell (in hash commit), ghi file (token ngẫu nhiên), đọc file.
- Đợt có smoke web ⇒ **probe browser** (công thức trong `agent-platform/README.md`).
- Đợt có nội dung nhạy cảm ⇒ probe model có chịu làm **đúng loại nội dung** đó không.

Ghi kết quả vào log và cập nhật profile. Probe hỏng ⇒ vá trước, giao việc sau.

## Bước 4 — Tạo `<đợt>-lessons.md`

Theo `workflow/templates/lessons.md`. Đã có ⇒ giữ nguyên. Link từ overview.

## Bước 5 — Tạo `agent-sessions.md`

Theo `workflow/templates/agent-sessions.md`. Đã có (vd từ lượt discovery) ⇒ giữ nguyên, bổ sung dòng. Hook `check-worker-session.js` đọc file này.

## Bước 6 — Level tự chủ & mốc log

Level mặc định **narrow** (User nghiệm thu sau mỗi Phase). User đã chỉ đạo khác ⇒ theo đó, ghi nguyên văn vào Epic doc mục *Định hướng User đã chốt*.

Ghi một dòng (giờ lấy trong cùng lệnh ghi):

```
YYYY-MM-DD HH:mm — Leader — <platform>/<model> — Kickoff thi công. Phase 1 Epic 1 SA PASS. Preflight: <nhánh@commit>, DB <tên>, baseline <…>. Probe: <kết quả>. Level <narrow|general>. Sẵn sàng giao Dev lượt 1.
```

Điền **checklist mở Phase** ([`team-leader.md §6`](../../../workflow/roles/team-leader.md)) vào log.

## Bước 7 — Báo User ngắn, rồi đi tiếp

```
Log: project-docs/<đợt>/<đợt>-workflow-log.md · Lessons · Sessions — đã sẵn.
Level: <narrow|general>. Em giao Dev lượt 1 (đọc + phản hồi doc) ngay bây giờ.
Anh sẽ được hỏi khi: chạm risk gate · va chạm định hướng · <narrow: hết mỗi Phase>.
```

⇒ Giao Dev theo skill `/delegate` **trong cùng lượt** — không kết thúc lượt bằng kế hoạch.
