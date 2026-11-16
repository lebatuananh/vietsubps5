SA-GATE R2 DONE
## Verdict
PASS
> Vòng 1 (Rev 1): BLOCKER (1) — xem dưới. Vòng 2 (Rev 2): PASS — xem mục *Re-review Rev 2* ở cuối file.

# 261114 — Epic 1 Phase 1 — SA review (SA gate)

**Role**: BA/SA · **Ngày**: 2026-11-15 · **Doc được thẩm định**: [`261114-epic1-phase1.md`](../261114-epic1-phase1.md) Rev 1 · **Đối chiếu**: `261114-epic1.md`, `261114-overview.md`, `261114-draft.md` §3–§8, `261114-discovery.md` Round 2 (cơ sở đã duyệt), repo tại commit `8101fc7`.

Điều kiện BLOCKER áp dụng (handbook §5): (1) gọi trả phí lặp / cấp tiền sai · (2) mất hoặc xoá nhầm dữ liệu · (3) spec thiếu tới mức Dev phải đoán · cộng mọi vi phạm *Ràng buộc đặc thù* của `CLAUDE.md`. Còn lại là NOTE.

Kết luận nhanh: doc **bám đúng lát 0**, không phình scope, tôn trọng mọi ràng buộc cứng (Dev không chạm TV thật, không PSN, không gọi trả phí, không `INTERNET`). Có **1 BLOCKER** loại (3): cách chấm số OCR (R-D) chưa định nghĩa đơn vị so khớp ⇒ Dev phải tự đoán, mà con số này quyết định có dựng PaddleOCR hay không. Có một NOTE nặng (N1: ảnh WGC của cửa sổ có cả thanh tiêu đề ⇒ dò viền theo "đen" sẽ sai) nên sửa cùng lượt.

---

## Finding

| # | Tiêu chí | Chỗ nào trong doc | Vấn đề | Evidence | Đề xuất sửa |
|---|---|---|---|---|---|
| **B1** | 3 · Đủ cho Dev | Bước 3 `engine ocr-score`; Deliverables dòng 1; TDD không có case | **BLOCKER (3)**. Golden là "mỗi dòng một đoạn như người đọc nhìn thấy", còn `Windows.Media.Ocr` trả **dòng** do engine tự gom. Màn Hộp thư là danh sách nhiều cột (người gửi · tiêu đề · ngày): OCR có thể gộp cả hàng thành một dòng hoặc tách khác người đọc ⇒ so "khớp nguyên văn từng đoạn" cho ra ~0% dù OCR đọc đúng từng chữ, hoặc ngược lại. Doc không nói đơn vị so khớp, cách ghép đoạn ↔ dòng, chữ hoa/thường, dấu câu ⇒ Dev phải đoán; con số sinh ra quyết định có dựng PaddleOCR (ngưỡng 95%) | Doc dòng 82: "số đoạn OCR khớp nguyên văn (so sau khi chuẩn hoá khoảng trắng)"; `OcrResult` trả `Lines` → `Words` (discovery §3.7, https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine) | Định nghĩa rõ, có TDD: (a) **đoạn golden khớp** nếu sau chuẩn hoá (gộp khoảng trắng, giữ hoa/thường, giữ dấu câu) nó là **chuỗi con liên tục** của một dòng OCR, hoặc ghép được từ các dòng OCR liền nhau; (b) báo thêm **tỉ lệ từ đúng** (word recall: số từ golden xuất hiện đúng thứ tự trong OCR / tổng từ golden) — số này ít nhạy với cách gom dòng; (c) ngưỡng 95% áp lên (a), (b) để tham khảo; (d) thêm 3 case TDD cho `ocr-score`: một hàng nhiều cột bị gộp, một đoạn bị tách 2 dòng, một từ sai 1 ký tự |
| N1 | 2 · Khớp thực tế | Bước 2 `DetectContentRect`, Bước 3 `capture`, Bước 7 | Chụp WGC **theo cửa sổ** gồm cả vùng non-client (thanh tiêu đề, viền) của PS Remote Play — không phải viền **đen**. `DetectContentRect` chỉ bỏ viền đen/gần đen ⇒ thanh tiêu đề (xám/trắng, có chữ) bị tính là nội dung ⇒ rect lệch, `Normalize` sai, R-C "rect ~16:9" có thể không bao giờ đạt. Cùng lý do: ảnh F1 chụp bằng `Win+Shift+S` (chế độ cửa sổ) cũng có thanh tiêu đề | `assumption` (hành vi đã biết của WGC window capture; kiểm ngay ở smoke bước 3: ảnh có thanh tiêu đề không) | Trước `DetectContentRect`, cắt về **vùng client**: `GetClientRect` + `ClientToScreen` so với `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` để ra offset client trong khung WGC; ghi vào `capture.json` cả rect client và rect nội dung. Thêm case TDD "ảnh có dải tiêu đề sáng phía trên" (đầu vào đã cắt client) hoặc nêu rõ hàm chỉ nhận ảnh đã cắt client |
| N2 | 2 · Khớp thực tế | Bước 3 `capture` | App console không có `DispatcherQueue` ⇒ dùng `Direct3D11CaptureFramePool.Create` thì `FrameArrived` không có thread để bắn. Phải dùng `CreateFreeThreaded` | `verified`: "dependency on the DispatcherQueue is removed and the FrameArrived event is raised on the frame pool's internal worker thread" — https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded | Ghi thẳng `CreateFreeThreaded` vào Bước 3 |
| N3 | 2 · Khớp thực tế | Bước 3 `capture` (HWND, kích thước) | Laptop màn 2880×1800 gần như chắc đang scale >100%; process không khai DPI-aware thì `GetWindowRect`/`GetClientRect` trả toạ độ ảo hoá, lệch với kích thước pixel thật của khung WGC ⇒ phép cắt client ở N1 sai | `verified` màn 2880×1800 (discovery §3.1); mức scale `unknown` | Khai `PerMonitorV2` (app.manifest `dpiAwareness` hoặc `ApplicationHighDpiMode`/`SetProcessDpiAwarenessContext`) — ghi vào Bước 1/3 và bẫy README |
| N4 | 2 · Khớp thực tế | Bước 4 quyền FGS | Doc viết "quyền theo loại FGS đã chọn (đề xuất `specialUse`)" mà không nêu tên. Với `targetSdk 35` cần đủ 3 thứ: `android:foregroundServiceType="specialUse"` trên `<service>`, quyền `FOREGROUND_SERVICE_SPECIAL_USE`, `<property android:name="android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE" .../>`, và gọi `startForeground(..., FOREGROUND_SERVICE_TYPE_SPECIAL_USE)` (dùng `ServiceCompat.startForeground` vì hằng số chỉ có từ API 34, `minSdk` 29) | `verified`: https://developer.android.com/develop/background-work/services/fgs/service-types (mục Special use) | Chốt `specialUse` (bỏ chữ "đề xuất"), liệt kê đủ 4 mục trên |
| N5 | 2 · Khớp thực tế | Bước 4 `POST_NOTIFICATIONS "nếu targetSdk yêu cầu"` | Câu điều kiện để Dev tự đoán. FGS vẫn chạy khi không có quyền thông báo (chỉ ẩn thông báo); TV thường không có ngăn thông báo như điện thoại | `assumption` (hành vi Android 13+, chưa thử trên TV) | Chốt: **không** khai/xin `POST_NOTIFICATIONS` ở Phase 1 |
| N6 | 2 · Khớp thực tế | Bước 5 màn chính "`WindowMetrics` px + density" | `WindowManager.getCurrentWindowMetrics()` có từ API 30; `minSdk 29` và TV Sony có thể đang chạy **Android 10** (discovery R2.3) ⇒ crash đúng trên thiết bị đích | `assumption` (API level; lint `NewApi` sẽ báo) | Dùng `androidx.window` `WindowMetricsCalculator` hoặc rẽ nhánh `Build.VERSION.SDK_INT` với `Display.getRealMetrics` |
| N7 | 3 · Đủ cho Dev | Bước 5 nút "Bỏ tối ưu pin" | Chỉ có fallback cho màn quyền overlay, **không** có cho `ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` — intent này cũng có thể không có activity trên Android TV (cùng loại vấn đề với overlay) ⇒ `ActivityNotFoundException` | `assumption` (discovery §3.5 nêu cùng rủi ro cho màn overlay) | Bọc `try/catch` cả hai intent; fallback hiện lệnh `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (`chưa kiểm`) và ghi kết quả thật trên TV vào `automation/README.md` |
| N8 | 3 · Đủ cho Dev | Bước 4–5, smoke 6–7 | Repo không có Gradle wrapper và máy không có `gradle` (U2 chỉ cài Android Studio). Dev (agent dòng lệnh) không có đường tạo `gradlew` + `gradle-wrapper.jar`; `gradlew` cũng cần `JAVA_HOME` và `sdk.dir`/`ANDROID_HOME` — U2 chỉ nói "thêm vào PATH" | `verified`: `git ls-files` không có `tv-overlay/` (doc dòng 10); Round 1 §3.1 `which: no java`; root `.gitignore` không liên quan | Thêm vào U2: đặt biến môi trường `JAVA_HOME` (JBR của Android Studio) và `ANDROID_HOME`; và chọn một: (a) User tạo khung dự án trống bằng Android Studio (template *No Activity*, đúng `applicationId`) rồi Dev sửa tiếp, hoặc (b) Dev tải bản phân phối Gradle chính thức vào thư mục tạm, chạy `gradle wrapper --gradle-version <x>` |
| N9 | 3 · Đủ cho Dev | Bước 3 lệnh `engine capture …` | Doc viết như lệnh có trên PATH; không nói gọi bằng `dotnet run --project engine/src/Engine -- capture …` hay file exe trong `bin/` | Doc dòng 80–82, smoke 3–5 | Ghi một dòng cách gọi chuẩn (và dùng đúng nó trong smoke) |
| N10 | 4 · Đủ cho smoke | Blackbox "Dữ liệu test", smoke 3/5, Bước 7 `<playtest>` | Đường dẫn evidence viết `playtest/epic1-phase1/<vai>/` không có tiền tố ⇒ chạy từ root sẽ tạo `playtest/` và `out/` ở **root repo** (không gitignore, lẫn vào `git status`). Quy ước workflow là `project-docs/[đợt]/playtest/<epic-phase>/<vai>/` | `verified`: `workflow/leader-stages/03-write-docs.md:36-48`; root `.gitignore` không có `playtest/`, `out/` (đã đọc) | Viết đủ đường dẫn `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/`; bỏ `out/` hoặc gitignore nó |
| N11 | 4 · Đủ cho smoke / dữ liệu | Prerequisite U5, "Golden JSON + fixture F1–F4 commit" | F3 là video MP4 30–60 s quay ở 2880×1800 có thể hàng chục–trăm MB; GitHub chặn file > 100 MB khi push ⇒ push nhánh hỏng, lịch sử git phình vĩnh viễn. F4 là ảnh phòng khách nhà User ⇒ lộ đời tư nếu repo chuyển public. Ảnh F2 4K PNG ~10 MB/ảnh | `assumption` (kích thước chưa đo); giới hạn 100 MB của GitHub là quy định nền tảng — `unknown` trong lượt này (chưa fetch tài liệu GitHub) | Quy định: F3 **không commit** (để ngoài repo hoặc nén ≤ 20 MB, 1080p, ≤ 30 s); F4 không commit (chỉ dùng cho mockup, để trong thư mục gitignore); đặt trần dung lượng cho mỗi ảnh commit |
| N12 | 4 · Đủ cho smoke | Bước 3 golden "Dev đọc ảnh, ghi tay" | Dev vừa tạo golden vừa tự chấm OCR của mình ⇒ không độc lập; agent đọc ảnh có thể chép sai chính chữ khó (chữ nhỏ, nghiêng) — đúng chỗ đo | Doc dòng 82; Reviewer rerun chỉ chạy lại lệnh, không soát golden | Reviewer **soát độc lập** ≥ 30% file golden với ảnh gốc trước khi chấp nhận số R-D; ghi trong phần Reviewer |
| N13 | 1 · Khớp định hướng / thứ tự | U5 F1 + Bước 7 | F1 chụp bằng Snipping Tool khác thứ engine thật sự nhận (WGC: có/không thanh tiêu đề, scale DPI, định dạng điểm ảnh). Bước 7 đã chụp cửa sổ PS Remote Play thật bằng chính `engine capture` | Doc dòng 41, 119 | Đổi thứ tự/ nguồn: làm Bước 7 sớm (khi U4 xong) và dùng chính PNG của `engine capture` làm F1 chính; ảnh Snipping Tool chỉ là phương án dự phòng |
| N14 | 1 · Khớp định hướng | Deliverables | Mục tiêu Phase nói chốt Q1 (tay cầm) + Q3 (model TV) và thu fixture cho Phase 2, nhưng Deliverables **không** có: kết quả Q1 ghi log + quyết định tài khoản PSN; fixture F1–F4 đã nhận đủ | Doc dòng 27 vs 130–136 | Thêm 2 dòng ✅: "Kết quả Q1 ghi workflow log; nếu tay cầm bị ngắt ⇒ đã trình User (Risk gate) trước Phase 3" · "Fixture F1–F4 có đủ trong repo/ thư mục đã định" |
| N15 | 2 · Khớp thực tế | U2 + smoke 7 (AVD) | Emulator x86_64 trên CPU AMD cần Windows Hypervisor Platform (hoặc AEHD); U2 không nhắc. Image "Android TV API 34 hoặc 35" — TV User nhiều khả năng Android 10–12 ⇒ AVD gần đích hơn là API 31 (Android TV 12) | `assumption`; discovery R2.3 | Thêm kiểm `emulator -accel-check` vào U2; khuyên AVD Android TV API 31 (thêm API 34 nếu muốn), ghi tên AVD vào `automation/README.md` |
| N16 | 6 · Trải nghiệm | Bước 5 Service | Lớp phủ vàng + lưới 10% nằm trên **mọi** nguồn (YouTube, truyền hình) cho tới khi tắt; TV thường không có ngăn thông báo để thấy FGS đang chạy ⇒ người nhà User bật TV vẫn thấy khung vàng; nếu service là `START_STICKY` thì bị giết xong lại tự hiện | Discovery §3.2 (overlay không gắn với nguồn HDMI) | Service `START_NOT_STICKY`; tự tắt sau 30 phút; dòng chữ nhỏ trên lớp phủ "VietSub — lớp phủ kiểm tra — tắt trong app" |
| N17 | 6 · Trải nghiệm | Bước 5 + TDD ghi chú | Miễn mockup cho màn chính APK kiểm tra: **chấp nhận được** (xem *Mục mở* điểm 1) với điều kiện: màn này **bị thay** ở Phase 3 bằng màn có mockup Phase 2 (không "nâng cấp dần" thành UI sản phẩm), chữ trên màn bằng tiếng Việt, trạng thái quyền **làm mới ở `onResume`** khi User quay lại từ màn cài đặt, focus D-pad mặc định vào nút việc tiếp theo chưa xong | Handbook B6 ("mockup tỉ mỉ … áp dụng cho CẢ admin console") — `workflow/roles/ba-sa.md:130` | Ghi 4 điều kiện vào Bước 5 |
| N18 | 2 · Khớp thực tế | `CLAUDE.md` mục Môi trường (không thuộc doc nhưng doc Phase 3 sẽ dựa vào) | `CLAUDE.md` nói ghi đè riêng máy là file `*.local.*` (gitignore), nhưng root `.gitignore` chỉ có `*.local.md`, `.env.local`, `.claude/*.local.json` ⇒ `engine.local.json` (Phase 3, chứa token) **sẽ bị commit** | `verified`: root `.gitignore` (đã đọc); `CLAUDE.md:95` | Không ảnh hưởng Phase 1; thêm `*.local.json` vào `.gitignore` ở Bước 8 (rẻ, đúng lúc đang sửa tài liệu) |
| N19 | 5 · Ràng buộc | Bước 6, 7; Blackbox | Không thấy vi phạm: Dev không chạy lệnh tới IP TV (dòng 107, 179, 215); không PSN (177); không HTTP/`INTERNET` (90, 215); U4 (thử tay cầm bằng tài khoản thật) do **User** làm và nằm trong gói User đã duyệt (draft §4 dòng 49). Một điểm nhỏ: Bước 7 "Dev/Leader chạy `engine`" trong lúc phiên Remote Play của User đang mở — chỉ đọc cửa sổ trên laptop, không chạm PSN; nên ghi rõ Dev **không** thao tác cửa sổ PS Remote Play (không click, không đóng) | Doc các dòng đã dẫn | Thêm một câu vào Bước 7 |
| N20 | 4 · Đủ cho smoke | Smoke 8 "bấm Bật bằng input keyevent (DPAD + ENTER)" | Đoán thứ tự focus; guideline yêu cầu lấy vị trí từ `uiautomator dump` | `workflow/guidelines/blackbox-android-adb.md` §3 | Thêm: dump trước khi bấm; hoặc cho Activity nhận `am start ... --ez overlay_on true` (chỉ bản debug) để smoke tất định |
| N21 | 3 · Đủ cho Dev | Bước 4 font | Tên file trong `res/font/` chỉ được chữ thường, số, `_` (tên file gốc `BeVietnamPro-Regular.ttf` sẽ lỗi build) | `assumption` (quy tắc tài nguyên Android) | Ghi: đổi tên `be_vietnam_pro_regular.ttf` …; chỉ nhúng các độ đậm dùng tới |

Không có finding loại (1) gọi trả phí hay (2) mất dữ liệu: Phase 1 không gọi dịch vụ ngoài, không xoá dữ liệu nào.

---

## Đối chiếu 6 tiêu chí

1. **Khớp định hướng** — Đạt. Đúng lát 0 đã duyệt (R2.7 lát 0 + APK 4 góc User đồng ý). `ocr-score`, biến thể phóng 2×, chữ Việt mẫu trong APK là công cụ đo, không phải phình scope. Danh sách ❌ khớp R2.6. Thiếu deliverable Q1/fixture (N14).
2. **Khớp thực tế** — Repo đúng là chưa có code (`git log` chỉ có `8101fc7`, `758b70c`, `5797e8e`; `git status` không có `engine/`, `tv-overlay/`). `workflow/guidelines/tdd.md`, `blackbox-android-adb.md` có thật (`ls workflow/guidelines`). TFM `net10.0-windows10.0.22621.0` hợp lệ (`verified`, discovery R2.3). `LEANBACK_LAUNCHER` + banner + `uses-feature leanback/touchscreen required=false` hợp lý cho sideload. Fallback `ACTION_MANAGE_OVERLAY_PERMISSION` có. Lệch: N1–N8, N10, N15, N18, N21.
3. **Đủ cho Dev** — B1 (phải đoán cách chấm), N7–N9, N21.
4. **Đủ cho smoke** — Bước, dữ liệu, quyết định để lại evidence, Reviewer chạy lại (bước 2, 3, 5, 6, 8, 9) đều có và có lý do. Lệch: N10–N13, N20.
5. **Ràng buộc dự án** — Đạt (N19). Không chèn gì vào đường HDMI; không đụng PSN ngoài phần User tự làm đã duyệt; không gọi LLM.
6. **Trải nghiệm** — Miễn mockup chấp nhận có điều kiện (N17); rủi ro lớp phủ quên tắt (N16).

---

## Mục mở

1. **Miễn mockup cho màn APK kiểm tra có chấp nhận được không?** Có, với điều kiện ở N17. Lý do: màn này chỉ User dùng vài lần trong Phase 1 để cấp quyền và bật/tắt; nó **không** được phát hành và sẽ bị thay ở Phase 3. Handbook đòi mockup cho cả màn admin vì sợ "hiểu nhầm về luồng" trong sản phẩm sống lâu — ở đây làm mockup tốn hơn chính màn hình. Nhưng nếu Phase 3 giữ lại màn này và chỉ "thêm vài dòng" thì miễn trừ thành lỗ hổng ⇒ Phase 2 phải có mockup màn chính app TV (Epic 1 đã ghi) và Phase 3 phải viết lại màn theo mockup.
2. **Doc chưa nói gì quan trọng**:
   - Thanh tiêu đề trong ảnh WGC (N1) — chạm thẳng R-B/R-C và cả cách tính toạ độ của Phase 3.
   - **Ghi lại điều kiện hình ảnh lúc chụp TV ở Bước 6**: PS5 có bật HDR không, chế độ ảnh TV (Game?), độ phân giải PS5 xuất ra (4K/1080p), 60/120 Hz. Đây là dữ liệu Phase 2 cần để chọn màu kiểu che, mà chỉ lấy được khi User đứng trước TV ở bước này.
   - Kết quả Q1 xử lý thế nào nếu tay cầm bị ngắt: doc nói "Leader dừng", nhưng Bước 7 (chụp PS Remote Play thật) vẫn làm được bằng cách không cầm tay cầm lúc chụp — nên nói rõ Bước 7 **không** bị chặn bởi kết quả Q1, chỉ Phase 3 bị chặn.
   - Không nói APK có tự cập nhật lên TV thế nào ở Phase 3 (cùng `applicationId`, cùng debug keystore của laptop — đổi máy build là `install -r` lỗi chữ ký). Ghi vào README `tv-overlay/` như một bẫy.
3. **Khái niệm gộp/ đặt tên sai**:
   - "Rect nội dung ~16:9" (R-C) gộp hai việc: **cắt vùng client** của cửa sổ (hình học Windows) và **dò viền đen** trong hình stream (letterbox của Remote Play). Hai việc khác nguồn, khác cách làm (N1).
   - "Khớp nguyên văn" gộp so khớp theo đoạn của người đọc với dòng của engine OCR (B1).
   - "4 góc màn" ở R-B thực ra là 4 góc **mặt phẳng đồ hoạ Android** (thường 1920×1080 dù panel 4K); việc nó trùng 4 góc **hình HDMI** chính là điều đang được kiểm, không phải điều được giả định — doc đã đúng ý nhưng nên ghi rõ để User không chỉnh sai mục TV.
4. **Thứ tự sai**: F1 nên lấy từ `engine capture` (Bước 7) thay vì Snipping Tool (N13). Ngoài ra không thấy bước nào sai thứ tự; Bước 6 (TV) và Bước 7 (laptop) độc lập nhau, chạy song song được.
5. **Giả định nền có sai không**: không thấy giả định nền sai. Một giả định chưa kiểm cần nói ra: "lớp phủ của app bên thứ ba vẫn hiện trên nguồn HDMI khi TV ở chế độ Game/ ALLM". Console Translator chỉ chứng minh được trên TV này ở chế độ User đang dùng; Bước 6.3 phải ghi lại TV đang ở picture mode nào khi chụp.

---

## Điều kiện để chuyển PASS

Sửa **B1** (định nghĩa cách chấm + TDD cho `ocr-score`). Các NOTE do Leader quyết; khuyên sửa cùng lượt N1, N2, N4, N8, N10, N11 vì rẻ và mỗi cái có thể làm Dev mất nửa ngày. Vòng duyệt lại chỉ xét phần delta.

---
---

# Re-review Rev 2

**Ngày**: 2026-11-15 · **Doc**: [`261114-epic1-phase1.md`](../261114-epic1-phase1.md) Rev 2 (dòng Revision liệt kê B1, N1–N21, 3 mục từ *Mục mở*) · **Phạm vi**: chỉ phần delta (handbook §5 — không mở loại lo ngại mới ngoài delta). Lựa chọn N8 của Leader: phương án (b), Dev sinh Gradle wrapper từ bản phân phối chính thức, `JAVA_HOME`/`ANDROID_HOME` chỉ đặt trong tiến trình shell.

## Verdict
**PASS**

B1 đã đóng: định nghĩa đoạn, quy tắc khớp, chuẩn hoá, chỉ số phụ và 7 case TDD đủ để Dev không phải đoán. Mọi NOTE N1–N21 đã được áp dụng. Delta sinh ra 6 NOTE mới nhỏ (D1–D6 bên dưới), không cái nào thuộc ba điều kiện BLOCKER hay vi phạm ràng buộc cứng. Khuyên sửa D1, D2, D3 trước khi giao Dev vì rẻ, và nếu để nguyên thì Dev sẽ mất thời gian vào test đỏ "giả".

## Finding → trạng thái

| # | Trạng thái | Chỗ trong Rev 2 | Ghi chú |
|---|---|---|---|
| B1 | **Đóng** | Bước 3 dòng 87–93; TDD `ScoreOcr` dòng 183–193 | Đoạn = ô của cột/dòng email; khớp = chuỗi con liên tục của 1 dòng hoặc chuỗi ghép 2–3 dòng liền nhau; giữ hoa/thường và dấu câu; (b) theo dãy con chung dài nhất theo từ; in đoạn không khớp kèm dòng gần nhất; golden rỗng báo rõ. Còn một lỗi số trong bảng TDD (D1) và một chỗ chưa nói thứ tự golden cho (b) (D2) — đều không làm sai chỉ số chính (a) |
| N1 | **Đóng** | Bước 2 dòng 74–77; Bước 3 dòng 85 | Tách rõ "cắt client" và "dò viền đen"; `DetectContentRect` chỉ nhận ảnh đã cắt; `capture.json` lưu cả hai rect. Smoke có vấn đề với cửa sổ thay thế (D3) |
| N2 | **Đóng** | Bước 3 dòng 85; README dòng 67 | `CreateFreeThreaded` ghi thẳng |
| N3 | **Đóng** | Bước 1 dòng 66 | `PerMonitorV2` |
| N4 | **Đóng** | Bước 4 dòng 102 | Đủ 4 mục. `ServiceCompat.startForeground` với hằng số `SPECIAL_USE` trên API 29–33: hành vi chưa kiểm (`assumption` — androidx có thể lọc kiểu không hỗ trợ), nhưng smoke bước 8 chạy trên AVD **API 31** sẽ lộ ngay nếu crash ⇒ đã có lưới đỡ, không cần sửa doc |
| N5 | **Đóng** | Bước 4 dòng 102; Verify dòng 261 | Chốt không `POST_NOTIFICATIONS` |
| N6 | **Đóng** | Bước 5 dòng 113 | `WindowMetricsCalculator` (androidx.window — nhớ ghi giấy phép vào README như mọi thư viện, Bước 1/4 đã yêu cầu) |
| N7 | **Đóng** | Bước 5 dòng 113 | Cả hai intent bọc `try/catch`, fallback hiện trên màn |
| N8 | **Đóng** (có D4) | Bước 4 dòng 100; U2 dòng 39 | Đường sinh wrapper rõ, commit wrapper không commit bản phân phối, biến môi trường trong tiến trình. Chỗ va với dòng "không gọi mạng ngoài" — D4 |
| N9 | **Đóng** | Bước 1 dòng 65 | `dotnet run --project engine/src/Engine -- …` từ root |
| N10 | **Đóng** (sót 1 chỗ) | Dòng 211, 228–233, 240, 262 | Còn dòng 223 viết `playtest/epic1-phase1/<vai>/` không tiền tố — dòng 211 ngay trên đã nói rõ, Dev khó hiểu sai; nên sửa cho đồng nhất (D5) |
| N11 | **Đóng** (có D6) | Bảng *Dữ liệu fixture* dòng 212–222; Bước 8 dòng 143 | F3/F4 không commit, có trần dung lượng. Thứ tự thêm `.gitignore` muộn — D6 |
| N12 | **Đóng** | Dòng 87, 240, 248, 154 | Reviewer soát ≥ 30% golden trước khi nhận số |
| N13 | **Đóng** | U5 dòng 42; Bước 7 dòng 134–135 | F1 chính từ `engine capture`; thứ tự Bước 3 (code) → Bước 7 (chụp) → `ocr-score` không vòng lặp |
| N14 | **Đóng** | Deliverables dòng 153–154 | |
| N15 | **Đóng** | U2 dòng 39; dòng 210, 232 | AVD API 31, `-accel-check`, WHPX. `ANDROID_HOME` = `%LOCALAPPDATA%\Android\Sdk` là mặc định trình cài — Dev nên đọc đường dẫn thật trong Android Studio (SDK Manager) thay vì tin mặc định (guideline `blackbox-android-adb.md` §1); không cần sửa doc |
| N16 | **Đóng** | Bước 5 dòng 115–116; bản đồ case dòng 249 | `START_NOT_STICKY`, tự tắt 30 phút, dòng chữ "lớp phủ kiểm tra" |
| N17 | **Đóng** | Bước 5 dòng 112; smoke 8 | 4 điều kiện ghi đủ, smoke kiểm focus mặc định |
| N18 | **Đóng** | Bước 8 dòng 143 | `*.local.json` |
| N19 | **Đóng** | Bước 7 dòng 135; Verify dòng 261 | |
| N20 | **Đóng** | Bước 5 dòng 114; smoke 8 | Extra `overlay_on` chỉ bản debug; có `uiautomator dump` |
| N21 | **Đóng** | Bước 4 dòng 103 | |
| Mục mở — điều kiện hình ảnh | **Đóng** | Bước 6.3 dòng 125 | HDR, chế độ ảnh, ALLM, độ phân giải + tần số |
| Mục mở — Q1 không chặn Bước 7 | **Đóng** | U4 dòng 41; Bước 7 dòng 134 | |
| Mục mở — bẫy chữ ký debug | **Đóng** | Bước 4 dòng 104 | |
| Mục mở — "4 góc" = mặt phẳng đồ hoạ | **Đóng** | Bước 5 dòng 116 | |

## NOTE mới do delta sinh ra

| # | Chỗ | Vấn đề | Đề xuất |
|---|---|---|---|
| D1 | TDD `ScoreOcr` case "Hàng nhiều cột bị gộp" (dòng 187) | Kỳ vọng (b) ghi **4/4 từ**, nhưng golden `["Board", "Season Objectives Update", "Today"]` có **5** từ ⇒ test viết theo bảng sẽ đỏ với code đúng quy tắc dòng 91. Quy tắc viết rõ nên Dev vẫn suy ra được (không phải BLOCKER), nhưng đúng loại lỗi khiến Dev nghi ngờ code của mình | Sửa thành **5/5 từ** |
| D2 | Quy tắc (b) dòng 91 + golden dòng 87 | (b) so theo **thứ tự** từ, nhưng doc không nói thứ tự các đoạn trong golden. Hộp thư nhiều cột: ghi golden theo hàng (người gửi → tiêu đề → ngày, hết hàng xuống hàng) hay theo cột cho ra (b) khác hẳn nhau. Chỉ ảnh hưởng chỉ số tham khảo (b) | Ghi: golden xếp theo **thứ tự đọc** — trên xuống dưới, trong một hàng trái sang phải |
| D3 | Smoke bước 3 (dòng 228) | Cửa sổ thay thế là **app Photos** (giao diện mới, tự vẽ thanh tiêu đề **bên trong vùng client**) ⇒ sau khi cắt client vẫn còn dải tiêu đề/thanh công cụ của Photos ⇒ tiêu chí "không có thanh tiêu đề" có thể không đạt dù code cắt client đúng. Dev sẽ đi sửa nhầm code (`assumption` — hành vi giao diện Photos trên Windows 11; kiểm bằng chính bước smoke) | Dùng cửa sổ có thanh tiêu đề chuẩn Win32 làm cửa sổ thay thế, vd `ffplay <ảnh>` (nếu bản ffmpeg trên PATH có kèm `ffplay`) hoặc một cửa sổ WinForms tối giản trong dự án test; hoặc đổi tiêu chí thành "rect client trong `capture.json` nhỏ hơn khung WGC đúng bằng phần non-client" |
| D4 | Verify dòng 261 "ngoại lệ duy nhất: … Gradle + font" | Build Gradle còn tải Android Gradle Plugin, Kotlin, androidx từ Maven (`google()`, `mavenCentral()`), và `dotnet restore` tải xUnit + gói interop từ NuGet ⇒ đọc đúng chữ thì build vi phạm dòng "không được xảy ra" | Thêm vào ngoại lệ: tải phụ thuộc build từ Maven Google/Central và NuGet.org. Đây là phụ thuộc lúc build, không phải runtime gọi mạng |
| D5 | Dòng 223 | Còn `playtest/epic1-phase1/<vai>/` không tiền tố (sót N10) | Viết đủ `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/<vai>/` |
| D6 | Bước 8 dòng 143 vs U5 dòng 42 | Mẫu `.gitignore` cho `_local/` (F3 video, F4 ảnh nhà User) chỉ được thêm ở **Bước 8** (cuối Phase), trong khi User đặt F3/F4 vào thư mục ngay từ U5 ⇒ trong suốt Phase các file này là untracked, một `git add -A` là lọt ảnh riêng tư/video lớn vào commit | Chuyển dòng `.gitignore` (`*.local.json` + `engine/tests/fixtures/**/_local/`) lên **Bước 1** (lúc Leader tạo thư mục fixture) |

## Mục mở — Rev 2 có làm gì tệ hơn hay sinh chỗ đoán mới không?

- **Tệ hơn**: không. Các thay đổi đều thu hẹp lựa chọn của Dev.
- **Chỗ đoán mới**: ba chỗ nhỏ, không chặn: D1 (số kỳ vọng sai, quy tắc đúng), D2 (thứ tự golden cho chỉ số phụ), D3 (cửa sổ thay thế cho smoke có thể làm tiêu chí "không có thanh tiêu đề" không đạt vì lý do không phải lỗi code).
- **Mâu thuẫn mới**: D4 (dòng "không gọi mạng" chặn luôn việc tải phụ thuộc build mà Bước 4 đòi) và D6 (gitignore thêm sau khi file đã có).
- Quy tắc (a) so **chuỗi con theo ký tự**: đoạn golden rất ngắn (vd "Today", "Board") có thể khớp nhờ trùng vào một dòng OCR khác, nên (a) có thể hơi lạc quan. Chỉ số phụ (b) và danh sách đoạn không khớp đã bù phần nào; Leader cân nhắc yêu cầu khớp **theo biên từ** (không cắt giữa từ). Không sửa cũng chấp nhận được ở mức spike.
