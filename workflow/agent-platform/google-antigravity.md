# Google Antigravity — Platform Profile

> **Probe gần nhất**: 2026-10-14 `agy 1.3.5` + `gemini-3.8-flash-high` — shell (`git rev-parse` khớp) · đọc token ngẫu nhiên · ghi file **PASS**. Browser qua Playwright MCP **PASS** lần cuối ở `1.2.12` (2026-09-29) — chưa probe lại trên `1.3.x`.
> **Bắt buộc ở lần dùng đầu với mỗi dự án**: thêm root dự án vào `trustedWorkspaces` (sửa bằng PowerShell/.NET, ghi **không BOM** — Git Bash nuốt dấu `\` trong đường dẫn Windows). Allow-rule cũ giữ nguyên qua các lần đổi version (đã thấy từ `1.2.x` lên `1.3.x`).
> **Stream-json**: mỗi dòng có khoá `event` (`init` · `step_update` · `result`), không phải `type`; id ở `init.conversation_id`; kết thúc = `result.status` (`SUCCESS` …).
> Lệnh CLI là **`agy`** (`%LOCALAPPDATA%\agy\bin\agy.exe`, trong PATH PowerShell) — không phải `antigravity`.
> **Trạng thái**: vai Reviewer đã có evidence bắt đúng lỗi mà `tsc` + unit test + grep của Leader đều không thấy.

## Model

`agy models` là source-of-truth. Account có: Gemini 3.8/3.7/3.6 Flash (`-low/medium/high`) · Gemini 3.1 Pro (`-low/high`) · `claude-sonnet-4-6` · `claude-opus-4-6-thinking` · `gpt-oss-120b-medium`.

⚠️ Claude/GPT ở đây chạy trên **pool của Antigravity**, khác hẳn pool Claude Code hay Cursor — nguồn capacity độc lập thật.

## Chọn nhanh

| Loại việc | Model | Ghi chú |
|---|---|---|
| **Reviewer** | `gemini-3.8-flash-high` | Vai khuyến nghị chính — khác platform + khác pool với Dev |
| Reviewer khi finding hời hợt / task quá phức tạp | `gemini-3.1-pro-high` · `claude-opus-4-6-thinking` | Cùng pool Antigravity; ghi lý do nâng |
| Dev dự phòng | `gemini-3.8-flash-medium/high` | Ưu tiên Cursor nhà trồng trước |
| Verify UI | `gemini-3.8-flash-high` + Playwright MCP | Cần allow-rule `mcp(*)` |

## 🔴 Quyền headless — allow-rule, không dùng cờ bỏ qua quyền

Từ `1.2.8`: headless **chạy được lệnh shell bằng allow-rule**, không cần `--dangerously-skip-permissions` (cờ này còn bị **auto-mode classifier của Claude Code chặn** — không lách).

Allow-rule ở `~/.gemini/antigravity-cli/settings.json` → `permissions.allow`, theo **tên tool + đích**:

| Cần | Rule |
|---|---|
| Chạy lệnh | `command(*)` · `command(git)` · `command(npm)` · `command(npx)` · `command(node)` … |
| 🔴 **Ghi file** (tách riêng quyền lệnh) | `write_file(*)` |
| Đọc file | `read_file(*)` |
| MCP (Playwright) | `mcp(*)` |

Cộng repo nằm trong `trustedWorkspaces`.

> Ca thật: Reviewer chạy 17 phút, **chạy lệnh được** nhưng **`write_file` bị deny** ⇒ không ghi được report (`denied_actions: [write_file]`). Thêm `write_file(*)` + `read_file(*)` ⇒ probe ghi file token ngẫu nhiên khớp.

⚠️ **`agy` tự ghi đè `settings.json` sau mỗi lượt và xoá entry nó không nhận ra** (đã xoá `edit_file(*)`, `command` trần, `defaultMode: "acceptEdits"`) — đừng coi các entry đó là đang có hiệu lực; kiểm lại file sau khi đổi version.

## Harness và cách gọi

- `agy -p` headless; `--output-format stream-json`; `--model`, `--effort`, `--json-schema`.
- `--add-dir <root project>` khoanh workspace. **Không có cờ read-only** — giới hạn vai read-only chỉ bằng brief.
- `--conversation <id>` resume (id ở dòng `init` của stream-json).
- 🔴 **`--print-timeout` mặc định 5 phút** — hết giờ in `print timeout after 5m0s with turn in progress`, **exit 0**, không report. Lượt review/dev ⇒ **`--print-timeout 0s`** (không giới hạn) hoặc giá trị đủ dài.

```powershell
& agy -p 'Read and follow <đợt>/_workers/briefs/reviewer-<việc>-brief.txt' `
  --model gemini-3.8-flash-high --add-dir '<root project>' `
  --print-timeout 0s --output-format stream-json *>&1 |
  Out-File '<đợt>\_workers\streams\reviewer-<việc>-stream.jsonl' -Encoding utf8
```

### 🔴 Bẫy parser brief

`agy` parse `-p` kém chặt hơn Cursor. **Prompt một dòng trỏ tới file brief** — không truyền brief nhiều dòng qua argument.

| Ký tự trong prompt | Hậu quả |
|---|---|
| Dấu nháy kép `"…"` | Tách thành argument ⇒ `unexpected argument`, bỏ cả brief |
| Dòng bắt đầu bằng `-` | Hiểu thành cờ |
| Xuống dòng | `unexpected argument "…\n3. …"`, exit 2 |

### ⚠️ Treo khi tự bật server bằng lệnh chặn

Reviewer tự khởi động server bằng `run_command` **chặn** ⇒ đứng vô hạn chờ lệnh đó kết thúc (CPU ~7s sau nhiều phút, stream đứng). Cách tránh: Leader bật server nền trước, brief *"dùng server có sẵn ở port X, không bật/tắt"*; hoặc brief bắt bật server bằng process nền tách rời.

### Đường dự phòng khi MCP không dùng được: Playwright qua shell

Toolset mặc định của agy **không có browser** (`read_url_content` chỉ fetch tĩnh — không chạy SPA, không click, không screenshot). Đường chính là Playwright MCP (trên). Nếu MCP hỏng, agy **tự viết script Playwright, tự chạy qua `run_command`, tự chụp ảnh** được — probe 2026-09-12: 49 giây, in đúng token, để lại ảnh trên đĩa.

Điều kiện: thư mục làm việc có `npm install playwright` (~2s) + `npx playwright install chromium` (~27s, 115 MiB, chỉ lần đầu; cache dùng chung ở `%LOCALAPPDATA%\ms-playwright\`). ⚠️ Bẫy version: cache chromium của MCP có thể khác build mà `playwright` npm đòi ⇒ lỗi rõ `Executable doesn't exist at …chromium_headless_shell-<build>`, chạy lại `npx playwright install chromium`.

Chỉ dùng khi MCP không khả dụng — cài `playwright` vào module làm bẩn `package.json`; cài vào thư mục scratch thay vì module.

## Quota

- Pro/Ultra: baseline reset mỗi **5 giờ** tới weekly limit. Con số tuyệt đối không được cam kết.
- Gemini Flash + Pro **chung một Gemini pool** (rút theo tỷ lệ giá); Claude/GPT-OSS ở **pool riêng**.
- Overage bằng AI credits: setting `Never`/`Always` — mặc định **Never** trừ khi User ủy quyền ngân sách.
- Không giả định slash command `/usage` là CLI argument; không đo được ⇒ `quota: unknown`.

## Kinh nghiệm

- [2026-09-13] Reviewer `gemini-3.8-flash-high`: vòng 1 bắt 2 blocking (helper đúng chuẩn nhưng anti-pattern bị chép tay sang hai file khác) mà `tsc`, unit test, grep của Leader đều không thấy; vòng 2 bắt filter sót làm validator luôn báo lỗi, kèm chẩn đoán đúng vì sao test không bắt (fixture mock dùng shape của bảng đã xoá). Nguồn: dự án dùng template.
- [2026-09-24] Phần đối chiếu spec↔code **bịa tên** action/state không có trong code, dù smoke và ảnh đúng ⇒ brief bắt `file:line` cho mọi claim ĐẠT, Leader grep kiểm vài tên. Nguồn: dự án dùng template.
- [2026-10-06] **Hết quota giữa lượt** (429 `RESOURCE_EXHAUSTED`, exit 3) bỏ lại server nó đã bật (2 cổng) và report dừng ở `IN PROGRESS`. Phép quan sát phải bắt dòng lỗi quota trong stream; trước khi giao platform khác, dừng đúng cây process + server của lượt đó và đổi report về trạng thái mới. Nguồn: dự án dùng template 261004.
- [2026-09-25] Reviewer hai Phase frontend liền: blackbox độc lập đủ ca lỗi/hold/nofill, probe contract SDK 15/15. Nguồn: thực nghiệm 2026-09-25.
- [2026-10-14] **Đặt tên session** (`1.3.4`, probe): không có cờ tên; `--project <tên>` không có tác dụng khi project chưa tồn tại (conversation vẫn ở `default-cli-project`). Dòng đầu brief ghi `<Dự án>-<đợt>-Reviewer` ⇒ hiện ở `preview` của `~/.gemini/antigravity-cli/conversation_summaries.db`; `title` do agy tự sinh sau. Bảng đó có `workspace_uris` để đối chiếu dự án. Không dùng `--continue`.

## Nguồn chính thức

- [Antigravity plans and quota](https://antigravity.google/docs/plans)
- [Antigravity models](https://antigravity.google/docs/models/)
- [Antigravity CLI headless](https://antigravity.google/docs/cli/headless/)
