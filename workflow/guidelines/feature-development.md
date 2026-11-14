# Feature Development Guidelines

**Mục đích**: mindset khi thêm tính năng sao cho **khớp với hệ thống đang chạy**. Đây là tài liệu về cách tiếp cận và nguyên tắc kiến trúc; pattern code cụ thể nằm trong `README.md` của từng module.

---

## 1. Hiểu hiện trạng trước khi viết

- Đọc tài liệu hiện trạng của luồng sắp đụng (bảng *Tài liệu hiện trạng* trong `CLAUDE.md`) và `README.md` của module. Mục tiêu là *thêm vào* hệ thống đang chạy, không dựng luồng song song đè lên cái cũ.
- Tìm pattern tương tự đã có và **học theo phong cách lân cận**. Coi file hiện có là bản thiết kế chuẩn để copy rồi biến đổi.
- **Tái sử dụng** helper, utility, component, service đã có trước khi viết mới.

> **Sự nhất quán quan trọng hơn sự "tối ưu" cục bộ.**

---

## 2. Thiết kế tương xứng bối cảnh (Proportional Architecture · KISS)

Không phải luồng nào cũng cần cơ chế phòng thủ cấp hệ phân tán. Phân định bối cảnh vận hành để chọn phương án **đơn giản nhất chạy tốt**:

| Luồng | Đòi hỏi |
|---|---|
| **End-user / public** (client đã phát hành, thanh toán, chat API) | Chịu lỗi cao · tương thích ngược với client cũ · transaction bền vững · retry an toàn |
| **Nội bộ / admin** (trang quản trị, tác vụ AI do admin bấm, batch script) | **Tối giản nhất**: xử lý in-memory + **một transaction commit, fail-closed**. Lỗi giữa chừng ⇒ rollback sạch, đánh dấu thất bại, admin bấm chạy lại |

🔴 **Cấm over-engineering ở luồng admin**: không bảng trung gian/staging, không durable idempotency/request hashing, không job queue, không optimistic version/khoá dòng chống ghi đồng thời. Admin chỉ vài người và thao tác **chạy lại được bằng tay** — cần chặn đua thì **disable nút ở UI lúc đang chạy là đủ**.

Câu tự hỏi khi sắp phân tích *"nếu hai người cùng bấm thì sao"*: ***"chạy lại có mất gì không?"*** Không mất gì đáng kể ⇒ bỏ qua hoàn toàn, **đừng đưa vào brief** cho worker (đưa vào là worker đào sâu và đẻ ra phương án nặng).

⚠️ Phân biệt với thứ **phải** báo User: mất một **cổng chặn nghiệp vụ** (vd chặn publish khi đang sinh dở) là **đổi ngữ nghĩa sản phẩm**, không phải chuyện tranh chấp kỹ thuật.

🔴 **Trước khi thiết kế cơ chế bảo vệ chi phí**, ghi ra **một lượt tốn bao nhiêu** (đo thật hoặc ước lượng) và **sai thì mất bao nhiêu**. Vài xu ⇒ không cơ chế, chỉ giữ chặn bấm đôi cơ bản. Finding của SA/Reviewer về chi phí cũng qua đúng câu hỏi đó trước khi Leader nhận. *(Đã gặp: Phase doc phình 150 → 330 dòng để bảo vệ một lượt AI tốn $0.00076; User gỡ hết.)*

---

## 3. Nguyên tắc quyết định: phân loại theo tính đảo ngược

Phân vân "làm ngay hay để sau":

- **Bỏ lỡ là mất vĩnh viễn** ⇒ làm ngay — metadata nguồn/thời điểm, truy vết gốc, khoá phân vùng dữ liệu.
- **Làm lại được bất cứ lúc nào** ⇒ để sau — embedding, reranker, adapter đa nguồn, auth bên thứ ba.

Và: **chưa biết thì đừng xây sẵn.** Đừng dựng hạ tầng cho mô hình kinh doanh/tính năng chưa tồn tại. Trừu tượng hoá sớm từ một ca duy nhất sẽ sai.

Riêng **metadata/nhãn dữ liệu**: mô tả nó **LÀ GÌ / TỪ ĐÂU** thì được; mô tả nó **NÓI GÌ** thì không. Nói được câu truy vấn cụ thể mà trường đó phục vụ, hoặc bỏ.

---

## 4. Theo đúng kiến trúc phân tầng của module

Mỗi module backend có luồng chuẩn riêng (thường **Controller → Service → Repository → DB**) mô tả trong `README.md` của nó. Controller mỏng (routing, auth, parse/validate), service dày (nghiệp vụ), logic thuần tách ra để test ([tdd](./tdd.md)).

🔴 **Không tự chế shape response/error.** Theo chuẩn có sẵn của module (error model nội bộ) hoặc chuẩn ngoài đã cam kết — **kiểm chứng từ nguồn** (code thật, docs chính thức), không khẳng định từ trí nhớ.

---

## 5. Phát triển tăng dần (Vertical Slicing)

Mỗi bước ra **thành phẩm test được ngay** trên UI/API từ đầu đến cuối — không big bang (hết DB → hết API → hết UI rồi mới ráp). Cắt nhỏ thành các lát dọc kiểm chứng độc lập.

> **Lát nhỏ nhất mà vẫn ra được thứ User dùng thử và đánh giá được** — không phải lát lớn nhất mà vẫn an toàn về kỹ thuật. Gộp nhiều thay đổi "cho tiện vì cùng một module" là tối ưu cho người viết code, không phải cho người phải nghiệm thu.

---

## 6. Tôn trọng chiều thay đổi

- **Thêm/mở rộng**: trong ra ngoài (producer trước consumer).
- **Xoá/thu hẹp**: ngoài vào trong (consumer trước producer), `DROP COLUMN` luôn cuối cùng.

Chi tiết: [safe-refactoring](./safe-refactoring.md).

---

## 7. AI sinh nội dung để tạo thực thể ⇒ tách preview và commit

Luồng client ra lệnh, AI sinh nội dung rồi lưu thành thực thể (hồ sơ nhân vật, kịch bản, bài viết…) ⇒ nếu làm được, **hai endpoint**:

1. **Preview** — AI sinh bản xem trước, **không ghi bảng nghiệp vụ**. Chỉ tốn tiền AI.
2. **Commit** — từ bản preview tạo dữ liệu thật; idempotent.

Lợi ích: test chất lượng AI rẻ và sạch (gọi preview → đọc → sửa prompt → gọi lại, không có dữ liệu rác); dễ chèn bước xem/tinh chỉnh về sau (admin sửa preview rồi submit; người dùng review trước khi bắt đầu) mà không đổi kiến trúc. Giá: gọi API hai lần, rủi ro lỗi tăng chút ít — chấp nhận.

Giữ **đơn giản** (không phải hệ thống giao dịch ngân hàng — User đã bác các chặn thêm của Leader):
- Commit là một `POST` bình thường: client lấy dữ liệu preview **điền vào body** rồi gọi. Không `previewId`, không lưu preview phía server, không chống gian lận riêng cho bước này; body được validate như mọi `POST`.
- Trừ lượt/phí (nếu có) ở **commit** — người dùng chỉ cần biết bấm tạo thì bị trừ.
- Chưa cần bước review ⇒ client gọi preview rồi gọi `POST` liền, không thêm màn; chỗ cho bước review đã sẵn.

---

## 8. Sửa prompt / nội dung AI ⇒ lab trước, ốp vào code sau

Chất lượng nội dung AI không suy ra được từ đọc prompt, và mỗi vòng sửa code rồi mới thử là một vòng "sửa mù".

1. **Lab**: script gọi thẳng model bằng template mới (ngoài code sản phẩm), chạy một lô mẫu nhỏ, User/Leader chấm.
2. Đạt ⇒ chốt bản prompt đã chấm (lưu file + hash) ⇒ Phase doc trỏ tới đúng bản đó ⇒ mới cho Dev ốp vào code.
3. Chữ và ảnh tách lab riêng — sửa chung một lượt không biết cái nào làm hỏng.
4. **Lab tương xứng**: một lô nhỏ, Leader/User đọc tay; không dựng matcher/rubric chấm tự động, không chạy lại cả lô nhiều vòng khi câu hỏi cốt lõi đã có lời đáp. Còn lệch nhỏ ⇒ ghi nợ rồi ốp code. *(Đã gặp: lab một tính năng cỡ vừa kéo tới Phase doc Rev 2.0, năm lô, đốt hơn 10% quota tuần của Leader.)*

Bẫy đã gặp khi viết prompt:
- **Câu mẫu trong prompt bị chép khuôn** — model lặp lại cấu trúc, thậm chí nguyên câu; kể cả câu ví dụ định lượng và danh sách ví dụ thay thế. Mô tả tiêu chí theo trục + nêu tên mặc định cần tránh, không đưa câu mẫu/danh sách ví dụ.
- **Lô sinh độc lập (mỗi call một mục) dồn về khuôn mới sau mỗi lần sửa** — cấm X thì model chọn hướng thay thế phổ biến nhất cho mọi mục. Mỗi vòng lab đếm tần suất cụm/khung trội của **toàn lô** (không chỉ tiêu chí cũ); các vòng chỉ đổi khuôn ⇒ dừng, chọn vòng tốt nhất, ghi lệch còn lại vào nợ.
- **Tiêu chí thẩm mỹ của ảnh** (đẹp, cuốn hút) không chấm được trên prompt chữ ⇒ lab prompt ảnh phải sinh ảnh thật ở liều nhỏ trước khi trình User.
- **Thẻ mẫu trình User** render bằng script từ file kết quả, không để worker chép tay (worker bịa tên/chi tiết); Leader đối chiếu từng trường ≥ 1 mẫu trước khi gửi.
- **Image-to-image dán theo nền và dáng của ảnh vào** ⇒ ảnh reference càng trơn (phông studio, không đạo cụ, dáng trung tính) càng dễ ra ảnh mới khác bố cục.
- Field bắt buộc kiểu chuỗi trong schema structured output **vẫn có thể trả chuỗi rỗng** — code đọc phải chịu được.

Hợp đồng nội dung (AI sinh những field gì, mỗi field trông thế nào) đưa User đọc **sớm** — ở brainstorm/discovery, không đợi mockup.

---

## Khi nào dùng tài liệu nào

| Tình huống | Đọc |
|---|---|
| Mindset & nguyên tắc kiến trúc | File này |
| Luồng nghiệp vụ hiện tại | `project-docs/present/` |
| Pattern code của một module | `README.md` của module |
| Unit test / logic thuần | [tdd](./tdd.md) |
| Test qua API/UI thật | [blackbox-testing](./blackbox-testing.md) · [automation](./automation.md) |
| Xoá/refactor an toàn | [safe-refactoring](./safe-refactoring.md) |
| Viết/sửa prompt LLM | [prompt-llm](./prompt-llm.md) |
