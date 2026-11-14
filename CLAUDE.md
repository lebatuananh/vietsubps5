# CLAUDE.md

Hướng dẫn cho AI agent (Claude Code và mọi platform khác) khi làm việc trong workspace này.

## ⚠️ Quy tắc đọc tài liệu (BẮT BUỘC)

File này là entry point — nó **trỏ tới** tài liệu chi tiết chứ không chứa hết. Khi làm bất kỳ task nào:

- **Thấy "có chút" liên quan là phải đọc.** Tài liệu được link dưới đây trông *có thể* liên quan (dù mơ hồ) ⇒ **đọc trọn trước khi viết code hay tài liệu**. Không suy đoán nội dung từ tên file, không làm theo trí nhớ.
- **Reference là chỉ thị, không phải gợi ý.** Link tới `project-docs/present/` (hiện trạng) và `workflow/` (quy trình) là tài liệu *phải đọc* khi đụng phạm vi tương ứng.
- **Đọc thẳng file được nêu**, không chỉ liếc tên thư mục.

| Muốn biết | Đọc |
|---|---|
| Cách team AI làm việc (quy trình, vai, công đoạn) | [`workflow/README.md`](workflow/README.md) — thuộc template, không đặc thù dự án |
| Dự án này là gì, ràng buộc gì | File này |
| Hiện trạng hệ thống, các đợt update | [`project-docs/`](project-docs/README.md) |
| Quy ước code của một module | `README.md` của module đó |

---

## Dự án

| | |
|---|---|
| **Sản phẩm** | **VietSub PS5** — dịch chữ tiếng Anh trong game PS5 sang tiếng Việt và **vẽ thẳng lên màn hình TV**, đè đúng chỗ chữ gốc, trông như game có sẵn tiếng Việt. Game đầu tiên: **EA SPORTS FC 26** (menu, Ultimate Team, mô tả cầu thủ, chiến thuật). Cơ chế: lấy bản sao hình game qua **Remote Play** (PS5 → máy xử lý trong LAN) ⇒ OCR ⇒ dịch (có glossary theo game) ⇒ gửi chữ + toạ độ qua LAN tới **app trên Google TV** vẽ lớp phủ trong suốt lên nguồn HDMI. **Hình game luôn đi thẳng HDMI PS5 → TV** |
| **Người dùng** | User và gia đình chơi PS5 trên TV ở nhà; sau này có thể chia sẻ cho **anh em bạn bè** (phi thương mại) |
| **Kênh phát hành** | APK cài tay (sideload) lên Google TV · lõi xử lý chạy trên máy tính Windows trong LAN (giai đoạn đầu) — **không** phát hành Play Store |
| **Thiết bị thật** | TV **Sony 65" Google TV** · PS5 · máy tính Windows 11 — cả ba cùng mạng LAN nhà User |
| **Team** | User — dev làm việc với team AI, **kiêm** người nhận bàn giao và tester (mục *Bàn giao*) |

🔴 **Lát cắt phải nhỏ vì mỗi Phase phải test được ngay.** Nguyên tắc chia Phase: *lát nhỏ nhất mà vẫn ra được thứ dùng thử và đánh giá được* — không phải lát lớn nhất mà vẫn an toàn về kỹ thuật. Ở dự án này "dùng thử được" = **User nhìn thấy kết quả trên TV thật**.

---

## 🔴 Ràng buộc đặc thù

| Ràng buộc | Hệ quả |
|---|---|
| **Không can thiệp PS5 hay game**: không jailbreak, không sửa file game, không giả lập tay cầm. Chỉ **đọc hình** qua Remote Play | Can thiệp ⇒ tài khoản PSN / máy PS5 có thể bị Sony khoá vĩnh viễn — mất game đã mua của User và người được chia sẻ |
| **Không bao giờ nhận, lưu, log mật khẩu PSN.** Liên kết tài khoản chỉ qua trang đăng nhập chính thức của Sony (lấy Account ID) + mã PIN Remote Play | Lộ mật khẩu PSN của User/anh em = mất tài khoản có tiền thật (ví, game đã mua) |
| **Không chèn gì vào đường hình HDMI PS5 → TV** (không capture card ở giữa, không ghép lại hình) | Thêm trễ 50–100 ms ⇒ FC 26 "dính tay", mất mục đích chính. Lớp phủ đi **song song**, trễ của chữ dịch chấp nhận được, trễ của hình game thì không |
| **Mã có giấy phép AGPL-3.0 (vd chiaki-ng cho Remote Play)**: dùng ⇒ khi chia sẻ APK/bản build cho bất kỳ ai ngoài User phải kèm **toàn bộ mã nguồn** theo AGPL | Chia sẻ cho anh em = phân phối; giấu mã ⇒ vi phạm giấy phép. Thêm thư viện mới phải ghi giấy phép vào README module |
| **Chữ Việt phải đẹp**: font có đủ dải dấu tiếng Việt (U+1EA0–1EF9, vd Be Vietnam Pro / Inter) nhúng trong app; không để chữ tràn khung, không nhấp nháy | Font thiếu dải ⇒ chữ có dấu lấy font hệ thống từng chữ, lệch nét — lỗi dễ lọt qua review ảnh. Chất lượng hiển thị **là** sản phẩm |
| **Chi phí dịch máy/LLM**: mọi câu đã dịch phải được **cache** (theo game + câu gốc), có **trần ký tự/ngày**; ưu tiên glossary + dịch offline, LLM chỉ cho câu mới | Dịch liên tục theo từng khung hình ⇒ hoá đơn API tăng không giới hạn |
| **Mạng LAN nhà là tin cậy có giới hạn**: kênh điện thoại/PC → TV chỉ nghe trong LAN, không mở cổng ra Internet | Mở ra ngoài ⇒ người lạ vẽ được chữ lên TV / đọc được hình game nhà User |

---

## Risk gate riêng

Ngoài risk gate chung ([team-leader §5](workflow/roles/team-leader.md)), dự án này luôn cần User duyệt khi:

- Đụng **tài khoản PSN / đăng ký Remote Play** (đăng nhập Sony, Account ID, mã PIN, token) — kể cả chỉ để thử.
- **Thao tác trên thiết bị thật của User**: cài/gỡ APK lên TV, bật tuỳ chọn nhà phát triển / gỡ lỗi qua mạng, đổi cài đặt hình ảnh TV, đổi cài đặt PS5. Agent đưa lệnh/hướng dẫn, **User chạy** (trừ khi User uỷ quyền rõ cho đúng việc đó).
- Gọi **dịch vụ dịch/LLM tốn tiền** ngoài chế độ giả lập, hoặc thêm nhà cung cấp mới.
- **Thêm thư viện có giấy phép copyleft** (GPL/AGPL) hoặc thay đổi cách chia sẻ bản build cho người khác.
- Quyết định **lệch hướng kiến trúc đã chốt** (lớp phủ trên Google TV, hình game đi thẳng HDMI) — ghi vào tài liệu đợt trước, không lệch ngầm trong code.

---

## Cấu trúc repo

🔴 **Dự án này là MONOREPO — lệch có chủ ý so với template** (User chốt 2026-11-14): **một repo git duy nhất** ở root, remote GitHub `origin` = `https://github.com/lebatuananh/vietsubps5.git` (máy User chưa có SSH key cho GitHub — kiểm 2026-11-14; có key thì đổi sang `git@github.com:lebatuananh/vietsubps5.git`), **private**. Khối "Repo con" trong `.gitignore` root **giữ comment**. Chuyển public chỉ khi chia sẻ bản build có mã AGPL (Risk gate riêng).

Đọc `workflow/` (viết cho chế độ nhiều repo) theo cách quy đổi sau:

| `workflow/` nói | Ở dự án này |
|---|---|
| "từng repo" / `git -C <repo>` / `git status` từng repo | **Một** `git status` ở root (lọc theo thư mục module nếu cần) |
| Tạo nhánh đợt ở mỗi repo module bị đụng | **Một** nhánh đợt cho cả repo, tên theo chuẩn [guidelines/git](workflow/guidelines/git.md) |
| `project-docs` chỉ `main`, tài liệu commit thẳng `main` | Tài liệu đợt commit **trên nhánh đợt**, cùng code; vào `main` khi nhánh được merge. Sửa `project-docs` ngoài đợt (vd `automation/`) ⇒ commit thẳng `main` |
| Nhánh chuẩn của module `main` + `dev` | Cả repo: `main` + `dev` |
| "deploy môi trường dev" | Cài bản build lên **TV + máy tính thật** của User (không có server) |

**Chưa có module nào.** Dự kiến (mở bằng đợt riêng khi tới lượt, **không** dựng sẵn):

| Module (dự kiến) | Vai trò | Stack (chốt ở discovery) |
|---|---|---|
| `tv-overlay/` | App Google TV: nhận chữ + toạ độ qua LAN, vẽ lớp phủ trong suốt lên nguồn HDMI, màn hiệu chỉnh tỉ lệ | Android (Kotlin) |
| `engine/` | Lõi xử lý: lấy khung hình Remote Play ⇒ OCR ⇒ dịch (glossary, cache) ⇒ gửi lên TV | Windows trước (chụp cửa sổ PS Remote Play chính thức) |
| `mobile/` | Đưa lõi xử lý lên điện thoại để không cần bật máy tính (Remote Play bằng chiaki-ng — AGPL) | Android — đợt sau, chỉ khi lõi Windows đã ổn |

**README của module là nguồn chân lý của module đó** — nhưng **chỉ chứa thứ đọc code rồi chạy thẳng vẫn dễ vấp, và vấp thì tốn công đáng kể** (bẫy môi trường, build, runtime, quy ước code không tự nói ra). Không kế hoạch, không mô tả lại thứ đọc code là thấy. Code xong có đổi quy ước hoặc gặp bẫy mới thoả tiêu chí đó ⇒ cập nhật README ngay.

---

## Môi trường

| | |
|---|---|
| **DB** | Chưa có — dự án không cần DB máy chủ. Cache bản dịch là file/DB nhúng cục bộ của module (chốt ở đợt đụng tới) |
| **Thiết bị test thật** | TV Sony Google TV · PS5 · máy tính Windows — địa chỉ IP, cách kết nối `adb` qua mạng, cài đặt hình ảnh đã chỉnh: [`project-docs/automation/README.md`](project-docs/automation/README.md). Agent được **đọc** trạng thái thiết bị (`adb devices`, chụp màn hình TV) sau khi User đã bật gỡ lỗi; mọi thay đổi trên thiết bị theo *Risk gate riêng* |
| **Emulator** | Android TV emulator (Android Studio) cho test tự động không cần TV thật — chú ý: emulator **không có nguồn HDMI**, chỉ kiểm được vẽ lớp phủ trên app thường ([guidelines/blackbox-android-adb](workflow/guidelines/blackbox-android-adb.md)) |
| **File cấu hình** | Cấu hình local của module **commit cùng repo** (IP mặc định, cổng LAN). Ghi đè riêng máy: file `*.local.*` (gitignore). Khoá API dịch/LLM **không bao giờ** vào repo — chỉ biến môi trường máy User |
| **Dịch vụ ngoài tốn tiền** | Dịch máy / LLM cho bản dịch chất lượng cao — **chưa chọn nhà cung cấp**; đợt nào tích hợp thì ghi key/hạn mức dùng cho dev ở đây. Mặc định dev/test dùng dịch offline hoặc giả lập |
| **Tài khoản ngoài** | Tài khoản **PSN của User** (Remote Play) — chỉ User thao tác (Risk gate riêng). Không có tài khoản test PSN |
| **Hạ tầng** | Không có server — mọi thứ chạy trong LAN nhà User |

---

## Bàn giao

Team AI dừng khi kết luận được **bản local đã ngon** ⇒ viết gói bàn giao ([05-handover](workflow/leader-stages/05-handover.md)) rồi dừng.

| Việc | Ai |
|---|---|
| Nhận gói bàn giao, cài APK lên TV, chạy lõi trên máy tính, merge nhánh | User |
| Test thật trên TV khi chơi FC 26 | User (kiêm tester) |
| Chỉ định thời điểm đóng đợt | User |

---

## Tài liệu hiện trạng (`project-docs/present/`)

**Đọc trọn file liên quan trước khi sửa luồng đó.** Sửa một luồng đã có doc ⇒ cập nhật luôn doc đó.

| Khi task đụng tới… | Đọc |
|---|---|
| Backlog, nợ kỹ thuật, bẫy môi trường | [backlog-and-techdebt.md](project-docs/present/backlog-and-techdebt.md) — đọc trước mọi đợt |

Chưa có luồng nào được code — đợt nào code luồng nào thì thêm dòng ở đây.

---

## Quy tắc vàng

1. **Khảo sát trước khi code** — tìm xem đã có tính năng/pattern/logic tương tự chưa.
2. **Tuân thủ pattern hiện tại** — không tự sáng tạo quy tắc mới khi cái cũ vẫn chạy tốt.
3. **Tái sử dụng** helper, component, service đã có.
4. **Thiết kế tương xứng** — tối giản nhất chạy tốt; chưa biết thì đừng xây sẵn ([feature-development](workflow/guidelines/feature-development.md)).
5. **Không tự chế format** — giao thức giữa lõi và TV theo một định dạng duy nhất, ghi ở README module; định dạng của thư viện/API bên ngoài kiểm chứng từ nguồn.

> **Sự nhất quán quan trọng hơn sự "tối ưu" cục bộ.**

---

## Quy trình

Mọi đợt update theo [`workflow/protocol.md`](workflow/protocol.md) — Direct Command: Team Leader brainstorm với User, gọi CLI của worker (BA/SA, Dev, Reviewer, End User) trực tiếp, verify rồi mới đi tiếp.

```
/brainstorm → [discovery ≥ 2 lượt] → /prepare-code-branch → /write-implementation-docs → [SA gate]
  → /kickoff-code-workflow → [Dev ↔ Reviewer] → [05-handover — DỪNG] … [06-close khi User chỉ định]
```

| Skill | Làm gì |
|---|---|
| `/brainstorm` | Lấp đủ **6 ô** với User (hỏi từng câu), tạo draft + log, dẫn dắt discovery với BA/SA |
| `/prepare-code-branch` | Xác định repo bị đụng, đề xuất tên nhánh, đưa lệnh tạo nhánh từng repo — **User chạy** |
| `/write-implementation-docs` | Overview → Epic → Phase cuốn chiếu, **SA thẩm định PASS** trước thi công |
| `/kickoff-code-workflow` | Verify tiền đề (nhánh mọi repo), probe platform, tạo lessons + sổ session |
| `/delegate` | Gọi worker qua CLI — dùng ở mọi công đoạn |
| `/bootstrap-project` | Một lần sau khi clone template |

Skill chỉ là con trỏ mỏng; quy trình gốc ở `workflow/leader-stages/`. Hook `SessionStart` nạp lại nội dung skill sau mỗi compact.

Tài liệu đợt nằm ở `project-docs/[YYMMDD]-[chủ-đề]/`. Danh sách các đợt: [`project-docs/overview-project.md`](project-docs/overview-project.md).

---

## Cách User muốn cộng tác

- Trao đổi bằng **tiếng Việt có dấu**, kể cả khi đang làm việc với code/brief tiếng Anh. Identifier giữ nguyên tiếng Anh.
- Hỏi bằng **text thường** — **không dùng `AskUserQuestion`** (User không nhìn được toàn cảnh). Một câu mỗi lần, kèm **đề xuất có chủ kiến**.
- **Không tự chạy lệnh git** (commit, push, tạo nhánh) trừ khi được ủy quyền rõ cho đúng việc đó. **Merge và deploy không bao giờ** là việc của agent.
- **Không hỏi thứ tự quyết được** — thứ tự code trong một Phase, thứ khảo sát ra được, hệ quả của cái đã chốt.
- Hành động dứt điểm — không kết thúc lượt bằng *"em sẽ làm A rồi B"*.
- Không chặn lượt của User khi chờ worker — chạy nền, User phải nhắn được bất cứ lúc nào.

---
*Tài liệu sống — cập nhật khi có thay đổi lớn về kiến trúc hoặc ràng buộc dự án.*
