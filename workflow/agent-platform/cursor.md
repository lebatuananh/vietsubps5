# Cursor CLI — Platform Profile

> **Rà soát**: 2026-09-25 · **Local runtime**: `2026.09.23-86fc751` — ⚠️ **chưa probe trên bản này** (evidence dưới đây ở `2026.09.10`)
> **Trạng thái**: vai Dev đã chạy nhiều đợt; CLI đổi nhanh — kiểm `--help` và `models` trước mỗi đợt lớn.

> **Evidence tốt nhất**: Dev; Reviewer với model khác Dev. Dùng cho vai nào là lựa chọn trong `worker-policy.local.md` ([README](./README.md#-chọn-platform--model-cho-worker--policy-cá-nhân-bắt-buộc)).

## Hai pool model — chọn pool trước, rồi mới chọn model

| Pool | Model | Id |
|---|---|---|
| **Cursor Models** ("nhà trồng", lượng dùng lớn hơn) | Cursor Grok 4.6 · Grok 4.5 · Composer 2.5 | `cursor-grok-4.6-{low,medium,high,xhigh}` · `cursor-grok-4.5-high` · `composer-2.5` (+ `-fast`) |
| **Other Models** (tính theo list price bên thứ ba) | mọi `gpt-*`, `claude-*`, `gemini-*` — kể cả gọi từ Cursor | |

- Pool nhà trồng thường rẻ/nhiều lượt hơn; đổi pool vì chất lượng hay quota ⇒ ghi lý do vào log. Thứ tự ưu tiên pool là lựa chọn trong policy cá nhân.
- ⚠️ Dev và Reviewer **không trùng model**. Cả hai buộc ở Cursor ⇒ chọn hai model khác nhà (vd Dev `cursor-grok-4.6-high`, Reviewer `cursor-grok-4.5-high` hoặc `gemini-3.8-flash-high`).

⚠️ **Pool Other Models cạn ⇒ `ActionRequiredError: You've hit your usage limit`, exit 0, không artifact** (reset theo chu kỳ tháng). Fallback sang Other Models ⇒ probe prompt 1 dòng trước.

## Quota

Hai pool độc lập, reset theo billing cycle tháng, không rollover. Dashboard **Spending** là source-of-truth. `Auto` vẫn tính theo model thực tế được route, không miễn phí. Fast variants đắt hơn.

## Harness và cách gọi

- `cursor-agent -p` non-interactive; `--output-format stream-json`.
- `--mode plan` / `--mode ask` read-only — **không ghi được file** ⇒ không dùng khi worker phải xuất report.
- `--workspace` = **root project**; `--add-dir`, `--worktree`, `--sandbox` theo phạm vi.
- `--resume <id>` giữ session; pin `--model` khi cần tái hiện.

### ⚠️ Cờ đã verify

| Cần gì | Cờ đúng | Bẫy |
|---|---|---|
| Chỉ định model | `--model <id>` | 🔴 **Không có `-m`** — dùng `-m` thì `unknown option`, **exit 0** |
| Ghi file | `--trust` | Thiếu ⇒ dừng ở "Workspace Trust Required", exit 0 |
| **Chạy lệnh** (test, build) | **`-f`** (= `--force`/`--yolo`) | 🔴 **`--trust` KHÔNG đủ** — mọi lệnh trả `Rejected:` im lặng |
| Nạp MCP (Playwright) headless | `--approve-mcps` | Thiếu ⇒ server không nạp |

| Vai | Cờ |
|---|---|
| BA/SA (đọc · chạy lệnh chỉ đọc như `grep`/`git` · ghi report) | `--trust -f` — Cursor không có chế độ "chỉ lệnh đọc"; brief cấm sửa code, Leader verify bằng `git status` |
| **Dev** · **Reviewer** · **End User** | **`-f --approve-mcps`** |

#### 🔴 Bẫy ký tự trong brief

Brief qua `-p <string>` có `->`, `--xxx`, `-x` ở đầu dòng ⇒ parser nuốt thành cờ ⇒ `unknown option`, exit 0. Ghi brief ra file, đọc `-Raw` vào biến; trong brief dùng `=>`/`→` và bullet `*`.

```powershell
$brief = Get-Content '<đợt>\_workers\briefs\<vai>-<việc>-brief.txt' -Raw -Encoding UTF8
& "$env:LOCALAPPDATA\cursor-agent\cursor-agent.ps1" -p $brief --model cursor-grok-4.6-high `
  --workspace '<root project>' -f --approve-mcps --output-format stream-json *>&1 |
  Out-File '<đợt>\_workers\streams\<vai>-<việc>-stream.jsonl' -Encoding utf8
```

⚠️ Stream của Cursor **in lại prompt** ở những dòng đầu ⇒ monitor grep chuỗi kết thúc có trong brief sẽ báo DONE giả. Neo `DONE` vào event assistant/result hoặc vào file report.

⚠️ CLI từng đứng ở `thinking completed` 25 phút sau khi đã ghi đủ report + dòng log ⇒ `DONE` phải có đường theo artifact; artifact đủ + stream đứng ⇒ dừng tiến trình.

🔴 Sau mỗi lượt: `git -C <repo> status --short` ở **từng repo** + artifact tồn tại **trước khi** đọc report.

## Kinh nghiệm

- [2026-09-07] `gemini-3.8-flash-high` trong Cursor làm Dev/UI nhanh, có browser/MCP. Nguồn: dự án dùng template.
- [2026-09-07] Worker từng sửa file ngoài scope để làm toàn bộ test xanh; từng hardcode một giá trị dữ liệu thật trong đúng file được phép sửa ⇒ bắt buộc baseline, kiểm file đổi, blackbox dữ liệu thật. Nguồn: dự án dùng template.
- [2026-09-11] Giao Dev bằng `--trust` ⇒ mất 2 lượt vì không chạy được test. Nguồn: thực nghiệm 2026-09-11.
- [2026-09-25] `cursor-grok-4.6-high` Dev hai Phase frontend liền: 143→157 test, đúng phạm vi, tự dọn port. Nguồn: thực nghiệm 2026-09-25.

## Guardrails

- Luôn ghi cả model ID/effort — không ghi chung "Cursor làm tốt".
- Không song song hai worker trên cùng working tree/data nếu chưa cô lập.
- Kết quả xanh hơn baseline dự kiến cũng là anomaly cần điều tra.

## Nguồn chính thức

- [Cursor models, pricing and usage pools](https://prod.cursor.com/docs/models-and-pricing)
- [Cursor CLI parameters](https://docs.cursor.com/en/cli/reference/parameters)
