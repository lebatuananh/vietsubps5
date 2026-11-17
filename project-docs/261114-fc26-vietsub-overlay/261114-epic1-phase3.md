# 261114 — Epic 1 — Phase 3: Chữ Việt thật lên TV (Hộp thư Career Mode)

📋 **[Epic 1](261114-epic1.md)** · **[Overview](261114-overview.md)**
**Revision**: Rev 1.4 — 2026-11-18 — sau review 3b. · Rev 1.3 — 2026-11-18 — sau review 3a (dòng "Sau review 3a" trong bảng). · Rev 1.2 — 2026-11-18 — SA duyệt lại Rev 1.1: BLOCKER (1) `fontScale` gửi hai nơi ⇒ sửa đúng cách SA chỉ (bỏ khỏi `display`); áp 2 NOTE (P3R2-D2, D3). · Rev 1.1 — 2026-11-18 — theo [SA review](reviews/261114-epic1-phase3-sa-review.md) (BLOCKER B1, B2 + 12 NOTE): Leader áp **tất cả**; bảng dưới là **ràng buộc**, đè lên thân doc. · Rev 1 — 2026-11-17

## Quyết định sau SA gate (Rev 1.1 — ràng buộc)

| # | Quyết định |
|---|---|
| B1 | **Chống gọi trả phí lặp qua phiên**: `TranslationEntry.status` thêm `rejected` (lưu cache, kèm `attempts`, `lastError`, `promptVersion`). Câu bị loại (slot sai) hoặc nằm trong lô bị bỏ (`refusal`/`max_tokens`/lỗi parse) ⇒ `attempts++`; **tổng ≤ 2 lần** qua mọi phiên; sau đó chỉ thử lại khi `promptVersion` hoặc `glossaryVersion` đổi. Tập **`inFlight`** theo `sourceKey`: đang chờ LLM thì không gửi lại dù màn quay về. Lỗi mạng/429/5xx **không** tính `attempts` nhưng lùi lại (≥ 30 s) trước lô kế. Test cả ba |
| B2 | **Giao thức chốt** (Dev 3a chép nguyên vào `tv-overlay/README.md` *Giao thức*): `OverlayItem` có thêm `alt` (string?, bản ngắn — **TV chọn**, D12) và `room` (rect 0..1?, N5); `OverlayFrame` có thêm `display` (tuỳ chọn) `{minFontPx:int, holdMs:int}` — **không** có `fontScale` (engine đã nhân vào `fontPx`, TV dùng `fontPx` nguyên). Màu = chuỗi **`#AARRGGBB`**. `holdMs` do **TV** áp: nhận `items:[]` (hoặc item biến mất) ⇒ giữ lớp cũ `holdMs` rồi fade-out 80 ms. Engine luôn gửi ngay. Bỏ câu "gửi trong `text` thay thế do engine chọn" ở D11 |
| N1 | Bố cục bước 0: **tách dòng OCR thành đoạn** theo `Words[].BoundingRect` khi khe giữa hai từ liền nhau > `SplitGapFactor` (1,2) × cao dòng. Test "hàng 3 cột bị OCR gộp ⇒ 3 item, 3 `group` cột" |
| N2 | `card` = nền **đục 100%**. Test TV: vùng che ⊇ `box` + lề |
| N3 | **Chữ ký theo ô lưới** 16×9 ô trên contentRect: ô đổi (sai khác trung bình > `CellDiffThreshold`) ⇒ ô động. Ô động liên tục ≥ 3 s ⇒ **ô nền động**, bỏ khỏi phép so (trừ ô chứa item đang vẽ). Khi chỉ một số ô đổi: chỉ các item giao các ô đó bị gỡ (gửi gói không có các item đó) và chạy lại OCR/D6/D7 **cho cả khung** khi đứng yên, nhưng item **không đổi** (cùng `id`) giữ nguyên ⇒ TV không fade lại. Hằng số có tên, log được |
| N4 | **Chế độ trận**: trong 3 s gần nhất > 50% ô đổi (không tính ô nền động) ⇒ chế độ trận: gửi `items:[]`, không OCR/dịch tới khi đứng yên liên tục ≥ 1,5 s. Dòng **chỉ chữ hoa ≤ 3 từ** hoặc **≤ 4 ký tự** ⇒ không gọi LLM (chỉ glossary/cache). In số ký tự LLM đã dùng mỗi 10 phút + khi thoát |
| N5 | Engine gửi `room` = hộp lớn nhất quanh bbox mà màu nền vẫn đồng nhất với màu che (dò trên khung, giới hạn ≤ 2× bbox mỗi chiều, không lấn bbox item khác). TV chỉ nới/xuống dòng **trong `room`** (thay luật +15%); vùng che không ra ngoài `room` |
| N6 | `tv-overlay` thêm `android.permission.INTERNET` (socket LAN), sửa comment manifest + README |
| N7 | `fontScale` (mặc định 0,78) nằm ở `engine.local.json` khối `display` — **chỉ engine** nhân vào `fontPx`; không gửi sang TV; chỉnh tối nay bằng cấu hình |
| N8 | `coverMode` mặc định **`auto`**: độ lệch màu vành lấy mẫu > `CoverUniformThreshold` ⇒ engine gửi `card`, ngược lại `solid`. Cấu hình vẫn ép được solid/card/none |
| N9 | `holdMs` mặc định **0** |
| N10 | D9 bổ sung: schema JSON mọi object `additionalProperties:false`, `short_vi` không nằm trong `required`; system (hướng dẫn + glossary) xếp **ổn định**, nếu < 512 token thì không cache (không phải lỗi); Sonnet 5.5 suy nghĩ thích ứng — `MaxTokens` 8000, `Effort = Low`; Bước 7 in `usage` cả hai lô |
| N11 | Mọi đường dẫn dữ liệu (`cache`, `usage`, `engine.local.json`) lấy từ thư mục gốc cấu hình được: biến môi trường **`VIETSUB_HOME`** (mặc định `%LOCALAPPDATA%\VietSub`); test dùng thư mục tạm |
| P3R2-D2 | `id` item = băm(**ô lưới** 16×9 chứa góc trên-trái bbox + câu gốc chuẩn hoá) — OCR lệch vài px không đổi `id` |
| P3R2-D3 | Lô LLM bị bỏ ⇒ lần thử lại **tách đôi lô** (câu tốt không mất lượt oan); `attempts` chỉ tăng cho câu nằm trong lô đơn lẻ (≤ 1 câu) hoặc câu bị loại vì slot sai |
| N12 | D12 theo C2: **chỉ nhãn thiếu khe** (< 2% bề rộng tới nhãn kề) dùng `alt`; còn thiếu ⇒ co **cả nhóm** |
| Sau review 3a — Rev 1.3 | Từ [review 3a](reviews/261114-epic1-phase3-review-3a.md) FAIL (4 blocking). **B1** `id` = băm(ô lưới + câu gốc chuẩn hoá + **số thứ tự lần xuất hiện** trong gói, xếp theo y rồi x); README ghi "`id` duy nhất trong một gói". **B2** lỗi không tạm thời (400/401/403/404, lỗi xác thực/cấu hình) ⇒ **tắt backend LLM cả phiên**, in một dòng rõ lý do, **không** tách đôi, **không** tăng `attempts`, **hoàn** ký tự đã giữ chỗ. **B3** ô nền động có item đang vẽ ⇒ chỉ so **điểm ảnh trong bbox chữ** của item (không trung bình cả ô); test ca J ⇒ ≤ 1 lần đổi trong 10 s. **B4** TV bắt đầu auto-fit ở `max(fontPx, minFontPx)` (ghi README); engine không kẹp. `fontScale` mặc định đổi **1,0** (review đo chữ Việt chỉ ~56% chữ gốc với 0,78). **NB** sửa luôn (trừ khi Dev có lý do, ghi report): NB2 SDK `MaxRetries = 0` (engine tự lùi) · NB3 hoàn ký tự cho 429/5xx, giữ cho timeout · NB4 `usage` ghi temp+rename, đọc hỏng ⇒ **coi như chạm trần** (fail-closed), không làm chết `run` · NB5 `fake` không ghi cache người dùng, không trừ trần · NB6 một hàng đợi dịch duy nhất/khoá ghi file · NB7 chế độ trận chỉ khi chuyển động **kéo dài** (≥ 3 nhịp lấy mẫu khác nhau trong 3 s), một lần đổi trang toàn màn không tính · NB8 ngưỡng gộp đoạn nới 0,35 (thêm test email 2 dòng) · NB9 engine giới hạn ≤ 200 item/gói · `--no-send` không cần giá trị · các NB còn lại theo đề xuất của review |
| Sau review 3b — Rev 1.4 | Từ [review 3b](reviews/261114-epic1-phase3-review-3b.md) FAIL (1 blocking). **Blocking — nhãn bị bẻ dòng, nửa dưới bị hàng dưới che**: `AutoFit` đúng thứ tự D11: **co cỡ tới `minFontPx` trước**, rồi `alt` (C2), rồi mới thêm dòng **chỉ khi tổng chiều cao còn nằm trong `room`**; vẫn không vừa ⇒ co tiếp tới sàn 0,85 × `minFontPx`; cuối cùng nhãn một dòng mới cắt "…" (chỉ là phương án cuối, log). Engine: item **không phải đoạn văn** gửi `maxLines = 1`; đoạn văn giữ số dòng gốc + 2. Test: "Cập nhật chấn thương", "Ban lãnh đạo" trong hàng hẹp ⇒ 1 dòng, không tràn `room`. **NB sửa luôn**: màn OFF focus mặc định **"Về màn hình chính"** (theo mockup) · server chỉ nhận kết nối từ địa chỉ **LAN riêng** (RFC 1918, link-local, loopback), hello phải tới trong 5 s, tối đa 4 kết nối chưa xác thực · không nhận gói nào trong 2 × `ttlMs` ⇒ đóng socket, về "Mất kết nối" (kết nối nửa mở) · "Tạo mã mới" sinh mã một lần · khi `MainActivity` đang ở tiền cảnh thì lớp phủ ẩn (chữ engine không đè giao diện app) · màn Chờ lõi sau khi đổi mã hiện một dòng "Mã đã đổi — trên máy tính chạy lại `engine pair`" · TV phòng thủ `id` trùng (giữ item đầu, log) |
| Mục mở SA | Mọi ngưỡng (khung, gom đoạn, `fontScale`, trận) **đọc từ `engine.local.json` khối `tuning`** (mặc định = hằng số có tên) và in log khi `--verbose` — chỉnh tại chỗ tối nay. Runbook tối: `engine capture` vài khung Hộp thư thật ⇒ `replay` trước rồi mới `run` |

---

## Hiện trạng

- Phase 1: `engine/` (WGC chụp cửa sổ PS Remote Play → cắt client → dò viền → `Windows.Media.Ocr` en-US, 28 test) và `tv-overlay/` (APK **kiểm tra**: lớp phủ `TYPE_APPLICATION_OVERLAY` vẽ 4 góc + chữ Việt mẫu, FGS `specialUse`, font Be Vietnam Pro, `CoordinateMapper`, 6 test) — Reviewer PASS. **Phần trên TV/PS5 thật chưa chạy** (User chưa ở nhà) ⇒ R-A..R-D chưa có số đo thật.
- Phase 2: mockup lớp chữ Hộp thư + màn app TV — Leader phê bình ĐẠT vòng 2; **User chưa xem trên TV**.
- 2026-11-17 User ủy quyền nguyên văn: *"Cứ tiếp tục code hết đi nhé tôi uỷ quyền cho bạn nhé"* ⇒ Leader **không chờ** User chốt mockup trước code (lệch lõi quy trình B6 — có ủy quyền rõ, ghi log). Hệ quả thiết kế: **mọi lựa chọn hiển thị User chưa chốt là cấu hình**, không phải hằng số trong code (bảng *Mặc định chờ User chốt*). Buổi xem mockup trên TV chỉ đổi file cấu hình, không đổi code.
- Nợ Phase 1 trả ở đây (`present/backlog-and-techdebt.md` mục 1): NB8 (giải phóng RCW/`winrtDevice`), NB9 (kiểm HRESULT DWM/`GetClientRect`), **tạo lại frame pool khi cửa sổ đổi cỡ** (bắt buộc — vòng chạy liên tục không được chết vì exit 6).

## Mục tiêu

User mở PS Remote Play trên laptop, chạy **một lệnh** `engine run`, ngồi chơi FC 26 trên TV: ở **Hộp thư Career Mode** (danh sách + email) chữ tiếng Anh được **thay tại chỗ** bằng tiếng Việt đẹp, đúng chỗ, không nhấp nháy, cuộn thì chữ mờ đi rồi hiện lại. Đây là **lần cài thử chính thức đầu** (ô 5).

Engine **không lọc theo màn**: mọi màn đứng yên đều được OCR + dịch (User: *"Tôi muốn bạn phải dịch được hết"*). Hộp thư là màn **được kiểm chất lượng** của Phase này; màn khác "chạy được, chưa cam kết đẹp" — nói rõ với User ở gói bàn giao.

## Prerequisites

- ✅ Phase 1 code + Reviewer PASS; ✅ mockup Phase 2 (hợp đồng hiển thị: `mockups/261114-mockup-inbox-overlay.html`, `mockups/261114-mockup-tv-app.html`, report vòng 2).
- ✅ LLM: **Claude Sonnet 5.5** (`claude-sonnet-5-5`), **trần 100.000 ký tự nguồn/ngày** — User duyệt 2026-11-15.
- ⏳ `ANTHROPIC_API_KEY` — **User tự đặt** trên laptop (biến môi trường người dùng). Không có ⇒ engine chạy chế độ **chỉ cache/glossary** + một dòng cảnh báo; Dev/Reviewer **không** cần khoá (test dùng dịch giả lập).
- ⏳ F1/F2 thật — **không chặn code**; chặn số đo chất lượng trên ảnh thật (Bước 8).

## Files bắt buộc đọc trước khi code

| File | Vì sao |
|---|---|
| `engine/README.md` | Bẫy WGC/DPI/OCR đã gặp, mã thoát, định nghĩa `confident`/`fallback` — vòng chạy liên tục dùng lại y nguyên |
| `engine/src/Engine/Capture/WindowCapturer.cs`, `Geometry/*`, `Ocr/OcrRunner.cs`, `Commands.cs` | Tái sử dụng — **không** viết lại chụp/dò viền/OCR |
| `tv-overlay/README.md`, `tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/*.kt` | `OverlayService` (FGS, thứ tự addView rồi startForeground), `OverlayView`, `CoordinateMapper`, quyền + hint miễn pin đã qua review 2b |
| `mockups/261114-mockup-inbox-overlay.html` + `reviews/261114-epic1-phase2-mockup-report.md` (cả Vòng 2) | **Hợp đồng hiển thị**: kiểu che, cỡ chữ tối thiểu SA đề xuất, fade 80/120–150 ms, quy tắc C1 (vùng che = hợp bbox gốc ∪ bbox Việt + lề), C2 (nhãn hàng ngang: `short_vi` trước rồi co cả nhóm), X4 (cùng cột cùng cỡ) |
| `mockups/261114-mockup-tv-app.html` | Luồng màn app TV sản phẩm (quyền → chờ lõi → đã kết nối; tắt lớp phủ; ghép cặp lại; mã ghép cặp dễ đọc X8; hint suy ra từ trạng thái X15) |
| `261114-discovery.md` §4.1–4.4, R2.2, R2.4 | Định dạng `OverlayFrame`, `TranslationEntry`, glossary, ghép cặp; kế hoạch smoke T1–T10 |
| `project-docs/present/backlog-and-techdebt.md` | Nợ engine (mục 1) + lớp phủ đè mọi nguồn (mục 2) |

## Quyết định thiết kế (Leader)

| # | Quyết định |
|---|---|
| D1 | **Kênh**: TCP, mỗi gói **một dòng JSON UTF-8** kết thúc `\n`. **TV là server** cổng cố định **47820**, engine là client. Dòng đầu engine gửi `{"hello":{"v":1,"token":"<mã>"}}`; TV trả `{"ok":true}` hoặc `{"ok":false,"reason":"token"\|"version"}` rồi đóng. Một kết nối tại một thời điểm (kết nối mới hợp lệ thay kết nối cũ). Định dạng ghi ở `tv-overlay/README.md` (nguồn chân lý) và `engine/README.md` trỏ tới |
| D2 | **`OverlayFrame` v1** (theo discovery §4.1, chốt): `{v:1, sessionId, seq, ttlMs, items:[OverlayItem]}`. `OverlayItem`: `id` (ổn định giữa khung: băm vị trí làm tròn + câu gốc), `box` {x,y,w,h 0..1 theo **vùng hình game** (contentRect)}, `text` (Việt, ≤ 1000 ký tự), `fontPx` (cỡ gốc ước lượng, đơn vị px trên khung cao 1080), `group` (string — các item cùng nhóm vẽ **cùng cỡ** = min sau auto-fit, X4/C2), `maxLines` 1–6, `align` start\|center\|end, `bold` bool, `textColor` ARGB, `cover` {`mode` solid\|card\|none, `color` ARGB}. Bỏ khỏi v1 (không cần): `screenKind`, `italic`, `caps`, `selected` (vệt chọn được xử lý bằng **lấy mẫu màu** — D7). TV: `v` ≠ 1 ⇒ bỏ; `seq` ≤ seq đã vẽ cùng `sessionId` ⇒ bỏ; `items:[]` ⇒ xoá |
| D3 | **Ghép cặp**: TV sinh **một lần** mã 8 ký tự từ bộ không nhập nhằng `ACDEFHJKMNPRTUVWXY3479` (lưu `SharedPreferences`), hiện dạng `XXXX-XXXX` cỡ lớn cùng `IP:47820` (X8). "Ghép cặp lại" = sinh mã mới, ngắt kết nối hiện tại. Engine: `engine pair --tv <ip> --code <mã>` ghi `%LOCALAPPDATA%\VietSub\engine.local.json` (ngoài repo; so mã không phân biệt hoa/thường, bỏ `-`) và thử hello ngay, in OK/lỗi |
| D4 | **Vòng chạy** `engine run [--window-title "PS Remote Play"] [--hwnd] [--translator claude\|cache\|fake] [--no-send]`: chụp ~4 khung/s (WGC hiện có, tạo lại frame pool khi đổi cỡ) → **chữ ký khung** = ảnh xám thu nhỏ 64×36 của contentRect → khác khung trước > ngưỡng (hằng số có tên, chỉnh sau khi có F3) ⇒ trạng thái **Động**: gửi ngay `items:[]` (TV fade-out ~80 ms) · **Đứng yên** ≥ `stableMs` (mặc định 250) ⇒ OCR → bố cục (D6) → dịch (D8) → gửi. Cùng chữ ký với lần đã gửi ⇒ **không** OCR lại, chỉ gửi lại gói cũ (seq mới) mỗi 1 s làm nhịp tim (`ttlMs` 3000). Cửa sổ thu nhỏ/không thấy ⇒ ngừng gửi (TV tự xoá theo ttl), thử lại mỗi 2 s, **không thoát**. Ctrl+C ⇒ gửi `items:[]` rồi thoát |
| D5 | **Dịch một phần** (X3): câu trúng cache/glossary vẽ ngay; câu mới gom **một lô** gọi LLM nền; có kết quả mà màn **vẫn cùng chữ ký** ⇒ gửi lại gói đủ. Câu chưa có bản dịch **không vẽ** (giữ tiếng Anh gốc — không vẽ bản thô) |
| D6 | **Bố cục từ OCR**: bỏ dòng chỉ gồm số/ký hiệu/≤ 1 chữ cái, và token icon tay cầm. Gộp **đoạn văn**: các dòng liên tiếp cùng mép trái (±0,6% rộng), chiều cao dòng lệch ≤ 20%, khoảng cách dọc ≤ 0,9× cao dòng ⇒ một item nhiều dòng (dịch cả đoạn, `maxLines` = số dòng gốc + 2). `group` = cùng mép trái (±0,6%) và cùng cao dòng (±10%) trong cùng ảnh — **cột** của danh sách; nhãn trên **cùng một hàng ngang** (tâm y ±0,5%, cao ±10%) cùng `group` "row-…" (C2). `fontPx` = cao dòng gốc × 1080 / cao contentRect × hệ số Leader 0,78 (cap-height → cỡ chữ; chỉnh khi có F2) |
| D7 | **Màu**: engine có pixel ⇒ **lấy mẫu** trên khung đã chụp: `cover.color` = trung vị màu vành 3 px quanh bbox (ngoài chữ); `textColor` = trung vị các điểm ảnh trong bbox có độ tương phản cao nhất so với nền (10% điểm xa màu nền nhất). Dòng nằm trên vệt chọn tự ra nền sáng/chữ tối — không cần cờ `selected` |
| D8 | **Dịch**: (1) **chuẩn hoá** khoảng trắng + dấu nháy; **che biến** bằng regex thành slot `{N1}`… (số, tiền `€12.5M`/`£40K`, phần trăm, ngày, giờ, `OVR 84`) ⇒ `sourceKey`; (2) **glossary** `engine/data/fc26/glossary.json` (khớp nguyên câu, không phân biệt hoa/thường) — có `short_vi` cho nhãn hẹp; (3) **cache** JSON `%LOCALAPPDATA%\VietSub\cache\fc26.json` (`TranslationEntry` theo discovery §4.2, khoá (`gameId`,`sourceKey`), `reviewed` không bị ghi đè, ghi file **nguyên tử** temp+rename) + **cache hạt giống** trong repo `engine/data/fc26/seed-cache.json` (nạp trước cache người dùng, cache người dùng thắng trừ khi seed `reviewed`); (4) LLM cho câu còn thiếu. Slot của bản dịch phải **đúng bằng** tập slot nguồn, sai ⇒ loại, không cache, không vẽ. Tên riêng (cầu thủ/CLB/giải) giữ nguyên — dặn trong prompt |
| D9 | **LLM**: gói NuGet chính thức **`Anthropic`** (ghi giấy phép vào README — kiểm `.nuspec`; không MIT/Apache ⇒ dừng báo Leader). `client.Messages.Create`, `Model = "claude-sonnet-5-5"`, `OutputConfig = { Effort = Low, Format = JSON schema {items:[{id, vi, short_vi?}]} }`, **không** prefill, **không** `temperature`; `System` = hướng dẫn + glossary, đặt `CacheControl = new CacheControlEphemeral()` ở khối cuối của system; lô ≤ 40 đoạn; `MaxTokens` 8000; kiểm `StopReason` (`refusal`/`max_tokens` ⇒ bỏ lô, log). Khoá chỉ từ `ANTHROPIC_API_KEY` — **không bao giờ** log/ghi file. Tham khảo cách gọi C#: skill `claude-api` (`csharp/claude-api/README.md` — Leader cấp `--add-dir`) — tên kiểu sai thì để compiler chỉ, không đoán bằng reflection |
| D10 | **Trần chi phí**: đếm **ký tự nguồn** gửi LLM theo ngày giờ máy, lưu `%LOCALAPPDATA%\VietSub\usage\YYYY-MM-DD.json`; chạm 100.000 ⇒ ngừng gọi LLM tới hết ngày (vẫn dùng cache), in cảnh báo một lần. Không gọi lại câu đã thất bại trong cùng phiên (chống gọi lặp) — tối đa 1 lần/`sourceKey`/phiên |
| D11 | **TV vẽ**: `OverlayView` vẽ từng item: (1) map `box` qua `CoordinateMapper` (Phase 1); (2) **auto-fit**: bắt đầu `fontPx` (quy theo chiều cao màn TV), xuống dòng trong bề rộng box (nới tối đa +15% bề rộng nếu không đè item khác), giảm cỡ tới **cỡ tối thiểu** (cấu hình, mặc định theo mockup report) rồi tăng dòng tới `maxLines`; vẫn tràn ⇒ dùng `short_vi` nếu có (gửi trong `text` thay thế do engine chọn — xem D12) rồi cắt dòng cuối bằng "…" **chỉ cho đoạn văn**; (3) cùng `group` ⇒ cùng cỡ = min; (4) **vùng che = hợp(bbox gốc, bbox chữ Việt) + lề ≥ 6/1080 chiều cao** (C1) — `solid`: tô `cover.color` viền mờ 2 px; `card`: bo góc 8 px nền tối 80%; `none`: chỉ chữ có bóng; (5) font Be Vietnam Pro Regular/SemiBold (`bold`). Đổi khung: item mới fade-in 120 ms, `items:[]` ⇒ fade-out 80 ms. Hết `ttlMs` không gói ⇒ xoá |
| D12 | **Nhãn hàng ngang** (C2): engine gửi kèm `alt` (string, `short_vi`) khi glossary có; TV dùng `alt` cho **cả nhóm** khi khe giữa hai nhãn liền nhau < 2% bề rộng, sau đó mới co cỡ cả nhóm |
| D13 | **App TV sản phẩm** thay màn kiểm tra Phase 1 (giữ màn kiểm tra là **debug-only** extra `--ez diag true` cho runbook Phase 1). Màn: **Cần cấp quyền** (quyền vẽ đè + miễn pin, hint suy ra từ trạng thái — X15, giữ logic review 2b) → **Chờ lõi** (`IP:47820`, mã `XXXX-XXXX` cỡ lớn, khối thu gọn "Hướng dẫn cho người cài" — C3) → **Đã kết nối** (2 nút: **Tắt lớp phủ** · **Ghép cặp lại**) · **Mất kết nối** (quay về Chờ lõi sau ttl). **Mở app = tắt lớp phủ** (X5-3): màn "Đã tắt lớp phủ — OK để bật lại" khi lớp phủ đang bật; focus mặc định chỉ đặt khi mở màn (NB-R2). FGS sống khi app ra nền; server chạy trong `OverlayService`. Bỏ tự tắt 30 phút của APK kiểm tra (ở bản sản phẩm lớp phủ rỗng khi lõi không gửi) |
| D14 | **Không làm ở Phase này**: UsageStatsManager tự ẩn theo nguồn TV (X5-1 — spike sau, ghi backlog); fuzzy matching cache; tray/autostart; mDNS; render offline trong engine (thay bằng emulator + màn nền debug — D15); dịch Batch API |
| D15 | **Màn nền debug** (chỉ bản debug): `BackdropActivity` hiển thị toàn màn một PNG (`--es backdrop /sdcard/Download/<file>.png`) để thay nguồn HDMI trên emulator — lớp phủ vẽ đè lên nó, `screencap` ra ảnh trước/sau thật của renderer TV |
| D16 | **Lệnh phụ engine**: `pattern [--send]` (gửi 4 item góc + lưới chữ để kiểm khớp góc trên TV thật — thay APK kiểm tra) · `replay --input <dir PNG> [--translator …] [--send] [--hold-ms 4000]` (đọc PNG thay WGC, cùng pipeline, ghi `<ảnh>.frame.json` = gói đã gửi, `misses.json`) · `warm --input <dir PNG>` (OCR + dịch, chỉ ghi cache — làm ấm, **tốn tiền** ⇒ in số ký tự dự kiến và cần `--yes`) · `cache-export --out <file.tsv>` (bảng gốc ↔ dịch cho End User đọc) |

## Mặc định chờ User chốt (cấu hình, không phải hằng số)

Mọi lựa chọn nằm ở khối `display` của `engine.local.json`; TV không có màn cài đặt (X6), chỉ vẽ theo gói:

| Khoá | Mặc định | User chốt khi xem mockup trên TV |
|---|---|---|
| `coverMode` | `solid` | solid / card / none |
| `minFontPx` | theo mockup report (px trên 1080) | cỡ nhỏ nhất |
| `holdMs` (giữ chữ cũ khi bắt đầu cuộn trước khi xoá) | 150 | 0 / 150 / 300 / 500 |
| `statusDot` | `false` | có/không chấm "đang dịch" — **v1 chỉ log**, chưa vẽ (ghi rõ) |
| xưng hô trong prompt | "HLV" ở nhãn, "ông" trong thư trang trọng | HLV / ông / anh |
| ngày / tiền | giữ nguyên dạng gốc qua slot | định dạng Việt (sau) |

`minFontPx` và `holdMs` engine gửi trong mỗi gói (`OverlayFrame.display {minFontPx, holdMs}` — trường tuỳ chọn) để TV không cần màn cài đặt (X6).

## Kế hoạch

TDD (logic thuần viết test trước): chuẩn hoá + che slot, khoá cache, validator slot, glossary (không trùng khoá, `short_vi` ngắn hơn), trần ký tự/ngày, chữ ký khung + máy trạng thái Động/Đứng yên (dãy chữ ký giả lập), bố cục (gộp đoạn, nhóm cột/hàng — từ `ocr.json` mẫu), lấy mẫu màu (ảnh tổng hợp), (de)serialize `OverlayFrame` + hello; TV: parse gói, từ chối token/`v`/seq cũ, auto-fit (câu dài nhất, nhiều dòng, cỡ tối thiểu, `group` cùng cỡ, `alt`), vùng che C1, sinh/so mã ghép cặp.

**Lượt code tách** (như Phase 1): **3a** = `engine` (Bước 1–4), **3b** = `tv-overlay` (Bước 5–6). Reviewer review sau mỗi lượt. 3a và 3b gặp nhau ở **giao thức** — Dev viết `tv-overlay/README.md` mục *Giao thức* ở **đầu** lượt 3a.

1. **Giao thức + ghép cặp (engine)**: mô hình `OverlayFrame`/hello (System.Text.Json, camelCase), `TvClient` (kết nối lại tự động mỗi 2 s, gửi bất đồng bộ, không chặn vòng chụp), lệnh `pair`, `pattern`. Cấu hình `engine.local.json` (đọc/ghi, `*.local.json` đã gitignore — file nằm ở `%LOCALAPPDATA%` nên ngoài repo).
2. **Vòng chạy**: `run` + `replay` dùng chung pipeline (nguồn khung là interface: WGC hoặc thư mục PNG). Chữ ký khung, máy trạng thái, tạo lại frame pool khi đổi cỡ (thay exit 6 ở chế độ `run`; `capture` giữ hành vi cũ), NB8, NB9.
3. **Bố cục + màu** (D6, D7) → item.
4. **Dịch**: glossary + seed (Dev soạn **seed ~80–150 nhãn/câu Hộp thư & khung Career Mode** thông dụng, `status: machine`; End User/Leader duyệt sau), cache, `ClaudeTranslator` (D9), `FakeTranslator` (`[VI] ` + gốc, cho test), trần (D10), `warm`, `cache-export`. **Không** gọi API thật trong lượt code (risk gate) — Leader gọi thật một lần có kiểm soát ở Bước 7.
5. **TV — server + renderer**: `OverlayServer` (một luồng nền, ServerSocket 47820, hello/token, đọc dòng ≤ 256 KB, sai ⇒ đóng), `OverlayView` vẽ theo D11/D12, ttl, fade.
6. **TV — màn sản phẩm** theo D13 + `BackdropActivity` debug (D15) + giữ màn kiểm tra sau `diag`.
7. **Leader — gọi LLM thật có kiểm soát** (trong trần đã duyệt, chỉ khi User đã đặt khoá): `engine warm` trên ảnh tổng hợp/F1 có sẵn ≤ 2.000 ký tự ⇒ kiểm slot/JSON schema/caching (`cache_read_input_tokens` > 0 ở lô thứ 2). Không có khoá ⇒ ghi `chưa kiểm`, chuyển cho buổi tối với User.
8. **Trên TV thật (User chạy, Leader dẫn)** — gộp với runbook Phase 1: cài APK sản phẩm, `pair`, `pattern` (R-A/R-B), `run` ở Hộp thư 10 phút; chụp TV bằng điện thoại trước/sau; đo "chữ ma" (discovery R2.4.3 #11).

## Blackbox Verify

### Chuẩn bị
- Không tài khoản. Emulator AVD theo `automation/README.md` (Dev `vietsub_dev`/5556, Reviewer `vietsub_rv`/5558). Ảnh: ảnh tổng hợp Hộp thư của mockup/`playtest/epic1-phase1/dev/synthetic/` (không phải FC 26 thật — ghi rõ "không phải R-D"); có F1 thật thì dùng thêm.
- Không `ANTHROPIC_API_KEY` trong tiến trình worker (đặt rỗng khi chạy smoke) ⇒ chứng minh 0 gọi mạng.

### Các bước (`chưa kiểm`)
1. `dotnet test engine` và `gradlew :app:testDebugUnitTest` xanh.
2. `engine replay --input <ảnh tổng hợp> --translator fake --no-send` ⇒ `.frame.json` có item đúng số đoạn chữ, đoạn văn được gộp, `group` cột đúng, màu lấy mẫu hợp lý (in vài giá trị); `misses.json` rỗng với fake.
3. `--translator cache` với seed ⇒ câu có trong seed ra tiếng Việt, câu khác **không có item** (không vẽ bản thô); log "0 HTTP".
4. Emulator: cài APK debug, `appops … SYSTEM_ALERT_WINDOW allow`, mở app ⇒ màn Chờ lõi có `IP:47820` + mã; `adb forward tcp:47820 tcp:47820`; `engine pair --tv 127.0.0.1 --code <mã>` OK; mã sai ⇒ lỗi rõ.
5. `BackdropActivity` với ảnh tổng hợp; `engine replay … --send --hold-ms 6000` ⇒ `screencap`: chữ Việt đè đúng chỗ, **không pixel chữ gốc ló ra** (C1), cùng cột cùng cỡ, đủ dấu (font Be Vietnam Pro), tab hàng ngang không dính (C2). Đóng engine ⇒ lớp phủ tự xoá ≤ ttl + 0,5 s.
6. Replay chuỗi ảnh A, A, B, B (giả lập cuộn): log trạng thái Động→Đứng yên; gói `items:[]` gửi ngay khi đổi; **0** lần đổi chữ khi khung đứng yên 10 s (đếm seq có `items` khác nhau).
7. `engine pattern --send` ⇒ 4 góc sát 4 góc emulator.
8. Bấm phím thật (`input keyevent`): mở app khi lớp phủ bật ⇒ màn "Đã tắt lớp phủ"; OK ⇒ bật lại; focus luôn thấy, không bị kéo sau thao tác; "Ghép cặp lại" ⇒ mã đổi, engine cũ bị từ chối.
9. Token sai / `v:2` / dòng > 256 KB ⇒ TV đóng kết nối, không crash (logcat).
10. Trần: test đơn vị + chạy `replay --translator claude` với `ANTHROPIC_API_KEY=` rỗng ⇒ cảnh báo "không có khoá — chỉ dùng cache", không exception.

**Reviewer chạy lại**: **có** (bước 2–9, emulator riêng 5558) — tích hợp mạng/Android/WGC.

## Bàn giao

- **Cài lên TV**: APK sản phẩm thay APK kiểm tra (`adb install -r`, cùng package `vn.vietsub.ps5.tv`) — User chạy.
- **Máy User**: `ANTHROPIC_API_KEY` (User tự đặt) · `%LOCALAPPDATA%\VietSub\` (cấu hình, cache, usage) — xoá thư mục = về như mới.
- **Firewall**: engine là client ⇒ Windows không hỏi quyền inbound.
- **Chi phí**: trần 100.000 ký tự/ngày ≈ cỡ < 1 USD/ngày với Sonnet 5.5 (`assumption` theo bảng giá discovery) — in số ký tự đã dùng khi thoát.

## Decision Check (risk gate)

| Gate | Trạng thái |
|---|---|
| LLM trả tiền | Đã duyệt Sonnet 5.5 + trần 100k ký tự/ngày (2026-11-15). Worker **không** gọi thật; Leader gọi thật ≤ 2.000 ký tự ở Bước 7 |
| Thao tác TV/PS5 | User chạy (Bước 8) |
| Thư viện mới | `Anthropic` (NuGet chính thức) — kiểm giấy phép; copyleft ⇒ dừng |
| PSN | Không đụng |
| Kiến trúc | Đúng hướng đã chốt (lớp phủ Google TV, HDMI đi thẳng) |
| Lệch quy trình | Code trước khi User chốt mockup — **có ủy quyền** 2026-11-17; giảm rủi ro bằng cấu hình hoá lựa chọn hiển thị |
