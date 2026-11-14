# Công đoạn 01 — Brainstorm: tiếp nhận yêu cầu từ User

> **Vào đây khi**: User vừa nêu một nhu cầu/ý tưởng mới cho một đợt update.
> **Ra khỏi đây khi**: đủ **6 ô** + draft + workflow log đã tạo + đã khảo sát sơ bộ ⇒ sang [02-discovery](./02-discovery.md), rồi quay lại bước 4.
>
> **Không phụ thuộc platform.** Claude Code có skill `/brainstorm` trỏ vào file này; agent khác đọc thẳng file này.

---

## ⚠️ Nguyên tắc tối thượng: CHỈ HỎI 1 CÂU MỖI LẦN

Câu trả lời trước thường đổi ngữ cảnh của câu sau:

1. Đặt **1** câu hỏi → 2. Đợi User trả lời → 3. Làm rõ nếu cần → 4. Câu tiếp theo.

Hỏi gộp thì User trả lời gộp, và Leader hiểu nông. Hỏi bằng **text thường**, không dùng UI chọn đáp án.

### 🔴 Nạp bối cảnh trước khi hỏi (pre-fill)

- Không bắt đầu như tờ giấy trắng. Trước câu hỏi đầu tiên, **đọc lại** `CLAUDE.md`, overview/epic của đợt liền trước, và các quyết định đã có.
- Ô nào đã có dữ liệu hoặc User đã nói rõ ⇒ **tự điền vào bảng 6 ô, kèm trích dẫn nguồn**, không hỏi lại.
- Chỉ hỏi ô thật sự thiếu, mơ hồ, hoặc có mâu thuẫn mới phát sinh.
- Không biến buổi brainstorm thành **"hỏi cung theo checklist máy móc"**.

---

## Bước 1: Làm rõ nhu cầu — 🔴 Sáu ô BẮT BUỘC

**Không đếm số câu hỏi — đếm ô đã đầy.** Đây là những thứ **chỉ User trả lời được**, không suy ra được từ code, tài liệu hay trí nhớ:

| # | Ô | Không có thì hỏng ra sao |
|---|---|---|
| 1 | **Vì sao bây giờ** — điều gì đang đau, ai đang chịu | Không biết cái đau thì mọi lát cắt đều là phỏng đoán |
| 2 | **Sửa cái gì** — phạm vi, ranh giới tới đâu | AI tự suy từ code ⇒ ra phạm vi của AI, không phải của User |
| 3 | **Sửa đến đâu là đủ** — mức "đủ tốt" cho đợt này | Làm quá (tốn) hoặc làm thiếu (không dùng được) |
| 4 | **Thứ tự** — cái gì trước, vì sao trước | Làm đúng nhưng sai thứ tự ⇒ không có phần nào deploy được |
| 5 | **Mốc deploy dev** — xong cái nào thì đẩy lên dev được | Không biết thì mọi Phase đều nửa vời |
| 6 | **Đánh đổi chấp nhận được** — nhanh hay chắc; tạm chịu lệch chuẩn hay làm cho đúng | AI tự chọn, và thường chọn thứ nó thấy "sạch" |

🔴 **Ghi mỗi ô bằng LỜI CỦA USER, có trích dẫn** — không phải lời AI tóm tắt. Ô nào User chưa nói thì để trống và **hỏi tiếp**.

🔴 **Không hỏi đóng để lấp ô cho nhanh.** Hỏi *"ưu tiên là X phải không?"* rồi User ừ — ô đó vẫn rỗng, vì nó chứa giả định của AI. Hỏi mở: *"trong mấy thứ này, cái nào ra trước thì đỡ nhất?"*

🔴 **Từ chuyên môn trong lời User quyết định phạm vi lớn ⇒ hỏi lại đúng nghĩa trước khi giao discovery.** Lời mô tả hiện tượng (vd *"được thưởng mà không cần xem hết quảng cáo"*) không tự động là tiêu chí nghiệm thu. Nếu một từ trong đó kéo theo việc tốn kém (vá SDK bên thứ ba, đổi hợp đồng, đổi kinh tế) thì hỏi một câu xác nhận nghĩa (*"'xem hết' là hết video, hay tới lúc nhà cung cấp cho đóng?"*) **trước** khi giao BA/SA khảo sát sâu theo nghĩa đó.

⚠️ **Chưa đủ 6 ô mà sang Bước 2 là làm sai quy trình.** Lỗi hay gặp nhất: hỏi vài câu, thấy "hiểu rồi", đi tiếp — trong khi thứ quyết định *làm gì, làm thế nào, chất lượng bao nhiêu là đủ* nằm ở ô 3, 4, 5.

> **KHÔNG hỏi User** *"đợt này dứt khoát không làm gì?"* — đó là **đầu ra của khảo sát** ([02](./02-discovery.md)), AI đề xuất sau khi biết hiện trạng rồi User duyệt.

### Ranh giới: hỏi ai cái gì

| Hỏi User | Hỏi code / tài liệu / test |
|---|---|
| Mục đích · ưu tiên · mức đủ · thứ tự · mốc deploy · đánh đổi | Hiện trạng là gì · cái gì đang có · cái gì sẽ vỡ |

🔴 Thiếu cột trái ⇒ **hỏi User**, đừng suy từ code. Thiếu cột phải ⇒ **đi đọc code**, đừng hỏi User. Thứ tự code *trong* một Phase, hệ quả của thứ đã chốt ⇒ **tự quyết** ([team-leader §4](../roles/team-leader.md)).

🔴 **Đọc `CLAUDE.md` trước khi đề xuất phạm vi** — mục *Dự án* (sản phẩm, người dùng, kênh phát hành) và *Ràng buộc đặc thù* quyết định cái gì làm đúng kỹ thuật nhưng vẫn sai hướng. Tiến trình (các đợt trước, đợt nào đang mở): `project-docs/overview-project.md`.

---

## Bước 2: Khởi tạo thư mục đợt, draft, workflow log

🔴 **Tạo ngay sau khi chốt 6 ô** — không đợi tới kickoff. Từ mốc này mọi thảo luận với SA, mọi quyết định của User đều cần có vết.

1. **Thư mục đợt**: `project-docs/[YYMMDD]-[chủ-đề]/` (`YYMMDD` = ngày bắt đầu đợt, `chủ-đề` tiếng Anh, gạch nối).
2. **Draft**: `[YYMMDD]-draft.md` theo [templates/draft.md](../templates/draft.md) — ghi **bảng 6 ô kèm trích dẫn**. **Chưa kết luận gì về giải pháp.**
3. **Workflow log**: `[YYMMDD]-workflow-log.md` theo [templates/workflow-log-header.md](../templates/workflow-log-header.md). Dòng mở màn bằng **giờ hệ thống thật** ([workflow-log](../practices/workflow-log.md)): mốc chốt 6 ô, link draft.
4. Thêm một dòng vào `project-docs/overview-project.md` (danh sách các đợt).

---

## Bước 3: Khảo sát sơ bộ & chuyển sang discovery với SA

- Hỏi User có chức năng nào liên quan cần tham khảo không — **nhưng dù User trả lời thế nào, vẫn tự khảo sát** (tìm, đọc file) để verify.
- Luồng đang đụng có tài liệu trong `project-docs/present/` hoặc README module ⇒ **đọc trọn**.
- Chốt được file/pattern liên quan ⇒ note vào draft.

🔴 **Không giao khoán một chiều — mở phiên thảo luận có dẫn dắt với SA.** Sau khi có 6 ô, **User không còn phải trả lời hàng chục câu hỏi chi tiết**. Leader thay mặt User mang 6 ô đi **thảo luận, khơi gợi và chất vấn SA** qua từng trụ cột. Chi tiết: [02-discovery](./02-discovery.md).

---

## Bước 4: Thống nhất phương án & nghiệm thu (sau discovery)

Từ kết quả thảo luận với SA, Leader cập nhật draft:

1. **Phương án kỹ thuật**: kiến trúc, thư viện, dữ liệu, luồng xử lý — 🔴 theo [thiết kế tương xứng](../guidelines/feature-development.md): bắt đầu từ phương án tối giản nhất vận hành được.
2. **Nghiệm thu & test 2 tầng**:
   - **Metric hoàn thành** — tiêu chí nào chứng minh chạy đúng bài toán của User.
   - **Tầng 1 — Blackbox smoke** (Phase có API/web): tài khoản nào, setup gì, gọi gì, verify bảng/cột nào, dữ liệu test giữ lại hay cần dọn.
   - **Tầng 2 — Chất lượng nội dung**: có cần End User không, vào mốc nào.
3. **Kiểm với mục *Ràng buộc đặc thù* và *Risk gate riêng* của `CLAUDE.md`**: phương án có chạm vùng nào không (contract client đã phát hành, schema DB, prompt template, chi phí AI…)?
4. Có điểm va chạm hoặc rủi ro lớn ⇒ trình User theo khuôn **đề xuất có chủ kiến** ([team-leader §4](../roles/team-leader.md)).

---

## Bước 5: Tác động đến tài liệu hệ thống

- **Tự rà** `project-docs/present/` và README các module bị đụng: tài liệu nào sẽ sai sau đợt này?
- Có ⇒ note *"Update tài liệu [tên]"* vào draft để kế hoạch có bước cập nhật.
- Phát hiện nợ kỹ thuật mới ⇒ note để bổ sung `present/backlog-and-techdebt.md`.

---

## Bước 6: Chốt lộ trình Epic / Phase theo vertical slicing

🔴 **Lộ trình bám ô 4 và ô 5**, không bám thứ tự kỹ thuật thấy thuận tay:

- **Ô 4** (thứ tự User muốn) quyết định Phase nào trước.
- **Ô 5** (mốc deploy dev) quyết định **ranh giới Phase** — cắt sao cho có Phase đẩy được lên dev.

Chia xong ⇒ **đối chiếu ngược**: mỗi Phase nói rõ *xong thì deploy dev được chưa*. Không Phase nào deploy được ⇒ cắt sai, chia lại.

- **Vertical slicing**: làm đến đâu, ra thành phẩm test được ngay trên UI/API đến đó. TUYỆT ĐỐI KHÔNG big bang (hết DB → hết backend → hết UI).
- **Chỉ chốt ở mức tổng quan**: Epic làm luồng gì, chia Phase nào. **Không** đi vào chi tiết code.
- Phase có business logic thuần ⇒ note *"áp dụng TDD"*.
- Phase có API/web ⇒ note sơ bộ kế hoạch smoke; **mỗi vai tự chuẩn bị tài khoản/dữ liệu riêng**.
- Đợt đụng nội dung AI ⇒ chốt có cần End User không.
- Yêu cầu cập nhật tài liệu ở Bước 5 nằm trong Phase phù hợp.

---

## Bước 7: Nhánh code

🔴 **Mỗi module là một repo riêng** — một đợt dùng **một tên nhánh**, tạo ở **từng repo bị đụng** ([guidelines/git.md](../guidelines/git.md)). `project-docs` không tách nhánh.

Chạy skill **`/prepare-code-branch`**: lấy danh sách repo bị đụng từ Epic/Phase đã chốt ở Bước 6 → khảo sát trạng thái từng repo → đề xuất tên nhánh `<features|enhancements|bugs>/<YYMMDD>-<slug>` (đúng `YYMMDD` của thư mục đợt) → đưa khối lệnh cho User chạy (🔴 AI không tự tạo nhánh trừ khi được ủy quyền) → verify từng repo → ghi tên nhánh + commit gốc từng repo vào draft.

---

## Ra khỏi công đoạn này

- [ ] Đủ **6 ô**, mỗi ô có trích dẫn lời User
- [ ] Draft + workflow log đã tạo, dòng log mở màn có giờ thật
- [ ] Đã qua [02-discovery](./02-discovery.md), chỗ va chạm đã trình User và được chốt
- [ ] Draft có: phương án · kế hoạch test 2 tầng · Epic/Phase · tài liệu cần cập nhật · nhánh
- [ ] Nhánh của đợt đã tạo, working tree sạch

⇒ Sang [03-write-docs](./03-write-docs.md).

⚠️ Gặp thứ thoả *"nếu biết trước thì đã không gặp phải"* ⇒ ghi ngay theo [lessons](../practices/lessons.md).
