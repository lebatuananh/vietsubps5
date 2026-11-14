# workflow/ — Quy trình chuẩn (thuộc PROJECT-TEMPLATE của AppFactory)

> **Nguồn**: `Z:\APP-FACTORY\SOURCE-CODE\PROJECT-TEMPLATE\workflow\`
> **Đồng bộ lần cuối từ template**: 2026-11-14 (từ `D:\Project\appfactory-project-template`)

Thư mục này mô tả **cách team AI làm việc** — giống nhau ở mọi dự án AppFactory clone từ template. Nó **không** mô tả dự án.

| Muốn biết | Đọc ở đâu |
|---|---|
| Cách làm việc (quy trình, vai, công đoạn, kỹ năng) | `workflow/` — thư mục này |
| Dự án này là gì, ràng buộc gì, môi trường nào | `CLAUDE.md` ở root |
| Hiện trạng hệ thống, các đợt update | `project-docs/` (repo riêng) |
| Quy ước code của từng module | `README.md` của module đó (repo riêng) |

---

## 🔴 Rule quyết định thư mục này có giá trị hay không

> **Không một chữ nào trong `workflow/` được đặc thù cho một dự án.**

Chỗ nào quy trình cần giá trị riêng của dự án (DB dev tên gì, vùng nào luôn phải hỏi User, ai nhận bàn giao…) thì **trỏ sang mục cố định của `CLAUDE.md`** hoặc `project-docs/automation/`, không ghi thẳng giá trị.

Các mục cố định mà `workflow/` trỏ tới (tên mục trong `CLAUDE.md` phải giữ đúng):

| Mục trong `CLAUDE.md` | Chứa gì |
|---|---|
| **Ràng buộc đặc thù** | Những điều sai thì hậu quả thật — inline ngắn, không chỉ link |
| **Risk gate riêng** | Vùng luôn phải dừng hỏi User, cộng thêm vào risk gate chung ở [team-leader §5](./roles/team-leader.md) |
| **Cấu trúc repo** | Repo nào, remote, README ở đâu, chạy lệnh gì, port nào, nhánh chuẩn |
| **Môi trường** | DB dev/prod, file cấu hình local, dịch vụ ngoài dùng chung, thứ gì tốn tiền thật |
| **Bàn giao** | Ai nhận gói bàn giao, ai merge/deploy, ai test manual |
| **Tài liệu hiện trạng** | Bảng "đụng X thì đọc `project-docs/present/Y`" |

Vi phạm rule này thì lần đồng bộ sau phải lọc tay từng dòng — đúng thứ template sinh ra để tránh.

---

## Đồng bộ với template — hai chiều

| Chiều | Khi nào | Làm gì |
|---|---|---|
| **Template → dự án** | Khi template có bản mới đáng lấy | Ghi đè cả `workflow/` — **giữ** `agent-platform/worker-policy.local.md` (của từng người, gitignore) · so diff `.claude/skills` + `.claude/hooks` · cập nhật ngày ở đầu file này |
| **Dự án → template** — lỗi của chính template | **Ngay khi sửa xong ở dự án** | Bug/bẫy trong code module mẫu, hook, skill, hoặc chỗ `workflow/` sai rõ ràng ⇒ sửa bản ở template luôn, báo User một dòng. Không chờ đóng đợt: lỗi rõ ràng không cần chắt lọc, còn để đến cuối dễ quên — và dự án khác có thể clone bất cứ lúc nào |
| **Dự án → template** — bài học | Khi đóng đợt ([06-close](./leader-stages/06-close.md)) | Lesson thuộc quy trình chung ⇒ sửa ở `workflow/` của dự án **và** đẩy về template |

Sửa `workflow/` ngay trong dự án là được — nhưng **mọi sửa đổi phải đẩy ngược về template** (lỗi: ngay; bài học: trong lượt đóng đợt). Sửa mà không đẩy về ⇒ bản dự án lệch dần, lần sync sau ghi đè mất bài học.

Phân biệt *lỗi của template* với *code đã đổi theo dự án*: chỉ đẩy về thứ **vẫn sai nếu clone template sạch ra dự án khác**. Code module đã sửa theo nghiệp vụ dự án thì không đẩy.

> Tiền lệ: hai dự án từng tiến hoá bộ quy trình này song song mà không có template — sau hai tuần **cả 18 file chung đều lệch nhau**, mỗi bên giữ bài học bên kia không có.

---

## Bản đồ

### Lõi

- [protocol.md](./protocol.md) — **đọc trước tiên**: triết lý, các vai, vòng đời một Phase, Direct Command
- [roles/](./roles/) — sổ tay theo vai. 🔴 Nhận vai nào đọc trọn sổ tay vai đó:
  [team-leader](./roles/team-leader.md) · [ba-sa](./roles/ba-sa.md) · [dev](./roles/dev.md) · [reviewer](./roles/reviewer.md) · [end-user](./roles/end-user.md)
- [leader-stages/](./leader-stages/) — 6 công đoạn của Leader, mỗi công đoạn một file:
  [01-brainstorm](./leader-stages/01-brainstorm.md) · [02-discovery](./leader-stages/02-discovery.md) · [03-write-docs](./leader-stages/03-write-docs.md) · [04-implement](./leader-stages/04-implement.md) · [05-handover](./leader-stages/05-handover.md) · [06-close](./leader-stages/06-close.md)

### Kỹ năng dùng xuyên suốt

- [practices/delegating-workers.md](./practices/delegating-workers.md) — giao việc cho worker qua CLI
- [practices/workflow-log.md](./practices/workflow-log.md) — ghi nhật ký đợt
- [practices/lessons.md](./practices/lessons.md) — ghi lesson, chuyển lesson thành rule

### Guideline kỹ thuật (không phụ thuộc stack)

- [guidelines/feature-development.md](./guidelines/feature-development.md) — mindset, KISS, thiết kế tương xứng
- [guidelines/tdd.md](./guidelines/tdd.md) — unit test logic thuần
- [guidelines/blackbox-testing.md](./guidelines/blackbox-testing.md) — test qua API/UI thật: chiến lược, 3 lớp verify
- [guidelines/blackbox-ui-playwright.md](./guidelines/blackbox-ui-playwright.md) — blackbox UI bằng Playwright MCP: dựng trang, danh tính giả lập, mock, report
- [guidelines/blackbox-android-adb.md](./guidelines/blackbox-android-adb.md) — blackbox app Android qua `adb`: emulator/thiết bị, cài APK, dump bounds + tap, seed state, bắt traffic, bẫy
- [guidelines/automation.md](./guidelines/automation.md) — server local, DB, domain, tài khoản test
- [guidelines/safe-refactoring.md](./guidelines/safe-refactoring.md) — thứ tự khi thêm/xoá
- [guidelines/prompt-llm.md](./guidelines/prompt-llm.md) — cấu trúc prompt template
- [guidelines/git.md](./guidelines/git.md) — nhiều repo, nhánh, ai chạy lệnh git

### Platform & template

- [agent-platform/](./agent-platform/README.md) — fact của máy: CLI, cờ, quota, bẫy từng platform
- [templates/](./templates/) — khuôn draft · overview · epic · phase · workflow log · lessons · agent-sessions

> Quy ước code **theo stack** (.NET/EF Core/Wolverine, ASP.NET Core MVC/theme Cuba…) **không** nằm ở đây — nằm trong `README.md` của module tương ứng, vì nó mô tả code, đi cùng code khi clone và lệch dần theo code của từng dự án.
