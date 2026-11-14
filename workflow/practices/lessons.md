# Ghi lesson — kỹ năng dùng ở mọi công đoạn

> **Không phải một công đoạn.** Lesson có thể xuất hiện ở bất kỳ đâu. Ghi **ngay khi gặp**.
>
> Việc *chuyển lesson thành rule* có mốc rõ — một lần khi đóng đợt: [06-close §4](../leader-stages/06-close.md).

---

## File và người ghi

Mỗi đợt có **`[YYMMDD]-lessons.md`** trong thư mục đợt (khuôn: [templates/lessons.md](../templates/lessons.md)), link từ overview.

🔴 **Leader ghi. Không ai khác.** Worker phát hiện thứ đáng ghi ⇒ báo trong handoff capsule, Leader chắt lọc và ghi. Một người ghi thì file mới nhất quán.

---

## Tiêu chí duy nhất: "nếu biết trước thì đã không gặp phải"

Thoả tiêu chí đó là đáng ghi — **không giới hạn loại**:

| Loại | Ví dụ |
|---|---|
| **Quy trình** | Gate không tạo giá trị · bước bị bỏ và hậu quả · thứ tự sai |
| **Cách gọi agent** | Cờ CLI đúng/sai · brief thế nào thì worker hiểu · ký tự làm CLI hiểu nhầm |
| **Từng platform, từng model** | Platform nào hợp vai nào · model nào đủ/không đủ cho loại task nào · quota cạn giữa chừng |
| **Lỗi gặp phải** | Hiện tượng · nguyên nhân thật · cách khắc phục |
| **Bẫy môi trường / stack** | Build hỏng vì lý do không liên quan code · process con không chết · biến build-time |
| **Hành vi của chính Leader** | Tự cho mình ngoại lệ · tin worker quá sớm · nghi oan worker |

Không phân biệt "đáng kể" hay "vặt". Một dòng về cờ CLI sai làm mất 2 lượt Dev đáng ghi ngang một sự cố production.

---

## 🔴 Ghi ngay khi gặp — rút kinh nghiệm thì để cuối đợt

| | Khi nào | Làm gì |
|---|---|---|
| **Ghi lesson** | Ngay khi vừa gỡ xong, ở **bất kỳ công đoạn nào** | Chép vào file lessons |
| **Rút kinh nghiệm** | **Một lần, khi đóng đợt** | Đọc lại cả file, chọn cái thành rule, phân loại đi đâu |

Dồn tới cuối thì chi tiết đã mờ, evidence đã trôi khỏi context. Nhưng **đừng vừa ghi vừa vội nâng thành rule** — đang làm thì chưa biết quan sát là đặc thù lượt đó hay lặp lại.

---

## Khuôn một lesson

```markdown
### <Tiêu đề — nêu thẳng vấn đề>

**Bối cảnh**: đang làm gì thì gặp.
**Quan sát**: hiện tượng thật, nguyên văn thông báo lỗi nếu có.
**Nguyên nhân**: cái gì thật sự gây ra — không phải phỏng đoán đầu tiên.
**Cách xử lý**: đã làm gì để gỡ, kèm lệnh/evidence.
**Phạm vi**: quy trình · dự án · module `<tên>` — và có đúng với mọi dự án dùng template không?
⇒ **Nếu biết trước**: một câu — lần sau làm gì thì không gặp lại.
```

Dòng cuối có giá trị nhất; thiếu nó thì lesson chỉ là nhật ký sự cố. Dòng **Phạm vi** giúp lúc đóng đợt phân loại nhanh — lesson đi về đâu (xem bảng hai trục ở [06-close §4](../leader-stages/06-close.md)).

> **Nguyên nhân phải là nguyên nhân thật.** Phỏng đoán đầu tiên thường sai — ghi phỏng đoán vào thì lần sau đi nhầm hướng, tệ hơn không ghi gì.

---

## Khác gì với workflow log

| | workflow log | lessons |
|---|---|---|
| Nội dung | *Lúc nào làm gì, kết quả ra sao* | *Điều gì đáng lẽ nên biết trước* |
| Đọc để | Biết đợt đã diễn ra thế nào | Không lặp lại sai lầm |

Một sự kiện có thể vào cả hai: log ghi *"worker treo 45 phút, phải dừng"*; lessons ghi *vì sao treo mà không ai biết, lần sau làm gì*.
