# Agent Sessions — Đợt 261114 Dịch FC 26 (Career Mode) lên TV

**Đơn vị**: một session cho mỗi vai trong đợt, sống suốt đợt, không giới hạn tuổi. Tên session: `VietSub-261114-<vai>` ([§8](../../workflow/practices/delegating-workers.md)).

🔴 **Trước khi gọi worker, tra bảng này.** Có session ⇒ **resume**, brief chỉ chứa delta.
Chưa có ⇒ tạo mới (đặt tên), rồi ghi tên + id vào đây **ngay sau lượt gọi đầu**.

| Vai | Tên session | Platform / model | Session ID | Lượt cuối | Trạng thái |
|---|---|---|---|---|---|
| BA/SA | `VietSub-261114-SA` | claude-code / claude-opus-5-5 | `b118b56d-eabb-42df-bd25-4a007a6ce832` | 2026-11-15 discovery lượt 2 (xong) | sống — resume cho SA gate |
| Dev | `VietSub-261114-Dev` | claude-code / claude-sonnet-5-5 | `<TBD>` | — | chưa gọi |
| Reviewer | `VietSub-261114-Reviewer` | claude-code / claude-opus-5-5 | `<TBD>` | — | chưa gọi |
| End User | — | — | — | — | chưa dùng |

Ngoài bảng (không phải vai, không resume): probe năng lực CLI `VietSub-261114-probe` — xem workflow log.

Cách resume: claude `--resume <id>`.
