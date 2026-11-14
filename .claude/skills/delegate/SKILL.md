---
name: delegate
description: Giao việc cho worker (BA/SA, Dev, Reviewer, End User) bằng cách gọi CLI của nền tảng mà policy cá nhân (workflow/agent-platform/worker-policy.local.md) chọn cho vai đó. Dùng khi Team Leader cần triệu hồi một vai ở bất kỳ công đoạn nào. Trỏ tới quy tắc brief context-by-reference, đọc profile platform trước khi gọi, quota, quyền theo vai, phép quan sát không chặn lượt User, và verify bằng trạng thái repo.
---

# Giao việc cho worker

🔴 **Đọc trọn và làm theo**: [`workflow/practices/delegating-workers.md`](../../../workflow/practices/delegating-workers.md)

## 🔴 Năm thứ làm trước khi gõ lệnh

**0. Chọn platform theo policy** — đọc `workflow/agent-platform/worker-policy.local.md` (của người đang chạy Leader, gitignore). **Chưa có ⇒ dừng**, cùng User lập từ `worker-policy.sample.md` — hook chặn mọi lệnh gọi worker tới khi có. Luật chung policy không được phá ([agent-platform/README](../../../workflow/agent-platform/README.md)): Reviewer độc lập với Dev (khác platform hoặc ít nhất khác model) · không để worker rút cạn pool của Leader · luật model riêng của dự án ở `CLAUDE.md` đè lên policy. Session cũ trên platform policy nay không cho thì chuyển context bằng artifact, không resume.

**1. Tra sổ session** `project-docs/<đợt>/agent-sessions.md` — có session của vai trong đợt ⇒ **resume**, brief chỉ delta. Hook `check-worker-session.js` chặn lệnh mở session mới. Session mới phải **đặt tên** `<Dự án>-<đợt>-<vai>` (dòng đầu brief) — [§8](../../../workflow/practices/delegating-workers.md).

Session cũ treo lặp lại ⇒ sau khi xác minh không còn tiến trình và không lặp paid call mơ hồ, **thử một session mới cùng platform/model** với artifact handoff trước khi đổi platform; ghi ngoại lệ vào sổ.

**2. Mở profile của platform sắp gọi** — [`workflow/agent-platform/`](../../../workflow/agent-platform/README.md) (link ở cột *Profile* của policy). Đọc **hai phần**: *Harness / cách gọi* (cờ, quyền, cách truyền brief) và **`## Kinh nghiệm`** — lọc dòng đúng **vai + model** sắp giao: lỗi model đó từng mắc ⇒ brief phải đòi thêm gì. Trả lời đủ **năm câu** ở [delegating-workers §2](../../../workflow/practices/delegating-workers.md). Cờ giữa các CLI **ngược nhau**, CLI **đổi nhanh**, và **mọi lỗi gọi sai đều exit 0**. Platform chưa có profile ⇒ probe và viết profile trước khi giao việc thật. Bảng chặn sai lầm thô cho các CLI đã có profile (vẫn phải mở profile):

| Platform | Ghi file | **Chạy lệnh** | Brief qua | Bẫy riêng |
|---|---|---|---|---|
| Cursor | `--trust` | **`-f`** (+ `--approve-mcps`) | `-p $brief` đọc từ file | không có `-m`; stream in lại prompt |
| Codex | `-s workspace-write` | `-s workspace-write` | 🔴 **stdin** (kèm `-` cuối) | truyền `-m` tường minh (model hiện hành: `codex.md`) |
| Antigravity | allow-rule `write_file(*)` | allow-rule `command(*)` | prompt **một dòng** trỏ file brief | `--print-timeout 0s`; không dùng `--dangerously-skip-permissions` |
| Claude Code | ⚠️ chưa probe làm worker — xem `claude-code.md` | | stdin (dự kiến) | nạp cả hook của dự án; chung pool với Leader nếu cùng account |

**3. Brief ghi ra file** `project-docs/<đợt>/_workers/briefs/<vai>-<việc>-brief.txt` — tiếng Anh, context-by-reference, **có mục mở**, không nháy kép, bullet `*` không dùng `-`. Khuôn chung + **bảng "Brief theo vai"** (thứ bắt buộc riêng của SA gate, Dev lượt 1/2, Reviewer, re-review, End User): [§1](../../../workflow/practices/delegating-workers.md#-brief-theo-vai--đọc-phần-riêng-trước-khi-viết). Vai chạy lệnh ⇒ có khối `NOT YOURS`.

**4. Quota preflight** — snapshot hoặc ghi rõ `unknown`; hết quota ⇒ đổi platform, không hạ model cùng pool.

## Khi gọi

- Script `project-docs/<đợt>/_workers/runs/run-<vai>-<việc>.ps1`, stream ra `_workers/streams/<vai>-<việc>-stream.jsonl` (gitignore).
- Chạy bằng `PowerShell`/`Bash` với **`run_in_background: true`** — gọi **một mình**, không nối `;`.
- **Cùng lượt**, dựng phép quan sát chạy **nền** (không dùng tool chặn lượt — User phải nhắn được): nhánh `DONE` theo artifact · `FAILED` theo event lỗi của CLI · `STUCK` · `POLL-BROKEN`, và ở gói sub thêm **mốc ngó lại `CHECK` giãn dần 120 → 300 → 900 → 1800 s** (tham số 7 `checkAt` của `watch-worker.sh`). Mỗi `CHECK`: đọc đuôi stream, ~2 phút hỏi *đọc đúng file / đúng cờ chưa*, ~5 phút hỏi *hướng có khớp Phase doc không*, sau đó hỏi *còn chạy xuôi hay kẹt*; ổn thì bật lại với mốc kế. `idleSec` 600 cho lượt có build/emulator.
- Ghi dòng giao việc vào workflow log: việc · platform/model/effort · profile đã đọc · session · quota.
- Lượt đầu của session ⇒ ghi session id vào sổ **ngay**.

## Khi worker xong

1. 🔴 `git -C <repo> status --short` ở **từng repo** (mỗi module là repo riêng — root không thấy gì) + artifact tồn tại — **trước khi** đọc report. Vai read-only mà có file code đổi ⇒ xử lý ngay.
2. Đọc handoff capsule; mở report chi tiết khi cần quyết định.
3. Worker để lại server/process ⇒ dừng đúng process tree, kiểm port.
4. Ghi dòng khép vào log: **cái đã verify**, không chép self-report.

## User muốn dừng giữa lượt để brainstorm lại

Mặc định **không dừng**: brainstorm song song, áp kết quả ở điểm dừng kế tiếp (Rev + SA gate nếu đổi Phase doc, resume với delta). Chỉ dừng cứng khi chờ là phí rõ ràng — kèm checklist năm bước. [§11](../../../workflow/practices/delegating-workers.md#11--user-muốn-dừng-giữa-lượt-để-brainstorm-lại)

## Quyền theo vai

| Vai | Phải làm được |
|---|---|
| BA/SA | Đọc repo, chạy lệnh chỉ đọc (`grep`, `git`), ghi report + mockup trong thư mục đợt — không sửa code |
| Dev · Reviewer · End User | Đọc, ghi, **chạy lệnh**, browser khi Phase có UI |

Platform không cấp được quyền vai cần ⇒ **đổi platform**, đừng hạ chuẩn.
