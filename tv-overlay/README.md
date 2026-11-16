# tv-overlay/ — app Google TV vẽ lớp phủ

Chỉ ghi thứ **đọc code rồi chạy thẳng vẫn dễ vấp**. Phase 1: APK **kiểm tra** (4 dấu góc + chữ Việt mẫu), không phải UI sản phẩm — `MainActivity` sẽ bị **thay hoàn toàn** ở Phase 3 theo mockup Phase 2. Không kết nối mạng, không dịch.

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
| `junit:junit` 4.13.2 | unit test (JVM) | EPL-1.0 |
| Be Vietnam Pro Regular + SemiBold | font nhúng (`res/font/be_vietnam_pro_*.ttf`), nguồn https://github.com/google/fonts/tree/main/ofl/bevietnampro | SIL OFL 1.1 — `OFL-BeVietnamPro.txt` |

Không Compose/AppCompat/Leanback/OkHttp. Manifest **không** có `INTERNET`, **không** `POST_NOTIFICATIONS` (kiểm bằng `aapt2 dump permissions`).

## Cấp quyền (trên TV — do User chạy)

- Cài/cấp quyền qua nút trong app. Nếu TV không có màn tương ứng, app hiện lệnh dự phòng ngay trên màn:
  - overlay: `adb shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`
  - miễn pin: `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (đã chạy được trên emulator API 31; **chưa kiểm trên TV thật**)
- Bản debug nhận extra qua `am start` (bản release không có): `adb shell am start -n vn.vietsub.ps5.tv/.MainActivity --ez overlay_on true [--el auto_off_ms 8000]` · `--ez overlay_on false` để tắt.

## Bẫy

- **Chữ ký debug**: APK debug ký bằng debug keystore của **máy build**; build ở máy khác thì `adb install -r` lên TV lỗi chữ ký (phải gỡ app cũ trước). Không dùng keystore nào khác.
- **Nút "Bỏ tối ưu pin" trên Google TV là stub rỗng**: intent `ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` phân giải sang `com.android.tv.settings/.EmptyStubActivity` (đóng ngay, **không** ném `ActivityNotFoundException`). App xử lý: (1) nếu `resolveActivity` ra `EmptyStub*` ⇒ coi như không có màn, hiện ngay lệnh dự phòng `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`; (2) với mọi nút: ghi nhớ nút vừa bấm, ở `onResume` nếu việc **vẫn chưa xong** ⇒ hiện lệnh dự phòng (pin: lệnh `dumpsys deviceidle`; overlay: lệnh `appops`). `<queries>` trong manifest để `resolveActivity` thấy Settings (package visibility API 30+).
- **Màn cấp quyền overlay trên Google TV là danh sách chung** (không mở trang của app): cuộn tìm dòng **"VietSub — lớp phủ kiểm tra"** rồi bật; app hiện lời nhắc này ngay khi bấm nút.
- **Focus D-pad nhìn thấy**: nút có focus = nền vàng `#FFD400` + chữ đen; không focus = nền xám đậm `#3A4654` + chữ trắng (Button mặc định của theme không có trạng thái focus trên TV). Màn bọc `ScrollView` (hint dự phòng làm nội dung cao hơn màn; không bọc thì nút bị ép mất chữ).
- **`launchMode="singleTop"` bắt buộc cho extra debug**: với `standard`, `am start` lên app đang chạy chỉ đưa task lên trước, **không** gọi `onNewIntent` ⇒ `overlay_on` bị bỏ qua (đã gặp).
- **`Paint.hasGlyph` không chứng minh font đủ dấu**: nó tính cả font hệ thống (đối chứng CJK trả `true`). Kiểm đủ dải U+1EA0–1EF9 bằng `FontCoverageTest` (đọc bảng `cmap` của file TTF).
- `OverlayService.addOverlay()` bắt `BadTokenException`/`SecurityException` (quyền bị thu hồi giữa `canDrawOverlays` và `addView`) ⇒ log, vẫn `startForeground` rồi tự dừng (không crash).
- **Foreground service `specialUse`** cần đủ 4 thứ: `foregroundServiceType` trên `<service>`, 2 quyền `FOREGROUND_SERVICE[_SPECIAL_USE]`, `<property …PROPERTY_SPECIAL_USE_FGS_SUBTYPE>`, và `ServiceCompat.startForeground(..., FOREGROUND_SERVICE_TYPE_SPECIAL_USE)`. Chạy được trên emulator Android 12 (API 31); TV thật chưa kiểm.
- **AGP tự cài SDK Build-Tools 36.0.0** vào SDK của máy (AGP 9.0 đòi tối thiểu) khi build lần đầu — cần mạng + licence đã chấp nhận.
- `uiautomator dump` lỗi `could not get idle state` nếu UI liên tục bị làm mới; vì vậy bộ làm mới nhãn "đang bật/tắt" chỉ `setText` khi **đổi**.
- Bẫy `sdkmanager`/`avdmanager` từ PowerShell: gói có `;` phải qua `cmd /c` + dấu ngoặc kép.
- Màn mức "lớp phủ" có `START_NOT_STICKY` và **tự tắt sau 30 phút** (bản debug rút ngắn được bằng `auto_off_ms`); service bị giết ⇒ cửa sổ cũng mất, mở lại app thấy "đang tắt".

## Kích thước lớp phủ

"4 góc" là 4 góc **mặt phẳng đồ hoạ Android** (emulator: 1920×1080 tại mật độ 2.0), việc nó trùng 4 góc **hình HDMI PS5** là điều Bước 6 trên TV thật kiểm. Trên màn lớp phủ có dòng "Kích thước View".
