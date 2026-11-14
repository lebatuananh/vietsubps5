# Công đoạn 04 — Implement: vòng Dev ↔ Reviewer tới `PHASE_DONE`

> **Vào đây khi**: Phase doc đã có SA verdict PASS ở [03](./03-write-docs.md) (và đợt đã kickoff).
> **Ra khỏi đây khi**: Reviewer `pass` trên changeset mới nhất + Leader đã probe ⇒ `PHASE_DONE`. Hết Phase ⇒ [05-handover](./05-handover.md).
>
> Cách gọi worker (brief, cờ CLI, quan sát, verify repo): [delegating-workers](../practices/delegating-workers.md).

---

## Vòng lặp

```
Leader giao Dev lượt 1: đọc doc + phản hồi — CHƯA code
  → Dev phản hồi                               ← GATE đồng thuận
  → [lộ ra hiểu sai / rà soát thiếu → quay lại BA/SA → sửa doc]
  → Leader xác nhận → giao Dev lượt 2: code
  → Dev code + tự smoke → báo xong
  → Leader kiểm trạng thái repo TRƯỚC khi đọc report
  → Reviewer độc lập review
  ├── có blocking → Dev sửa → lặp từ "Dev báo xong"
  └── pass → Leader probe độc lập phần load-bearing
        → [đợt đụng nội dung AI: End User trải nghiệm + chấm]
        → PHASE_DONE
```

**Nhiều vòng là bình thường.** 🔴 Nhưng **vòng mới lặp lại đúng lớp lỗi cũ ⇒ vấn đề ở brief hoặc Phase doc, không ở Dev.** Sửa nguồn, đừng giao lại với cùng đầu vào.

🔴 **Dev báo blocking không tự giải được ⇒ Leader xử lý**, không để Dev tự vòng qua — tự tìm đường vòng là cách sinh ra hardcode và sửa ngoài phạm vi.

---

## 🔴 Bảy thứ phải có trước khi gọi Dev

Áp cho mọi vai **tự chạy code hoặc dựng dữ liệu** (Dev, Reviewer, End User). Thiếu một mục thì worker sẽ tự xoay xở bằng cách tệ hơn — đoán, hardcode, hoặc bỏ smoke.

| Hạng mục | Thiếu thì hỏng thế nào |
|---|---|
| **Phase doc** đầy đủ + guideline liên quan | Không biết làm gì. Còn phải chép spec vào brief ⇒ doc chưa đạt |
| **Nhánh + commit** đang đứng | Code nhầm nhánh, phát hiện rất muộn |
| **Baseline test** đã chụp | Không có baseline thì "test pass" vô nghĩa |
| **Quyền chạy shell** đã probe | Không chạy được test ⇒ không đạt DoD |
| 🔴 **Tài khoản test — hoặc cách tự dựng** | **Hay bị quên nhất.** Worker báo "code xong" mà chưa từng chạy thật |
| **Môi trường đích** (localhost / dev) | Gọi nhầm ⇒ kết luận sai mà không có dấu hiệu gì |
| **Quyết định dữ liệu test** (giữ lại hay có bước dọn) | Worker tự ý dọn sạch fixture có giá trị cho lượt sau, hoặc để rác ở chỗ phải dọn |

Ba mục cuối là section **"Blackbox Verify"** của [template phase](../templates/phase.md) — dùng đúng template là có sẵn.

🔴 **Không giả định có sẵn tài khoản**: đọc `project-docs/automation/test-accounts.md` → kiểm hiện trạng → thiếu thì dựng. Dựng xong, kiểm worker **đã ghi vào file đó** — liệt kê ID trong report là chưa đủ.

🔴 **Kế hoạch yêu cầu Reviewer chạy lại smoke ⇒ Dev và Reviewer không dùng chung dữ liệu test** — dùng chung thì yêu cầu độc lập vô hiệu, cả hai cùng mù trước đúng một lớp lỗi.

### Environment preflight

Trước lượt ghi/test đầu tiên của mỗi workstream, ghi snapshot vào log: **nhánh + commit từng repo bị đụng** · môi trường đích (DB nào — **không ghi secret**) · baseline test · port đang dùng. Worker read-only ghi `N/A`.

> Bẫy tốn thời gian nhất: một instance server cũ còn sống ở nhánh khác ⇒ route trả 404 ⇒ hiểu nhầm thành "server hỏng".

---

## 🔴 Gate đồng thuận tài liệu — hai lượt, không gộp

**Dev phản hồi Phase doc trước; chỉ code sau khi Leader xác nhận.** Gộp hai lượt thì Dev đọc lướt rồi code luôn, mất sạch giá trị của gate. *"Đã đọc, đủ để code, không nghi ngờ"* là phản hồi hợp lệ — nhưng phải nói ra. **Im lặng không phải đồng thuận.**

### Phản hồi của Dev — ai xử lý?

| Phản hồi | Ai xử lý |
|---|---|
| Thiếu chi tiết kỹ thuật | Leader tự bổ sung (Rev x.1) |
| Tài liệu tự mâu thuẫn | Leader tự sửa — nhưng kiểm code xem bên nào đúng |
| 🔴 **Tài liệu khớp sai với hệ thống thật** — sai nghiệp vụ, sai luồng, sót consumer | **Quay lại BA/SA** ([02](./02-discovery.md)) — discovery sai từ đầu, sửa mò thì lỗi còn nguyên chỗ khác |

⚠️ Đừng quay lại BA/SA cho mọi thắc mắc vặt.

🔴 **Ghi log**: đã giao lượt 1 · tóm tắt phản hồi + link file feedback · phân loại từng ý · nếu quay lại BA/SA thì lý do + kết quả · **dòng xác nhận đồng thuận**.

---

## Review

Output của Dev là **đề xuất có bằng chứng**, không phải sự thật cuối cùng. Review tại **reviewable diff boundary** — một changeset mạch lạc sau khi Dev đã tự check. Fix cùng vòng được batch, Reviewer **re-review phần delta**.

🔴 **Bất biến**: không đánh dấu hoàn thành hoặc chuyển gate khi changeset mới nhất **chưa có Reviewer verdict `pass`**.

### Lưới kiểm tra tối thiểu (chia cho Dev và Reviewer)

1. Đối chiếu file thay đổi với phạm vi được giao.
2. So test result với baseline.
3. Tự chạy lại các check load-bearing.
4. Prompt LLM: render + gọi thật ở liều nhỏ — kiểm *đủ* lẫn *đúng*.
5. API/web: blackbox qua đường người dùng thật, verify response + database + log.
6. UI: theo [blackbox-ui-playwright](../guidelines/blackbox-ui-playwright.md) — network là bằng chứng chính cho logic chặn/cho phép, ảnh đặt tên, 4 trạng thái mockup, hàng nào mock phải ghi rõ.
7. Hardcode, fallback, shortcut chỉ để test qua.
8. Race, retry, idempotency, timeout, cleanup cho flow bất đồng bộ.
9. **Fidelity của error mapping** — broad catch biến lỗi lạ thành response bình thường là **blocking**.
10. **Tương thích client đã phát hành** — không đổi type, xoá field, đổi nghĩa.

### 🔴 Leader kiểm sau mỗi lượt worker — không tin report suông

Report của worker từng sai theo cùng một kiểu nhiều lần: chuỗi ảnh "4 khung" trùng byte, "khớp prototype" khi chưa mở ảnh, trích `file:line` tới file không tồn tại, "đã dọn 0 hàng" trong khi còn dữ liệu, "công cụ không chạy được" chỉ vì sandbox. Sau mỗi lượt, trước khi chuyển bước, Leader tự kiểm các mục khớp với lượt đó và **ghi từng mục đã kiểm vào dòng log verify**:

| Report có | Leader kiểm |
|---|---|
| Chuỗi ảnh / evidence nhiều khung | Hash từng file (`sha256sum`) — trùng hash là bằng chứng giả |
| Kết luận "khớp mockup" | Tự mở **cặp ảnh mockup ↔ ảnh chụp thật** của ít nhất 2–3 màn; brief Reviewer đòi một dòng khớp/lệch cho **mỗi** màn |
| Trích code `file:line` | Grep vài trích dẫn — sai một cái là finding về cả report |
| Dữ liệu test theo kế hoạch | Kế hoạch có bước dọn ⇒ truy vấn **riêng của Leader** theo toàn dải dữ liệu test (tiền tố id, khoảng thời gian), không theo dải trong report; không có ⇒ dữ liệu để lại đã ghi `test-accounts.md` |
| "Không chạy được / unknown" về công cụ trên máy | Chạy lại lệnh ở shell của Leader trước khi tin |
| Phase đụng tiền | Leader tự đọc diff phần ghi tiền/ledger, không chỉ dựa vào Reviewer |
| "Hiển thị đúng" một phần tử thị giác mới, nhất là phần tử nhỏ (thanh tiến độ, viền, chấm ≤ 4 px) | Report phải kèm **số đo** (`getBoundingClientRect` / computed style / mẫu pixel); Leader tự đo ít nhất một phần tử mới — ảnh chụp có thể thiếu hẳn phần tử mà cả Dev lẫn Reviewer vẫn ghi "đúng" |

### Giao Reviewer

Chi tiết: [roles/reviewer.md](../roles/reviewer.md). Leader đảm bảo:

- **Session độc lập** với Dev — lý tưởng khác platform và khác pool quota. Buộc cùng platform ⇒ **bắt buộc khác model**, ghi lý do vào log.
- Mặc định Reviewer **không chạy lại smoke** — soi code, test/build và đối chiếu evidence của Dev ([reviewer](../roles/reviewer.md)). Phase doc ghi *"Reviewer chạy lại smoke: có"* ⇒ **dữ liệu test riêng**, không sao chép fixture của Dev. Brief không tự thêm việc smoke mà Phase doc không yêu cầu.
- 🔴 **Brief re-review nêu hai thứ**: (a) cái gì đã verify vòng trước; (b) **rủi ro hồi quy của chính bản sửa**.
- 🔴 **Phase đụng tiền hoặc có BINDING của SA** ⇒ brief bắt Reviewer lập bảng **từng câu BINDING → `file:line` thực thi → ca test**. Audit theo luồng bỏ lọt câu cuối của một BINDING dài (đã lọt qua cả Dev smoke lẫn Reviewer PASS, SA Phase sau mới bắt).

🔴 **Reviewer không thực thi được phần nào ⇒ route phần đó sang session có công cụ**, không bỏ qua. Không để Reviewer trả `pass` khi mới kiểm được một nửa.

> 📗 *"Worker không có tool X"* thường chỉ đúng với **toolset mặc định**. Worker chạy được shell thì phần lớn năng lực thiếu dựng thêm được (vd lái Playwright). Trước khi kết luận platform không làm được, hỏi: *cái thiếu này cài vào được không?*

### 🔴 Verdict PASS không phải điểm dừng

- **Code tích hợp API bên thứ ba** ⇒ brief Reviewer **bắt đối chiếu docs chính thức**; Leader **tự tra docs** sau PASS, trước khi bàn giao.
- **Probe độc lập phần load-bearing** — không dùng test của Dev để verify hiểu biết của Dev. Viết script nhỏ, biệt lập, mô phỏng payload thật **theo tài liệu nhà cung cấp** (không theo code của Dev), chạy, ghi kết quả vào log, rồi **xoá script**. Script đặt trong `scripts/` của module (không trong `src/` nếu server đang watch `src/`), hoặc scratchpad của session.

### Khi Dev và Reviewer bất đồng

Leader drill-down vào diff/evidence khi: hai bên bất đồng · finding ảnh hưởng product/architecture/data/security · kết quả bất thường · lịch sử chất lượng của Reviewer chưa đủ tin. Ngoài các trường hợp đó Leader không cần đọc toàn bộ diff — nhưng Reviewer **không được thành rubber stamp**: verdict phải độc lập với self-report của Dev và trỏ tới evidence kiểm lại được.

---

## Khi nào gọi End User

**Không mặc định gọi.** Chỉ khi đợt đụng **nội dung AI** (hội thoại, cốt truyện, persona, prompt sinh ảnh) và User muốn đánh giá chất lượng trước khi tự dùng. Gọi **sau khi Reviewer `pass`** — code còn đổi thì trải nghiệm chưa có nghĩa.

🔴 Ba ràng buộc ([end-user.md](../roles/end-user.md)):

| | |
|---|---|
| **Model riêng** | Đánh giá văn là năng lực ngôn ngữ. **Không dùng lại model của Dev chỉ vì tiện** |
| **Brief phải mù** | Không đưa Phase doc, diff, tiêu chí thành công. Chỉ nói: dùng tính năng nào, ở đâu, dựng ngữ cảnh tới mức nào |
| **Đủ ngữ cảnh** | Nói rõ cần bao nhiêu lượt/dữ liệu tích luỹ |

Điểm số của End User là **đầu vào cho User**, không phải phán quyết — Leader chuyển tiếp kèm trích dẫn, không tự kết luận "chất lượng đạt".

---

## Ra khỏi công đoạn này

- [ ] Reviewer verdict `pass` trên changeset mới nhất
- [ ] Leader đã probe độc lập phần load-bearing
- [ ] Code tích hợp API ngoài: đã đối chiếu docs chính thức
- [ ] Unit test xanh, blackbox smoke đã chạy, dữ liệu test xử lý đúng kế hoạch (dọn nếu có bước dọn / đã ghi `test-accounts.md`)
- [ ] Mọi màn / route / dialog UI mới đã được render thật ít nhất một lần
- [ ] Server local của mọi vai đã tắt, port trống
- [ ] (nếu có) End User đã nộp báo cáo kèm trích dẫn

⇒ `PHASE_DONE` (ghi log). Còn Phase ⇒ quay lại [03](./03-write-docs.md) cho Phase kế. Hết Phase ⇒ [05-handover](./05-handover.md).

⚠️ Gặp thứ thoả *"nếu biết trước thì đã không gặp phải"* ⇒ ghi ngay theo [lessons](../practices/lessons.md).
