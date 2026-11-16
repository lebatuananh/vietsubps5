DEV-FEEDBACK DONE
# 261114 — Epic 1 Phase 1 — Dev feedback (turn 1, chỉ đọc, chưa viết code)

**Vai**: Dev · **Đã đọc trọn**: `261114-epic1-phase1.md` Rev 2.1, `261114-epic1.md` (mục *Định hướng User đã chốt*), `261114-discovery.md` (R1 + R2, gồm §3.5, §3.6, R2.2–R2.3), `reviews/261114-epic1-phase1-sa-review.md` (cả re-review Rev 2), `CLAUDE.md`, `workflow/roles/dev.md`, `workflow/guidelines/blackbox-android-adb.md`, `workflow/guidelines/tdd.md`, `project-docs/automation/README.md`.
**Không** đụng code/config/`.gitignore`/file sẵn có; chỉ tạo file này và file token.

## 0. Probe (đúng brief)

- Token: `dev-token-in.txt` = `dc0d2f9c8b01` → đã ghi `dev-token-out.txt` = `dc0d2f9c8b01-dev`.
- `git rev-parse --short=8 HEAD` → `8101fc74` (nhánh `features/261114-fc26-vietsub-overlay`).
- `dotnet --list-sdks` →
  ```
  8.0.121 [C:\Program Files\dotnet\sdk]
  10.0.404 [C:\Program Files\dotnet\sdk]
  ```
  ⇒ U1 đã thoả (có `10.0.x`).
- Quyền shell: Bash chạy được. Một lệnh Bash **ghép nhiều phần** (`cd … ; sed ; ls /c/ffmpeg… ; powershell.exe -Command …`) bị từ chối nên em **không** thử lại biến thể; các việc đó nằm trong mục 3 (quyền cần cho turn 2). Dùng công cụ Grep/Glob đọc được: `.gitignore` root và `ffplay.exe` (xem mục 1.4).

## 1. Doc có đủ để code không?

**Kết luận: gần đủ, KHÔNG đủ để code "không đoán" — còn 12 chỗ phải đoán (mục 1.1), 8 chỗ mâu thuẫn/sai thứ tự (1.2–1.3). Không có BLOCKER theo nghĩa dev.md (không chỗ nào chặn hoàn toàn việc bắt đầu), nên em đề nghị Leader chốt từng dòng "Quyết định đề xuất" bên dưới rồi cho em code; chỗ nào Leader im lặng em sẽ làm đúng như cột đề xuất và ghi vào dev report.** Không phải "đã đọc, đủ để code, không nghi ngờ".

### 1.1 Chỗ em phải đoán (kèm quyết định đề xuất)

| # | Chỗ | Vì sao phải đoán | Em sẽ làm (đề xuất) |
|---|---|---|---|
| G1 | **`--frames N` của `engine capture`** (Bước 3, smoke 3) | WGC chỉ phát khung **khi nội dung cửa sổ đổi**. Cửa sổ thay thế (`ffplay` hiện ảnh tĩnh) có thể chỉ cho 1–2 khung rồi im ⇒ `--frames 3` treo hoặc thiếu. Doc không nói "N khung" là N khung *khác nhau* hay N lần lấy mẫu | `--frames N` = lấy mẫu N lần cách nhau `--interval-ms` (mặc định 500) bằng **khung mới nhất đang có**; chưa nhận khung nào sau `--timeout-ms` (mặc định 5000) ⇒ lỗi rõ, exit ≠ 0 (không treo). Ghi cả `capturedAtUtc` mỗi khung vào `capture.json` |
| G2 | **Nhiều cửa sổ khớp tiêu đề** (Bước 3) | "Tìm HWND cửa sổ top-level có tiêu đề chứa chuỗi" — không nói khi có ≥ 2 khớp, cửa sổ ẩn, cửa sổ UWP bị *cloaked*. `PS Remote Play` còn có thể khớp thư mục Explorer/ tab trình duyệt cùng tên | Chỉ xét cửa sổ `IsWindowVisible` và không `DWMWA_CLOAKED`; khớp ≠ 1 ⇒ lỗi, in danh sách (HWND, tiêu đề, kích thước), exit ≠ 0; thêm `--hwnd` để chỉ định thẳng (cần cho Bước 7 nếu tiêu đề thật mơ hồ) |
| G3 | **Ngưỡng `DetectContentRect`** (Bước 2) | Doc nói "ngưỡng độ sáng + tỉ lệ điểm sáng tối thiểu mỗi hàng/cột" nhưng không cho số; dung sai "gần 16:9" cũng không có | Hằng số có tên + comment: luma ≤ 24 coi là "đen" (video limited-range có thể ra ~16, không phải 0), hàng/cột là nội dung nếu ≥ 1% điểm sáng, 16:9 dung sai ±1,5%. **Đặt trước từ bảng TDD, không chỉnh cho đến khi test xanh nhờ dữ liệu tổng hợp** (chống kiểu hardcode). Số thật chỉ chỉnh được sau khi có F1 ⇒ ghi `chưa kiểm` |
| G4 | **Biên từ trong `ScoreOcr` (a)** | "Không bắt đầu/kết thúc giữa một từ": ký tự liền kề là dấu câu (`Today,`) có tính là biên không? | Biên = ký tự liền kề **không phải** letter/digit (`char.IsLetterOrDigit`) hoặc đầu/cuối chuỗi. `Boardroom` vs `Board` → không khớp (đúng bảng TDD); `Today,` vs `Today` → khớp |
| G5 | **`Normalize` khi box tràn mép** | "Kẹp trong [0,1], w/h không âm" — kẹp từng thành phần hay kẹp hai góc? Kẹp từng thành phần cho `x+w > 1` | Kẹp **hai góc** (x1,y1,x2,y2) vào [0,1] rồi tính lại w,h ≥ 0 |
| G6 | **Schema `capture.json` / `<ảnh>.ocr.json` / `<ảnh>.golden.json`** | Doc chỉ liệt kê vài trường; `ocr.json` được mô tả là "mảng dòng" nhưng còn phải chứa tỉ lệ thu nhỏ/ phóng 2×, rect nội dung, `confident`. Quy tắc vàng 5 (không tự chế format) ⇒ phải chốt và ghi `engine/README.md` | `capture.json`: `{window:{hwnd,title}, wgcFrameSize, clientRect, contentRect, confident, meanLuma, frames:[{file,capturedAtUtc}]}`. `ocr.json`: `{image, imageSize, contentRect, confident, scaleApplied, variant, lines:[{text, boxPx, box01}]}` (`boxPx`/`box01` = `{x,y,w,h}`; `boxPx` luôn theo ảnh **gốc đã cắt client**, kể cả biến thể 2×). `golden.json`: mảng chuỗi theo thứ tự đọc. Ghép `<ảnh>.ocr.json` ↔ `<ảnh>.golden.json` theo **tên không đuôi**. **Phase 3 có đọc `capture.json`/`ocr.json` không?** Nếu có, Leader chốt schema ngay bây giờ |
| G7 | **Golden: ghi những chữ nào?** (Bước 3) | Doc định nghĩa "đoạn" nhưng không nói: có ghi chữ ngoài Hộp thư (thanh trên: tên CLB, ngân sách, ngày), chữ của nút/ gợi ý tay cầm, chữ nhỏ/nhoè mà em không đọc chắc? Ghi thừa/ thiếu làm (a) lệch cả hai phía | Đề xuất: golden = **mọi** đoạn tiếng Anh đọc rõ trên ảnh (cả thanh trên, nhãn nút), bỏ ký hiệu icon (△ ○ R1…); đoạn em **không đọc chắc** thì **không** đưa vào golden mà liệt kê ở `golden/_unreadable.md` (để Reviewer soát độc lập, không làm (a) đẹp giả). Leader xác nhận quy tắc này |
| G8 | **Smoke OCR khi chưa có F1** (smoke 5 vs mục *Data*) | Smoke 5 trỏ `.../f1-remoteplay` (rỗng đến Bước 7), còn mục *Data* nói dùng ảnh tổng hợp. Tổng hợp để ở đâu, golden của nó là gì? | Ảnh tổng hợp sinh bằng `ffmpeg` `drawtext` (font hệ thống), để ở `project-docs/…/playtest/epic1-phase1/dev/synthetic/` (**không** vào `engine/tests/fixtures/fc26/…` để khỏi lẫn với F1), golden của nó = đúng chuỗi em đã vẽ. Số (a)/(b) trên đó chỉ chứng minh **đường chạy**, ghi rõ "không phải R-D" |
| G9 | **Tên/ vị trí file nhỏ** | `.sln` ở đâu; `Engine.Tests` ở đâu; `Activity` tên gì; dự án test chứa cửa sổ WinForms (smoke 3 phương án dự phòng) | `engine/Engine.slnx` (.NET 10 `dotnet new sln` mặc định ra `.slnx`; chỉ **một** file sln/slnx trong `engine/` để `dotnet build engine` không lỗi mơ hồ) · `engine/src/Engine/` · `engine/tests/Engine.Tests/` · `vn.vietsub.ps5.tv.MainActivity` · không cần WinForms vì **`ffplay.exe` có thật** ở `C:\ffmpeg-n8.1.2-win64-gpl-8.1\bin` (đã Glob) |
| G10 | **Phiên bản công cụ build Android** | Doc: "bản Gradle ổn định tương thích AGP đã chọn" — nhưng không chọn AGP/Kotlin/`compileSdk`. Và JBR của máy là **openjdk 25.0.4** (`automation/README.md`) | Gradle chạy trên JDK 25 cần **Gradle ≥ 9.1** (theo hiểu biết của em, `chưa kiểm` — em sẽ đối chiếu *compatibility matrix* chính thức trước khi chọn) ⇒ phải chọn cặp AGP/Kotlin/Gradle hỗ trợ cả hai; `compileSdk 35` (platform 35 đã cài); nếu `androidx.window` bản mới đòi `compileSdk 36` thì ghim bản cũ hơn **hoặc** cần cài `platforms;android-36` (xin quyền, mục 3). Nếu ma trận không khớp JDK 25 ⇒ dừng báo Leader, **không** tự cài JDK khác |
| G11 | **Hình học dấu góc & lưới** (Bước 5) | "Dấu góc chữ L … tại (0,0)…" — nét 6 dp vẽ *tâm* trên mép sẽ bị cắt nửa; "lưới 10%" là bước lưới 10% hay độ đục 10%? | Đỉnh L đúng ở góc, **hai cạnh vẽ vào trong** View (nét nằm hẳn trong View); lưới = 10 ô theo mỗi chiều (đường ở 10%, 20%…), nét mảnh, độ đục 30% |
| G12 | **Phần "Đo được" của R-C** | "PNG không đen" chỉ kiểm bằng mắt | Em thêm `meanLuma` vào `capture.json` (1 dòng, hữu ích cho Leader kiểm không cần mở ảnh). Nếu Leader coi là mở rộng scope thì bỏ |

### 1.2 Mâu thuẫn / sai thực tế

1. **AVD image**: Phase doc U2 + dòng *Môi trường* viết `system image Android TV x86_64 API 31`; `automation/README.md` ghi image đã cài là `system-images;android-31;google-tv;x86` và **"API 31 không có bản x86_64"**. Em dùng image **x86** đã cài (sẽ chạy `emulator -accel-check`/WHPX đã báo usable). Doc nên sửa chữ "x86_64".
2. **Mục *Hiện trạng* đã cũ** so với `automation/README.md` (2026-11-15): .NET 10.0.404, Android Studio, platform-tools/emulator/`platforms;android-35`/`build-tools;35.0.0` **đã cài**; chỉ là không có trong PATH (probe dotnet của em xác nhận 10.0.404). U1/U2 coi như xong, chỉ còn tạo AVD.
3. **PowerShell 5.1 làm hỏng PNG**: smoke 8 viết `adb … exec-out screencap -p > …png` và Bước 6.6 (lệnh Leader đưa **User** chạy) cũng `exec-out screencap -p > hdmi.png`. Toán tử `>` của Windows PowerShell 5.1 ghi qua mã hoá văn bản ⇒ PNG hỏng. Em sẽ chạy bằng **Bash tool** (chuyển hướng nhị phân đúng) hoặc `adb shell screencap -p /sdcard/x.png` + `adb pull`. **Leader cần chỉnh lệnh Bước 6.6 gửi User**: dùng `cmd /c "adb … exec-out screencap -p > hdmi.png"` hoặc `adb shell screencap -p /sdcard/hdmi.png` + `adb pull /sdcard/hdmi.png`, kẻo User lấy file hỏng rồi kết luận nhầm "HDMI đen".
4. **`.gitignore` root hiện KHÔNG có** `playtest/`, `out/`, `*.local.json`, `engine/tests/fixtures/**/_local/` (đã Grep: chỉ có `.claude/*.local.json`, `*.local.md`…). Mục *Dữ liệu test* nói evidence **để lại, không commit script**; `dev.md` §3 nói thư mục playtest "gitignore". Hiện `project-docs/261114-…/playtest/…` sẽ hiện là untracked và `git add -A` lôi vào. Em sẽ thêm `*.local.json` + `_local/` ở Bước 1 (D6) như doc; còn `playtest/` ⇒ **Leader quyết**: gitignore `project-docs/*/playtest/` hay commit evidence ảnh/JSON (và chỉ loại script)? Em đề xuất gitignore script (`playtest/**/*.ps1|*.sh`), commit ảnh/JSON kết quả (nhỏ) để Reviewer/Leader đọc được trên nhánh.
5. **Cấu hình DPI**: Bước 1 gợi ý "app manifest `dpiAwareness`, hoặc `ApplicationHighDpiMode`". `ApplicationHighDpiMode` chỉ của WinForms (không dùng). Với cách gọi chuẩn `dotnet run --project …`, manifest nhúng trong **apphost** có thể không phải thứ quyết định nếu tiến trình chạy qua `dotnet.exe` (em **chưa kiểm** — nghi ngờ, không khẳng định). Em sẽ gọi `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)` ở **dòng đầu `Main`**, in kết quả awareness ra log, và kiểm bằng smoke 3 (kích thước client trùng khung WGC).
6. **Smoke bước 8 sai thứ tự để kiểm N17(iv)**: doc muốn kiểm "focus mặc định = nút việc tiếp theo chưa xong", nhưng bước 8 chạy `appops set … allow` **trước** `uiautomator dump` ⇒ chỉ còn kiểm được trạng thái "đã có overlay, chưa miễn pin" và không bao giờ thấy trạng thái "chưa có quyền ⇒ focus nút cấp quyền". Em sẽ dump **3 lần**: (i) trước khi cấp quyền; (ii) sau `appops allow` + mở lại; (iii) sau `dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (cũng kiểm luôn lệnh dự phòng đang ghi `chưa kiểm` — chỉ trên emulator của em).
7. **Tự tắt 30 phút không kiểm được bằng smoke**: bản đồ case chỉ "đọc code". Em đề xuất thêm extra **debug-only** `--el auto_off_ms <số>` (cùng cơ chế với `overlay_on`, release không có) để smoke chứng minh service tự gỡ cửa sổ trong ~10 s trên emulator. Nhỏ, cùng loại N20. Leader duyệt (nếu không, em chỉ đọc code như doc).
8. **Bẫy chữ Việt bị font hệ thống che**: `CLAUDE.md` cảnh báo font thiếu dải dấu ⇒ Android lấy font hệ thống từng chữ, "lỗi dễ lọt qua review ảnh". Smoke 8 chỉ xem ảnh. Em sẽ thêm, trong bản **debug**, một dòng `Log.i` kết quả `Paint.hasGlyph()` của typeface Be Vietnam Pro cho toàn dải `U+1EA0–U+1EF9` + chuỗi mẫu (không đổi UI) và đưa vào evidence. Leader duyệt.

### 1.3 Thứ tự bước nhìn có vấn đề

- U5/Prerequisites nói F1 **chặn Bước 3** — nhưng code Bước 3 không cần F1, chỉ **số đo** cần. Em code B1→B5 hết, rồi để R-D `chưa kiểm` tới Bước 7 (đúng tinh thần doc); đề nghị Leader sửa chữ "chặn Bước 3 (đo OCR)" cho khỏi hiểu là chặn code.
- Smoke 5 chạy trên thư mục F1 **rỗng** (G8).
- Smoke 8: thứ tự dump/appops (1.2 #6).
- Bước 4: `.gitignore` module + root `.gitignore` **cùng lúc** với tạo thư mục fixture; Git không giữ thư mục rỗng ⇒ cần `.gitkeep` ở `f1-remoteplay/ f2-ps5/ f3-frames/ golden/` (em sẽ thêm; `_local/` bị ignore nên sẽ **không** tồn tại trên bản clone mới — em ghi vào `engine/README.md`).

### 1.4 Quan sát đã verify bằng đọc/Glob/Grep (không phải suy đoán)

- `ffplay.exe` **có** tại `C:\ffmpeg-n8.1.2-win64-gpl-8.1\bin\ffplay.exe` ⇒ cửa sổ thay thế có thanh tiêu đề Win32 chuẩn (D3) dùng được, không cần WinForms.
- Bảng TDD số đúng với quy tắc: bảng `DetectContentRect` (1920×1200 − 2×60 = 1080 ✓; 2000 − 2×40 = 1920 ✓), `Normalize` ((160−60)/1080 ✓), `ScoreOcr` hàng gộp = 5/5 từ ✓ (D1 đã sửa), sai 1 ký tự (b) = 2/3 ✓. Số test dự kiến: `DetectContentRect` 6 + `Normalize` 4 + `ScoreOcr` 8 = **18** (xUnit) · ánh xạ pixel 3 (JUnit). Smoke 2 "đúng số test theo bảng" em hiểu là 18 + 3 (nếu dùng `[Theory]` em báo số *case*, không phải số method).

## 2. Cách tiếp cận từng Bước (không code)

### Bước 1 — `engine/`
- `engine/Engine.slnx` · `src/Engine/Engine.csproj` (Exe, `net10.0-windows10.0.22621.0`, nullable, implicit usings) · `tests/Engine.Tests/Engine.Tests.csproj` (cùng TFM — project test tham chiếu được project Exe cần cùng TFM windows). Logic để `public` trong `Engine`, `Main` tường minh (không top-level) để test project tham chiếu sạch.
- CLI: lớp `Cli` tự parse `--key value` + bảng lệnh `capture|ocr|ocr-score`; không thêm thư viện CLI.
- NuGet: `xunit` 2.9.x (Apache-2.0), `xunit.runner.visualstudio` (Apache-2.0), `Microsoft.NET.Test.Sdk` (MIT). **Cố ý chọn xUnit v2**: v3 chạy trên Microsoft.Testing.Platform và `dotnet test` của .NET 10 đòi bật chế độ riêng qua `global.json` — thêm file cấu hình ngoài doc. WinRT lấy qua TFM (`Microsoft.Windows.SDK.NET.Ref`, tự restore từ NuGet).
- Thư mục fixture + `.gitkeep`; `.gitignore` root thêm `*.local.json`, `engine/tests/fixtures/**/_local/` (+ quyết định `playtest/` ở 1.2 #4); `engine/README.md` ghi: cách gọi chuẩn, TFM + lý do, gói + giấy phép, bẫy WGC/DPI/`CreateFreeThreaded`, schema G6, bẫy PowerShell `>` (1.2 #3).

### Bước 2 — Hàm thuần (TDD)
- `Geometry/ContentRect.cs` (record struct), `Geometry/ContentRectDetector.cs`, `Geometry/Normalizer.cs`. Hàm nhận `ReadOnlySpan<byte> bgra, width, height, stride` — không phụ thuộc WinRT nên test được bằng ảnh dựng trong test.
- Thuật toán: luma nguyên (`(54R+183G+18B)>>8`), quét hàng/cột, đếm điểm > ngưỡng, lấy hàng/cột đầu–cuối đạt tỉ lệ; so tỉ lệ với 16:9; toàn đen/không có rect ⇒ cả ảnh + `confident=false`.
- Quy trình dev.md: **Stub (`throw NotImplementedException`) → viết 6+4 test → chạy, chụp output ĐỎ (runtime failure, không phải lỗi biên dịch) → implement → XANH**. Em lưu output đỏ vào dev report.

### Bước 3 — Chụp + OCR
- Capture: P/Invoke tối thiểu tự khai báo (user32/dwmapi: `EnumWindows`, `GetWindowText`, `IsWindowVisible`, `IsIconic`, `GetClientRect`, `ClientToScreen`, `DwmGetWindowAttribute`); COM `IGraphicsCaptureItemInterop::CreateForWindow` (đây là đoạn **rủi ro cao nhất** — COM/WinRT interop trên .NET 10 chưa kiểm). D3D11: để lấy pixel cần staging texture + `Map`; khai báo tay vtable D3D11 dễ sai ⇒ em nghiêng về **`Vortice.Direct3D11` + `Vortice.DXGI` (MIT)** cho thiết bị/ staging, tay khai báo chỉ phần WinRT interop. Nếu Leader muốn không thêm gói ⇒ nói, em tự khai báo (tốn thời gian hơn, rủi ro hơn). Ghi giấy phép vào README.
- Ghi PNG + đọc PNG bằng `Windows.Graphics.Imaging` (`BitmapEncoder`/`BitmapDecoder`/`SoftwareBitmap`) — **không** thêm gói ảnh; cũng là đầu vào của `OcrEngine`.
- OCR: `OcrEngine.TryCreateFromLanguage(new Language("en-US"))`, `null` ⇒ lỗi rõ + exit ≠ 0. Biến thể 2×: `BitmapTransform` (Fant) và quy toạ độ về ảnh gốc; kiểm `MaxImageDimension` (2880×1800 → 5760 phải còn nhỏ hơn giới hạn, nếu vượt thì thu lại và ghi `scaleApplied`).
- `ScoreOcr` thuần, tách file, TDD 8 case bảng; in theo ảnh + tổng; đoạn không khớp kèm dòng OCR gần nhất (khoảng cách chỉnh sửa đơn giản, tự viết).
- Smoke trước khi có F1: `ffplay -window_title VietSubTest <png tổng hợp>` bằng `Start-Process`, theo công thức tách tiến trình (guideline §7), tắt đúng PID của em.

### Bước 4 — `tv-overlay/` khung
- Gradle Kotlin DSL, một module `app`; wrapper sinh từ bản phân phối chính thức tải vào thư mục **ngoài repo**, kiểm SHA-256 công bố, `gradle wrapper --gradle-version <x>`; commit `gradlew`, `gradlew.bat`, `gradle/wrapper/*`; chạy xong `gradlew --stop` (daemon do em bật thì em tắt).
- Phụ thuộc: AGP + Kotlin Gradle plugin (Apache-2.0), `androidx.core:core-ktx` (Apache-2.0, cho `ServiceCompat`), `androidx.window:window` (Apache-2.0, cho `WindowMetricsCalculator`), test: `junit:junit:4.13.2` (EPL-1.0). **Không** dùng AppCompat/Leanback/Compose — Activity thuần, UI dựng bằng code Kotlin (không XML layout) để ít phụ thuộc, ít giấy phép.
- Font: Be Vietnam Pro Regular + SemiBold (OFL 1.1) từ nguồn chính thức, đặt `res/font/be_vietnam_pro_regular.ttf`, `be_vietnam_pro_semibold.ttf`, kèm `OFL.txt`; README ghi nguồn + giấy phép. Banner leanback bằng drawable vector (không cần ảnh nhị phân).
- Manifest: đúng 4 thứ `specialUse`, `SYSTEM_ALERT_WINDOW`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`; **không** `INTERNET`, **không** `POST_NOTIFICATIONS`. `buildFeatures.buildConfig=true` để có `BuildConfig.DEBUG` cho extra debug.
- `minSdk 29`, `targetSdk 35`, `compileSdk 35`, Java/Kotlin target 17 (chạy Gradle trên JDK 25 nhưng bytecode 17) — `chưa kiểm` tương thích, phụ thuộc G10.

### Bước 5 — APK kiểm tra
- `MainActivity` (Activity thuần): chữ tiếng Việt, 3 nút + dòng trạng thái, làm mới ở `onResume`, `requestFocus` nút việc kế tiếp; hai intent bọc `try/catch ActivityNotFoundException` + lệnh dự phòng hiện trên màn; đo kích thước bằng `WindowMetricsCalculator`.
- `OverlayService` (foreground): thêm cửa sổ overlay **trước**, rồi `ServiceCompat.startForeground(..., SPECIAL_USE)`; `START_NOT_STICKY`; `Handler.postDelayed` 30 phút tự `stopSelf` + gỡ cửa sổ; kênh notification tối thiểu; chỉ nhận extra `overlay_on` khi `BuildConfig.DEBUG`.
- `OverlayView` (Canvas + `StaticLayout`) + `CoordinateMapper.kt` thuần (JUnit 3 case: 4 góc, cỡ khác, ngoài miền) — TDD đỏ→xanh như Bước 2.
- Smoke emulator: AVD `vietsub_dev` từ image google-tv x86 API 31, `-port 5556`, bật **tách tiến trình**, tắt đúng cây tiến trình, kiểm `adb devices` + cổng 5556/5557 đóng.

## 3. Lệnh + quyền em cần ở turn 2 (để Leader cấp)

Phải cấp thêm cho **ghi/đọc ngoài** thư mục làm việc (sandbox từng chặn đọc ngoài workdir): `C:\Users\lebatuananh\.nuget`, `.gradle`, `.android` (AVD, adb key), `%LOCALAPPDATA%\Android\Sdk`, `C:\Program Files\Android\Android Studio\jbr`, `C:\ffmpeg-n8.1.2-win64-gpl-8.1\bin`, và thư mục tải tạm `D:\Temp\claude\…\scratchpad\probe\` (đang là thư mục phụ được phép — em đặt bản phân phối Gradle + bản tải font ở đó, không tạo scratchpad cha). **Mạng** (chỉ lúc dựng — D4): `api.nuget.org`, `services.gradle.org` (+ chuyển hướng tới `downloads.gradle.org`/GitHub releases), `dl.google.com`, `maven.google.com`/`dl.google.com/dl/android/maven2`, `repo.maven.apache.org`, `plugins.gradle.org`, `github.com`/`raw.githubusercontent.com` hoặc `fonts.google.com` (font + OFL).

Môi trường (PowerShell, đặt trong tiến trình, không sửa biến máy):
```
$env:JAVA_HOME='C:\Program Files\Android\Android Studio\jbr'
$env:ANDROID_HOME="$env:LOCALAPPDATA\Android\Sdk"
$env:PATH="$env:JAVA_HOME\bin;$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\emulator;$env:PATH"
```
Nhóm lệnh (mỗi lệnh chạy riêng, **không** ghép nhiều phần trong một dòng Bash vì dạng đó bị từ chối):
- **Git chỉ đọc**: `git status --short`, `git branch --show-current`, `git rev-parse --short=8 HEAD`, `git diff`, `git ls-files`, `git check-ignore -v <path>`, `git status --ignored --short`. (Không `add/commit/checkout/stash/reset`.)
- **.NET**: `dotnet --list-sdks`, `dotnet restore engine`, `dotnet build engine`, `dotnet test engine`, `dotnet test engine --filter <…>`, `dotnet add <csproj> package <id> --version <v>`, `dotnet new sln -n Engine --format slnx` (hoặc em tự viết csproj/slnx bằng Write), `dotnet run --project engine/src/Engine -- capture|ocr|ocr-score …`.
- **Tải & kiểm**: `Invoke-WebRequest` (Gradle dist, TTF, OFL), `Get-FileHash -Algorithm SHA256`, `Expand-Archive`.
- **Gradle**: `<dist>\bin\gradle.bat wrapper --gradle-version <x>`, `.\gradlew.bat :app:testDebugUnitTest`, `:app:assembleDebug`, (tuỳ) `:app:lintDebug`, `.\gradlew.bat --stop`. Lần build đầu có thể >10 phút ⇒ em chạy nền (`run_in_background`) ghi log.
- **SDK/AVD**: `cmd /c ""%ANDROID_HOME%\cmdline-tools\latest\bin\avdmanager.bat" create avd -n vietsub_dev -k "system-images;android-31;google-tv;x86" -d tv_1080p"` (gói có `;` phải qua `cmd /c` + ngoặc kép — bẫy ghi ở README), `emulator -list-avds`, `emulator -accel-check`, (nếu cần) `sdkmanager --install "platforms;android-36"` (chỉ khi G10 đòi, có chấp nhận licence).
- **Emulator** (guideline §7): `Start-Process -FilePath cmd.exe -ArgumentList '/c','"%ANDROID_HOME%\emulator\emulator.exe" -avd vietsub_dev -port 5556 -no-window -gpu swiftshader_indirect -no-audio -no-boot-anim -no-snapshot > <log> 2>&1' -WindowStyle Hidden -PassThru`; tắt: `taskkill /T /F /PID <pid của em>` (hoặc `Stop-Process`), rồi `Get-NetTCPConnection -LocalPort 5556,5557`.
- **adb — luôn `-s emulator-5556`, không `adb connect`, không IP LAN**: `adb devices`, `adb -s emulator-5556 shell getprop sys.boot_completed`, `install -r <apk>`, `shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`, `shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`, `shell am start -n vn.vietsub.ps5.tv/.MainActivity --ez overlay_on true`, `shell am force-stop`, `shell input keyevent …`, `shell dumpsys package|activity services vn.vietsub.ps5.tv`, `exec-out uiautomator dump /dev/tty`, `exec-out screencap -p > <png>` (**Bash tool**, không PowerShell 5.1), `logcat -d -s <tag>`.
- **Cửa sổ thay thế**: `ffmpeg -f lavfi -i color=…` / `drawtext` để sinh ảnh tổng hợp; `Start-Process ffplay -ArgumentList '-window_title','VietSubTest',<png>`; thu nhỏ cửa sổ cho smoke 4 cần `Add-Type` P/Invoke `ShowWindow` trong PowerShell (script đặt trong `playtest/…/dev/`).
- **Chỉ đọc môi trường**: `Test-Path`, `Get-ItemProperty 'HKCU:\Control Panel\Desktop\WindowMetrics'` (mức DPI), lệnh kiểm OCR `en-US` ở discovery R2.3 (Leader đã probe `en-US`; em chạy lại một lần để ghi vào report).

## 4. Rủi ro khi User vắng nhà (không PS5, không TV, chưa có F1)

1. **R-D không thể đo thật ở turn 2.** `DetectContentRect`, ngưỡng, golden đều chỉ kiểm bằng ảnh tổng hợp ⇒ nguy cơ "xanh trên tổng hợp, đỏ trên stream thật" (video limited-range làm viền "đen" không đen; nén làm nhiễu viền). Em đặt ngưỡng từ trước (G3), không chỉnh theo dữ liệu chưa thấy, và ghi đúng "chưa chứng minh" trong report. Deliverable 1 (số % trên F1) **chắc chắn chưa đóng được** ở turn 2.
2. **Cửa sổ PS Remote Play thật có thể khác cửa sổ Win32 chuẩn**: nếu app tự vẽ thanh tiêu đề *trong vùng client* (như Photos, D3) thì phép cắt client **đúng mà ảnh vẫn còn thanh tiêu đề**, R-C "rect ~16:9, không còn thanh tiêu đề" sẽ không đạt dù code đúng — chỉ lộ ở Bước 7. Cần Leader biết trước để không quy lỗi cho code; phương án nếu xảy ra: tham số cắt tay `--crop-top` (mở rộng nhỏ, em **không** làm nếu không được duyệt).
3. **WGC chỉ phát khung khi nội dung đổi** (G1): smoke 3 với ảnh tĩnh dễ treo/thiếu khung — đã có quy tắc timeout.
4. **Interop COM/WinRT trên .NET 10 là chỗ dễ mất nhiều giờ nhất** (WGC `CreateForWindow`, staging texture, ARM/`HD color`). Không có test đơn vị cho phần này; chỉ có smoke với `ffplay`. Nếu kẹt >1 ca, em dừng và báo (dev.md §2 "gặp blocking"), không đổi sang `GraphicsCapturePicker` âm thầm (phương án đó cần User bấm — User đang vắng).
5. **Chuỗi công cụ Android lần đầu trên máy này**: JBR 25 + Gradle/AGP/Kotlin, tải hàng trăm MB, `x86` google-tv API 31 trên CPU AMD (WHPX). Có thể chạy >10 phút/ lệnh; có thể kẹt vì bản AVD (image google-tv không root; không cần `adb root`). Dự phòng: em ghi log, hạ idle ~600 s theo guideline §7.
6. **Emulator xanh ≠ TV xanh**: `specialUse` + `ServiceCompat` trên API 29–33 (SA đã nêu), `appops`, màn cấp quyền thiếu trên TV — emulator API 31 chỉ phủ **một** điểm trong dải Android 10–14 của TV Sony. Mọi kết luận R-A/R-B chỉ ở Bước 6 (User).
7. **Phiên làm việc dài + giới hạn usage**: lượt trước của em đã bị cắt vì usage limit. Turn 2 gồm 2 module + emulator + 3 lần dump + golden. **Đề xuất tách turn 2 thành 2a (B1–B3, `engine`) và 2b (B4–B5, `tv-overlay`)** — hai nửa độc lập nhau, mỗi nửa có báo cáo + mục "chưa chứng minh được" riêng.
8. **Không có người nhìn ảnh**: golden và kiểm "chữ Việt đủ dấu" đều do chính em đọc ảnh; dev.md cấm tự phê duyệt ⇒ Reviewer soát ≥ 30% golden (đã có) và em bổ sung kiểm máy `hasGlyph` (1.2 #8).
9. **Đụng laptop của User**: `engine capture` đọc cửa sổ theo chuỗi tiêu đề trên desktop đang có người dùng ⇒ lỡ khớp cửa sổ riêng tư (ví dụ trình duyệt). Quy tắc G2 (khớp ≠ 1 ⇒ lỗi + liệt kê) giảm rủi ro; evidence PNG chụp từ `ffplay` của em, không chụp cửa sổ khác.

## 5. Câu hỏi mở — Leader chưa hỏi gì, khái niệm nào bị gộp/đặt sai, giả định nền có sai không

**Điều quan trọng nhất Leader chưa hỏi: không có bước nào của Phase này kiểm *mối nối* giữa hai hệ toạ độ.** R-B chỉ chứng minh *mặt phẳng đồ hoạ Android ≈ hình HDMI trên TV*; R-C chỉ chứng minh *cửa sổ Remote Play → rect nội dung*. Nhưng Phase 3 sẽ dựa vào giả định thứ ba chưa ai kiểm: **cùng một toạ độ chuẩn hoá (0..1) trong khung stream Remote Play và trong hình HDMI thật là cùng một điểm của màn game** (PS5 có *Adjust Display Area*, HUD safe-area, TV zoom; stream Remote Play có thể là khung trước/ sau các chỉnh này — discovery §3.6 ghi `unknown`). Đề xuất thêm 1 việc rẻ ở Bước 6.3 + 7: User chụp **cùng một màn tĩnh** có điểm mốc rõ (ví dụ góc khung nút) bằng điện thoại (F4) và bằng `engine capture` (F1), Dev/Leader so vị trí chuẩn hoá của mốc ở hai ảnh. Không cần code thêm.

Các điểm khác:
1. **Điều kiện stream quyết định số R-D mà doc không yêu cầu ghi**: kích thước cửa sổ Remote Play lúc chụp (laptop 2880×1800 có thể scale >100%; app có thể bị DWM co giãn bitmap ⇒ chữ mờ), và **cài đặt chất lượng/ độ phân giải stream** trong app PS Remote Play (theo hiểu biết của em app có tuỳ chọn; mặc định có thể thấp hơn 1080p — `chưa kiểm`, User cần đọc giá trị thật). Đề xuất: U4/Bước 7 ghi "độ phân giải stream, kích thước cửa sổ, scale Windows" kèm F1; nếu không số OCR không so sánh được giữa các lần.
2. **Khái niệm bị gộp/ đặt tên**: (a) `confident` gộp "có rect hợp lý" với "đúng 16:9" — cửa sổ 4:3 thật vẫn đúng mà `confident=false`; (b) "golden" ở đây là **ground truth chép tay**, không phải golden-master (kết quả ghi lại) — Reviewer có thể hiểu nhầm là sinh từ OCR; ghi rõ "ground-truth" trong README; (c) "chụp không đen" gộp kiểm bằng mắt với kiểm máy (G12); (d) "Phase 1: Chuẩn bị & khử rủi ro" nhưng thực chất dựng hai module sẽ sống tiếp ở Phase 3 — đúng ý doc ("không phải script vứt đi") nhưng nghĩa là code chất lượng module, không phải spike, nên em sẽ viết/ test theo chuẩn module.
3. **Giả định nền chưa kiểm**: (a) khung WGC của cửa sổ = `DWMWA_EXTENDED_FRAME_BOUNDS` (đã gọi là "cùng hệ toạ độ" ở Bước 3; chưa kiểm, bóng đổ/ góc bo Windows 11 có thể lệch vài px — smoke 3 sẽ đo, dung sai ±1–2 px em ghi vào README); (b) ảnh stream Remote Play có viền **đen thuần** để dò (viền có thể là màu khác hoặc có nhiễu); (c) lớp phủ của app bên thứ ba vẫn hiện khi TV ở chế độ Game/ALLM (SA đã nêu — Bước 6.3 ghi chế độ ảnh).
4. **Không thấy giả định nền sai về kiến trúc.** Em không đề xuất lệch hướng đã chốt.

## 6. Tóm tắt cho Leader

- **Chưa phải "đủ, không nghi ngờ"**: cần Leader trả lời nhanh G1, G2, G6, G7, 1.2 #1 (AVD x86), #3 (sửa lệnh `screencap` gửi User), #4 (`playtest/` có gitignore không), #7–#8 (hai thêm debug-only), và chọn **Vortice (MIT)** hay tự khai báo COM D3D11.
- Sau khi có xác nhận, em code Bước 1→5 theo đúng thứ tự, TDD Stub→Red→Green cho 4 hàm thuần, chia turn 2 làm 2a/2b như mục 4.7.
- Em **không** chạm TV, PS5, IP LAN, `adb connect`, PSN, khoá API; **không** commit/checkout/stash/reset.
