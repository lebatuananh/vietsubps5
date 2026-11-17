SA-GATE-P3R2 DONE
## Verdict
BLOCKER (1)
> Vòng 1 (Rev 1): BLOCKER (2). Vòng 2 (Rev 1.1): BLOCKER (1) — D1 `fontScale`, sửa một dòng. Xem *Duyệt lại Rev 1.1* ở cuối file.

# 261114 — Epic 1 Phase 3 — SA review

**Doc**: [`261114-epic1-phase3.md`](../261114-epic1-phase3.md) Rev 1. **Đối chiếu**: code tại `0aa3527` (`engine/`, `tv-overlay/`), mockup report Vòng 2 + Leader critique (C1/C2/C3), backlog mục 1–3, discovery R2.6.

**Kết luận nhanh**: hướng đúng, đủ chi tiết ở phần lớn. Hai BLOCKER, đều sửa vài dòng:
- **B1** — gọi trả phí lặp qua các phiên.
- **B2** — giao thức chỗ hai lượt 3a/3b gặp nhau có mâu thuẫn, Dev phải đoán.

Ngoài ra có 4 NOTE "nên sửa trước 3a" (N1–N4): đây là những thứ làm User chê ngay trên TV.

## Finding

| # | Tiêu chí | Chỗ | Vấn đề | Evidence | Đề xuất sửa |
|---|---|---|---|---|---|
| **B1** | 5 · gọi trả phí lặp (BLOCKER 1) | D8, D10 | Câu bị **loại** (slot sai), lô **bị bỏ** (`refusal`/`max_tokens`) không được lưu ⇒ **mỗi phiên `engine run` gọi lại** đúng các câu đó, mãi mãi. D10 chỉ chặn "1 lần/`sourceKey`/**phiên**". Thêm nữa, doc không nói câu **đang chờ** LLM: màn đổi rồi quay lại cùng chữ ký trước khi lô trả về thì câu có bị gửi lần hai không | D10 nguyên văn "tối đa 1 lần/`sourceKey`/phiên"; D8 "sai ⇒ loại, không cache" | (a) Cache thêm trạng thái `rejected` kèm `attempts` + `lastError`, lưu **qua phiên**. Thử lại tối đa 2 lần tổng; sau đó chỉ thử lại khi `promptVersion`/`glossaryVersion` đổi. (b) Giữ tập `inFlight` theo `sourceKey`, không gửi trùng khi đang chờ. (c) Thêm test cho cả hai |
| **B2** | 3 · Dev phải đoán ở giao thức (BLOCKER 3) | D2 vs D11/D12, bảng *Mặc định* | (1) D11 nói bản ngắn "gửi trong `text` thay thế do engine chọn", D12 nói gửi trường **`alt`** để TV chọn ⇒ hai cách loại trừ nhau. (2) `alt` và `display {minFontPx, holdMs}` **không có** trong danh sách trường `OverlayFrame v1` của D2. (3) `holdMs` không rõ bên nào áp: engine trì hoãn gửi `items:[]` hay TV trì hoãn xoá? D4 lại nói "Động ⇒ gửi **ngay** `items:[]`". (4) Định dạng ARGB (số nguyên hay chuỗi `#AARRGGBB`) chưa chốt. 3a và 3b là hai lượt Dev riêng, chỉ gặp nhau ở README ⇒ Dev 3a phải tự chọn | D2, D11 (2), D12, dòng 74 | Chốt trong doc: `alt` là trường của `OverlayItem`; TV chọn (D12); bỏ câu trong D11. Thêm `alt` + `display` (tuỳ chọn) vào D2. `holdMs` do **TV** áp (giữ lớp cũ `holdMs` rồi fade-out khi nhận `items:[]`). Màu = chuỗi `#AARRGGBB`. Dev 3a chép nguyên vào README *Giao thức* |
| N1 | 3/6 · bố cục sai trên màn chính | D6 | `Windows.Media.Ocr` có thể **gộp cả hàng danh sách** (người gửi · tiêu đề · ngày) thành **một dòng**. Đó chính là lý do quy tắc chấm OCR Phase 1 phải khớp chuỗi con. D6 chỉ gộp/nhóm theo **dòng** ⇒ cả hàng thành một item, bị dịch như một câu, vùng che trùm 3 cột | `engine/src/Engine/Ocr/OcrRunner.cs:63-68` có cả `Words` + `BoundingRect` từng từ; quy tắc B1 Phase 1 | D6 thêm bước đầu: **tách dòng OCR thành đoạn** khi khe giữa hai từ liền nhau > 1,2 × cao dòng (hằng số có tên). Thêm test "hàng 3 cột bị gộp ⇒ 3 item, 3 `group` cột" |
| N2 | 5/6 · trái C1 | D11 (4) | `card` "nền tối **80%**". Mockup Vòng 2 đã chứng minh 80–90 % **lộ bóng chữ gốc** ⇒ C1 (ràng buộc Phase 3 trong bảng quyết định) yêu cầu `card` **đục hoàn toàn** | mockup report Vòng 2 §C1; `r2-21-xray-email_long-translated.jpg` | `card` = nền đục 100 %. Thêm test C1 trên TV: vùng che ⊇ `box` + lề |
| N3 | 6 · nhấp nháy / màu vệt chọn (câu hỏi 2–3 của brief) | D4, D7 | Chữ ký **trung bình toàn khung 64×36** có hai lỗi ngược nhau. (a) Menu FC có **nền động** (hạt, video sân, bảng tin chạy) ⇒ khung không bao giờ "đứng yên", hoặc bật/tắt liên tục. (b) Vệt chọn dời một dòng chỉ đổi rất ít điểm trên toàn khung ⇒ có thể **dưới ngưỡng** ⇒ không OCR/lấy mẫu lại ⇒ mảng che màu vệt chọn **nằm ở dòng cũ**, đúng lỗi "méo xệch". D7 lấy mẫu chỉ đúng khi được chạy lại | `assumption` (chưa có F3) | Chữ ký **theo ô lưới** (vd 16×9 ô): ô nào đổi > T ⇒ Động. Ô **động liên tục** ≥ 3 s ⇒ đánh dấu "ô nền động", bỏ khỏi phép so (ngoại trừ ô chứa item đang vẽ). Ô chứa item đổi ⇒ chạy lại D6/D7 cho item đó. Hằng số có tên, log được để chỉnh tối nay |
| N4 | 6 · trận đấu + chi phí (câu hỏi 1) | Mục tiêu "không lọc theo màn", D4 | Trái **R2.6 đã duyệt** ("đang đá trận: lớp phủ im lặng") và backlog mục 2 ("lõi chỉ gửi khi khung là FC 26"). Mỗi lúc trận đấu dừng ≥ 250 ms (đá phạt, phát lại, ăn mừng) ⇒ OCR tên cầu thủ/tỉ số/bình luận chữ ⇒ mảng che chớp trên HUD. Rác OCR từ cảnh sân ⇒ khoá mới ⇒ đốt trần 100k ký tự/ngày ⇒ tới lúc vào Hộp thư thì **hết ngân sách**, chữ tiếng Anh | R2.6; backlog `present/backlog-and-techdebt.md:23` | Giữ "dịch mọi màn tĩnh" nhưng thêm: (a) **chặn theo trận** — trong 3 s gần nhất > 50 % ô lưới đổi ⇒ chế độ trận, không gửi/không dịch tới khi đứng yên liên tục ≥ 1,5 s; (b) dòng chỉ có chữ hoa ≤ 3 từ hoặc ≤ 4 ký tự ⇒ không gọi LLM (chỉ glossary/cache); (c) in số ký tự đã dùng mỗi 10 phút. Ghi rõ với User: màn khác Hộp thư "chạy được, chưa cam kết đẹp" |
| N5 | 6 · tràn màu khi chữ Việt dài | D7, D11 (2)(4) | Màu che lấy quanh **bbox gốc** (engine). Vùng che lại mở theo **chữ Việt** (TV, nới +15 %). Chữ Việt dài vượt mép vệt chọn/ô ⇒ mảng màu vệt chọn tràn sang nền khác | — | Engine gửi thêm `room` (hộp lớn nhất quanh bbox mà màu nền còn đồng nhất, dò từ khung). TV chỉ nới/xuống dòng trong `room`, thay luật "+15 % nếu không đè item khác" |
| N6 | 3 · thiếu quyền | Bước 5 | TV làm `ServerSocket` ⇒ **cần `android.permission.INTERNET`**, kể cả chỉ trong LAN. Manifest hiện có comment ràng buộc Phase 1 "KHÔNG INTERNET" — Dev 3b sẽ ngại thêm | `tv-overlay/app/src/main/AndroidManifest.xml:4-8` | Ghi rõ trong Bước 5: thêm `INTERNET`, cập nhật comment + README (lý do: socket LAN) |
| N7 | 6 · cỡ chữ đoán | D6 `fontPx` × 0,78 | Hệ số 0,78 là đoán ⇒ chữ Việt to/nhỏ hơn chữ game thấy ngay | — | Đưa hệ số vào `display.fontScale` (cấu hình) để chỉnh tối nay không cần build |
| N8 | 6 · kiểu che mặc định | Bảng *Mặc định* `coverMode: solid` | Mockup đề xuất `auto` (solid trên nền phẳng, card trên ảnh/gradient). `solid` trên nền ảnh = "miếng dán" | mockup report §5 | Mặc định `auto`: độ lệch màu vành lấy mẫu (D7) > ngưỡng ⇒ `card` |
| N9 | 6 · trễ cố ý | `holdMs` mặc định 150 | Độ trễ ở mockup X1 là **giới hạn vật lý** để User cảm nhận, không phải tính năng. Giữ chữ cũ thêm 150 ms là **cố ý** để chữ ma nằm sai dòng lâu hơn | mockup report X1 | Mặc định `holdMs = 0` |
| N10 | 4 · đúng D9 | D9 | Đã kiểm với docs Anthropic (đọc 2026-11-17): `OutputConfig { Effort, Format = JsonOutputFormat{Schema} }` đúng; `effort` hỗ trợ `claude-sonnet-5-5`; structured output **stable** cho `claude-sonnet-5-5`, không cần beta header. Cần ghi rõ ba điểm: (a) schema mọi object phải có `additionalProperties:false`; `short_vi` tuỳ chọn = bỏ khỏi `required`. (b) Prompt cache tối thiểu **512 token** với Sonnet 5.5; ngắn hơn thì **không cache, không báo lỗi** ⇒ Bước 7 kiểm `cache_read_input_tokens > 0` có thể "trượt giả" nếu system + glossary < 512 token; glossary phải xếp **ổn định** (cùng thứ tự) mới trúng cache. (c) Sonnet 5.5 mặc định có suy nghĩ thích ứng, tính vào `MaxTokens`; 8000 đủ, nhưng trễ câu mới có thể vài giây | https://platform.claude.com/docs/en/build-with-claude/structured-outputs · …/effort · …/prompt-caching | Ghi (a)–(c) vào D9; Bước 7 in `usage` cả hai lô |
| N11 | 3 · test | Kế hoạch TDD | Test cache/trần/usage không được ghi vào `%LOCALAPPDATA%` thật | — | Đường dẫn gốc là tham số (thư mục tạm trong test) |
| N12 | 1 | D12 dùng `alt` cho **cả nhóm** | C2 (mockup Vòng 2): bản ngắn **trước cho nhãn thiếu khe**, rồi mới co cả nhóm — không đổi cả hàng sang bản ngắn | mockup report Vòng 2 §C2 | Sửa D12 cho khớp C2 |

**Đạt, không cần sửa**:
- **Thứ tự `addView` → `startForeground`**: khớp code (`OverlayService.kt:39-42`).
- **`CoordinateMapper`**: hàm thuần, kẹp [0,1], dùng lại được (`CoordinateMapper.kt`).
- **NB8/NB9/tạo lại frame pool**: đúng nợ backlog mục 1.
- **Token LAN**: mã 8 ký tự từ bộ 22 ký tự, TV làm server, engine client ⇒ không mở firewall. Khoá API chỉ từ biến môi trường, không log.
- **Font** Be Vietnam Pro.
- **C3**: có trong D13.
- **Worker không gọi API thật**: Leader gọi ≤ 2.000 ký tự ở Bước 7.
- **Miễn mockup**: chấp nhận được với ủy quyền nguyên văn và cấu hình hoá lựa chọn hiển thị. Các mục N7–N9 làm cho cấu hình đó đủ dùng tối nay.

## Trả lời 4 câu của brief

1. **"Dịch mọi màn tĩnh, chỉ kiểm chất lượng Hộp thư"** là **cái bẫy, nếu không có N4**: lớp phủ hiện giữa trận (trái R2.6), và rác OCR đốt hết trần ngày trước khi tới Hộp thư. Có N4 thì chấp nhận được.
2. **Chữ ký khung + `stableMs` chưa đủ để hết nhấp nháy**: cần chữ ký theo ô lưới + mặt nạ ô nền động (N3). Nếu không, nền động gây nhấp nháy liên tục, còn vệt chọn đổi dòng lại không được nhận ra.
3. **Lấy mẫu màu (D7) đúng nguyên lý cho vệt chọn**, với hai điều kiện: phải được chạy lại khi vệt chọn dời (N3), và vùng che không được tràn ra ngoài vùng màu đồng nhất (N5).
4. **D9 chạy được như viết**: tên tham số đúng với docs hiện hành. Ba bẫy âm thầm ở N10: thiếu `additionalProperties:false` thì bị lỗi 400; prompt dưới 512 token thì không cache mà không báo; suy nghĩ thích ứng làm chậm câu mới.

## Mục mở — 5 phút đầu trên TV thật, cái gì làm User nói "xấu / không chơi được" mà doc chưa chặn

1. **Mỗi lần bấm ↑↓ trong Hộp thư, cả lớp chữ tắt rồi hiện lại sau ~0,5–1 s.** Đây là chớp đều đặn theo từng phím. Doc chấp nhận vì là thiết kế mockup, nhưng User chưa từng xem mockup trên TV. **Đề xuất**: chỉ xoá các item nằm trong ô lưới đã đổi (N3), giữ nguyên tiêu đề, tab, khung xem trước không đổi ⇒ khi chuyển vệt chọn, chỉ 2 dòng chớp chứ không cả màn.
2. **Hàng danh sách bị dịch thành một câu dài trùm 3 cột** (N1) — lỗi đập vào mắt ngay trên màn duy nhất được cam kết.
3. **Chữ Việt to/nhỏ lệch hẳn chữ game** (N7) và **mảng `solid` như miếng dán trên nền gradient/HDR** (N8): không chỉnh được tối nay nếu không có cấu hình.
4. **Lớp chữ hiện chớp trên HUD khi trận đấu dừng** (N4): người cùng xem sẽ nói ngay.
5. **Buổi đầu toàn câu mới**: Sonnet trả lô trong vài giây, Hộp thư hiện tiếng Việt lốm đốm. Seed 80–150 câu giảm được phần nào.

**Leader chưa hỏi**: chưa có **F1/F3 thật** nên mọi ngưỡng (diff khung, gom đoạn, `fontPx`) đều là đoán. Phải làm các ngưỡng này **chỉnh được bằng cấu hình** và **in log** để tối nay chỉnh tại chỗ. Ngoài ra, nên chụp ngay `engine capture` vài khung Hộp thư thật đầu buổi tối, chạy `replay` trước khi bật `run`.

## Duyệt lại Rev 1.1 (Leader chép từ kết quả cuối của SA — file trên đĩa không có mục này dù SA báo đã nối)

**BLOCKER (1)**. B1, B2 đã đóng. Mới: **D1** `fontScale` vừa nằm trong `display` gửi TV vừa được engine nhân vào `fontPx` ⇒ Dev 3b có thể nhân hai lần (≈ 0,61× cỡ đúng). Sửa một dòng: TV không dùng `fontScale` hoặc bỏ khỏi `display`. NOTE: **D2** `id` băm theo vị trí làm tròn ⇒ OCR lệch vài px đổi `id`, TV fade lại item không đổi — làm tròn theo ô lưới. **D3** lô 40 câu bị bỏ làm câu tốt mất lượt thử — tách đôi lô khi thử lại. Không vi phạm mới `CLAUDE.md`; thân doc còn chỗ cũ nhưng bảng Rev 1.1 đè rõ.

**Leader** (Rev 1.2): bỏ `fontScale` khỏi `display` (đúng cách SA chỉ, một dòng) + áp D2, D3. Không gửi SA vòng 3: thay đổi là đúng nguyên văn đề xuất sửa của SA, không mở phạm vi mới.
