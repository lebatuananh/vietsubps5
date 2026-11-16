REVIEW-RE2B DONE
# 261114 — Epic 1 Phase 1 — Review lượt 2b (Bước 4–5, `tv-overlay/`)

Reviewer: session độc lập. Nguồn chân lý: [Phase doc](../261114-epic1-phase1.md) Bước 4–5 và các dòng BINDING Rev 2.2–2.5. Đối chiếu với [dev report 2b](261114-epic1-phase1-dev-report-2b.md). Evidence của Reviewer: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/reviewer/2b-*` (viết tắt `<rv>/`).

Smoke chạy trên **emulator riêng của Reviewer**: AVD `vietsub_rv` (cùng image `system-images;android-31;google-tv;x86`, device `tv_1080p`), cổng 5558, `-no-window`, bật tách tiến trình. Launcher PID 25548 → `emulator.exe` 27872 → `qemu` 7196. Đã dừng bằng `taskkill /T /F /PID 25548`; `adb devices` rỗng; `gradlew --stop` → `1 Daemon stopped` (`<rv>/2b-09-emulator-stop.txt`). Không đụng `emulator-5556`, TV thật, IP LAN.

## Verdict

**Verdict: FAIL** — 2 finding blocking. Cả hai đều chỉ lộ ra khi **bấm thật nút trong app bằng phím remote**; Dev chưa bấm (dev report §7). Sửa rất rẻ.

Blocking:

1. **B1 — Nút "Bỏ tối ưu pin" không làm gì trên Google TV và không hiện lệnh dự phòng.** `MainActivity.kt:143-151`. Intent `ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` **resolve được** tới `com.android.tv.settings/.EmptyStubActivity`, một stub tự đóng ngay, nên **không** có `ActivityNotFoundException`. Kết quả: nhánh `catch` hiện `fallback_battery` không bao giờ chạy.
   - Đã chạy: bấm DPAD_CENTER khi focus ở nút ⇒ logcat `START u0 {act=android.settings.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS dat=package:vn.vietsub.ps5.tv cmp=com.android.tv.settings/.EmptyStubActivity}`. App vẫn ở trên cùng, trạng thái vẫn "CHƯA MIỄN", không có gợi ý (`<rv>/2b-06-battery-intent.txt`, `<rv>/2b-05-smoke-log.txt` mục 8b, dump `2b-08b-after-battery-press.xml`).
   - Vì sao chặn: N7 (BINDING) đòi "không mở được ⇒ hiện ngay trên màn lệnh dự phòng". Ở đây không mở được, nhưng User không thấy gì. TvSettings là AOSP, nên TV Sony Google TV gần như chắc chắn cũng vậy. Miễn pin lại là điều kiện của R-A (TV không giết app khi chuyển nguồn).
   - Nguyên nhân: doc chỉ nói bọc `ActivityNotFoundException`, không lường trường hợp stub. Dev làm đúng chữ của doc nhưng chưa bấm thử.
   - Sửa: sau khi bấm, ở `onResume` nếu vẫn `!isIgnoringBatteryOptimizations` ⇒ `showHint(R.string.fallback_battery)`. Hoặc kiểm `packageManager.resolveActivity(intent, 0)?.activityInfo?.name` là `EmptyStubActivity` ⇒ coi như không có màn. Áp cùng logic cho nút overlay (TV có thể không liệt kê app sideload trong danh sách). Kiểm lại: bấm nút trên emulator ⇒ thấy lệnh `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` trên màn.
2. **B2 — Nút đang focus không có dấu hiệu nhìn thấy được**, kể cả sau khi đã bấm phím D-pad (không còn ở touch mode). `MainActivity.kt:95-102`: `Button` mặc định trên `Theme.DeviceDefault.NoActionBar` (`AndroidManifest.xml:23`).
   - Đã chạy: ảnh `<rv>/2b-08c-main-all-granted.png` (focus ở "BẬT LỚP PHỦ" theo dump `2b-08c-dump3.xml`) và `<rv>/2b-07-main-after-dpad.png` (sau DPAD_UP rồi DPAD_DOWN). 3 nút giống hệt nhau. Dev cũng đã thấy (dev §6.1.4) nhưng chưa sửa.
   - Vì sao chặn: N17 (iv) là điều kiện BINDING để được miễn mockup, với mục đích dẫn User bằng remote tới việc tiếp theo. Focus không nhìn thấy thì không dẫn được. Ở Bước 6, User thao tác app trên TV **chỉ bằng remote**, và phải tắt được lớp phủ chắc chắn (N16).
   - Sửa: đổi nền/viền khi focus, ví dụ `setOnFocusChangeListener` đổi nền sang `#FFD400` + chữ đen, hoặc một `StateListDrawable` `state_focused`. Kiểm bằng ảnh chụp sau một phím D-pad.

Non-blocking: xem bảng Finding.

## Đối chiếu BINDING ↔ code (Bước 4–5)

| Yêu cầu | Kết quả | Chỗ trong code / evidence |
|---|---|---|
| `specialUse` đủ 4 thứ | OK | (1) `AndroidManifest.xml:35` `foregroundServiceType="specialUse"`; (2) `:7-8` `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_SPECIAL_USE`; (3) `:36-38` `PROPERTY_SPECIAL_USE_FGS_SUBTYPE`; (4) `OverlayService.kt:42` `ServiceCompat.startForeground(this, NOTIF_ID, buildNotification(), ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)`. Chạy thật: `isForeground=true foregroundId=1` (`2b-05` mục 8d) |
| Không `INTERNET`, không `POST_NOTIFICATIONS` | OK | `aapt2 dump permissions` chỉ có 4 quyền + quyền receiver nội bộ do AndroidX tự sinh; `INTERNET/POST_NOTIFICATIONS count: 0` (`2b-05` mục 0) |
| `SYSTEM_ALERT_WINDOW`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` | OK | `AndroidManifest.xml:5-6` |
| Leanback launcher + banner; `leanback`/`touchscreen` `required=false` | OK | `:26`, `:15`, `:10-11`; aapt2: `leanback-launchable-activity` |
| `minSdk 29` / `targetSdk 35` / `compileSdk 35` | OK | `app/build.gradle.kts:7,11,12` |
| Thêm cửa sổ overlay **trước** `startForeground` | OK | `OverlayService.kt:40` `addOverlay()` rồi mới `:42` |
| `START_NOT_STICKY` + tự tắt 30 phút | OK | `:37,46,58`; `DEFAULT_AUTO_OFF_MS = 30L * 60 * 1000` `:144`; `postDelayed(autoOff, …)` `:54-55` |
| Cờ cửa sổ | OK | `:63-73`: `TYPE_APPLICATION_OVERLAY`, `MATCH_PARENT`×2, `FLAG_NOT_FOCUSABLE | NOT_TOUCHABLE | LAYOUT_NO_LIMITS | LAYOUT_IN_SCREEN`, `TRANSLUCENT`, cutout `SHORT_EDGES`. Chạy thật: `ty=APPLICATION_OVERLAY fmt=TRANSLUCENT fl=NOT_FOCUSABLE NOT_TOUCHABLE LAYOUT_IN_SCREEN LAYOUT_NO_LIMITS`, `Requested w=1920 h=1080` |
| Hai intent bọc `try/catch` + lệnh dự phòng trên màn | Có code, nhưng **vô hiệu với nút pin trên TV** (B1) | `MainActivity.kt:133-151`; chuỗi `strings.xml:18-19` |
| `WindowMetricsCalculator` | OK | `MainActivity.kt:118`; `app/build.gradle.kts:33` |
| Focus vào việc tiếp theo | OK về thuộc tính, **không nhìn thấy** (B2) | `MainActivity.kt:124-130`. Ba dump: CHƯA CẤP ⇒ focus "CẤP QUYỀN…"; sau appops ⇒ "BỎ TỐI ƯU PIN"; sau whitelist ⇒ "BẬT LỚP PHỦ" (`2b-08a/b/c-dump*.xml`) |
| Làm mới ở `onResume` | OK | `:47-51`. Chạy thật: cấp quyền qua màn TV Settings bằng remote, BACK về app ⇒ "ĐÃ CẤP", focus chuyển sang "BỎ TỐI ƯU PIN" (`2b-07-permission-list-and-focus.txt`) |
| Chữ tiếng Việt | OK | `strings.xml:3-24` |
| Extra debug-only (`overlay_on`, `auto_off_ms`) không có ở release | OK | `MainActivity.kt:177` `if (!BuildConfig.DEBUG || …) return`; `OverlayService.kt:49-53`. Build release: `BuildConfig.java` release `DEBUG = false`, debug `DEBUG = Boolean.parseBoolean("true")` (`<rv>/2b-04-tests-buildconfig.txt`) |
| Dấu góc chữ L + lưới 10% | OK | `OverlayView.kt:63-77` (đỉnh tại góc, cạnh vào trong qua `dir`), `:54-61` (9 đường, `strokeWidth = 1f`, alpha 77 ≈ 30% `:25-29`). Ảnh `<rv>/2b-08d-overlay-over-settings.png`: 4 góc sát mép, lưới, thẻ 3 cỡ, dấu chồng đúng, "Kích thước View: 1920 × 1080 px", dòng cuối góc dưới phải |
| Chuỗi mẫu đúng doc, 3 cỡ 24/32/44 sp, thẻ ~80% đục | OK | `strings.xml:24` trùng nguyên văn doc; `OverlayView.kt:84-86`, `:30` |
| Font tên chữ thường + `_`, kèm OFL | OK | `res/font/be_vietnam_pro_regular.ttf`, `be_vietnam_pro_semibold.ttf`; `OFL-BeVietnamPro.txt` (SIL OFL 1.1); SHA-256 khớp dev report |
| Wrapper commit, không có bản phân phối | OK | `gradlew`, `gradlew.bat`, `gradle/wrapper/{jar 45457 B, properties}`; `distributionSha256Sum` ghim (`gradle-wrapper.properties:3`); 24 file untracked, không có zip |
| `.gitignore` module | OK | `build/ .gradle/ local.properties *.keystore` — `git check-ignore -v` (`<rv>/2b-10-gitignore.txt`) |
| Giấy phép trong README | OK | `tv-overlay/README.md:31-39` |
| Bẫy chữ ký debug trong README | OK | `README.md:52` |

## Lệch của Dev (dev report §5) — đánh giá

| # | Lệch | Đánh giá |
|---|---|---|
| 1 | Kotlin built-in của AGP 9, không apply `kotlin-android` | **Chấp nhận.** Đúng mặc định chính thức của AGP 9 (dev đã dẫn release notes); build sạch |
| 2 | AGP tự cài Build-Tools 36.0.0 vào SDK của User | **Chấp nhận**, nằm trong phạm vi "cài công cụ" G10 cho phép. Leader cần ghi vào U2/`automation/README.md` (đang ghi `build-tools;35.0.0` là đủ — không đúng) |
| 3 | `FontCoverage` đọc `cmap` thay vì dựa vào `hasGlyph` | **Tốt hơn kế hoạch.** Đã kiểm độc lập (mục TDD). `hasGlyph` trên emulator của mình cũng trả `{日=true, 한=true}` — vô dụng làm bằng chứng, đúng như Dev nói |
| 4 | `launchMode="singleTop"` | **Chấp nhận, cần thiết** cho N20. Đã chạy: tắt bằng extra khi app đang ở trên ⇒ 0 service, 0 cửa sổ (`2b-05` mục 10b). Phụ: `onNewIntent` → `setIntent` (`MainActivity.kt:41-45`) đúng |
| 5 | Làm mới nhãn mỗi giây | **Chấp nhận.** Chỉ chạy khi resumed (gỡ ở `onPause` `:53-56`), chỉ `setText` khi đổi. Đã chạy: t≈3 s "ĐANG BẬT", t≈10 s "ĐANG TẮT" sau auto-off (`2b-11-main-t3/t10.xml`) |
| 6 | Dòng "Kích thước View" chuyển lên mép trên | **Chấp nhận.** Ảnh không có chồng lấn |

## TDD

- Mapper: RED thật — `dev/2b-01-tdd-red.txt`: `3 tests completed, 3 failed`, `kotlin.NotImplementedError`. GREEN: 3/3. Test khớp bảng doc (4 góc, View 3840×2160, kẹp `(−0.1, 1.2)` ⇒ `(0, H)`) — `CoordinateMapperTest.kt:6-23`.
- Font: RED `dev/2b-08-font-tdd-red.txt` `6 tests completed, 3 failed`.
- **`FontCoverage` có parse `cmap` thật không** — kiểm bằng 2 cách độc lập (`<rv>/2b-01-font-cmap-check.txt`): (1) port Python **từng dòng** của `FontCoverage.kt` ra 90/90 dải U+1EA0–1EF9, chuỗi mẫu không thiếu, `A=True 日=False 한=False`, **khớp 100%** với oracle Windows GDI `GetGlyphIndicesW` (`GGI_MARK_NONEXISTING_GLYPHS`) trên 139 code point, cho cả hai file; (2) **ca âm tính**: chính parser đó trên `symbol.ttf`/`marlett.ttf` ra `viet 0/90`. Test còn có đối chứng 'A' có / CJK không (`FontCoverageTest.kt:31-37`), nên stub luôn-đúng hoặc luôn-sai đều sẽ đỏ.
- Chạy lại (cưỡng bức `--rerun`): `CoordinateMapperTest tests="3" failures="0"`, `FontCoverageTest tests="3" failures="0"` (`<rv>/2b-04`). Build debug + release `BUILD SUCCESSFUL`; không có warning của Kotlin/Java. Có một cảnh báo môi trường "SDK XML version 4" do lệch bản SDK tools, không phải từ code (`<rv>/2b-02-gradle-build.txt`).

## Smoke lại (emulator-5558, dữ liệu và kịch bản của Reviewer)

| Bước | Kết quả | Evidence |
|---|---|---|
| 7 install | `Success` | `2b-05` |
| 8 ba dump (1.2 #6) | focus đúng thứ tự cả 3 lần (xem bảng BINDING) | `2b-08a/b/c-*.xml`, `2b-08a-main-no-perm.png` |
| 8 **thêm**: bấm thật nút cấp quyền | mở `com.android.tv.settings/.device.apps.specialaccess.SystemAlertActivity` — danh sách **chung** (bỏ qua `package:`); app nằm ở dòng thứ 8; bật công tắc bằng remote ⇒ `SYSTEM_ALERT_WINDOW: allow` | `2b-08a-after-grant-press.png`, `2b-07-overlay-list-toggled.png`, `2b-07-permission-list-and-focus.txt` |
| 8 **thêm**: bấm thật nút pin | **không có gì xảy ra** ⇒ B1 | `2b-06-battery-intent.txt` |
| 8 bật overlay tất định (force-stop trước), HOME, Settings | 1 service foreground, 1 cửa sổ overlay; ảnh đúng | `2b-08d-overlay-over-settings.png` |
| 9 không ăn phím | Settings: focus `[220,446][900,573]` → DPAD_DOWN → `[220,575][900,701]` | `2b-09-settings-before/after.xml` |
| 10 tắt **bằng nút trong app** (Dev dùng extra) | DPAD_CENTER trên "TẮT LỚP PHỦ" ⇒ 0 service, 0 cửa sổ, `(nothing)`; nhãn "ĐANG TẮT", focus "BẬT LỚP PHỦ" | `2b-10-*.xml`, `2b-10-home-overlay-off.png` |
| 10b bật bằng nút, tắt bằng extra (`singleTop`) | 1/1 ⇒ 0/0 | `2b-05` |
| `auto_off_ms 6000` | log `overlay bật; tự tắt sau 6000 ms` lúc 08:36:25.444 → `tự tắt sau hạn` lúc 08:36:31.447 (6,003 s); t≈10 s: 0 service, 0 cửa sổ | `2b-05` mục 11 |
| **thêm**: tiến trình bị giết khi overlay đang bật (`am crash`) | 0 service, 0 cửa sổ, không tự bật lại (`START_NOT_STICKY`); mở lại app: "ĐANG TẮT" — đúng edge case Bước 5 | `2b-12-main-after-crash-reopen.xml` |

Khớp với evidence của Dev ở mọi điểm Dev đã chạy (thứ tự focus, cờ cửa sổ, auto-off, phím đi xuyên).

## Finding

| # | Mức độ | Chỗ nào | Vấn đề | Nguyên nhân | Chấp nhận được? |
|---|---|---|---|---|---|
| B1 | **blocking** | `MainActivity.kt:143-151` | Nút pin là stub, không hiện lệnh dự phòng (chi tiết ở Verdict) | Doc chỉ định `ActivityNotFoundException`; chưa bấm thử | Không |
| B2 | **blocking** | `MainActivity.kt:95-102` | Focus không nhìn thấy (chi tiết ở Verdict) | Button mặc định của theme không có trạng thái focus trên TV | Không |
| N1 | non-blocking | `OverlayService.kt:61-77` | `wm.addView` không bắt `WindowManager.BadTokenException`/`SecurityException` (quyền bị thu hồi giữa `canDrawOverlays` và `addView`) ⇒ crash trước `startForeground` | Spike | Bọc `try/catch` ⇒ log + `shutdown()` |
| N2 | non-blocking | `MainActivity.kt:133-141` | Trên Google TV intent overlay mở **danh sách chung**, không mở trang của app; User phải cuộn ~8 dòng để tìm "VietSub — lớp phủ kiểm tra" | Hành vi TvSettings | Thêm vào hint/README: "tìm dòng VietSub — lớp phủ kiểm tra" |
| N3 | non-blocking | `OverlayView.kt:82-88` | Thẻ chữ cao ~78% màn ở mật độ 2.0 ⇒ trên TV sẽ che gần hết hình PS5 khi kiểm R-B | Cỡ chữ theo doc | Chấp nhận cho công cụ kiểm tra; User chỉ cần nhìn 4 góc |
| N4 | non-blocking | `OverlayService.kt:57,120-137` | Log `hasGlyph` (debug) vẫn chạy mỗi lần bật, dù đã chứng minh không có giá trị | Giữ theo 1.2 #8 | Chấp nhận (README ghi rõ) |
| N5 | non-blocking | — | Nhánh API 29–30 chỉ đọc code: `ServiceCompat` nhận type `specialUse` (0x40000000), là tập con của type khai trong manifest nên không ném lỗi; `layoutInDisplayCutoutMode` (API 28+), `resources.getFont` (API 26+), `WindowMetricsCalculator` hỗ trợ API 29. Không thấy API nào > 29 bị gọi không điều kiện | Không có image API 29/30 | Chấp nhận; TV thật xác nhận |
| N6 | non-blocking | — | Vòng đời: `handler.removeCallbacks` cả trong `shutdown()` lẫn `onDestroy()` (`:93,101`); `removeView` cả hai nơi, có bắt `IllegalArgumentException` (`:80-90`); ticker gỡ ở `onPause`; `postDelayed` refresh có chặn `isDestroyed` (`MainActivity.kt:168`). **Không thấy rò** | — | OK |
| N7 | non-blocking | — | AVD `vietsub_rv` của Reviewer để lại trong `~\.android\avd` (cùng kiểu `vietsub_dev`) | — | Leader quyết xoá hay ghi vào `automation/README.md` |

## Chưa kiểm được

- TV Sony thật (vẽ đè HDMI, giữ app khi chuyển nguồn, mật độ thật, màn quyền của Sony) — ngoài vai.
- API 29/30 chạy thật — chỉ có image API 31.
- Tự tắt 30 phút thật — chỉ kiểm bản 6 s (cùng code path, khác hằng số).
- Bản release cài thật — APK release chưa ký; chỉ kiểm `BuildConfig.DEBUG=false` và cổng chặn trong code.

## Câu hỏi mở — TV Sony thật có thể làm hỏng gì mà emulator không cho thấy; User nên kiểm gì đầu tiên

1. **Kiểm trước khi cài**: `adb shell getprop ro.build.version.sdk` ≥ 29. TV Android 9 sẽ báo `INSTALL_FAILED_OLDER_SDK` vì `minSdk 29` — U3 vẫn chưa có.
2. **Miễn pin**: nút trong app gần như chắc chắn không làm gì (B1) ⇒ User **chạy luôn** `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`, rồi mở lại app và thấy "ĐÃ MIỄN". Ngay cả khi đã miễn, Sony có thể có trình dọn bộ nhớ riêng giết app khi chuyển sang HDMI — đây là rủi ro R-A số 1. Kiểm bằng Bước 6.5 (HDMI → chờ 1 phút → lớp phủ còn không).
3. **Quyền overlay**: trên một số bản Google TV, app sideload không có trong danh sách "Hiển thị trên ứng dụng khác", hoặc công tắc bị khoá. Không thấy app trong danh sách ⇒ dùng ngay `adb shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow` (Bước 6.2 đã có lệnh này) — đừng mất thời gian tìm.
4. **Lớp phủ trên nguồn HDMI**: hình HDMI trên TV thường đi qua plane phần cứng riêng. Cửa sổ `TYPE_APPLICATION_OVERLAY` thường vẫn nổi trên, nhưng chế độ Game/ALLM của Sony có thể tắt hoặc giới hạn lớp đồ hoạ. Nếu không thấy 4 góc ở nguồn HDMI mà vẫn thấy trên Home ⇒ ghi lại chế độ ảnh, thử tắt chế độ Game, **trước** khi kết luận R-A trượt.
5. **Thao tác bằng remote**: focus không nhìn thấy (B2) — nếu chưa sửa, User nhìn dòng trạng thái để biết đã bật/tắt; nút được focus mặc định luôn là "việc tiếp theo".

---

## Re-review fix 2b

**Verdict: PASS** — không có finding blocking.

Phạm vi: chỉ phần delta B1, B2, N1, N2 (+ `ScrollView` Dev tự khai), đối chiếu với [dev report fix2b](261114-epic1-phase1-dev-report-fix2b.md). Chạy trên emulator riêng `emulator-5558` (AVD `vietsub_rv`, bật tách tiến trình): launcher PID 30484 → `emulator.exe` 11080 → `qemu` 25916. Đã dừng bằng `taskkill /T /F /PID 30484`; `adb devices` rỗng; `1 Daemon stopped` (`<rv>/re2b-14-emulator-stop.txt`). Evidence mới có tiền tố `re2b-`.

### Build · test

`gradlew clean :app:testDebugUnitTest :app:assembleDebug` ⇒ `BUILD SUCCESSFUL`; `CoordinateMapperTest tests="3" … failures="0"`, `FontCoverageTest tests="3" … failures="0"`. Không có warning `w:`/`warning:` từ code; chỉ còn dòng môi trường "SDK XML version 4" như ở lượt 2b (`<rv>/re2b-01-build-test.txt`).

### Từng mục: đã kiểm + rủi ro hồi quy

| Mục | Đã kiểm (code) | Chạy lại — **chỉ `input keyevent`** sau bước reset | Rủi ro hồi quy của chính bản sửa |
|---|---|---|---|
| **B1** | `MainActivity.kt:39-40` (`Pending`), `:167-170` `opensRealScreen` (tên lớp chứa `EmptyStub`), `:172-185` `launchSettings` (vẫn giữ `ActivityNotFoundException`), `:203-212` `checkPendingAfterReturn`, gọi ở `onResume` `:57`; `<queries>` `AndroidManifest.xml:11-20` | DPAD_DOWN → ENTER trên "BỎ TỐI ƯU PIN": app vẫn ở trên cùng, **hint vàng hiện**: "Miễn tối ưu pin vẫn CHƯA được (TV này không có màn thật)… `adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`" (`re2b-05-battery-hint.png`, dump `re2b-05-battery-pressed.xml`) | **Phát hiện stub theo tên lớp** chỉ là đường tắt. Nếu TV Sony dùng stub tên khác ⇒ `startActivity` chạy ⇒ stub tự đóng ⇒ app `onResume` ⇒ `checkPendingAfterReturn` vẫn hiện hint. Hai lớp bảo vệ độc lập — **chấp nhận**. Lưu ý: nhánh `onResume` cho nút **pin** chưa chạy được trên emulator (đường tắt bắt trước), chỉ chạy cho nút overlay. **`pending` không sống qua việc tiến trình bị giết hay Activity bị tạo lại** (không có `onSaveInstanceState`) ⇒ khi đó quay về sẽ không có hint, nhưng dòng trạng thái vẫn "CHƯA …" và bấm lại nút sẽ hiện hint ⇒ non-blocking (NB-R1). `resolveActivity == null` ⇒ để `startActivity` quyết: đúng hướng an toàn |
| **N2** + B1 overlay | `MainActivity.kt:187-193` (hint `hint_overlay_list` trước khi mở); `strings.xml` `hint_overlay_list`, `fallback_overlay` | DPAD_UP → ENTER: mở `com.android.tv.settings/.device.apps.specialaccess.SystemAlertActivity` (`re2b-07-overlay-settings-opened.png`). BACK ⇒ hint "…Trong danh sách, tìm dòng “VietSub — lớp phủ kiểm tra” và bật; nếu TV không có màn này, chạy lệnh … `adb shell appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow`" (`re2b-08-after-back-overlay-hint.png`). Cấp thật bằng remote: dòng VietSub được focus sau **8** lần DPAD_DOWN, ENTER ⇒ `SYSTEM_ALERT_WINDOW: allow`; BACK ⇒ "ĐÃ CẤP", **hint ẩn**, focus chuyển sang "BỎ TỐI ƯU PIN" (`re2b-10-after-grant-back.*`) | Không thấy |
| **B2** | `MainActivity.kt:150-164` `styleForTv`: `StateListDrawable` (`state_focused`/`state_pressed` ⇒ `#FFD400`, mặc định `#3A4654`) + `ColorStateList`; áp ở `:106` | Sau DPAD_DOWN: chỉ nút được focus có nền vàng, chữ đen; hai nút kia xám (`re2b-04-down-battery-focused.png`, `re2b-06-up-grant-focused.png`, `re2b-09-toggle-focused-with-hint.png`; ghép ở `re2b-13-contact-sheet.png`) | `StateListDrawable`, `GradientDrawable.cornerRadius`, `ColorStateList` đều có từ API 1 ⇒ **an toàn trên API 29** (đọc code). Thay `background` làm mất ripple/elevation mặc định — không ảnh hưởng gì với remote |
| **ScrollView** (lệch Dev tự khai) | `MainActivity.kt:115-122`, `isFillViewport = true` | Khi có hint 3 dòng, DPAD_DOWN ×2 tới "BẬT LỚP PHỦ" ⇒ nút ở `[96,971][366,1067]` trên màn 1080, đủ chữ, nền vàng (`re2b-09`). Focus tự cuộn | **Chấp nhận** — sửa đúng chỗ (nút bị ép mất chữ), không có tác dụng phụ với D-pad. Nội dung đã sát đáy ở mật độ 2.0 ⇒ TV có mật độ cao hơn sẽ phải cuộn, nhưng `ScrollView` lo được |
| **N1** | `OverlayService.kt:40` `val added = canDraw && (view != null \|\| addOverlay())`, `:42` vẫn `startForeground`, `:43-47` `if (!added)` ⇒ `shutdown()`; `:62-88` `addOverlay(): Boolean` bắt `BadTokenException` + `SecurityException` | Chỉ đọc code (giống Dev — không tái hiện được việc thu hồi quyền giữa hai lệnh) | `startForeground` vẫn được gọi trước `shutdown` ⇒ không vi phạm hợp đồng `startForegroundService`. Nếu `view != null` (đang chạy) mà quyền bị thu hồi ⇒ `added=false` ⇒ `shutdown` gỡ view — đúng |

**Hành vi mới thấy khi chỉ dùng remote** (NB-R2, non-blocking): bấm ENTER trên "BẬT LỚP PHỦ" lúc pin **chưa** miễn ⇒ overlay bật (1 service), nhưng 500 ms sau `refresh()` (`MainActivity.kt:229` → `:146`) **kéo focus về "BỎ TỐI ƯU PIN"**. Lần ENTER thứ hai của mình bấm vào nút pin chứ không phải "TẮT LỚP PHỦ": overlay vẫn bật, hint pin hiện (`re2b-12-after-off.xml`). Focus có viền vàng nên User **thấy** được nó đang ở đâu. Theo thứ tự Bước 6 (miễn pin xong mới bật), focus sẽ nằm ở nút Bật/Tắt ⇒ chấp nhận cho Phase 1. Sửa rẻ nếu muốn: chỉ `requestFocus` ở `onResume`, không làm trong `refresh()` sau khi bật/tắt.

### Câu hỏi mở 1–6 của Dev (UX chỉ có remote) — phân loại

| # | Câu hỏi | Phân loại | Lý do |
|---|---|---|---|
| 1 | Focus nhảy về "việc tiếp theo" sau khi bấm Bật mà thiếu quyền / thiếu miễn pin | **Chấp nhận cho APK chẩn đoán** | Focus đã nhìn thấy được; đúng thứ tự Bước 6 thì không gặp. Đưa vào mockup Phase 2: focus không được dời sau một thao tác của User (NB-R2) |
| 2 | Danh sách quyền overlay dài; thoát bằng HOME thì hint chỉ hiện lần mở sau; `pending` mất khi tiến trình chết | **Chấp nhận** | Dòng trạng thái luôn đúng ở `onResume`; hint nêu tên dòng cần tìm; đã chứng minh 8 phím là tới (NB-R1) |
| 3 | Hint là lệnh `adb` — người chỉ có remote không chạy được | **Chấp nhận cho Phase 1** | Bước 6 do User + Leader làm, có sẵn máy tính và `adb` (6.1–6.2). Phase 2 phải thiết kế đường không cần máy tính |
| 4 | Không có cách tắt nhanh khi đang ở HDMI (phải mở lại app hoặc chờ 30 phút) | **Chấp nhận cho Phase 1** | Có tự tắt 30 phút (N16) + mở app ⇒ focus ở "TẮT LỚP PHỦ" khi mọi quyền đã xong. **Bắt buộc** đưa vào mockup Phase 2 (sản phẩm sẽ phủ lâu dài) |
| 5 | Thông báo foreground bị ẩn | **Chấp nhận** | Theo quyết định N5 (không `POST_NOTIFICATIONS`); TV không có ngăn thông báo hữu dụng |
| 6 | Không thấy app khi lớp phủ đang phủ | **Chấp nhận** | Lớp phủ trong suốt + `NOT_FOCUSABLE`; app vẫn nhìn và thao tác được bên dưới (smoke 2b bước 9; `re2b-11-overlay-on-over-app.png`) |

Không mục nào phải sửa ngay.

### Câu hỏi mở — APK chẩn đoán đã đủ an toàn và dùng được bằng remote để User cài lên TV thật chưa?

**Đủ, với 3 điều kiện thao tác.**
- An toàn: không `INTERNET`, không đụng đường HDMI, tự tắt 30 phút, `START_NOT_STICKY`, process chết thì không tự bật lại (đã kiểm ở 2b).
- Dùng được bằng remote: focus nhìn thấy, hai nút quyền luôn dẫn tới lệnh dự phòng, cấp quyền overlay bằng remote đi được trọn đường.

Ba điều kiện:
1. Kiểm `ro.build.version.sdk` ≥ 29 trước khi cài.
2. Leader chạy **luôn** lệnh `deviceidle whitelist` trong Bước 6.2 — nút pin trên Google TV gần như chắc chắn là stub.
3. Làm theo thứ tự quyền → miễn pin → bật, để focus luôn nằm ở nút Bật/Tắt.

Rủi ro còn lại chỉ TV thật trả lời được (đã nêu ở mục Câu hỏi mở của review 2b): Sony giết app khi chuyển sang HDMI, chế độ Game/ALLM có hiện lớp phủ không, app sideload có trong danh sách quyền overlay không.
