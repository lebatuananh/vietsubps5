# Blackbox Android qua `adb` — sổ tay thao tác & bẫy

> Quy trình và lệnh để Dev/Reviewer/End User blackbox một app Android (thật hoặc máy ảo) qua `adb`: cài APK, điều khiển UI, seed/đọc state, bắt traffic, dọn dẹp. **Trung lập dự án** — mọi tên cụ thể (package, serial, đường dẫn SDK, host) là **placeholder**, thay bằng giá trị thật của dự án (thường ở `README.md` của module mobile và `project-docs/automation/test-accounts.md`).
>
> File chung blackbox (web/API + Android): [blackbox-testing.md](./blackbox-testing.md). File này đào sâu riêng phần Android.

Ký hiệu placeholder dùng xuyên suốt: `<pkg>` package app · `<activity>` main activity · `<serial>` serial thiết bị (serial như `emulator-5556` hoặc serial máy thật) · `<apk>` đường dẫn APK · `<sdk>` thư mục Android SDK · `<host>` host emulator dùng để gọi máy chủ.

---

## 0. 🔴 Năm luật nền

1. **Luôn `-s <serial>`** trong mọi lệnh `adb`. Máy có thể có nhiều thiết bị/emulator; thiếu `-s` là gõ nhầm máy. **Không** `adb kill-server`, **không** `-wipe-data` nếu máy đang có phiên khác (vd User hoặc tester đang dùng).
2. **Thiết bị/emulator của người khác là bất khả xâm phạm.** Chỉ thao tác thiết bị của chính mình; thiết bị/AVD đang được User hoặc vai khác dùng thì không đụng.
3. **Toạ độ UI lấy từ `uiautomator dump`, không ước từ ảnh chụp.** Xem §3.
4. **`adb` thường KHÔNG trong PATH.** Gọi bằng đường dẫn đầy đủ `"<sdk>/platform-tools/adb.exe"`, hoặc export PATH đầu mỗi lệnh.
5. **Verify bằng state thật của thiết bị/DB/log, không bằng việc lệnh `adb` exit 0.** `adb shell` trả 0 cả khi lệnh bên trong fail.

---

## 1. Môi trường & công cụ

- Tìm `adb` + `emulator` trong SDK: thường `<sdk>/platform-tools/adb.exe` và `<sdk>/emulator/emulator.exe`. SDK và AVD có thể nằm ngoài ổ mặc định ⇒ đọc đường dẫn thật ở README dự án, đừng đoán `%LOCALAPPDATA%\Android\Sdk`.
- Liệt kê thiết bị: `adb devices` · AVD có sẵn: `emulator -list-avds`.
- 🔴 **Emulator gọi máy chủ trên host qua địa chỉ đặc biệt, KHÔNG phải `localhost`.** Với AVD Google mặc định là `10.0.2.2` (map tới `127.0.0.1` của host). Build debug trỏ backend local phải dùng địa chỉ này.

---

## 2. Vòng đời thiết bị / emulator

### 2.1 Tạo & chạy AVD
- 🔴 **Bản release (Flutter) KHÔNG chạy trên emulator x86** — Flutter chỉ hỗ trợ debug trên x86. Để blackbox **bản release** phải dùng **AVD x86_64** (image `system-images;android-<API>;google_apis;x86_64`). Image `google_apis` (non-Play, userdebug) cho phép `adb root` để seed prefs; image `google_apis_playstore` thì không.
- Chạy headless (cho worker chạy nền):
  ```
  emulator -avd <AVD_NAME> -port 5556 -no-window -gpu swiftshader_indirect -no-audio -no-boot-anim -no-snapshot
  ```
  Serial khi đó là `emulator-5556`. Boot xong khi `adb -s emulator-5556 shell getprop sys.boot_completed` trả `1` (chờ ~40 s).
- 🔴 **Bật emulator trong shell của worker phải tách tiến trình** (xem §7) — chạy trực tiếp sẽ treo tool call.

### 2.2 Cài APK & mở app
```
adb -s <serial> install -r <apk>                      # -r giữ dữ liệu; Success = OK
adb -s <serial> shell dumpsys package <pkg> | grep -E "versionName|versionCode|minSdk|targetSdk"
adb -s <serial> shell am force-stop <pkg>
adb -s <serial> shell am start -n <pkg>/<activity>    # cold start
# hoặc mở qua launcher:
adb -s <serial> shell monkey -p <pkg> -c android.intent.category.LAUNCHER 1
```
- `install -r` **giữ** dữ liệu app (không đăng ký lại). Muốn trạng thái sạch: `adb -s <serial> shell pm clear <pkg>` trước — **nhưng** xem bẫy §8 (pm clear không đổi AndroidId).

### 2.3 🔴 Dừng emulator — đừng tin `adb emu kill`
- `adb -s <serial> emu kill` in `OK: killing emulator, bye bye` nhưng **không chắc tắt** tiến trình chạy `-no-window` qua `nohup`.
- Dọn đúng: `Stop-Process` đúng cây tiến trình (bash/nohup/`emulator`/`qemu`) rồi kiểm `adb devices`.
- 🔴 **Kiểm lại sau 10–15 phút** — emulator có thể **tự bật lại** nếu một session worker cũ còn sống và chạy lại lệnh `nohup emulator -avd …`.

---

## 3. Blackbox UI: dump bounds → tap/type

🔴 **Không bao giờ ước toạ độ từ ảnh chụp.** Layout đổi theo banner quảng cáo / bàn phím / popup; toạ độ cũ trỏ sai chỗ ⇒ báo nhầm "app không nhận chạm".

```
adb -s <serial> exec-out uiautomator dump /dev/tty       # hoặc dump ra file rồi pull
adb -s <serial> shell input tap <x> <y>                  # x,y = TÂM của bounds [x1,y1][x2,y2]
adb -s <serial> shell input text "noi_dung"              # gõ text (tránh ký tự đặc biệt/space phức tạp)
adb -s <serial> shell input keyevent 4                   # BACK
```
- Tâm của node bounds `[x1,y1][x2,y2]` = `((x1+x2)/2, (y1+y2)/2)`.
- **Dump lại sau MỖI thay đổi layout** (banner hiện, bàn phím mở, popup). "Chạm không ăn" ⇒ dump trước khi kết luận bug.
- 🔴 **Đóng quảng cáo bằng BACK, không tap nút X ước toạ độ** — tap sai có thể **lỡ click vào quảng cáo thật** (mở landing page). Endcard WebView của quảng cáo thường **không dump được** bằng uiautomator ⇒ cũng đóng bằng BACK.

---

## 4. Seed & đọc state của app (cần `adb root`, image userdebug)

> Để dựng checkpoint test (vd đã đạt mục tiêu, đã chat n lượt) mà không phải chơi lại từ đầu. `adb root` chỉ được trên image userdebug / non-Play.

```
adb -s <serial> root                                      # chỉ chạy được trên userdebug build
adb -s <serial> shell run-as <pkg> cat 'files/<path>'     # đọc file trong sandbox app không cần root
adb -s <serial> pull /data/data/<pkg>/databases/<db>.sqlite  ./local-copy.sqlite
```
- **Flutter `shared_preferences`** lưu ở `/data/data/<pkg>/shared_prefs/FlutterSharedPreferences.xml`, mỗi khoá có tiền tố `flutter.`. Node đúng kiểu: `putLong` ⇒ `<long name="flutter.<key>" value="…"/>`, `putString` ⇒ `<string>`. Sửa khi app **đã force-stop**, giữ nguyên owner/mode của file, rồi mở lại app.
- 🔴 **Đọc log của bản release**: app release thường **không** in ra logcat mà ghi file trong sandbox (`files/logs/…`). Đọc bằng `run-as <pkg> cat files/logs/<file>`. **Log có thể chứa token/JWT/PII ⇒ che trước khi lưu vào evidence.**

---

## 5. Bắt & kiểm traffic (app VPN, vd PCAPdroid)

```
# bắt (ví dụ PCAPdroid CaptureCtrl):
adb -s <serial> shell am start -e action start -e pcap_dump_mode pcap_file \
  -e pcap_name <ten>.pcap -e app_filter <pkg> -n com.emanuelef.remote_capture/.activities.CaptureCtrl
```
- 🔴 **Dừng capture xong PHẢI tắt hẳn VPN**: lệnh `CaptureCtrl action stop` **không đủ** — interface `tun0` còn thì app mất mạng (trông như lỗi backend). Kiểm & dọn:
  ```
  adb -s <serial> shell ip addr | grep tun0          # còn dòng tun0 = VPN chưa tắt
  adb -s <serial> shell am force-stop com.emanuelef.remote_capture
  ```
- Dùng traffic để verify app chỉ gọi đúng host dự án cho phép, và **vắng mặt** các SDK/host không được dùng. Danh sách host whitelist/blacklist là **đặc thù dự án** — để ở tài liệu dự án, không ở đây.

---

## 6. Phân biệt lỗi thật của app với nhiễu do môi trường test

Trước khi báo "bug", loại các nguyên nhân do chính cách test gây ra:

| Triệu chứng | Nghi do test, kiểm gì |
|---|---|
| App "mất mạng" / màn hình rỗng sau khi bắt traffic | `tun0` còn? (§5) — VPN capture chưa tắt |
| "Chạm không ăn", nút không phản hồi | Toạ độ ước từ ảnh? Dump lại bounds (§3) |
| Màn hình "trống/0 phần tử" ngay sau cài sạch + đăng ký | Backend tạo state bất đồng bộ sau khi trả response ⇒ force-stop + mở lại; đây thường là bẫy backend, không phải lỗi adb |
| Emulator "treo"/chậm | RAM guest cạn (`dumpsys meminfo`), hoặc bản debug nặng — không phải ANR |
| Nhiễu SDK trong **bản debug** | Bản debug có thể còn SDK bên thứ ba chưa cấu hình cho môi trường debug ⇒ lỗi init SDK là nhiễu, không liên quan backend. **Không bấm quảng cáo trên bản debug.** |

---

## 7. 🔴 Bật emulator/server trong shell worker ⇒ treo

Lệnh bật tiến trình nền (emulator, dev server) chạy trực tiếp trong tool bash của worker theo kiểu kế thừa pipe ⇒ **tool call không bao giờ trả về**. Tách tiến trình ra window riêng:
```powershell
Start-Process -FilePath cmd.exe -ArgumentList '/c','<lệnh> > logs\x.log 2>&1' -WindowStyle Hidden -PassThru
```
Không dùng `-NoNewWindow`, `&`, `Start-Job`, hay chạy lệnh server/emulator trực tiếp. Brief cho worker có lệnh loại này **phải** kèm công thức trên.

Watcher cho lượt có build/emulator: hạ `idleSec` xuống ~600 s (một lệnh đứng >10 phút ~ chắc kẹt), thay vì mặc định 1500 ([delegating-workers §5](../practices/delegating-workers.md)).

---

## 8. Bẫy đã gặp

- **`pm clear` KHÔNG đổi AndroidId.** App định danh thiết bị bằng AndroidId ⇒ sau `pm clear` + đăng ký lại ra **đúng** thiết bị/tài khoản cũ. Muốn tài khoản mới thật phải đổi AndroidId (hoặc dùng AVD khác).
- **Build khác ổ đĩa với pub cache ⇒ Kotlin incremental lỗi** (`this and base files have different roots`). Fix: `kotlin.incremental=false` trong `android/gradle.properties`.
- **Đổi nhánh git khi IDE mở ⇒ `pubspec.lock` bị extension Dart tự sửa.** Sau `git checkout` chạy `git status --short`, revert `pubspec.lock` nếu đợt không đụng app.
- **Android 9 (một số máy tính bảng đời cũ): không có `input motionevent`, và `input swipe` có thể không kéo được `PageView`/carousel của Flutter** (nhãn trang không đổi dù đã thử nhiều duration). Bắn sự kiện cảm ứng thô bằng `sendevent` vào thiết bị cảm ứng thật: `getevent -pl` ⇒ chọn thiết bị có `INPUT_PROP_DIRECT`, đọc `max` của `ABS_MT_POSITION_X/Y` (toạ độ px); gửi MT protocol B (`EV_ABS` 3: `ABS_MT_SLOT` 47, `ABS_MT_TRACKING_ID` 57, `ABS_MT_POSITION_X/Y` 53/54, `ABS_MT_PRESSURE` 58; `EV_KEY` 1 `BTN_TOUCH` 330; `EV_SYN` 0) với ~10 bước di chuyển, gói trong một script đẩy lên `/data/local/tmp` rồi `sh` một lần (shell ADB thường thuộc nhóm `input`, không cần root).
- **`cd` bị khoá trong một số shell worker** ⇒ dùng `env -C <dir> <lệnh>` thay `cd`.

---

## Phần đặc thù dự án — KHÔNG ghi ở đây

Giá trị cụ thể của dự án (package, activity, serial/AVD, đường dẫn SDK, host whitelist, path prefs/db, tài khoản test) để ở tài liệu dự án:
- Toolchain, lệnh build, đường dẫn SDK/adb, path log/prefs/db ⇒ `README.md` của module mobile.
- Thiết bị thật, AVD, AndroidId, tài khoản test ⇒ `project-docs/automation/test-accounts.md`.
