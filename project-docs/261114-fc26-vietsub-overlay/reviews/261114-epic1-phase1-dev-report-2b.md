DEV-2B DONE
# 261114 — Epic 1 Phase 1 — Dev report 2b (Bước 4–5, `tv-overlay/`)

**Phạm vi**: Bước 4 + 5 (`tv-overlay/`) theo mọi dòng BINDING của Rev 2.2–2.5. Không đụng `engine/`, `CLAUDE.md`, `automation/README.md`; git chỉ đọc; không TV/PS5/IP LAN/`adb connect`; chỉ `emulator-5556` do em bật (đã dừng, `adb devices` sạch). Evidence: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/dev/2b-*` (viết tắt `<dev>/`). Baseline: HEAD `8101fc74`, nhánh `features/261114-fc26-vietsub-overlay`; trước lượt này `tv-overlay/` không tồn tại, 0 test Android.

## 1. Đã làm gì (file) — `tv-overlay/` (24 file chưa theo dõi, `build/`, `.gradle/` bị ignore)

| Nhóm | File |
|---|---|
| Gradle | `settings.gradle.kts`, `build.gradle.kts`, `gradle.properties`, `.gitignore`, `gradlew`, `gradlew.bat`, `gradle/wrapper/{gradle-wrapper.jar,gradle-wrapper.properties}`, `app/build.gradle.kts` |
| Manifest/ tài nguyên | `app/src/main/AndroidManifest.xml` · `res/values/strings.xml` · `res/drawable/{banner,ic_stat}.xml` (vector, không PNG) · `res/font/be_vietnam_pro_{regular,semibold}.ttf` · `OFL-BeVietnamPro.txt` |
| Mã | `CoordinateMapper.kt` (hàm thuần) · `MainActivity.kt` · `OverlayService.kt` · `OverlayView.kt` (package `vn.vietsub.ps5.tv`) |
| Test (JVM) | `CoordinateMapperTest.kt` (3) · `FontCoverage.kt` + `FontCoverageTest.kt` (3) |
| Doc | `README.md` (module) |

Manifest đúng BINDING: `specialUse` đủ 4 thứ (type + `FOREGROUND_SERVICE`/`FOREGROUND_SERVICE_SPECIAL_USE` + `<property …PROPERTY_SPECIAL_USE_FGS_SUBTYPE>` + `ServiceCompat.startForeground(…SPECIAL_USE)`), `SYSTEM_ALERT_WINDOW`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`, leanback launcher + banner, `touchscreen`/`leanback` `required=false`; **không** `INTERNET`, **không** `POST_NOTIFICATIONS` (`aapt2 dump permissions | grep -c "INTERNET|POST_NOTIFICATIONS"` = 0 — `<dev>/2b-13-final-facts.txt`).

## 2. Phiên bản, giấy phép, wrapper

| | Phiên bản | Giấy phép / căn cứ |
|---|---|---|
| Gradle | 9.1.0 — wrapper ghim `distributionSha256Sum` | Java 25 chạy Gradle từ 9.1.0: https://docs.gradle.org/current/userguide/compatibility.html |
| AGP | 9.0.1 (min Gradle 9.1.0, JDK 17, API tối đa 36.1) | Apache-2.0 · https://developer.android.com/build/releases/about-agp · https://developer.android.com/build/releases/agp-9-0-0-release-notes |
| Kotlin | built-in Kotlin của AGP 9 (KGP 2.2.10 runtime) | Apache-2.0; **không** apply `org.jetbrains.kotlin.android` |
| `androidx.core:core-ktx` 1.16.0 · `androidx.window:window` 1.4.0 | `ServiceCompat` · `WindowMetricsCalculator` | Apache-2.0 |
| `junit:junit` 4.13.2 | test JVM | EPL-1.0 |
| Be Vietnam Pro Regular/SemiBold | SHA-256 `cd1ef6e9…e4c3` / `bd8e27eb…bb71`, nguồn `google/fonts/ofl/bevietnampro` | SIL OFL 1.1 (`OFL-BeVietnamPro.txt`) |

Gradle 9.1.0-bin tải về **ngoài repo** (`scratchpad/probe/gradle-dist`), SHA-256 công bố (`…zip.sha256`) = thực tế = `a17ddd85a26b6a7f5ddb71ff8b05fc5104c0202c6e64782429790c933686c806` (khớp). Chạy trên JBR **OpenJDK 25.0.4**: Gradle 9.1.0 + AGP 9.0.1 + Kotlin 2.2.10 build được (không cần JDK khác). `compileSdk 35 / targetSdk 35 / minSdk 29`; không cần `platforms;android-36`. Commit-ready: `gradlew`, `gradlew.bat`, `gradle/wrapper/*` (jar 45 KB); không có bản phân phối trong repo.

## 3. Test: số lượng, RED rồi GREEN

Trước: 0. Sau: **6** (3 mapper + 3 font), 6/6 xanh, build không warning (`grep warning|w:` trống — `<dev>/2b-10-final-build.txt`).

**Mapper (bảng doc, 3 ca)** — RED `<dev>/2b-01-tdd-red.txt` (stub `NotImplementedError`, runtime): `3 tests completed, 3 failed` (`fourCorners_onFullHdView`, `differentViewSize_centerMapsToHalf`, `outOfRange_isClampedToViewBounds` → `kotlin.NotImplementedError`). GREEN `<dev>/2b-02-green-build.txt`: `BUILD SUCCESSFUL`, `CoordinateMapperTest tests=3 failures=0`.

**Font cmap (thêm — xem mục 6.1)** — RED `<dev>/2b-08-font-tdd-red.txt`: `6 tests completed, 3 failed` (3 test font fail vì parser stub). GREEN `<dev>/2b-09-font-tdd-green.txt`: `BUILD SUCCESSFUL`; `FontCoverageTest tests=3 failures=0` ⇒ **cả hai file TTF đều có đủ 90 glyph U+1EA0–U+1EF9 và mọi ký tự chuỗi mẫu** (đọc thẳng bảng `cmap`), và đối chứng `日`/`한` **không** có trong font.

## 4. Smoke trên emulator (Blackbox bước 6–11)

AVD `vietsub_dev` = `system-images;android-31;google-tv;x86`, device `tv_1080p`, tạo bằng `avdmanager` qua `cmd /c` (ngoặc kép); `emulator -accel-check`: WHPX usable; bật **tách tiến trình** qua `cmd /c` + file `.cmd` (ngoài repo) cổng 5556; boot sau 40 s; luôn `adb -s emulator-5556`. Máy ảo: "AOSP TV on x86", Android 12 / API 31, 1920×1080, mật độ 2.0.

| Bước | Kết quả | Evidence (đã xem ảnh/ dump) |
|---|---|---|
| 6 `gradlew :app:testDebugUnitTest` | xanh (6/6) | `2b-02`, `2b-10`, `2b-13-final-facts.txt` |
| 7 `assembleDebug` + `install -r` | APK 7,2 MB; `Success`; `minSdk=29 targetSdk=35` | `2b-03-emu-install.txt` |
| 8 dump ×3 (1.2 #6) | **dump 1** (chưa cấp quyền): focus = nút **CẤP QUYỀN…**; **dump 2** (sau `appops set … allow` + mở lại): "ĐÃ CẤP", focus = **BỎ TỐI ƯU PIN**; **dump 3** (sau `dumpsys deviceidle whitelist +vn.vietsub.ps5.tv`): "ĐÃ MIỄN", focus = **BẬT LỚP PHỦ**. Lệnh dự phòng miễn pin chạy được trên emulator | `2b-04-dump1-*`, `2b-05-dumps-2-3-summary.txt`, `2b-06-dump3-*.xml`, ảnh `2b-04-main-before-grant.png` |
| 8 overlay bật (`am start --ez overlay_on true`) rồi HOME → Settings → `screencap` (Bash) | Ảnh `2b-08-overlay-over-settings-v2.png` (đã xem): 4 dấu góc vàng **sát 4 góc ảnh**, lưới 10%, thẻ chữ Việt 3 cỡ (24/32/44 sp) đủ dấu chồng `ẾỖẶỮỴ ếỗặữỵ` Be Vietnam Pro, "Kích thước View: 1920 × 1080 px" (giữa mép trên), dòng "VietSub — lớp phủ kiểm tra — tắt trong app" góc dưới phải. `dumpsys`: service foreground, cửa sổ `TYPE_APPLICATION_OVERLAY`, `NOT_FOCUSABLE NOT_TOUCHABLE LAYOUT_NO_LIMITS LAYOUT_IN_SCREEN`, `layoutInDisplayCutoutMode=shortEdges`, `TRANSLUCENT` | `2b-07-logcat-service.txt`; `2b-07-overlay-over-settings.png` là bản **trước khi sửa** (dòng "lớp phủ" đè lên "Kích thước View" — lỗi bố cục đã sửa) |
| 9 không ăn phím | Settings có overlay: focus `[220,318][900,444]` → sau `input keyevent KEYCODE_DPAD_DOWN` → `[220,446][900,573]` (di chuyển 1 mục); ảnh trước/sau khác nhau | `2b-09-settings-dump-before/after.xml`, `2b-09-settings-before/after-key.png`, `2b-08-09-10-run.txt` |
| 10 tắt lớp phủ | `am start --ez overlay_on false` (app ở nền) ⇒ `dumpsys activity services` = `(nothing)`; ảnh `2b-10-overlay-off.png` không còn dấu góc | `2b-08-09-10-run.txt` |
| auto_off_ms (1.2 #7) | `--el auto_off_ms 8000`: t+3 s có 1 service; log `overlay bật; tự tắt sau 8000 ms` → `…08:22:15.680 tự tắt sau hạn` (đúng 8,001 s); t+12 s = 0 service, 0 cửa sổ overlay | `2b-11-autooff.txt`, `2b-11-autooff-t3-on.png`, `2b-11-autooff-t12-off.png` |
| hasGlyph log (1.2 #8) | `range U+1EA0-U+1EF9: total=90 ok=90 missing=0`, `sample string missing glyphs: []` — **nhưng đối chứng `{日=true, 한=true}`** ⇒ `hasGlyph` bị fallback hệ thống làm nhiễu, **không** chứng minh được gì (mục 6.1) | `2b-07-logcat-service.txt` |
| nhãn trạng thái sống | màn chính từng hiển thị "ĐANG BẬT" sau khi service tự tắt; đã sửa (làm mới mỗi giây, không đụng focus): dump t+3 s "ĐANG BẬT / TẮT LỚP PHỦ", t+9 s "ĐANG TẮT / BẬT LỚP PHỦ" | `2b-12-live-summary.txt`, `2b-12-live-status-t3/t9.png` |
| 11 dừng emulator | `taskkill /T /F /PID 25468` (cmd launcher của em) ⇒ giết `emulator.exe` 26204 + `qemu` 2156 + con; `adb devices` = rỗng; cổng 5556/5557 đóng; không còn process `emulator`/`qemu`/`ffplay`; `gradlew --stop` ⇒ "1 Daemon stopped", không còn daemon Gradle/Kotlin | `2b-emulator-pids.txt` |

## 5. Lệch so với doc / quyết định (kèm lý do)

1. **Kotlin = built-in của AGP 9**, không apply plugin `kotlin-android` (AGP 9 bật built-in Kotlin mặc định). Doc nói "Kotlin" chung chung.
2. **AGP 9.0 tự cài SDK Build-Tools 36.0.0** vào SDK của User (`…\Android\Sdk\build-tools\36.0.0`, licence đã chấp nhận) ở lần build đầu — AGP đòi tối thiểu 36.0.0, máy chỉ có 35.0.0. Không cài `platforms;android-36`.
3. **Thêm `FontCoverage` + `FontCoverageTest` (test JVM đọc bảng `cmap`)** vì `Paint.hasGlyph` thất bại phép đối chứng (mục 6.1). Giữ luôn log `hasGlyph` ở bản debug theo 1.2 #8 như dữ liệu phụ, README ghi rõ không dùng nó làm bằng chứng.
4. **`android:launchMode="singleTop"`** cho `MainActivity`: với `standard`, `am start` lên app đang chạy chỉ đưa task lên trước, **không** gọi `onNewIntent` ⇒ `overlay_on`/`auto_off_ms` bị bỏ qua (đã tái hiện: không có service). Cold start thì chạy.
5. **Bộ làm mới nhãn mỗi giây** (`MainActivity`): chỉ `setText` khi **đổi** (vì `setText` liên tục làm `uiautomator dump` báo `could not get idle state` — đã gặp).
6. **Dòng "Kích thước View" chuyển lên giữa mép trên** (bản đầu đè lên dòng "lớp phủ kiểm tra" góc dưới phải). Thẻ chữ cao ~80% màn ở mật độ 2.0 (3 cỡ chồng) — giữ cỡ doc.
7. Banner/icon là **vector XML** (không PNG); `ic_stat` vector; `OFL-BeVietnamPro.txt` ở `tv-overlay/` (gốc module).
8. **Lệnh `Start-Process cmd /c "…emulator.exe … > log"` trực tiếp không bật được emulator** (quote bị cmd nuốt, không có tiến trình); dùng file `.cmd` ngoài repo (`scratchpad/probe/start-emu.cmd`) rồi `Start-Process cmd /c <file>`. Guideline §7 nên ghi công thức này.
9. `adb` server (daemon) khởi động lần đầu bởi lệnh của em; không `kill-server` (guideline cấm). Không có thiết bị nào khác.
10. AVD `vietsub_dev` **để lại** trong `~\.android\avd` (Leader ghi tên/API vào `automation/README.md` ở Bước 8).

## 6. Câu hỏi mở

### 6.1 Quyết định nào của Leader hoá ra sai trong thực tế
1. **1.2 #8 — log `Paint.hasGlyph` làm bằng chứng đủ dấu: sai.** `hasGlyph` trả `true` cho `日`/`한` (không có trong Be Vietnam Pro) ⇒ nó tính cả font hệ thống, đúng loại lỗi mà `CLAUDE.md` cảnh báo ("lỗi dễ lọt qua review ảnh"). Cách đúng đã dùng: đọc bảng `cmap` của chính file TTF (unit test, chạy được không cần thiết bị).
2. **N20 — "bật lớp phủ tất định bằng `am start --ez overlay_on true`" thiếu điều kiện**: chỉ đúng khi cold start hoặc `launchMode=singleTop`; mặc định im lặng bỏ qua (tạo kết luận sai "overlay không bật"). Smoke của Reviewer cũng phải `force-stop` trước hoặc dùng bản sửa.
3. **G10/U2 — "chọn cặp Gradle/AGP/Kotlin; compileSdk 35" thiếu 2 hệ quả của AGP 9**: built-in Kotlin (không apply plugin Kotlin) và đòi Build-Tools 36.0.0 (AGP tự cài vào SDK của User). U2 ghi `build-tools;35.0.0` là đủ — không đủ.
4. **Dump `uiautomator` đo focus** (1.2 #6) đúng, nhưng **không** chứng minh focus **nhìn thấy được**: ảnh `2b-04-main-before-grant.png` không có viền/ nền nổi quanh nút đang focus dù `focused=true` (emulator ở touch mode tới khi có phím). Tiêu chí N17(iv) chỉ kiểm thuộc tính, không kiểm thị giác.
5. Thẻ chữ ở mật độ 2.0 chiếm ~80% chiều cao; "24/32/44 sp" lấy từ doc nhưng sp phụ thuộc mật độ của TV — chưa biết kích thước thật.

### 6.2 Chỉ TV Sony thật mới chứng minh được
- Cửa sổ `TYPE_APPLICATION_OVERLAY` **vẽ đè lên nguồn HDMI** và 4 góc mặt phẳng đồ hoạ **trùng 4 góc hình PS5** (overscan/zoom, R-A/R-B) — emulator không có HDMI.
- App không bị TV giết khi chuyển nguồn/ chờ (miễn pin hiệu lực, FGS giữ sống) — emulator không mô phỏng.
- Màn "Cấp quyền hiển thị trên ứng dụng khác" và "bỏ tối ưu pin" **có tồn tại** trên TV hay hiện lệnh dự phòng; `ACTION_*` intent trên Android TV của Sony.
- `specialUse` FGS + `ServiceCompat` trên **Android 10–14** của TV (em chỉ chạy API 31; nhánh API 29–30 chưa chạy).
- Mật độ/ kích thước thật: chữ Việt đọc được từ chỗ ngồi, dấu góc dày 6 dp, thẻ vừa khung; focus D-pad nhìn thấy; banner trên màn Home Google TV.
- Màu/ độ sáng overlay với HDR/ chế độ ảnh; cảm giác trễ tay cầm (không đụng đường hình).
- Chữ ký debug khi cài từ máy khác; tự tắt 30 phút thật (em chỉ kiểm bản rút ngắn 8 s).

## 7. Chưa chứng minh được
- **TV thật** — toàn bộ mục 6.2.
- **Nút trong app** (cấp quyền, bỏ tối ưu pin, Bật/Tắt) chưa được bấm bằng `input` (em dùng `appops`/`dumpsys`/`am start --ez`); nhánh `ActivityNotFoundException` ⇒ hiện lệnh dự phòng chỉ kiểm bằng đọc code.
- Service bị hệ thống giết ⇒ mở lại app thấy trạng thái đúng — chưa kill tiến trình để thử (chỉ thấy `running` reset khi đổi APK/force-stop).
- Bản **release** chưa build ⇒ "không có đường `overlay_on`/`auto_off_ms`" chỉ dựa `BuildConfig.DEBUG` trong code; chưa chạy lint.
- `START_NOT_STICKY` sau khi bị kill chưa kiểm; chưa thử mật độ ≠ 2.0 hay độ phân giải 4K.
- Log `hasGlyph` không có giá trị chứng minh (6.1.1).
- Chưa có chạy lại bởi Reviewer (emulator-5558).

## 8. `git status --short` (cuối lượt)

```
 M .gitignore
 M project-docs/261114-fc26-vietsub-overlay/261114-draft.md
 M project-docs/261114-fc26-vietsub-overlay/261114-workflow-log.md
 M project-docs/261114-fc26-vietsub-overlay/agent-sessions.md
 M project-docs/automation/README.md
 M project-docs/overview-project.md
?? engine/
?? project-docs/261114-fc26-vietsub-overlay/261114-epic1-phase1.md
?? project-docs/261114-fc26-vietsub-overlay/261114-epic1.md
?? project-docs/261114-fc26-vietsub-overlay/261114-lessons.md
?? project-docs/261114-fc26-vietsub-overlay/261114-overview.md
?? project-docs/261114-fc26-vietsub-overlay/_workers/…   (của Leader, có từ trước)
?? project-docs/261114-fc26-vietsub-overlay/playtest/
?? project-docs/261114-fc26-vietsub-overlay/reviews/
?? tv-overlay/
```
Phần của em lượt này: `?? tv-overlay/` (24 file nguồn/ cấu hình; `app/build/`, `.gradle/`, `local.properties` bị ignore — `git check-ignore -v` xác nhận), `?? …/playtest/` (thêm `2b-*`), report này trong `reviews/`; các `M`/`??` còn lại của Leader hoặc lượt 2a. Không sửa `CLAUDE.md`, `automation/README.md`, doc đợt nào khác.
