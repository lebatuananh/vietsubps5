DISCOVERY-R2 DONE
# 261114 Dịch FC 26 (Career Mode) lên TV — Discovery BA/SA (Round 1)

**Role**: BA/SA · **Ngày**: 2026-11-14 · **Phạm vi được giao**: Discovery round 1 (định hướng + phản biện) — phản biện A1–A7, so sánh chỗ đặt lõi (PC / TV / điện thoại), inventory màn Career Mode, test 2 tầng, vertical slice bám ô 5, trải nghiệm người dùng (trụ cột 7), dứt khoát KHÔNG làm, module + tên nhánh, câu hỏi mở.

> Quy ước nhãn (theo `workflow/roles/ba-sa.md` §3): **`verified`** = đã đọc nguồn chính/ chạy lệnh trong lượt này (kèm URL hoặc lệnh + output) · **`verified-community`** = thấy tận mắt nhưng nguồn là diễn đàn/ người dùng, không phải tài liệu chính thức · **`assumption`** = suy luận, kèm cách kiểm · **`unknown`** = không xác định được, kèm lý do. Không có TV/PS5 trong lượt này ⇒ mọi thứ về thiết bị thật là `assumption`/`unknown` tới khi User chạy thử.

---

## 1. Tóm tắt cho Leader

1. **Console Translator KHÔNG phải bằng chứng cho trải nghiệm "không cần bật gì".** App TV của nó chỉ là màn hiển thị; lõi (Remote Play + OCR + dịch) chạy trên **iPhone/iPad hoặc app PC**, bản PC dùng **chiaki-ng (AGPL-3.0)** và hướng dẫn tạo **tài khoản PSN phụ** (`verified`, repo `kyozxx/PS-Translator`). Câu ô 3 *"cài app trên TV sau đó không cần làm gì cả"* rất có thể chỉ mô tả phần TV — cần hỏi lại User đã chạy lõi ở đâu và có phải mở nó mỗi lần không.
2. **A4 đúng về cơ chế**: lớp phủ = quyền `SYSTEM_ALERT_WINDOW` ("Hiển thị trên ứng dụng khác") + xin bỏ tối ưu pin; TV khoá quyền thì cấp bằng `adb shell appops set <pkg> SYSTEM_ALERT_WINDOW allow` (`verified`, TV.md của Console Translator). Không cần accessibility service.
3. **A2 xung đột với ô 3 (ràng buộc)**: cửa sổ PS Remote Play chính thức **chụp được** bằng Windows Graphics Capture (`verified-community`, BitBlt thì đen), nhưng app chính thức phải được mở, đăng nhập, bấm kết nối mỗi buổi chơi ⇒ trái *"không cần bật lại"*. Chính Console Translator bản PC đã bỏ hướng chụp app chính thức để nhúng chiaki-ng. ⇒ **App chính thức chỉ hợp làm đường nhanh cho lát đầu; trạng thái cuối đáp ứng ô 3 gần như chắc phải có client Remote Play tự chạy (chiaki-ng, AGPL)** — User cần quyết sớm (Risk gate: thư viện copyleft).
4. **Câu hỏi về tài khoản PSN mà brief chưa có**: phiên Remote Play chạy *song song* với người đang cầm tay cầm ở nhà; Console Translator cảnh báo dùng trùng tài khoản đang chơi sẽ ngắt tay cầm ⇒ có thể cần **tài khoản PSN phụ đăng nhập trên PS5** (Risk gate PSN). Chưa kiểm với app chính thức.
5. **A3 — khuyến nghị**: lõi trên **PC** cho cả đợt này (nhanh làm, dễ gỡ lỗi, OCR mạnh, không AGPL nếu dùng app chính thức); đưa "không cần bật gì" thành một lát riêng *trên PC* (lõi tự khởi động cùng Windows + client Remote Play tự kết nối). Lõi trên **TV** là cách duy nhất đáp ứng ô 3 đúng từng chữ nhưng nặng nhất (decode stream + OCR trên SoC TV ~4 nhân A73 1,8 GHz, RAM 3–4 GB theo nguồn cộng đồng, AGPL bắt buộc, khoá API LLM nằm trong APK) ⇒ đợt sau, sau khi đo. **Điện thoại**: kém nhất (vẫn phải bật app điện thoại mỗi lần, hao pin) — không khuyến nghị.
6. **Độ trễ ~0,5 s (ô 6 ③) chỉ giữ được khi trúng cache.** Câu mới đi qua LLM mất cỡ giây (`assumption`, chưa đo) ⇒ cần **làm ấm cache trước** từ ảnh chụp Career Mode, và luật "chưa có bản dịch tốt thì chưa vẽ" — phải nói rõ với User.
7. **Cái khó nhất của "đẹp, không méo xệch" không phải font mà là chuyển động**: overlay luôn trễ sau hình HDMI (stream Remote Play + xử lý) ⇒ khi User cuộn danh sách/ đổi mục chọn, chữ Việt lệch chỗ. Bắt buộc có luật ẩn khi màn đang động, hiện lại khi đứng yên; mục đang chọn (đổi màu nền) phải đổi kiểu theo. Overlay **không xoá được pixel** HDMI — chỉ che bằng mảng màu; ảnh nền/gradient + HDR làm mảng che lộ.
8. **Lệch tài liệu**: `CLAUDE.md` mục *Dự án* ghi phạm vi FC 26 là "menu, Ultimate Team, mô tả cầu thủ, chiến thuật", còn ô 2 của User là **Career Mode, chơi offline** ⇒ cần sửa `CLAUDE.md`.
9. Máy Windows: **không có `adb`, không có `java`** (cần cho build/cài APK); GPU **AMD Radeon 780M** tích hợp, màn 2880×1800 ⇒ nhiều khả năng là laptop (`assumption`) — "PC phải bật khi chơi" nghĩa là laptop không được ngủ.

---

## 2. Phát hiện làm thay đổi quyết định

| # | Phát hiện | Nhãn | Evidence | Hệ quả nếu bỏ qua |
|---|---|---|---|---|
| D1 | Console Translator: TV chỉ hiển thị; lõi chạy trên iPhone/iPad hoặc PC; bản PC Remote Play dựa trên chiaki-ng AGPL-3.0, mã ở `source/pc-remote-play` | verified | https://github.com/kyozxx/PS-Translator (README: "kết nối thẳng tới máy chơi game qua Wi-Fi nhà"; TV hiện mã 8 số để nhập vào iPhone/PC) · https://github.com/kyozxx/PS-Translator/releases/tag/pc-1.1.1 (Remote Play based on chiaki-ng, AGPL-3.0) | Leader/ User tưởng mức kỳ vọng ô 3 đã được chứng minh bằng một sản phẩm thật — thực ra sản phẩm đó cũng cần lõi ngoài TV chạy. Hứa "không cần bật gì" rồi giao bản phải mở 2 app mỗi buổi ⇒ trượt ô 3 |
| D2 | Cơ chế lớp phủ của Console Translator TV = `SYSTEM_ALERT_WINDOW` + miễn tối ưu pin; TV khoá quyền ⇒ cấp qua `adb shell appops set com.kyozx.consoletranslator.tv SYSTEM_ALERT_WINDOW allow`; Android TV ≤ 8 không cài được | verified | https://github.com/kyozxx/PS-Translator/blob/main/TV.md | Chọn sai cơ chế (accessibility, TIF) ⇒ tốn công; quên miễn tối ưu pin ⇒ TV giết app khi chuyển sang HDMI (TV.md nêu đúng triệu chứng này) |
| D3 | Chụp cửa sổ PS Remote Play: BitBlt ⇒ đen sau khi kết nối; "Windows 10 (1903 and up)" (WGC) ⇒ hiện đúng; `PrintWindow(...,3)` cũng chạy | verified-community | https://kaweah.freedombox.rocks/kiwix/stackoverflow.com_en_all_2023-11/a/76399855 · https://obsproject.com/forum/threads/remote-play-captured-as-black-screen.94822/ | A2 về mặt kỹ thuật chụp **khả thi**; nhưng phải dùng đúng API (WGC), không BitBlt/GDI |
| D4 | App PS Remote Play chính thức: phải đăng nhập "the same account that you use for your console", hướng dẫn cắm tay cầm qua USB; PS5 hiện thông báo "Remote Play connected" (ẩn được) | verified | https://www.playstation.com/en-us/support/games/playstation-remote-play-on-pc-and-mac/ | Mỗi buổi chơi User phải: bật PC → mở app → (có thể cắm tay cầm) → kết nối → để yên cửa sổ ⇒ trái ô 3. Có thể vướng tay cầm (xem D5) |
| D5 | Console Translator khuyên **tài khoản PSN phụ** bật Remote Play; cảnh báo một tài khoản chỉ dùng một nơi một lúc, dùng trùng tài khoản đang cầm tay cầm sẽ ngắt tay cầm đó | verified (với Console Translator) · unknown (với app chính thức) | README https://github.com/kyozxx/PS-Translator | Lõi kết nối Remote Play bằng tài khoản chính của User có thể đá tay cầm đang chơi ⇒ hỏng trải nghiệm ngay buổi đầu. Tạo tài khoản phụ = Risk gate PSN |
| D6 | Remote Play PS5 tự chọn tới 1080p theo chất lượng mạng (không cố định) | verified (snippet trang Sony SX365201 qua tìm kiếm; fetch trực tiếp trả 403) | https://www.sony-asia.com/electronics/support/articles/SX365201 | Toạ độ theo pixel sẽ sai khi stream tụt 720p/540p ⇒ phải dùng toạ độ chuẩn hoá 0..1; OCR chữ nhỏ trên stream 540p/720p kém hơn hẳn |
| D7 | Khởi chạy foreground service (FGS) từ nền: Android 15 chỉ miễn hạn chế khởi chạy FGS từ nền cho app có `SYSTEM_ALERT_WINDOW` **khi đang có cửa sổ overlay hiển thị**; receiver `BOOT_COMPLETED` không được khởi FGS loại `dataSync`, `mediaPlayback`, `mediaProjection`, `camera`, `phoneCall`, `microphone`; miễn tối ưu pin cũng là một ngoại lệ | verified | https://developer.android.com/develop/background-work/services/fgs/restrictions-bg-start · https://developer.android.com/about/versions/15/behavior-changes-15 | Tự khởi động sau khi bật TV phải thiết kế theo thứ tự: tạo overlay (rỗng) trước rồi mới FGS, chọn loại FGS không bị chặn (vd `specialUse`/`connectedDevice` — `assumption`, chọn ở Phase doc). **[Sửa ở Round 2]** Các giới hạn Android 15 chỉ có hiệu lực khi TV chạy Android 15; TV Sony hiện ở Android TV 10–14 (R2.3) ⇒ không có hiệu lực trên TV này, vẫn nên viết đúng thứ tự |
| D8 | Font UI của EA SPORTS FC là **Cruyff Sans** (F37, độc quyền cho EA; có bản back-slant/italic) | verified-community | https://fontsinuse.com/uses/63122/ea-sports-fc24 · https://www.f37foundry.com/custom-fonts/eafc · https://www.designweek.co.uk/issues/10-april-14-april-2023/ea-sports-fc-identity-uncommon/ | Không thể nhúng font gốc ⇒ "khớp kiểu chữ" = chọn font mở gần giống (Be Vietnam Pro/ Inter) + chấp nhận khác; chữ nghiêng/ ngược nghiêng làm OCR khó hơn |
| D9 | Trễ ô 6 ③ (~0,5 s) chỉ khả thi khi trúng cache; câu mới qua LLM cỡ giây | assumption (chưa đo; kiểm bằng đo end-to-end ở lát 1) | — | User kỳ vọng 0,5 s mọi lúc; buổi chơi đầu toàn câu mới ⇒ cảm giác "chậm, nhấp nháy" đúng lúc User duyệt kiểu hiển thị |
| D10 | Lệch phạm vi giữa `CLAUDE.md` ("menu, Ultimate Team…") và ô 2 (Career Mode, offline) | verified | `CLAUDE.md:26` vs `261114-draft.md:13` | Dev đọc `CLAUDE.md` có thể làm cả Ultimate Team |

---

## 3. Hiện trạng đã khảo sát

### 3.1 Repo và máy Windows

- Repo chưa có module code; `project-docs/present/backlog-and-techdebt.md` chỉ có khuôn mẫu (`verified`, đọc file, 19 dòng). `project-docs/automation/README.md` chưa ghi IP TV/PS5, chưa bật adb (`verified`, dòng 17–18).
- Lệnh `Get-Command claude, adb, java, dotnet, python, node -ErrorAction SilentlyContinue | Select-Object Name, Source, Version` (`verified`), output nguyên văn (exit 1 vì thiếu 2 lệnh):
  ```
  Name       Source                                                                  Version
  claude.exe C:\Users\lebatuananh\.local\bin\claude.exe                              2.1.325.0
  dotnet.exe C:\Program Files\dotnet\dotnet.exe                                      8.0.3026.36720
  python.exe C:\Users\lebatuananh\AppData\Local\Programs\Python\Python314\python.exe 3.14.3150.1013
  node.exe   C:\Program Files\nodejs\node.exe                                        22.20.0.0
  ```
  Bash `which adb java` ⇒ `which: no adb in (...)` và `which: no java in (...)` (exit 2). PATH có `/c/ffmpeg-n8.1.2-win64-gpl-8.1/bin` (hữu ích để cắt khung hình từ video làm fixture).
- `Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, AdapterRAM, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate` (`verified`):
  ```
  Name                        : AMD Radeon(TM) 780M
  DriverVersion               : 32.0.32015.7029
  AdapterRAM                  : 1073741824
  CurrentHorizontalResolution : 2880
  CurrentVerticalResolution   : 1800
  CurrentRefreshRate          : 60
  ```
  ⇒ GPU tích hợp (có bộ giải mã phần cứng H.264/HEVC — `assumption`), màn 2880×1800 kiểu laptop (`assumption` — hỏi User).
- PS Remote Play đã cài chưa: `Get-ChildItem 'C:\Program Files\Sony'` và Bash `ls "/c/Program Files/Sony"` ⇒ **bị sandbox chặn** ("Claude Code may only access files in the allowed working directories for this session: 'D:\Project\vietsubps5'") ⇒ `unknown`. User tự kiểm: Start menu có "PS Remote Play" không.
- Hệ quả: Dev cần cài Android SDK (adb) + JDK trước khi build `tv-overlay/`; .NET 8 có sẵn phù hợp cho lõi Windows (WGC + `Windows.Media.Ocr` dùng được từ .NET qua WinRT — `assumption`, kiểm bằng spike).

### 3.2 A1 — Kiến trúc (phản biện)

- Cách đặt tên trong A1 gộp 3 thứ khác nhau dưới chữ "Remote Play": (a) **giao thức** Remote Play của Sony, (b) **app PS Remote Play chính thức** (Windows/ Android TV/ di động), (c) **client bên thứ ba** (chiaki-ng). A2 chọn (b), Console Translator chọn (c). Ba lựa chọn khác nhau về giấy phép, khả năng tự chạy, rủi ro tài khoản (§3.4).
- "Overlay vẽ lên nguồn HDMI" thực chất là **cửa sổ hệ thống `TYPE_APPLICATION_OVERLAY` nằm trên mọi thứ đang hiển thị**, không gắn với nguồn HDMI (`verified` về loại cửa sổ: https://developer.android.com/about/versions/15/behavior-changes-15 mục SYSTEM_ALERT_WINDOW). Hệ quả chưa ai nêu: nếu User chuyển sang YouTube/ Netflix trong khi PS5 vẫn bật và phiên Remote Play vẫn chạy, **chữ dịch FC 26 vẫn được vẽ đè lên YouTube**. App TV cần biết nguồn hiện tại; API công khai cho app thường để biết nguồn TV đang hiện là `unknown` (kiểm: thử `TvInputManager`/ trạng thái HDMI-CEC trên TV thật). Giảm thiểu tạm: lõi phát hiện khung Remote Play không phải FC 26 ⇒ không gửi; và ẩn overlay khi app khác lên foreground (nếu phát hiện được).
- Vì sao cần Remote Play mà không chụp thẳng nguồn HDMI trên TV: lớp video HDMI trên TV là bề mặt phần cứng/ được bảo vệ (PS5 bật HDCP), chụp bằng `MediaProjection` sẽ ra đen (`assumption` — nguồn cộng đồng nói lớp có cờ bảo vệ bị tắt tiếng khi chụp: https://android-overflow.vercel.app/thread/79896431 ; kiểm được trên TV thật khi đã có adb: `adb exec-out screencap -p` lúc đang ở nguồn HDMI). Nếu kiểm ra **không** đen thì cả kiến trúc đơn giản hẳn (không cần Remote Play) — đáng 5 phút thử ngay khi User bật adb.

### 3.3 A2 — Chụp cửa sổ PS Remote Play chính thức trên Windows

- Chụp được: WGC (OBS "Windows 10 (1903 and up)") hiển thị đúng, BitBlt đen sau khi kết nối (`verified-community`, D3). Không thấy tài liệu Sony nói chặn chụp/ DRM với app Remote Play (`unknown` — Sony không công bố; đã đọc trang PC/Mac, không có mục hạn chế).
- Giới hạn của WGC cần ghi vào Phase doc (`assumption` — hành vi đã biết của WGC, kiểm bằng spike): cửa sổ **thu nhỏ (minimize)** thì không có khung mới; cửa sổ bị che vẫn chụp được; viền vàng chụp có thể hiện trên Windows 10/11 cũ (tắt được bằng `IsBorderRequired = false` trên Win11).
- Cái A2 bỏ sót:
  1. **Tự chạy**: app chính thức không có CLI/ chế độ nền; mỗi buổi phải mở và bấm kết nối ⇒ trái ô 3 (D4). Tự động hoá UI của app Sony (click giả) là dễ vỡ sau mỗi cập nhật.
  2. **Tay cầm & tài khoản**: Sony hướng dẫn cắm tay cầm USB vào PC (D4). Không có tay cầm thì app có stream không, và phiên Remote Play có tranh tay cầm với người đang chơi không ⇒ `unknown`, phải thử thật (tầng 1, chỉ User làm được).
  3. **Thông báo "Remote Play connected"** hiện trên TV mỗi lần kết nối — ẩn bằng tuỳ chọn "Hide Remote Play Connection Status" trên PS5 (`verified`, trang Sony PC/Mac) ⇒ ghi vào hướng dẫn cài.
  4. **Chất lượng stream tự động** (tới 1080p, có thể tụt — D6) ⇒ OCR chữ nhỏ trên stream nén là điểm rủi ro số 1 về chất lượng.
- Bằng chứng gián tiếp: Console Translator PC (cùng bài toán, cùng thị trường VN) chuyển sang nhúng chiaki-ng (D1). Lý do họ chọn vậy `unknown`, nhưng phù hợp với 1–2 ở trên.

### 3.4 A3 — Đặt lõi ở đâu: PC / TV / điện thoại

Leader cho rằng chi phí thêm chủ yếu là client Remote Play chứ không phải APK — **đúng một nửa**: với TV còn có chi phí *tài nguyên SoC* và *phân phối khoá API*; và client Remote Play tự chạy **cũng cần ngay cả trên PC** nếu muốn đạt ô 3 (§3.3).

| Tiêu chí | **PC (Windows)** | **TV (Sony Google TV)** | **Điện thoại (Android)** |
|---|---|---|---|
| Nguồn hình | App chính thức + WGC (lát đầu) → chiaki-ng tự kết nối (lát "không cần bật") | Bắt buộc client tự viết/nhúng (chiaki-ng Android); app Sony Remote Play cho Android TV OS 12+ có tồn tại (`verified`: https://www.playstation.com/en-bg/support/games/remote-play-android-tv/) nhưng nó chiếm toàn màn hình, không dùng làm nguồn ẩn được (`assumption`) | chiaki-ng Android (`verified` có Android trong danh sách nền tảng: https://github.com/streetpea/chiaki-ng) |
| Công sức | Thấp nhất cho lát 1 (C#/.NET 8 có sẵn, WGC + `Windows.Media.Ocr` sẵn trong Windows) | Cao nhất: build chiaki-ng (C/NDK) trong APK, decode MediaCodec, OCR ML Kit, dịch, vẽ — tất cả trong một tiến trình nền trên TV | Cao: như TV nhưng ít ràng buộc hơn về RAM; vẫn cần app TV riêng để vẽ |
| Trễ chữ dịch (khi trúng cache) | Thấp nhất: OCR trên CPU/GPU laptop (`assumption`, chưa đo) | Cao nhất: OCR trên 4×A73 1,8 GHz (`assumption`) có thể hàng trăm ms tới >1 s/khung 1080p | Trung bình (SoC điện thoại mạnh hơn TV — `assumption`) + 1 bước LAN tới TV |
| Tải CPU/GPU/RAM | Không đáng kể với laptop Radeon 780M (`assumption`) | Nặng: SoC lớp MT5895 (4×A73 1,8 GHz), RAM 3–4 GB theo nguồn cộng đồng (`verified-community`: https://www.tvsbook.com/threads/2023-sony-tv-x80l-vs-x85l-vs-x90l-vs-x95l-vs-a80l-vs-a95l-specs-guide.8095 ; Sony không công bố). TV đồng thời xử lý ảnh HDMI 4K ⇒ nguy cơ bị LMK giết, nóng, giật UI TV | Hao pin, nóng máy nếu chạy hàng giờ |
| Ổn định | Cao (dễ log, dễ gỡ lỗi); rủi ro: laptop ngủ/ Wi-Fi | Thấp nhất: TV có thể giết tiến trình khi chuyển nguồn (TV.md đã gặp), cập nhật firmware Sony đổi hành vi | Thấp: điện thoại rời nhà/ gọi điện/ hết pin là mất |
| Nghĩa vụ giấy phép khi chia sẻ | App chính thức: không AGPL. chiaki-ng: nếu chạy **nguyên bản như một chương trình riêng** và chỉ chụp cửa sổ của nó thì nghĩa vụ là kèm/ trỏ mã nguồn chiaki-ng (`assumption` — "mere aggregation", cần đọc AGPL §5 kỹ); nếu link vào lõi ⇒ lõi thành AGPL | APK nhúng chiaki-ng ⇒ **toàn bộ APK AGPL**, phải công bố mã khi chia sẻ (ràng buộc `CLAUDE.md:43`) | Như TV |
| Rủi ro tài khoản PSN | App chính thức: thấp nhất (app của Sony). chiaki-ng: dùng cùng handshake Remote Play; chưa thấy báo cáo bị khoá; bước lấy Account ID qua OAuth là chỗ "có thể vi phạm ToS" theo guide cộng đồng (`verified-community`: https://lr.eu.psf.lt/r/SteamDeck/wiki/guides/psremoteplay) | Như chiaki-ng | Như chiaki-ng |
| Đáp ứng ô 3 ("không cần bật lại") | App chính thức: **không**. chiaki-ng + lõi tự chạy khi Windows khởi động + tự phát hiện PS5 bật: **gần đạt** — chỉ cần PC đang bật (đã chấp nhận ở ô 6 ①) | **Đạt đúng chữ** nếu tự khởi động sau khi TV bật được (D7) | **Không**: phải mở app điện thoại mỗi buổi |
| Khoá API LLM | Biến môi trường máy User (đúng `CLAUDE.md` mục Môi trường) | Phải nằm trong APK/ cấu hình TV ⇒ trích xuất được; mỗi người được chia sẻ cần khoá riêng hoặc User trả tiền cho họ | Như TV |

**Khuyến nghị**: PC cho toàn bộ đợt này. Lát đầu dùng app chính thức + WGC để có hình lên TV nhanh nhất (không AGPL, không đụng Remote Play protocol). Đưa "không cần bật gì" thành lát riêng, vẫn trên PC: lõi chạy nền tự khởi động cùng Windows, client Remote Play tự kết nối (chiaki-ng chạy như chương trình riêng — cần User duyệt AGPL + cách lấy Account ID). TV chỉ xét ở đợt sau, khi đã có số đo OCR/ trễ thật và biết chính xác SoC. Điện thoại: loại.

**Phải hỏi User để xác định SoC TV**: tên model chính xác (TV: *Cài đặt → Hệ thống → Giới thiệu → Model*, dạng `KD-65X85L`, `K-65XR70`, `K-65S30`…), phiên bản Android TV OS và firmware (cùng màn hình), năm mua. Khi có adb: `adb shell getprop ro.product.model`, `getprop ro.board.platform`, `getprop ro.build.version.release`, `cat /proc/meminfo` (agent chỉ đọc, sau khi User bật gỡ lỗi — Risk gate).

### 3.5 A4 — Lớp phủ trên Sony Google TV

- Cơ chế (`verified`, D2): `SYSTEM_ALERT_WINDOW` + cửa sổ `TYPE_APPLICATION_OVERLAY`; miễn tối ưu pin để không bị giết khi chuyển sang HDMI. Không thấy accessibility service trong hướng dẫn.
- Hạn chế: nhiều bản Android TV/ Google TV **không có** màn cấp quyền "Display over other apps" hoặc intent `ACTION_MANAGE_OVERLAY_PERMISSION` không mở được (`verified-community`: https://issuetracker.google.com/issues/37121932 ; https://blog.floatingapps.net/2018/02/using-floating-apps-on-nvidia-shield-and-android-tv/). Console Translator đã chạy trên chính TV này ⇒ ít nhất **một** cách cấp quyền đã thành công trên TV User (`assumption` — hỏi User đã cấp qua màn cài đặt hay qua adb).
- User cấp quyền cho APK sideload: (1) bật "Nguồn không xác định" cho app cài (vd Downloader/ trình quản lý file) — hoặc cài qua `adb install`; (2) *Cài đặt → Ứng dụng → Quyền truy cập đặc biệt → Hiển thị trên ứng dụng khác* bật cho app; không có mục đó ⇒ `adb shell appops set <pkg> SYSTEM_ALERT_WINDOW allow`; (3) bỏ tối ưu pin cho app (mục tương ứng hoặc `adb shell dumpsys deviceidle whitelist +<pkg>` — `assumption`, kiểm trên TV). Mọi bước trên thiết bị thật: **User chạy** (Risk gate).
- Tự khởi động: `BOOT_COMPLETED` cho phép khởi FGS nhưng có loại bị cấm từ Android 15 (D7) — **[Round 2]** không áp dụng trên TV Sony Android 10–14; boot start thuộc lát X. TV Sony thường "ngủ" chứ không tắt hẳn ⇒ không có `BOOT_COMPLETED` mỗi lần bật TV; tiến trình có thể sống qua chế độ chờ hoặc bị giết (`unknown`, kiểm thật).
- Cảm ứng/ phím remote: overlay phải `FLAG_NOT_FOCUSABLE | FLAG_NOT_TOUCHABLE` để không nuốt phím remote/ không cản game (`assumption` — hành vi chuẩn Android; kiểm trên emulator được).

### 3.6 A5 — Ánh xạ toạ độ khung Remote Play → panel TV

"Tắt overscan rồi nhân tỉ lệ" là **chưa đủ**. Những thứ làm vỡ ánh xạ:

| Nguồn sai lệch | Nhãn | Cách xử lý đề xuất |
|---|---|---|
| Stream đổi độ phân giải theo mạng (tới 1080p, có thể thấp hơn — D6) | verified | Toạ độ chuẩn hoá 0..1 theo vùng hình thật của khung, không theo pixel |
| Cửa sổ app Remote Play không đúng 16:9 ⇒ có viền đen trong ảnh chụp WGC | assumption | Tự dò viền đen/ khung hình 16:9 trong ảnh chụp trước khi chuẩn hoá |
| PS5 "Adjust Display Area" (thu nhỏ vùng hình trên HDMI) — có áp cho stream Remote Play không | unknown | Hỏi User đã chỉnh chưa; màn hiệu chỉnh trên TV (`CLAUDE.md:80`) phải có bước căn 4 góc |
| TV: chế độ "Screen size"/ zoom trong Game Menu, overscan, chế độ ảnh | verified-community (Game Menu có Screen Size: https://manuals.plus/m/a02486946b1057d98bc0f995b58e8c134311d4715ace688202ad89ec6a22930f) | Ghi tên mục chính xác vào `automation/README.md` khi User chỉnh lần đầu |
| Mặt phẳng đồ hoạ Android trên TV thường 1920×1080 dù panel 4K; overlay vẽ theo dp/px của mặt phẳng này | assumption | Lấy kích thước thật bằng `WindowMetrics` trên TV; mọi phép tính dùng toạ độ chuẩn hoá |
| Trễ thời gian (overlay trễ sau HDMI) khi màn đang chuyển động/ cuộn | assumption (chắc chắn về nguyên lý, độ lớn chưa đo) | Không phải lỗi không gian mà lỗi thời gian — luật ẩn/hiện (§3.8) |

### 3.7 A6 — OCR và dịch

**OCR theo chỗ đặt lõi**

| Engine | Chỗ dùng | Điểm mạnh | Điểm yếu cho FC 26 | Nhãn |
|---|---|---|---|---|
| `Windows.Media.Ocr` | PC | Có sẵn trong Windows, trả dòng → từ + vị trí; có `MaxImageDimension` | Không trả độ tin cậy, không trả màu/ cỡ chữ (suy từ chiều cao dòng); chữ nghiêng/ nền ảnh chưa rõ | verified (API): https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine · độ chính xác trên FC 26: unknown |
| ML Kit Text Recognition v2 (Latin) | TV/ điện thoại | On-device, block/line/element/symbol + bbox + confidence + góc; Android API 23+; bundled ~4 MB/script | Chữ cần ≥16×16 px mỗi ký tự — chữ nhỏ trên stream 720p dễ dưới ngưỡng | verified: https://developers.google.com/ml-kit/vision/text-recognition/v2 · https://developers.google.com/ml-kit/vision/text-recognition/v2/android |
| Tesseract | Cả hai | Mở, tuỳ biến được | Kém với chữ trên nền ảnh/ chữ cách điệu nếu không tiền xử lý | assumption |
| PaddleOCR / mô hình ONNX | PC | Thường tốt hơn với chữ cảnh (scene text) | Phải đóng gói runtime, tải mô hình | assumption |

Đề xuất: lát đầu dùng `Windows.Media.Ocr` trên fixture, **đo** tỉ lệ đúng; chỉ đổi engine khi số đo kém. Mẹo chắc chắn có ích (`assumption`): OCR theo vùng (crop) thay vì cả khung; phóng to 2× vùng chữ nhỏ trước khi OCR; so khung với khung trước để chỉ OCR lại vùng đổi.

**Dịch**: glossary bóng đá + cache (game + câu gốc chuẩn hoá) + LLM cho câu mới — đúng hướng `CLAUDE.md:45`. Bổ sung:
- **Chuẩn hoá để cache trúng** (câu Career Mode là template có biến): (1) chuẩn hoá khoảng trắng, dấu nháy, hoa/thường của nhãn; (2) che biến bằng regex: số, tiền (`€12.5M`, `£40K/wk`), ngày, phần trăm, chỉ số (`OVR 84`), vị trí (`ST`, `CDM`); (3) che tên riêng: tên cầu thủ/ CLB/ giải — dùng danh sách tên lấy từ chính màn hình (vd tên cầu thủ trên màn Squad đã OCR) + để LLM trả về **template có slot** (`{P1} đã đồng ý gia hạn tới {D1}`) ⇒ cache lưu template, lần sau chỉ điền slot; (4) khoá cache = `gameId` + `screenKind` + template chuẩn hoá + `glossaryVersion`.
- **OCR sai làm cache phình**: một ký tự OCR sai = khoá mới = một lần gọi LLM mới. Cần so khớp gần đúng (khoảng cách chỉnh sửa nhỏ với template đã có) trước khi gọi LLM (`assumption`).
- **Làm ấm cache trước khi cài**: dịch sẵn chữ từ bộ ảnh fixture của từng nhóm màn (§7) ⇒ buổi chơi đầu chủ yếu trúng cache ⇒ giữ được trễ ô 6 ③.
- **Câu chưa có bản dịch**: không vẽ bản dịch máy tạm rồi đổi sang bản LLM (đổi chữ = nhấp nháy, trái `CLAUDE.md:44`) — giữ tiếng Anh tới khi có bản tốt rồi hiện một lần.

**Ước lượng chi phí dịch Career Mode có cache** (`assumption` — ước lượng bậc độ lớn, chưa đo):
- Giả định: số câu/ nhãn **duy nhất sau chuẩn hoá** gặp trong một mùa giải Career Mode: 10.000–30.000; trung bình ~20 token tiếng Anh/ câu; đầu ra tiếng Việt ~1,5–2× token; gọi theo lô ~20 câu/lần, mỗi lần kèm ~2.000 token glossary + hướng dẫn (đọc từ prompt cache).
- Giá (`verified`, https://platform.claude.com/docs/en/about-claude/pricing, đọc 2026-11-14): Claude Sonnet 5.5 $2/MTok vào · $10/MTok ra · cache hit $0,10/MTok; Claude Haiku 5.5 $0,10/MTok vào · $0,50/MTok ra (prompt ≤100k token); Batch API giảm 50%.
- Tính: vào ~0,2–0,6 MTok + glossary đọc cache ~1–3 MTok; ra ~0,4–1,2 MTok ⇒ **Sonnet 5.5: cỡ $5–$15 cho một mùa giải; Haiku 5.5: cỡ dưới $1**; mùa sau phần lớn trúng cache (chủ yếu tên/ số mới) ⇒ thấp hơn nhiều. Nếu **không** chuẩn hoá (mỗi tin tức có tên riêng là một khoá) số câu duy nhất có thể tăng 5–20× (`assumption`). Làm ấm cache từ fixture bằng Batch API giảm 50%.
- Trần ký tự/ngày (`CLAUDE.md:45`) vẫn bắt buộc vì OCR lỗi có thể sinh vô hạn khoá mới.
- Nhà cung cấp chưa chọn (`CLAUDE.md:96`) — con số trên chỉ minh hoạ bằng bảng giá Anthropic; quyết định nhà cung cấp là Risk gate.

### 3.8 A7 — Vẽ đẹp tại chỗ: cái gì khó trên màn FC 26 thật

| Khó khăn | Vì sao | Hướng xử lý |
|---|---|---|
| **Không xoá được chữ gốc** | Overlay là lớp trên; pixel HDMI bên dưới không đọc/ sửa được trên TV | Che bằng mảng màu đặc (hoặc gradient đơn giản) lấy mẫu màu nền quanh chữ từ khung Remote Play |
| **Màu mẫu từ stream ≠ màu trên TV** | Stream nén SDR (trừ khi bật HDR cho client) còn TV đang hiển thị HDR10 từ PS5; mặt phẳng đồ hoạ Android có thể bị tone-map khác lớp video (`assumption`) | Đừng hứa "trùng màu tuyệt đối"; thiết kế kiểu che có chủ ý (§5) + màn hiệu chỉnh màu/ độ sáng overlay; hỏi User PS5 có bật HDR không |
| **Chữ trên ảnh/ gradient** (thẻ cầu thủ, tin tức có ảnh, nền sân) | Không có màu nền phẳng để che | Kiểu "thẻ phụ đề" (card bo góc, nền tối mờ) thay vì giả làm chữ gốc |
| **Chuyển động & trễ** | Menu trượt/ mờ dần khi chuyển màn; cuộn danh sách; mục đang chọn đổi nền và phóng to; tin chạy (ticker) | Máy trạng thái: phát hiện khung đổi mạnh ⇒ ẩn ngay (fade-out ngắn); vùng đứng yên ≥ N khung + OCR ổn định 2 lần ⇒ hiện (fade-in); vùng đang chọn vẽ kiểu "selected" |
| **Chữ Việt dài hơn** | Tiếng Việt thường dài hơn tiếng Anh, có dấu chồng (ế, ỗ, ặ) cao hơn cap-height | Auto-fit: giảm cỡ tới mức tối thiểu đọc được từ xa (chốt ở mockup) → xuống 2 dòng nếu ô đủ cao → dùng bản ngắn trong glossary (vd nhãn tab) — **không** dùng "…" cho nhãn |
| **Font gốc độc quyền, có chữ nghiêng ngược** | Cruyff Sans (D8) | Be Vietnam Pro (mở, đủ dải U+1EA0–1EF9) nhúng trong APK; giữ đậm/nhạt và hoa/thường theo bản gốc; chữ nghiêng: `skewX` nhẹ |
| **Nhãn HOA TOÀN BỘ** (tab, nút) | FC dùng nhiều nhãn hoa | Viết hoa tiếng Việt vẫn giữ dấu ("CHUYỂN NHƯỢNG") — kiểm font có đủ glyph hoa có dấu |
| **Icon nút tay cầm lẫn trong câu** (△, ○, R1…) | OCR đọc thành ký tự rác | Che theo từng đoạn chữ, chừa vùng icon; glossary bỏ qua token icon |
| **Thông báo "Remote Play connected" của PS5, popup hệ thống PS5** | Không phải chữ game | Lọc theo vùng/ kiểu; ẩn thông báo bằng cài đặt PS5 |

---

## 4. Cấu trúc dữ liệu và ràng buộc

Không có DB máy chủ. Ba thực thể mới (chốt định dạng cụ thể ở README module theo `CLAUDE.md` Quy tắc vàng 5). Đây là đề xuất để Phase doc chốt — **chưa có code nào** (`verified`: repo không có module).

### 4.1 `OverlayFrame` — thông điệp lõi → TV (LAN)

| Field | Kiểu | Nullable | Mặc định | Ai ghi | Ghi chú |
|---|---|---|---|---|---|
| `v` | int | không | 1 | lõi | Phiên bản giao thức; TV từ chối bản lạ |
| `sessionId` | string | không | — | lõi | Đổi mỗi lần lõi khởi động |
| `seq` | long | không | — | lõi | Tăng dần; TV bỏ gói `seq` cũ hơn gói đã vẽ |
| `screenKind` | string | có | null | lõi | vd `inbox_list`, `inbox_mail` — để log/ kiểu hiển thị |
| `items` | array<`OverlayItem`> | không | `[]` | lõi | Mảng rỗng = xoá hết overlay |
| `ttlMs` | int | không | 3000 | lõi | Hết hạn không có gói mới ⇒ TV tự xoá (lõi chết thì chữ không đứng mãi) |

`OverlayItem`: `id` (string, ổn định giữa các khung để animate), `box` {`x`,`y`,`w`,`h`: float 0..1 theo vùng hình game}, `text` (string tiếng Việt, ≤ 1000 ký tự), `style` {`fontScale` float, `color` ARGB, `cover` {`mode`: `solid`|`card`|`none`, `color` ARGB}, `align`: `start`|`center`|`end`, `weight`: `regular`|`bold`, `caps` bool, `italic` bool, `selected` bool}, `maxLines` int (1–3).

### 4.2 `TranslationEntry` — cache bản dịch (file/ DB nhúng của lõi)

| Field | Kiểu | Nullable | Mặc định | Ai ghi | Ghi chú |
|---|---|---|---|---|---|
| `gameId` | string | không | `fc26` | lõi | |
| `sourceKey` | string | không | — | lõi | Template tiếng Anh đã chuẩn hoá (slot `{P1}`, `{N1}`…) |
| `sourceSample` | string | không | — | lõi | Một câu gốc thật (để review) |
| `target` | string | không | — | lõi (LLM/ người sửa) | Template tiếng Việt, cùng tập slot |
| `provider`, `model` | string | không | — | lõi | |
| `glossaryVersion` | int | không | — | lõi | Đổi glossary ⇒ có thể dịch lại có chọn lọc |
| `status` | enum `machine`|`reviewed`|`rejected` | không | `machine` | lõi / End User review | `reviewed` không bao giờ bị ghi đè tự động |
| `createdAt`, `hitCount`, `lastHitAt` | datetime/int | không | now/0 | lõi | |

Ràng buộc: duy nhất (`gameId`, `sourceKey`) — chặn ở **tầng DB nhúng** (unique index) · tập slot của `target` phải bằng tập slot của `sourceKey` — chặn ở **validator của lõi** trước khi ghi (LLM trả thiếu slot ⇒ loại, không cache) · `status=reviewed` bất biến trước ghi đè tự động — chặn ở **lõi**.

### 4.3 `GlossaryTerm` (file trong repo, có version)

`term_en` (khoá, không trùng trong một game) · `term_vi` · `short_vi` (nullable — bản ngắn cho nhãn hẹp) · `kind` (`position`|`attribute`|`ui`|`finance`|`competition`…) · `doNotTranslate` bool (tên giải, thương hiệu). Ràng buộc chặn bằng **test** đọc file (không trùng khoá, có `short_vi` thì ngắn hơn `term_vi`).

### 4.4 Cấu hình ghép cặp TV ↔ lõi

**[Sửa ở Round 2]** Kênh = TCP, mỗi gói một dòng JSON, **TV là server**, engine là client (R2.3). `tvHost`, `port`, `pairingToken` (TV sinh một lần, hiện trên màn app TV; User chép vào `engine.local.json`) — LAN only, không mở cổng ra Internet (`CLAUDE.md:46`). TV chỉ nhận gói có token đúng ⇒ máy lạ trong LAN không vẽ được chữ. Không lưu bất cứ gì về mật khẩu PSN (`CLAUDE.md:41`).

Dữ liệu cũ: không có (đợt đầu).

---

## 5. Mockup UI — đề xuất cho vòng mockup (round này KHÔNG vẽ)

**User sẽ thấy & cảm thấy gì**: bật PS5 → vào Career Mode → sau một thoáng, chữ tiếng Anh ở đúng chỗ được thay bằng tiếng Việt cùng cỡ, cùng tông; cuộn menu thì chữ Việt mờ đi rồi hiện lại khi dừng; không có cửa sổ/ biểu tượng thừa; không phải cầm điện thoại.

**So với Console Translator** (`verified` theo TV.md/ README): nó là phụ đề/ "Dịch vùng" — chữ dịch hiện như một khối riêng, kiểu phụ đề, kèm đọc giọng. Mức kỳ vọng ô 3 ("chuẩn và đẹp, không méo xệch") cao hơn: **thay tại chỗ**, như game có sẵn tiếng Việt.

**Định hướng thiết kế (để Leader + SA chốt, User duyệt mức này)**:
- Vibe: *bản địa (native) · kín đáo · sắc nét · bình tĩnh*. Tham chiếu: chính UI FC 26 (phẳng, tối, chữ không chân đậm) — lấy *nhịp chữ và thứ bậc đậm/nhạt*; tham chiếu phụ đề Netflix trên TV — lấy *độ dễ đọc từ 3 m*.
- Kiểu che (component): `solid` (nền phẳng: mảng màu lấy mẫu, viền mờ 1–2 px để không lộ mép) · `card` (chữ trên ảnh: thẻ bo góc nền tối ~80% đục) · `selected` (mục đang chọn: theo màu nhấn của game).
- Chữ: Be Vietnam Pro, 2–3 độ đậm; cỡ tối thiểu chốt bằng thử trên TV 65" ở khoảng cách ngồi thật.
- Chuyển động: fade-in ~120–150 ms khi chữ ổn định, fade-out ~80 ms khi màn đổi; không trượt/ nảy (chữ game không nảy). Không có reduced-motion trên TV ⇒ cho tắt hiệu ứng trong màn cài đặt.
- Khoảnh khắc chính: *lần đầu mở Hộp thư và đọc được cả email bằng tiếng Việt mà không thấy "miếng dán"*.

**Mockup phase phải thể hiện** (prototype bấm được theo §2 Bước 6 của handbook, khung 16:9 như TV): mỗi màn trong lát 1 trên **ảnh nền là ảnh chụp FC 26 thật** — trước/sau; trạng thái: *đang chờ bản dịch* (giữ tiếng Anh), *đã dịch*, *đang chuyển động* (ẩn), *mục đang chọn*, *Overflow* (câu Việt dài nhất trong fixture, 2 dòng, cỡ tối thiểu), *Error/ mất kết nối lõi* (overlay tự xoá sau `ttlMs`, có hay không một chấm trạng thái nhỏ — User chọn), *chưa ghép cặp* (màn app TV hiện mã ghép + IP), *màn hiệu chỉnh 4 góc*. Thêm so sánh 3 kiểu che trên cùng một vùng chữ-trên-ảnh.

---

## 6. Rà consumer

Không áp dụng — repo chưa có code, không có contract cũ. Contract mới duy nhất là `OverlayFrame` (§4.1); khi đổi phải đổi đồng thời `engine/` và `tv-overlay/` (TV đã cài là "client đã phát hành": giữ trường `v` để TV cũ từ chối gói mới thay vì vẽ sai).

---

## 7. Kế hoạch nghiệm thu & test 2 tầng

**Metric thành công** (đề xuất, User duyệt):
- Trên màn của lát: ≥ 95% đoạn chữ tiếng Anh được thay; 0 chữ tràn khung; 0 nhấp nháy khi màn đứng yên 10 s (`chưa kiểm`).
- Trễ hiện chữ Việt khi trúng cache: mục tiêu ≤ 0,5 s sau khi màn đứng yên (ô 6 ③); câu mới: hiện trong ≤ N s (N đo ở lát 1) (`chưa kiểm`).
- Chi phí: theo dõi ký tự gửi LLM/ngày; không vượt trần (`chưa kiểm`).

**Tầng 1 — smoke KHÔNG cần PS5** (Dev/ Reviewer tự chạy):
- **Fixture**: ưu tiên **ảnh/video User tự chụp bằng nút Create của PS5** trong Career Mode (đúng font/ độ phân giải; PS5 chặn chụp ở vài cảnh — `assumption`) + ghi một đoạn video cửa sổ PS Remote Play (đúng chất lượng nén thật). Bổ sung ảnh/ video công khai trên web chỉ để khám phá, **không commit** vào repo (bản quyền EA; repo có thể chuyển public khi chia sẻ AGPL — `CLAUDE.md:64`). Ảnh fixture User tự chụp: commit được khi repo private; quyết lại khi chuyển public.
- **Lõi**: test chạy trên thư mục fixture ⇒ JSON {đoạn chữ, bbox, bản dịch}; so với file kỳ vọng (golden) — đo tỉ lệ OCR đúng; render overlay *offline* lên chính ảnh fixture (PNG trước/sau) để review mắt và so ảnh.
- **TV app**: Android TV emulator (Android Studio) — chạy một app phát video FC 26 toàn màn hình thay cho nguồn HDMI (emulator không có HDMI — `CLAUDE.md:94`), lõi gửi `OverlayFrame` từ fixture qua `10.0.2.2`/ LAN; chụp `adb -s emulator-xxxx exec-out screencap -p` để so. Kiểm: overlay không ăn phím, tự xoá theo `ttlMs`, chữ Việt đủ dấu, auto-fit.
- **Pure logic có TDD**: chuẩn hoá/ che biến, khoá cache, so khớp gần đúng, ánh xạ toạ độ + dò viền đen, auto-fit, máy trạng thái ẩn/hiện, (de)serialize `OverlayFrame`, validator slot.
- **Dữ liệu test**: cache bản dịch test là file riêng trong thư mục test, để lại; không gọi LLM thật trong test (giả lập) — gọi thật là Risk gate.
- **Reviewer chạy lại smoke**: **có** cho lát 1 (tích hợp ngoài: WGC + OCR trên ảnh thật, dễ "xanh trên fixture, đỏ trên máy thật").

**Chỉ verify được trên TV thật, User làm** (Leader đưa lệnh từng bước):
1. Cấp `SYSTEM_ALERT_WINDOW` + bỏ tối ưu pin cho APK của mình; overlay hiện trên nguồn HDMI PS5.
2. Overlay sống qua: chuyển nguồn HDMI ↔ Home, TV chờ/ bật lại, PS5 nghỉ/ bật lại.
3. Phiên Remote Play song song khi đang chơi bằng tay cầm: tay cầm có bị ngắt không, cần tài khoản phụ không (D5).
4. Căn toạ độ 4 góc; màu/ độ sáng mảng che khi HDR bật/ tắt.
5. Cảm giác trễ khi cuộn menu; game có "dính tay" không (chứng minh không đụng đường hình).
6. (5 phút, nên làm sớm) `adb exec-out screencap -p` khi đang ở nguồn HDMI — đen hay không (§3.2).

**Tầng 2 — chất lượng bản dịch (End User)**: có. Mốc: trước **mỗi** lần cài lên TV (mỗi lát), End User đọc bảng câu gốc ↔ bản dịch của nhóm màn đó + ảnh render trước/sau. Tiêu chí: đúng thuật ngữ bóng đá VN quen dùng (glossary), tự nhiên, xưng hô nhất quán, giữ đúng biến (tên, số, tiền), độ dài vừa khung, không dịch tên riêng/ thương hiệu. Câu đạt ⇒ `status=reviewed`.

---

## 8. Đề xuất vertical slice & TDD (bám ô 5)

> **[Round 2] Bảng lát dưới đây đã được thay bằng R2.7** (thứ tự User duyệt, lát X sau 2–3 nhóm, cột mockup). Bảng inventory màn bên dưới vẫn còn giá trị.

| Lát | Làm gì | Cài lên TV thử? | TDD | Ghi chú |
|---|---|---|---|---|
| 0 (nội bộ) | Spike: chụp cửa sổ PS Remote Play bằng WGC + OCR fixture + đo; APK tối thiểu vẽ một khung chữ cố định trên emulator | Không (ô 5: lần cài đầu = một màn đẹp) — **trừ** một APK "hello overlay" nếu Leader muốn khử rủi ro quyền overlay sớm (đề xuất có, cần User đồng ý vì lệch nhẹ ô 5) | Ánh xạ toạ độ | `chưa kiểm` |
| **1 — lần cài đầu** | **Hộp thư (Inbox)**: danh sách thư + nội dung một email, đủ đẹp/chuẩn; cache làm ấm từ fixture; ghép cặp TV ↔ lõi; màn hiệu chỉnh | **Có** | Chuẩn hoá, cache, auto-fit, máy trạng thái, protocol | Lõi PC + app chính thức (User mở tay). Nói rõ với User: lát này chưa đạt "không cần bật gì" |
| 2 | Khung chính Career: Career Hub, tab, Switcher Menu, menu tạm dừng (chữ tĩnh, ngắn) | Có | glossary `short_vi` | Nhiều nhãn HOA |
| 3 | Squad Hub/ Team Management, hồ sơ & chỉ số cầu thủ (attributes, vị trí, Player Stats) | Có | | Nặng glossary |
| 4 | Chuyển nhượng: Transfer Hub, Global Transfer Network/ scout report, đàm phán, gia hạn hợp đồng, cho mượn | Có | | Nhiều tiền tệ/ số |
| 5 | Sự kiện: Unexpected Events, Decision Points, Board Objectives, Manager Market/ job offers | Có | | Popup toàn màn, chữ dài |
| 6 | Tin tức & mạng xã hội trong game | Có | | Động nhất — kiểm chi phí |
| 7 | Tập luyện/ phát triển, học viện trẻ, National Team Training Plans | Có | | |
| 8 | Manager Live (Challenge details, objectives, rewards), cài đặt, tutorial | Có | | |
| X ("không cần bật gì") | Lõi tự chạy cùng Windows + client Remote Play tự kết nối + tự phát hiện PS5 bật | Có | Logic phát hiện/ kết nối lại | **Cần User duyệt AGPL + tài khoản** trước; đặt sớm (sau lát 2–3) để ô 3 không bị dồn cuối |

Inventory màn Career Mode và tính tĩnh/động (`verified` theo EA Pitch Notes: https://www.ea.com/en/games/ea-sports-fc/fc-26/news/pitch-notes-fc26-career-mode-deep-dive ; phân loại tĩnh/động là `assumption`):

| Nhóm màn | Ví dụ nội dung (theo EA) | Tĩnh/động |
|---|---|---|
| Manager Live Hub | Tab For You/ Continue/ Featured/ Social/ Popular/ Favourites/ Completed; Challenge Details (objectives, limiters, parameters, rewards); màn hoàn thành | Nhãn tĩnh; tên/ mô tả challenge đổi theo tuần/tháng |
| Unexpected Events & Emerging Scenarios | Popup toàn màn nút "I understand"; tin đồn truyền thông, lo lắng của cầu thủ; Decision Points ("Take immediate action") | Template + tên riêng |
| Hộp thư | Đề nghị hợp đồng, job offers, thông báo Staff Market làm mới 2 tuần/lần | Template + tên/ số |
| Squad/ Team Management | Gia hạn hợp đồng do cầu thủ chủ động, "Dismiss Contract Renewal", cầu thủ bị chặn, Player Stats + Average Ratings | Nhãn tĩnh + số |
| Chuyển nhượng & scouting | Transfer Limiters/ Embargoes, Global Transfer Network, scout report có tiềm năng, cho mượn cầu thủ vừa mua | Template + số tiền |
| Học viện trẻ | Youth Tournaments, sân trẻ | Hỗn hợp |
| Tập luyện | National Team Training Plans (Sharpness, Energy); drill Player Career; Archetypes/ perks | Nhãn tĩnh + mô tả perk tĩnh |
| Manager Market & mục tiêu | Sa thải/ chiêu mộ HLV, đơn xin việc, Board Objectives, nhãn SAFE/ VERY INSECURE | Hỗn hợp |
| Tin tức/ mạng xã hội | Bài kiểu ESPN FC, bài đăng mạng xã hội | Động nhất |
| Khác | Bảng xếp hạng nhiều giải, Tactical Vision, timestamp save, Switcher Menu, tutorial, Player Career Origin Stories | Phần lớn tĩnh |

Chưa xác minh bằng ảnh chụp từng màn (search ảnh trả về bài review, không có ảnh xem được — `unknown`); fixture User tự chụp sẽ thay thế.

---

## 9. Tài liệu cần cập nhật

- `CLAUDE.md:26` — phạm vi game đầu tiên ghi "menu, Ultimate Team, mô tả cầu thủ, chiến thuật" ⇒ sửa theo ô 2: Career Mode, chơi offline (Ultimate Team/ online ngoài đợt). Đang **mô tả sai** định hướng hiện tại.
- `CLAUDE.md:81` (module `engine/`) và `:82` (`mobile/`): nếu User duyệt hướng chiaki-ng trên PC thì ghi rõ "Windows: app chính thức (lát đầu) → client tự chạy (lát X)"; `mobile/` hiện được mô tả là bước kế tiếp ⇒ đề xuất hạ xuống "chưa lên kế hoạch" vì điện thoại không đáp ứng ô 3.
- `project-docs/automation/README.md`: thêm IP TV/PS5, model TV, cách cấp quyền overlay đã dùng, tên mục overscan/ Screen size của Sony, cài đặt PS5 (Remote Play, Hide Remote Play Connection Status, HDR, Adjust Display Area).
- `project-docs/present/backlog-and-techdebt.md`: ghi mục "overlay vẽ đè cả khi TV đang ở nguồn khác" (§3.2) nếu chưa giải trong đợt.
- README module `tv-overlay/`, `engine/` (khi tạo): giao thức `OverlayFrame`, giấy phép thư viện (`CLAUDE.md:43`), bẫy WGC (minimize), bẫy quyền overlay trên Sony.

---

## 10. Rủi ro và unknown

| # | Rủi ro/ unknown | Mức | Cách gỡ |
|---|---|---|---|
| R1 | Phiên Remote Play ngắt tay cầm đang chơi/ cần tài khoản phụ | Cao | User thử ngay khi bật Remote Play (Risk gate PSN) |
| R2 | OCR trên stream nén (720p/1080p) không đủ cho chữ nhỏ FC 26 | Cao | Đo trên fixture video Remote Play thật ở lát 0 |
| R3 | Overlay lệch/ trễ khi menu chuyển động gây cảm giác "méo xệch" | Cao | Máy trạng thái ẩn/hiện; User duyệt ở lát 1 |
| R4 | Màu mảng che lệch khi HDR | Trung bình | Kiểu `card`; hiệu chỉnh; hỏi HDR |
| R5 | TV giết app/ không tự chạy lại sau chờ | Trung bình | Miễn tối ưu pin; thử trên TV thật |
| R6 | Overlay vẽ đè khi TV ở nguồn khác | Trung bình | §3.2 |
| R7 | Laptop ngủ/ đóng nắp ⇒ mất lõi giữa buổi | Trung bình | Hỏi User laptop/desktop; cấu hình nguồn |
| R8 | Cập nhật app Sony/ firmware PS5 làm vỡ chụp cửa sổ hoặc client bên thứ ba | Trung bình | Không kiểm soát được; ghi backlog |
| R9 | Chi phí LLM phình do OCR sai tạo khoá mới | Trung bình | So khớp gần đúng + trần/ngày |
| U1 | PS Remote Play đã cài trên máy chưa | — | Sandbox chặn; User kiểm |
| U2 | Model/ SoC/ Android TV OS của TV | — | Hỏi User (§3.4) |
| U3 | Remote Play có ảnh hưởng HDR/ 120 Hz/ VRR của đường HDMI không | — | Không có tài liệu Sony; User quan sát ở lát 1 |
| U4 | Chụp nguồn HDMI trên TV có thật sự đen không | — | `screencap` khi có adb |

---

## 11. Đề xuất "dứt khoát KHÔNG làm" (đợt này)

> **[Round 2] Đã được thay bằng R2.6** (cập nhật theo kế hoạch User duyệt: chiaki-ng được phép ở lát X sau khi User duyệt AGPL + tài khoản).

- Không can thiệp PS5/ game, không capture card, không chèn gì vào đường HDMI (ràng buộc sẵn có).
- Không làm Ultimate Team, chế độ online, Kick-off, Pro Clubs — chỉ Career Mode (ô 2: "chơi offline là chính… nhất là career mode"). Các phần khác của app FC 26 ⇒ đợt sau.
- Không dịch bình luận/ lời thoại âm thanh, không đọc giọng (TTS) — Console Translator có, User không yêu cầu.
- Không dịch chữ **trong trận đấu** (scoreboard, popup khi đang đá) — động, trễ nhạy cảm nhất; đề xuất ngoài đợt (Leader xác nhận với User vì ô 2 nói "toàn bộ app").
- Không đặt lõi trên TV hay điện thoại; không làm module `mobile/`.
- Không nhúng/ link chiaki-ng vào lõi trong đợt này **cho tới khi User duyệt** (Risk gate copyleft + PSN); nếu duyệt thì chỉ chạy như chương trình riêng.
- Không hỗ trợ game thứ hai, không đa ngôn ngữ đích (chỉ EN → VI).
- Không phát hành Play Store, không tự cập nhật APK qua mạng, không bản build cho anh em bạn bè.
- Không server/ cloud riêng; không mở cổng ra Internet.
- Không bản dịch máy tạm hiển thị trước rồi thay (gây nhấp nháy).

---

## 12. Phát hiện ngoài phạm vi (KHÔNG tự đưa vào scope)

- **Bảng chuỗi tiếng Anh của FC 26 bản PC**: nếu trích được bảng chuỗi localization từ bản PC thì có thể dịch sẵn toàn bộ Career Mode trước khi chơi (cache gần như 100% trúng). Vướng EULA EA/ pháp lý, và User có thể không sở hữu bản PC ⇒ chỉ ghi nhận, User/Leader quyết.
- Console Translator có sẵn tính năng "Dịch menu"/ "Dịch vùng" trên chính TV này — có thể dùng làm **mốc so sánh** khi End User chấm chất lượng (cùng màn, hai sản phẩm).
- Nếu `screencap` nguồn HDMI trên TV **không** đen (U4), toàn bộ nhánh Remote Play có thể bỏ — một đợt kiến trúc riêng (Risk gate "lệch hướng kiến trúc").

---

## 13. Câu hỏi mở — điều Leader chưa hỏi, khái niệm gộp/ đặt tên sai, giả định nền có sai không

1. **Giả định nền sai một phần**: brief coi Console Translator là bằng chứng cho mức kỳ vọng ô 3 ("cài app trên TV rồi không cần làm gì"). Bằng chứng (D1) cho thấy sản phẩm đó cũng cần một lõi ngoài TV (iPhone/PC) đang chạy. Hoặc User đang dùng iPhone và quen mở app đó (thì ô 3 nghĩa là "không phải cài/ cấu hình lại", không phải "không mở gì"), hoặc User hiểu nhầm. **Phải hỏi User trước khi chốt lộ trình**: *"Khi chơi với Console Translator, anh có mở app trên iPhone/PC mỗi lần không?"*. Câu trả lời đổi thứ tự ưu tiên của lát X.
2. **Điều chưa ai hỏi — tài khoản PSN của phiên Remote Play**: tài khoản nào đăng nhập phiên Remote Play khi User đang chơi bằng tay cầm; có phải tạo tài khoản phụ không; tài khoản phụ có quyền dùng game của tài khoản chính không (không cần — Remote Play chỉ mirror màn hình — `assumption`). Đụng Risk gate PSN.
3. **Điều chưa ai hỏi — chữ đang hiện khi TV ở nguồn khác** (§3.2) và **khi trận đấu đang diễn ra** (overlay có được phép hiện giữa trận không).
4. **Khái niệm gộp**: "Remote Play" gộp giao thức / app Sony / chiaki-ng (§3.2). "Overlay over HDMI input" thực ra là overlay trên toàn màn hình. "Trễ ~0,5 s" (ô 6 ③) gộp hai trường hợp rất khác nhau: trúng cache và câu mới (D9). "Lát đầu dùng app chính thức để tránh AGPL" và "ô 3 không cần bật gì" là hai mục tiêu **mâu thuẫn**, không phải hai giai đoạn của cùng một hướng — ai đó phải chọn khi nào trả giá AGPL.
5. **A3 đặt câu hỏi sai trục**: câu hỏi thật không phải "PC hay TV hay điện thoại", mà là "**client Remote Play tự chạy hay không**". Có client tự chạy thì PC đã gần đạt ô 3 (vì ô 6 đã chấp nhận PC bật); không có thì đặt lõi ở đâu cũng trượt ô 3.
6. **Các câu cần User trả lời** (Leader hỏi từng câu, có đề xuất): (a) model TV chính xác + Android TV OS; (b) đã dùng Console Translator với iPhone hay PC, có mở mỗi lần không, đã cấp quyền overlay bằng cách nào; (c) máy tính là laptop hay desktop, có để bật/cắm sạc khi chơi được không; (d) PS5 có bật HDR không, đã chỉnh Adjust Display Area chưa; (e) đồng ý tạo tài khoản PSN phụ cho Remote Play không; (f) chấp nhận AGPL (chiaki-ng chạy như chương trình riêng) để đạt "không cần bật gì" không; (g) trần chi phí LLM/tháng.

---

## 14. Module và tên nhánh

- Module tạo trong đợt: `engine/` (Windows; **[Sửa ở Round 2]** .NET 10 — .NET 8 hết hỗ trợ 2026-11-10, xem R2.3; stack chốt ở Phase doc) và `tv-overlay/` (Android, Kotlin — `CLAUDE.md:80`). **Không** tạo `mobile/`.
- Nhánh (monorepo, một nhánh cho cả repo, theo `workflow/guidelines/git.md:43`: tiền tố `features|enhancements|bugs` + `YYMMDD` trùng thư mục đợt): **`features/261114-fc26-vietsub-overlay`**.

---

## Tự kiểm (handbook §4)

- [x] Mọi claim có nhãn · [x] claim `verified` có URL/ file:dòng/ lệnh + output · [x] claim cơ chế đọc chỗ định nghĩa (Android docs, TV.md, Sony) · [x] cấu trúc dữ liệu + ràng buộc nêu tầng chặn · [x] dữ liệu cũ: không có · [ ] mockup .html: **chưa** — brief round 1 cấm vẽ; đã nêu phạm vi mockup phase · [x] contract: không có consumer cũ · [x] test 2 tầng; dòng chưa chạy đánh dấu `chưa kiểm` · [x] ≥ 1 điểm phản biện (D1, D4, D9, A5, A3) · [x] trả lời câu hỏi mở (§13) · [x] ngoài phạm vi tách riêng (§12) · [x] chỉ ghi file này (kiểm `git status` cuối lượt).

---
---

# Round 2

**Ngày**: 2026-11-15 · **Phạm vi**: delta sau khi User duyệt kế hoạch (lõi PC; lát 1 = Hộp thư qua app PS Remote Play chính thức + WGC, User tự mở app; thêm nhóm màn mỗi lần cài; lát X "không cần bật gì" đặt sớm sau 2–3 nhóm, chiaki-ng chạy như chương trình riêng, AGPL + tài khoản PSN duyệt riêng khi tới). Phản biện stack Leader đề xuất, thiết kế tối giản lát 1, test cụ thể, metric, danh sách không làm, bảng lát, câu hỏi cho User.

Nhãn giữ như Round 1. Đầu vào đã đọc lượt này: `261114-workflow-log.md:38-39` (Leader verify D1, D2, D5, D7, giá — đúng), `261114-draft.md:23` (đính chính Console Translator).

## R2.1 Tóm tắt cho Leader

1. **Có thể vướng ngay ở lát 1, chưa ai kiểm**: app PS Remote Play chính thức đòi đăng nhập "the same account that you use for your console" (`verified`, trang Sony PC/Mac — §3.3). Nếu User đang chơi bằng DualSense với tài khoản chính mà PC kết nối Remote Play cùng tài khoản đó ⇒ có thể **ngắt tay cầm** như Console Translator cảnh báo (D5). Khi đó tài khoản PSN phụ (Risk gate) bị kéo lên **lát 1**, không phải lát X. Kiểm mất 5 phút, không cần code ⇒ User làm **trước khi** Dev bắt đầu (R2.7 câu Q1).
2. **Stack: giữ phần lớn, đổi 3 chỗ**: (a) **TV làm server, lõi làm client** — để lõi không phải mở cổng nghe trên Windows (Firewall chặn inbound mặc định và hiện hộp thoại cần quyền admin — `verified`); (b) **bỏ WebSocket/OkHttp cho lát 1**: TV là server thì OkHttp không giúp gì (OkHttp chỉ có WebSocket *client*); dùng **TCP + JSON mỗi dòng một gói** — không cần thư viện; (c) **target Android 15 không phải lý do thiết kế**: TV Sony dòng MediaTek cao cấp vẫn ở **Android TV 12**, dòng Realtek rẻ mới lên 14 (`verified-community`) ⇒ giới hạn FGS của Android 15 (D7) **không áp dụng trên TV này**; cái cần chốt là `minSdk` theo model TV thật.
3. **.NET 10 là đúng**: .NET 8 **đã hết hỗ trợ 2026-11-10**, .NET 10 LTS tới **2028-11-14** (`verified`). WinRT dùng qua TFM `net10.0-windows10.0.<build>.0` — **không cần thêm gói CsWinRT riêng** (`verified`).
4. **`Windows.Media.Ocr` cần gói OCR `en-US` đã cài**; không có thì `TryCreateFromLanguage` trả `null` (`verified`). Máy User chưa kiểm (brief giới hạn lệnh) ⇒ một lệnh kiểm cho Dev/User (R2.3).
5. **Lát 1 tối giản**: app console (không tray, không autostart), cache **file JSON**, không fuzzy matching, không mDNS, IP TV ghi tay trong file cấu hình, token tĩnh hiện trên màn TV. **Cần** ngay lát 1: FGS + miễn tối ưu pin trên TV (không có thì overlay chết khi chuyển sang HDMI — TV.md), luật ẩn/hiện chống nhấp nháy, gọi LLM thật có trần/ngày (Hộp thư trong save thật của User khác fixture ⇒ không thể chỉ dùng bản dịch sẵn).
6. **Việc chặn đầu tiên của cả đợt là fixture do User chụp** — mockup (bắt buộc trước code — handbook B6), golden test, làm ấm cache đều cần ảnh Hộp thư thật. Leader nên giao User chụp ngay (hướng dẫn R2.3.1).
7. **Lát X có rủi ro kế hoạch chưa ai nêu**: "lõi tự chạy cùng Windows + chiaki-ng chạy riêng rồi chụp cửa sổ" đòi một **phiên Windows đã đăng nhập, cửa sổ chiaki-ng không thu nhỏ**, và có thể **màn không khoá/ không tắt** — WGC không chạy trong Windows Service (Session 0) và hành vi khi khoá máy/ tắt màn là `unknown`. Phải spike đầu lát X; phương án thay là link thư viện chiaki-ng để lấy khung trực tiếp (lõi thành AGPL).

## R2.2 Thiết kế tối giản cho LÁT 1 (KISS)

Luồng lát 1: User mở PS Remote Play trên PC, kết nối PS5 → chạy `engine` (console) → engine chụp cửa sổ Remote Play bằng WGC ~2–4 khung/s → phát hiện khung đứng yên → OCR → tra cache JSON / gọi LLM cho câu mới → gửi `OverlayFrame` qua TCP tới TV → app TV vẽ.

| Thành phần | Lát 1 cần? | Lý do |
|---|---|---|
| Chụp WGC theo cửa sổ PS Remote Play | **Cần** | Đã duyệt. Tạo `GraphicsCaptureItem` từ HWND qua COM interop `IGraphicsCaptureItemInterop::CreateForWindow` (`assumption` — chưa thử). **Không** dùng `GraphicsCaptureItem.TryCreateFromWindowId`: hàm này yêu cầu `RequestAccessAsync(Programmatic)` + capability `graphicsCaptureProgrammatic` trong *package manifest* (`verified`: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscaptureitem.trycreatefromwindowid) ⇒ app không đóng gói MSIX không dùng được. Phương án đơn giản hơn nữa nếu interop vướng: `GraphicsCapturePicker` (User chọn cửa sổ mỗi lần chạy — chấp nhận được ở lát 1 vì User đã mở app tay) |
| Chế độ `replay` (đọc PNG/MP4 thay vì WGC) | **Cần** | Là nền của mọi test không có PS5 (R2.4) |
| Phát hiện khung đứng yên + luật ẩn/hiện | **Cần** | Cốt lõi của "không nhấp nháy, không méo xệch" (§3.8); thuần logic ⇒ TDD |
| `Windows.Media.Ocr` | **Cần** | PaddleOCR chỉ khi số đo trên fixture kém (đồng ý với Leader) |
| Chuẩn hoá câu | **Một phần**: khoảng trắng, dấu nháy, che số/ tiền/ ngày/ % bằng regex | Che **tên riêng** bằng slot template ⇒ hoãn tới lát Tin tức (lượng câu có tên mới lớn). Ở Hộp thư lát 1 mỗi email mới tốn một lần gọi LLM — vài email/ngày, không đáng kể |
| Cache: **file JSON** (đọc vào `Dictionary`, ghi lại cả file khi thêm) | **Cần — JSON, không SQLite** | Lát 1 chỉ vài trăm mục; JSON đọc được bằng mắt, End User review/ sửa trực tiếp, diff được trong git. Đổi sang SQLite khi số mục > vài nghìn hoặc ghi file chậm thấy được (đo ở lát Tin tức) |
| Glossary JSON trong repo | **Cần** (nhỏ) | Thuật ngữ Hộp thư (contract, transfer, board…) |
| LLM cho câu mới + trần ký tự/ngày + khoá API từ biến môi trường | **Cần** | Bắt buộc theo `CLAUDE.md:45`; nhà cung cấp = Risk gate ⇒ chặn trước khi code phần dịch (Q2). Test dùng giả lập |
| Chỉ dịch câu OCR **giống nhau 2 lần liên tiếp** | **Cần** (thay cho fuzzy matching) | Chặn khoá rác do OCR nhiễu khung-tới-khung, rẻ hơn nhiều so với so khớp gần đúng |
| Fuzzy matching | **Hoãn** | Chỉ làm khi số đo chi phí/ tỉ lệ trúng cache ở lát sau cho thấy cần |
| Kênh LAN: TCP, mỗi dòng một JSON `OverlayFrame` | **Cần** | Xem R2.3 (TV là server) |
| Token ghép cặp | **Cần — bản tĩnh**: TV sinh token ngẫu nhiên lần đầu, hiện trên màn app TV; User chép vào file cấu hình engine | ~vài chục dòng; TV đang nghe một cổng trong LAN (khách, thiết bị IoT) ⇒ đáng giữ. **Không** làm mã 8 số đổi mỗi 60 s như Console Translator |
| mDNS/ tự dò TV | **Hoãn** (lát X) | Lát 1: IP TV ghi tay trong `engine.local.json` (gitignore — `CLAUDE.md` mục Môi trường); khuyên User đặt IP cố định cho TV ở router (Q4) |
| Tray app, autostart Windows | **Hoãn** (lát X) | Lát 1 User mở tay; console app + log ra màn hình là đủ, dễ gỡ lỗi |
| Màn hiệu chỉnh trên TV | **Hoãn** — thay bằng lệnh `engine pattern` gửi 4 dấu góc + lưới | Nếu lệch, chỉnh offset/ scale trong file cấu hình engine. Màn hiệu chỉnh có UI chỉ làm khi thật sự cần |
| App TV: FGS giữ cửa sổ overlay + miễn tối ưu pin | **Cần** | Không có thì TV giết app khi chuyển sang HDMI (TV.md — `verified` Round 1 D2) |
| App TV: tự chạy khi bật TV (`BOOT_COMPLETED`) | **Hoãn** (lát X) | Lát 1 User mở app TV một lần mỗi khi TV khởi động lại |
| App TV: màn chính | **Cần — tối giản**: trạng thái (chờ lõi/ đã kết nối), `IP:cổng`, token, 2 nút hướng dẫn cấp quyền overlay & bỏ tối ưu pin, nút Tắt | Vẫn là màn người dùng ⇒ cần mockup (handbook B6) |

**Over-engineering cho lát 1**: tray + autostart, SQLite, fuzzy matching, mDNS, WebSocket + OkHttp, mã ghép cặp xoay vòng, màn hiệu chỉnh có UI, boot start, slot template cho tên riêng, PaddleOCR/ONNX dựng sẵn.

## R2.3 Phản biện stack

### Engine (Windows)

| Lựa chọn của Leader | Kết luận | Lý do + nguồn |
|---|---|---|
| C# / .NET 10 | **Giữ** | .NET 8: hết hỗ trợ **2026-11-10**; .NET 9 cũng hết 2026-11-10; .NET 10 LTS phát hành 2025-11-11, hỗ trợ tới **2028-11-14**, bản vá mới nhất 10.0.15 (`verified`: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core, đọc 2026-11-15). Máy hiện chỉ thấy `dotnet.exe` bản host 8.0.3026 (Round 1 §3.1) ⇒ User cài .NET 10 SDK. Chưa chạy `dotnet --list-sdks` (ngoài danh sách lệnh được phép) — `unknown` máy đã có SDK nào |
| WinRT từ .NET 10 | **Giữ — qua TFM, không thêm gói CsWinRT** | Đặt `<TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>` (hoặc `...26100.0`); TFM tự tham chiếu gói `Microsoft.Windows.SDK.NET.Ref` lúc build (`verified`: https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps). Máy là Windows 11 build 26100 (môi trường phiên) ⇒ `22621` hoặc `26100` đều được. Interop COM cho WGC theo HWND và cho D3D11 device (`CreateDirect3D11DeviceFromDXGIDevice`) phải tự khai báo hoặc dùng thư viện (vd Vortice.Direct3D11 — `assumption`, ghi giấy phép vào README module) |
| Windows.Graphics.Capture | **Giữ** | Xem R2.2 dòng 1. Lưu ý từ tài liệu: máy bật HD color thì định dạng điểm ảnh có thể không phải BGRA8 (ảnh bị bạc màu) — dùng `R16G16B16A16Float` nếu cần (`verified`: https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture); viền vàng quanh cửa sổ đang chụp là bình thường |
| `Windows.Media.Ocr` trước, PaddleOCR/ONNX nếu kém | **Giữ** | **Cần gói OCR en-US**: `TryCreateFromLanguage` trả `null` nếu ngôn ngữ không có trên máy (`verified`: https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine.trycreatefromlanguage). Kiểm (không cần admin, `chưa kiểm`): PowerShell 5.1 `[Windows.Media.Ocr.OcrEngine,Windows.Foundation,ContentType=WindowsRuntime] > $null; [Windows.Media.Ocr.OcrEngine]::AvailableRecognizerLanguages \| Select LanguageTag`. Thiếu ⇒ User chạy PowerShell admin: `Get-WindowsCapability -Online \| Where-Object Name -like 'Language.OCR*en-US*' \| Add-WindowsCapability -Online` (`verified-community`: tài liệu PowerToys Text Extractor https://learn.microsoft.com/windows/powertoys/text-extractor). Máy User nếu chỉ cài giao diện tiếng Việt thì rất có thể thiếu gói này (`assumption`). Engine phải báo lỗi rõ khi `null`, không im lặng |
| Glossary JSON trong repo | **Giữ** | Có test kiểm trùng khoá (Round 1 §4.3) |
| Cache SQLite | **Đổi → JSON cho lát 1** | R2.2. Giữ SQLite làm hướng sau, quyết bằng số đo |
| LLM câu mới (Haiku 5.5 / Sonnet 5.5) | **Giữ; đề xuất Sonnet 5.5 cho lát 1** | Lát 1 lượng câu nhỏ (Hộp thư) ⇒ chi phí cỡ cent (`assumption` theo bảng giá đã Leader verify); chất lượng ("chuẩn") quan trọng nhất ở lần User duyệt đầu. So Haiku vs Sonnet bằng End User trên cùng bộ câu fixture trước lát Tin tức — lúc đó chi phí mới đáng kể |
| WebSocket qua LAN + pairing token | **Đổi → TCP + JSON-dòng; TV là server** | Xem bảng dưới |
| Tray app + autostart | **Hoãn → lát X** | R2.2 |
| xUnit + fixture ảnh FC 26 | **Giữ** | Thêm chế độ `replay` để test end-to-end không cần PS5 |

**Bên nào làm server**

| | TV làm server (đề xuất) | Engine làm server |
|---|---|---|
| Firewall Windows | Engine chỉ kết nối **ra** ⇒ không cần quy tắc firewall | Windows Firewall mặc định **chặn inbound**; lần đầu app nghe cổng hiện hộp thoại, user không phải admin hoặc bấm nhầm ⇒ tạo luật **chặn** và phải xoá luật mới hiện lại (`verified`: https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules) |
| Nhập địa chỉ | IP TV hiện trên màn app TV, User gõ vào file cấu hình trên PC bằng bàn phím | User phải gõ IP PC bằng remote TV |
| Tiền lệ | Console Translator: TV hiện IP + mã, thiết bị lõi kết nối tới TV (`verified` TV.md) | — |
| Emulator | Phải `adb -s emulator-5556 forward tcp:<cổng> tcp:<cổng>` để PC kết nối vào emulator (`assumption` — cơ chế NAT chuẩn của emulator; `chưa kiểm`) | Emulator gọi `10.0.2.2` |
| Thư viện | Android: `java.net.ServerSocket` có sẵn; .NET: `TcpClient` có sẵn | — |

WebSocket không mang lại gì ở đây: một kết nối, một chiều, gói nhỏ, LAN. OkHttp chỉ có WebSocket client ⇒ nếu TV là server thì phải thêm thư viện server khác (`assumption` — OkHttp không cung cấp WebSocket server ngoài `MockWebServer` dùng cho test). Đề xuất: TCP, mỗi gói là **một dòng JSON** (`\n` kết thúc), dòng đầu là `{"hello":{"v":1,"token":"..."}}`; TV đóng kết nối nếu sai token. Ghi định dạng vào README `tv-overlay/` (Quy tắc vàng 5).

### tv-overlay (Android)

| Lựa chọn | Kết luận | Lý do |
|---|---|---|
| Kotlin | **Giữ** | `CLAUDE.md:80` |
| "Target Android 15" | **Đổi cách nói: `targetSdk` 35 được, nhưng chốt `minSdk` theo TV thật** | TV Sony dòng MediaTek (OLED/ miniLED cao cấp) vẫn Android TV 12; đợt Android TV 14 (từ 08/2025) chỉ cho dòng Realtek phổ thông; một số máy còn ở Android 10/11 (`verified-community`: https://www.androidauthority.com/sony-bravia-tv-android-tv-14-update-with-google-tv-3585790 , https://www.channelnews.com.au/sony-updates-entry-level-tvs-to-android-14-excludes-premium-models/). Hành vi đổi theo `targetSdk` chỉ áp dụng khi máy chạy OS ≥ bản đó ⇒ trên TV này các giới hạn Android 15 (D7) **không có hiệu lực** (vẫn viết đúng thứ tự "overlay hiện trước, FGS sau" cho an toàn). Đề xuất `minSdk 29` (Android 10 — Google TV của Sony ra đời trên Android 10, `assumption`) tới khi biết model; Console Translator hỗ trợ từ Android 9 (`verified` TV.md). Sideload không bị yêu cầu targetSdk của Play |
| `TYPE_APPLICATION_OVERLAY` + View tự vẽ Canvas + `StaticLayout` (không Compose) | **Giữ** | Toàn quyền đo chữ để auto-fit, không cần lifecycle owner như Compose trong cửa sổ `WindowManager` (`assumption` — ComposeView gắn ngoài Activity phải tự gắn `ViewTreeLifecycleOwner`/`SavedStateRegistryOwner`). Cờ cửa sổ: `FLAG_NOT_FOCUSABLE \| FLAG_NOT_TOUCHABLE \| FLAG_LAYOUT_NO_LIMITS`, nền trong suốt (`PixelFormat.TRANSLUCENT`) — `chưa kiểm` |
| Be Vietnam Pro nhúng | **Giữ** | Giấy phép OFL (`assumption` — kiểm file LICENSE khi thêm, ghi vào README module) |
| OkHttp WebSocket client | **Bỏ** | TV là server ⇒ `ServerSocket` có sẵn |
| FGS + boot start + miễn tối ưu pin | **Tách**: FGS + miễn pin **lát 1**; boot start **lát X** | Loại FGS: chọn loại không bị chặn khi khởi từ `BOOT_COMPLETED` (không dùng `dataSync`/`mediaPlayback`/`mediaProjection` — `verified` Round 1 D7); ứng viên `specialUse` (`assumption`) |
| JUnit + Android TV emulator | **Giữ** | Logic auto-fit/ parse gói là JVM unit test thuần; emulator cho kiểm vẽ + kết nối. Trên CPU AMD, emulator cần Windows Hypervisor Platform (`assumption`; máy có Docker Desktop/WSL trong PATH ⇒ có thể đã bật Hyper-V — Round 1 §3.1 output `which`) |

### Công cụ User cài

- Android Studio (kèm SDK, `adb`, JDK đi kèm, image **Android TV x86_64**) · .NET 10 SDK · (nếu thiếu) gói OCR en-US. Thêm: **không** cần Visual Studio — `dotnet` CLI + VS Code/ Rider đủ (`assumption`).

## R2.4 Kế hoạch test lát 1

### R2.4.1 Fixture User phải chụp (việc chặn đầu tiên)

Điều kiện: FC 26 ngôn ngữ **English**, một save Career Mode có ≥ 5 email trong Hộp thư, gồm ít nhất 1 email dài (hợp đồng/ đề nghị chuyển nhượng/ thông báo ban lãnh đạo).

| # | Chụp gì | Cách | Để làm gì |
|---|---|---|---|
| F1 | 3–5 ảnh **cửa sổ PS Remote Play** trên PC: danh sách Hộp thư (đầu, giữa, cuối danh sách), 3 email mở khác nhau, 1 ảnh có mục đang chọn ở dòng khác | Mở PS Remote Play, kết nối, để cửa sổ ~1920×1080 (hoặc toàn màn hình); `Win+Shift+S` → chế độ cửa sổ, hoặc Snipping Tool; lưu PNG | **Đúng thứ engine sẽ thấy** (đã qua nén stream) ⇒ golden test OCR, làm ấm cache |
| F2 | Cùng các màn đó chụp bằng **nút Create của PS5** (ảnh PNG) | PS5: *Settings → Captures and Broadcasts → Captures → Screenshot Format* chọn PNG; bấm Create → Take Screenshot; chép ra USB (Media Gallery → Copy to USB) hoặc tự động sang app PS | **Đúng thứ TV hiển thị** ⇒ nền cho mockup và render so sánh trước/sau. PS5 bật HDR thì ảnh chụp có thể khác màu TV (`unknown`) |
| F3 | Video 30–60 s cửa sổ PS Remote Play: mở Hộp thư → cuộn lên xuống → mở 2 email → quay lại → để yên 10 s | Snipping Tool → Record (Windows 11), lưu MP4 | Test luật ẩn/hiện, phát hiện khung đứng yên, chống nhấp nháy |
| F4 | Ảnh chụp **màn hình TV bằng điện thoại** khi đang ở Hộp thư (đứng ở chỗ ngồi chơi) | Điện thoại | Ước cỡ chữ đọc được từ xa cho mockup |

Đặt vào `engine/tests/fixtures/fc26/inbox/` (repo private — commit được; khi repo chuyển public phải xét lại bản quyền EA — Round 1 §7). **Không** dùng ảnh tải từ web cho golden test.

### R2.4.2 Tầng 1 — smoke KHÔNG cần PS5 (Dev chạy, Reviewer chạy lại)

Tất cả `chưa kiểm` (chưa có code). Tên lệnh là đề xuất, Phase doc chốt.

| # | Bước | Lệnh (đề xuất) | Kỳ vọng |
|---|---|---|---|
| T1 | Kiểm gói OCR en-US | lệnh PowerShell ở R2.3 | Có `en-US` trong danh sách; nếu không ⇒ dừng, báo User cài |
| T2 | Unit test thuần | `dotnet test engine/` | Xanh: chuẩn hoá, khoá cache, phát hiện khung đứng yên, máy trạng thái ẩn/hiện, ánh xạ toạ độ + dò viền đen, validator slot, (de)serialize gói |
| T3 | Replay OCR trên F1 | `dotnet run --project engine -- replay --input engine/tests/fixtures/fc26/inbox --out out/replay` | `out/replay/<ảnh>.json` có từng đoạn chữ + bbox chuẩn hoá; so golden: ≥ 95% đoạn chữ đúng nguyên văn |
| T4 | Replay dịch với LLM **giả lập** + cache fixture | thêm `--translator fake` | Mọi đoạn trúng cache fixture có bản Việt; đoạn không trúng ghi vào `out/replay/misses.json`; **0** lệnh gọi mạng (kiểm log) |
| T5 | Render offline trước/sau | `... replay --render` | `out/replay/<ảnh>.after.png` — Dev + Reviewer xem bằng mắt: không tràn, không lệch, đủ dấu |
| T6 | Replay video F3 | `... replay --input F3.mp4` (khung tách bằng ffmpeg có sẵn trên máy) | Log trạng thái: ẩn khi cuộn, hiện khi đứng yên ≥ ngưỡng; **0** lần đổi chữ khi đứng yên 10 s |
| T7 | Unit test app TV | `./gradlew :tv-overlay:testDebugUnitTest` (từ thư mục module) | Xanh: auto-fit (câu Việt dài nhất trong fixture, 1–3 dòng, cỡ tối thiểu), parse gói, từ chối token sai/ `v` lạ |
| T8 | Emulator: cài + quyền | bật AVD Android TV theo `workflow/guidelines/blackbox-android-adb.md` §2, §7; `adb -s emulator-5556 install -r <apk>`; `adb -s emulator-5556 shell appops set <pkg> SYSTEM_ALERT_WINDOW allow` | App mở, hiện IP/ cổng/ token |
| T9 | Emulator: gửi gói | mở một video FC 26 toàn màn hình trên emulator (thay nguồn HDMI); `adb -s emulator-5556 forward tcp:<cổng> tcp:<cổng>`; `... replay --input ... --send 127.0.0.1:<cổng> --token <token>` | `adb -s emulator-5556 exec-out screencap -p > out/emu.png`: chữ Việt đè đúng chỗ; overlay không ăn phím (bấm `input keyevent` vẫn điều khiển video); ngắt engine ⇒ overlay tự xoá sau `ttlMs` |
| T10 | Pattern | `... pattern --send ...` | 4 dấu góc nằm sát 4 góc màn emulator |

**Dữ liệu**: `out/` gitignore, xoá thoải mái; cache fixture (`engine/tests/fixtures/.../cache.json`) commit và giữ; cache thật của User ở thư mục dữ liệu ứng dụng trên máy User (`%LOCALAPPDATA%`), không commit. Không gọi LLM thật trong test (Risk gate).
**Reviewer chạy lại smoke**: **có** (T2–T10) — tích hợp WGC/OCR/Android, dễ "xanh trên máy Dev". Ngoài ra Reviewer chạy **một lần** replay với LLM thật trên F1 **chỉ khi** User đã duyệt nhà cung cấp và khoá (Q2), để kiểm prompt/ slot.

### R2.4.3 Chỉ User làm được trên TV thật (checklist — Leader đưa lệnh, User chạy)

Trước khi có code (làm ngay):
1. Bật Remote Play trên PS5; cài PS Remote Play trên PC; **đang chơi FC 26 bằng DualSense, kết nối Remote Play từ PC bằng cùng tài khoản** ⇒ ghi lại: tay cầm có bị ngắt không, PC có stream được khi **không** cắm tay cầm vào PC không (Q1). `chưa kiểm`
2. Bật "Hide Remote Play Connection Status" trên PS5 nếu thông báo gây vướng. `chưa kiểm`
3. Ghi model TV + phiên bản Android TV (Cài đặt → Hệ thống → Giới thiệu) vào `project-docs/automation/README.md` (Q3). `chưa kiểm`
4. Chụp fixture F1–F4. `chưa kiểm`

Khi có bản build lát 1:
5. Bật chế độ nhà phát triển + gỡ lỗi qua mạng trên TV, ghi IP (Risk gate — User làm). `chưa kiểm`
6. Cài APK (`adb connect <ip>:5555` rồi `adb -s <ip>:5555 install -r <apk>`), cấp "Hiển thị trên ứng dụng khác" qua cài đặt; không có mục ⇒ `adb -s <ip>:5555 shell appops set <pkg> SYSTEM_ALERT_WINDOW allow`; bỏ tối ưu pin. `chưa kiểm`
7. (5 phút, một lần) Ở nguồn HDMI PS5, chạy `adb -s <ip>:5555 exec-out screencap -p > hdmi.png` — đen hay thấy hình game (Round 1 U4). `chưa kiểm`
8. Mở app TV, chép token + IP vào `engine.local.json`; chạy `engine pattern` ⇒ 4 dấu góc trùng 4 góc hình game; lệch thì báo Leader. `chưa kiểm`
9. Mở PS Remote Play, vào Hộp thư; chạy `engine`. Xem 10 phút: chữ Việt đúng chỗ, đẹp, không nhấp nháy; cuộn danh sách, mở/đóng email. `chưa kiểm`
10. Chuyển nguồn HDMI ↔ Home ↔ HDMI; tắt/bật TV (chờ); overlay có còn/ có tự xoá đúng không. `chưa kiểm`
11. Đo "chữ ma": quay màn TV bằng điện thoại chế độ slow-motion khi bấm sang email khác ⇒ đếm khung tới khi chữ cũ biến mất và chữ mới hiện. `chưa kiểm`
12. Chơi một trận ngắn: game có "dính tay" không (chứng minh không đụng đường hình); overlay có hiện giữa trận không (không được — R2.6). `chưa kiểm`

### R2.4.4 Tầng 2 — End User (chất lượng bản dịch)

- **Khi**: sau khi Dev chạy replay với LLM thật trên F1 (cần Q2), **trước** khi đóng gói bản cài lát 1; lặp lại mỗi lát.
- **Đầu vào**: bảng `câu gốc | bản Việt | slot | ảnh` (xuất từ cache) + ảnh render trước/sau T5 + glossary.
- **Đạt khi**: 100% câu xuất hiện trên fixture được End User đánh `reviewed` hoặc đã sửa; 0 lỗi thuật ngữ so với glossary; 0 sai biến (tên, số, tiền, ngày); 0 câu dài quá khung sau auto-fit ở cỡ tối thiểu; giọng văn nhất quán (cách xưng hô với "HLV/ ông/ anh" chốt trong glossary); tên CLB/ giải/ cầu thủ không dịch.

## R2.5 Metric nghiệm thu lát 1

**User tự chấm trên TV** (ngồi chỗ chơi thật, ~3 m):
1. Ở danh sách Hộp thư và trong email: không còn chữ tiếng Anh ở các vùng đã dịch; chữ Việt trông như của game (cỡ, màu, căn lề, đậm/nhạt khớp), đọc rõ, dấu không vỡ.
2. Không chữ nào tràn khung hay bị cắt "…".
3. Để yên 30 s: không nhấp nháy, không nhảy chữ.
4. Cuộn/ chuyển email: chữ cũ biến mất gọn, chữ mới hiện lại mượt, không có chữ Việt nằm đè sai dòng quá một thoáng.
5. Game không trễ tay.
6. User nói "được, đáng ngồi chơi thử" (ô 3).

**Đo được** (đều `chưa kiểm`; ngưỡng là đề xuất cho User duyệt):
| Metric | Ngưỡng | Đo bằng |
|---|---|---|
| OCR đúng nguyên văn trên F1 | ≥ 95% đoạn | T3 |
| Câu trên fixture qua End User | 100% | R2.4.4 |
| Trễ khi trúng cache (từ lúc engine thấy khung đứng yên tới khi TV vẽ xong) | p50 ≤ 0,5 s · p95 ≤ 0,8 s | timestamp engine + gói ack từ TV |
| Trễ câu mới (LLM) | ≤ 3 s (tạm; chỉnh sau khi đo) | log engine |
| "Chữ ma" sau khi chuyển màn trên TV | ≤ 0,3 s (tạm) | bước 11 (slow-motion) |
| Đổi chữ khi màn đứng yên 60 s | 0 | T6 + bước 9 |
| Ký tự gửi LLM/ngày | ≤ trần User duyệt | log engine |

## R2.6 Dứt khoát KHÔNG làm trong đợt này (thay §11 Round 1)

- Không can thiệp PS5/ game, không capture card, không chèn vào đường HDMI, không nhận/ lưu mật khẩu PSN (ràng buộc sẵn có).
- Không đặt lõi trên TV hay điện thoại; không tạo module `mobile/`; không dùng app PS Remote Play cho Android TV.
- Không Ultimate Team, online, Kick-off, Pro Clubs, Rush — chỉ Career Mode (ô 2).
- Không dịch chữ **trong trận** (tỉ số, popup khi đang đá, replay) — động và nhạy trễ nhất; overlay phải **im lặng** khi đang trong trận. Lý do giữ ngoài: không có lý do mới để đưa vào.
- Không dịch âm thanh/ bình luận, không TTS.
- Không game thứ hai, không ngôn ngữ đích khác tiếng Việt.
- Không Play Store, không tự cập nhật APK, không bản build cho người khác (chia sẻ = Risk gate, có thể kéo theo AGPL).
- Không server/ cloud riêng, không mở cổng ra Internet, không mDNS cho tới lát X.
- Không hiện bản dịch tạm rồi thay (nhấp nháy).
- **Trong lát 1 cụ thể**: không tray, không autostart, không boot start TV, không SQLite, không fuzzy matching, không slot tên riêng, không màn hiệu chỉnh có UI, không PaddleOCR dựng sẵn.
- **chiaki-ng**: không đụng tới trước lát X; tới lát X chỉ chạy như chương trình riêng **sau khi** User duyệt AGPL + tài khoản (đã chốt) — trừ khi spike đầu lát X chứng minh cách đó không chạy được, khi ấy Leader đưa lại lựa chọn cho User.

## R2.7 Bảng lát (cập nhật theo thứ tự User duyệt — thay §8 Round 1)

Mọi lát có màn người dùng ⇒ **mockup trước code, User duyệt** (handbook B6). Với overlay, "mockup" = prototype HTML khung 16:9, nền là fixture F2 thật, chữ Việt vẽ đè ở các trạng thái (chờ dịch · đã dịch · đang động/ẩn · mục đang chọn · Overflow · mất kết nối).

| Lát | Nội dung | Mockup trước? | Cài lên TV? | TDD | Chặn bởi |
|---|---|---|---|---|---|
| 0 | Fixture F1–F4 (User) · spike WGC theo HWND + OCR trên F1 (đo) · APK "hello overlay" vẽ pattern 4 góc trên HDMI (tuỳ User — Q8) | Không (spike) | Chỉ APK pattern, nếu User đồng ý | — | Q1, Q3, fixture |
| 0.5 | **Mockup** lát 1: Hộp thư (danh sách + email) + màn chính app TV; định hướng thiết kế (Round 1 §5) | — | Không | — | Fixture F2, F4 |
| **1** | **Hộp thư** đẹp/chuẩn; engine console + replay; LLM thật có trần; app TV (FGS, miễn pin, server TCP, token) | Có (0.5) | **Có — lần cài đầu (ô 5)** | Chuẩn hoá, cache, đứng yên/ẩn-hiện, ánh xạ, auto-fit, gói, validator slot | Q2 (nhà cung cấp LLM) |
| 2 | Career Hub, tab, Switcher Menu, menu tạm dừng | Có | Có | `short_vi` glossary | — |
| 3 | Squad Hub/ Team Management, hồ sơ & chỉ số cầu thủ | Có | Có | Phân loại vùng chỉ số | — |
| **X** | "Không cần bật gì": spike WGC khi khoá máy/ tắt màn/ cửa sổ chiaki-ng (đầu lát) → engine tray + autostart Windows → chiaki-ng tự kết nối → boot start app TV → mDNS/ tự dò | Có (tray menu + thông báo trạng thái; màn TV khi mất lõi) | Có | Logic phát hiện PS5 bật/tắt, kết nối lại | User duyệt AGPL + tài khoản PSN (Risk gate) |
| 4 | Chuyển nhượng: Transfer Hub, Global Transfer Network/ scout report, đàm phán, gia hạn, cho mượn | Có | Có | Che tiền tệ phức tạp | — |
| 5 | Unexpected Events, Decision Points, Board Objectives, Manager Market | Có | Có | — | — |
| 6 | Tin tức & mạng xã hội — **thêm slot tên riêng + xem lại SQLite/ fuzzy theo số đo** | Có | Có | Slot template | Số đo chi phí từ lát 1–5 |
| 7 | Tập luyện/ phát triển, học viện trẻ, National Team Training Plans | Có | Có | — | — |
| 8 | Manager Live, cài đặt, tutorial | Có | Có | — | — |

Ghi chú: thứ tự 2 ↔ 3 có thể đổi; X đặt sau 2–3 nhóm đúng như User duyệt (ở bảng: sau lát 3 = nhóm thứ 3 tính cả Hộp thư).

## R2.8 Câu hỏi chỉ User trả lời được (kèm đề xuất)

| # | Câu hỏi | Đề xuất | Chặn gì |
|---|---|---|---|
| Q1 | Thử ngay: đang chơi bằng DualSense, PC kết nối PS Remote Play cùng tài khoản ⇒ tay cầm có bị ngắt? PC có stream được khi không cắm tay cầm? | Làm ngay trước khi code. Nếu bị ngắt ⇒ tạo tài khoản PSN phụ từ lát 1 (Risk gate, User tự đăng ký trên trang Sony) | Lát 1 |
| Q2 | Nhà cung cấp LLM + trần chi phí cho lát 1 | Claude Sonnet 5.5, khoá API trong biến môi trường máy User, trần 100.000 ký tự/ngày (với Hộp thư thì thực tế dùng rất ít) | Phần dịch của lát 1 |
| Q3 | Model TV + phiên bản Android TV | Đọc ở Cài đặt → Hệ thống → Giới thiệu, gửi Leader | `minSdk`, kỳ vọng hiệu năng |
| Q4 | Đặt IP cố định cho TV (và PC) trên router? | Có — để lát 1 không phải sửa cấu hình khi IP đổi | Tiện lát 1, bắt buộc tới lát X nếu không làm mDNS |
| Q5 | Windows có gói OCR en-US chưa (User chạy lệnh R2.3 nếu Dev không chạy được) | Nếu thiếu: cài bằng PowerShell admin | Lát 1 |
| Q6 | PS5 có bật HDR? | Giữ như User đang chơi; mockup sẽ có kiểu che chịu được lệch màu (`card`) | Mockup |
| Q7 | Xưng hô trong bản dịch (game gọi người chơi là "Coach/ Boss/ you"): "HLV", "ông", "anh", "sếp"? | "HLV" trong nhãn, "ông" trong email trang trọng — End User đề xuất, User chốt khi duyệt mockup | Tầng 2 |
| Q8 | Có muốn một APK "hello overlay" (chỉ vẽ 4 dấu góc lên hình PS5) trước lát 1 để khử rủi ro quyền overlay/ căn góc sớm, dù lệch nhẹ ô 5 ("lần cài đầu = một màn đẹp")? | Có — 1 lần cài 5 phút, phát hiện sớm TV khoá quyền hoặc lệch toạ độ | Lát 0 |
| Q9 | Máy tính là laptop hay desktop; có để máy không ngủ, không khoá khi chơi được không? | Hỏi lúc vào lát X; ghi trước để Leader biết rủi ro R2.1 điểm 7 | Lát X |

## R2.9 Câu hỏi mở

- **Leader chưa hỏi**: (1) xung đột tài khoản/ tay cầm **với app chính thức ở lát 1** (Q1) — kế hoạch đang ngầm giả định chỉ lát X mới đụng tài khoản PSN phụ; (2) gói OCR en-US trên Windows (nếu thiếu, OCR trả `null` ngay buổi đầu); (3) bên nào nghe cổng và Firewall Windows; (4) đo trễ/ "chữ ma" trên TV thật bằng cách nào (đề xuất quay slow-motion); (5) WGC cần phiên Windows đã đăng nhập ⇒ lõi **không thể** là Windows Service, và chưa biết WGC có chạy khi khoá máy/ tắt màn không — quyết định khả thi của lát X; (6) fixture là việc chặn đầu tiên, phải giao User trước cả mockup.
- **Khái niệm bị gộp/ đặt tên sai**: "WebSocket over LAN with a pairing token" gộp ba việc khác nhau — giao vận, xác thực, tìm thiết bị (mDNS); "foreground service + boot start" gộp nhu cầu lát 1 (FGS) với nhu cầu lát X (boot); "target Android 15" gộp `targetSdk` với OS thật của TV (TV này Android 10–14, không phải 15); "C# .NET 10 + CsWinRT" — CsWinRT không phải lựa chọn riêng, nó đi kèm TFM; "tray app with autostart" được liệt như nền tảng của engine trong khi nó thuộc lát X; "Inbox" — tên màn trong FC 26 có thể không đúng chữ "Inbox" (Pitch Notes chỉ nói "inbox emails"), xác nhận bằng fixture.
- **Phần kế hoạch đã duyệt có thể sai**:
  1. Giả định "lát 1 không đụng tài khoản PSN" — chưa kiểm, có thể sai (Q1).
  2. Giả định lát X "chiaki-ng chạy riêng + chụp cửa sổ" đáp ứng được "không cần bật gì" — có thể sai nếu máy khoá/ tắt màn làm WGC ngừng, hoặc cửa sổ chiaki-ng không thu nhỏ được mà vẫn chụp; lúc đó còn hai đường: (a) cấu hình PC không khoá/ không tắt màn khi cắm sạc (đánh đổi bảo mật của laptop), (b) link thư viện chiaki-ng để lấy khung trực tiếp không cần cửa sổ (lõi thành AGPL — chia sẻ phải công bố mã lõi). User nên biết trước rằng lát X có thể đòi một trong hai đánh đổi này.
  3. "Mỗi màn người dùng cần mockup" áp vào overlay nghĩa là mockup **phụ thuộc fixture** — nếu User chưa chụp được (vd PS5 chặn chụp ở màn nào đó), lát đó đứng. Không sai, nhưng phải đưa vào lộ trình như phụ thuộc thật.

## R2.10 Những chỗ Round 1 đã sửa (đánh dấu trong thân bài)

- §3.5 + D7: thêm ghi chú — giới hạn Android 15 không áp dụng trên TV Sony hiện tại (Android 10–14).
- §4.4: kênh LAN = TCP + JSON-dòng, TV là server.
- §8, §11: bị thay bởi R2.7, R2.6.
- §14: .NET 10 thay .NET 8.

## R2.11 Tự kiểm

- [x] Claim có nhãn + URL/ lệnh · [x] lệnh trên máy: lượt này chỉ chạy `git status --short` (kiểm phạm vi ghi) — lệnh kiểm OCR để `chưa kiểm` vì brief giới hạn danh sách lệnh · [x] mọi bước test chưa chạy đánh dấu `chưa kiểm` · [x] phản biện ≥ 1 (R2.1, R2.3, R2.9) · [x] câu hỏi mở trả lời · [x] chỉ sửa file report (kiểm `git status` cuối lượt).
