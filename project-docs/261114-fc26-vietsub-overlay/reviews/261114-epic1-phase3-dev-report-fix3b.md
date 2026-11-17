DEV-FIX3B DONE
# Dev report fix3b — sửa blocking review 3b (Rev 1.4) + 7 NB

Căn cứ: [review 3b](261114-epic1-phase3-review-3b.md) · dòng "Sau review 3b — Rev 1.4" trong bảng đầu `261114-epic1-phase3.md`. Bằng chứng: `playtest/epic1-phase3/dev/fix3b/` (viết tắt `<fx>/`, tiền tố `fix3b-`). Engine: `VIETSUB_HOME` tạm, `ANTHROPIC_API_KEY=''`, proxy chết, translator `cache`/`fake` — **0 HTTP**. Engine smoke chạy từ bản build riêng `D:\Temp\engine-fix3b` (không đụng `bin/` hay tiến trình của Reviewer; lần này `bin/` không bị khoá).

## 1. Test trước / sau

| | Trước fix3b | Sau |
|---|---|---|
| engine (`dotnet test engine`) | 183 | **186 xanh**, `build --no-incremental` 0 Warning / 0 Error |
| tv-overlay (`gradlew :app:testDebugUnitTest :app:assembleDebug --rerun-tasks`) | 75 | **90 xanh**, BUILD SUCCESSFUL, 0 warning |

- ĐỎ TV: `<fx>/fix3b-01-tv-red.txt` — 90 test, **12 fail** (6 AutoFit, 1 LanFilter, 1 OverlayModel, 4 server). XANH: `<fx>/fix3b-03-tv-green.txt` (90/90). `fix3b-02-tv-green-try.txt`: lần giữa chừng (server xanh hết; 4 test AutoFit còn lại xem dưới).
- ĐỎ engine: `<fx>/fix3b-04-engine-red.txt` — 186 test, **2 fail** (`maxLines`). XANH: `<fx>/fix3b-05-engine-green.txt` (186/186).
- Để lấy ĐỎ trước khi cài, tôi thêm **stub** chữ ký (tham số `OverlayServer`, `LanFilter.allowed` = true, hằng `AutoFit.FLOOR_FACTOR`) — nên đỏ là đỏ theo hành vi, không phải lỗi biên dịch.
- 3 test AutoFit cũ được **sửa đúng theo Rev 1.4** (không nới tuỳ ý): `never_goes_below_min…` (giờ: nhãn không vừa ở sàn ⇒ 1 dòng "…"), `paragraph_that_cannot_fit…` và `fontPx_and_min_scale…` (sàn 0,85 × min thay vì min). Và test mới `paragraph_shrinks_to_min_before_adding_a_line` ban đầu **tôi viết sai số liệu** (16 từ không thể vừa 2 dòng) — sửa dữ liệu (12 từ "abcd", cần 3 dòng ở cỡ 30, 2 dòng ở cỡ ≤ 26), kỳ vọng giữ nguyên.
- Test `OverlayModelTest.duplicate_ids_…` của fix3a ("giữ cả hai") **đổi** thành "giữ item đầu + log" vì Rev 1.4 quyết định như vậy.

## 2. Blocking — nhãn bị bẻ dòng, nửa dưới bị hàng dưới che

**TV (`AutoFit.kt`)** — thứ tự cho mỗi item: (1) co cỡ từ `max(fontPx,min)` tới `minFontPx` với **số dòng gốc** (nhãn = 1; đoạn văn = `maxLines − 2`); (2) `alt` (nhóm hàng thiếu khe → co cả nhóm; thêm: nhãn không vừa kể cả ở sàn mà có `alt` vừa ⇒ dùng `alt`); (3) **thêm dòng chỉ khi tổng chiều cao ≤ chiều cao `room`** (không có `room` ⇒ box); (4) co tiếp tới sàn **0,85 × min**; (5) cuối cùng mới "…" (nhãn một dòng cũng vậy, phát `onEllipsis` → `Log.w`). Nhiều dòng được kẹp dọc trong `room` (`rectFor`). Sửa luôn lỗi cũ: bước giảm 1 px có thể nhảy qua đúng `minSize` — giờ dãy cỡ luôn kết thúc đúng ở min/sàn.

**Engine** (`Layout.cs`): nhãn một dòng gửi `maxLines = 1`; đoạn văn (gộp ≥ 2 dòng) giữ `dòng gốc + 2`.

Test (ĐỎ→XANH): `narrow_row_label_shrinks_below_min_to_floor_and_stays_on_one_line` (box 189×35, room 283×63, nhãn dài ⇒ 1 dòng, cỡ ∈ [0,85·min, min), không vượt room), `second_label_case_ban_lanh_dao_fits_one_line_in_box_width`, `label_that_cannot_fit_even_at_floor_is_ellipsized_last_and_reported`, `label_uses_alt_before_ellipsis`, `extra_lines_are_added_only_within_room_height` (đáy chữ ≤ đáy room), `legacy_label_with_extra_lines_may_wrap_only_inside_room_height`, `paragraph_shrinks_to_min_before_adding_a_line`; engine `SingleLineLabels_GetMaxLines1`, `Paragraph_KeepsOriginalLineCountPlus2`, `ItemsInFrame_CarryTheSameMaxLines`.

**Ảnh phóng to Hộp thư** — `<fx>/fix3b-30-inbox-rows-zoom.png` (cắt 2× từ `fix3b-21-cache-rvinbox-2-later.png`, emulator, `replay --send` ảnh `rv-a-inbox.png` của Reviewer): **mọi nhãn ở cả ba cột nằm một dòng trong hàng của nó** ("Đội ngũ y tế", "Ban lãnh đạo" ở hàng đang chọn và hàng thường, "Cập nhật chấn thương", "Cập nhật mục tiêu mùa giải"…); không còn dòng 2 tràn xuống/ bị hàng kế che. (Trước: `fix3a-21-cache-rvinbox-2-later.png`.) Hệ quả thấy được: nhãn cột giữa nhỏ hơn cột 1 vì cùng `group` = cỡ nhỏ nhất (đúng X4/C2); "Head Scout", "Club Secretary", "12 Nov" vẫn tiếng Anh vì cache không có bản dịch.

## 3. NB (7 mục Rev 1.4)

| NB | Làm | Test / bằng chứng |
|---|---|---|
| OFF focus mặc định | Màn OFF focus **"Về màn hình chính"** (mockup); nút "Bật lại lớp phủ" ở bên trái | smoke: `focused=[Về màn hình chính]`, LEFT ⇒ `[Bật lại lớp phủ]` (`<fx>/fix3b-23-off-left.xml`) |
| NB2 LAN-only | `LanFilter` (RFC 1918, 169.254/16, loopback, IPv6 fc00::/7 + fe80::/10); nguồn khác bị **đóng, không trả lời**, log | `LanFilterTest` ×2 (public bị loại: 8.8.8.8, 172.32.x, 100.64.x, IPv6 công cộng…), `source_address_outside_lan_is_dropped_without_any_reply`, `lan_source_is_accepted` |
| NB3 hello/pending | hello **hạn tổng 5 s** (watchdog đóng cưỡng bức, kể cả nhỏ giọt 1 byte/100 ms); **≤ 4** kết nối chưa xác thực, kết nối thứ 5 đóng ngay | `hello_has_a_total_deadline_even_when_dribbled_one_byte_at_a_time` (hạn 400 ms ⇒ đóng < 2,5 s), `at_most_maxPending_unauthenticated_connections` (giải phóng chỗ ⇒ hello hợp lệ lại nhận) |
| NB5 nửa mở | sau hello: không nhận gói parse được nào trong **2 × `ttlMs`** ⇒ `SocketTimeout` ⇒ đóng ⇒ `onConnectionChanged(false)` ⇒ "Mất kết nối" | `half_open_connection_is_closed_after_2x_ttl_and_reports_lost`, `heartbeats_within_2x_ttl_keep_the_connection` |
| NB6 mã sinh một lần | "Tạo mã mới": qua `OverlayService.repair()` (tự sinh) hoặc sinh trực tiếp nếu service chưa chạy — không còn hai lần | đọc code (khác mã sau mỗi lần bấm: smoke "new code=XWA7-3RKF changed=True") |
| NB7 ẩn lớp phủ khi app tiền cảnh | `AppState.appInForeground`; `OverlayService.setAppForeground` (onResume/onPause); view `GONE` khi app hiện | `<fx>/fix3b-24-screen-connected.png`: `pattern --send` đang chạy mà màn "Đã kết nối" **sạch**, không có lưới đè (trước: `3b-08k` của Reviewer) |
| Màn Chờ sau đổi mã | dòng vàng "Mã đã đổi — trên máy tính chạy lại engine pair với mã mới." tới khi engine nối lại (`AppState.codeChanged`) | `<fx>/fix3b-25-screen-after-repair.png` |
| NB4 id trùng | TV giữ item đầu, bỏ item sau, log (`OverlayModel(onWarn)` → `Log.w`) | `duplicate_ids_in_one_frame_keep_the_first_and_log` |

## 4. Smoke emulator-5556 (Blackbox 4, 5, 7, 8, 9) — `<fx>/smoke-fix3b-tv.ps1`, log `<fx>/fix3b-20-tv-smoke.txt`

- **4**: cài APK mới, cấp overlay, màn Chờ mã `UD4R-NTW7` (focus "Hướng dẫn cho người cài ▸"); `pair` mã sai ⇒ exit 3 `token`; mã đúng (thường, không `-`) ⇒ OK. Nguồn của `adb forward` (loopback/10.0.2.x) qua bộ lọc LAN bình thường.
- **5**: `replay --send` fake lên `synth-inbox-list` và cache lên `rv-a-inbox` (2 hàng "Ban lãnh đạo" cùng hiện đúng màu); chữ Việt che kín chữ Anh, engine thoát ⇒ lớp chữ xoá (`…-3-after-engine-closed.png`). Ảnh phóng to ở mục 2.
- **7**: pattern 4 góc (`fix3b-22-pattern-corners.png`).
- **8** (phím thật): BACK về `MainActivity` khi lớp phủ bật ⇒ OFF, focus "Về màn hình chính"; LEFT ⇒ "Bật lại lớp phủ"; OK ⇒ lớp phủ bật, sang màn Chờ; engine nối ⇒ "● Đã kết nối" (focus "Tắt lớp phủ"), **lớp phủ ẩn dù pattern đang gửi**; RIGHT ⇒ "Ghép cặp lại", 3 s sau vẫn đó; OK ⇒ mã `XWA7-3RKF` + dòng "Mã đã đổi…", hello mã cũ ⇒ `token`.
- **9**: `v=2` ⇒ `version`; sai mã ⇒ `token`; dòng rác ⇒ đóng không trả lời; hello đúng + 300 KB ⇒ TV đóng; hello đúng ngay sau ⇒ `ok`; `logcat -b crash` 0 dòng; service còn chạy (1).
- Lần chạy đầu của script hỏng vì focus mặc định màn OFF đã đổi (OK bấm trúng "Về màn hình chính" ⇒ về Home, các bước sau sai); tôi thêm phím LEFT rồi chạy lại toàn bộ — log cuối cùng là lần chạy lại.
- **Dọn dẹp**: `adb -s emulator-5556 emu kill`, `forward --remove`, `gradlew --stop`; sau đó không còn tiến trình emulator/qemu nào, `adb devices` **rỗng** (PID của tôi ở `<fx>/fix3b-emulator-pids.txt`). Không đụng tiến trình nào của Reviewer.

## 5. Chưa chứng minh được / lưu ý

- Chỉ emulator (mật độ 2.0, API 31): chưa TV Sony thật, chưa chữ có độ rộng/ hộp OCR thật của FC 26 (cỡ sàn 0,85 × min có đọc được từ sofa hay không là việc của Bước 8).
- LAN-only chỉ kiểm bằng `LanFilter` + nguồn loopback; chưa thử nguồn công cộng thật. IPv4-mapped IPv6 chưa thử.
- NB5 phụ thuộc engine gửi nhịp tim ≤ mỗi `ttlMs` (đang 1 s với ttl 3 s) — nếu nâng `HeartbeatMs` > 2 × ttl thì TV tự đóng kết nối.
- Hành vi mới đáng báo: nhãn dài ở hàng hẹp giờ **nhỏ hơn** (cùng nhóm = cỡ nhỏ nhất) thay vì bẻ dòng; nếu quá nhỏ thì chỉnh `display.minFontPx`/`fontScale` tối nay.
- Không động tới phase doc / CLAUDE.md.

## 6. `git status --short` (cuối fix3b, 77 dòng)

Thuộc fix3b:
```
 M engine/README.md  tv-overlay/README.md
?? engine/src/Engine/Layout/            (Layout.cs: maxLines)
?? engine/tests/Engine.Tests/Fix3bTests.cs
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/{AutoFit,OverlayModel,OverlayServer,AppState}.kt
 M tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/{MainActivity,OverlayService,OverlayView}.kt
 M tv-overlay/app/src/main/res/values/strings.xml
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/{AutoFitTest,OverlayModelTest}.kt   (sửa)
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/Fix3bServerTest.kt                  (mới)
?? project-docs/261114-fc26-vietsub-overlay/reviews/261114-epic1-phase3-dev-report-fix3b.md
?? project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/dev/fix3b/          (bằng chứng)
```
Phần còn lại: các lượt trước (3a, 3b, fix3a, review, SA, Leader). Không chạy lệnh git ghi nào.
