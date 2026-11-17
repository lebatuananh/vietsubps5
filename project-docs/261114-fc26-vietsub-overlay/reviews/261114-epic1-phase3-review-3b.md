REVIEW-RE3B DONE
# 261114 — Epic 1 Phase 3 — Review lượt 3b (`tv-overlay/` Bước 5–6)
Verdict: PASS (re-review fix 3b — 0 blocking; review 3b ban đầu: FAIL 1 blocking)

Reviewer: session độc lập. Chấm theo Phase doc Rev 1.2 + README *Giao thức*. Rev 1.3 (B1/B4 từ review 3a) chỉ ghi trạng thái. Đối chiếu [dev report 3b](261114-epic1-phase3-dev-report-3b.md).

- **Evidence**: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/reviewer/3b-*` (viết tắt `<rv>/`).
- **Emulator**: của riêng Reviewer `emulator-5558` (AVD `vietsub_rv`), bật tách tiến trình. Launcher PID 8580 → `emulator.exe` 22524 → `qemu` 24344. Đã dừng bằng `taskkill /T /F /PID 8580`.
- **Forward**: chỉ dùng `tcp:47830 → 47820` của mình (đã gỡ), **không** đụng forward 47820 của `emulator-5556` (Dev). Cuối lượt `adb devices` rỗng (`<rv>/3b-10-emulator-stop.txt`).
- **Engine**: `VIETSUB_HOME` tạm, `ANTHROPIC_API_KEY=''`, proxy chết, translator `fake`. **0 HTTP**.
- Không `gradlew --stop`, vì daemon có thể đang dùng chung với Dev.
- **Lưu ý**: Dev đang sửa `engine/` song song. Engine em chạy là bản build của cây làm việc lúc review, đã có một phần vòng sửa 3a (gộp đoạn email, `id` không trùng).

## Blocking

1. **B1 — Nhãn một dòng bị xuống dòng tràn ra ngoài hàng; dòng thứ hai nằm ngoài vùng che và bị vùng che của hàng kế bên vẽ đè** — `AutoFit.kt:89-97` (`individualSize`), `:115-128` (`tryLayout`), `CoverRegion.compute` `:201`.
   - **Code làm gì**: `individualSize` đi từ cỡ lớn xuống, nhận **bố cục đầu tiên có số dòng ≤ `maxLines`**. Nghĩa là **xuống dòng trước, co chữ sau**. `tryLayout` không kiểm **chiều cao** bố cục so với `room`/box. Vùng che lại bị **cắt trong `room`** ⇒ dòng thứ hai rơi ra ngoài vùng che.
   - **Evidence**: ảnh `<rv>/3b-05-inbox-overlay.png`, phóng to ở `<rv>/3b-06-c1-zoom.png` (trái = phông gốc, phải = lớp phủ):
     - "Cập nhật / chấn thương", "Báo cáo / chấn thương", "Ban lãnh / đạo" (hàng **đang chọn**) đều bị bẻ 2 dòng.
     - Dòng 2 tràn xuống khe/hàng dưới; "đạo" và "chấn thương" **bị vùng che của hàng kế bên cắt mất nửa dưới**.
     - Trong khi "Cập nhật mục tiêu mùa giải" (dài hơn) lại vừa **một** dòng ở cùng cột.
   - **Đối chiếu**: D11 (ràng buộc) ghi "… **giảm cỡ tới cỡ tối thiểu rồi tăng dòng** tới `maxLines`". C1: vùng che phải ⊇ bbox chữ Việt.
   - **Góp phần từ engine**: mọi item, kể cả nhãn một dòng, được gửi `maxLines = số dòng gốc + 2 = 3` (`Layout.cs:130`; `<rv>/3b-06-c1-check.txt`: mọi item `maxLines=3`). TV không phân biệt được nhãn với đoạn văn.
   - **Vì sao chặn**: đây là màn Hộp thư — màn được kiểm chất lượng. Chữ Việt bị cắt nửa và đè lên hàng khác là lỗi nhìn thấy ngay. Trái C1/D11.
   - **Sửa**:
     - (a) Trong `individualSize`, thử **đủ số dòng gốc** (1 với nhãn) ở mọi cỡ xuống tới `minSize` trước; chỉ khi vẫn không vừa mới cho thêm dòng.
     - (b) Loại bố cục có `lines × lineHeight` > chiều cao `room` (hoặc box khi không có `room`).
     - (c) Engine: nhãn một dòng gửi `maxLines = 1` (hoặc giao thức thêm cờ đoạn văn) — Leader quyết.
     - Test: box 189×35 px, `room` 283×63 px, "Cập nhật chấn thương" ⇒ 1 dòng; không item nào vượt đáy `room`.
   - Đã kiểm C1 theo nghĩa "**chữ gốc ló ra**": **không thấy**. Mọi chữ Anh dưới item được vẽ đều bị che kín ("Inbox", "Board", "Injury update", "Contract Update", đoạn email — `<rv>/3b-06-c1-zoom.png`). Vệt 2 px Dev nêu ở dev report §4c không tái hiện trên ảnh của em (engine đã có `boxPadPx: 3` trong cấu hình mặc định).

## Trạng thái B1/B4 của Rev 1.3

- **B4** (TV bắt đầu từ `max(fontPx, minFontPx)`): **đã có** ở TV — `AutoFit.kt:54` `max(inputs[i].item.fontPx * scale, minSize)`.
- **B1** (`id` duy nhất trong gói): phía engine **có vẻ đã sửa** trong cây đang làm (ảnh `rv-a-inbox` của review 3a: `dup ids {}`, hai hàng "Ban lãnh đạo" đều hiện) — chờ review vòng sửa.
- Phía TV **không phòng thủ**: `OverlayModel` dùng `LinkedHashMap` theo `id` (`OverlayModel.kt:12,45-79`) ⇒ `id` trùng thì chỉ còn item sau cùng (NB4).

## Ưu tiên 1 — Giao thức + độ bền `OverlayServer`

| Hạng mục | Kết quả | Chỗ / evidence |
|---|---|---|
| hello: `v` ≠ 1 / sai mã / dòng đầu rác / thiếu `token` | `{"ok":false,"reason":"version"}` / `{"ok":false,"reason":"token"}` / đóng không trả lời / đóng không trả lời — khớp README §1–2 | `Protocol.kt:64-80`, `OverlayServer.kt:70-76`; `<rv>/3b-03-smokeA.txt` |
| Mã: không phân biệt hoa/thường, bỏ `-` | `engine pair` mã thật viết thường không `-` ⇒ OK; mã đúng bảng chữ nhưng sai ⇒ exit 3 `token`; sai bảng chữ ⇒ exit 1 | `<rv>/3b-03-smokeA.txt` |
| Dòng > 256 KB | Đóng kết nối, log `dòng dài hơn 262144 byte`; hello mới ngay sau vẫn `ok` | `Protocol.kt:170-183`; `<rv>/3b-03-smokeA.txt` |
| Gói sai (màu `red`, `text` 1001 ký tự, 201 item, `ttlMs:0`, `v:2`, không phải JSON, thiếu trường) | Bỏ **từng gói**, **giữ** kết nối (README §4 "Giới hạn") | `Protocol.kt:83-122`; `after 7 malformed frames, connection open` |
| `seq` | seq 10 rồi seq 5 ⇒ chỉ thấy "SEQ MƯỜI"; `sessionId` mới seq 1 ⇒ nhận, item cũ biến mất | `SeqGate` `Protocol.kt:153-167`; ảnh `<rv>/3b-09a`, `3b-09b`, ghép ở `<rv>/3b-09-contact.png` |
| `ttlMs` | ttl 2000: còn ở +1 s, mất ở +3 s | `OverlayModel.kt:37-43`; `<rv>/3b-09c/d` |
| `holdMs` (TV áp) | hold 2000 sau `items:[]`: còn ở +1 s, mất ở +3 s | `OverlayModel.kt:76-78`; `<rv>/3b-09e/f` |
| Một kết nối hoạt động | Kết nối hợp lệ mới đóng cái cũ; cái cũ thoát êm, không bắn `onConnectionChanged(false)` sai | `OverlayServer.kt:77-83,95-98` |
| Luồng | `onFrame` chạy ở luồng socket, `main.post` trước khi chạm `model`/`view` ⇒ model chỉ bị đụng ở luồng UI | `OverlayService.kt:62-67` |
| Rò socket | Mọi đường thoát `handle` đều `finally { s.close() }`; `stop()` đóng server + kết nối hiện tại; `onDestroy` gọi `stop()` | `OverlayServer.kt:94-99,44-48`; `OverlayService.kt:146-148` |
| Crash | `logcat -b crash` = 0 dòng sau toàn bộ smoke (hostile, seq, ttl, replay, pattern, phím) | `<rv>/3b-03-smokeA.txt`, `<rv>/3b-05-smokeC-step8.txt` |
| FGS khi app ra nền | HOME ⇒ lớp phủ (`pattern`) vẫn vẽ trên launcher, service còn | `<rv>/3b-08l-home-overlay-on.png` |
| Chuyển sang HDMI | Không kiểm được trên emulator. `START_NOT_STICKY` ⇒ TV giết tiến trình thì không ai bật lại cho tới khi mở app | `OverlayService.kt:56` |

## Ưu tiên 2 — Chất lượng vẽ

| Hạng mục | Kết quả |
|---|---|
| C1 — chữ gốc ló ra | Không thấy (xem B1, `<rv>/3b-06-c1-zoom.png`). **Chữ Việt tràn khỏi vùng che** ⇒ B1 |
| `card` đục 100% | `CoverRegion.opaque` ép alpha `0xFF` (`AutoFit.kt:210`, `OverlayView.kt:88`); smoke của em chỉ ra `solid` (nền đồng màu) ⇒ `card` mới kiểm bằng unit test |
| `room` | Nới bề rộng trong `room` có (`availWidth` `AutoFit.kt:105-113`), nhưng chiều cao không kiểm (B1) |
| Cùng `group` cùng cỡ | Có — cột 2 cùng cỡ trên ảnh; `AutoFit.kt:57-77` |
| `alt` | Chỉ cho nhóm `row-` thiếu khe < 2% (`AutoFit.kt:60-75`). Ảnh của em không có nhóm `row-` ⇒ chưa kiểm E2E |
| Be Vietnam Pro | `OverlayView.kt:30-31`; dấu chồng đúng trên ảnh ("Cập nhật", "chấn thương", "Hôm nay") |
| Không nhấp nháy khi `id` không đổi | Cùng `id` + nội dung ⇒ không đổi `version` (`OverlayModel.kt:45-79`); `id` mới mới fade-in |
| Pattern 4 góc | TL/TR/BL/BR sát góc 1920×1080 (`<rv>/3b-07-pattern.png`) |
| Engine thoát ⇒ xoá | Ảnh +3,5 s sau khi engine thoát: phông gốc sạch (`<rv>/3b-05-inbox-after-engine-closed-3.5s.png`) |

## Ưu tiên 3 — Màn sản phẩm (D13 + mockup), **chỉ phím**

| Luồng | Kết quả |
|---|---|
| Cần cấp quyền | Focus "Mở cài đặt" (vàng); chip "CHƯA BẬT"; bước pin ghi "khuyên, không bắt buộc" (`<rv>/3b-04a-permission.*`) |
| Có quyền ⇒ tự sang **Chờ máy tính** | Mã `XXXX-XXXX` cỡ lớn + "Địa chỉ TV: …:47820"; focus "Hướng dẫn cho người cài ▸"; khối hướng dẫn **gập mặc định**, OK mở/gập (`<rv>/3b-04b/c/d`) |
| Đã kết nối | "● Đã kết nối", focus "Tắt lớp phủ"; RIGHT ⇒ "Ghép cặp lại", **sau 4 s vẫn ở đó** (`<rv>/3b-08q`, `3b-08r`) |
| Mở app khi lớp phủ đang bật | Màn "Đã tắt lớp phủ — OK để bật lại", lớp phủ tắt thật (`<rv>/3b-08m`) |
| **OK hai lần** | OFF (focus "Bật lại lớp phủ") ⇒ OK ⇒ CONNECTED (focus "Tắt lớp phủ") ⇒ OK ⇒ OFF ⇒ OK ⇒ CONNECTED (`<rv>/3b-08n/o/p`) — bật/tắt qua lại. Mockup chủ ý tránh đúng điều này (NB1) |
| BACK ở màn OFF | Lớp phủ **vẫn tắt** sau khi về (`<rv>/3b-08b` → `3b-08c`) — đúng X5-3, nhưng là bẫy cho người chỉ có remote (câu hỏi mở) |
| Ghép cặp lại | Mã đổi (`WV…` → `7F9W-HRT7`); hello với mã cũ ⇒ `{"ok":false,"reason":"token"}`; engine đang nối log `[tv] TV từ chối: token — dừng kết nối` (`<rv>/3b-05-smokeC-step8.txt`) |
| Debug-only vắng ở release | `assembleRelease`: manifest hợp nhất chỉ còn `MainActivity`, `OverlayService`; dex release **không** có chuỗi `DiagActivity`/`BackdropActivity`/`DiagOverlayService`; `BuildConfig.DEBUG = false` (`<rv>/3b-02-release-manifest.txt`) |

## Ưu tiên 4 — An ninh LAN

- Không có đường gọi ra ngoài: grep `URL(`/`HttpURLConnection`/`Socket(`/`connect(` trong `src/main` chỉ thấy `ServerSocket` (`OverlayServer.kt:36,38`). `INTERNET` chỉ để nghe (manifest có chú thích N6).
- **Mã ghép cặp không bị log**: grep `Log.` trong `src/main` không thấy mã; reply chỉ có `ok`/`reason`; log server chỉ ghi lý do (`từ chối hello: token`).
- **NB2**: `ss.bind(InetSocketAddress(port))` nghe **mọi** giao diện (`OverlayServer.kt:38`), không lọc địa chỉ nguồn (site-local/loopback). Trên TV trong mạng nhà (sau NAT) chấp nhận được; nếu TV có VPN/IPv6 công khai thì cổng lộ ra ngoài LAN — trái tinh thần CLAUDE.md "chỉ nghe trong LAN".
- **NB3**: mỗi kết nối chưa xác thực được một luồng, timeout 5 s **mỗi lần đọc** (`OverlayServer.kt:59,67`). Em mở 30 socket không gửi gì: hello hợp lệ vẫn được nhận. Nhưng gửi nhỏ giọt 1 byte/4 s thì giữ luồng vô hạn (DoS từ trong LAN).

## Test · build

- `gradlew :app:testDebugUnitTest :app:assembleDebug :app:assembleRelease --rerun-tasks` ⇒ `BUILD SUCCESSFUL`; **72 test, 0 fail/error**; không có `w:`/`warning:` từ compiler (chỉ dòng môi trường "SDK XML version 4") — `<rv>/3b-01-gradle.txt`.
- Bằng chứng RED→GREEN của Dev có (`dev/3b-01-tdd-red.txt` 64 fail `NotImplementedError`; `3b-04` 1 fail → `3b-05` xanh) — không chạy lại.

## Non-blocking

| # | Chỗ | Vấn đề | Đề xuất |
|---|---|---|---|
| NB1 | `MainActivity.kt:279-290` (màn OFF), `:257-267` (CONNECTED) | Focus mặc định màn OFF là "Bật lại lớp phủ", trong khi mockup (`off:default`) ghi rõ "Focus mặc định 'Về màn hình chính' để lỡ bấm OK lần nữa không bật lại". "Tắt lớp phủ" ở CONNECTED chuyển sang màn OFF thay vì đổi nhãn tại chỗ (`connected:default`). Remote Sony hay lặp phím ⇒ bật/tắt qua lại (đã tái hiện) | Theo mockup: OFF focus "Về màn hình chính"; nút Tắt/Bật đổi nhãn tại chỗ, focus giữ nguyên |
| NB2 | `OverlayServer.kt:38` | Nghe mọi giao diện, không lọc nguồn | Từ chối kết nối có địa chỉ không site-local/loopback |
| NB3 | `OverlayServer.kt:59,67` | Không giới hạn số kết nối chưa xác thực / thời hạn hello tổng | Hạn hello tổng 5 s (đóng cưỡng bức) + tối đa ~4 kết nối chờ |
| NB4 | `OverlayModel.kt:45-79` | `id` trùng trong gói ⇒ item trước bị thay âm thầm | Log cảnh báo / thêm hậu tố — phòng thủ dù engine đã sửa |
| NB5 | `OverlayServer.kt:82` (`soTimeout = 0`) | Engine mất mạng/ngủ không gửi FIN ⇒ kết nối nửa mở, app vẫn "● Đã kết nối" mãi (lớp chữ tự xoá theo ttl, nhưng màn trạng thái sai) | `soTimeout` ≈ 3× ttl (nhịp tim 1 s) hoặc tính "đã kết nối" theo gói nhận trong ttl |
| NB6 | `MainActivity.kt:244` + `OverlayService.kt:123` | "Tạo mã mới" sinh mã **hai lần** (vô hại) | Bỏ một lần |
| NB7 | `OverlayService.kt:110-113` | Mở app lần đầu (service chưa chạy) với lớp phủ bật ⇒ chữ engine vẽ đè lên chính UI app (`<rv>/3b-08k-connected.png` thấy lưới pattern đè lên màn Đã kết nối) | Ẩn lớp phủ khi `MainActivity` ở foreground |
| NB8 | dev report §5.3 | Lệch mockup màn OFF đã khai — gộp vào NB1 | — |

Các lệch khác Dev khai (§5: bước pin không chặn, tự chuyển màn, mã 8 ký tự theo D3, Gson Apache-2.0, `app_name`, Backdrop đọc `filesDir`, nút focus phóng 1,05 bị cắt 1–2 px): **chấp nhận**.

## Chưa kiểm được

- TV Sony thật: HDMI + `TYPE_APPLICATION_OVERLAY`, TV giết FGS khi chuyển nguồn, phím Home/Back của remote Sony, màn quyền overlay của Sony, độ đọc được từ sofa.
- `card`, `alt`/nhóm `row-` end-to-end (chỉ có unit test của Dev).
- Kết nối nửa mở (NB5) và nhỏ giọt (NB3): chỉ đọc code.
- API 29/30 và mật độ ≠ 2.0.

## Câu hỏi mở — người chỉ cầm remote Sony sẽ kẹt ở đâu tối nay?

1. **Cấp quyền overlay**: màn hệ thống là danh sách chung; nếu Sony không liệt kê app cài tay thì kẹt hẳn ở "Chuẩn bị" — phải có người cầm máy tính chạy `adb … appops`. Chuẩn bị sẵn lệnh, đừng để User mò.
2. **Mở app để xem trạng thái = tắt chữ**. Bấm BACK để quay lại game thì chữ **vẫn tắt** (đã tái hiện). Lỡ bấm OK hai lần thì bật rồi tắt lại (NB1). Dặn User: "mở VietSub xong phải bấm OK **một lần** rồi Home".
3. **Chữ Việt ở Hộp thư bị bẻ dòng, cắt nửa** (B1) — thấy ngay ở màn đầu tiên.
4. TV giết app khi chuyển sang HDMI (`START_NOT_STICKY`, miễn pin là stub trên Google TV) ⇒ chữ biến mất, app không tự bật lại. Engine trên PC thì báo "chưa kết nối" mỗi 2 s. User chỉ thấy "không có chữ". Kiểm Bước 6.5 sớm; cần `deviceidle whitelist` qua adb.
5. Đổi mã ("Tạo mã mới"/"Ghép cặp lại") ⇒ TV chỉ hiện màn chờ, không báo engine cũ bị từ chối. Người ở sofa không biết phải chạy lại `engine pair` trên PC.

---

## Re-review fix 3b

**Verdict: PASS** — blocking B1 đã đóng, 7 NB đều đã làm. Không có blocking mới.

Đối chiếu [dev report fix3b](261114-epic1-phase3-dev-report-fix3b.md) với dòng "Sau review 3b — Rev 1.4".
- **Điều kiện chạy**: emulator riêng `emulator-5558` (launcher PID 11084, dừng bằng `taskkill /T`; `adb devices` rỗng; chỉ gỡ forward 47830 của mình — `<rv>/re3b-08-emulator-stop.txt`). Engine = **bản copy riêng** của build, `VIETSUB_HOME` tạm, `ANTHROPIC_API_KEY=''`, proxy chết, translator `fake` — 0 HTTP.
- **Test**: engine `0 Warning(s)` / `0 Error(s)`, `Passed: 186` (`<rv>/re3b-02-engine.txt`); tv-overlay **90 test, 0 fail/error**, `BUILD SUCCESSFUL` (`<rv>/re3b-01-gradle.txt`).

| Mục | Kết quả (`<rv>/re3b-03-smoke.txt`) |
|---|---|
| **B1** nhãn bẻ dòng | `rv-a-inbox` `replay --send`: frame `maxLines` = `{1: 19}`, `dup ids: {}`. Ảnh phóng to `<rv>/re3b-06-inbox-rows.png` (trên = gốc, dưới = lớp phủ): **mọi nhãn cả 3 cột một dòng, nằm trong hàng của nó**, kể cả hàng đang chọn; không còn tràn/ bị hàng dưới che. Code: co cỡ theo số dòng gốc trước, sàn `FLOOR_FACTOR = 0.85` (`AutoFit.kt:44-47`) |
| OFF focus | Mở app khi lớp phủ bật ⇒ OFF, focus **"Về màn hình chính"**; OK ⇒ về launcher (lớp phủ vẫn tắt, không bật lại oan); LEFT ⇒ "Bật lại lớp phủ" ⇒ OK ⇒ "● Đã kết nối" |
| Focus không bị kéo | RIGHT ⇒ "Ghép cặp lại", sau 3 s vẫn đó |
| LAN-only | `LanFilter.allowed` (`OverlayServer.kt:144-148`): loopback, IPv4 site-local + link-local, IPv6 link-local/ULA. Nguồn loopback qua forward được nhận. Nguồn công cộng thật **chưa thử** (chỉ có unit test của Dev) |
| Hạn hello / số kết nối chờ | 6 socket không gửi gì ⇒ socket 5–6 bị đóng ngay (`maxPending = 4`, `OverlayServer.kt:26,82`); hello nhỏ giọt 1 byte/0,5 s ⇒ **đóng sau 5,0 s** (log `hello quá hạn 5000 ms`) |
| Kết nối nửa mở | Hello hợp lệ rồi im lặng ⇒ TV **đóng sau 6,0 s** (= 2 × ttl) |
| Ẩn lớp phủ khi app tiền cảnh | `pattern --send` đang chạy: màn "Đã kết nối" **sạch**; về Home thì lớp phủ hiện (`<rv>/re3b-07-screens.png` ảnh 1–2) |
| Mã sinh một lần + gợi ý đổi mã | Ghép cặp lại ⇒ mã mới, dòng vàng "Mã đã đổi — trên máy tính chạy lại engine pair với mã mới." (ảnh 3); hello mã cũ ⇒ `{"ok":false,"reason":"token"}` |
| `id` trùng (phòng thủ TV) | Unit test `duplicate_ids_in_one_frame_keep_the_first_and_log` xanh; engine không còn gửi trùng |
| Crash | `logcat -b crash` = 0 |

**Non-blocking:**
- Nhãn dài ở hàng hẹp giờ **co nhỏ cả cột** (cùng `group` = cỡ nhỏ nhất, xuống tới 0,85 × `minFontPx`). Trên ảnh, cột giữa nhỏ rõ so với cột 1 và 3. Đọc từ sofa được không là việc Bước 8 — chỉnh `display.minFontPx`/`fontScale`.
- NB5 phụ thuộc engine gửi nhịp tim < 2 × ttl (đang 1 s / ttl 3 s) — đừng nâng `heartbeatMs` khi chỉnh tối nay.

### 5 điều Leader phải nói với User trước lần chạy đầu trên TV thật

1. **Kiểm `ro.build.version.sdk` ≥ 29 trước khi cài**; cài bằng `adb install -r` từ **cùng máy build**, khác máy thì gỡ app cũ trước (chữ ký debug).
2. **Chạy ngay hai lệnh adb** sau khi cài, đừng mò bằng remote: `appops set vn.vietsub.ps5.tv SYSTEM_ALERT_WINDOW allow` (nếu danh sách quyền của Sony không có VietSub) và `dumpsys deviceidle whitelist +vn.vietsub.ps5.tv` (nút pin trên Google TV không làm gì). Sau đó thử HDMI → chờ 1 phút → còn chữ không.
3. **Mở app VietSub = tắt chữ.** Muốn bật lại: ← (sang "Bật lại lớp phủ") rồi OK, sau đó Home để quay về game. OK ngay ở focus mặc định chỉ đưa về Home, chữ **vẫn tắt**.
4. **Lần gọi LLM đầu tiên**: trước khi `run`, chạy `engine warm --translator claude --yes true` trên **một** ảnh chụp nhỏ và đọc dòng `[llm]`. Có dòng "tắt backend LLM cho phiên này" ⇒ khoá/cấu hình sai: dừng lại sửa, không chạy tiếp.
5. **Chữ có thể nhỏ hoặc bị cắt "…" ở cột hẹp**; ngày ("12 Nov") và tên lạ vẫn tiếng Anh — bình thường ở bản này. Đổi mã trên TV ⇒ phải chạy lại `engine pair` trên PC (TV có dòng vàng nhắc). Chỉnh cỡ chữ trong `engine.local.json` (`display.fontScale`/`minFontPx`), không cần cài lại APK.
