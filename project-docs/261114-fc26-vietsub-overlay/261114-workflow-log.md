# Workflow Log — Đợt 261114 Dịch FC 26 (Career Mode) lên TV

**Mode**: Direct Command · level `narrow`
**Draft**: [261114-draft.md](261114-draft.md) · **Overview**: [261114-overview.md](261114-overview.md) · **Lessons**: [261114-lessons.md](261114-lessons.md) · **Sessions**: [agent-sessions.md](agent-sessions.md)

## Roles — ai đóng vai gì, chạy trên đâu

| Vai | Platform / model | Ghi chú |
|---|---|---|
| Team Leader | claude-code / claude-opus-5-5 | |
| BA/SA | TBD (policy: claude-code / claude-opus-5-5) | |
| Dev | TBD (policy: claude-code / claude-sonnet-5-5) | |
| Reviewer | TBD (policy: claude-code / claude-opus-5-5) | **Khác session Dev** — khác model Dev |
| End User | TBD — đợt có nội dung AI sinh ra (bản dịch) | |

> Cập nhật platform/model thật khi gọi vai đó lần đầu — **ngoại lệ duy nhất** được sửa ngoài phần append.

## Ủy quyền & định hướng — ⚠️ KHÔNG ghi ở đây

Ghi ở Epic doc, mục **Định hướng User đã chốt**. Leader chỉ đọc đuôi log ⇒ header là vùng chết.

## Required Reading — cho mọi agent vào đợt

1. `CLAUDE.md`
2. `workflow/roles/<vai của bạn>.md` — **đọc trọn**
3. `project-docs/261114-fc26-vietsub-overlay/261114-draft.md` — 6 ô User đã chốt
4. `project-docs/261114-fc26-vietsub-overlay/261114-discovery*.md` — hiện trạng đã khảo sát
5. Phase doc được brief trỏ tới

---

<!-- Append-only, mới nhất ở cuối. Khuôn: YYYY-MM-DD HH:mm — <vai> — <platform>/<model> — <nội dung>
     Giờ lấy bằng lệnh hệ thống TRONG CÙNG lệnh ghi — không gõ tay. -->

2026-11-14 18:33 — Leader — claude-code/claude-opus-5-5 — Mở đợt. Chốt đủ 6 ô với User qua `/brainstorm` → [draft](261114-draft.md). Bằng chứng lớp phủ: User đã chạy Console Translator trên chính TV Sony (vẽ đè nguồn HDMI, chạy nền) ⇒ rủi ro TV chặn overlay coi như giảm, vẫn xác nhận bằng app của mình ở bước đầu.
2026-11-14 18:36 — Leader — claude-code/claude-opus-5-5 — Probe CLI worker (claude 2.1.325 đổi từ 2.1.303 ⇒ bắt buộc probe): haiku, session 98d6b922; đọc token ngẫu nhiên ✅ · ghi ngoài cwd qua --add-dir ✅ (6bc7f386dc4b-ok) · shell git rev-parse = 758b70c3 khớp HEAD ✅ · WebSearch ✅ · 0 hook · permission_denials rỗng · ~0,02 USD quy đổi. Ghi vào worker-policy.local.md. Hook check-worker-session chặn lần đầu (chưa có sổ) ⇒ tạo [agent-sessions.md](agent-sessions.md).
2026-11-14 18:36 — Leader — claude-code/claude-opus-5-5 — Giao BA/SA discovery LƯỢT 1 (định hướng + phản biện 7 giả định A1–A7, gồm câu hỏi mới của User: đặt lõi trên TV thay máy tính) → [brief](_workers/briefs/sa-discovery-r1-brief.txt) · claude-code/opus effort high · profile đã đọc: claude-code.md (rà soát 2026-10-16) + mục Kinh nghiệm · quyền: đọc + Write/Edit (chỉ report) + web + lệnh chỉ đọc · session VietSub-261114-SA b118b56d (mới — hook chặn rồi chạy lại sau khi tra sổ) · PID 10956 · quota: unknown (subscription chung pool Leader, không đọc được /status từ headless) · watcher sub checkAt 120. Brief có mục mở.
2026-11-14 18:49 — Leader — claude-code/claude-opus-5-5 — Khép lượt SA discovery 1. Verify: dòng đầu [261114-discovery.md](261114-discovery.md) = DISCOVERY-R1 DONE, 370 dòng; git status chỉ có thư mục đợt + overview-project.md (do Leader) ⇒ SA không sửa ngoài phạm vi; result is_error=false; 1 permission_denial (lệnh PowerShell foreach Get-Command không khớp allow-rule — SA tự chạy lại dạng khác, output có trong §3.1); kiểm thư mục Program Files bị sandbox chặn ⇒ U1 unknown.
2026-11-14 18:49 — Leader — claude-code/claude-opus-5-5 — Verify 5 claim load-bearing của SA: (1) D2 cơ chế overlay = SYSTEM_ALERT_WINDOW + adb appops + bỏ tối ưu pin — ĐÚNG (TV.md kyozxx/PS-Translator). (2) D1 TV của Console Translator chỉ hiển thị, lõi ở thiết bị khác — ĐÚNG ("Chỉ có chữ… đi từ iPhone sang TV qua Wi-Fi"; trang Google Play Android cũng ghi điện thoại đọc + dịch); chi tiết "core trên PC" của SA: README không nói, release pc-1.1.1 có app PC chơi trực tiếp + Remote Play chiaki-ng AGPL-3.0 — ĐÚNG. ⇒ Leader đã nói SAI với User lúc brainstorm ("app chạy ngầm trên TV… bằng chứng TV cho vẽ đè" — phần vẽ đè đúng, phần "không cần lõi ngoài TV" sai) ⇒ sửa draft + báo User. (3) D5 tài khoản PSN phụ + ngắt tay cầm khi trùng tài khoản — ĐÚNG (README). (4) D7 Android 15: miễn FGS nền cho SYSTEM_ALERT_WINDOW cần cửa sổ overlay đang hiển thị; miễn khi tắt tối ưu pin — ĐÚNG (developer.android.com). (5) Giá Sonnet 5.5 $2/$10, Haiku 5.5 $0,10/$0,50 (≤100k), cache hit Sonnet $0,10 — ĐÚNG (platform.claude.com pricing).
2026-11-15 09:38 — Leader — claude-code/claude-opus-5-5 — User duyệt kế hoạch 3 bước (lõi PC; lát 1 = Hộp thư, dùng app PS Remote Play chính thức; thêm nhóm màn; lát "không cần bật gì" với chiaki-ng chạy riêng sau 2–3 nhóm màn): "tôi đồng ý nhé". User chưa trả lời đã dùng Console Translator trên thiết bị nào ⇒ coi ô 3 "không cần bật lại" là đích của lát X. Leader trình tech stack đề xuất (engine C#/.NET 10 + WGC + Windows.Media.Ocr + SQLite + LLM + WebSocket; tv-overlay Kotlin + TYPE_APPLICATION_OVERLAY + Canvas/StaticLayout + Be Vietnam Pro) — đưa SA phản biện.
2026-11-15 09:38 — Leader — claude-code/claude-opus-5-5 — Giao BA/SA discovery LƯỢT 2 (chất vấn: KISS cho lát 1, phản biện stack, test 2 tầng cụ thể, metric, danh sách không làm, bảng lát cập nhật, câu hỏi còn lại cho User) → [brief](_workers/briefs/sa-discovery-r2-brief.txt) · RESUME session VietSub-261114-SA b118b56d (đã kiểm không còn process cũ) · claude-code/opus effort high · profile claude-code.md đã đọc lượt trước, CLI không đổi version · PID 12788 · quota unknown · watcher sub checkAt 120. Brief có mục mở.
2026-11-15 09:45 — Leader — claude-code/claude-opus-5-5 — Khép lượt SA discovery 2. Verify: dòng đầu = DISCOVERY-R2 DONE, file 612 dòng (Round 2 nối cuối, Round 1 sửa có đánh dấu); git status chỉ thư mục đợt + overview-project.md; is_error=false, permission_denials rỗng; ~7,13 USD quy đổi (pool subscription).
2026-11-15 09:45 — Leader — claude-code/claude-opus-5-5 — Verify claim lượt 2: (1) .NET 8 + 9 hết hỗ trợ 2026-11-10, .NET 10 tới 2028-11-14, bản 10.0.15 — ĐÚNG (dotnet support policy). (2) TryCreateFromWindowId cần RequestAccessAsync(Programmatic) + capability graphicsCaptureProgrammatic trong package manifest ⇒ app không đóng gói dùng interop CreateForWindow — ĐÚNG (learn.microsoft.com). (3) Probe máy (chỉ đọc, thay SA bị giới hạn lệnh): OCR AvailableRecognizerLanguages = en-US ⇒ Q5 ĐÃ GIẢI, không cần cài gói; dotnet --list-sdks = chỉ 8.0.121 ⇒ cần cài .NET 10 SDK; Windows 11 Pro build 26100; ChassisTypes=10 (Notebook) + 1 pin ⇒ máy là LAPTOP (Q9 một nửa: còn hỏi ngủ/khoá ở lát X).
2026-11-15 09:52 — Leader — claude-code/claude-opus-5-5 — User duyệt gói đề xuất sau discovery (stack điều chỉnh, lộ trình P0 → P0.5 mockup → P1 Hộp thư, Claude Sonnet 5.5 + trần 100.000 ký tự/ngày, danh sách không làm R2.6, APK 4 góc ở P0): "ok bạn cứ làm tiếp nhé". User hỏi thêm về khớp vị trí PC ↔ TV ⇒ Leader giải thích toạ độ chuẩn hoá + dò viền đen + APK 4 góc kiểm. Draft §3–§7 cập nhật. Sang /prepare-code-branch.
