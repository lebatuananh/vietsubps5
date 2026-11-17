# Agent Sessions — Đợt 261114 Dịch FC 26 (Career Mode) lên TV

**Đơn vị**: một session cho mỗi vai trong đợt, sống suốt đợt, không giới hạn tuổi. Tên session: `VietSub-261114-<vai>` ([§8](../../workflow/practices/delegating-workers.md)).

🔴 **Trước khi gọi worker, tra bảng này.** Có session ⇒ **resume**, brief chỉ chứa delta.
Chưa có ⇒ tạo mới (đặt tên), rồi ghi tên + id vào đây **ngay sau lượt gọi đầu**.

| Vai | Tên session | Platform / model | Session ID | Lượt cuối | Trạng thái |
|---|---|---|---|---|---|
| BA/SA | `VietSub-261114-SA` | claude-code / claude-opus-5-5 | `b118b56d-eabb-42df-bd25-4a007a6ce832` | 2026-11-18 SA gate Phase 3 (Rev 1.1 duyệt lại) | sống — resume cho gate Phase sau |
| Dev | `VietSub-261114-Dev` | claude-code / claude-sonnet-5-5 | `c22fb9fd-d0fa-4e9d-a847-5dde6c2de3ea` | 2026-11-18 E1P3 fix 3b xong | sống — resume cho sửa sau buổi chạy thật |
| Reviewer | `VietSub-261114-Reviewer` | claude-code / claude-opus-5-5 | `6f0ab583-0788-43c6-bb65-76fb5153b73f` | 2026-11-18 re-review fix 3b PASS | sống — resume khi có kết quả trên TV thật |
| End User | — | — | — | — | chưa dùng |

Ngoài bảng (không phải vai, không resume): probe năng lực CLI `VietSub-261114-probe` — xem workflow log.

Cách resume: claude `--resume <id>`.
