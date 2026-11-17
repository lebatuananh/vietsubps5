DEV-3A DONE
# 261114 — Epic 1 Phase 3 — Dev report 3a (`engine/` Bước 1–4)

**Phạm vi**: Phase doc Rev 1.2 *Kế hoạch* 1–4 (engine) + mục *Giao thức* của `tv-overlay/README.md` viết **trước** mọi mã engine (lượt 3b dựng TV theo đó). Không đụng TV/PS5/IP LAN/adb/emulator; không cửa sổ lạ; git chỉ đọc; không sửa CLAUDE.md/automation/README/Phase doc. **LLM thật: 0 lần gọi** (mọi smoke `ANTHROPIC_API_KEY` rỗng; test dùng transport/ backend giả). Mọi dữ liệu smoke/test ở `VIETSUB_HOME` = thư mục tạm (`%TEMP%\vs3a-*`); em không đọc/ ghi `%LOCALAPPDATA%\VietSub` thật. Evidence: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/dev/` (`<dev>/`). Feedback bước 0: `reviews/261114-epic1-phase3-dev-feedback.md` (không blocker).

## 1. Đã làm gì (file)

| Nhóm | File (`engine/src/Engine/…`) |
|---|---|
| Giao thức | `Protocol/Protocol.cs` (`OverlayFrame`, `OverlayItem`, `DisplaySpec`, hello/reply, `#AARRGGBB`) · `Protocol/TvClient.cs` (`IFrameSink`, `NullSink`, `TvClient`: kết nối lại mỗi 2 s, gửi bất đồng bộ "gói mới thay gói cũ", `TestHelloAsync`) |
| Cấu hình | `Config/EngineConfig.cs` (`VietSubHome` (`VIETSUB_HOME`), `EngineConfig` tv/display/tuning, `PairCode`) |
| Vòng chạy | `Pipeline/Motion.cs` (`FrameSignature` 16×9, `MotionTracker`: Động/Đứng yên, ô nền, chế độ trận, `DirtySinceAck`) · `Pipeline/FramePipeline.cs` (`FramePipeline`, `ItemBuilder`, `RoomFinder`) · `Pipeline/Sources.cs` (`PngDirSource`, `WindowsOcrProvider`, `FakeTranslator`, `DataPaths`) |
| Chụp liên tục | `Capture/WindowCapturer.cs` → `CaptureSession` (tạo lại frame pool khi đổi cỡ **chỉ ở `run`**; NB8 giải phóng RCW/thiết bị; NB9 kiểm HRESULT DWM/`GetClientRect`/`ClientToScreen`); `WindowCapturer.Capture` (lệnh `capture`) giữ exit 6 |
| Bố cục + màu | `Layout/Layout.cs` (`LayoutBuilder`, `ItemId`, `ColorSampler`) · `Ocr/OcrRunner.cs` (+`RecognizeLinesAsync` trả từng từ) |
| Dịch | `Translation/Translation.cs` (`SourceNormalizer`, `SlotValidator`, `LlmEligibility`, `Glossary`, `TranslationCache`, `UsageCounter`, `TranslationService`) · `Translation/Claude.cs` (`ClaudePrompt`, `ClaudeBackend`, `IClaudeTransport`) · `Translation/AnthropicSdkTransport.cs` (SDK `Anthropic` 13.9.0) |
| Lệnh | `Commands3.cs` (`pair`, `pattern`, `replay`, `run`, `warm`, `cache-export`, `Runtime`) · `Program.cs` (nối lệnh) · `Engine.csproj` (+`Anthropic`, copy `data/**/*.json`) |
| Dữ liệu | `engine/data/fc26/glossary.json` (**129** mục, 38 mục có `short_vi` ngắn hơn bản đầy đủ), `engine/data/fc26/seed-cache.json` (**67** câu/ nhãn) — soạn tay, tiếng Việt bóng đá tự nhiên (xưng "HLV"/"ông"), `status: machine` ⇒ **chờ End User duyệt**; hai file không trùng khoá |
| Test | `ProtocolConfigTests`, `MotionTests`, `LayoutTests`, `TranslationTests`, `ClaudeTvTests`, `PipelineTests` |
| Docs | `tv-overlay/README.md` mục *Giao thức* (viết đầu lượt) · `engine/README.md` (lệnh, mã thoát, file dưới `VIETSUB_HOME`, bảng giấy phép, hành vi, bẫy) |

**Giấy phép gói mới** (đọc `.nuspec`): `Anthropic` 13.9.0 = **Apache-2.0** (không copyleft ⇒ không dừng); phụ thuộc kéo theo: `Microsoft.Extensions.AI.Abstractions` 10.5.1, `System.Net.ServerSentEvents` 10.0.1, `System.Text.Json` 10.0.6, `System.Collections.Immutable` 8.0.0, `System.Diagnostics.DiagnosticSource` 8.0.1, `System.Threading.Tasks.Extensions` 4.6.3 (gói Microsoft/.NET, ghi vào README). Mạng chỉ cho NuGet restore.

## 2. Test: trước/ sau, RED rồi GREEN

Trước: **28**. Sau: **143** (+115). `dotnet build engine --no-incremental`: **0 warning, 0 error**.

- **RED** `<dev>/01-tdd-red.txt` — stub ném `NotImplementedException`: `Failed: 109, Passed: 28, Total: 137` (106 × `NotImplementedException`, 2 × `FileNotFoundException` ở test file glossary/seed chưa có, 1 theory cover-mode) — 109 case mới đỏ ở **runtime**, không phải lỗi biên dịch. Phủ: serialize/parse frame + hello + reply + `#AARRGGBB`, cấu hình (`VIETSUB_HOME`, mặc định, round-trip, file thiếu khoá), mã ghép cặp, chữ ký ô, `CellsOf`, máy trạng thái (đứng yên một lần, đổi → Động, dưới ngưỡng, ô nền ≥ 3 s, ô được bảo vệ, chế độ trận, ô bẩn), bố cục (hàng 3 cột bị gộp ⇒ 9 block/ 3 `col-` nhóm, gộp đoạn, nhiễu, hàng nhãn, `fontPx`, `box01`), `ItemId`, lấy mẫu màu (nền tối/ vệt chọn sáng/ gradient/ biên) + `DecideCoverMode`, chuẩn hoá + che slot + `Restore`, validator slot, `LlmEligibility`, file glossary (khoá không trùng, `short_vi` ngắn hơn) + file seed (slot hợp lệ, không trùng khoá, không trùng glossary), cache (seed/ người dùng thắng/ `reviewed` bất biến/ ghi nguyên tử/ `MayAttempt`), trần ngày, `TranslationService` (glossary, dịch+cache+đếm ký tự, câu hoa ngắn không gọi, slot sai ⇒ `rejected` + attempts qua phiên + dừng ở 2 lần, thử lại khi đổi `promptVersion`, lô bị bỏ ⇒ tách đôi + attempts chỉ ở câu đơn, lỗi mạng ⇒ 0 attempts + lùi 30 s, `inFlight` không gửi lại, trần ngày, không backend, lô ≤ `BatchMax`), `ClaudePrompt`/`ClaudeBackend` (system ổn định, schema đóng, parse, refusal/max_tokens/lỗi transport), `TvClient` (hello+frame qua listener loopback, sai token ⇒ dừng, kết nối lại, không chặn, `TestHello`).
- **GREEN** `<dev>/02-tdd-green.txt`: `Passed: 137, Failed: 0` (lần chạy đầu sau khi cài đặt).
- **Thêm sau khi có mã** (không có RED riêng — nói rõ): `RoomFinder` ×2, `FramePipeline` ×3 (đứng yên gửi 1 lần + nhịp tim + không vẽ câu chưa dịch; đổi màn ⇒ `items:[]` ngay rồi gói mới; `pattern`), `LayoutBuilder` "cùng cột hộp OCR cao 26/33 ⇒ cùng nhóm cùng `fontPx`" (thêm sau khi smoke cho thấy lệch cỡ). `<dev>/03-tests-pipeline.txt`: 142 xanh (trước ca cuối); cuối lượt **143/143**. Một assertion viết hỏng trong `FramePipelineTests` (so `ItemSetChanges` với biểu thức vô nghĩa) đã sửa — là lỗi test, không nới kỳ vọng.

## 3. Smoke (Blackbox Verify — phía engine, ảnh TỔNG HỢP, KHÔNG phải R-D/F1)

| Bước | Kết quả | Evidence |
|---|---|---|
| 1 `dotnet test engine` | 143/143 (`gradlew` thuộc lượt 3b) | `<dev>/03-…`, cuối lượt chạy lại |
| 2 `replay --translator fake --no-send` (3 ảnh synthetic) | `.frame.json` đúng số đoạn: `synth-inbox-list` **15 item** (hàng 3 cột tách đúng, `col-1/2/3`, 4 ngày `{N1}` thuộc `misses` vì không đủ điều kiện LLM); `synth-email` 6 item; `synth-dark` 1 item. Màu mẫu: cover `#FF141F2C` (nền thật `#14202C`, sai 1/255 do trung vị vành), text `#FFFFFFFF`, `cover.mode=solid`, có `room`. Seq tăng; `display {24,0}`; `misses.json` còn 4 mục "not-eligible" (ngày) — **không** có mục do thiếu bản dịch (fake) | `<dev>/04-smoke2-replay-fake.txt`, `<dev>/replay-fake/` |
| 3 `--translator cache` + seed | Câu có trong glossary/seed ra tiếng Việt ("Ban lãnh đạo", "Cập nhật mục tiêu mùa giải", "Hôm nay", "Giám đốc bóng đá", "Đội ngũ y tế", "Học viện trẻ", "Cập nhật hợp đồng", "Kính gửi ông,", "Vui lòng hoàn tất đăng ký trước hạn chót."…); câu lạ (vd "Transfer Committee", "Contract offer for J. Smith") **không có item**; log `gọi backend: 0 · HTTP thật: 0` | `<dev>/05-smoke3-replay-cache.txt`, `<dev>/replay-cache/` |
| 4 (engine↔listener) `pair` | Listener loopback do em bật (`127.0.0.1:47821`, PID đã dừng): mã đúng (`acde-fhjk` ⇒ chuẩn hoá `ACDEFHJK`) ⇒ `OK`, exit 0; mã **sai bảng chữ** ⇒ exit 1 không ghi file; mã hợp lệ nhưng **sai** (`ACDEFHJM`) ⇒ listener `REPLY token-reject`, engine exit 3 "sai mã ghép cặp". `engine.local.json` ghi ở thư mục tạm (token che khi in) | `<dev>/09-smoke45-…txt`, `<dev>/09-listener-received.txt`, `<dev>/10-smoke-run-and-reject.txt` |
| 5 (engine↔listener) gói gửi | `replay --send`: listener nhận hello + 4 gói (seq 1..4, cùng `sessionId`, 15 item, ~6 KB/gói ≪ 256 KB); `pattern --send --seconds 2`: 22 item (góc `p-tl`… + nhãn %) + gói `items:[]` kết thúc (87 byte) | `<dev>/09-listener-received.txt` |
| 6 chuỗi A,A,B,B (hold 10 s/ ảnh) | `01-a`: bộ item đổi 2 lần (có bản dịch về), `02-a`: **0 lần đổi trong 10 s đứng yên** (10 gói nhịp tim), `03-b`: 3 lần (rỗng → từng phần → đủ), `04-b`: **0 lần**; log trạng thái Động→Đứng yên (`--verbose`). Gói `items:[]` gửi ngay khi đổi (test `PipelineTests`) | `<dev>/06-smoke6-seq-aabb.txt`, `<dev>/replay-seq/` |
| 10 trần + khoá rỗng | `--translator claude` với `ANTHROPIC_API_KEY=''`: một dòng "cảnh báo: không có ANTHROPIC_API_KEY — chỉ dùng cache/glossary", exit 0, **0 backend, 0 HTTP**; `warm` không `--yes` ⇒ in "10 câu, 322 ký tự, TỐN TIỀN" và exit 1; trần/ ngày kiểm bằng test đơn vị (`UsageCounter`, `DailyCap_…`) | `<dev>/07-smoke10-claude-nokey.txt` |
| `cache-export` | TSV (cache + glossary) cho End User | `<dev>/cache-export.tsv`, `08-…` |

**Chứng minh 0 HTTP**: transport SDK (`AnthropicSdkTransport`) chỉ được tạo khi `translator=claude` **và** có khoá; mọi smoke dùng khoá rỗng/ `fake`/ `cache`; mọi dòng tổng kết in `HTTP thật: 0`; test `ClaudeBackend` dùng `IClaudeTransport` giả.

## 4. Chưa chứng minh được

1. **`run` trên cửa sổ thật + tạo lại frame pool khi đổi cỡ**: lúc chạy, phiên Windows ở trạng thái **ngắt kết nối** (`quser`: `Disc`, `LogonUI` đang chạy) ⇒ WGC không phát khung; **cả `capture` cũ cũng exit 4** trên cửa sổ `ffplay` của em (kiểm lại bằng `capture --frames 1`) ⇒ nguyên nhân là môi trường, không phải mã mới (nhưng em **không** thể chứng minh bằng chạy). Chứng minh được: vòng `run` không thoát khi không có khung (log "thử lại sau 2 s", client TCP tiếp tục kết nối lại), và thoát gọn qua `--max-seconds` kèm dòng usage. Tạo lại pool, NB8, NB9 chỉ qua đọc mã + build.
2. **LLM thật**: `AnthropicSdkTransport` (kiểu SDK đúng ngay lần biên dịch đầu) **chưa chạy**; schema JSON/ `OutputConfig`/ caching (`cache_read_input_tokens` > 0) chưa kiểm — việc của Bước 7 (Leader). Mapping thuần (prompt/schema/parse/refusal) đã có test.
3. **Chất lượng bản dịch seed/ glossary** (196 mục tự soạn): chưa End User duyệt; xưng hô "ông" là mặc định chờ User chốt.
4. **Hiệu năng**: thời gian OCR + dựng item mỗi khung trên laptop 2880×1800 chưa đo (replay trên ảnh 1920×1080 chạy tức thì nhưng không đo); `FrameSignature` quét mỗi khung (lấy mẫu cách 1 điểm).
5. **Ngưỡng** (`cellDiffThreshold` 6, `stableMs` 250, `splitGapFactor` 1,2, `roomMaxFactor` 2, `coverUniformThreshold` 18…) chỉ kiểm trên ảnh tổng hợp; chưa có F2/F3 thật. Đều ở `engine.local.json` khối `tuning`.
6. **TV** (3b): mọi thứ phía hiển thị; `room`/`alt`/`group` mới được engine gửi, chưa được TV dùng.
7. `Ctrl+C` thật chưa bấm (dùng `--max-seconds` thay); `holdMs` do TV áp nên engine không kiểm.

## 5. Lệch so với doc (và lý do)

| # | Lệch | Lý do |
|---|---|---|
| 1 | **Nhóm**: `col-n` thắng `row-n` (Feedback F5 em đề xuất ngược) — cột (≥ 2 thành viên) → hàng ngang (≥ 2 nhãn sát nhau) → `solo-n` | Hàng danh sách 3 cột cũng nằm trên "cùng một hàng ngang"; SA N1 yêu cầu "3 `group` cột" |
| 2 | `GroupHeightTolerance` mặc định **0,30** (doc D6 ±10%) + **`fontPx` = trung vị theo nhóm** | Smoke: hộp OCR cùng cột dao động 26 ↔ 33 px theo nét lên/ xuống ⇒ nhóm vỡ thành `solo`, lệch cỡ trong một cột. Vẫn chỉnh được ở `tuning` |
| 3 | `pair`: mã sai bảng chữ ⇒ exit 1 **không ghi** `engine.local.json` (doc D3: ghi rồi thử hello). Mã hợp lệ nhưng TV từ chối ⇒ **đã ghi** rồi exit 3 như doc | Tránh lưu mã vô nghĩa; không mất tính năng |
| 4 | Thêm tham số `run --max-seconds N` | Smoke không có Ctrl+C trong shell không tương tác |
| 5 | `Missing` trong lô OK (LLM bỏ sót id) ⇒ coi là `rejected` (+1 attempts) | Doc chỉ nói slot sai/ lô bị bỏ; sót id cũng là đầu ra không dùng được |
| 6 | Ngày/ giờ đứng một mình (`12 Nov` ⇒ khoá `{N1}`) **không** được dịch/ không vẽ | Khoá toàn slot không đủ điều kiện LLM; hậu quả "ngày giữ tiếng Anh" đúng bảng *Mặc định* ("giữ nguyên dạng gốc qua slot") nhưng người xem thấy cột ngày tiếng Anh |
| 7 | Glossary 129 + seed 67 = 196 mục (doc "~80–150") | Soạn rộng hơn để Hộp thư đỡ trống; không có mục nào `reviewed` |
| 8 | `usage/YYYY-MM-DD.json` theo **giờ máy** `DateTime.Now` (doc D10 "ngày giờ máy") | — |
| 9 | Bản dịch fake/ cache ghi vào `VIETSUB_HOME` tạm | Đúng N11; không động dữ liệu thật |

## 6. Câu hỏi mở — cái gì sẽ trông tệ nhất trên TV thật, và em sửa gì trước?

1. **Màn nửa Anh nửa Việt + chữ "bật ra" trễ**: (a) mọi dòng chưa có bản dịch **không vẽ** nên cột ngày (`12 Nov`) và mọi tên/ câu lạ vẫn tiếng Anh cạnh cột Việt; (b) câu mới phải đợi LLM (vài giây) ⇒ mỗi lần cuộn danh sách, hàng mới hiện tiếng Anh rồi "pop" thành Việt; (c) vệt chọn đổi hàng ⇒ ô đổi ⇒ item của cả hàng bị gỡ, rồi OCR lại **cả khung** (chưa đo, có thể 0,3–1 s ở 2880×1800) ⇒ "chữ ma" khi lướt. **Sửa trước**: (1) dịch cục bộ các dòng chỉ gồm slot ngày/ giờ/ tiền (định dạng Việt "12 thg 11", giữ số) — không cần LLM, thấy ngay ở mọi hàng Hộp thư; (2) OCR chỉ **vùng ô bẩn** (hộp bao các ô đổi, nới lề) thay vì cả khung, giữ nguyên item không đổi để vệt chọn dời một hàng không làm cả danh sách nhấp nháy.
2. **Cỡ chữ Việt vs vùng che**: `fontScale 0,78` + `minFontPx 24` là phỏng đoán; chữ Việt dài hơn Anh ~15–30% trong ô hẹp ⇒ TV phải co/ xuống dòng trong `room` — chưa có ảnh thật để chốt.
3. **Bản dịch seed chưa duyệt** có thể sai giọng ("ông" trong dialog UI) — hiện đúng ngay màn đầu User xem.
4. **Chế độ trận** có thể kích hoạt nhầm khi chuyển màn menu đổi toàn khung (đã thấy trong replay giữa các ảnh) — vô hại (chỉ trễ 1,5 s) nhưng làm chậm lần hiện đầu sau mỗi lần đổi màn.

## 7. `git status --short` (cuối lượt)

```
 M engine/README.md
 M engine/src/Engine/Capture/WindowCapturer.cs
 M engine/src/Engine/Engine.csproj
 M engine/src/Engine/Ocr/OcrRunner.cs
 M engine/src/Engine/Program.cs
 M tv-overlay/README.md
 M project-docs/261114-fc26-vietsub-overlay/261114-epic1.md          (của Leader)
 M project-docs/261114-fc26-vietsub-overlay/261114-workflow-log.md   (của Leader)
?? engine/data/
?? engine/src/Engine/Commands3.cs
?? engine/src/Engine/Config/ Layout/ Pipeline/ Protocol/ Translation/
?? engine/tests/Engine.Tests/{ClaudeTvTests,LayoutTests,MotionTests,PipelineTests,ProtocolConfigTests,TranslationTests}.cs
?? project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/
?? project-docs/261114-fc26-vietsub-overlay/reviews/261114-epic1-phase3-{dev-feedback,dev-report-3a,sa-review}.md
?? project-docs/261114-fc26-vietsub-overlay/261114-epic1-phase3.md  (+ _workers/… của Leader)
```
Phần của em: toàn bộ `engine/`, `tv-overlay/README.md` (mục *Giao thức*), `playtest/epic1-phase3/dev/` (script `.ps1` bị `project-docs/.gitignore` loại), feedback + report này. Không có tiến trình em bật còn sống (listener PID 15752/14472, `ffplay` PID đã dừng).
