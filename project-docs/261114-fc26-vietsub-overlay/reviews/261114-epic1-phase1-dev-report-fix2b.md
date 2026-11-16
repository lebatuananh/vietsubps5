DEV-FIX2B DONE
# 261114 — Epic 1 Phase 1 — Dev report fix2b (vòng sửa sau review 2b: B1, B2, N1, N2)

Chỉ sửa trong `tv-overlay/` (mã + `README.md`) và report này. Emulator: chỉ `emulator-5556` do em bật (cmd launcher PID 11832 → `emulator.exe` 30308 → `qemu` 30176), đã dừng bằng `taskkill /T /F /PID 11832`; `adb devices` rỗng, cổng 5556/5557 đóng, không còn process emulator/qemu/daemon Gradle. Không đụng emulator-5558, TV, PS5, IP LAN, `adb connect`; git chỉ đọc. Evidence: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase1/dev/fix2b-*` (`<dev>/`).

## 1. Từng mục: đổi gì (file:dòng) + bằng chứng

| Mục | Thay đổi | Vị trí | Bằng chứng |
|---|---|---|---|
| **B1** pin/ overlay "vẫn chưa xong sau khi quay về" | (a) Ghi nhớ nút vừa bấm (`pending`); ở `onResume` nếu việc **vẫn chưa xong** ⇒ hiện lệnh dự phòng (pin: `dumpsys deviceidle whitelist +…`; overlay: `appops set …`); đã xong ⇒ ẩn hint. (b) `resolveActivity` ra `EmptyStub*` ⇒ coi như không có màn, hiện hint ngay, không mở. (c) `ActivityNotFoundException` vẫn giữ. (d) `<queries>` cho 2 action để `resolveActivity` thấy Settings (package visibility API 30+) | `MainActivity.kt:167` `opensRealScreen`, `:172` `launchSettings`, `:187/:195` hai nút, `:203` `checkPendingAfterReturn`, `:57` gọi ở `onResume`; `AndroidManifest.xml:11-20`; `strings.xml:19-20` | `fix2b-11-resolve.txt`: intent pin ⇒ `com.android.tv.settings/.EmptyStubActivity` (đúng như reviewer), intent overlay ⇒ `…/.SystemAlertActivity`. **Bấm ENTER thật** vào "BỎ TỐI ƯU PIN": `mResumedActivity` vẫn là `MainActivity`, ảnh `fix2b-05-s3-after-battery-enter.png` (đã xem) hiện dòng vàng "Miễn tối ưu pin vẫn CHƯA được … adb shell dumpsys deviceidle whitelist +vn.vietsub.ps5.tv"; dump `fix2b-05-s3.xml`. **Bấm ENTER thật** vào "CẤP QUYỀN…": mở `SystemAlertActivity` (`fix2b-07-s4-overlay-settings-opened.png`), nhấn **BACK** ⇒ `onResume` ⇒ ảnh `fix2b-08-s5-after-back-overlay-hint.png` (đã xem) hiện hint `fallback_overlay` kèm lệnh `appops` |
| **B2** focus nhìn thấy | Nút: focus/pressed = nền `#FFD400` + chữ đen; không focus = nền `#3A4654` + chữ trắng (`StateListDrawable` + `ColorStateList`, bo góc, padding) | `MainActivity.kt:150` `styleForTv`, áp ở `button(...)` | `fix2b-04-s2-down-battery-focused.png` (sau **DPAD_DOWN** thật: "BỎ TỐI ƯU PIN" vàng, hai nút kia xám); `fix2b-06-s4-up-grant-focused.png` (sau **DPAD_UP**); `fix2b-09-s6-toggle-focused.png` ("BẬT LỚP PHỦ" vàng sau 2×DPAD_DOWN); dump kèm `focused=true` khớp (`fix2b-0x-*.xml`) |
| **N1** `addView` | `addOverlay()` trả `Boolean`; bọc `try/catch` `WindowManager.BadTokenException` + `SecurityException` ⇒ log + `false`; `onStartCommand` vẫn gọi `startForeground` rồi `shutdown()` khi `!added` (không crash trước `startForeground`) | `OverlayService.kt:40` (`val added`), `:43` (`if (!added)`), `:61-62`, `:81-86` | **Chỉ đọc code + build** — không tái hiện được quyền bị thu hồi giữa hai lệnh trên emulator |
| **N2** hint overlay | Bấm nút cấp quyền ⇒ hiện ngay `hint_overlay_list` "tìm dòng “VietSub — lớp phủ kiểm tra” rồi bật"; sau khi quay về mà chưa cấp, `fallback_overlay` cũng nhắc dòng này + lệnh `appops` | `strings.xml:18-19`, `MainActivity.kt:187` | ảnh `fix2b-08-…png` |
| README | Ghi: stub pin trên Google TV + cách xử lý, màn overlay là danh sách chung, focus màu, `ScrollView`, `addOverlay` try/catch | `tv-overlay/README.md` (mục *Bẫy*) | — |

### Lệch/ thêm ngoài danh sách (nêu rõ)
- **`ScrollView` bọc màn** (`MainActivity.kt:115-121`): ở lần chạy đầu của B1, khi hint (2–3 dòng) hiện, nút thứ ba **bị ép mất chữ** (ảnh trung gian thấy nút "BẬT LỚP PHỦ" thành một thanh xám trống) — làm B1 mất ý nghĩa với người dùng remote. Sửa tối thiểu bằng `ScrollView`; chạy lại toàn bộ kịch bản sau đó (các ảnh `fix2b-*` ở bảng trên là **sau** sửa; ở `fix2b-09` nút thứ ba nằm sát đáy màn nhưng đủ chữ).
- Không đổi gì khác (không đụng `engine/`, test, `OverlayView`, manifest ngoài `<queries>`).

## 2. Build + test

Trước: 6 test, sau: **6** (3 mapper + 3 font), 6/6 xanh (`CoordinateMapperTest tests=3 failures=0`, `FontCoverageTest tests=3 failures=0`). `gradlew :app:testDebugUnitTest :app:assembleDebug` ⇒ `BUILD SUCCESSFUL`, **0 warning** (`grep "warning|w: |e: "` trống — `<dev>/fix2b-01-build.txt`). Không có test JVM mới: B1/B2/N1/N2 là hành vi Android (Activity/Window), kiểm bằng smoke có phím thật bên dưới; không có hàm thuần mới để TDD.

## 3. Kịch bản phím thật trên emulator (không `appops`/`am start --ez` cho các nút)

Chuẩn bị: gỡ `appops` về `default`, `deviceidle whitelist -pkg`, `force-stop`, mở app bằng `am start -n …/.MainActivity` (chỉ để khởi động). Rồi chỉ dùng `input keyevent`:

1. Khởi động: focus = nút cấp quyền (dump `fix2b-03-s1.xml`; ảnh `fix2b-03-s1-initial.png`).
2. `DPAD_DOWN` → focus pin (vàng) · `ENTER` → hint pin (B1 ✔, ảnh `…05…`).
3. `DPAD_UP` → focus cấp quyền · `ENTER` → mở `SystemAlertActivity` (ảnh `…07…`).
4. `BACK` → về app → hint overlay có tên dòng cần tìm + lệnh `appops` (B1 ✔ + N2 ✔, ảnh `…08…`).
5. `DPAD_DOWN`×2 → focus "BẬT LỚP PHỦ" (vàng, ảnh `…09…`) · `ENTER` khi chưa có quyền → hint "Chưa có quyền hiển thị… bấm nút cấp quyền trước" (không crash; dump `fix2b-10-s6.xml`) — **sau đó focus nhảy về nút cấp quyền** vì `refresh()` chuyển focus tới "việc tiếp theo chưa xong" (ảnh `fix2b-10-s6-toggle-enter-no-permission.png`).

## 4. Chưa chứng minh được
- **TV thật**: trên TV Sony, intent pin có phân giải sang stub hay màn thật không; overlay có danh sách chung hay trang app; `ENTER`/`BACK` bằng remote thật.
- **N1** (BadToken/Security khi thu hồi quyền giữa chừng): chỉ đọc code.
- Đường `onResume` cho nút **pin** khi intent mở được một màn thật rồi User thoát mà chưa miễn: đã chứng minh cho nút overlay, không cho pin (emulator chỉ có stub); logic dùng chung `checkPendingAfterReturn`.
- Focus sau `DPAD_DOWN` ở **touch mode** thật (TV không dùng touch) — emulator headless.
- Bản release chưa build/ lint; cỡ chữ/ mật độ trên TV thật (emulator mật độ 2.0 làm nội dung cao, phải cuộn).

## 5. Câu hỏi mở — người chỉ có remote còn có thể kẹt ở đâu?
1. **Focus tự nhảy sau khi bấm "BẬT LỚP PHỦ" thiếu quyền** (bước 5): `refresh()` luôn `requestFocus` nút "việc tiếp theo" ⇒ bấm bật, focus bị kéo về nút cấp quyền; User không cuộn hoặc đang ở giữa danh sách sẽ khó hiểu. Chưa sửa (ngoài phạm vi).
2. **Màn cấp quyền overlay là danh sách chung dài**: remote phải cuộn tới "VietSub — lớp phủ kiểm tra"; app không biết khi nào User bật xong cho tới khi quay về bằng **BACK** — nếu User thoát bằng **HOME**, hint dự phòng chỉ hiện ở lần mở app kế tiếp (và `pending` mất khi tiến trình bị giết).
3. **Hint dự phòng là lệnh `adb` chạy trên máy tính** — người chỉ có remote không làm được; hint nên nói rõ "cần máy tính; nếu không, nhờ người cài đặt" hoặc trỏ tới đường bật gỡ lỗi (Bước 6 do Leader/User). Hiện hint tuyên bố "do bạn chạy" nhưng không nói điều kiện `adb connect` đã bật.
4. **Tắt lớp phủ**: lớp phủ phủ toàn màn, `NOT_FOCUSABLE` — phím remote đi xuyên xuống app dưới, nên **chỉ** tắt được bằng cách mở lại VietSub (Home → app) rồi bấm "TẮT LỚP PHỦ", hoặc chờ 30 phút. Người chỉ có remote đang xem PS5 trên HDMI **không** có phím nào tắt nhanh (gợi ý nếu cần: tắt qua thông báo/ nút remote riêng — thuộc Phase 3).
5. **Thông báo foreground bị ẩn** (không `POST_NOTIFICATIONS`, TV không có ngăn thông báo) nên không có chỗ để User thấy/ chạm "dừng".
6. **Không có phản hồi khi phím `ENTER` bấm vào nút trong lúc overlay đang phủ** (nút nằm dưới lớp phủ; phải tắt/ chờ mới thấy màn) — liên quan mục 4.

## 6. `git status --short` (cuối lượt)

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
?? project-docs/261114-fc26-vietsub-overlay/playtest/
?? project-docs/261114-fc26-vietsub-overlay/reviews/
?? tv-overlay/
```
(+ `?? project-docs/261114-fc26-vietsub-overlay/_workers/…` của Leader.) Phần của lượt này: sửa trong `tv-overlay/` (24 file nguồn chưa theo dõi; `build/`, `.gradle/` bị ignore), `?? …/playtest/` (thêm `fix2b-*`), report này.
