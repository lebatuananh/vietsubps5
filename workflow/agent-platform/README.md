# Agent Platform Profiles

> **Mục đích**: giúp Team Leader chọn tổ hợp `platform + model + effort + permission` cho một work package, và hiểu đúng quota trước khi phân tải.
>
> Thư mục có hai loại nội dung:
>
> | | Là gì | Thuộc về |
> |---|---|---|
> | Profile từng CLI (`claude-code.md`, `codex.md`…) + file này | **Kiến thức chung** về CLI: cờ, quyền, quota, bẫy. Học được gì mới ⇒ sửa ở đây **và đẩy về template** | Template |
> | `worker-policy.local.md` | **Lựa chọn của từng người**: mình có platform nào, vai nào chạy trên đâu, model nào, ngân sách. Gitignore | Cá nhân |
>
> Profile ghi version/probe tại thời điểm viết — **probe lại** trước lần dùng đầu trên máy của mình.

---

## 🔴 Chọn platform / model cho worker — policy cá nhân, bắt buộc

Team dùng nền tảng khác nhau, nên template **không** quy định vai nào chạy trên platform nào. Chỉ cố định:

- **Leader chạy trên Claude Code** — giả định hiện tại của template (skill + hook nằm ở `.claude/`). Leader trên platform khác chưa được hỗ trợ: phải tự ánh xạ skill/hook trước.
- **Worker chạy trên bất kỳ platform nào** có CLI headless — kể cả Claude Code.
- 🔴 **Mỗi người chạy Leader phải có `worker-policy.local.md`** cùng thư mục này, tạo từ [`worker-policy.sample.md`](./worker-policy.sample.md) (mẫu là cấu hình của một người, không phải chuẩn). Thiếu file ⇒ hook `check-worker-session.js` chặn mọi lệnh gọi worker; Leader dừng, cùng User lập file trước. 🔴 Đồng bộ template ⇒ **giữ nguyên** file này, không xoá cả thư mục.
- Leader **đọc policy trước mỗi lần chọn worker** — không chọn theo trí nhớ.

**Luật chung — policy cá nhân không được phá:**

| Luật | Vì sao |
|---|---|
| Reviewer là **session độc lập** với Dev — khác platform khi có thể, buộc cùng platform thì **khác model** | Cùng session/model thì cùng mù trước một lớp lỗi |
| End User dùng model khác Dev | Đánh giá văn độc lập với người viết code |
| Luật model riêng của dự án (`CLAUDE.md`, vd cấm model Claude cho mọi worker) **đè lên** policy cá nhân | Ràng buộc dự án áp cho mọi người trong team |
| Worker cùng pool quota với Leader (vd worker Claude Code cùng account) ⇒ policy phải **ghi rõ chấp nhận chia pool** và giới hạn dùng | Worker rút cạn pool ⇒ Leader chết giữa đợt |
| Platform cấp đủ **quyền theo vai** ([delegating-workers §4](../practices/delegating-workers.md)) — thiếu thì đổi platform, không hạ chuẩn | Worker thiếu quyền tự xoay xở bằng cách tệ hơn |
| Platform **chưa có profile** ⇒ probe (đọc · ghi · shell · browser nếu cần) và viết profile trước khi giao việc thật | Mọi lỗi gọi sai đều exit 0 |
| Hết lựa chọn hợp lệ trong policy ⇒ báo vướng mắc, không tự phá luật | |

🔴 **CLI đã đổi version kể từ lần probe cuối ⇒ probe lại trước khi giao việc thật** ([delegating-workers §2](../practices/delegating-workers.md)). Kết luận về quyền/cờ của bản cũ có thể đã lật ngược.

⚠️ **Codex và Cursor không nằm trong PATH của Git Bash** — `which` không ra không có nghĩa là chưa cài. Gọi qua PowerShell với đường dẫn đầy đủ. Đừng đoán tên lệnh từ tên platform (Antigravity là `agy`, không phải `antigravity`); kiểm bằng `Get-Command <tên>`.

---

## 🔴 Bẫy exit-code-0 — mọi CLI đã có profile đều dính

Cùng triệu chứng: **thoát sạch, không làm gì, không báo lỗi rõ.**

| Bẫy | Ở đâu |
|---|---|
| `--trust` chỉ cho ghi file, **không** cho chạy lệnh; Dev/Reviewer cần `-f` | [cursor.md](./cursor.md) |
| `-m` không tồn tại, phải viết đủ `--model` | [cursor.md](./cursor.md) |
| Brief nhiều dòng phải qua **stdin** (kèm `-` cuối), không qua argument | [codex.md](./codex.md) |
| Headless auto-deny tool cần approval ⇒ cần allow-rule `command(...)` · `write_file(*)` · `read_file(*)` · `mcp(*)` | [google-antigravity.md](./google-antigravity.md) |
| `--print-timeout` mặc định 5 phút cắt ngang lượt dài, exit 0 | [google-antigravity.md](./google-antigravity.md) |
| Pool model bên thứ ba cạn ⇒ `usage limit`, exit 0, không artifact | [cursor.md](./cursor.md) |
| Brief có nháy kép / dòng bắt đầu bằng `-` qua PowerShell 5.1 ⇒ tách argument | mọi CLI — [delegating-workers §2](../practices/delegating-workers.md) |

---

## Lưu ý khi chọn

🔴 **Phase đụng UI ⇒ brief nói rõ dùng Playwright MCP (server `playwright`)**, kèm đường dẫn lưu ảnh evidence.

⚠️ Bảng phân vai trong policy là thứ tự khởi đầu, không thay cho probe năng lực/quota. Cùng model trên harness khác có thể dùng pool khác — pool mới quyết định capacity, không phải tên model.

⚠️ **Nội dung nhạy cảm** (tán tỉnh, gợi cảm, bạo lực nhẹ…): model có thể từ chối viết prompt/kịch bản hay chấm ảnh loại này. Probe **đúng loại nội dung** trên từng platform ở kickoff đợt; ghi kết quả vào profile.

---

## Browser: Playwright MCP — dùng chung mọi platform

> Mục này là **cài đặt & cấp quyền**. Cách thực hiện một lượt blackbox UI: [guidelines/blackbox-ui-playwright.md](../guidelines/blackbox-ui-playwright.md).

Smoke/verify UI dùng **Playwright MCP** (`@playwright/mcp`, cài global: `npm i -g @playwright/mcp` ⇒ lệnh `playwright-mcp`). **Không** `npm install playwright` vào workspace. Tool: navigate · click · fill · snapshot DOM sau render · screenshot · `browser_network_requests` · `browser_run_code_unsafe` (vd `page.route` giả lập lỗi).

| Platform | Đăng ký | Cấp quyền headless |
|---|---|---|
| Claude Code | `.mcp.json` ở root project (commit được) | `--allowedTools "mcp__playwright__*"`; phiên interactive hỏi duyệt lần đầu |
| Cursor | `.cursor/mcp.json` ở root project | `--approve-mcps`. Global có thể có server trùng tên của dự án khác ⇒ brief gọi đích danh `playwright` |
| Codex | global — `codex mcp add playwright -- playwright-mcp --headless` | `default_tools_approval_mode = "approve"` trong `[mcp_servers.playwright]` của `~/.codex/config.toml`. Thiếu ⇒ `MCP tool call requires approval, but approval policy is never`. Global có thể có server trùng tên của dự án khác ⇒ brief gọi đích danh `playwright` |
| Antigravity | global — `agy mcp add playwright -- playwright-mcp --headless` | allow-rule `mcp(*)` trong `~/.gemini/antigravity-cli/settings.json` (agy **giữ nguyên** rule này sau lượt chạy, khác các entry nó không nhận ra). Thiếu ⇒ `a tool required the "mcp" permission that headless mode cannot prompt for` |

**Công thức probe browser** (bằng giá trị không đoán được):
1. Sinh token ngẫu nhiên, viết `index.html` có nút mà khi click mới hiện `CLICKED-<token>`; phục vụ bằng `python -m http.server <port> --bind 127.0.0.1` chạy nền.
2. Prompt **một dòng, không nháy kép**: dùng MCP server `playwright`: mở `http://127.0.0.1:<port>/`, click nút, báo nguyên văn text của `#o`, chụp ảnh `probe-out/<platform>.png`. Workspace root = root project.
3. Đạt khi: text đúng token · ảnh có ở `<root>/probe-out/`. Xong xoá `probe-out/`, tắt server.

🔴 **Không đặt `--output-dir`** cho Playwright MCP. Để mặc định: file tự đặt tên rơi vào `.playwright-mcp/` tính từ **workspace root** (gitignore); ảnh evidence thì **brief bắt đặt tên** theo đường dẫn tương đối, vd `project-docs/<đợt>/playtest/epic1-phase1/dev/x.png`.

🔴 **Workspace root của worker = root project** (repo wrapper), không phải thư mục module — gọi với cwd là repo con thì log/ảnh rơi vào repo đó.

🔴 **Playwright MCP in nguyên `Page URL` sau mỗi thao tác** ⇒ thứ nhạy cảm đưa qua URL (hash/query chứa token) lọt vào transcript. Xoá khỏi URL ngay khi trang đọc xong (`history.replaceState` ở `DOMContentLoaded`); không truyền credential qua URL nếu có đường khác.

---

## Chọn nhanh — từng profile

| Platform | Điểm mạnh đã có evidence | Profile |
|---|---|---|
| Claude Code | Team Leader (giả định của template). Làm worker được — cùng account thì chung pool với Leader | [claude-code.md](./claude-code.md) |
| Codex CLI | BA/SA; phản biện có `file:line` | [codex.md](./codex.md) |
| Cursor CLI | Dev backend/UI, có browser qua MCP; Reviewer với model khác Dev | [cursor.md](./cursor.md) |
| Antigravity | Reviewer — pool độc lập, bắt lỗi "xanh hết nhưng sai ngầm"; Dev/BA-SA dự phòng | [google-antigravity.md](./google-antigravity.md) |

Chất lượng thực tế thuộc về cả tổ hợp: `platform + CLI version + model + effort + toolset + permission + task + context`. Đây là **evidence đã có**, không phải bảng xếp hạng hay phân vai bắt buộc. Dùng platform chưa có ở đây ⇒ thêm profile mới theo mục cuối file này.

---

## Cấu trúc quota phải kiểm

Không so "một request" giữa các nền tảng. Trước fan-out hoặc việc dài, biết tối thiểu:

| Thuộc tính | Câu hỏi |
|---|---|
| Billing mode | Subscription, included credits hay API pay-as-you-go? |
| Pool | Surface/model nào dùng chung một quỹ? |
| Reset | Rolling 5 giờ, tuần, tháng? |
| Remaining | Còn bao nhiêu theo dashboard/CLI? |
| Overage | Hết thì dừng, fallback, hay tự phát sinh tiền? Ai đã cho phép? |

### Chính sách phân tải mặc định

1. Phân vai theo `worker-policy.local.md`. Bất kể policy: không để worker rút cạn pool của Leader.
2. Model vừa/rẻ cho implementation đã có contract rõ, tìm kiếm có phạm vi, smoke lặp.
3. Phân tán work package độc lập qua **pool thực sự độc lập** — hai model cùng pool không tạo thêm capacity.
4. Không bật overage/auto-reload nếu User chưa ủy quyền ngân sách.
5. Quota thấp ⇒ ưu tiên hoàn tất vertical slice đang dở và tạo handoff sạch trước khi đổi platform.
6. Không đo được ⇒ ghi `unknown`, không bịa.

### Fan-out

Chỉ fan-out khi rút ngắn **bottleneck thật** — thường là discovery, phản biện, testing; thời gian gõ code thường ngắn hơn nhiều. Mặc định **một Dev session chính** mỗi đợt. Code chỉ chạy song song khi đã chứng minh ranh giới file/data không giao nhau.

---

## Cách đọc & cập nhật profile

Mỗi profile tách ba loại: **fact hiện tại** (docs chính thức / CLI đang cài, có ngày) · **kinh nghiệm thực nghiệm** (có nguồn) · **giả thuyết routing** (nên thử trước, chưa phải kết luận).

Runtime và tài liệu mâu thuẫn ⇒ ưu tiên: (1) model/quota hiển thị lúc chạy → (2) `--help` và danh sách model của CLI đang cài → (3) docs chính thức mới nhất → (4) profile này → (5) kinh nghiệm cũ.

Chỉ thêm kinh nghiệm khi nó **đổi được quyết định routing hoặc mức giám sát**, tối đa vài dòng:

```text
- [YYYY-MM-DD] <vai/task>: <takeaway cực ngắn>. Nguồn: <dự án> <đợt>
```

Nhận xét theo **vai và loại task**, không kết luận chung "model X tốt/kém". Không biến profile thành nhật ký.
