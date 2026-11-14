---
name: bootstrap-project
description: Dùng MỘT LẦN ngay sau khi clone PROJECT-TEMPLATE của AppFactory sang một dự án mới — dẫn User điền phần đặc thù của dự án (CLAUDE.md, project-docs/automation, module giữ/bỏ, tên app, port, DB, remote GitLab từng repo), tách các repo git riêng, và kiểm rằng workflow/ không bị sửa. Không dùng cho dự án đã chạy.
---

# Bootstrap dự án mới từ template

Chạy khi thư mục dự án vừa được copy từ `Z:\APP-FACTORY\SOURCE-CODE\PROJECT-TEMPLATE\` và `CLAUDE.md` còn các mục `<!-- ĐIỀN KHI CLONE -->`.

## Nguyên tắc

- Hỏi **một câu mỗi lần**, bằng text thường, tiếng Việt. Đọc tài liệu ý tưởng của User (vd `CONCEPT.md`) trước — **tự điền thứ đã rõ, kèm trích dẫn**.
- **Không sửa `workflow/`** — nó thuộc template. Chỉ điền ngày đồng bộ ở `workflow/README.md`.
- 🔴 **Không `git init` / tạo remote / commit / push thay User** — đưa khối lệnh, chờ User xác nhận.

## Bước 1 — Hiểu dự án

Đọc tài liệu ý tưởng. Điền mục **Dự án** của `CLAUDE.md`: sản phẩm là gì, người dùng là ai, kênh phát hành, team gồm ai. 🔴 **Chỉ thứ đứng yên qua mọi đợt** — tiến trình nằm ở `project-docs/overview-project.md`.

## Bước 2 — Chọn module

Template có `api-service/` (.NET 10) và `admin-console/` (.NET 10 MVC, theme Cuba). Hỏi User giữ module nào, **đổi tên** thư mục nếu cần. Module bỏ ⇒ xoá thư mục (sau khi User đồng ý). Module cần mà template chưa có (mobile, service thứ hai…) ⇒ ghi vào `CLAUDE.md` là *"chưa có"*, **không** dựng sẵn.

Với mỗi module giữ lại: đổi `ServiceName` trong `appsettings.json` và cấu hình local `appsettings.Development.json` (**commit cùng repo** để cả team pull về là chạy): connection string trỏ DB dev của dự án, `Jwt:Secret` (≥ 32 byte) / `Auth:BootstrapToken` sinh mới — không dùng lại giá trị của template. Hỏi User key dịch vụ ngoài (OpenRouter…) dùng chung của team hay key riêng của dự án.

Feature mẫu (`notes` ở api-service; Dashboard/Users ở admin-console) là **EXAMPLE**: hỏi User giữ làm khung tham khảo cho đợt đầu hay xoá ngay (cách xoá ghi ở README module).

## Bước 3 — Điền các mục cố định của `CLAUDE.md`

`workflow/` trỏ tới đúng các mục này — **giữ nguyên tên mục**:

| Mục | Hỏi / điền |
|---|---|
| **Ràng buộc đặc thù** | Điều gì sai thì hậu quả thật? (DB dùng chung, client đã phát hành, nội dung nhạy cảm, chi phí AI…). Giữ hai dòng chung của AppFactory (SQL viết tay, thứ tự SQL ↔ image). 🔴 **Chỉ ở mức nguyên tắc** — quyết định tính năng ghi ở tài liệu đợt |
| **Risk gate riêng** | Vùng nào luôn phải dừng hỏi User, ngoài risk gate chung? |
| **Cấu trúc repo** | Remote GitLab từng repo · README · lệnh chạy · port · repo nào có nhánh chuẩn khác `main` |
| **Môi trường** | DB dev local tên gì · agent được ghi DB dev không · dịch vụ ngoài nào tốn tiền thật |
| **Bàn giao** | Ai nhận gói bàn giao (merge/deploy) · ai test manual |
| **Tài liệu hiện trạng** | Bảng `present/` — ban đầu gần rỗng |

## Bước 4 — `project-docs/automation/`

- `README.md`: DB dev local, role chạy DDL, domain môi trường dev — giá trị local/dev ghi thẳng (commit chung cho team); chỉ secret production không ghi.
- `test-accounts.md`: giữ cách dựng mặc định của template, sửa nếu dự án đổi luồng đăng nhập. **Xoá dòng tài khoản của template** (DB template), dựng admin mới trên DB dev của dự án ở Bước 5 rồi ghi username + mật khẩu vào bảng.

## Bước 4b — Policy worker của người đang chạy Leader

`workflow/agent-platform/worker-policy.local.md` (gitignore — **mỗi người một bản**, không thuộc dự án). Đã có (copy từ máy cũ) ⇒ kiểm lại với User. Chưa có ⇒ dựng từ `worker-policy.sample.md`, hỏi từng câu: User có những platform/CLI nào · vai nào chạy trên đâu, model nào · có chấp nhận worker chung pool quota với Leader không · ngân sách/overage. 🔴 Mẫu là cấu hình của **một người** — không chép nguyên. Đối chiếu luật chung ở `workflow/agent-platform/README.md` và luật model riêng của dự án (`CLAUDE.md`). Platform chưa có profile ⇒ ghi chú để probe ở kickoff đợt đầu.

Thành viên khác mở dự án lần đầu cũng phải có bản của mình — hook `SessionStart` nhắc, hook `PreToolUse` chặn gọi worker tới khi có.

## Bước 5 — DB dev & chạy thử

- Tạo DB dev trống (theo `CLAUDE.md` mục *Môi trường*), apply `api-service/db/init.sql`.
- Test + build từng module giữ lại theo mục *Chạy* trong README của module (`dotnet test` — integration test của api-service cần Docker) — phải sạch.
- Chạy `api-service` + `admin-console`, đăng nhập admin, mở trang mẫu ⇒ xác nhận khung chạy.

## Bước 6 — Tách repo git

Template là **monorepo**; dự án thật thì mỗi module + `project-docs` + root là **một repo riêng**. Trước khối lệnh, Leader tự làm và kiểm ba việc:

1. 🔴 **Không còn `.git/` của template** — ở root lẫn trong module (`Get-ChildItem -Force -Recurse -Directory -Filter .git -Depth 1`, bỏ qua `node_modules`). Còn ⇒ báo User xoá; `git init` trên đó sẽ dùng lại lịch sử template mà không báo gì.
2. **Bỏ comment khối "Repo con" ở root `.gitignore`**, khớp đúng danh sách repo đã chốt ở Bước 2 (module bỏ ⇒ xoá dòng, module đổi tên ⇒ sửa dòng). `.gitignore` của module và `project-docs` giữ nguyên — đúng cho cả hai chế độ.
3. **Copy `.gitattributes` ở root vào root mỗi repo con** (module + `project-docs`) — attribute của wrapper không áp vào repo con; thiếu thì file `.sh` bị commit CRLF và lỗi khi chạy bằng bash.

User tạo project trên GitLab trước, rồi chạy (Leader điền remote thật vào khối lệnh):

```bash
git init -b main && git add -A && git commit -m "chore: bootstrap from AppFactory PROJECT-TEMPLATE" && git remote add origin <remote-wrapper>
git -C project-docs init -b main && git -C project-docs add -A && git -C project-docs commit -m "docs: bootstrap" && git -C project-docs remote add origin <remote-docs>
git -C api-service init -b main && git -C api-service add -A && git -C api-service commit -m "chore: bootstrap" && git -C api-service remote add origin <remote-api>
git -C admin-console init -b main && git -C admin-console add -A && git -C admin-console commit -m "chore: bootstrap" && git -C admin-console remote add origin <remote-admin>
# nhánh dev cho mỗi repo module (project-docs và wrapper chỉ có main):
git -C api-service branch dev && git -C admin-console branch dev
```

Khối "Repo con" đã bỏ comment ⇒ commit của wrapper không nuốt code module. Verify: `git ls-files | grep -E '^(api-service|admin-console|project-docs)/'` ở root phải **rỗng**.

CI: mỗi module có `.gitlab-ci.yml.sample` (pipeline chuẩn AppFactory) — commit nguyên dạng `.sample`, **không** đổi tên. Báo User một dòng: dev phụ trách đổi tên thành `.gitlab-ci.yml` khi dựng pipeline (hạ tầng ngoài phạm vi team AI).

## Bước 7 — Kiểm & bàn giao

- `grep -rn "ĐIỀN KHI CLONE" CLAUDE.md project-docs workflow/README.md` ⇒ không còn mục nào chưa điền (hoặc đã ghi rõ "chưa áp dụng").
- Hook chạy được: `echo '{"source":"startup","hook_event_name":"SessionStart"}' | node .claude/hooks/reload-after-compact.js` trả JSON hợp lệ.
- 🔴 **Mở lại phiên Claude Code ở thư mục dự án trước khi gọi worker nào** — `.claude/settings.json` sinh ra giữa phiên thì hook **không được nạp**.
- `workflow/agent-platform/worker-policy.local.md` đã có (Bước 4b).
- Báo User *"bootstrap xong"* kèm danh sách mục đã ghi "chưa áp dụng". Skill này không chạy lại cho dự án này nữa.

Xong ⇒ đợt đầu tiên bắt đầu bằng `/brainstorm`.
