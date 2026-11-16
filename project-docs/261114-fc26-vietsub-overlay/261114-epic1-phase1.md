# 261114 — Epic 1 — Phase 1: Chuẩn bị & khử rủi ro

📋 **[Epic 1](261114-epic1.md)** · **[Overview](261114-overview.md)**
**Revision**: Rev 2.2 — 2026-11-15 — Leader bổ sung theo [Dev feedback lượt 1](reviews/261114-epic1-phase1-dev-feedback.md) (loại "thiếu chi tiết kỹ thuật" — 04-implement: Leader tự bổ sung, không đổi phạm vi): mục **Quyết định sau phản hồi Dev** ngay dưới đây là **ràng buộc** cho lượt code, đè lên chỗ mơ hồ ở thân doc; sửa image AVD (`google-tv` x86), lệnh `screencap` (PowerShell 5.1 làm hỏng PNG).
· Rev 2.1 — 2026-11-15 — SA PASS ở duyệt lại Rev 2; áp 6 NOTE delta của SA (không đổi phạm vi): D1 (5/5 từ), D2 (thứ tự đọc golden), D3 (cửa sổ thay thế Win32 chuẩn), D4 (ngoại lệ tải phụ thuộc build), D5 (đường dẫn evidence), D6 (`.gitignore` lên Bước 1) + khớp (a) theo biên từ.
· Rev 2 — 2026-11-15 — theo [SA review](reviews/261114-epic1-phase1-sa-review.md): **B1** định nghĩa cách chấm OCR + TDD `ocr-score`; NOTE N1 (cắt vùng client trước dò viền), N2 (`CreateFreeThreaded`), N3 (DPI `PerMonitorV2`), N4 (đủ khai báo `specialUse`), N5 (không `POST_NOTIFICATIONS`), N6 (`WindowMetrics` API 30), N7 (fallback miễn pin), N8 (Gradle wrapper + `JAVA_HOME`/`ANDROID_HOME`), N9 (cách gọi `engine`), N10 (đường dẫn evidence), N11 (fixture lớn/riêng tư không commit), N12 (Reviewer soát golden), N13 (F1 chính lấy từ `engine capture`), N14 (deliverable Q1 + fixture), N15 (AVD API 31 + `-accel-check`), N16 (lớp phủ kiểm tra tự tắt), N17 (điều kiện miễn mockup), N18 (`*.local.json` gitignore), N19 (Dev không thao tác PS Remote Play), N20 (smoke bật lớp phủ tất định), N21 (tên file font) + Mục mở (ghi điều kiện hình ảnh khi chụp TV; Bước 7 không bị Q1 chặn; bẫy chữ ký debug).
· Rev 1 — 2026-11-15

---

## Quyết định sau phản hồi Dev (Rev 2.2 — ràng buộc)

Mã G/1.2/1.3 theo [dev feedback](reviews/261114-epic1-phase1-dev-feedback.md). Đề xuất của Dev được **chấp nhận nguyên văn** trừ chỗ ghi khác:

| # | Quyết định |
|---|---|
| G1 | `--frames N` = lấy mẫu N lần cách `--interval-ms` (mặc định 500) từ **khung mới nhất**; không có khung nào sau `--timeout-ms` (mặc định 5000) ⇒ lỗi rõ, exit ≠ 0. Ghi `capturedAtUtc` mỗi khung |
| G2 | Chỉ cửa sổ `IsWindowVisible` và không cloaked; khớp ≠ 1 ⇒ lỗi + liệt kê (HWND, tiêu đề, kích thước), exit ≠ 0; thêm `--hwnd` |
| G3 | Hằng số có tên: luma ≤ 24 là "đen", hàng/cột nội dung khi ≥ 1% điểm sáng, 16:9 dung sai ±1,5%. Đặt trước, không chỉnh theo dữ liệu chưa thấy; chỉnh sau khi có F1 ⇒ ghi `chưa kiểm`. `confident` = "tìm được rect hợp lý **và** gần 16:9" (ghi rõ trong README) |
| G4 | Biên từ = ký tự liền kề không phải `char.IsLetterOrDigit` hoặc đầu/cuối chuỗi |
| G5 | Kẹp **hai góc** về [0,1] rồi tính lại w,h ≥ 0 |
| G6 | Schema `capture.json` / `<ảnh>.ocr.json` / `<ảnh>.golden.json` theo đề xuất Dev; là **output chẩn đoán của Phase 1**, Phase 3 **không** đọc như contract (giao thức PC → TV là `OverlayFrame`, chốt ở Phase 3). Ghi schema vào `engine/README.md`. "golden" = **ground truth chép tay** (ghi rõ trong README) |
| G7 | Golden = mọi đoạn tiếng Anh **đọc rõ** trên ảnh (cả thanh trên, nhãn nút), bỏ ký hiệu icon; đoạn không đọc chắc ⇒ `golden/_unreadable.md`, không vào golden |
| G8 | Ảnh tổng hợp (ffmpeg `drawtext`) ở `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/dev/synthetic/`, golden = chuỗi đã vẽ; số trên đó ghi rõ **"không phải R-D"** |
| G9 | `engine/Engine.slnx`, `engine/src/Engine/`, `engine/tests/Engine.Tests/`, `vn.vietsub.ps5.tv.MainActivity`. Cửa sổ thay thế = **`ffplay`** (có thật ở `C:\ffmpeg-n8.1.2-win64-gpl-8.1\bin`) — không cần WinForms |
| G10 | Chọn cặp Gradle/AGP/Kotlin theo **ma trận tương thích chính thức** với JBR 25 (đối chiếu nguồn, ghi vào README); `compileSdk 35`. Không khớp ⇒ ghim bản thư viện cũ hơn; vẫn không được ⇒ **dừng, báo Leader**, không tự cài JDK khác. Cần `platforms;android-36` ⇒ được cài (Leader ủy quyền trong phạm vi cài công cụ của User) |
| G11 | Đỉnh L đúng góc, hai cạnh vẽ vào trong View; lưới = đường ở 10%, 20%…, nét mảnh, đục 30% |
| G12 | Thêm `meanLuma` vào `capture.json` — chấp nhận |
| 1.2 #4 | `playtest/`: **commit** ảnh/JSON evidence; script đã bị `project-docs/.gitignore` loại (`*/playtest/**/*.ps1|*.sh|*.py…`) — không cần thêm |
| 1.2 #5 | DPI: gọi `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)` ở **dòng đầu `Main`**, in kết quả ra log; kiểm bằng smoke 3. Bỏ gợi ý `ApplicationHighDpiMode` (chỉ WinForms) |
| 1.2 #6 | Smoke 8: `uiautomator dump` **3 lần** — trước cấp quyền · sau `appops allow` + mở lại · sau `dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (chỉ trên emulator của vai) |
| 1.2 #7 | Thêm extra **debug-only** `--el auto_off_ms <số>` để smoke chứng minh tự tắt (~10 s); release không có |
| 1.2 #8 | Bản debug log `Paint.hasGlyph()` của Be Vietnam Pro cho dải U+1EA0–U+1EF9 + chuỗi mẫu; đưa vào evidence |
| 1.3 | F1 **không chặn code** Bước 3 — chỉ chặn **số đo** R-D. Thêm `.gitkeep` cho thư mục fixture được commit; ghi README rằng `_local/` không có trên bản clone |
| D3D11 | Dùng **`Vortice.Direct3D11` + `Vortice.DXGI` (MIT)**; tự khai báo chỉ phần interop WinRT (`IGraphicsCaptureItemInterop`). Ghi giấy phép vào README |
| xUnit | **v2** (2.9.x) như Dev đề xuất — tránh cấu hình Microsoft.Testing.Platform của v3 |
| Lượt code | **Tách**: lượt 2a = Bước 1–3 (`engine`), lượt 2b = Bước 4–5 (`tv-overlay`). Mỗi lượt có report + mục "chưa chứng minh được" riêng; Reviewer review sau mỗi lượt |
| Kẹt interop | Kẹt WGC/COM > 1 ca ⇒ dừng, báo Leader; **không** tự đổi sang `GraphicsCapturePicker` |
| (b) không phụ thuộc thứ tự — Rev 2.3 | Từ [dev report 2a §6.1](reviews/261114-epic1-phase1-dev-report-2a.md): Windows OCR trả dòng **theo cột**, golden theo hàng ⇒ LCS làm (b) thấp giả. **Đổi định nghĩa (b)**: độ phủ từ theo **đa tập hợp** (mỗi từ golden được khớp tối đa một lần với một từ OCR cùng ảnh, không xét thứ tự); bảng TDD `ScoreOcr` cập nhật số (b) theo định nghĩa mới. Chỉ số chính (a) giữ nguyên. Dev sửa ở vòng sau review 2a |
| Bước 7 chụp F1 — Rev 2.3 | `--frames 1` cho **mỗi trạng thái màn** (màn tĩnh cho N ảnh y hệt). Khi chép vào `f1-remoteplay/` **đổi tên ngay** theo nội dung (`inbox-list-top.png`, `inbox-list-mid.png`, `email-contract.png`…) **trước** khi viết golden (`ocr-score` ghép theo tên). Ảnh đen ⇒ chạy chẩn đoán `meanLuma` + `--no-crop true` trước khi kết luận "engine không chụp được" (cửa sổ render GPU có thể ra đen dưới WGC — đã gặp với `ffplay`); chuẩn bị `--hwnd` nếu tiêu đề khớp ≥ 2 cửa sổ |
| Sau review 2a — Rev 2.4 | Từ [review 2a](reviews/261114-epic1-phase1-review-2a.md) (PASS, non-blocking). **Dev sửa ở vòng sửa 2a**: **NB2** — quy tắc dự phòng: không `confident` mà vùng client gần 16:9 (dung sai G3) ⇒ dùng **cả client** làm rect nội dung, `confident=true`, ghi `fallback:"client"` trong `capture.json`/`ocr.json`; ngược lại giữ cờ `false` và **không** chuẩn hoá theo rect không confident (dùng cả ảnh) — thêm ca TDD "nền tối đồng màu, chữ ở giữa, không chi tiết ở mép" · **NB3** — (a) tính **bội số**: đoạn golden xuất hiện k lần cần ≥ k lần xuất hiện riêng biệt (không chồng lấn) trong văn bản OCR của ảnh; thêm ca TDD "Board ×2, OCR đọc lần hai B0ard ⇒ 1/2" · **NB6** — (b) đa tập hợp (Rev 2.3) · **NB4** — `frame.ContentSize` ≠ kích thước pool ⇒ lỗi rõ, exit 6 (không trả ảnh cắt cụt) · **NB7** — `--hwnd` không hợp lệ và thư mục `ocr-score` không có ⇒ exit 1 có thông điệp · **NB10** — bỏ `AllowUnsafeBlocks` · **NB5** — README ghi rõ `capturedAtUtc` là thời điểm lấy mẫu. **Để nợ Phase 3** (ghi `backlog-and-techdebt.md` ở Bước 8): NB8 (giải phóng RCW/`winrtDevice`), NB9 (kiểm HRESULT DWM/GetClientRect), tái tạo frame pool khi đổi cỡ. **Bước 7 (NB1)**: ghi scale Windows; scale ≠ 100% ⇒ chụp thêm một lần `--no-crop true` để so `clientOffset`; dặn User **không đổi cỡ cửa sổ** khi đang chụp; `confident=false` ⇒ xem PNG trước khi kết luận R-C |
| Sau re-review fix 2a — Rev 2.5 | Từ [re-review](reviews/261114-epic1-phase1-review-2a.md) (PASS). **Xác nhận** lệch Dev tự khai: thêm điều kiện `LooksLikeBars` cho quy tắc dự phòng NB2 (giữ ca TDD "Không ra 16:9") và `ocr.json.contentRect` = **rect đã dùng**. **Bước 7 sửa quy tắc**: xem PNG khi `confident=false` **hoặc** `fallback` ≠ `null` (Hộp thư FC 26 nền tối gần như chắc ra `fallback:"client"`); kiểm R-C bằng `capture.json` + PNG, **không** bằng `ocr.json`; gặp exit 6 (đổi kích thước) ngay khung đầu ⇒ chờ stream ổn định rồi chạy lại `--frames 1`, không kết luận engine hỏng. Nợ Phase 3 thêm: dòng "OCR gần nhất" in sai cho đoạn trùng không khớp (chỉ hiển thị) |
| Mối nối toạ độ (Mục mở Dev) | Thêm vào **Bước 6.3 + Bước 7**: User đứng ở **cùng một màn tĩnh** (vd Hộp thư, không cuộn) chụp TV bằng điện thoại (F4) **và** Leader chạy `engine capture` cùng lúc; Leader so vị trí chuẩn hoá của 2–3 mốc rõ (góc khung, đầu dòng tiêu đề) giữa hai ảnh — lệch > 1% ⇒ ghi vào log, đưa vào Phase 3. Ghi kèm F1: **độ phân giải stream** trong cài đặt PS Remote Play, kích thước cửa sổ, mức scale Windows |

---

## Hiện trạng

> ⚠️ Cập nhật 2026-11-15 (sau Rev 2.1): U1, U2 **đã xong** — .NET SDK 10.0.404, Android Studio + JBR 25.0.4, platform-tools, emulator (WHPX), `platforms;android-35`, `build-tools;35.0.0`, image `google-tv` x86 API 31 — không nằm trong PATH, đường dẫn ở [`automation/README.md`](../automation/README.md). Còn tạo AVD (Dev).

- Repo chỉ có quy trình + tài liệu, **chưa có module code** (`git ls-files` không có `engine/`, `tv-overlay/`).
- Máy dev (Leader probe 2026-11-15, workflow log): Windows 11 Pro build **26100** · laptop · `dotnet --list-sdks` = **chỉ 8.0.121** · OCR `AvailableRecognizerLanguages` = **`en-US`** · **không có** `adb`, `java` · có `ffmpeg` trên PATH · GPU AMD Radeon 780M, màn 2880×1800.
- Thiết bị thật: TV Sony 65" Google TV (model + Android TV OS chưa biết), PS5, cùng LAN `192.168.1.x`. Chưa bật gỡ lỗi trên TV.
- Discovery đã verify (Leader): cơ chế vẽ đè = `SYSTEM_ALERT_WINDOW` (+ `adb shell appops set <pkg> SYSTEM_ALERT_WINDOW allow` khi TV không có màn cấp quyền) + miễn tối ưu pin để TV không giết app khi chuyển sang HDMI; WGC cửa sổ PS Remote Play chụp được (BitBlt ra đen); `TryCreateFromWindowId` cần package manifest ⇒ dùng interop `IGraphicsCaptureItemInterop::CreateForWindow`; .NET 8 hết hỗ trợ 2026-11-10 ⇒ .NET 10.
- 🔴 **Claim đã verify là SAI**: "Console Translator chạy toàn bộ trên TV" — sai, app TV của nó **chỉ hiển thị**; đừng dựa vào nó để kỳ vọng app TV tự nhận hình (draft §2 đính chính).

## Mục tiêu Phase

Khử **trước khi làm đẹp** bốn rủi ro chỉ lộ trên máy/thiết bị thật, mỗi cái kết thúc bằng **số đo hoặc ảnh**, không bằng lời:

| # | Rủi ro | Bằng chứng khi xong |
|---|---|---|
| R-A | TV Sony cho app của mình vẽ đè lên nguồn HDMI PS5 và không giết app khi chuyển nguồn | Ảnh điện thoại chụp TV: 4 dấu góc + chữ Việt mẫu nổi trên hình PS5 |
| R-B | Toạ độ lớp phủ khớp vùng hình game (overscan/zoom TV) | 4 dấu góc nằm đúng 4 góc hình PS5 trên ảnh chụp |
| R-C | Chụp được cửa sổ PS Remote Play trên laptop, không đen, đúng khung 16:9 | PNG chụp từ cửa sổ PS Remote Play thật + vùng hình đã dò viền |
| R-D | OCR đọc được chữ Hộp thư FC 26 trên ảnh đã qua nén stream | Tỉ lệ đoạn chữ đúng nguyên văn trên fixture F1 (có số) |

Cùng lúc chốt hai câu hỏi tài khoản/thiết bị chỉ User trả lời được (Q1 tay cầm, Q3 model TV) và thu fixture cho Phase 2 (mockup).

**Không phải lần cài thử chính thức**: APK ở Phase này chỉ là công cụ kiểm tra (vẽ dấu góc + chữ mẫu), User đã đồng ý lệch nhẹ ô 5 (Epic 1, 2026-11-15). Chưa dịch gì, chưa gọi LLM.

## Prerequisites

User làm trước khi Dev bắt đầu phần tương ứng (Leader gửi danh sách này cho User, User báo xong từng mục):

| # | Việc (User) | Chặn bước Dev nào |
|---|---|---|
| U1 | Cài **.NET 10 SDK** (x64) — kiểm: `dotnet --list-sdks` có dòng `10.0.x` | Bước 1–3 |
| U2 | Cài **Android Studio** (kèm Android SDK, `adb`, JDK đi kèm — JBR) + SDK Platform **API 35** + system image **Google TV x86 API 31** — `system-images;android-31;google-tv;x86` (API 31 không có bản x86_64; Android TV 12 — gần TV đích nhất) — **đã cài 2026-11-15**. Kiểm: `adb --version`, `java -version` (JBR), `emulator -accel-check` báo có tăng tốc (CPU AMD cần **Windows Hypervisor Platform** bật). Không bắt buộc đặt biến môi trường máy — Dev đặt `JAVA_HOME` (JBR của Android Studio) và `ANDROID_HOME` (`%LOCALAPPDATA%\Android\Sdk`) **trong tiến trình shell của mình** | Bước 4–5 |
| U3 | Ghi **model TV** + **phiên bản Android TV OS** (TV: Cài đặt → Hệ thống → Giới thiệu) — gửi Leader | Chốt `minSdk` (mặc định 29) |
| U4 | Cài **PS Remote Play** trên laptop, bật Remote Play trên PS5; **thử Q1**: đang chơi FC 26 bằng DualSense, kết nối Remote Play từ laptop **cùng tài khoản** ⇒ ghi: tay cầm có bị ngắt không; laptop có hiện hình khi **không** cắm tay cầm vào laptop không | Bước 7 (chụp thật) — **không** bị chặn bởi kết quả Q1 (chụp được khi không cầm tay cầm). Tay cầm bị ngắt ⇒ chỉ **Phase 3** bị chặn: Leader trình User quyết tài khoản PSN phụ (Risk gate) trước Phase 3 |
| U5 | Chụp **fixture** theo [discovery R2.4.1](261114-discovery.md) (FC 26 ngôn ngữ English, save Career Mode có ≥ 5 email, ≥ 1 email dài). **F1 chính lấy từ `engine capture` ở Bước 7** (đúng thứ engine nhận: có/không thanh tiêu đề, DPI, định dạng điểm ảnh); ảnh `Win+Shift+S` chỉ là dự phòng khi Bước 7 chưa chạy được. **F2** (nút Create, PNG), **F3** (video), **F4** (ảnh TV bằng điện thoại). Vị trí & commit: xem *Dữ liệu fixture* ở Blackbox Verify | Bước 3 (đo OCR) · Phase 2 (mockup) |
| U6 | Bật **chế độ nhà phát triển + gỡ lỗi qua mạng** trên TV (Cài đặt → Hệ thống → Giới thiệu → bấm Bản dựng 7 lần → Tuỳ chọn nhà phát triển → Gỡ lỗi USB/ADB qua mạng), ghi IP TV vào `project-docs/automation/README.md` | Bước 6 |
| U7 | (khuyên) Đặt **IP cố định** cho TV và laptop trên router | Không chặn Phase 1 |

## Files bắt buộc đọc trước khi code

- [`CLAUDE.md`](../../CLAUDE.md) — *Ràng buộc đặc thù* (font, LAN, không đụng HDMI) và *Risk gate riêng* (mọi thao tác trên TV thật do User chạy).
- [`261114-discovery.md`](261114-discovery.md) §3.5 (cơ chế overlay + cấp quyền), §3.6 (nguồn sai lệch toạ độ), R2.2–R2.3 (stack đã chốt, WGC interop, OCR language, TFM, `minSdk`) — để không chọn lại thứ đã loại (`TryCreateFromWindowId`, OkHttp, Compose cho overlay, SQLite).
- [`workflow/guidelines/blackbox-android-adb.md`](../../workflow/guidelines/blackbox-android-adb.md) §2 (tạo/chạy/tắt AVD headless, serial `emulator-5556`), §7 (bật emulator phải tách tiến trình — không thì treo tool call), §8 (bẫy) — Bước 5.
- [`workflow/guidelines/tdd.md`](../../workflow/guidelines/tdd.md) — Bước 2 (hàm thuần ánh xạ toạ độ + dò viền).
- https://github.com/kyozxx/PS-Translator/blob/main/TV.md — cách một app thật xin quyền overlay + miễn pin trên Android TV (tham khảo hành vi, **không** chép mã).

## Kế hoạch

### Tổng quan

Dựng hai module tối thiểu và ba công cụ kiểm tra: (1) `engine` chụp được cửa sổ bất kỳ theo tiêu đề và lưu PNG; (2) `engine` OCR một thư mục ảnh ra JSON đoạn chữ + toạ độ chuẩn hoá, có số đo so với golden; (3) APK `tv-overlay` khi mở thì vẽ lớp phủ trong suốt gồm **4 dấu góc tại (0,0) (1,0) (0,1) (1,1) của màn**, lưới 10% mờ, và **một dòng chữ Việt mẫu** bằng Be Vietnam Pro, chạy dưới foreground service, có nút xin quyền overlay + miễn pin + bật/tắt. Xong Phase: APK được cài lên TV thật để kiểm R-A, R-B (User chạy); laptop chụp cửa sổ PS Remote Play thật (R-C); có số OCR trên fixture (R-D). Chưa có kết nối PC ↔ TV.

### Bước 1: Dựng `engine/` (code)

**Files**: `engine/` — solution `.NET 10`: dự án console `Engine` (TFM `net10.0-windows10.0.22621.0`) + dự án test `Engine.Tests` (xUnit) · `engine/README.md`.
**Thay đổi**:
* Lệnh con theo dạng `engine <lệnh> [tham số]`: `capture`, `ocr`, `ocr-score` (Bước 3). Không dùng thư viện CLI ngoài nếu `args` thủ công đủ gọn.
* **Cách gọi chuẩn** (dùng đúng dạng này trong smoke và README): `dotnet run --project engine/src/Engine -- <lệnh> [tham số]`, chạy từ **root repo**. Trong doc này, `engine <lệnh>` là viết tắt của dòng đó.
* Process khai **DPI-aware `PerMonitorV2`** (app manifest `dpiAwareness`, hoặc `ApplicationHighDpiMode`) — laptop 2880×1800 chạy scale > 100%; không khai thì `GetWindowRect`/`GetClientRect` trả toạ độ ảo hoá lệch với khung WGC (N3).
* README module ghi: cách build/chạy, TFM và lý do, gói NuGet đã thêm + **giấy phép từng gói**, bẫy WGC (cửa sổ thu nhỏ thì không có khung; viền vàng khi chụp là bình thường; HD color có thể không phải BGRA8; khung WGC theo cửa sổ **gồm cả thanh tiêu đề/viền**; console app phải dùng `CreateFreeThreaded`), bẫy DPI.
* Gitignore đã phủ `bin/ obj/` ở root — kiểm `git status` không lọt artifact build.
* **Ngay ở bước này** (D6): thêm vào root `.gitignore` `*.local.json` và `engine/tests/fixtures/**/_local/`, rồi tạo các thư mục fixture (`f1-remoteplay/`, `f2-ps5/`, `f3-frames/`, `golden/`, `_local/f3-video/`, `_local/f4-tv-photo/`) — trước khi User đặt F3/F4 vào, để video lớn/ảnh nhà User không lọt vào commit.
**Logic**: không có — khung module.

### Bước 2: Hàm thuần toạ độ (code, TDD)

**Files**: `engine/src/.../Geometry/*` + test tương ứng.
**Hai việc khác nhau, đừng gộp** (N1): (1) **cắt vùng client** của cửa sổ — hình học Windows, làm ở Bước 3 lúc chụp (bỏ thanh tiêu đề/viền); (2) **dò viền đen** trong hình stream (letterbox của Remote Play) — hàm thuần dưới đây, **chỉ nhận ảnh đã cắt client**.

**Logic** — hai hàm thuần, **extract riêng, TDD bắt buộc**:
* `DetectContentRect(image)`: đầu vào là ảnh **đã cắt về vùng client**. Từ ảnh BGRA (mảng byte + rộng/cao/stride), tìm hình chữ nhật nội dung bỏ viền đen/gần đen bao quanh (ngưỡng độ sáng + tỉ lệ điểm sáng tối thiểu mỗi hàng/cột), trả về rect pixel. Ưu tiên kết quả gần tỉ lệ **16:9**; nếu không tìm được rect hợp lý ⇒ trả cả ảnh và cờ `confident=false`.
* `Normalize(boxPx, contentRect)` → box 0..1 theo vùng nội dung; giá trị kẹp trong [0,1].
**Edge case**: ảnh không viền · viền trên/dưới (letterbox) · viền trái/phải (pillarbox) · màn game tối gần đen ở mép (không được cắt nhầm vào nội dung) · ảnh toàn đen.

### Bước 3: Spike chụp cửa sổ + OCR có số đo (code)

**Files**: `engine/src/.../Capture/*`, `engine/src/.../Ocr/*`.
**Thay đổi**:
* `engine capture --window-title <chuỗi con> --frames N --out <thư mục>`: tìm HWND cửa sổ top-level có tiêu đề chứa chuỗi (không phân biệt hoa thường), tạo `GraphicsCaptureItem` qua interop `CreateForWindow` (**không** `TryCreateFromWindowId`), chụp N khung bằng frame pool tạo bằng **`Direct3D11CaptureFramePool.CreateFreeThreaded`** (console app không có `DispatcherQueue` — `Create` thì `FrameArrived` không bao giờ bắn; N2). **Cắt về vùng client**: offset = `ClientToScreen(GetClientRect)` so với `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` (cùng hệ toạ độ pixel thật nhờ `PerMonitorV2`), rồi mới `DetectContentRect`. Lưu PNG **đã cắt client** + `capture.json` (kích thước khung WGC, rect client trong khung, rect nội dung trong ảnh đã cắt, thời điểm). Interop D3D11/COM: tự khai báo hoặc dùng thư viện — **ghi giấy phép vào README**. Không tìm thấy cửa sổ / cửa sổ thu nhỏ ⇒ báo lỗi rõ, exit ≠ 0.
* `engine ocr --input <thư mục PNG> --out <thư mục>`: với mỗi ảnh — `DetectContentRect`, OCR bằng `Windows.Media.Ocr` `en-US` (`TryCreateFromLanguage` trả `null` ⇒ lỗi rõ ràng, exit ≠ 0), xuất `<ảnh>.ocr.json`: mảng dòng `{ text, boxPx, box01 }` (box01 qua `Normalize`). Ảnh lớn hơn `OcrEngine.MaxImageDimension` ⇒ thu nhỏ có ghi lại tỉ lệ, toạ độ quy về ảnh gốc.
* **Golden + số đo (R-D)**: Dev tạo `engine/tests/fixtures/fc26/inbox/golden/<ảnh>.golden.json` cho các ảnh **F1** = mảng **đoạn** chữ đúng nguyên văn đọc từ ảnh (Dev đọc ảnh, ghi tay). **Đoạn** = một khối chữ liền mạch mà người đọc coi là một đơn vị (một ô của một cột trong hàng danh sách: người gửi / tiêu đề / ngày là **3 đoạn riêng**; một dòng trong nội dung email là một đoạn). Golden do Dev tạo phải được **Reviewer soát độc lập** (N12, xem phần Reviewer).
* **Cách chấm `engine ocr-score --ocr <thư mục kết quả> --golden <thư mục golden>`** — hàm thuần `ScoreOcr(goldenSegments, ocrLines)`, **TDD bắt buộc** (B1):
  * **Chuẩn hoá** cả hai phía trước khi so: gộp mọi khoảng trắng liên tiếp thành 1 dấu cách, cắt hai đầu; **giữ** hoa/thường; **giữ** dấu câu và ký hiệu tiền tệ.
  * **Thứ tự golden** (D2): các đoạn trong file golden xếp theo **thứ tự đọc** — trên xuống dưới; trong một hàng, trái sang phải.
  * **(a) Tỉ lệ đoạn khớp** (chỉ số chính, ngưỡng 95%): một đoạn golden **khớp** nếu nó là **chuỗi con liên tục theo biên từ** (không bắt đầu/kết thúc giữa một từ) của một dòng OCR, **hoặc** của chuỗi ghép từ **2–3 dòng OCR liền nhau theo thứ tự** (nối bằng 1 dấu cách). Mỗi dòng OCR được dùng cho nhiều đoạn golden (một hàng danh sách bị OCR gộp vẫn khớp đủ 3 đoạn). Tỉ lệ = số đoạn khớp / tổng đoạn golden.
  * **(b) Tỉ lệ từ đúng** (tham khảo): tách từ theo dấu cách sau chuẩn hoá; đếm số từ golden xuất hiện **đúng thứ tự** trong chuỗi từ OCR của cùng ảnh (dãy con chung dài nhất theo từ); tỉ lệ = số từ khớp / tổng từ golden.
  * **In ra**: theo từng ảnh và tổng — số đoạn, (a) %, số từ, (b) %, danh sách đoạn **không** khớp kèm dòng OCR gần nhất (để đọc bằng mắt).
* Thử thêm **một** biến thể tiền xử lý rẻ (phóng 2× vùng ảnh trước OCR) và báo cả hai số (a), (b) cho cả hai biến thể. **Không** dựng PaddleOCR ở Phase này — chỉ đề xuất nếu (a) < 95% ở biến thể tốt hơn.
**Logic**: spike — mục đích là **số đo + ảnh**, không phải kiến trúc cuối; nhưng code đặt trong module, không phải script vứt đi, vì Phase 3 dùng lại.

### Bước 4: Dựng `tv-overlay/` (code)

**Files**: `tv-overlay/` — dự án Gradle Kotlin, một module app, `applicationId` đề xuất `vn.vietsub.ps5.tv` · `tv-overlay/README.md`.
**Thay đổi**:
* **Khung Gradle** (N8): repo chưa có wrapper, máy không có `gradle`. Dev tải **bản phân phối Gradle chính thức** (gradle.org, bản ổn định tương thích Android Gradle Plugin đã chọn) vào **scratchpad/thư mục tạm ngoài repo**, chạy `gradle wrapper --gradle-version <x>` trong `tv-overlay/` để sinh `gradlew`, `gradlew.bat`, `gradle/wrapper/*` — **commit** các file wrapper; không commit bản phân phối. Mọi lệnh Gradle chạy với `JAVA_HOME` = JBR của Android Studio và `ANDROID_HOME` = SDK, đặt **trong tiến trình shell** (không sửa biến môi trường máy); `local.properties` (`sdk.dir`) chỉ ở máy, gitignore.
* `minSdk 29` (đổi khi U3 có model + OS thật), `targetSdk 35`; manifest khai `leanback` launcher (`android.intent.category.LEANBACK_LAUNCHER`) + banner để hiện trên màn Home Google TV; `uses-feature android.software.leanback` `required=false`, `android.hardware.touchscreen` `required=false`.
* Quyền & FGS (N4, N5) — **chốt `specialUse`**, đủ 4 thứ: (1) `<service android:foregroundServiceType="specialUse">`; (2) quyền `FOREGROUND_SERVICE` + `FOREGROUND_SERVICE_SPECIAL_USE`; (3) trong `<service>`: `<property android:name="android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE" android:value="..."/>` mô tả ngắn (lớp phủ hiển thị bản dịch trên TV); (4) gọi `ServiceCompat.startForeground(service, id, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)` (hằng số từ API 34; `ServiceCompat` lo `minSdk 29`). Thêm `SYSTEM_ALERT_WINDOW`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`. **Không** khai/xin `POST_NOTIFICATIONS` ở Phase 1 (FGS vẫn chạy, chỉ ẩn thông báo). **Không** khai `INTERNET` ở Phase 1 (chưa nghe cổng).
* Font **Be Vietnam Pro** (OFL) — tải từ nguồn chính thức (Google Fonts / repo gốc), đặt `res/font/` với **tên file chữ thường + số + `_`** (vd `be_vietnam_pro_regular.ttf`, `be_vietnam_pro_semibold.ttf` — tên gốc có chữ hoa/gạch nối làm build lỗi; N21), chỉ nhúng độ đậm dùng tới, **kèm file LICENSE (OFL)** trong repo và ghi vào README.
* README ghi thêm bẫy **chữ ký debug**: APK debug ký bằng debug keystore của máy build — build ở máy khác thì `install -r` lên TV lỗi chữ ký (phải gỡ app cũ trước).
* `.gitignore` của module: `.gradle/`, `local.properties`, `build/`, `*.keystore`, `.cxx/`, `captures/` — kiểm `git status` không lọt.
* README ghi: build (`gradlew assembleDebug`), cài, cấp quyền (cả đường cài đặt và đường `adb shell appops`), miễn pin, bẫy đã gặp.

### Bước 5: APK kiểm tra lớp phủ (code)

**Files**: `tv-overlay/app/src/main/...`
**Thay đổi**:
* **Màn chính (Activity) tối giản — công cụ kiểm tra, không phải UI sản phẩm** (UI sản phẩm có mockup ở Phase 2). Miễn mockup **có điều kiện** (N17): (i) màn này **bị thay hoàn toàn** ở Phase 3 bằng màn theo mockup Phase 2 — không "nâng cấp dần"; (ii) chữ trên màn bằng **tiếng Việt**; (iii) trạng thái quyền **làm mới ở `onResume`** khi User quay lại từ màn cài đặt; (iv) focus D-pad mặc định vào **nút của việc tiếp theo chưa xong** (chưa có quyền overlay ⇒ nút cấp quyền; có rồi ⇒ nút miễn pin; xong ⇒ nút Bật).
* Nội dung màn: trạng thái quyền overlay (có/không), trạng thái miễn pin (có/không), kích thước cửa sổ đo được px + density — **`WindowMetrics` chỉ có từ API 30**, `minSdk 29` ⇒ dùng `androidx.window` `WindowMetricsCalculator` (N6) — và phiên bản Android; nút **Cấp quyền hiển thị trên ứng dụng khác** (`ACTION_MANAGE_OVERLAY_PERMISSION`), nút **Bỏ tối ưu pin** (`ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`), nút **Bật / Tắt lớp phủ**. **Cả hai intent bọc `try/catch` `ActivityNotFoundException`** (N7): không mở được ⇒ hiện ngay trên màn lệnh dự phòng — overlay: `adb shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`; miễn pin: `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (`chưa kiểm`).
* **Bản debug** nhận extra `--ez overlay_on true|false` qua `am start` để bật/tắt lớp phủ tất định trong smoke (N20) — bản release không có đường này.
* **Service** foreground (notification tối thiểu): khi bật — **thêm cửa sổ overlay trước, rồi mới gọi `startForeground`** (thứ tự an toàn với giới hạn FGS nền — discovery D7, R2.3); `onStartCommand` trả **`START_NOT_STICKY`** và **tự tắt sau 30 phút** (N16 — lớp phủ kiểm tra nằm trên mọi nguồn, người nhà bật TV không được thấy khung vàng mãi); cửa sổ `TYPE_APPLICATION_OVERLAY`, toàn màn, `MATCH_PARENT`, `PixelFormat.TRANSLUCENT`, cờ `FLAG_NOT_FOCUSABLE | FLAG_NOT_TOUCHABLE | FLAG_LAYOUT_NO_LIMITS` (+ cờ cần để phủ cả vùng cutout/thanh hệ thống nếu có).
* **View tự vẽ** (Canvas): nền trong suốt; 4 dấu góc hình chữ L màu nổi (vd vàng #FFD400, nét ~6 dp) tại 4 góc **theo toạ độ chuẩn hoá (0,0),(1,0),(0,1),(1,1) của kích thước View**; lưới 10% nét mảnh 30% đục; ô giữa màn: thẻ bo góc nền tối ~80% đục với dòng chữ Việt mẫu bằng Be Vietnam Pro, gồm chữ hoa có dấu và dấu chồng: `HỘP THƯ · CHUYỂN NHƯỢNG — Bản hợp đồng mới đã được gửi tới Ban lãnh đạo. ẾỖẶỮỴ ếỗặữỵ` ở 3 cỡ (24/32/44 sp); dưới thẻ in kích thước View (px); góc dưới phải một dòng chữ nhỏ: `VietSub — lớp phủ kiểm tra — tắt trong app` (N16). Lưu ý tên gọi: "4 góc" là 4 góc **mặt phẳng đồ hoạ Android** (thường 1920×1080 dù panel 4K) — việc nó trùng 4 góc **hình HDMI PS5** chính là điều Bước 6 kiểm, không phải điều được giả định.
* Hàm ánh xạ chuẩn hoá → pixel của View là **hàm thuần, TDD** (JUnit, chạy JVM).
**Edge case**: quyền chưa cấp ⇒ nút Bật báo rõ, không crash · tắt lớp phủ ⇒ gỡ cửa sổ + dừng service · service bị hệ thống giết ⇒ khi mở lại app, trạng thái hiển thị đúng.

### Bước 6: Kiểm trên TV thật (manual — **User chạy**, Leader đưa lệnh)

Risk gate riêng: cài APK, quyền, gỡ lỗi qua mạng trên TV đều do User. Dev **không** chạy lệnh nào tới IP TV. Leader đưa từng lệnh sau khi Dev + Reviewer xong:
1. `adb connect <ip-tv>:5555` (chấp nhận trên TV) → `adb -s <ip-tv>:5555 install -r <apk>`.
2. Mở app trên TV; cấp quyền qua nút; không có màn cấp quyền ⇒ `adb -s <ip-tv>:5555 shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`; bấm Bỏ tối ưu pin.
3. Bấm Bật lớp phủ → chuyển sang **nguồn HDMI PS5**, đứng ở màn Home PS5 rồi vào FC 26 → **chụp TV bằng điện thoại** (thẳng, cả màn). Ghi: dấu góc có nằm đúng 4 góc **hình PS5** không (R-B); chữ Việt đủ dấu, nét đều, đọc được từ chỗ ngồi (R-A + font). **Ghi kèm điều kiện hình ảnh** (dữ liệu cho Phase 2 chọn màu kiểu che): PS5 có bật **HDR** không, **chế độ ảnh TV** (Game/Standard…, có ALLM không), **độ phân giải + tần số** PS5 xuất ra (4K/1080p, 60/120 Hz).
4. Lệch ⇒ chỉnh TV: tắt overscan / chọn hiển thị toàn khung (Sony: mục kiểu *Screen size / Display area / Full pixel* — ghi tên mục thật vào `automation/README.md`), chụp lại.
5. Chuyển HDMI → Home → HDMI, để TV chờ 1 phút rồi bật lại: lớp phủ còn không (ghi kết quả — không phải tiêu chí PASS của Phase 1, là dữ liệu cho Phase 3).
6. (5 phút) Ở nguồn HDMI PS5: `adb -s <ip-tv>:5555 shell screencap -p /sdcard/hdmi.png` rồi `adb -s <ip-tv>:5555 pull /sdcard/hdmi.png` (**không** dùng `exec-out … > file` trong Windows PowerShell 5.1 — toán tử `>` làm hỏng PNG, dễ kết luận nhầm "HDMI đen") — đen hay thấy hình game (discovery U4); gửi Leader.
7. `adb -s <ip-tv>:5555 shell getprop ro.product.model`, `getprop ro.build.version.release`, `getprop ro.board.platform` — chép output vào `automation/README.md` (bổ sung U3).
8. Tắt gỡ lỗi qua mạng trên TV nếu không dùng tiếp (User quyết).

### Bước 7: Chụp cửa sổ PS Remote Play thật (manual — User mở app, Dev/Leader chạy `engine`)

Bước này **không** bị chặn bởi kết quả Q1 (chụp khi không cầm tay cầm). Làm **sớm** — ngay khi U4 xong — vì PNG ở đây là **F1 chính** (N13).
User mở PS Remote Play, kết nối PS5, vào Hộp thư FC 26, để cửa sổ không thu nhỏ; User tự điều hướng trong game. Dev/Leader **chỉ chạy `engine` đọc cửa sổ** — **không** click, gõ phím, đóng hay di chuyển cửa sổ PS Remote Play (N19). Chạy `engine capture --window-title "PS Remote Play" --frames 5 --out project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/leader/remoteplay` (tiêu đề cửa sổ thật xác nhận lúc chạy), lặp cho: danh sách Hộp thư (đầu/giữa/cuối), 3 email mở khác nhau, 1 khung có mục đang chọn ở dòng khác. Verify R-C: PNG có hình game (không đen), **không còn thanh tiêu đề**, `capture.json` có rect nội dung ~16:9. Chép các PNG đạt vào thư mục F1 (*Dữ liệu fixture*), rồi chạy `engine ocr` + `ocr-score`.

### Bước 8: Cập nhật tài liệu (docs)

* `engine/README.md`, `tv-overlay/README.md` (Bước 1, 4).
* `project-docs/automation/README.md`: IP TV, model/OS, cách đã cấp quyền overlay, tên mục hiển thị toàn khung của Sony, cài đặt PS5 liên quan, AVD đã tạo (tên, API).
* `CLAUDE.md` mục *Dự án*: phạm vi FC 26 = **Career Mode, chơi offline** (đang ghi sai "menu, Ultimate Team…" — discovery D10); mục *Cấu trúc repo*: bảng module có `engine/` (.NET 10, lệnh chạy), `tv-overlay/` (Kotlin, lệnh build), `mobile/` hạ xuống "chưa lên kế hoạch".
* `project-docs/present/backlog-and-techdebt.md`: ghi kết quả bước 6.5 (lớp phủ sau khi chuyển nguồn/chờ) nếu có hành vi xấu; ghi R6 discovery (lớp phủ vẽ đè cả khi TV ở nguồn khác) là nợ đã biết.
* Root `.gitignore`: đã thêm ở **Bước 1** (D6) — ở đây chỉ kiểm lại `*.local.json` (N18) và `engine/tests/fixtures/**/_local/` (N11) có mặt, `git status --ignored` thấy F3/F4 bị bỏ qua.

## Deliverables

Phase xong khi:
- ✅ `engine/` build + `dotnet test` xanh; `engine ocr` + `engine ocr-score` chạy trên fixture F1, **có số % đoạn khớp nguyên văn** (2 biến thể tiền xử lý) ghi trong dev report.
- ✅ `engine capture` chụp được cửa sổ theo tiêu đề (smoke với cửa sổ thay thế) và — sau Bước 7 — cửa sổ PS Remote Play thật: PNG không đen.
- ✅ `tv-overlay` build APK debug; unit test xanh; trên emulator Android TV: lớp phủ hiện 4 góc đúng 4 góc màn, không ăn phím remote, bật/tắt được.
- ✅ Trên TV thật (User): ảnh chụp có 4 góc + chữ Việt mẫu nổi trên hình PS5; kết luận R-A, R-B ghi vào log; output `getprop` + `screencap` HDMI ghi lại.
- ✅ README hai module, `automation/README.md`, `CLAUDE.md`, `.gitignore` cập nhật theo Bước 8.
- ✅ Kết quả **Q1** (tay cầm có bị ngắt khi Remote Play cùng tài khoản; stream được khi không cắm tay cầm) ghi workflow log; nếu bị ngắt ⇒ Leader đã trình User quyết tài khoản PSN phụ (Risk gate) **trước Phase 3** (N14).
- ✅ Fixture **F1** (từ `engine capture`), **F2**, **F3**, **F4** đã có đủ ở đúng vị trí (*Dữ liệu fixture*); golden F1 đã qua Reviewer soát (N14, N12).
- ❌ **Chưa làm**: kết nối PC ↔ TV, giao thức `OverlayFrame`, token, dịch, glossary, cache, LLM, phát hiện khung đứng yên, kiểu che chữ, Hộp thư tiếng Việt, mockup sản phẩm, tray/autostart, boot start, chiaki-ng.

---

## TDD Checklist

- [ ] Stub → viết test → **FAIL** → implement → **PASS** cho cả 4 hàm thuần dưới đây (`DetectContentRect`, `Normalize`, `ScoreOcr`, ánh xạ chuẩn hoá → pixel).

**`DetectContentRect`** (engine, xUnit — ảnh tổng hợp dựng trong test, không dùng fixture thật):

| Case | Input | Expected |
|---|---|---|
| Không viền | 1920×1080 toàn màu xám giữa | rect = cả ảnh, `confident=true` |
| Letterbox | 1920×1200, đen 60 px trên + 60 px dưới, nội dung 1920×1080 | rect (0,60,1920,1080) ±1 px |
| Pillarbox | 2000×1080, đen 40 px trái + 40 px phải | rect (40,0,1920,1080) ±1 px |
| Mép nội dung tối | nội dung có dải gần đen 20 px ở mép trái nhưng có chi tiết sáng rải rác | **không** cắt mất dải đó |
| Toàn đen | 1920×1080 đen | rect = cả ảnh, `confident=false` |
| Không ra 16:9 | nội dung 1500×1080 sau khi bỏ viền | trả rect thật, `confident=false` |

**`Normalize`** (engine):

| Case | Input | Expected |
|---|---|---|
| Giữa | box (960,540,96,54) trong rect (0,0,1920,1080) | (0.5,0.5,0.05,0.05) |
| Có offset rect | box (100,160,…) trong rect (0,60,1920,1080) | y = 100/1080 |
| Tràn mép | box vượt rect | kẹp trong [0,1], w/h không âm |
| Rect rỗng | rect w=0 | lỗi rõ (exception có thông điệp), không chia 0 |

**`ScoreOcr`** (engine, xUnit — dữ liệu dựng trong test; B1):

| Case | Input | Expected |
|---|---|---|
| Hàng nhiều cột bị gộp | golden `["Board", "Season Objectives Update", "Today"]` · OCR 1 dòng `"Board Season Objectives Update Today"` | (a) 3/3 = 100% · (b) 5/5 từ |
| Không khớp giữa từ | golden `["Board"]` · OCR 1 dòng `"Boardroom meeting"` | (a) **không** khớp (khớp phải theo biên từ) |
| Đoạn bị tách 2 dòng | golden `["Your contract offer has been accepted by the player"]` · OCR 2 dòng liền nhau `"Your contract offer has been"`, `"accepted by the player"` | (a) 1/1 (ghép 2 dòng) |
| Sai 1 ký tự | golden `["Transfer Budget: €45.0M"]` · OCR `"Transfer Budget: €45.OM"` | (a) 0/1 · (b) 2/3 từ; đoạn này có trong danh sách không khớp kèm dòng OCR gần nhất |
| Khoảng trắng thừa | golden `["Scout Report"]` · OCR `"Scout   Report "` | (a) khớp (sau chuẩn hoá) |
| Hoa/thường khác | golden `["INBOX"]` · OCR `"Inbox"` | (a) **không** khớp (giữ hoa/thường) |
| Không ghép quá 3 dòng | đoạn golden chỉ khớp khi ghép 4 dòng OCR | (a) không khớp |
| Golden rỗng | `[]` | tỉ lệ = không xác định ⇒ báo rõ, không chia 0 |

**Ánh xạ chuẩn hoá → pixel** (tv-overlay, JUnit JVM):

| Case | Input | Expected |
|---|---|---|
| 4 góc | (0,0),(1,1) trên View 1920×1080 | (0,0),(1920,1080) |
| View khác cỡ | (0.5,0.5) trên 3840×2160 | (1920,1080) |
| Ngoài miền | (−0.1, 1.2) | kẹp về (0, H) |

> Màn Activity của APK là công cụ kiểm tra, không có test render; kiểm bằng blackbox emulator (dưới).

## Blackbox Verify

### Chuẩn bị (vai tự làm)
- **Tài khoản**: không dùng tài khoản nào (`test-accounts.md`: chưa áp dụng). **Không** đăng nhập PSN.
- **Data**: fixture F1 (khi đã có — U5/Bước 7). Chưa có F1 ⇒ smoke OCR trên **ảnh tổng hợp** do Dev tự tạo (chữ tiếng Anh vẽ lên nền tối, font sans) để chứng minh đường chạy; số đo R-D để `chưa kiểm` và báo Leader — **không** tự lấy ảnh FC 26 trên web làm golden.
- **Môi trường**: laptop dev (localhost). Emulator Google TV **x86 API 31** AVD riêng của vai, serial `emulator-5556` (Dev) / `emulator-5558` (Reviewer). **Không** kết nối IP TV thật.
- **Đường dẫn evidence** (N10): mọi output của smoke ghi vào `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/` (đường dẫn đầy đủ từ root repo) — **không** tạo `out/` hay `playtest/` ở root repo.
- **Dữ liệu fixture** (N11) — vị trí dưới `engine/tests/fixtures/fc26/inbox/`:

  | Fixture | Thư mục | Commit? | Trần |
  |---|---|---|---|
  | F1 (PNG từ `engine capture`) | `f1-remoteplay/` | Có | ≤ 3 MB/ảnh (PNG đã cắt client) |
  | Golden F1 | `golden/` | Có | — |
  | F2 (PNG nút Create) | `f2-ps5/` | Có | ≤ 5 MB/ảnh; ảnh 4K lớn hơn ⇒ chuyển 1080p trước khi commit |
  | F3 (video) | `_local/f3-video/` | **Không** (gitignore) | Tách khung bằng `ffmpeg` khi cần; khung nào dùng cho test thì commit **PNG** khung đó vào `f3-frames/` |
  | F4 (ảnh TV chụp bằng điện thoại — ảnh phòng nhà User) | `_local/f4-tv-photo/` | **Không** (riêng tư) | — |

  Repo private ⇒ ảnh FC 26 commit được; chuyển public phải xét lại (bản quyền EA).
- 🔴 **Dữ liệu test**: thư mục `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/` (ảnh chụp, json kết quả) — **để lại** (evidence), không commit script. Không có dữ liệu cần dọn.

### Các bước — tất cả `chưa kiểm`
1. `dotnet --list-sdks` có `10.0.x` (U1) — dừng nếu thiếu, báo Leader. `chưa kiểm`
2. `dotnet build engine` + `dotnet test engine` — xanh, đúng số test theo bảng TDD. `chưa kiểm`
3. `engine capture --window-title <cửa sổ thay thế> --frames 3 --out project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/cap` — cửa sổ thay thế phải có **thanh tiêu đề chuẩn Win32** (D3 — app Photos của Windows 11 tự vẽ thanh tiêu đề **bên trong** vùng client nên không dùng được): `ffplay -window_title VietSubTest <ảnh F1 hoặc ảnh tổng hợp>` nếu bản ffmpeg trên PATH có `ffplay`; không có ⇒ một cửa sổ WinForms tối giản trong dự án test hiện ảnh đó, tiêu đề `VietSubTest`. Verify: 3 PNG không đen, nhìn đúng nội dung, **không có thanh tiêu đề** của cửa sổ; `capture.json` có rect client + rect nội dung. `chưa kiểm`
4. Thu nhỏ cửa sổ đó rồi chạy lại ⇒ báo lỗi rõ, exit ≠ 0 (không treo). `chưa kiểm`
5. `engine ocr --input engine/tests/fixtures/fc26/inbox/f1-remoteplay --out project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/ocr` rồi `engine ocr-score --ocr <thư mục đó> --golden engine/tests/fixtures/fc26/inbox/golden` — in số (a), (b); ghi cả biến thể phóng 2×. Mở 2 file `.ocr.json` đối chiếu bằng mắt với ảnh: box01 nằm đúng chỗ chữ. `chưa kiểm`
6. `gradlew :app:testDebugUnitTest` (trong `tv-overlay/`) — xanh. `chưa kiểm`
7. `gradlew :app:assembleDebug` → APK; bật AVD Android TV API 31 **tách tiến trình** (guideline §7), chờ `sys.boot_completed=1`; `adb -s emulator-5556 install -r <apk>`. `chưa kiểm`
8. `adb -s emulator-5556 shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`; `uiautomator dump` màn chính để xác nhận nút + focus (guideline §3) — focus mặc định đúng "việc tiếp theo" (N17 iv); bật lớp phủ **tất định** bằng `am start -n vn.vietsub.ps5.tv/<activity> --ez overlay_on true` (bản debug, N20); về màn Home (`input keyevent KEYCODE_HOME`) rồi mở một app bất kỳ toàn màn (vd Settings). `adb -s emulator-5556 exec-out screencap -p > project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/08-emu-overlay.png` — Verify: 4 dấu góc sát 4 góc ảnh, lưới, thẻ chữ Việt đủ dấu, dòng "lớp phủ kiểm tra" (xem ảnh). `chưa kiểm`
9. Overlay không ăn phím: khi lớp phủ bật, `input keyevent KEYCODE_DPAD_DOWN` vẫn di chuyển focus trong app bên dưới (so 2 ảnh screencap trước/sau). `chưa kiểm`
10. Tắt lớp phủ qua app ⇒ screencap không còn dấu góc; `dumpsys activity services vn.vietsub.ps5.tv` không còn service. `chưa kiểm`
11. Tắt AVD đúng cây tiến trình (guideline §2.3), `adb devices` không còn `emulator-5556`. `chưa kiểm`

Bước 6–7 của *Kế hoạch* (TV thật, PS Remote Play thật) **User chạy** với Leader — không thuộc smoke của Dev/Reviewer.

**Reviewer chạy lại smoke**: **có** — bước 2, 3, 5, 6, 8, 9 (tích hợp WGC/OCR/Android overlay dễ "xanh trên máy Dev"; số OCR phải tái lập được). Reviewer dùng `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/reviewer/` và AVD riêng (`emulator-5558`). Thêm (N12): Reviewer **soát độc lập ≥ 30% file golden F1** (chọn ngẫu nhiên, gồm ≥ 1 email dài) với ảnh gốc, liệt kê mọi đoạn golden sai/thiếu **trước khi** chấp nhận số R-D; golden có lỗi ⇒ Dev sửa golden, chạy lại `ocr-score`.

### Bản đồ case → công cụ đỡ

| Case | Đỡ bằng | Trạng thái |
|---|---|---|
| Dò viền, chuẩn hoá, ánh xạ, chấm OCR | unit test (TDD) | chưa kiểm |
| Cắt vùng client (bỏ thanh tiêu đề) + DPI | smoke bước 3 (ảnh không có thanh tiêu đề) + Bước 7 thật | chưa kiểm |
| Golden F1 đúng | Reviewer soát ≥ 30% | chưa kiểm |
| Lớp phủ kiểm tra tự tắt sau 30 phút, không tự bật lại | đọc code (`START_NOT_STICKY`, hẹn giờ) | chưa kiểm |
| WGC chụp cửa sổ không đen | smoke bước 3 + Bước 7 thật | chưa kiểm |
| Cửa sổ thu nhỏ/không có | smoke bước 4 | chưa kiểm |
| OCR thiếu `en-US` | đọc code (nhánh `null` ⇒ lỗi rõ) — máy hiện có `en-US` | chưa kiểm |
| Số OCR trên chữ FC 26 | smoke bước 5 trên F1 | chưa kiểm (chờ U5) |
| Lớp phủ vẽ + không ăn phím + bật/tắt | emulator bước 8–10 | chưa kiểm |
| Vẽ đè nguồn HDMI, khớp góc hình PS5, font trên TV | TV thật (User, Bước 6) | chưa kiểm |
| App không đụng đường hình HDMI | đọc code: APK không đọc/ghi gì của nguồn HDMI, chỉ thêm cửa sổ | chưa kiểm |

### Verify (3 lớp)
- [ ] Output: PNG/JSON/APK đúng chỗ; số OCR in ra.
- [ ] Log: `engine` in rõ cửa sổ đã chọn (HWND, tiêu đề, kích thước), số khung; lỗi có thông điệp + exit ≠ 0.
- [ ] Không được xảy ra: không chạm IP TV thật; không thao tác cửa sổ PS Remote Play; không gọi mạng ngoài (engine Phase 1 không có HTTP; ngoại lệ — chỉ lúc dựng/build: Dev tải bản phân phối Gradle + font Be Vietnam Pro từ nguồn chính thức, và phụ thuộc build từ Maven Google/Central (Android Gradle Plugin, Kotlin, androidx) và NuGet.org (xUnit, gói interop) — D4); không khai `INTERNET`/`POST_NOTIFICATIONS` trong manifest; không commit `bin/ obj/ build/ .gradle/ local.properties`, bản phân phối Gradle, fixture `_local/`; không tạo `out/`/`playtest/` ở root repo.
- [ ] Ảnh evidence: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/NN-<mô-tả>.png`.

### Sau khi test — bắt buộc
- [ ] Tắt AVD của mình, `adb devices` sạch emulator của mình.
- [ ] Bẫy môi trường mới (Android Studio/AVD trên AMD, WGC, OCR) ⇒ README module hoặc `present/backlog-and-techdebt.md`.

---

## Bàn giao

- **SQL**: không.
- **Env mới**: không (Phase 1 không gọi dịch vụ ngoài).
- **Client**: APK debug cài tay lên TV — chưa phát hành cho ai; User có thể gỡ bất cứ lúc nào (Cài đặt → Ứng dụng).
- **Công cụ User phải cài**: .NET 10 SDK, Android Studio (U1, U2).
- **Kiểm sau khi cài lên TV**: mở app trên TV thấy trạng thái quyền + kích thước màn; bật lớp phủ thấy 4 góc + chữ mẫu trên hình PS5.
