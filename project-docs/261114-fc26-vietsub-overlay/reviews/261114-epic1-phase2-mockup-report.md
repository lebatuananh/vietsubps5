MOCKUP R2 DONE
# 261114 — Epic 1 Phase 2 — Báo cáo dựng mockup (vai SA)

**Ngày**: 2026-11-17 · **Theo**: [`261114-epic1-phase2.md`](../261114-epic1-phase2.md) Rev 1.1, Bước 1–2 và bảng *Quyết định sau SA gate* X1–X15 (ràng buộc). Nền là **ẢNH TẠM** vì F2 chưa có.

## 1. File

| File | Nội dung |
|---|---|
| [`mockups/261114-mockup-inbox-overlay.html`](../mockups/261114-mockup-inbox-overlay.html) (≈ 409 KB) | Lớp chữ Việt vẽ đè trên Hộp thư. 4 Màn × 24 Trạng thái |
| [`mockups/261114-mockup-tv-app.html`](../mockups/261114-mockup-tv-app.html) (≈ 403 KB) | Màn app VietSub trên Google TV + 3 cảnh TV (nguồn HDMI, app Video, màn hình chính) để đếm phím. 10 Màn × 31 Trạng thái |
| `playtest/epic1-phase2/sa/NN-*.jpg` (66 ảnh) + 5 file `tv-walk-*.txt` | Ảnh mọi Màn × Trạng thái + bản ghi đi bằng phím (xem §4) |

**Tự chứa**:
- Font Be Vietnam Pro Regular + SemiBold nhúng base64 lấy từ `tv-overlay/app/src/main/res/font/` (X12).
- Ảnh tạm vẽ bằng SVG ngay trong file.
- **Không tải gì từ mạng**: `grep -o "https\?://…"` trên hai file chỉ ra đúng chuỗi namespace `http://www.w3.org/2000/svg` (định danh, không phải request); `src="http|href="http|url(http` = 0 ở cả hai file.

**Cách dựng lại**: hai file được build từ mã nguồn ở `D:\Temp\vietsub-mockup\` (`common.css`, hai template, `build.py` chèn font base64, `shoot.sh` chụp bằng Edge headless). Các file đó **không** nằm trong repo. Sửa trực tiếp file HTML trong `mockups/` vẫn được — mã đọc được, chỉ có font là chuỗi base64 dài.

**Khung hình**:
- Sân khấu 16:9, 1920×1080 logic, tự co theo cửa sổ; phím F hoặc nút "Toàn màn" để chiếu lên TV qua HDMI.
- Phím = remote: ↑↓←→, Enter, Backspace; TV app thêm phím Home.
- Thanh công cụ nổi: **Màn** rồi **Trạng thái** (hai ô riêng). Trang Hộp thư có thêm **Độ trễ 0/150/300/500 ms** và **Kiểu che**; trang app TV có **vị trí ghim** + nút "Đếm phím".
- Nút "Quyết định cần chốt" (phím Q) mở bảng quyết định cho User.
- `?capture=1` ẩn thanh công cụ.

**Thay F2 thật**: chỉ sửa khối `BACKGROUNDS` (một chỗ, đầu `<script>`). Danh sách nhận nhiều ảnh theo dòng đang chọn, vd `{0:'data:…', 3:'data:…'}`; ↑↓ chỉ đi qua các dòng có ảnh. Toạ độ chữ nằm ở `SCREEN_DATA` (một khối mỗi màn, số 0..1). Nút "Khung chữ" vẽ viền mọi ô để căn. Nhãn ẢNH TẠM tự ẩn khi màn có ảnh thật.

## 2. Mỗi X được thể hiện ra sao

| # | Cách thể hiện | Bằng chứng |
|---|---|---|
| X1 | Bấm ↑↓ ⇒ ảnh nền (vệt chọn) đổi **ngay**; lớp chữ cũ giữ đúng `độ trễ` đã chọn, rồi mờ 80 ms → chờ đứng yên 250 ms → hiện 150 ms. Dải thời gian in trên màn (vd 300 ms ⇒ chữ mới đủ sau ≈ 780 ms). Ảnh tạm dựng sẵn 8 biến thể vệt chọn | `11-inbox-list-motion-freeze-300ms.jpg`: vệt chọn đã sang dòng 2, mảng chữ Việt cũ còn tô dòng 1 — đúng khoảnh khắc "chữ ma" |
| X2 | Bảng quyết định ghi rõ: chỉ chốt khi xem trên TV, nhận xét từ laptop chưa tính. Sân khấu 16:9 + toàn màn để cắm HDMI. Phần PNG trong suốt cho APK là việc đầu Phase 3 — **không** làm ở đây | `28-inbox-decisions-panel.jpg` |
| X3 | `Dịch một phần` (2 dòng còn tiếng Anh, có/không chấm vàng cuối dòng); `Màn chưa hỗ trợ` (Career Hub tạm, không lớp phủ, một dòng giải thích) | `04`, `05`, `27-inbox-hub-unsupported.jpg` |
| X4 | Mỗi ô thuộc `group`; cả nhóm dùng cỡ **nhỏ nhất** mà mọi ô vừa. Thứ tự: bản đầy đủ ở cỡ gốc → bản ngắn ở cỡ gốc → co dần tới tối thiểu 24 px → 2 dòng → viền đỏ "vượt khung". Đoạn văn email auto-fit riêng. Tab "MEDIA" dùng bản ngắn "BÁO CHÍ" thay vì co cả hàng tab | `06` (cột người gửi/ tiêu đề/ ngày cùng cỡ), `09-inbox-list-overflow.jpg`, `17-inbox-email-long-overflow.jpg` |
| X5 | (1) Cảnh "người nhà mở app Video": lớp chữ tự ẩn, **0 phím**, ghi "chờ kiểm trên TV Sony"; kèm cảnh ngược "chưa có quyền ⇒ chữ vẫn đè". (2) Màn "Máy tính mất kết nối": lớp chữ tự xoá. (3) Mở app khi lớp phủ đang bật = màn "Đã tắt lớp phủ" + hướng dẫn ghim vào Yêu thích. Đếm phím thật trên màn hình chính mô phỏng — xem §4 | `58`, `59`, `49`, `53`, `62–64` + `tv-walk-quickoff-pinned{1,4,0}.txt` |
| X6 | Không có màn Cài đặt. "Đã kết nối" chỉ có **Tắt lớp phủ** · **Ghép cặp lại**; bấm Tắt ⇒ nút đổi chữ thành "Bật lớp phủ", focus giữ nguyên | `44-tv-connected-default.jpg` |
| X7 | 4 trạng thái chuẩn chỉ vẽ khi có nghĩa; chỗ không áp vẽ thẻ **N/A + lý do** (8 thẻ). Thêm "Mở app khi lớp phủ đang bật" và "Quyền bị thu hồi / TV vừa cập nhật" | `35–37`, `43`, `48`, `51–52`, `54`, `55–56` |
| X8 | Mã **6 chữ số chia 2 nhóm** `482 913`, cỡ 156 px, chữ số tabular. Khung phụ **"màn LAPTOP (không hiện trên TV)"** mô phỏng VietSub Engine nhập địa chỉ + mã | `39-tv-pair-default.jpg`, `42-tv-pair-overflow.jpg` |
| X9 | Nhãn **"Bản dịch minh hoạ"** + **"ẢNH TẠM"** trên mọi khung. Bảng quyết định có định dạng **ngày** (`12/07` · `12/07/2026` · `12 Th7` · giữ) và **tiền** (`45,0 triệu €` · `€45,0 tr` · giữ), tên riêng/ mã vị trí giữ nguyên, cùng các quyết định khác | `28`, `61` |
| X10 | Bảng quyết định nhắc: chốt **sau** Phase 1 Bước 6 | `28` |
| X11 | CLB/cầu thủ hư cấu (Riverside Athletic, Port Alder United, Northgate FC, Kestrel Town, Marco Velden, Kai Brenner, Leo Marsh, Tomás Ruel). Không logo, không huy hiệu, không ảnh người. Chữ gốc trong ảnh tạm dùng font hệ thống (Segoe UI/Arial), **không** Cruyff Sans. Không lấy ảnh nào từ web | toàn bộ ảnh |
| X12 | Font nhúng base64; ảnh chụp cho thấy dấu chồng đủ (ẾỖẶ…) bằng Be Vietnam Pro | `06`, `16` |
| X13 | Đường dẫn đầy đủ như doc | §1 |
| X14 | Kiểu che chọn được trên thanh công cụ: `tự chọn` (đề xuất: solid trên nền phẳng, card trên ảnh) · `solid` · `card`. `none` **chỉ** xuất hiện trong khung so sánh. Ghi chú "solid ở đây khớp màu hơn thực tế — xem X2" nằm trên khung so sánh và trong bảng quyết định | `18-inbox-email-long-compare.jpg`, `12`, `20` |
| X15 | Hướng dẫn nằm trong thẻ mỗi bước, hiện khi trạng thái là CHƯA, không phụ thuộc thao tác vừa làm. Cảnh "quay về từ cài đặt, quyền vẫn chưa có": focus giữ ở nút vừa bấm | `31-tv-setup-back.jpg`, `66-tv-walk-setup-back-not-granted.jpg` + `.txt` |

Backlog mục 3 (UX chỉ-có-remote):
- **(a)** = X5.
- **(b)** Mỗi hướng dẫn có dòng "Người cài (cần máy tính): `adb …`". Màn nói thẳng "TV Sony thường không có màn này", không giả vờ có đường chỉ-remote.
- **(c)** = X15 / NB-R2.
- **(d)** Màn hệ thống mô phỏng là danh sách chung, VietSub ở dòng 8; hướng dẫn nói "kéo xuống tìm dòng VietSub".
- **(e)** Nút pin "Thử mở cài đặt": màn chớp rồi quay về, trạng thái vẫn CHƯA, hướng dẫn vẫn hiện, không vòng lặp (`32-tv-setup-stub.jpg`).

## 3. Phần nào là tạm

- **Toàn bộ ảnh nền** (Hộp thư, email, Career Hub, cảnh HDMI, app Video): SVG tự vẽ, bố cục **đoán** theo mô tả discovery. Không giống UI thật của FC 26. Vị trí chữ phải căn lại khi có F2.
- **Câu tiếng Việt**: worker (SA) viết tay — không LLM, chưa qua End User.
- **Màn hình chính Google TV và màn hệ thống "Hiển thị trên ứng dụng khác"**: mô phỏng. Số phím ở §4 đúng **trên mô phỏng**, chưa đúng trên TV Sony thật.
- **Hằng số chuyển động**: `STABLE_MS = 250` là **giả định** cho thời gian chờ đứng yên + OCR + tra cache (Phase 3 đo). Mờ 80 ms và hiện 150 ms theo discovery §5.
- **Màu mảng `solid`** lấy mẫu từ chính ảnh tạm (đọc pixel ảnh SVG chạy được trong Edge) ⇒ khớp hoàn hảo hơn thực tế (X14).

## 4. Đi bằng phím — số phím thật (trên mô phỏng)

Bấm tự động từng phím, ghi focus sau mỗi phím (`?walk=…`, xuất bằng `--dump-dom`):

| Đường | Phím | Số phím |
|---|---|---|
| Tắt nhanh (3), VietSub **ghim vị trí 1** Yêu thích | Home, ↓, ↓, OK | **4** |
| Tắt nhanh (3), **ghim vị trí 4** | Home, ↓, ↓, ▶, ▶, ▶, OK | **7** |
| Tắt nhanh (3), **chưa ghim** (trang Ứng dụng) | Home, ▶, ▶, ↓, ↓, ▶, ▶, ▶, OK | **9** |
| Cài đặt lần đầu chỉ bằng phím: cấp quyền overlay | OK, ↓×7 (tới dòng VietSub), OK, Back | 10 |
| … tiếp: miễn pin | ↓, OK ⇒ màn chớp, vẫn CHƯA ⇒ **kẹt**: "Tiếp tục" bị khoá vì bước 2 bắt buộc | 12 phím rồi dừng |

Bản ghi đầy đủ: `tv-walk-quickoff-pinned1.txt`, `…pinned4.txt`, `…pinned0.txt`, `tv-walk-setup-keys-only.txt`, `tv-walk-setup-back-not-granted.txt`.

**Phát hiện từ việc đi phím**: người chỉ có remote **không hoàn tất được** phần cài đặt trên TV Sony, vì miễn pin cần `adb`. Với User thì không sao: cài APK đã cần máy tính. Nhưng nếu sau này chia sẻ cho anh em mà họ cài bằng app Downloader thì sẽ kẹt. Xem câu hỏi Q3 ở §6.

## 5. Định hướng thiết kế đã áp

- **Vibe**: *bản địa · kín đáo · sắc nét · bình tĩnh* (discovery §5).
  - Lớp chữ: không có viền, icon hay màu riêng nào của VietSub. Màu chữ bám màu chữ gốc (trắng / xám xanh / chữ tối trên vệt chọn).
  - App TV: tối, phẳng, một màu nhấn vàng `#FFD400` (giữ từ APK kiểm tra).
- **Thang chữ**:
  - Lớp chữ: cỡ = cỡ chữ gốc ước theo ảnh (18–58 px trên khung 1080); **tối thiểu 24 px** (quyết định #7).
  - App TV: wordmark 76 · tiêu đề 44–64 · tiêu đề bước 30 · mô tả 23 · hướng dẫn 21 · nút 28 · mã ghép cặp 156. Mọi thứ ≥ 21 px để đọc từ 3 m.
- **Kiểu che**:
  - `solid` = màu trung vị lấy mẫu ở vành 3 px quanh ô chữ, mép mờ `0 0 3px 2px`. Với ô chữ nằm trong nút, lấy mẫu vành **bên trong** để giữ màu nút.
  - `card` = thẻ tối **90 %** bo góc 10 px (discovery §5 ghi ~80 %; trên ảnh chụp, 82 % còn **lộ bóng chữ tiếng Anh** ⇒ nâng lên 90 %).
  - `tự chọn` = solid trên nền phẳng, card trên ảnh — **đề xuất**.
- **Chuyển động**:
  - Lớp chữ: chỉ mờ — tắt 80 ms, hiện 150 ms. Không trượt, không nảy.
  - Chấm "đang dịch": 14 px, nhịp 2 s, độ mờ 0,55↔1.
  - App TV: focus phóng 1,05 + bóng vàng, 120 ms; không có hiệu ứng chuyển màn.
- **Focus trên app TV**: nền vàng chữ đen (nhìn rõ từ xa, User đã quen ở APK). Focus mặc định chỉ đặt khi mở màn (X15).

## 6. Câu hỏi thiết kế cho vòng phê bình của Leader

1. **Đoạn văn email auto-fit riêng (X4)**: ở `17-…overflow.jpg`, đoạn 1 co còn 28 px còn đoạn 2–3 giữ 30 px ⇒ một email có hai cỡ chữ. Cách này có "méo" không? **Đề xuất**: mọi đoạn trong một email dùng chung cỡ (coi cả thân thư là một nhóm).
2. **Mảng `solid` trên nền gradient** để lộ hình chữ nhật mờ (quanh "HỘP THƯ", hàng tab — `06`). Trên hình HDR thật sẽ rõ hơn. Có nên coi gradient là "ảnh" (⇒ `card`) không, hay tăng độ mờ mép?
3. **Bước miễn pin có bắt buộc để "Tiếp tục" không** (§4)? **Đề xuất**: cho tiếp tục kèm cảnh báo vàng "TV có thể tắt VietSub khi chuyển sang PS5", vì đây là rủi ro chứ không phải hỏng chức năng.
4. **Màn "Đã tắt lớp phủ"**: X5 ghi "OK để bật lại", mockup đặt focus mặc định vào **"Về màn hình chính"**. Lý do: User vừa bấm OK để mở app, bấm OK thêm lần nữa theo đà là bật lại ngay. Leader chốt.
5. **Bước 3 "Biết ứng dụng đang mở"** (quyền `GET_USAGE_STATS`) đã có trên màn cài đặt dù chưa spike. Giữ (khuyên, không chặn) hay ẩn tới khi Phase 3 chứng minh nó chạy trên Sony?
6. **Chấm chờ của "Dịch một phần"** nằm sát cuối cột ngày (`05`), hơi chạm chữ "Yesterday". Dời vào lề phải của dòng, hay bỏ chấm theo dòng, chỉ giữ một chấm chung?
7. **`STABLE_MS` 250 ms** quyết định phần lớn tổng trễ (300 ms trễ ⇒ 780 ms mới có chữ mới). Nếu Phase 3 đo ra lớn hơn, cảnh mô phỏng đang **lạc quan**. Có nên thêm lựa chọn 500 ms để User thấy trường hợp xấu không?
8. **Thanh công cụ** dùng chuột; khi chiếu lên TV, User phải điều khiển bằng bàn phím laptop (phím tắt `[ ] , . D C Q F`). Có cần chế độ "trình chiếu" tự đi qua các trạng thái không?
9. Mockup Hộp thư có **khung xem trước bên phải** (bố cục đoán). Nếu FC 26 thật không có khung này, phần đó bỏ — chờ F2.

## 7. Chưa kiểm

- Chưa mở trên **TV thật** (X2) và chưa thử ở chế độ có cửa sổ trên Chrome — chỉ chụp bằng Edge headless 1920×1080.
- Chưa kiểm font bằng DevTools *Rendered Fonts* (Blackbox bước 4); ảnh chụp cho thấy đúng hình chữ Be Vietnam Pro, nhưng chưa có bằng chứng từ DevTools.
- Chưa có F2 ⇒ chưa căn chữ theo UI thật.
- Ảnh evidence đã chuyển từ PNG sang JPG (q 3) để giảm dung lượng: 30 MB → 16 MB. Leader quyết có commit cả 16 MB không.

## 8. Câu hỏi mở — phần tôi kém chắc User chấp nhận nhất

**Khoảnh khắc chuyển dòng (X1).** Ở `11-…freeze-300ms.jpg`:
- Vệt chọn đã nhảy sang dòng mới, nhưng mảng chữ Việt cũ vẫn tô dòng cũ.
- Sau đó cả lớp chữ biến mất gần nửa giây rồi mới hiện lại.

Đây không phải chi tiết thiết kế có thể làm đẹp hơn. Nó là giới hạn vật lý của kiến trúc: hình HDMI đi thẳng, chữ đi vòng qua Remote Play → máy tính → TV.

User duyệt kiểu chữ trên một khung đứng yên thì sẽ thấy "đẹp". Nhưng khi chơi thật, cuộn Hộp thư là việc làm liên tục, nên "chữ ma" và "nhấp nháy khi cuộn" sẽ là cảm giác chính — đúng điều User cấm (*"không bị méo xệch"*, *"không nhấp nháy"*).

Nếu ở 300–500 ms User thấy không chịu được, các phương án đều có giá:
- ẩn lớp chữ ngay khi bấm phím (cần biết phím được bấm — PS5 không cho);
- chỉ dịch khung xem trước, để nguyên danh sách tiếng Anh;
- chấp nhận "chỉ hiện khi đứng yên".

Vì vậy mockup cố ý để User **tự chọn độ trễ trên TV**. Đây là quyết định nên đưa ra trước mọi quyết định thẩm mỹ.

---

## Vòng 2 (2026-11-17) — sửa C1, C2, C3 theo [phê bình Leader vòng 1](261114-epic1-phase2-leader-critique.md)

Dựng lại cả hai file như vòng 1 (template ngoài repo → `build.py` chèn font base64). Không có request mạng: quét URL trên hai file vẫn chỉ ra chuỗi namespace `http://www.w3.org/2000/svg`. Có 36 ảnh mới, tiền tố `r2-`, trong `playtest/epic1-phase2/sa/`. Ảnh vòng 1 giữ nguyên để so.

### C1 — che kín chữ gốc

**Nguyên nhân thật** của lỗi ở `16-…`:
- Thẻ `card` 90 % trong suốt nên chữ gốc nằm dưới vẫn hiện qua ("len" sau "…Marco Velden", bóng quanh tiêu đề).
- Vùng che bám ô dữ liệu, không bám vùng chữ gốc thực.

**Đã sửa**:
- Vùng che giờ là một lớp riêng, bằng **hợp(bbox chữ gốc, bbox chữ Việt) + lề 6 px** (`MARGIN`). Riêng `card` có thêm 6 px lề ngang.
  - bbox chữ gốc: với ảnh thật thì là ô OCR (`box`). Với ảnh tạm thì đo đúng phần chữ SVG đã vẽ, kể cả khi tràn ô.
- `card` đổi sang **đục hoàn toàn** (`#090d14`).
- `solid` lấy mẫu màu ở vành ngay ngoài vùng che. Bỏ cách lấy mẫu "vành trong" cho nút: vùng che giờ ôm sát chữ nên tự nằm trong nút.

**Kiểm bằng chế độ X-ray** `?xray=1`: chữ Việt trong suốt, chỉ còn vùng che; nếu chữ gốc nào ló ra là thấy ngay. Đã chụp 10 ảnh `r2-17…r2-26`:
- danh sách: đã dịch, mục đang chọn, overflow, dịch một phần, giữa chuyển động 300 ms;
- email dài: đã dịch, overflow, kiểu card;
- email ngắn: đã dịch, mục đang chọn.

Xem từng ảnh: **không thấy ký tự gốc nào**. Lưu ý về phạm vi:
- Ở "dịch một phần", dòng chờ vẫn hiện tiếng Anh vì đúng là chưa che (chủ ý).
- `none` trong khung so sánh vẫn lộ chữ gốc — đó là ô đối chứng.

Ảnh thường của mọi trạng thái bị ảnh hưởng: `r2-01…r2-16`.

### C2 — khe tối thiểu cho nhãn hàng ngang

- Nhãn có `hrow` (tab, gợi ý nút ở chân màn) được rộng tới **vị trí nhãn kế tiếp − 40 px** (`MIN_GAP`).
- Thiếu chỗ thì dùng bản ngắn trước. Vẫn thiếu thì cả nhóm co về cùng một cỡ (cơ chế nhóm X4).
- TRANSFERS → "CHUYỂN NHƯỢNG" (≈ 228 px) không vừa 210 px ⇒ dùng bản ngắn **"MUA BÁN"** ở cỡ gốc 26 px, cả hàng tab giữ 26 px (`r2-03`).
- Bảng quyết định (`r2-27`) có mục mới **"Ràng buộc cho bản cài đầu (Phase 3) — không phải lựa chọn của anh"** ghi C1 và C2.

### C3 — lệnh `adb` gom vào một khối ẩn

- Bỏ mọi dòng lệnh mono nhỏ trong từng thẻ bước. Hướng dẫn chỉ còn câu "(lệnh: xem "Hướng dẫn cho người cài")".
- Thêm nút **"Hướng dẫn cho người cài"** cạnh "Tiếp tục" (màn Cài đặt lần đầu) và dưới thẻ bước (màn Quyền bị thu hồi). Bấm thì mở một khối chứa cả 3 lệnh:
  - tiêu đề lệnh 22 px, lệnh 22 px mono, mô tả 24 px — đều ≥ 20 px;
  - mặc định ẩn; focus chỉ trong khối; "Đóng" hoặc Backspace thoát, focus về nút đã mở.
- Ảnh: `r2-28…r2-35` (thêm trạng thái "Mở Hướng dẫn cho người cài" — `r2-33`). `r2-36` là đi bằng phím ↓ ↓ ↓ OK từ màn đầu để mở khối.

### Câu hỏi mở — sửa C1 có làm màn nào xấu đi không?

- **Thẻ trên ảnh tiêu đề email: không to hơn mà gọn hơn** (`r2-09`).
  - Thẻ tiêu đề rộng theo chữ gốc (dài hơn câu Việt khoảng 10 %).
  - Thẻ dòng phụ ngắn lại hẳn, vì trước đây nó kéo theo ô dữ liệu 1400 px, giờ chỉ ôm phần chữ.
  - Cái mất là thẻ giờ **đục hẳn**, che nhiều ảnh nền hơn và trông "nặng" hơn bản 90 %. Đây là cái giá bắt buộc của C1 với kiểu `card`.
- **Xấu đi thật ở hai chỗ nhỏ**:
  1. Lề 6 px của nhãn tab "TẤT CẢ" đè lên **gạch chân tab đang chọn** của game: X-ray `r2-17` cho thấy đoạn gạch dưới chữ bị mảng `solid` màu nền che, chỉ còn mẩu ở mép phải. Ở ảnh thường (`r2-03`) khó nhận ra trên ảnh tạm, nhưng với ảnh thật thì vạch nhấn/ đường kẻ sát chữ sẽ bị "ăn" mất. **Đề xuất cho Phase 3**: khi lấy mẫu thấy vành có màu nhấn đậm khác nền (đường kẻ), cắt lề phía đó về 2 px thay vì 6 px — ràng buộc C1 vẫn giữ vì chữ gốc không ló.
  2. Bản ngắn "MUA BÁN" (C2) để lại **khe lớn** trước "ĐỘI HÌNH", vì vị trí tab bám tab gốc: hàng tab nhìn thưa không đều. Chấp nhận được (không dính, không méo cỡ). Từ "MUA BÁN" cần End User/ User xác nhận có hợp giọng game không.
- Mảng `solid` giờ ôm sát chữ nên **nhỏ hơn**, ít lộ hình chữ nhật trên nền gradient hơn vòng 1 — không xấu đi.

### Chưa kiểm (giữ như vòng 1)

- Chưa xem trên TV thật.
- Chưa căn theo F2.
- Đo bbox chữ gốc của ảnh tạm dựa trên Edge vẽ SVG bằng Segoe UI. Máy khác thiếu font này thì đo lệch — chỉ ảnh hưởng ảnh tạm, ảnh thật dùng ô OCR.
