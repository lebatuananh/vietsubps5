# Ghi workflow log — kỹ năng dùng ở mọi công đoạn

> **Không phải một công đoạn.** Có việc đáng ghi ở bất kỳ đâu — lúc brainstorm, lúc worker trả về, lúc User quyết, lúc gặp sự cố.

---

## Log này viết cho ai

🔴 **Câu hỏi log phải trả lời: *"đợt update này đã thực sự diễn ra những gì?"***

Ở Direct Command, việc giao đã xong qua CLI ngay lúc đó. Log không có nhiệm vụ điều phối — nó là **log thực thụ**, viết cho **người đọc sau**:

| Ai đọc | Đọc để |
|---|---|
| **User** | Nhìn một lượt là biết đợt đã tới đâu, đã xảy ra gì |
| **Leader sau compact** | Dựng lại trạng thái mà không phải đọc cả repo |
| **Leader khác tiếp quản** | Hiểu vì sao các quyết định đã được đưa ra |

📗 **Phép thử**: một người chưa từng theo đợt này đọc log có dựng lại được diễn biến không? Không ⇒ log chưa đạt, bất kể đã ghi bao nhiêu dòng.

---

## Tám nguyên tắc

| # | Nguyên tắc |
|---|---|
| 1 | **Leader ghi chính** |
| 2 | Leader **có thể dặn worker tự ghi** — *"code xong thì thêm một dòng vào log"* |
| 3 | Mỗi dòng đủ **thời gian thật · ai ghi (vai · platform · model)** |
| 4 | **Ngắn gọn.** Kết quả thì **dẫn link file**, không trình bày dài trong log |
| 5 | Dòng **giao việc** ghi: việc cực ngắn + **platform · model · effort** + quota |
| 6 | 🔴 **Ghi ngay, không dồn cuối đợt** |
| 7 | 🔴 **Mỗi lượt giao việc phải được khép bằng dòng kết quả** |
| 8 | 🔴 **Lượt hỏng cũng ghi, kèm nguyên nhân thật** |

### 🔴 6. Giờ thật — gộp lấy giờ và ghi log vào MỘT lệnh

```bash
TS=$(date "+%Y-%m-%d %H:%M") && cat >> <log> << EOF
$TS — Leader — <platform>/<model> — <nội dung>
EOF
```

```powershell
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm'; Add-Content -Encoding utf8 <log> "$ts — Leader — <platform>/<model> — <nội dung>"
```

Chạy `date` ở một lệnh rồi soạn log ở lệnh khác **tạo khoảng trống để tự chế số**. Đã tái phạm nặng: Leader **có chạy `date`** nhưng khi viết lại bịa một dãy tròn trịa đều đặn — sai gần 2 tiếng, **24 dòng liên tiếp toàn giờ tương lai**, User phải phát hiện. Lỗi này **giả dạng đúng quy trình**: có `date` trong lịch sử lệnh, giờ trông hợp lý, thứ tự tăng dần.

- **Tuyệt đối không** nội suy từ dòng trước, ước lượng theo thời lượng, tái dùng giờ cũ, hay gõ theo trí nhớ — kể cả "vừa chạy `date` xong nên nhớ rồi".
- Không lấy được đồng hồ hệ thống ⇒ **không append**; sửa phép đo trước.
- Định kỳ tự kiểm: so giờ ở `tail` log với giờ hiện tại — **bất kỳ mốc tương lai nào cũng sai chắc chắn**.
- Sửa hàng loạt timestamp do chính mình ghi sai ⇒ **sửa tại chỗ** (lỗi ghi chép, không phải thông tin nghiệp vụ).

### 🔴 7. Lượt giao việc phải được khép lại

Dòng giao việc treo lơ lửng là lỗi. Người đọc thấy *"đã gọi Dev code Phase 2"* rồi nhảy sang *"gọi Reviewer"* — không biết Dev làm được không, có phải gọi lại không.

| Ai ghi | Ghi gì |
|---|---|
| **Worker** | Đã làm xong gì + **link file kết quả**. Không tự đánh giá |
| **Leader** | Đã **verify** ra sao + quyết định tiếp theo |

🔴 **Dòng của Leader ghi cái đã kiểm, không chép self-report của worker.**

- ❌ `Dev báo code xong Phase 2`
- ✅ `Verify: git status 3 file khớp báo cáo. Test 14/14 (baseline 12). Port 3000 trống. Giao Reviewer.`

### 8. Lượt hỏng cũng ghi

> `Dev báo "shell blocked". Probe: đúng — gọi thiếu cờ cho phép chạy lệnh. Gọi lại với cờ đúng.`

Chính dòng sự cố khiến người sau không vấp lại. Nguyên nhân phải là **nguyên nhân thật**, không phải phỏng đoán đầu tiên.

---

## Khuôn một dòng

```
YYYY-MM-DD HH:mm — <vai> — <platform>/<model> — <nội dung>
```

🔴 `<platform>/<model>` phải là **model và nền tảng đang thực thi phiên đó tại thời điểm ghi** — đối chiếu runtime context, không copy theo thói quen từ dòng trước.

### Quota trong dòng giao/khép việc

```text
quota before: measured <used>/<limit> (<unit>), reset <time>
quota: estimated <range + unit>, basis <nguồn + cách tính>
quota: unknown (<lý do>)
quota after / delta: … — chỉ khi đo được cùng pool, cùng đơn vị
```

Không dùng thời lượng, số tool call, tên model hay bảng giá để tự điền phần trăm/chi phí.

**Mức chi tiết**: mặc định **1–2 dòng**. Sự cố và quyết định được dài hơn nhưng gói trong **~5 dòng** — chi tiết đẩy ra file riêng rồi dẫn link.

Ví dụ một lượt trọn vẹn:

```markdown
2026-09-18 14:20 — Leader — claude-code/opus-5 — Giao Dev lượt 2 (code) Phase 1. cursor/grok-4.6-high · resume `0a76…` · brief `_workers/briefs/dev-p1-code-brief.txt` · quota: unknown (no CLI surface)
2026-09-18 14:45 — Dev — cursor/grok-4.6-high — Code xong. Report: `reviews/260918-epic1-phase1-dev-report.md`
2026-09-18 14:52 — Leader — claude-code/opus-5 — Verify: git status 5 file khớp phạm vi. Test 143/143 (baseline 125). Port 3099/5173 trống. Giao Reviewer agy/gemini-3.8-flash-high (session mới, ghi sổ).
```

---

## Những mốc chắc chắn phải ghi

| Mốc | Ghi gì |
|---|---|
| 🔴 **Mở đợt (ngay sau 6 ô)** | Bài toán đã chốt · link draft |
| **Thảo luận discovery với SA** | Mỗi lượt giao (≥ 2 dòng riêng) · kết quả · claim đã verify |
| **Xong một công đoạn** | Kết quả + artifact sinh ra |
| **Giao việc worker** | Việc gì + platform/model/effort + profile đã đọc + quota |
| **Worker trả về** | Kết quả **đã verify** + quyết định tiếp |
| **User quyết một điều gì** | **Nguyên văn** câu User nói, nhất là khi đổi hướng |
| **Ủy quyền** | Nguyên văn + phạm vi + điều kiện |
| **Phát hiện đổi kế hoạch** | Phát hiện gì, đổi gì, vì sao |
| **Sự cố** | Hiện tượng · nguyên nhân thật · cách gỡ |
| **Bất thường quota/thời gian** | Hết quota giữa chừng, worker chạy lâu bất thường |
| **Checklist mở Phase** | Đã điền ([team-leader §6](../roles/team-leader.md)) |

---

## Kỷ luật file

- 🔴 **Append-only.** Chỉ thêm ở cuối. Không sửa/xoá dòng cũ — cần đính chính thì thêm dòng mới nói rõ dòng nào sai. (Ngoại lệ: timestamp tự ghi sai — xem nguyên tắc 6.)
- 🔴 **Đọc lại đuôi file trước khi ghi.** Worker chạy nền có thể đang ghi cùng lúc.
- Ngoại lệ thứ hai được sửa ngoài phần append: **header** — cập nhật platform/model thật trong bảng Roles khi một vai vào đợt.
- Ủy quyền/định hướng chi phối cả Epic **không** ghi ở header (Leader chỉ đọc đuôi log ⇒ header là vùng chết) — ghi ở Epic doc mục *Định hướng User đã chốt*.

---

## Khác gì với hai file kia

| | **workflow log** | [lessons](./lessons.md) | Phase doc |
|---|---|---|---|
| Trả lời | *Đã diễn ra những gì* | *Điều gì đáng lẽ nên biết trước* | *Phải làm gì* |
| Thứ tự | Thời gian | Chủ đề | Cấu trúc công việc |
| Vòng đời | Một đợt | Một đợt, rồi chắt thành rule | Một Phase |
