# Claude Code — Platform Profile

> **Rà soát**: 2026-10-16 · **Local runtime**: `2.1.301` — probe làm worker 2026-10-16: đọc · ghi · shell · resume **PASS**, browser chưa
> **Trạng thái**: vai Team Leader đã chạy nhiều đợt ở dự án dùng template.

## Chọn nhanh

| Loại việc | Điểm khởi đầu | Lý do |
|---|---|---|
| Team Leader, synthesis, kiến trúc/blocker khó | Opus; Fable khi cần năng lực cao nhất | Dành quota mạnh cho judgment và long-horizon |
| Worker (BA/SA, Dev, Reviewer…) | Sonnet hoặc model đủ sức cho vai | Làm worker được — xem quota bên dưới trước khi chọn |

Dùng Claude Code làm worker hay không là lựa chọn trong `worker-policy.local.md` của từng người ([README](./README.md#-chọn-platform--model-cho-worker--policy-cá-nhân-bắt-buộc)). Dự án có thể cấm model Claude cho mọi worker — xem `CLAUDE.md`.

## Quota và billing

- Subscription: Claude Code dùng **chung pool** với Claude web/desktop/mobile. Rolling **5 giờ** + **weekly limit**. `/status` xem allowance, `/model` xem model được cấp.
- Fable có thể không nằm trong quota thông thường như model khác — kiểm plan trước khi route.
- Có `ANTHROPIC_API_KEY` trong môi trường ⇒ CLI có thể dùng **API billing** thay vì subscription. Xác nhận auth mode trước khi fan-out.
- Extra usage ở mức API rate là overage có tiền — Leader không tự bật khi chưa có ủy quyền ngân sách.
- 🔴 **Worker Claude Code cùng account rút cùng pool với Leader** — một worker chạy dài có thể làm Leader hết quota giữa đợt. Dùng làm worker ⇒ policy cá nhân phải ghi rõ chấp nhận chia pool (vd chỉ cho lượt ngắn, model nhẹ), hoặc dùng account/API key riêng cho worker.

## Harness khi gọi làm worker

> **Probe 2026-10-16** (`2.1.301`, Windows 11, subscription, `--model haiku`, brief qua stdin, cwd = root project): đọc **PASS** (token ngẫu nhiên) · ghi **PASS** (`--permission-mode acceptEdits`) · shell **PASS** (`--allowedTools "Bash(node:*)" "PowerShell(node:*)"`, file do `node` ghi kèm PID) · thiếu quyền ⇒ **bị từ chối, exit 0** · resume **PASS** · browser **chưa probe**.

**Kết quả probe — dùng khi dựng run script:**

- 🔴 **Hook của Leader rò sang worker.** Mặc định worker nạp `.claude/settings.json` ⇒ hook `SessionStart` in *"QUY TRÌNH BẮT BUỘC…"* vào context worker. `--setting-sources user` bỏ được hook dự án nhưng **hook plugin ở user-level vẫn chạy** (vd plugin skill nạp `<EXTREMELY_IMPORTANT>`). Tắt sạch: `--setting-sources user --settings <file chứa {"disableAllHooks": true}>` ⇒ không còn `hook_started` nào trong stream. `--bare` **không dùng được với subscription** (chỉ nhận `ANTHROPIC_API_KEY`/`apiKeyHelper`).
- `CLAUDE.md` của dự án **vẫn được nạp** dưới `--setting-sources user` — đúng ý (worker vẫn phải đọc `CLAUDE.md`).
- 🔴 **Thiếu quyền ⇒ exit 0**; tool bị từ chối nằm ở mảng `permission_denials` của dòng `"type":"result"` ⇒ watcher neo `FAILED`/thiếu quyền vào mảng này khác rỗng, không vào exit code.
- **Session id**: trường `"session_id"` ở dòng `system/init` đầu stream. `--resume <id>` giữ được context (hook chạy lại với `SessionStart:resume`). Tên session không có cờ riêng ⇒ ghi `<Dự án>-<đợt>-<vai>` ở dòng đầu brief, tra bằng id.
- `total_cost_usd` trong dòng result là **giá quy đổi** (subscription vẫn in) — dùng để ước lượng mức rút pool, không phải hoá đơn.
- Hook `check-worker-session.js` của Leader từng **bỏ lọt** lệnh gọi kiểu splatting PowerShell (`& claude @args`) — đã sửa 2026-10-16 (nhận `@biến` sau tên CLI). ⚠️ Còn một kẽ: run script `.ps1` có chữ `--resume` ở **bất kỳ đâu** (vd một lượt resume ở cuối script) thì cả script được coi là resume và cho qua ⇒ mỗi run script chỉ gọi worker một lần.

Mẫu (PowerShell, cwd = root project):

```powershell
$p = '<đợt>\_workers'
Get-Content "$p\briefs\dev-<việc>-brief.txt" -Raw -Encoding UTF8 |
  claude -p "Follow the brief on stdin exactly." --model sonnet --effort medium `
    --setting-sources user --settings "$p\worker-settings.json" `
    --permission-mode acceptEdits --allowedTools "Bash(npm:*)" "Bash(node:*)" "Bash(git status:*)" "Bash(git diff:*)" `
    --output-format stream-json --verbose *>&1 |
  Out-File "$p\streams\dev-<việc>-stream.jsonl" -Encoding utf8
# worker-settings.json: {"disableAllHooks": true}
```

- `claude -p` non-interactive; `--output-format stream-json` (kèm `--verbose`) để giám sát.
- Brief: ghi ra file, **truyền qua stdin** (`Get-Content <brief> -Raw -Encoding UTF8 | claude -p "Follow the brief on stdin"`) — tránh bẫy argument của PowerShell 5.1 ([delegating-workers §2](../practices/delegating-workers.md)). Tên session = dòng đầu brief như các CLI khác — chưa kiểm cách CLI hiển thị.
- Quyền: ghi file và chạy lệnh cấp riêng qua `--permission-mode` / `--allowedTools` (vd `"Bash(npm test:*)"`) — headless không hỏi được, thiếu quyền thì tool bị từ chối im lặng.
- `--model`, `--effort`, `--fallback-model` điều khiển route.
- `--permission-mode plan` cho discovery read-only; `--allowedTools`/`--disallowedTools` giới hạn tool.
- `--json-schema` ép output contract; `--max-budget-usd` chặn spend (chỉ khi API billing).
- `--resume`, `--continue`, `--fork-session` giữ/phân nhánh context. `--worktree` tạo vùng git cô lập.
- Không dùng `--dangerously-skip-permissions` trên working tree chung chỉ để bớt thao tác.
- Worker chạy trong thư mục dự án ⇒ **nạp luôn `.claude/settings.json` + hook của dự án** — tắt bằng `--setting-sources user --settings {"disableAllHooks": true}` (đã probe, xem đầu mục).

## Ánh xạ yêu cầu của `workflow/` sang Claude Code (khi làm Leader)

| Yêu cầu | Cơ chế |
|---|---|
| Gọi worker chạy nền | `Bash`/`PowerShell` + `run_in_background: true` — **không** dùng `&` |
| Phép quan sát worker | [`tools/watch-worker.sh`](tools/watch-worker.sh) chạy **nền**: chế độ `sub` với gói subscription (ngó lại `CHECK` giãn dần 120 → 300 → 900 → 1800 s, báo ngay ở nhánh kết thúc), `api` khi Leader chạy qua OpenRouter (thêm `TICK` ≤ 230 s) — [delegating-workers §5](../practices/delegating-workers.md). 🔴 **Không dùng tool chặn lượt** — User phải nhắn được trong lúc chờ |
| Chẩn đoán treo | `Get-CimInstance Win32_Process`, đối chiếu CPU tích luỹ với thời gian chạy |
| Probe độc lập | Script trong scratchpad của session, xoá sau khi đo |
| Nạp lại quy trình sau compact | Hook `SessionStart` → `.claude/hooks/reload-after-compact.js` |
| Chặn mở session worker mới khi chưa tra sổ | Hook `PreToolUse` → `.claude/hooks/check-worker-session.js` |

> ⚠️ **Lỗi đã biết của `check-worker-session.js` (chưa sửa)**: hook khớp chuỗi trên dòng lệnh nên **chặn nhầm** lệnh chỉ đọc có nhắc tới CLI worker (`cat` một run script cũ, `git add _workers/runs/…`) và lệnh chạy một file `.ps1`/`.sh` chứa `resume <id>` (hook chỉ thấy tên file). Cách vượt hiện tại: chạy lại **đúng** lệnh đó trong vòng 10 phút. Sửa đúng: hook đọc nội dung script được gọi trực tiếp và bỏ qua lệnh chỉ đọc.


### 🔴 Hai giới hạn phải biết

1. **Danh sách skill KHÔNG được nạp lại sau compact** — chỉ skill *đã gọi* được giữ. Leader từng bỏ qua gate SA vì **không thấy skill nào để mà gọi**. ⇒ Hook `reload-after-compact.js` **nạp thẳng nội dung skill** + bản đồ công đoạn vào mọi phiên (`startup|compact|resume|clear`). Vẫn nên gọi `/<skill>` khi vào công đoạn (bản đầy đủ + lưu vết).
2. **Hook chạy ngoài model** nên là lớp duy nhất sống sót qua compact. Lời nhắc sống trong context — mà context chính là thứ bị cắt.

> **Bẫy khi sửa hook**: (a) lệnh `echo` ở đầu `.bashrc` dính vào JSON làm hook **hỏng im lặng**; (b) sửa bằng script sinh code dễ lệch ngoặc/nuốt escape. ⇒ Luôn `node --check <hook>` và bơm input thử (`echo '{"source":"compact"}' | node <hook>`), kiểm JSON hợp lệ trước khi tin là xong.
>
> 🔴 **Ghi tài liệu có ký tự đặc biệt: dùng Edit/Write, không dùng shell.** Markdown có backtick, `$`, `\` đưa qua heredoc hay `node -e "…"` trong nháy kép ⇒ bash hiểu backtick là command substitution, `\n` trong đường dẫn Windows thành xuống dòng — nội dung **bị nuốt im lặng** (ca thật: mất cả tên file, câu SQL, một dòng hướng dẫn). Edit/Write nhận chuỗi **nguyên văn** — đừng escape nháy trong đó. Cần script ⇒ ghi script ra file trong scratchpad rồi `node <file>`.
>
> ⚠️ `mtime` trên Windows không đáng tin làm tín hiệu "đợt đang làm" — hook liệt kê vài ứng viên thay vì khẳng định một file.

⚠️ Auto-mode classifier của Claude Code có thể **chặn** lệnh gọi worker kèm cờ bỏ qua quyền (vd `--dangerously-skip-permissions` của agy) — không lách; dùng allow-rule của CLI đó.

## Kinh nghiệm

- [2026-10-16] **`[IO.File]::…` trong PowerShell không theo `Set-Location`** — API .NET dùng thư mục hiện tại của process ⇒ đường dẫn tương đối ra sai chỗ (`Could not find a part of the path`). Luôn truyền đường dẫn tuyệt đối. Nguồn: dự án dùng template.

- [2026-09-27] 🔴 **Windows PowerShell 5.1 đọc `.ps1` không BOM theo mã ANSI** ⇒ chuỗi tiếng Việt trong script làm vỡ cú pháp (lỗi parse khó hiểu). Script `.ps1` do agent sinh: giữ ASCII, hoặc ghi UTF-8 **có BOM**, hoặc đưa chữ tiếng Việt ra file dữ liệu. Nguồn: thực nghiệm 2026-09-27.

- [2026-09-07] Opus giữ vai Leader, trực tiếp gọi Codex/Cursor CLI, giữ mạch nhiều phase. Nguồn: dự án dùng template.
- [2026-09-07] Leader mạnh vẫn từng hiểu sai tiêu chí đánh giá thành backlog và đưa ước lượng như số đo — model mạnh không thay decision gate, evidence hygiene và phản biện BA/SA. Nguồn: dự án dùng template.

## Nguồn chính thức

- [Claude plans and pricing](https://claude.com/pricing)
- [Models, usage, and limits in Claude Code](https://support.claude.com/en/articles/14552983-models-usage-and-limits-in-claude-code)
- [Claude Code CLI reference](https://docs.anthropic.com/en/docs/claude-code/cli-usage)
