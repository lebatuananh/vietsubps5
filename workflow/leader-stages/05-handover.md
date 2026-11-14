# Công đoạn 05 — Chuẩn bị gói bàn giao

> **Vào đây khi**: các Phase của đợt đã `PHASE_DONE` — Leader kết luận được **bản local đã ngon**.
> **Ra khỏi đây khi**: gói bàn giao đầy đủ đã trình User. Đây là **điểm dừng cuối cùng** của Leader trong luồng code.

---

## 🔴 Leader dừng ở bản local — không bao giờ chạm merge/deploy

Trách nhiệm của Leader kết thúc ở thời điểm **kết luận được bản local đã chạy đúng**. Mọi việc sau đó — commit/push (trừ khi được ủy quyền rõ), merge `dev`, chạy SQL, deploy môi trường dev, lên `main`, deploy production — thuộc **dev phụ trách** (xem `CLAUDE.md` mục *Bàn giao*).

- Không merge, không deploy, không đụng hạ tầng (cluster, image, pipeline) — kể cả khi có quyền kỹ thuật.
- Không chạy SQL trên DB nào ngoài DB dev local đã ghi ở `CLAUDE.md` mục *Môi trường*.
- Không "theo dõi giúp" sau khi bàn giao. Dev báo lỗi về ⇒ quay lại [04](./04-implement.md) trên chính nhánh đó.

Gói bàn giao ghi vào `[YYMMDD]-handover.md` trong thư mục đợt. Viết cho **dev phụ trách** đọc — người không theo đợt này từ đầu.

---

## Checklist gói bàn giao

### A. Code & changeset — theo từng repo
- [ ] Bảng repo bị đụng: repo · nhánh · commit đầu/cuối của đợt · tóm tắt thay đổi.
- [ ] `git -C <repo> status --short` sạch ở **mọi** repo bị đụng — không file thừa, rác, log, script đo tạm. Repo nào chưa commit (User chưa ủy quyền commit) ⇒ ghi rõ "chờ dev commit", liệt kê file.
- [ ] Build sạch ở mọi module bị đụng.
- [ ] Repo **không** bị đụng nhưng dễ tưởng là có ⇒ ghi một dòng "không đổi".

### B. Bằng chứng kiểm thử nội bộ
- [ ] Unit test pass — số case, baseline trước/sau.
- [ ] Blackbox smoke trên local đã chạy và pass luồng chính.
- [ ] Reviewer verdict `pass` + Leader probe.
- [ ] Dữ liệu test xử lý đúng kế hoạch: Phase có bước dọn ⇒ đã dọn; không ⇒ đã ghi vào `test-accounts.md`.

### C. Database (nếu có)
- [ ] Toàn bộ SQL trong **một file** `[YYMMDD].sql` ở thư mục đợt, comment phân tách theo Phase.
- [ ] Ghi rõ **thứ tự so với việc cập nhật image**: thêm cột/bảng ⇒ chạy *trước* khi cập nhật image · xoá cột ⇒ cập nhật image *trước*, `DROP` *sau* ([safe-refactoring](../guidelines/safe-refactoring.md)).
- [ ] SQL đã apply trên DB dev local hay chưa.
- [ ] 🔴 File SQL **chạy được theo mốc**: mỗi phần có header `-- === Phase X: <việc> ===` và kết thúc bằng **query đọc lại** (định nghĩa cột/index/constraint, số dòng bị ảnh hưởng) kèm kết quả mong đợi. Dev chạy từng phần từ header tới hết, không trích theo số dòng đếm tay — cắt hụt một dòng có thể bỏ mất mệnh đề cuối (`WHERE …`) mà câu lệnh vẫn chạy thành công, chỉ sai nghĩa; query đọc lại là chỗ dev bắt được điều đó.
- 🔴 SQL trên dev/production **do dev tự chạy** — gói bàn giao chỉ đưa script, thứ tự và query đọc lại.

### D. Biến môi trường (nếu có)
- [ ] Tên biến · repo/module nào · mục đích · giá trị local (đã có trong `appsettings.Development.json`). 🔴 **Không ghi secret production** — dev phụ trách tự điền ở môi trường.
- [ ] `appsettings.Development.json` (giá trị local, commit chung) và `appsettings.json` (key mặc định) của module đã cập nhật cùng bộ biến.
- [ ] Biến build-time (vd `VITE_*`) ⇒ ghi rõ phải truyền lúc build image, đổi giá trị phải build lại.

### E. Client đã phát hành (nếu có)
- [ ] Có cần phát hành lại client (mobile app…)? Người dùng bản cũ bị ảnh hưởng gì? Thứ tự giữa phát hành client và cập nhật backend.

### F. 🔴 Hướng dẫn test manual — BẮT BUỘC
Viết cho **tester** (người không đọc code), test trên môi trường dev sau khi dev deploy:
- [ ] **Kịch bản nghiệp vụ chính**: từng bước thao tác · kết quả mong đợi nhìn thấy được.
- [ ] **Dữ liệu kiểm thử**: tài khoản, cách dựng trạng thái đầu (theo `project-docs/automation/test-accounts.md`).
- [ ] **Ca biên / ca lỗi** đáng thử — thứ blackbox local không phủ được (máy thật, mạng chậm, dữ liệu thật).
- [ ] **Tiêu chí đạt**: điều kiện để coi là đạt.
- [ ] Chỗ **đã biết là chưa làm** — để tester không báo nhầm thành bug.

### G. Known limitations & nợ kỹ thuật
- [ ] Hạn chế đã biết của đợt.
- [ ] Nợ kỹ thuật / backlog mới (việc User chốt làm sau) ⇒ đã ghi `present/backlog-and-techdebt.md`.
- [ ] Cấu hình tạm (vd bật `LOG_LEVEL=debug` để đo) và thời điểm phải gỡ.

---

## Khuôn báo cáo bàn giao

```markdown
# Bàn giao — <đợt>

## 1. Kết quả
- Phase đã xong: …
- Kiểm thử nội bộ: unit X/X · blackbox local PASS · Reviewer PASS · Leader probe PASS

## 2. Repo & nhánh
| Repo | Nhánh | Commit | Thay đổi | Trạng thái |
|---|---|---|---|---|
| api-service | `features/YYMMDD-slug` | `abc123..def456` | … | đã commit / chờ dev commit |

## 3. Việc của dev trước/khi deploy
- Database: có/không — `[YYMMDD].sql` · chạy trước/sau khi cập nhật image
- Env: có/không — `TÊN_BIẾN` ở repo `…`
- Client: có/không phải phát hành lại

## 4. Hướng dẫn test manual (cho tester)
- Kịch bản 1: bước … → thấy …
- Tài khoản / dữ liệu: …
- Ca biên: …
- Chưa làm (đừng báo bug): …

## 5. Known limitations
- …
```

---

## Ra khỏi công đoạn này

Gửi báo cáo ⇒ **dừng**. Leader không hỏi "khi nào deploy", không đề nghị deploy giúp.

| Sau đó | Leader làm |
|---|---|
| Dev/tester báo lỗi | Quay lại [04](./04-implement.md) trên chính nhánh đó, bàn giao lại phần đổi |
| User chỉ định đóng đợt | [06-close](./06-close.md) |
| Im lặng | Không làm gì. Đợt ở trạng thái `HANDED_OVER` |
