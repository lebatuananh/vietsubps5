# Khuôn sổ session của đợt

> Tạo ở kickoff. Hook `.claude/hooks/check-worker-session.js` đọc file này để chặn việc mở session worker mới khi chưa tra sổ. Quy tắc: [delegating-workers §8](../practices/delegating-workers.md).

---

```markdown
# Agent Sessions — Đợt [YYMMDD] <tên đợt>

**Đơn vị**: một session cho mỗi vai trong đợt, sống suốt đợt, không giới hạn tuổi. Tên session: `<Dự án>-<đợt>-<vai>` ([§8](../practices/delegating-workers.md)).

🔴 **Trước khi gọi worker, tra bảng này.** Có session ⇒ **resume**, brief chỉ chứa delta.
Chưa có ⇒ tạo mới (đặt tên), rồi ghi tên + id vào đây **ngay sau lượt gọi đầu**.

| Vai | Tên session | Platform / model | Session ID | Lượt cuối | Trạng thái |
|---|---|---|---|---|---|
| BA/SA | `<Dự án>-[YYMMDD]-SA` | <TBD> | `<TBD>` | — | chưa gọi |
| Dev | `<Dự án>-[YYMMDD]-Dev` | <TBD> | `<TBD>` | — | chưa gọi |
| Reviewer | `<Dự án>-[YYMMDD]-Reviewer` | <TBD> | `<TBD>` | — | chưa gọi |
| End User | — | — | — | — | chưa dùng |

Cách resume: codex `exec resume <id>` · cursor `--resume <id>` · agy `--conversation <id>` · claude `--resume <id>`.
```
