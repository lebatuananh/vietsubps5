# tv-overlay/ — app Google TV vẽ lớp phủ

Chỉ ghi thứ **đọc code rồi chạy thẳng vẫn dễ vấp**. Phase 3 (3b): app **sản phẩm** — `OverlayServer` (TCP 47820) nhận khung từ engine, vẽ chữ Việt đè lên hình game (auto-fit + vùng che, theo mục "Giao thức" bên dưới), `MainActivity` theo mockup `mockups/261114-mockup-tv-app.html` (D13). Màn kiểm tra của Phase 1 (4 dấu góc + chữ mẫu) **chỉ còn ở bản debug** (mục "Bản debug").

## Build / test / chạy

Biến môi trường **chỉ đặt trong tiến trình shell**, không sửa biến máy:

```powershell
$env:JAVA_HOME='C:\Program Files\Android\Android Studio\jbr'      # JBR = OpenJDK 25.0.4
$env:ANDROID_HOME="$env:LOCALAPPDATA\Android\Sdk"
cd tv-overlay
.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug           # APK: app/build/outputs/apk/debug/app-debug.apk
.\gradlew.bat --stop                                              # tắt daemon khi xong
```

Lần build đầu tải Gradle 9.1.0 + AGP + androidx (hàng trăm MB, ~1–3 phút trở lên) — chạy nền, ghi log ra file. `local.properties` (`sdk.dir`) không cần khi có `ANDROID_HOME`; nếu tạo thì đã gitignore.

## Toolchain (chọn theo ma trận chính thức, JBR 25)

| | Phiên bản | Căn cứ |
|---|---|---|
| Gradle | **9.1.0** (wrapper ghim `distributionSha256Sum` = SHA-256 công bố `a17ddd85…c806`) | Chạy Gradle trên Java 25 từ **9.1.0** — https://docs.gradle.org/current/userguide/compatibility.html · AGP 9.0 đòi tối thiểu Gradle 9.1.0 — https://developer.android.com/build/releases/about-agp |
| AGP | **9.0.1** (JDK ≥ 17, API tối đa 36.1) | https://developer.android.com/build/releases/agp-9-0-0-release-notes |
| Kotlin | **built-in Kotlin của AGP 9** (KGP 2.2.10 là phụ thuộc runtime của AGP; **không** apply `org.jetbrains.kotlin.android`) | cùng trang release notes; Kotlin 2.2.10 biên dịch được trên JDK 25 (đã chạy) |
| `compileSdk`/`targetSdk`/`minSdk` | 35 / 35 / 29 (đổi `minSdk` khi U3 có model TV) | |
| Java/Kotlin target | 17 (chạy Gradle trên JDK 25, bytecode 17) | |

Wrapper sinh từ bản phân phối **chính thức tải ngoài repo**, đối chiếu SHA-256 công bố tại `…/gradle-9.1.0-bin.zip.sha256` (khớp). Commit `gradlew`, `gradlew.bat`, `gradle/wrapper/*`; **không** commit bản phân phối.

## Thư viện và giấy phép

| Thư viện | Dùng cho | Giấy phép |
|---|---|---|
| Android Gradle Plugin 9.0.1 (+ KGP 2.2.10 runtime) | build | Apache-2.0 |
| `androidx.core:core-ktx` 1.16.0 | `ServiceCompat.startForeground` (minSdk 29 < API 34) | Apache-2.0 |
| `androidx.window:window` 1.4.0 | `WindowMetricsCalculator` (`WindowMetrics` chỉ từ API 30) | Apache-2.0 |
| `com.google.code.gson:gson` 2.11.0 | đọc/ghi JSON giao thức (API cây `JsonObject`, không reflection) | Apache-2.0 |
| `junit:junit` 4.13.2 | unit test (JVM) | EPL-1.0 |
| Be Vietnam Pro Regular + SemiBold | font nhúng (`res/font/be_vietnam_pro_*.ttf`), nguồn https://github.com/google/fonts/tree/main/ofl/bevietnampro | SIL OFL 1.1 — `OFL-BeVietnamPro.txt` |

Không Compose/AppCompat/Leanback/OkHttp. Manifest **có** `INTERNET` (N6) — **bắt buộc** để `ServerSocket` nhận kết nối từ engine trên LAN; app **không** gọi ra ngoài (chỉ `accept` cổng 47820 trong mạng nhà, không có client HTTP, không có thư viện mạng). **Không** `POST_NOTIFICATIONS` (kiểm bằng `aapt2 dump permissions`).

## Giao thức (engine ↔ TV) — NGUỒN CHÂN LÝ, v1

Engine (PC, client) → TV (server). Phase 3. `engine/README.md` trỏ về đây. **Không** tự chế thêm trường ngoài bảng này; thêm trường = đổi `v`.

**Kênh**: TCP, cổng cố định **47820**, mỗi gói là **một dòng JSON UTF-8** kết thúc `\n` (LF). Dòng dài tối đa **262144 byte** (256 KB, không tính `\n`); dài hơn ⇒ TV đóng kết nối. Tên trường **camelCase**. Trường lạ ⇒ bỏ qua. Trường tuỳ chọn: vắng mặt **hoặc** `null` đều nghĩa là "không có". Một kết nối tại một thời điểm: kết nối mới **hợp lệ** (hello đúng) thay kết nối cũ (đóng cái cũ).

### 1. Hello (dòng đầu tiên của engine)

```json
{"hello":{"v":1,"token":"ACDEFHJK"}}
```
- `v`: int, phiên bản giao thức, hiện **1**. `token`: 8 ký tự `A–Z/3479` (mã ghép cặp TV hiện dạng `XXXX-XXXX`); TV so **không phân biệt hoa/thường, bỏ `-`**.
- Bảng chữ cái mã (không nhập nhằng): `ACDEFHJKMNPRTUVWXY3479` (22 ký tự).

### 2. Trả lời của TV (một dòng)

```json
{"ok":true}
{"ok":false,"reason":"token"}
{"ok":false,"reason":"version"}
```
`reason` ∈ `"token"` (sai mã) \| `"version"` (`v` ≠ 1). Sau `ok:false` TV **đóng** kết nối. Dòng đầu không phải JSON hợp lệ / không có `hello` ⇒ TV đóng, không trả lời. Sau `ok:true` mọi dòng tiếp theo là `OverlayFrame`.

### 3. `OverlayFrame` (engine → TV)

```json
{"v":1,"sessionId":"3f2c…","seq":42,"ttlMs":3000,
 "display":{"minFontPx":24,"holdMs":0},
 "items":[ { …OverlayItem… } ]}
```

| Trường | Kiểu | Bắt buộc | Nghĩa |
|---|---|---|---|
| `v` | int | có | luôn 1; **≠ 1 ⇒ TV bỏ gói** |
| `sessionId` | string | có | GUID mới mỗi lần `engine run`/`replay` khởi động |
| `seq` | long | có | tăng dần từ 1 trong một `sessionId` (kể cả gói nhịp tim). **`seq` ≤ seq đã xử lý cùng `sessionId` ⇒ bỏ**; `sessionId` khác ⇒ coi như phiên mới, đặt lại bộ đếm |
| `ttlMs` | int | có | hết hạn sau từng ấy ms kể từ lúc nhận mà không có gói mới ⇒ TV **xoá** lớp phủ (lõi chết thì chữ không đứng mãi). Engine gửi 3000 |
| `display` | object | không | `{ "minFontPx": int, "holdMs": int }` — cấu hình hiển thị (X6, TV không có màn cài đặt). **Không có `fontScale`**: engine đã nhân vào `fontPx`, TV dùng `fontPx` nguyên |
| `items` | array | có | `[]` ⇒ xoá |

`display.minFontPx`: cỡ chữ nhỏ nhất (px trên màn cao **1080**) mà auto-fit được giảm tới; mặc định engine 24. **Engine không kẹp `fontPx ≥ minFontPx`; TV bắt đầu auto-fit từ `max(fontPx, minFontPx) × H/1080`** (B4 review 3a) — nên `fontPx` 18 với `minFontPx` 24 vẽ ở 24, không bao giờ nhỏ hơn `minFontPx`. `fontPx` = cao hộp OCR × `fontScale` (mặc định **1,0**). `display.holdMs`: **TV** áp — nhận `items:[]` (hoặc một item biến mất) ⇒ giữ lớp cũ `holdMs` ms rồi fade-out 80 ms; engine **luôn gửi ngay**, mặc định 0.

### 4. `OverlayItem`

| Trường | Kiểu | Bắt buộc | Nghĩa |
|---|---|---|---|
| `id` | string | có | **duy nhất trong một gói** (B1 review 3a) và ổn định giữa các khung = băm(ô lưới 16×9 chứa góc trên-trái bbox + câu gốc chuẩn hoá + **số thứ tự lần xuất hiện** của cặp (ô, câu) trong gói, xếp theo y rồi x) — hai hàng cùng chữ cùng ô vẫn ra hai `id` khác nhau. TV **không** được giả định gì khác; nếu vẫn gặp `id` trùng (engine lỗi) TV **giữ item đầu, bỏ item sau và log** (Rev 1.4). Cùng `id` và **nội dung y hệt** ⇒ TV **không** vẽ lại/ fade lại; `id` mới ⇒ fade-in 120 ms; `id` biến mất ⇒ fade-out 80 ms |
| `box` | `{x,y,w,h}` | có | float 0..1 theo **vùng hình game** (contentRect), gốc trên-trái. TV map qua `CoordinateMapper` lên cả mặt phẳng đồ hoạ |
| `text` | string | có | tiếng Việt, ≤ 1000 ký tự, có thể chứa `\n` (đoạn văn nhiều dòng engine không chèn `\n`; TV tự xuống dòng) |
| `alt` | string \| null | không | bản **ngắn** (`short_vi`). **TV chọn** dùng hay không (N12): chỉ khi nhãn hàng ngang thiếu khe (< 2% bề rộng tới nhãn kề cùng hàng) mà vẫn chưa vừa; còn thiếu ⇒ co **cả nhóm** |
| `fontPx` | int | có | cỡ chữ **mong muốn** (px trên khung cao 1080, đã nhân `fontScale`). TV quy theo chiều cao màn thật (`fontPx × H/1080`) |
| `group` | string | có | các item **cùng `group` vẽ cùng cỡ** = min sau auto-fit (X4/C2). Dạng `col-<n>` (cột danh sách) hoặc `row-<n>` (nhãn hàng ngang) hoặc `solo-<id>`; chỉ dùng làm khoá so sánh |
| `maxLines` | int | có | 1–6. **Nhãn một dòng (không phải đoạn văn) = 1**; đoạn văn = số dòng gốc + 2. TV coi **số dòng gốc = `maxLines` − 2** (tối thiểu 1) cho thứ tự auto-fit bên dưới |
| `align` | string | có | `"start"` \| `"center"` \| `"end"` |
| `bold` | bool | có | SemiBold khi `true`, Regular khi `false` |
| `textColor` | string | có | **`#AARRGGBB`** (hex 8 chữ số, hoa/thường đều được), ví dụ `#FFFFFFFF` |
| `cover` | object | có | `{ "mode": "solid"\|"card"\|"none", "color": "#AARRGGBB" }`. `solid`: tô `color` viền mờ 2 px; `card`: **nền đục 100%** bo góc 8 px (C1/N2 — không 80%); `none`: chỉ chữ có bóng |
| `room` | `{x,y,w,h}` \| null | không | hộp lớn nhất quanh bbox mà nền còn đồng nhất với màu che (N5), cùng hệ toạ độ `box`. TV chỉ nới/xuống dòng **trong `room`**, vùng che **không ra ngoài `room`**; vắng ⇒ TV dùng luật trong `box` |

**Auto-fit trên TV (Rev 1.4, D11)** — thứ tự cho mỗi item: (1) **co cỡ** từ `max(fontPx, minFontPx)×H/1080` tới `minFontPx` với số dòng gốc (bề rộng box rồi `room`); (2) `alt` (chỉ nhãn hàng ngang thiếu khe < 2% bề rộng; còn thiếu ⇒ co **cả nhóm**; nhãn không vừa kể cả ở sàn mà có `alt` vừa cũng dùng `alt`); (3) **thêm dòng** (tới `maxLines`) CHỈ khi tổng chiều cao còn nằm trong `room` (không có `room` ⇒ trong box); (4) co tiếp tới **sàn 0,85 × `minFontPx`**; (5) cuối cùng mới cắt "…" (nhãn một dòng cũng vậy — phương án cuối, TV log). Nhiều dòng luôn nằm gọn trong `room` theo chiều dọc.

**Vùng che (C1)** = hợp(bbox gốc `box`, bbox chữ Việt sau auto-fit) + lề ≥ 6/1080 chiều cao màn, cắt trong `room` nếu có.

**Giới hạn** (engine tự giữ: giữ 200 item đầu trên-xuống, cắt `text` về 1000 ký tự; `display.coverMode` ngoài auto/solid/card/none bị từ chối khi nạp cấu hình): ≤ 200 item/gói; `text` ≤ 1000 ký tự; dòng ≤ 256 KB; `ttlMs` 1–60000. Vi phạm giới hạn ⇒ TV bỏ gói (không đóng kết nối, trừ dòng > 256 KB).

### 5. Nhịp và vòng đời

- Engine gửi ngay khi đổi trạng thái (mỗi gói là **trạng thái đầy đủ**: ô đổi ⇒ gói **bỏ các item giao ô đổi** — có thể còn các item ở ô khác, có thể rỗng; có bản dịch ⇒ gói đủ) và **gửi lại gói gần nhất (seq mới) mỗi 1 s** làm nhịp tim khi màn đứng yên (TV không xoá vì ttl).
- Mất kết nối: TV xoá lớp phủ sau `ttlMs` (không cần gói xoá); engine tự kết nối lại mỗi 2 s, gửi lại hello.
- Ctrl+C: engine gửi `items:[]` rồi thoát.

### 6. Ví dụ

```json
{"v":1,"sessionId":"b6c1c9f0-0b1d-4a52-9c2e-7a8f5a1d0e11","seq":7,"ttlMs":3000,
 "display":{"minFontPx":24,"holdMs":0},
 "items":[
  {"id":"4e1a9b27c0d3","box":{"x":0.052,"y":0.185,"w":0.048,"h":0.024},"text":"Ban lãnh đạo","alt":null,
   "fontPx":22,"group":"col-1","maxLines":1,"align":"start","bold":false,
   "textColor":"#FFFFFFFF","cover":{"mode":"solid","color":"#FF14202C"},"room":{"x":0.04,"y":0.18,"w":0.2,"h":0.04}}
 ]}
```

## Cấp quyền (trên TV — do User chạy)

- Cài/cấp quyền qua nút trong app. Nếu TV không có màn tương ứng, app hiện lệnh dự phòng ngay trên màn:
  - overlay: `adb shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`
  - miễn pin: `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (đã chạy được trên emulator API 31; **chưa kiểm trên TV thật**)
- Tên app trong danh sách "hiển thị trên ứng dụng khác" của Google TV nay là **"VietSub"** (trước là "VietSub — lớp phủ kiểm tra").
- Cấp quyền xong, app **tự chuyển** sang màn chờ ghép (không cần bấm "Tiếp tục"); `ensureService()` chạy ở mỗi nhịp làm mới 500 ms nên cấp bằng `appops` từ ngoài cũng khởi động được service.

## Màn hình app (D13)

`MainActivity` dựng bằng code (không XML layout), co theo `H/1080`, font Be Vietnam Pro; **một trong 5 màn**, tự đổi theo trạng thái (`AppState`):

| Màn | Khi nào | Nút (focus mặc định đứng đầu) |
|---|---|---|
| PERMISSION | chưa có quyền overlay | Mở cài đặt (+ lệnh `appops` dự phòng) |
| WAITING | có quyền, chưa có engine | hiện `IP:47820` + mã `XXXX-XXXX`; "Hướng dẫn cho người cài ▸" (gập), "Tạo mã mới" |
| CONNECTED | engine đang nối | Tắt lớp phủ · Ghép cặp lại |
| LOST | engine từng nối rồi mất (hiện 6 s `LOST_SHOW_MS`, sau đó về WAITING) | Ghép cặp lại |
| OFF | lớp phủ đang tắt | Bật lại lớp phủ · Về màn hình chính |

- **Mở app khi lớp phủ đang bật ⇒ tắt lớp phủ** (mục đích: người chơi Home → VietSub để tắt chữ); màn OFF ghi "OK để bật lại". Xử lý ở `onCreate`/`onNewIntent`/`onRestart` (`openedByUser()`).
- **Focus chỉ đặt khi màn đổi** (không bị kéo lại mỗi nhịp 500 ms); focus luôn nhìn thấy (vàng `#FFD400`, chữ đen, phóng 1.05 — phóng làm cạnh nút có thể bị cắt 1–2 px bởi cột cha).
- "Ghép cặp lại"/"Tạo mã mới" = `PairingStore.regenerate()` + `OverlayService.repair()`: mã mới, ngắt engine đang nối, **mã cũ bị `reason:"token"`**.
- Mã ghép cặp lưu trong `SharedPreferences` (mất khi gỡ app); 8 ký tự bảng `ACDEFHJKMNPRTUVWXY3479` (mockup vẽ 6 chữ số — **sai lệch có chủ ý**, theo D3 ràng buộc).
- Dịch vụ `OverlayService` **không tự tắt sau 30 phút** nữa (đó là hành vi màn kiểm tra Phase 1); chỉ tắt khi người dùng tắt, mở app, hoặc service bị hệ thống giết (`START_NOT_STICKY`).

## Bản debug (chỉ `src/debug`, không vào release)

- `--ez diag true` mở `DiagActivity` = màn kiểm tra Phase 1 (4 dấu góc, `overlay_on`/`auto_off_ms`…): `adb shell am start -n vn.vietsub.ps5.tv/.MainActivity --ez diag true`. `MainActivity` gọi nó **bằng tên lớp** và chỉ khi `BuildConfig.DEBUG`.
- `BackdropActivity`: phông nền để chạy `engine replay --send` mà không cần PS5/Remote Play: `adb shell am start -n vn.vietsub.ps5.tv/.BackdropActivity --es backdrop /sdcard/Download/<ảnh>.png`. Nếu app không đọc được đường dẫn đó (emulator hay chặn scoped storage) thì tự thử `filesDir/<tên file>`; đưa ảnh vào đó bằng `adb push … /data/local/tmp/x.png` rồi `adb shell run-as vn.vietsub.ps5.tv cp /data/local/tmp/x.png files/x.png`. Nó nằm **chung task** với `MainActivity`: muốn quay về `MainActivity` bấm BACK (`am start` MainActivity khi Backdrop đang trên cùng không đưa nó lên).
- Nhận lệnh smoke: `adb forward tcp:47820 tcp:47820` rồi `engine pair --tv 127.0.0.1 --code <mã>`; `engine replay --send` / `engine pattern --send` vẽ lên TV.

## Test

`.\gradlew.bat :app:testDebugUnitTest` — **90 test** JVM (trước 3b: 6; fix3a +3; fix3b +15): `ProtocolTest`, `PairingCodeTest`, `AutoFitTest`, `CoverRegionTest`, `OverlayModelTest` (có test `id` trùng không làm mất item), `OverlayServerTest` (socket loopback thật), `CoordinateMapperTest`, `FontCoverageTest`. Logic thuần (parse/từ chối khung & hello, mã ghép, auto-fit, vùng che, thẻ đục) viết TDD Stub→Red→Green; bằng chứng đỏ/xanh ở `playtest/epic1-phase3/dev/3b-01…05`.

## Bẫy

- **Chữ ký debug**: APK debug ký bằng debug keystore của **máy build**; build ở máy khác thì `adb install -r` lên TV lỗi chữ ký (phải gỡ app cũ trước). Không dùng keystore nào khác.
- **Nút "Bỏ tối ưu pin" trên Google TV là stub rỗng**: intent `ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` phân giải sang `com.android.tv.settings/.EmptyStubActivity` (đóng ngay, **không** ném `ActivityNotFoundException`). App xử lý: (1) nếu `resolveActivity` ra `EmptyStub*` ⇒ coi như không có màn, hiện ngay lệnh dự phòng `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`; (2) với mọi nút: ghi nhớ nút vừa bấm, ở `onResume` nếu việc **vẫn chưa xong** ⇒ hiện lệnh dự phòng (pin: lệnh `dumpsys deviceidle`; overlay: lệnh `appops`). `<queries>` trong manifest để `resolveActivity` thấy Settings (package visibility API 30+).
- **Màn cấp quyền overlay trên Google TV là danh sách chung** (không mở trang của app): cuộn tìm dòng **"VietSub"** rồi bật; app hiện lời nhắc này ngay khi bấm nút.
- **Focus D-pad nhìn thấy**: nút có focus = nền vàng `#FFD400` + chữ đen; không focus = nền xám đậm `#3A4654` + chữ trắng (Button mặc định của theme không có trạng thái focus trên TV). Màn bọc `ScrollView` (hint dự phòng làm nội dung cao hơn màn; không bọc thì nút bị ép mất chữ).
- **`launchMode="singleTop"` bắt buộc cho extra debug**: với `standard`, `am start` lên app đang chạy chỉ đưa task lên trước, **không** gọi `onNewIntent` ⇒ extra (`diag`, `overlay_on`) bị bỏ qua (đã gặp).
- **Server**: `OverlayServer` chạy thread nền riêng (`accept`) + một thread/kết nối; hello phải **hoàn tất trong 5 s TỔNG** (đóng cưỡng bức bằng watchdog, kể cả nhỏ giọt từng byte); **tối đa 4 kết nối chưa xác thực**; **chỉ nhận nguồn LAN riêng** (`LanFilter`: RFC 1918, link-local, loopback, IPv6 ULA/link-local — nguồn khác bị đóng không trả lời); kết nối **nửa mở** (không nhận gói nào trong 2 × `ttlMs`) bị đóng ⇒ app về "Mất kết nối"; khung vào được chuyển sang main thread rồi vào `OverlayModel`. Dòng > 256 KB đọc bằng `BoundedLineReader` (đọc từng byte, ném lỗi khi vượt) nên **không** nuốt RAM; kết nối hỏng chỉ đóng kết nối đó, service vẫn sống (đã thử: v=2, sai mã, dòng rác, 300 KB — `3b-14-pattern-hostile.txt`).
- **Cổng 47820 trên emulator**: dùng `adb forward`; mạng emulator (10.0.2.x) không cho PC nối thẳng tới IP hiện trên màn.
- **`Paint.hasGlyph` không chứng minh font đủ dấu**: nó tính cả font hệ thống (đối chứng CJK trả `true`). Kiểm đủ dải U+1EA0–1EF9 bằng `FontCoverageTest` (đọc bảng `cmap` của file TTF).
- `OverlayService.addOverlay()` bắt `BadTokenException`/`SecurityException` (quyền bị thu hồi giữa `canDrawOverlays` và `addView`) ⇒ log, vẫn `startForeground` rồi tự dừng (không crash).
- **Foreground service `specialUse`** cần đủ 4 thứ: `foregroundServiceType` trên `<service>`, 2 quyền `FOREGROUND_SERVICE[_SPECIAL_USE]`, `<property …PROPERTY_SPECIAL_USE_FGS_SUBTYPE>`, và `ServiceCompat.startForeground(..., FOREGROUND_SERVICE_TYPE_SPECIAL_USE)`. Chạy được trên emulator Android 12 (API 31); TV thật chưa kiểm.
- **AGP tự cài SDK Build-Tools 36.0.0** vào SDK của máy (AGP 9.0 đòi tối thiểu) khi build lần đầu — cần mạng + licence đã chấp nhận.
- `uiautomator dump` lỗi `could not get idle state` nếu UI liên tục bị làm mới; vì vậy bộ làm mới nhãn "đang bật/tắt" chỉ `setText` khi **đổi**.
- Bẫy `sdkmanager`/`avdmanager` từ PowerShell: gói có `;` phải qua `cmd /c` + dấu ngoặc kép.
- **Lớp phủ ẩn khi `MainActivity` ở tiền cảnh** (chữ engine không đè giao diện app); màn OFF focus mặc định "Về màn hình chính"; "Tạo mã mới"/"Ghép cặp lại" sinh mã **một** lần và màn Chờ hiện "Mã đã đổi — trên máy tính chạy lại engine pair" tới khi có engine nối lại.
- `OverlayService` có `START_NOT_STICKY`; service bị giết ⇒ cửa sổ cũng mất, mở lại app thấy màn chờ (mã ghép giữ nguyên).
- **Vùng che bị cắt trong `room`** (N5) nên nếu engine đo `room` sát bbox OCR mà OCR bbox hụt vài px thì còn sót 1 vệt chữ gốc rất nhỏ (thấy ở emulator, `3b-13-fake-2-overlay-later.png` quanh x=1110,y=545) — nguyên nhân phía engine (`RoomFinder`), không sửa ở 3b.
- Chữ nhãn cột hẹp có thể xuống 2 dòng ("Ban lãnh / đạo"): auto-fit chọn bố cục **ít dòng nhất** giữa bề rộng box và bề rộng room (đã sửa sau lần chạy đầu: trước đó rơi 3 dòng).

## Kích thước lớp phủ

"4 góc" là 4 góc **mặt phẳng đồ hoạ Android** (emulator: 1920×1080 tại mật độ 2.0; `3b-14-pattern-corners.png` cho thấy 4 ô TL/TR/BL/BR sát 4 góc), việc nó trùng 4 góc **hình HDMI PS5** là điều kiểm trên TV thật.
