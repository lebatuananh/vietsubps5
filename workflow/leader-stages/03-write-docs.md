# Công đoạn 03 — Viết tài liệu triển khai

> **Vào đây khi**: hướng đã chốt ở [01](./01-brainstorm.md) + discovery xong ở [02](./02-discovery.md).
> **Ra khỏi đây khi**: SA verdict PASS trên Phase doc + User đã nhận tóm tắt ⇒ sang [04-implement](./04-implement.md).
>
> Claude Code có skill `/write-implementation-docs` trỏ vào file này.

---

## ⚠️ Ba nguyên tắc tối thượng

1. **Cuốn chiếu** — không viết trước toàn bộ Phase/Epic. Chỉ viết cái sắp làm.
2. **Chỉ viết tài liệu, không code** — trừ khi User nói rõ "code đi".
3. **Không biến tài liệu thành code** — Phase doc chi tiết từng bước, nhưng **không** chép sẵn mã nguồn hoàn chỉnh.

---

## Chọn cấu trúc

| Mức độ | Dùng khi |
|---|---|
| **Draft-only** | Thay đổi vặt < 50 dòng — hotfix, typo |
| **Single document** `[YYMMDD]-implementation.md` | Cải tiến gọn, 1–2 module, < 250 dòng |
| **3 cấp Overview → Epic → Phase** | Feature phức tạp, nhiều Epic, xuyên module |

### Vị trí & tên file — tất cả trong thư mục đợt `project-docs/[YYMMDD]-[chủ-đề]/`

| File | Tên |
|---|---|
| Overview | `[YYMMDD]-overview.md` — tài liệu sống: vấn đề, giải pháp tổng quát, danh sách Epic, nhánh |
| Epic | `[YYMMDD]-epic[X].md` — vấn đề của Epic, **Định hướng User đã chốt**, danh sách Phase |
| Phase | `[YYMMDD]-epic[X]-phase[Y].md` — chi tiết nhất, cho thi công |
| SQL của đợt | `[YYMMDD].sql` — **một file duy nhất cả đợt**, mỗi Phase append phần của mình (có comment phân tách) |
| Mockup | `mockups/[YYMMDD]-mockup-<màn-hình>.html` |
| Báo cáo quanh một Phase | `reviews/[YYMMDD]-epic[X]-phase[Y]-{sa-review,dev-feedback,dev-report,review}.md` |
| Bằng chứng smoke/playtest | `playtest/<epic-phase>/<vai>/` |
| Script thử nghiệm (smoke, harness, lab) | `playtest/<epic-phase>/<vai>/` — **gitignore**, xem dưới |
| Brief · script · stream của worker | `_workers/{briefs,runs,streams}/` — xem [delegating-workers](../practices/delegating-workers.md) |

🔴 Root thư mục đợt chỉ chứa tài liệu chính (draft, discovery, overview, epic, phase, SQL, log, lessons, sessions, handover); mọi thứ khác vào thư mục con ở bảng trên.

### 🔴 Script thử nghiệm đặt ở đâu

Chia theo **vòng đời** của script — script một lượt không được lẫn vào code module hay git.

| Loại script | Vị trí | Git |
|---|---|---|
| Script của một đợt (smoke, lab, dựng dữ liệu, kiểm SQL) | `project-docs/[đợt]/playtest/<epic-phase>/<vai>/` | **Không** commit (`project-docs/.gitignore` bỏ `.js .mjs .cjs .ts .ps1 .sh .py` trong `playtest/`). Kết quả, report, ảnh vẫn commit |
| Script của một đợt buộc phải nằm cạnh code module (import `src/`, cần `node_modules`/`tsconfig` của module) | `<module>/scripts/` | **Không** commit (`.gitignore` **của repo module** bỏ `scripts/`, mở lại đích danh script build/vận hành có commit). `tsconfig` của module không được nạp thư mục này |
| Harness dùng lâu dài của một module (danh tính giả lập, SDK giả cho Playwright) | `<module>/playtest/` | **Commit** (repo module), kèm README. Chỉ chứa harness — không chứa script của đợt |
| Script dùng lại được qua nhiều đợt | `project-docs/automation/scripts/` | **Commit** (repo `project-docs`). Không secret, không ID cứng — nhận qua env/tham số. Đưa lên ở bước [06-close](./06-close.md) |

Không rải script ở root module hay chỗ khác. Phase doc ghi đường dẫn script theo bảng này.

Template: [overview](../templates/overview.md) · [epic](../templates/epic.md) · [phase](../templates/phase.md).

### Trình tự cuốn chiếu

1. Viết Overview + Epic 1 + Phase 1 (+ header file SQL nếu đụng DB) ⇒ SA gate ⇒ tóm tắt cho User ⇒ kickoff.
2. Xong Phase 1 ⇒ mới viết Phase 2 **dựa trên kết quả thật vừa có** ⇒ link vào Epic ⇒ SA gate ⇒ thi công.
3. Xong Epic 1 ⇒ mới viết Epic 2 + Phase 1 của nó ⇒ link vào Overview ⇒ lặp.

📘 Được viết Phase doc kế tiếp **song song** trong lúc Reviewer đang soi Phase trước — miễn Phase sau không phụ thuộc kết quả review.

---

## Ràng buộc khi viết Phase doc

Phase doc phải đủ để **một AI khác hoặc một Dev mới hoàn toàn** đọc và code đúng.

### 1. Vertical slicing bám ô 4 và ô 5

Mỗi Phase ra thành phẩm test được ngay trên UI/API. Thứ tự Phase theo ô 4; **ranh giới Phase** theo ô 5. Không chia ngang (Phase 1 hết DB, Phase 2 hết API…).

### 2. 🔴 "Files bắt buộc đọc trước khi code"

Phase có unit test / integration test / business logic ⇒ **liệt kê từng file kèm lý do cụ thể**: *"đọc X để biết field nào method truy cập"*, *"đọc Y để biết shape return cần mock"*, *"đọc Z để biết biến của template"*. Thiếu section này ⇒ worker đoán cấu trúc mock và mất hàng chục vòng sửa test.

### 3. ⛔ Cấm kế hoạch test nước đôi

Không được viết *"nếu tách hàm thì unit test, nếu để inline thì dựa vào smoke"*. Phase có pure logic ⇒ Leader **chốt**: *"extract thành pure function X + unit test các case (gồm case biên)"*. Chính câu nước đôi là cách bug case biên lọt tới blackbox.

### 4. 🔴 Blackbox verify — quy trình tư duy tường minh

Tránh hai thái cực: ❌ chung chung *"smoke qua API thật"* · ❌ cầm tay userId/data cụ thể (sai vai, nhanh lỗi thời). ✅ Vạch các bước:

1. **Chuẩn bị (vai tự làm)**: tra `project-docs/automation/test-accounts.md`, inspect hiện trạng, thiếu thì dựng. Mô tả **trạng thái data** cần có. Mỗi vai **tự chuẩn bị tài khoản/data RIÊNG**.
2. **Các bước**: làm gì · môi trường — mặc định **localhost** (code đang sửa); domain dev chỉ đọc đối chiếu khi User yêu cầu, không ghi dữ liệu lên đó ([automation §3](../guidelines/automation.md)) · **inspect DB giữa các bước** · **dừng hỏi khi nghi vấn, không đoán**.
3. **Verify**: bảng/cột nào, giá trị mong đợi; log nào; cái gì **không** được xảy ra.
4. **Dữ liệu test**: quyết **ngay khi lập kế hoạch** có cần dọn không — mặc định để lại và ghi vào `test-accounts.md` (dữ liệu test thường có giá trị cho lượt sau). Cần dọn (có lý do cụ thể) ⇒ thành **một bước trong *Các bước***; không coi dọn dẹp là việc mặc định của vai.
5. **Reviewer có chạy lại smoke không**: mặc định **không** — Reviewer soi code, test/build và đối chiếu evidence của Dev. Chỉ ghi "có" (kèm bước nào, vì sao) khi Leader và SA thấy evidence của Dev không đủ chứng minh (luồng tiền, tích hợp ngoài, lỗi chỉ lộ khi chạy thật).

Quy mô smoke: **trong scope Phase thì chạy ở quy mô đủ để dám nói tính năng chạy tốt**; ngoài scope thì chứng minh "không bị đụng" bằng unit test/đọc code, không chạy thật tốn tiền. Chi tiết: [blackbox-testing](../guidelines/blackbox-testing.md).

### 5. Mockup UI

Phase đụng giao diện ⇒ **bắt buộc** file mockup `.html` ở `mockups/` của thư mục đợt, phủ **Loading · Empty · Error · Overflow**. Không nhận mô tả chay bằng lời. Hình thức: **prototype bấm được từng màn một + thanh công cụ nổi với hai lựa chọn tách riêng: Màn, rồi Trạng thái của màn đó**, nút/điều hướng chạy thật — không dàn phẳng trạng thái ([ba-sa §Bước 6](../roles/ba-sa.md)).

Epic có **bất kỳ giao diện nào — người dùng cuối lẫn admin console** ⇒ **Phase 0 = mockup tỉ mỉ, User duyệt cẩn thận** trước khi viết các Phase code: đủ state, một luồng bấm trọn vẹn, chuyển động, định hướng thiết kế (admin: không cầu kỳ thị giác nhưng mức tỉ mỉ về luồng/state/UX như nhau). Mockup chuẩn là **hợp đồng** — Phase doc rút trường dữ liệu, API, trạng thái lỗi từ mockup; thiếu gì lộ ra ngay ở mockup thay vì tắc giữa lúc code.

🔴 **Màn đã tồn tại**: phần mockup ghi *giữ nguyên* phải vẽ theo đúng option/hành vi của widget trong code thật (đọc lại file lúc vẽ — vd carousel quay vòng hay dừng ở hai đầu, chạm hay chỉ vuốt). User so mockup với app đang chạy; vẽ theo trí nhớ là sai ngay chỗ không ai định đổi.

Admin: không cầu kỳ nhưng **UX phải tốt**. Màn người dùng cuối: mockup là bản thiết kế — phải có **định hướng thiết kế** đã chốt và **≥ 1 vòng Leader phê bình** trước khi đưa User ([ba-sa §Bước 6](../roles/ba-sa.md)).

### 6. Env, SQL, bàn giao

- Thêm biến cấu hình ⇒ ghi vào Phase doc **và** cấu hình local commit chung của module (`appsettings.Development.json` — thiếu thì người khác pull về là hỏng), key rỗng/mặc định trong `appsettings.json`. Không bao giờ đưa secret production vào repo.
- Đổi schema ⇒ SQL append vào `[YYMMDD].sql`, **không** migration tự sinh. Phase doc trỏ tới file SQL, không chép lại. Mỗi phần mở bằng **header mốc** (`-- === Phase X: <việc> ===`) và kết thúc bằng **query đọc lại** (định nghĩa cột/index/constraint, số dòng bị ảnh hưởng) để dev chạy theo mốc và tự kiểm sau khi chạy — xem [05-handover §C](./05-handover.md).
- Đụng DB / env / client đã phát hành ⇒ section **"Bàn giao"**: thứ tự SQL so với cập nhật image (thêm cột chạy *trước* · xoá cột thì cập nhật image *trước* rồi mới `DROP`), env mới ở repo nào, client cần phát hành lại không. Đây là nguyên liệu cho gói bàn giao ở [05](./05-handover.md).
- Tài liệu `present/` / README module bị ảnh hưởng ⇒ đưa bước cập nhật vào Phase phù hợp.

🔴 **Mọi business rule, acceptance criterion, thiết kế, danh sách file phải sống trong Phase doc** — vì brief cho worker chỉ trỏ tới nó. Còn phải chép spec vào brief ⇒ Phase doc chưa làm đúng vai source-of-truth.

---

## ⚠️ Kế hoạch blackbox phải là thứ đã chạy thử

**Mọi dòng trong kế hoạch blackbox phải đã chạy thử ít nhất một lần, hoặc đánh dấu `chưa kiểm`.** Viết kỳ vọng cho endpoint chưa gọi bao giờ là đang **đoán** — và cái đoán đó đi thẳng vào Definition of Done.

> Ca thật, lặp ba lần một pilot: *"có resume từ stage cuối"*, *"thực thể được chi tiết hoá"*, *"`/validate` trên cây mới trả `valid: true"`* — cả ba đều sai, cả ba nằm ở chỗ **giả định là đã có sẵn**, nên không ai nhìn lại.

---

## Decision Check — khi chạm risk gate

Chỉ áp dụng khi phương án chạm [risk gate](../roles/team-leader.md) (cả phần chung và mục *Risk gate riêng* của `CLAUDE.md`).

| # | Mục | Yêu cầu |
|---|---|---|
| 1 | **Vấn đề** | Đã được chứng minh, không phải suy đoán |
| 2 | **Evidence** | Mỗi claim hiện trạng dẫn nguồn kiểm chứng được, hoặc gắn `assumption`. Số liệu gắn `measured` (nguồn + thời điểm) hoặc `inferred`. Không có khoảng đáng tin ⇒ `unknown`, **không đưa một con số trung bình duy nhất** |
| 3 | **Upstream alternative** | Quyết định upstream nào **loại bỏ** được vấn đề, và vì sao vẫn cần state/schema mới |
| 4 | **Recommendation** | Phương án khuyến nghị kèm thay đổi product/data/schema kéo theo |
| 5 | **Independent challenge** | Một session **không tham gia tạo recommendation** ghi `pass` hoặc blocker |

🔴 `assumption` về hiện trạng **không được** thành contract hoặc acceptance criterion trước khi verify.

> Vì sao có block này: một worker nêu *"cây có thể lệch khung"*, Leader vá bằng hai cột revision thay vì hỏi ngược *"có nên cho sửa khung sau khi đã dựng cây không?"*. Ba phase sau User bác toàn bộ cơ chế, mất trọn một phase để gỡ. **Mục 3 chính là câu hỏi đã bị bỏ qua.**

---

## 🔴 Gate bắt buộc: SA thẩm định Phase doc

User không nên bị biến thành người review từng DTO, SQL hay context mock. **Ngay sau khi Leader viết xong Phase doc, SA thẩm định độc lập** theo 6 tiêu chí ([ba-sa §5](../roles/ba-sa.md)):

```
Leader viết Phase doc
  → Giao SA thẩm định (brief có mục mở)
  → SA soi: 1. khớp định hướng · 2. khớp code thật · 3. đủ cho Dev · 4. đủ cho smoke · 5. khớp ràng buộc CLAUDE.md · 6. trải nghiệm (nếu có UI)
  → [BLOCKER → Leader sửa → SA duyệt lại phần delta]
  → SA verdict PASS — ghi log
```

🔴 **Bất biến**: không Phase doc nào đi vào thi công khi chưa có SA verdict PASS ghi trong log.

## Báo User sau SA PASS

User đã nói rõ ở nhiều dự án: **SA đồng thuận và đã duyệt doc thì User không đọc lại** — *"không rảnh mà đi đọc lại đâu"*. Vì vậy:

- Leader **ghi nhận** SA PASS vào log, **báo User một đoạn tóm tắt ngắn** (Phase làm gì · xong thì deploy dev được chưa · đánh đổi đáng chú ý), rồi **đi thẳng kickoff/giao Dev** — không dừng chờ duyệt.
- **Vẫn phải dừng chờ User** khi Phase: có **va chạm định hướng** chưa chốt · chạm **risk gate** (schema, xoá dữ liệu, chi phí AI mới, contract client đã phát hành…) · cần User quyết một đánh đổi sản phẩm.
- Ủy quyền dạng này ghi **nguyên văn** vào Epic doc, mục *Định hướng User đã chốt*.

🔴 **Không đẩy danh sách phương án ngang hàng lên User.** Có nêu alternative thì vẫn chỉ rõ phương án khuyến nghị và lý do.

---

## Ra khỏi công đoạn này

- [ ] Phase doc đủ: files bắt buộc đọc · TDD · blackbox verify · env · SQL · bàn giao (khi cần)
- [ ] Đụng UI ⇒ đã có mockup `.html` đủ 4 trạng thái
- [ ] Dòng blackbox chưa chạy thử đã đánh dấu `chưa kiểm`
- [ ] Decision Check đã làm (nếu chạm risk gate)
- [ ] 🔴 SA verdict PASS trên Phase doc, ghi log
- [ ] User đã nhận tóm tắt (hoặc đã duyệt, nếu Phase chạm va chạm/risk gate)

⇒ Sang [04-implement](./04-implement.md) — lần đầu của đợt thì qua skill `/kickoff-code-workflow` trước.

⚠️ Gặp thứ thoả *"nếu biết trước thì đã không gặp phải"* ⇒ ghi ngay theo [lessons](../practices/lessons.md).
