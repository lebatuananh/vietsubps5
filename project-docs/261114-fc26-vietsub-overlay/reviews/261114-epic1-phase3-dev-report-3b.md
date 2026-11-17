DEV-3B DONE
# Dev report 3b — Phase 3, `tv-overlay/` Bước 5–6

Phạm vi: chỉ `tv-overlay/` (+ báo cáo này + bằng chứng). `engine/` **không đụng** trong 3b. Làm đúng theo `tv-overlay/README.md` mục "Giao thức" (viết ở 3a) và bảng quyết định đầu `261114-epic1-phase3.md` (D11–D13, D15, B2, N2, N5, N6, N12, P3R2-D2).

Đường dẫn bằng chứng bên dưới tính từ `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/dev/` (tiền tố `3b-`).

## 1. File đã làm

**Mới (`tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/`)**
- `Protocol.kt` — data class `OverlayFrame/OverlayItem/Display/Box/CoverSpec/Hello`, parse hello + khung có giới hạn chặt (≤200 item, text ≤1000, ttl 1..60000, maxLines 1..6, màu `#AARRGGBB`, box 0..1), reply `ok/token/version`, `SeqGate` (theo `sessionId`), `BoundedLineReader` (≤262144 B/dòng, đọc từng byte, vượt ⇒ ném `LineTooLongException`).
- `PairingCode.kt` — 8 ký tự bảng `ACDEFHJKMNPRTUVWXY3479`, hiển thị `XXXX-XXXX`, so khớp không phân biệt hoa/thường và dấu `-`.
- `AutoFit.kt` — `AutoFit` (cỡ `fontPx×H/1080` giảm tới `minFontPx`, xuống dòng trong box rồi room, **ít dòng nhất**, "…" chỉ cho đoạn văn `maxLines>1`, cùng `group` = cỡ nhỏ nhất, nhãn hàng ngang thiếu khe <2% ⇒ dùng `alt` rồi co cả nhóm) và `CoverRegion` (hợp(box, chữ)+lề 6/1080·H, cắt trong `room` và mặt phẳng, luôn phủ bbox gốc; `opaque()` ép alpha 0xFF cho `card`).
- `OverlayModel.kt` — trạng thái hiển thị: fade-in 120 ms, fade-out 80 ms, `holdMs` do TV áp, ttl hết ⇒ xoá, cùng `id`+nội dung ⇒ không vẽ lại.
- `OverlayServer.kt` — `ServerSocket` 47820, thread `accept` nền, một thread/kết nối, hello timeout 5 s, **một kết nối hoạt động, kết nối hợp lệ mới thay cái cũ**, `SeqGate` dùng chung, `disconnectCurrent()`, `stop()`.
- `AppState.kt` — `AppState`, `PairingStore` (mã trong SharedPreferences, `regenerate`), `NetInfo.ip()`.

**Viết lại**
- `OverlayView.kt` (vẽ sản phẩm: map toạ độ, auto-fit, solid viền mờ 2 px / card đục bo 8·u / none có bóng, fade), `OverlayService.kt` (server + model + FGS `specialUse`, `repair()`, không tự tắt sau 30 phút), `MainActivity.kt` (5 màn D13), `res/values/strings.xml` (chuỗi sản phẩm; chuỗi Phase 1 chuyển sang `src/debug/res/values/diag_strings.xml`), `AndroidManifest.xml` (+ `INTERNET` có chú thích N6), `app/build.gradle.kts` (+ Gson 2.11.0, Apache-2.0), `tv-overlay/README.md`.

**Chỉ bản debug (`tv-overlay/app/src/debug/`)** — `BackdropActivity` (D15), `DiagActivity`/`DiagOverlayService`/`DiagOverlayView` (màn Phase 1, mở bằng `--ez diag true`), `AndroidManifest.xml` debug.

**Test mới** (`app/src/test/…`): `ProtocolTest` 19, `PairingCodeTest` 7, `AutoFitTest` 14, `CoverRegionTest` 5, `OverlayModelTest` 10, `OverlayServerTest` 11 (socket loopback thật).

## 2. Test trước / sau

| | Số test JVM | Kết quả |
|---|---|---|
| Trước 3b | **6** (`CoordinateMapperTest` 3 + `FontCoverageTest` 3) | xanh |
| Sau 3b | **72** (6 cũ + 66 mới) | **72 chạy, 0 fail, 0 error** (`3b-06-final-build.txt`: `:app:testDebugUnitTest :app:assembleDebug --rerun-tasks` = BUILD SUCCESSFUL, 0 warning của compiler) |

**ĐỎ rồi XANH** (TDD Stub→Red→Green cho phần logic thuần: parse/từ chối khung và hello, mã ghép, auto-fit, vùng che, thẻ đục):
- ĐỎ: `3b-01-tdd-red.txt` — 71 test, **64 fail** bằng `NotImplementedError` (stub `TODO()`), 7 pass (6 cũ + hằng số bảng chữ cái).
- XANH: `3b-02-tdd-green.txt` (71/71) — `3b-02-tdd-green-try.txt` là lần thử trước khi sửa 2 lỗi thiết kế test (mục 6).
- Vòng bổ sung sau khi nhìn ảnh thật: nhãn "Ban lãnh đạo" rơi 3 dòng ⇒ viết test đỏ `prefers one line in room` (`3b-04-fit-prefer-fewer-lines-red.txt`: 72 test, **1 fail**) → sửa `AutoFit.tryLayout` chọn bố cục ít dòng nhất → XANH `3b-05-fit-green.txt`.
- Quyền (`3b-07-permissions.txt`, `aapt2 dump permissions`): `INTERNET`, `SYSTEM_ALERT_WINDOW`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_SPECIAL_USE`; **không** `POST_NOTIFICATIONS`.

## 3. Smoke trên `emulator-5556` (AVD `vietsub_dev`, google-tv API 31, do tôi khởi động)

Cách chạy: `VIETSUB_HOME` = thư mục tạm (`3b-vietsub-home.txt`), `ANTHROPIC_API_KEY` rỗng, translator `fake`/`cache` (không có HTTP ra ngoài), `adb forward tcp:47820 tcp:47820`, engine `--tv 127.0.0.1`. Ảnh chụp bằng `adb screencap`, dump UI bằng `uiautomator` (file `.xml` cùng tên).

**Bước 4 — ghép cặp + màn hình app (D13)**
- Màn cấp quyền: `3b-10-screen-permission.png` (+`3b-10-permission.xml`: focus ở "Mở cài đặt", nền vàng). Cài/cấp quyền: `3b-10-install-permission.txt`.
- Sau `appops set … SYSTEM_ALERT_WINDOW allow`: app **tự** sang màn chờ, hiện `Địa chỉ TV: 10.0.2.16:47820` + mã `P4RM-4WET`, focus ở "Hướng dẫn cho người cài ▸" (`3b-11-screen-waiting.png`, `3b-11-waiting.xml`).
- `engine pair` mã sai nhưng đúng bảng chữ cái ⇒ exit 3, server log "từ chối hello: token"; mã đúng, viết thường, không dấu `-` ⇒ "OK — TV chấp nhận mã" (`3b-12-pair.txt`).
- Đã nối: `3b-15-screen-connected.png` / `3b-15-connected-right.png` ("● Đã kết nối", "Lớp phủ: đang bật", nút Tắt lớp phủ / Ghép cặp lại). DPAD_DOWN không đổi focus (2 nút nằm ngang — đúng), DPAD_RIGHT chuyển focus sang "Ghép cặp lại", **3 s sau focus vẫn ở đó** (không bị kéo lại) — `3b-15-connected-after-down.xml`, `…-right.xml`, `…-right-later.xml`.
- "Ghép cặp lại" (OK): mã đổi `P4RM-4WET` → `XPUK-7FVR`, engine đang nối bị ngắt, màn chờ (`3b-15-after-repair.png`); hello với mã cũ ⇒ `{"ok":false,"reason":"token"}`.
- LOST: ghép mã mới, chạy engine 4 s, engine thoát ⇒ sau ~1.5 s màn "Máy tính không còn gửi chữ" + 3 gợi ý, focus ở "Ghép cặp lại" (`3b-15-lost.png`); sau ~8 s tự về màn chờ (`3b-15-after-lost.png`).

**Bước 5 — `replay --send` (fake và cache) lên phông `synth-inbox-list.png`** (`BackdropActivity`)
- `3b-13-fake-1-overlay.png`, `3b-13-fake-2-overlay-later.png`, `3b-13-cache-1/2-*.png`; log `3b-13-replay-*.txt`. Chữ Việt vẽ đè, chữ Anh của các item được gửi bị che, chữ còn nguyên ở cả ảnh chụp lúc ~7 s (`…-1-overlay.png`) và ~10 s (`…-2-overlay-later.png`) sau khi engine khởi động; sau khi engine thoát, lớp chữ **tự xoá ≤ ttl** và phông gốc lộ lại (`3b-13-fake-3-after-engine-closed.png`, `3b-13-cache-3-…`).
- **C1 kiểm bằng mắt**: đạt, trừ **một vệt rất nhỏ** của chữ gốc quanh (1110, 545) trong `3b-13-fake-2-overlay-later.png` — xem "Chưa chứng minh được" mục (c). Các ngày "12 Nov", "10 Nov"… vẫn còn tiếng Anh vì `fake` translator không phát item cho chúng (không phải lỗi TV).

**Bước 7 — `pattern --send --seconds 7` lên nền đen `synth-dark.png`**: `3b-14-pattern-corners.png` — 4 ô TL/TR/BL/BR **sát đúng 4 góc** của mặt phẳng 1920×1080, các mốc 10–90% đúng chỗ dọc cạnh trên và cạnh trái. (Việc trùng 4 góc hình HDMI PS5 là việc của TV thật.)

**Bước 8 — mở app khi lớp phủ đang bật + phím remote thật (`adb shell input keyevent`)**
- Lớp phủ đang bật, quay về `MainActivity` (BACK từ `BackdropActivity`) ⇒ màn OFF "Đã tắt lớp phủ … OK để bật lại.", focus **"Bật lại lớp phủ"** (`3b-15-screen-off.png`, `3b-15-off.xml`).
- `KEYCODE_DPAD_CENTER` ⇒ lớp phủ bật lại, sang màn chờ ghép (`3b-15-after-enable.png`).
- `--ez diag true` (debug) mở `DiagActivity` (`3b-15-diag.png`, 0 dòng crash).

**Bước 9 — đầu vào thù địch qua socket thô** (`3b-14-pattern-hostile.txt`): hello `v=2` ⇒ `{"ok":false,"reason":"version"}`; sai mã ⇒ `reason:"token"`; dòng đầu rác (`GET / HTTP/1.1`) ⇒ TV đóng, không trả lời; hello đúng + dòng 300 KB ⇒ TV đóng ("dòng dài hơn 262144 byte"); hello đúng ngay sau đó **vẫn được nhận** (server sống); `logcat -b crash` = 0 dòng; service còn chạy.

**Dọn dẹp**: `adb -s emulator-5556 emu kill`, `adb forward --remove tcp:47820`, `gradlew --stop`. Sau đó `adb devices` **rỗng**, không còn tiến trình `emulator`/`qemu` nào (kiểm sau ~25 s, vì emulator tự để một helper `-kill` sống 20 s). Tôi không khởi động ffplay hay listener nào trong 3b.

## 4. Chưa chứng minh được

a. **TV thật (Sony Google TV)**: mọi thứ ở trên chỉ chạy trên emulator API 31 (mật độ 2.0, 1920×1080). Chưa kiểm: font/độ đọc được ở khoảng cách sofa, độ trễ vẽ, `TYPE_APPLICATION_OVERLAY` + `specialUse` trên TV thật, "mở app ⇒ tắt lớp phủ" bằng phím Home/Back **của remote Sony**, và việc 4 góc trùng hình HDMI PS5.
b. **Chạy cùng PS Remote Play thật** (cửa sổ thật, WGC có khung) — 3b chỉ dùng ảnh tĩnh làm phông và `replay`/`pattern`; không có LLM thật (translator fake/cache, 0 HTTP).
c. **Vệt chữ gốc nhỏ (C1) — nguyên nhân phía engine, chưa sửa**: ở `replay` fake còn sót một vệt nhỏ chữ gốc quanh (1110, 545). Đo: `room` do `RoomFinder` (engine) ôm sát bbox OCR, bbox OCR ngắn hơn chữ thật ~2 px, mà TV cắt vùng che trong `room` theo N5 ⇒ 2 px chữ gốc không bị che. TV làm đúng spec; cần xử lý ở engine (nới `room` hoặc bbox) — đề xuất cho vòng sau, **3b không đụng `engine/`**.
d. **Bản release chưa build** (`assembleRelease` không chạy; `MainActivity` gọi `DiagActivity` bằng tên lớp, chỉ khi `BuildConfig.DEBUG`) — mới chắc cho debug.
e. **Mật độ/độ phân giải khác**: chưa thử TV 4K hoặc `H ≠ 1080` ngoài emulator.
f. **Mã ghép cặp qua gỡ-cài-lại, TV ngủ/đổi IP, hai engine cùng lúc** — chưa chạy; chỉ có test JVM cho "kết nối hợp lệ mới thay cũ".
g. Chưa đo bằng số thời gian "ttl ⇒ xoá" (chỉ thấy bằng ảnh sau khi engine thoát) và chưa đo độ mượt fade 80/120 ms (chỉ có test model).

## 5. Lệch so với doc

1. **Bước pin (miễn tối ưu pin) không chặn**: app không bắt người dùng đi qua bước này (trên Google TV nút là stub rỗng); chỉ còn trong phần hướng dẫn gập dành cho người cài. Doc D13 liệt kê nó như một bước.
2. **Tự chuyển màn thay vì nút "Tiếp tục"**: có quyền xong, app tự vào màn chờ (ticker 500 ms) — bớt một lần bấm remote.
3. **Màn OFF**: focus mặc định ở "Bật lại lớp phủ" (mockup đặt "Về màn hình chính"). Lý do: câu chữ trên màn là "OK để bật lại". Dễ đổi nếu Leader muốn theo mockup.
4. **Mã ghép cặp**: mockup vẽ 6 chữ số; app dùng **8 ký tự** `XXXX-XXXX` theo D3 ràng buộc.
5. **Thêm phụ thuộc Gson 2.11.0** (Apache-2.0) cho JSON; doc chỉ nói "JSON". Dùng API cây, không reflection.
6. **`app_name`** đổi thành "VietSub" (trước: "VietSub — lớp phủ kiểm tra").
7. **Backdrop debug lấy ảnh từ `filesDir/<tên file>`** (đưa vào bằng `adb push` → `run-as cp`) thay vì `/sdcard/Download` — emulator chặn đọc đường dẫn kia; vẫn nhận `--es backdrop <đường dẫn>`.
8. **Sửa `AutoFit` sau khi nhìn ảnh**: chọn bố cục ít dòng nhất giữa bề rộng box và room (không có trong doc; test đỏ→xanh `3b-04/05`).
9. **Hai lỗi thiết kế test tự sửa, không nới kỳ vọng**: `new_item_fades_in_over_120ms` đo alpha ở 4999 ms sau khi ttl hết (đổi 4000 ms); `row_label_lacking_gap…` đặt box rộng 80 làm cỡ rơi thẳng xuống min nên không tạo "thiếu khe" (đổi box rộng 200).
10. **Nút có focus bị cắt cạnh 1–2 px** do phóng 1.05 trong cột cha (thấy trong `3b-15-screen-off.png` và `…connected-right.png`) — không ảnh hưởng thao tác; chưa sửa.
11. `engine/` không đụng (đúng phạm vi), nhưng vì thế vệt C1 ở mục 4c chưa có người xử lý.

## 6. Câu hỏi mở — "Trong app TV, người chỉ cầm remote sẽ kẹt ở đâu?"

1. **Bước cấp quyền overlay trên Google TV**: màn hệ thống là danh sách chung, phải cuộn tìm "VietSub" và bật — bằng D-pad dễ lướt qua / không biết đã bật hay chưa. App chỉ nhắc chữ và hiện lệnh `adb`, mà người chỉ có remote không chạy được lệnh đó. Nếu màn đó không có/không bật được, người dùng **kẹt hẳn** ở màn PERMISSION (chưa thử trên TV thật).
2. **"Tắt lớp phủ" bằng cách mở app**: người chơi phải thoát game (Home) → tìm lại icon VietSub trong danh sách app → mở → (lớp phủ tắt). Không có phím tắt trong game; nếu icon bị đẩy khỏi hàng đầu launcher thì khó tìm. Ngược lại, **vô tình mở app sẽ tắt chữ** mà không có báo trước (chỉ báo sau bằng màn OFF).
3. **Mã ghép cặp 8 ký tự phải gõ trên PC**: nhìn mã trên TV rồi sang PC gõ — ổn, nhưng nếu mã đổi (ai đó bấm "Tạo mã mới"/"Ghép cặp lại") thì PC báo `token` mà TV chỉ hiện màn chờ bình thường, **người dùng không thấy thông báo "mã cũ bị từ chối"** trên TV.
4. **Màn LOST chỉ hiện 6 s** rồi tự về màn chờ — nếu người dùng không nhìn TV đúng lúc thì không biết "vì sao chữ biến mất"; và màn chờ trông như chưa từng ghép.
5. **"Hướng dẫn cho người cài ▸"** gập mặc định; focus mặc định rơi vào đó ở màn chờ. Người mới có thể bấm OK nhầm (mở rộng ra, nội dung dài, phải cuộn) thay vì thấy ngay mã.
6. **Nút "Về màn hình chính"** chỉ có ở màn OFF; ở các màn khác phải dùng Back/Home của remote — bình thường, nhưng nếu remote không có phím Home riêng (một số remote) thì chỉ còn Back, mà Back ở màn gốc có thoát app hay không chưa kiểm trên TV thật.
7. **Không có cách tự khôi phục nếu IP TV đổi** (DHCP): màn chờ hiện IP hiện tại, nhưng engine PC giữ IP cũ trong `engine.local.json` ⇒ phải chạy lại `engine pair` trên PC.

## 7. `git status --short` (cuối 3b)

62 dòng; các file thuộc 3b:

```
 M tv-overlay/README.md
 M tv-overlay/app/build.gradle.kts
 M tv-overlay/app/src/main/AndroidManifest.xml
 M tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/MainActivity.kt
 M tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/OverlayService.kt
 M tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/OverlayView.kt
 M tv-overlay/app/src/main/res/values/strings.xml
?? tv-overlay/app/src/debug/
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/AppState.kt
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/AutoFit.kt
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/OverlayModel.kt
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/OverlayServer.kt
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/PairingCode.kt
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/Protocol.kt
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/AutoFitTest.kt
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/OverlayModelTest.kt
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/OverlayServerTest.kt
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/PairingCodeTest.kt
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/ProtocolTest.kt
?? project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/        (bằng chứng 3a + 3b)
?? project-docs/261114-fc26-vietsub-overlay/reviews/261114-epic1-phase3-dev-report-3b.md
```

Các dòng còn lại trong 62 là từ vòng 3a/fix3a/review/SA và lead (không phải 3b): `engine/` (M: README, WindowCapturer.cs, Engine.csproj, OcrRunner.cs, Program.cs; ??: data/, Commands3.cs, Config/, Layout/, Pipeline/, Protocol/, Translation/, 6 file test), `project-docs/…` (M: `261114-epic1.md`, `261114-workflow-log.md`; ??: `261114-epic1-phase3.md`, `_workers/briefs/*`, `_workers/runs/*.ps1`, `reviews/…phase3-dev-feedback.md`, `…dev-report-3a.md`, `…review-3a.md`, `…sa-review.md`). Không có lệnh git ghi nào được chạy trong 3b.
