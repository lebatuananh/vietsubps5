# automation/ — giá trị cụ thể cho test của dự án

Phương pháp chung (server local, chọn môi trường, dựng tài khoản): [`workflow/guidelines/automation.md`](../../workflow/guidelines/automation.md) — **đọc trước**. Test qua thiết bị Android/TV: [`workflow/guidelines/blackbox-android-adb.md`](../../workflow/guidelines/blackbox-android-adb.md).

| File | Chứa gì |
|---|---|
| [test-accounts.md](./test-accounts.md) | Tài khoản test — dự án này chưa có (xem file) |
| `scripts/` | Script tái dùng (xem README trong thư mục) |

## Thiết bị & mạng LAN

Dự án không có DB hay server. Môi trường test là **thiết bị thật trong LAN nhà User** + emulator.

| | |
|---|---|
| **Máy tính Windows (máy dev)** | Windows 11 · IP Wi-Fi LAN `192.168.1.30` (kiểm 2026-11-14 — có thể đổi, kiểm lại bằng `Get-NetIPAddress`). Cùng mạng `192.168.1.x` với TV và PS5 |
| **TV** | Sony 65" Google TV · IP: `<chưa ghi>` · gỡ lỗi qua mạng (`adb connect <ip>:5555`): `<chưa bật>` — **User bật** (Risk gate riêng). Bật xong ghi IP + ngày vào đây |
| **PS5** | IP: `<chưa ghi>` · Remote Play: `<chưa bật>` — **User bật** trong Cài đặt PS5 |
| **Cài đặt hình ảnh TV cho lớp phủ khớp toạ độ** | Tắt overscan / chọn chế độ hiển thị toàn khung (tên mục theo Sony — ghi lại khi chỉnh lần đầu) |
| **Emulator** | AVD đã có: `vietsub_dev` (vai Dev, cổng 5556) · `vietsub_rv` (vai Reviewer, cổng 5558) — cùng `system-images;android-31;google-tv;x86`, device `tv_1080p` (Android 12, 1920×1080, mật độ 2.0). Bật **tách tiến trình**: ghi lệnh `emulator.exe -avd <tên> -port <cổng> -no-window … > log` vào một file `.cmd` ngoài repo rồi `Start-Process cmd /c <file.cmd>` (gọi thẳng `Start-Process cmd /c "…"` bị nuốt ngoặc, không bật được); tắt bằng `taskkill /T /F /PID <pid launcher>`. TvSettings của image này: intent miễn pin là **stub tự đóng**. Image gốc: Image đã cài: `system-images;android-31;google-tv;x86` (API 31 không có bản x86_64). Tăng tốc: `emulator -accel-check` ⇒ WHPX usable (2026-11-15) |
| **Công cụ dev đã cài (2026-11-15)** | .NET SDK `10.0.404` (`C:\Program Files\dotnet`) · Android Studio `2026.2.2.8` — JBR `C:\Program Files\Android\Android Studio\jbr` (openjdk 25.0.4) · Android SDK `%LOCALAPPDATA%\Android\Sdk`: cmdline-tools latest, platform-tools (adb 1.0.41), emulator, `platforms;android-35`, `build-tools;35.0.0` + `build-tools;36.0.0` (AGP 9 tự cài ở build đầu), Gradle 9.1.0 qua wrapper của `tv-overlay/`. **Không** có trong PATH máy — đặt trong tiến trình shell: `JAVA_HOME` = JBR, `ANDROID_HOME` = SDK, thêm `%ANDROID_HOME%\platform-tools` + `%ANDROID_HOME%\emulator` vào PATH. 🔴 Bẫy: gọi `sdkmanager.bat` từ PowerShell với tên gói có `;` ⇒ bị tách tham số ("Package platforms not found") — gọi qua `cmd /c` và đặt tên gói trong ngoặc kép |

Giá trị local (IP, cổng LAN) ghi thẳng. 🔴 Khoá API dịch/LLM và mọi thông tin tài khoản PSN **không bao giờ** ghi ở đây.
