# Codex CLI — Platform Profile

> **Rà soát**: 2026-10-09 · **Local runtime**: `codex-cli 0.159.2` — probe 2026-10-09: đọc file · ghi file mới · chạy shell **PASS** (token ngẫu nhiên + SHA-256 file, `-s workspace-write --skip-git-repo-check`, brief qua stdin, `-m gpt-6.1-sol`)
> **Trạng thái**: vai BA/SA đã chạy nhiều đợt ở dự án dùng template.

## Chọn nhanh

| Loại việc | Điểm khởi đầu | Lý do |
|---|---|---|
| **BA/SA** (evidence tốt nhất) | **`gpt-6.1-sol`** (mới từ 2026-10-01, thay `gpt-6-sol`), reasoning `high` | Truyền `-m gpt-6.1-sol` tường minh — `config.toml` local có thể đặt model khác |
| Codex hết quota | **Đổi nền tảng, không hạ model** | Mọi model Codex (app/CLI/cloud) chung allowance — hạ model không thêm capacity |

Dùng Codex cho vai nào là lựa chọn trong `worker-policy.local.md` của từng người.

## Quota

- Đăng nhập bằng ChatGPT plan ⇒ Codex local/CLI/cloud dùng **chung allowance**, cửa sổ 5 giờ + weekly. Số message ước tính phụ thuộc context, reasoning, tool use — không phải số cố định.
- Vượt allowance ⇒ ChatGPT credits theo bảng giá model. Fast mode tiêu hao cao hơn.
- Xem quota bằng usage dashboard hoặc `/status` trong CLI. Không suy quota còn lại từ số prompt đã gửi.
- Dùng API key ⇒ pay-as-you-go, tách khỏi allowance ChatGPT. Xác nhận auth mode trước khi gọi.

### 🔴 Reset credit (quota tuần) — CLI thường không có lệnh, dùng `app-server`

- Hết quota tuần (`turn.failed` *"You've hit your usage limit … try again at <ngày>"*) mà tài khoản còn **reset credit**: `codex app-server` (JSON-RPC qua stdio, mỗi dòng một JSON) có `account/rateLimits/read` (trả `rateLimitResetCredits.availableCount`, danh sách credit + hạn dùng) và `account/rateLimitResetCredit/consume { idempotencyKey }` ⇒ `outcome`: `reset` · `nothingToReset` · `noCredit` · `alreadyRedeemed`. Trình tự: `initialize { clientInfo }` ⇒ `initialized` ⇒ gọi hàm.
- Script mẫu: [`tools/codex-rate-limits.js`](./tools/codex-rate-limits.js) — mặc định **chỉ đọc**; cờ `--consume` mới tiêu một credit.
- Luật: (1) Codex báo hết quota ⇒ **đọc `account/rateLimits/read` trước khi chuyển sang nền tảng trả tiền**; (2) tiêu credit **chỉ khi User đồng ý** (credit là tài sản của User); backend tự chọn credit **sắp hết hạn nhất**; (3) đọc lại sau **~20 giây** mới kết luận (backend trễ vài giây); (4) không in credential.

### 🔴 `workspace-write` chỉ ghi được trong thư mục hiện hành của tiến trình

Vùng ghi của sandbox = cwd lúc bật `codex`. Run script bật từ thư mục con (vd `_workers/`) ⇒ worker xong việc nhưng **không ghi được report / log** ở chỗ khác (nội dung chỉ nằm trong tin nhắn cuối). ⇒ Run script của Codex **luôn mở đầu bằng `Set-Location <root repo>`**, không phụ thuộc shell của Leader đang đứng đâu.

## Harness và cách gọi

- `codex exec` headless; `--json` stream JSONL.
- `-s read-only | workspace-write | danger-full-access` — sandbox. BA/SA cần **ghi report** ⇒ `workspace-write` (read-only thì không ghi được file).
- `-C <dir>` workspace root = **root project**; `--add-dir` chỉ khi thật cần.
- `--output-schema <file>` ép contract. `exec resume <id>` tiếp session.

```powershell
Get-Content '<brief>.txt' -Raw -Encoding UTF8 |
  & "$env:LOCALAPPDATA\CodexTools\codex.cmd" exec -m gpt-6.1-sol `
    -s workspace-write -C '<root project>' --json - *>&1 |
  Out-File '<đợt>\_workers\streams\<vai>-<việc>-stream.jsonl' -Encoding utf8
```

### 🔴 Ngoài git repo ⇒ từ chối chạy, exit 0

Thư mục chưa `git init` (hay gặp nhất: dự án **vừa clone từ template**) ⇒ Codex in `Not inside a trusted directory and --skip-git-repo-check was not specified.` rồi thoát — **không artifact, exit 0**. Chưa có repo ⇒ thêm `--skip-git-repo-check` (an toàn cho vai chỉ đọc + ghi report); verify bằng snapshot hash file thay cho `git status`. Thêm chuỗi `Not inside a trusted` vào nhánh `FAILED` của phép quan sát.

### 🔴 Brief nhiều dòng PHẢI qua stdin

Truyền brief làm **argument** thì chỉ **dòng đầu** tới được worker; phần còn lại rơi xuống stdin và Codex đứng chờ. Kết quả: **exit 0, không artifact**. Dấu hiệu trong stream:

```
Reading additional input from stdin...
Please provide the work-package brief, ...
```

Worker hỏi lại brief là nó **làm đúng vai** — nó chỉ nhận được *"bạn là BA/SA"* mà không có task. Thêm chuỗi `"Please provide the work-package"` vào phép quan sát — dấu hiệu *chạy nhưng không nhận được task*, khác hẳn treo.

## Kinh nghiệm

- [2026-09-07] BA/SA reasoning `high`: nhiều lượt phản biện dẫn đúng `file:line`, bắt được contract/edge case Leader bỏ sót. Nguồn: dự án dùng template.
- [2026-09-07] Browser không khả dụng trong session thử — giới hạn của toolset lúc đó, không phải kết luận vĩnh viễn; hiện đã đăng ký Playwright MCP global. Nguồn: dự án dùng template.
- [2026-09-07] Brief tiếng Việt từng lỗi encoding qua wrapper Windows ⇒ stdin UTF-8 + brief tiếng Anh ổn định hơn. Nguồn: dự án dùng template.
- [2026-09-26] `0.155.0-alpha.16.3`: probe lần 1 FAIL im lặng vì repo chưa `git init` ⇒ thêm `--skip-git-repo-check` thì PASS. Reasoning đang truyền bằng `-c model_reasoning_effort=high` — **chưa xác nhận có hiệu lực** (stream `--json` không in mức reasoning).
- [2026-09-24] Hai pool GPT (Codex + Cursor Other Models) cạn cùng lúc giữa gate SA; thời điểm đó đã dùng Claude Code `sonnet` làm dự phòng BA/SA. Chính sách 2026-09-30 thay thế đường fallback này: phải loại platform đang làm Leader; dự án cấm model Claude cho worker (nếu `CLAUDE.md` ghi) thì áp thêm. Nguồn: thực nghiệm 2026-09-24.
- [2026-10-14] **Đặt tên session** (`0.159.2`, probe): `exec` không có cờ tên; `title` của thread = tin nhắn đầu ⇒ dòng đầu brief ghi `<Dự án>-<đợt>-SA`. Thread lưu `cwd` (bảng `threads` trong `~/.codex/state_5.sqlite`, `session_meta` của file rollout) — dùng để đối chiếu khi nghi bốc nhầm. Không dùng `resume --last` (lấy thread gần nhất của cả máy).

## Guardrails

- Đọc model mặc định/config trước khi gọi; bỏ `-m` không đồng nghĩa dùng model rẻ.
- Tự kiểm citation, diff; evidence BA/SA tốt không tự chứng minh chất lượng Dev/UI.

## Nguồn chính thức

- [ChatGPT Work và Codex pricing/limits](https://learn.chatgpt.com/docs/pricing)
- [Codex CLI command reference](https://learn.chatgpt.com/docs/developer-commands?surface=cli)
