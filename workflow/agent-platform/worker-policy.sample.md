# Worker policy — MẪU

> 🔴 **Đây là file MẪU, không phải quy định chung.** Nội dung bên dưới là cấu hình **của một người** (Leader Claude Code · BA/SA Codex · Dev Cursor · Reviewer Antigravity) — để tham khảo cách viết.
>
> Mỗi người chạy Leader **bắt buộc** có bản của mình: copy thành **`worker-policy.local.md`** cùng thư mục rồi sửa theo platform, tài khoản, ngân sách của mình. File `.local.md` bị gitignore — không commit, không đẩy về template.
>
> Thiếu `worker-policy.local.md` ⇒ hook `check-worker-session.js` **chặn mọi lệnh gọi worker**. Luật chung mà policy cá nhân không được phá: [agent-platform/README](./README.md#-chọn-platform--model-cho-worker--policy-cá-nhân-bắt-buộc).

---

**Chủ sở hữu**: <tên> · **Máy**: <tên máy> · **Cập nhật**: YYYY-MM-DD

## 1. Platform đang có trên máy

| CLI | Version (ngày kiểm) | Đường dẫn | Probe trên version này | Profile |
|---|---|---|---|---|
| Claude Code | `2.1.282` (2026-09-25) | trong PATH (`claude`) | Leader — đang dùng | [claude-code.md](./claude-code.md) |
| Codex CLI | `codex-cli 0.159.2` (2026-10-09) | `%LOCALAPPDATA%\CodexTools\codex.cmd` | ✅ 2026-10-09 — đọc/ghi/shell | [codex.md](./codex.md) |
| Cursor Agent | `2026.09.23-86fc751` (2026-09-25) | `%LOCALAPPDATA%\cursor-agent\cursor-agent.ps1` | ⚠️ chưa — evidence cũ ở `2026.09.10` | [cursor.md](./cursor.md) |
| Antigravity | `agy 1.3.5` (2026-10-14) | `%LOCALAPPDATA%\agy\bin\agy.exe` (PATH PowerShell) | ✅ 2026-10-14 — đọc/shell/ghi | [google-antigravity.md](./google-antigravity.md) |
| Playwright MCP | global npm | `%APPDATA%\npm\playwright-mcp.ps1` | — | [README](./README.md#browser-playwright-mcp--dùng-chung-mọi-platform) |

> Platform chưa có profile ⇒ probe (đọc · ghi · shell · browser nếu cần) rồi viết profile trước khi giao việc thật.

## 2. Phân vai

> Trước mỗi lần gọi: mở profile ở cột *Profile* §1 — mục *Harness / cách gọi* và **`## Kinh nghiệm`** (lọc theo vai + model). Brief: khuôn chung + bảng *Brief theo vai* ở `workflow/practices/delegating-workers.md` §1. Kinh nghiệm mới về một CLI/model ⇒ ghi vào **profile** (đẩy về template), không ghi ở đây — file này chỉ chứa **lựa chọn** của bạn.

| Vai | Platform chính | Model khởi đầu | Dự phòng (theo thứ tự) |
|---|---|---|---|
| **Team Leader** | Claude Code | Opus | — |
| **BA/SA** | Codex | model mạnh nhất cho phân tích (`-m` tường minh, xem [codex.md](./codex.md)), reasoning `high` | Antigravity → Cursor |
| **Dev** | Cursor | `cursor-grok-4.6-high` | Antigravity — khi đó Reviewer phải dùng model **khác** Dev |
| **Reviewer** | Antigravity | `gemini-3.8-flash-high` (khác nhà với Dev) | Cursor với model khác Dev |
| **End User / vai phát sinh** | Cursor hoặc Antigravity theo quyền và nội dung | khác model Dev | chỉ đổi giữa hai platform này |

## 3. Luật riêng của tôi

- **Không giao worker nào cho Claude Code** — cùng account ⇒ rút đúng pool của Leader.
- **Codex chỉ cho BA/SA** — Codex app/CLI/cloud chung allowance; giữ cho phân tích. Codex hết quota ⇒ đổi platform, **không hạ model** trong cùng pool.
- **Dev ưu tiên model "nhà trồng" của Cursor** (pool Cursor Models, lượng dùng lớn hơn); chỉ sang pool Other Models khi nhà trồng không đảm bảo chất lượng (có evidence) hoặc hết quota — ghi lý do vào log.
- Hết mọi lựa chọn hợp lệ ở bảng §2 ⇒ báo vướng mắc, không tự phá luật.

## 4. Quota & ngân sách

| Platform | Billing | Xem quota ở đâu | Hết thì |
|---|---|---|---|
| Claude Code | subscription, rolling 5h + tuần | `/status` | Leader dừng, báo User |
| Codex | ChatGPT plan, 5h + tuần | `/status`, [`tools/codex-rate-limits.js`](./tools/codex-rate-limits.js) | đọc reset credit trước, tiêu credit chỉ khi User đồng ý; không thì đổi platform |
| Cursor | hai pool tháng (nhà trồng / Other Models) | dashboard Spending | đổi pool theo §3, rồi đổi platform |
| Antigravity | pool riêng | `agy` báo lúc chạy | đổi platform |

- **Overage / auto-reload**: không bật khi chưa có ủy quyền ngân sách.
