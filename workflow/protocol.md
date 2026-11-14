# Protocol — Multi-agent workflow (Direct Command)

Quy tắc cho một team gồm nhiều AI agent — mỗi agent đóng một vai — phối hợp triển khai từng **đợt update**. Agent có thể chạy trên bất kỳ platform nào (Claude Code, Codex, Cursor, Antigravity…); protocol không giả định platform cụ thể.

Mode vận hành duy nhất là **Direct Command**: **Team Leader gọi CLI của worker trực tiếp**, dựng phép quan sát ngay lúc giao việc, verify kết quả rồi mới đi tiếp. User chỉ làm việc với Leader.

> Hai mode cũ (Manual — User tự gọi từng agent; Loop — agent tự poll event log) đã bỏ khỏi template. Direct Command giữ nguyên lõi của chúng, chỉ đổi *ai gọi worker*.

---

## 1. Triết lý

- **Tài liệu là source of truth.** Chat chỉ là giao diện tức thời. Đóng chat, đóng máy, compact context — mọi agent vẫn đọc tài liệu để tiếp tục.
- **User là meta-orchestrator**: chốt định hướng qua brainstorm, duyệt ở các gate, là điểm escalate cuối.
- **Leader thay User điều phối** — giải phóng User khỏi việc quản lý vi mô và thúc tiến độ. Chi tiết sứ mệnh: [team-leader §0](./roles/team-leader.md).
- **Thảo luận, không phải ra lệnh.** Làm việc với worker là hai chiều: mọi brief phải chừa chỗ để worker phản biện chính đề bài ([02-discovery §1b](./leader-stages/02-discovery.md)).
- **Verify bằng trạng thái thật, không bằng lời kể.** Exit code 0, self-report của worker, test của chính Dev — không cái nào tự nó là bằng chứng.

---

## 2. 🔴 Bốn thứ thuộc lõi — không được bỏ

| # | Lõi | Ở đâu |
|---|---|---|
| 1 | **Brainstorm với User, lấp đủ 6 ô** | [01-brainstorm](./leader-stages/01-brainstorm.md) |
| 2 | **BA/SA khảo sát (≥ 2 lượt) trước khi viết Phase doc** | [02-discovery](./leader-stages/02-discovery.md) |
| 3 | **SA thẩm định Phase doc, verdict PASS** trước khi thi công | [03-write-docs](./leader-stages/03-write-docs.md) |
| 4 | **Gate đồng thuận tài liệu với Dev** — Dev phản hồi doc, Leader xác nhận, rồi mới code | [04-implement](./leader-stages/04-implement.md) |

Mức tự chủ (narrow / general — §6) chỉ đổi **nhịp User nghiệm thu**. Nó **không** được bỏ thứ nào ở bảng trên.

---

## 3. Các vai

**Năng lực, không phải ghế.** Một agent đảm nhận một vai; cùng một vai có thể do agent/platform khác đảm nhận giữa các đợt.

🔴 **Nhận vai nào thì đọc trọn sổ tay vai đó** trước khi làm bất cứ việc gì.

| Vai | Làm gì (một dòng) | Sổ tay |
|---|---|---|
| **Team Leader** | Brainstorm với User · điều phối · viết doc · verify · bàn giao. Đầu mối **duy nhất** làm việc với User | [team-leader.md](./roles/team-leader.md) |
| **BA/SA** | Khảo sát code/tài liệu độc lập, **phản biện giả định của Leader**, thẩm định Phase doc. Read-only | [ba-sa.md](./roles/ba-sa.md) |
| **Dev** | Review doc → phản hồi → code → **tự smoke** trước khi báo xong | [dev.md](./roles/dev.md) |
| **Reviewer** | Coder thứ hai soi Dev bằng **session độc lập**: đúng kế hoạch, test, build, đối chiếu evidence/dữ liệu test của Dev; chạy lại smoke chỉ khi kế hoạch yêu cầu. Read-only với code | [reviewer.md](./roles/reviewer.md) |
| **End User** *(tuỳ chọn)* | Trải nghiệm như người dùng thật, chấm chất lượng nội dung AI sinh ra | [end-user.md](./roles/end-user.md) |

🔴 **Ranh giới quan trọng nhất**: ai sửa code thì **không được review chính patch của mình** — kể cả Leader. Reviewer phải là session độc lập, lý tưởng là khác platform và khác pool quota với Dev.

Worker chỉ cần đọc **sổ tay vai của mình** + tài liệu brief trỏ tới. Chỉ Leader đọc trọn bộ quy trình.

---

## 4. Vòng đời một đợt

### 4.1. Khởi đầu đợt

```
1. Brainstorm (User + Leader)                 → draft, ĐỦ 6 Ô              ← GATE User
2. Leader dẫn dắt BA/SA discovery, ≥ 2 lượt   → file discovery
3. [chỗ định hướng User va vào hiện trạng]                                ← GATE User
4. Leader viết Overview + Epic 1 + Phase 1 (+ file SQL đợt nếu đụng DB)
5. SA thẩm định Phase doc                     → verdict PASS
6. Leader trình User tóm tắt (bảo chứng bởi SA)                           ← GATE User*
7. Kickoff thi công                           → lessons · agent-sessions · mốc log
```

\* User đã nói rõ: **SA PASS thì không cần User đọc lại doc** — Leader trình tóm tắt rồi đi thẳng bước kế, trừ khi Phase chạm va chạm định hướng hoặc risk gate ([03-write-docs](./leader-stages/03-write-docs.md)).

### 4.2. Vòng đời một Phase

```
Leader giao Dev lượt 1: đọc doc + phản hồi — CHƯA code
  ├── phản hồi lộ chỗ hiểu sai / rà soát thiếu → quay lại BA/SA → sửa doc
  └── Leader xác nhận đồng thuận                                ← GATE
Leader giao Dev lượt 2: code + TỰ SMOKE
  → Dev báo xong (kèm báo cáo smoke)
Leader kiểm trạng thái repo TRƯỚC khi đọc report
Reviewer (session độc lập) review
  ├── có blocking → Dev sửa → Reviewer re-review phần delta
  └── pass → Leader probe độc lập phần load-bearing
        → [đợt đụng nội dung AI: End User trải nghiệm + chấm]
        → PHASE_DONE
```

🔴 **Không chuyển gate kế tiếp khi changeset mới nhất chưa có Reviewer verdict `pass`.**

🔴 **Issue sửa được trong ≤ 5 phút ⇒ sửa ngay trong vòng đó**, không defer vào backlog.

### 4.3. Kết thúc đợt

```
Hết Phase, bản local đã ngon → 05-handover (gói bàn giao + hướng dẫn test manual) → DỪNG
   (dev phụ trách chạy SQL, merge, deploy; tester test trên dev — ngoài phạm vi team AI;
    lỗi báo về ⇒ quay lại 04 trên chính nhánh đó)
User chỉ định đóng đợt → 06-close (tổng kết · cập nhật hiện trạng · chuyển lesson thành rule)
```

---

## 5. Direct Command — cách Leader gọi worker

Chi tiết đầy đủ: [practices/delegating-workers.md](./practices/delegating-workers.md). Năm rule không được quên:

1. **Chọn platform theo policy cá nhân và giữ quota Leader.** Vai nào chạy trên platform/model nào do `worker-policy.local.md` của người chạy Leader quyết (gitignore, bắt buộc có); policy không được phá [luật chung](./agent-platform/README.md#-chọn-platform--model-cho-worker--policy-cá-nhân-bắt-buộc) và luật model riêng của dự án ở `CLAUDE.md`.
2. **Brief context-by-reference.** Brief chỉ trỏ tới tài liệu (vai · Phase doc · task). Mọi spec phải sống trong tài liệu, không sống trong brief.
3. **Đọc profile platform ngay trước khi gọi** ([agent-platform/](./agent-platform/README.md)). Cờ giữa các CLI ngược nhau, CLI đổi nhanh, và **mọi lỗi gọi sai đều exit 0**.
4. **Mỗi worker chạy nền có phép quan sát độc lập**, bám artifact, không chặn lượt của User, có nhánh thoát `DONE` / `FAILED` / `STUCK` / `POLL-BROKEN`.
5. **Verify bằng trạng thái repo** (`git status` ở **từng repo** có thể bị đụng, artifact tồn tại) **trước khi** đọc report.

Session worker **sticky theo `đợt × vai`**, đặt tên `<Dự án>-<đợt>-<vai>`, tra ở `project-docs/<đợt>/agent-sessions.md` — session trên platform còn hợp lệ thì resume, brief chỉ chứa delta. Session trên platform mà policy nay không cho thì chuyển context qua artifact và tạo session ở platform hợp lệ. Hook `.claude/hooks/check-worker-session.js` chặn việc mở session mới khi chưa tra sổ.

---

## 6. Hai level tự chủ

| Level | User nghiệm thu khi | Dùng khi |
|---|---|---|
| **Narrow** *(mặc định)* | Sau mỗi Phase | Đợt mới, vùng rủi ro, User muốn cầm nhịp |
| **General** | Ở milestone (hết Epic, trước bàn giao) | Quy trình đã chạy ổn, Phase doc đã được duyệt đủ |

Level chỉ điều chỉnh **cadence**. Nó không đo thứ thực sự gây rủi ro — loại quyết định, khả năng hoàn tác, độ chắc của evidence. Những thứ đó do [risk gate](./roles/team-leader.md) chặn, ở **mọi** level.

🔴 **Đã duyệt Phase doc = đã trao quyền** cho mọi hành động doc mô tả sẵn (chạy smoke với fixture đã chốt, gọi API tốn tiền ở liều đã ghi…). Không dừng hỏi lại User những việc đó. Chỉ dừng khi gặp thứ doc **chưa** quyết, hoặc chạm risk gate.

---

## 7. Ba file sống suốt một đợt

| File | Trả lời câu | Quy tắc |
|---|---|---|
| `<đợt>-workflow-log.md` | *Đợt này đã thực sự diễn ra những gì?* | [practices/workflow-log.md](./practices/workflow-log.md) — append-only, giờ thật, mỗi lượt giao việc phải có dòng khép |
| `<đợt>-lessons.md` | *Điều gì đáng lẽ nên biết trước?* | [practices/lessons.md](./practices/lessons.md) — ghi ngay khi gặp, chuyển thành rule lúc đóng đợt |
| `agent-sessions.md` | *Đang nói chuyện với ai, ở session nào?* | [practices/delegating-workers §8](./practices/delegating-workers.md) — ghi id ngay sau lượt gọi đầu |

Workflow log tạo **ngay sau khi chốt 6 ô** ở brainstorm, không đợi tới kickoff — để các lượt thảo luận với SA và các quyết định của User đều có vết.

---

## 8. Thu dọn sau mỗi lượt chạy local

Mọi vai tự bật server/process thì **tự tắt đúng process tree mình tạo**, xác nhận port đã đóng, xoá script đo tạm. Dữ liệu test: dọn **chỉ khi kế hoạch có bước dọn**, không thì để lại và ghi vào `test-accounts.md`. Chi tiết: [guidelines/automation.md](./guidelines/automation.md).

---

## 9. Thao tác git

🔴 **Mặc định mọi thao tác git (tạo nhánh, commit, merge, push) thuộc User.** Agent đề xuất lệnh, User chạy. User ủy quyền rõ (*"làm luôn đi"*) thì agent làm **đúng việc đó, trong đợt đó**, ghi nguyên văn vào workflow log. Chi tiết: [guidelines/git.md](./guidelines/git.md).
