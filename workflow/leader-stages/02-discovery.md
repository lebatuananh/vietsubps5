# Công đoạn 02 — Discovery: thảo luận giải pháp với BA/SA

> **Vào đây khi**: đã có bảng 6 ô và khảo sát sơ bộ ở [01-brainstorm](./01-brainstorm.md).
> **Ra khỏi đây khi**: phiên thảo luận ≥ 2 lượt với SA xong + file discovery đạt barem trụ cột (1–6, thêm 7 khi có màn người dùng) + chỗ va chạm đã trình User ⇒ quay lại [01 bước 4](./01-brainstorm.md), rồi sang [03-write-docs](./03-write-docs.md).
>
> Cách gọi worker: [delegating-workers](../practices/delegating-workers.md). Sổ tay BA/SA: [roles/ba-sa.md](../roles/ba-sa.md).

---

## 🔴 Đây là phiên THẢO LUẬN, không phải giao khoán

- User đã ủy quyền định hướng qua 6 ô. **Leader đóng vai User đại diện** để dẫn dắt, khơi gợi và chất vấn SA.
- 🔴 **Cấm giao khoán một cục** — ném bảng 6 ô cho SA rồi bảo *"tự đi làm discovery"*. SA sẽ quá tải, bơi trong repo, báo cáo lướt vài file mà bỏ quên bức tranh tổng quan (test, tài khoản, metric, tài liệu hiện trạng, tương thích client).
- **Leader dẫn dắt từng khía cạnh** và **bắt bẻ** khi SA trả lời hời hợt.

---

## 🔴 Không có ngưỡng "việc nhỏ được miễn discovery"

**Trước khi viết bất kỳ Phase doc nào, phải có file discovery của BA/SA trong thư mục đợt.** Không ngoại lệ cho việc trông cơ học, lặp khuôn cũ, hay Leader đã tự khảo sát.

Việc càng trông cơ học, giả định càng ít được kiểm. **Càng quen việc càng dễ bỏ bước** — ngược trực giác, vì ta tưởng quen thì an toàn hơn.

---

## Cách điều phối phiên thảo luận

### 1. Brief lượt 1 — định hướng & đặt đề bài

Nạp 6 ô làm ràng buộc, nhưng **phân rã thành câu hỏi khảo sát cụ thể**:

- *Đây là nỗi đau của User (ô 1–3). Giả định ban đầu của Leader là luồng X ở module Y.*
- Yêu cầu khảo sát hiện trạng trong code thật (call-site, controller, data model).
- 🔴 **Mời phản biện**: *"The Leader believes this is low-risk and mechanical. YOUR JOB IS TO CHALLENGE THAT, not confirm it. Assume the Leader missed something."*
- Yêu cầu trả lời cụ thể về rủi ro vỡ hệ thống, dữ liệu cũ, contract client đã phát hành, prompt template, và đề xuất "dứt khoát KHÔNG làm gì".

### 🔴 1b. Mục MỞ bắt buộc trong MỌI brief

Mọi brief gửi BA/SA (lượt 1, lượt 2, thẩm định Phase doc) **bắt buộc có một mục không khoanh vùng**, đại ý:

> *"Leader đã nêu N vấn đề ở trên. Câu hỏi cuối, không có khung sẵn: **điều gì QUAN TRỌNG mà Leader chưa hỏi?** Khái niệm nào trong brief bị gộp sai hoặc đặt tên sai? Nếu một giả định nền của brief sai, nói ra — kể cả khi nó làm hỏng toàn bộ đề bài."*

⚠️ **Mời phản biện CÓ KHOANH VÙNG thì không tính.** *"Phản biện 5 giả thuyết H1–H5 của tôi"* vẫn là Leader tự khoanh vùng được phép nghi ngờ. Thứ Leader bỏ sót, theo định nghĩa, nằm **ngoài** vòng tròn đó.

> Ca thật: brief lặp một cụm từ gộp hai khái niệm khác nhau thành một. Câu *"cái gì thật sự quyết định bản ghi thuộc nhóm nào?"* không có trong brief nào ⇒ SA không đi tìm. SA báo `[verified blocker]`, Leader "verify" rồi xác nhận — **hai bên đồng thuận SAI**, suýt bắt User quyết một câu hỏi không tồn tại.

🔴 **Ba việc một câu hỏi tốt phải làm được CÙNG LÚC** — thiếu việc 3 thì đó là ra lệnh, không phải thảo luận:

| # | Đối phương phải | |
|---|---|---|
| 1 | **Nắm được vấn đề** | hiểu bài toán, không chỉ nhận task |
| 2 | **Rà soát đối chiếu** | tự đi kiểm hiện trạng trong code |
| 3 | 🔴 **Phát hiện thêm / phản biện chỗ Leader nêu thiếu** | thứ có giá trị nhất, và dễ mất nhất |

### 2. Brief lượt 2 — chất vấn & lấp barem

SA nộp phát hiện sơ bộ ⇒ **Leader KHÔNG nghiệm thu ngay.** Đối chiếu barem bên dưới và chất vấn tiếp:

- *"Hiện trạng đã rõ — phương án tối giản nhất là gì? Có over-engineering không?"*
- *"Smoke thế nào? Tài khoản nào trong `test-accounts.md`? Setup data gì? Gọi API nào, kiểm bảng/cột nào? Dữ liệu test giữ lại hay cần dọn? Reviewer có cần chạy lại smoke không?"*
- *"Có cần End User đánh giá nội dung không? Metric thành công là gì?"*
- *"Tài liệu nào trong `present/` hoặc README module sẽ sai?"*

🔴 **Cấm gộp hai lượt làm một.** Brief đóng khung sẵn rồi nhận thẳng báo cáo hoàn chỉnh là **xoá mất lượt SA được phản biện đề bài**. Kiểm bằng workflow log: phải thấy **hai dòng giao việc riêng biệt**.

---

## 🔴 Barem trụ cột của file discovery

Leader chỉ nhận file `[YYMMDD]-discovery.md` khi đã được thảo luận và phủ **đủ cả 6** — và trụ cột **7** khi đợt có màn người dùng:

| # | Trụ cột | Phải có |
|---|---|---|
| **1** | **Hiện trạng & phản biện** | Hiện trạng code thật (`file:line`, nhãn `verified`), chỗ định hướng User va vào code, danh sách "dứt khoát KHÔNG làm" |
| **2** | **Phương án kỹ thuật** | Kiến trúc tối giản tương xứng · cấu trúc dữ liệu & ràng buộc từng field (chặn ở tầng nào, bản ghi cũ ra sao) · mockup `.html` đủ 4 trạng thái nếu đụng UI |
| **3** | **Tác động hệ thống** | Tài liệu `present/` + README module cần cập nhật · rà consumer theo loại **và** theo endpoint (đặc biệt prompt template, client đã phát hành) |
| **4** | **Nghiệm thu & test 2 tầng** | 🔴 **Không được thiếu**: metric thành công · tầng 1 smoke (tài khoản, setup, API + payload, query verify, quyết định dọn dữ liệu, Reviewer có chạy lại smoke không) · tầng 2 chất lượng nội dung (có/không, lúc nào) |
| **5** | **Vertical slicing & TDD** | Chia Phase test được ngay, bám ô 5 · Phase nào có pure logic phải TDD |
| **6** | **Nhánh** | Module nào bị đụng · tên nhánh đề xuất |
| **7** | **Trải nghiệm** (đợt có màn người dùng; admin chỉ phần UX) | Người dùng nhận được gì, cảm xúc gì, so với app quen thuộc cùng loại · "khoảnh khắc chính" của màn (không chỉ 4 trạng thái kỹ thuật) · **định hướng thiết kế** (vibe, component, chuyển động — [ba-sa §Bước 6](../roles/ba-sa.md)) Leader & SA đã bàn chốt · mockup phải thể hiện khoảnh khắc đó |

---

## 🔴 Xử lý va chạm & chỉ đạo mới của User: đồng thuận nội bộ trước

1. **Cấm Leader tự bơi, tự sửa rồi hỏi lại User.** Nhận chỉ đạo mới ⇒ **không** vội sửa draft rồi hỏi *"như này ok chưa"* — biến buổi làm việc thành hỏi đáp vụn vặt và đẩy việc nghĩ ngược lên User.
2. **Leader & SA là một khối.** Chỉ đạo mới phải đưa vào thảo luận tiếp với SA: *chạy trong code thật ra sao, vướng bẫy kiến trúc/contract nào, phát sinh rủi ro gì User chưa lường?* Leader và SA **có quyền phản biện User** — nhưng phải có tiếng nói chung trước.
3. **Trình User theo khuôn đề xuất có chủ kiến**: điểm va chạm thật trong code → phương án Leader & SA khuyến nghị (lý do + đánh đổi) → nếu phản biện User thì vì sao hướng ban đầu rủi ro → User chỉ việc chốt.

---

## 🔴 Verify claim của BA/SA trước khi dùng

BA/SA sai ngay cả khi phần lớn đúng — hay gặp nhất: nhầm giữa hai module có hàm cùng tên, nhầm code sống với code chết.

- Tự verify **≥ 3 claim load-bearing**, ghi kết quả vào workflow log.
- Claim về **cơ chế** ⇒ đọc chỗ nó được **định nghĩa** ([team-leader §2b](../roles/team-leader.md)).
- Claim **sai** cũng ghi tường minh vào Phase doc — nếu không, Dev sẽ đi "sửa" thứ đang đúng.
- Claim gắn `assumption` **không được** thành acceptance criterion trước khi verify.
- Report chỉ xác nhận mà không tìm ra gì mới ⇒ khảo sát chưa đủ sâu, **mở vòng phản biện thứ hai**.

## Spike thay vì hỏi User

Unknown kỹ thuật giải được bằng spike nhỏ, không đổi hướng sản phẩm, hoàn tác an toàn ⇒ **tự tổ chức spike**, dùng kết quả làm evidence. Chỉ escalate khi kết quả spike lộ ra product fork, scope/risk đáng kể, hoặc hành động khó hoàn tác. Spike tốn tiền thật (gọi model trả phí) ⇒ nêu chi phí ước lượng trước.

---

## Ra khỏi công đoạn này

- [ ] File discovery trong thư mục đợt, phủ **đủ 6 trụ cột** (+ trụ cột 7 nếu có màn người dùng)
- [ ] Workflow log có **≥ 2 dòng giao việc BA/SA riêng biệt**, mỗi brief có mục mở
- [ ] Kế hoạch test 2 tầng đã chốt rõ
- [ ] Leader đã verify ≥ 3 claim load-bearing, ghi log
- [ ] Chỗ va chạm đã trình User và được chốt
- [ ] Danh sách "dứt khoát không làm" đã được User duyệt

⇒ Quay lại [01 bước 4](./01-brainstorm.md), rồi sang [03-write-docs](./03-write-docs.md).

⚠️ Gặp thứ thoả *"nếu biết trước thì đã không gặp phải"* ⇒ ghi ngay theo [lessons](../practices/lessons.md).
