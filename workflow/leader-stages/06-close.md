# Công đoạn 06 — Đóng đợt

> **Vào đây khi**: **User chỉ định đóng đợt** — thời điểm do User quyết, không phụ thuộc đợt đã lên production hay chưa.
> **Ra khỏi đây khi**: tài liệu hiện trạng khớp code, lesson đã được phân loại + chuyển thành rule, đợt được đánh dấu `CLOSED`.

---

## 1. 🔴 Leader không tự đóng đợt, cũng không giục đóng

Đợt đã bàn giao ([05](./05-handover.md)) thì nằm ở `HANDED_OVER` cho đến khi User nói đóng. Leader không đề xuất đóng chỉ vì "đã bàn giao lâu rồi" — dev có lịch merge/deploy riêng, tester có thể còn báo lỗi về.

Khi User chỉ định đóng mà chưa rõ: hỏi một câu — *đợt đã có phản hồi từ dev/tester chưa, còn lỗi nào treo không?* Còn lỗi treo ⇒ ghi vào `present/backlog-and-techdebt.md` hoặc mở đợt mới theo ý User, không âm thầm bỏ qua.

---

## 2. Tổng kết đóng đợt

Đọc lại: draft (6 ô) · overview · handover · lessons · workflow log.

```markdown
# Đóng đợt [YYMMDD]

## 1. Đối chiếu mục tiêu (6 ô)
- [x] Ô 3 — "đủ đến đâu": …
- [ ] …: chưa đạt — vì … — chuyển vào …

## 2. Phản hồi sau bàn giao
- Lỗi dev/tester báo: … — đã sửa / treo ở …
- (Nếu User cung cấp) kết quả thật trên dev/production: …

## 3. Việc dọn
- Gỡ cấu hình tạm: …
- Lesson → rule: N mục (xem §4)
```

🔴 Không bịa số vận hành. Leader không theo dõi production; chỉ ghi số liệu User/dev đưa, ghi rõ nguồn.

---

## 3. Các bước đóng

1. **Cập nhật tài liệu hiện trạng**: `project-docs/present/` + README các module bị đụng — khớp hiện trạng mới. Nợ kỹ thuật / backlog mới ⇒ `present/backlog-and-techdebt.md`.
2. **Chuyển lesson thành rule** — §4.
3. **Dọn local**: gỡ cấu hình tạm, tắt server/tunnel Leader còn bật. Xoá nhánh **không** phải việc của Leader.
4. **Đưa script dùng lại được lên `project-docs/automation/scripts/`**: rà script của đợt (`playtest/`, `<module>/scripts/` ở từng repo module, scratchpad) — cái nào đợt sau còn dùng ⇒ bỏ ID cứng/secret (nhận qua env/tham số), chép lên và ghi vào README của thư mục đó. Phần còn lại để nguyên (gitignore) hoặc xoá. Luật vị trí: [03-write-docs](./03-write-docs.md#-script-thử-nghiệm-đặt-ở-đâu).
5. **Đánh dấu `CLOSED`** trong overview đợt, workflow log, và `project-docs/overview-project.md`.
6. Tài liệu đợt nằm trong repo `project-docs` ⇒ đưa lệnh commit cho User (hoặc tự commit nếu đã được ủy quyền rõ cho đúng việc này).

---

## 4. 🔴 Chuyển lesson thành rule

File lessons ghi *điều gì đã xảy ra*. Đóng đợt là lúc **chắt lọc**: đọc lại cả file, cái nào xuất hiện nhiều lần hoặc hậu quả đủ lớn thì nâng thành **rule kiểm được**.

🔴 **Rule phải kiểm được khách quan.** *"Nhớ gọi BA/SA"* là vô hiệu; *"phải có file discovery trước khi viết Phase doc"* mới là rule.

### Mỗi lesson đi đâu — hai trục

| Lesson thuộc về | Đúng với **mọi dự án** dùng template | Chỉ đúng **dự án này** |
|---|---|---|
| **Quy trình** (gate, vai, cách giao việc, CLI, platform) | Sửa `workflow/` **và đẩy về template** | `CLAUDE.md` — mục *Ràng buộc đặc thù* / *Risk gate riêng* |
| **Toàn dự án** (nghiệp vụ, môi trường, dữ liệu) | — | `CLAUDE.md` hoặc `project-docs/present/` |
| **Một module** (stack, build, runtime, thư viện) | Sửa `README.md` module **và đẩy về README module ở template** | `README.md` của module trong dự án |

⚠️ **Ô góc dưới trái là ô dễ sót nhất.** Lesson nảy ra khi làm dự án này nhưng đúng với mọi dự án clone cùng module template — vd *"biến `VITE_*` là build-time, đổi phải build lại"*, *"dừng task nền không giết process `node` con do `npx` sinh ra"*. Chỉ ghi vào README của dự án ⇒ dự án sau vấp lại đúng chỗ đó.

Câu tự hỏi cho mỗi lesson: ***"Nếu mai clone template làm dự án khác, lesson này còn đúng không?"*** Đúng ⇒ nó thuộc template.

🔴 **Lesson vào README module chỉ khi thoả tiêu chí của README**: *đọc code rồi chạy thẳng vẫn dễ vấp, và vấp thì tốn công đáng kể để tìm ra/chạy lại*. Không thoả (đọc code là thấy, sửa mất vài phút) ⇒ để ở file lessons của đợt, **không** nhét vào README. README phình ra thì người đọc bỏ qua cả những dòng quan trọng.

### Đẩy về template

- Template nằm ở đường dẫn ghi trong [workflow/README](../README.md) (mục *Nguồn*). Sửa trực tiếp file tương ứng ở đó (`workflow/…`, `.claude/…` hoặc `<module>/README.md`).
- Lỗi của chính template (bug code mẫu, hook, skill) **đã được đẩy về ngay lúc sửa** ([workflow/README — đồng bộ](../README.md)) — ở đây chỉ rà lại file lessons xem có sót mục nào không.
- Ghi vào bảng *Đã chuyển thành rule* đầu file lessons: lesson · chuyển vào đâu (dự án / template / cả hai) · ngày.
- 🔴 Viết lại cho **trung lập** trước khi đẩy về template — bỏ tên riêng, ID, domain của dự án ([workflow/README](../README.md)).
- Template cũng là repo riêng ⇒ đưa lệnh commit cho User, không tự commit.

---

## 5. Artifact phải có khi đóng đợt

| Nhóm | File |
|---|---|
| **Quyết định & work package** | draft · discovery · overview → epic → phase · mockup · `[YYMMDD].sql` |
| **Trạng thái & bàn giao** | workflow log · agent-sessions · handover · lessons |
| **Bằng chứng kiểm chứng** | SA review · dev feedback · báo cáo smoke/playtest · review |

Script gọi worker (`_workers/runs/`) và brief (`_workers/briefs/`) giữ trong thư mục đợt để tra cứu forensic; stream thô (`_workers/streams/`) chỉ ở local (gitignore) — không cần đọc lại khi đóng.
