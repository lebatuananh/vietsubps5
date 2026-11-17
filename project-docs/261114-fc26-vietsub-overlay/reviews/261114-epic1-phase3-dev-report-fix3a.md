DEV-FIX3A DONE
# Dev report fix3a — sửa 4 blocking của review 3a (Rev 1.3) + NB + vệt chữ gốc (C1)

Căn cứ: [review 3a](261114-epic1-phase3-review-3a.md) · dòng "Sau review 3a — Rev 1.3" trong bảng đầu `261114-epic1-phase3.md`. Phạm vi: `engine/` + `tv-overlay/` + hai README. Bằng chứng: `playtest/epic1-phase3/dev/fix3a/` (tiền tố `fix3a-`, viết tắt `<fx>/`). Mọi tiến trình con: `VIETSUB_HOME` = thư mục tạm, `ANTHROPIC_API_KEY=''`; smoke engine đặt thêm `HTTPS_PROXY=http://127.0.0.1:9` (proxy chết). **Không gọi API thật** (mọi dòng tổng kết `HTTP thật: 0`).

## 1. Test trước / sau

| | Trước fix3a | Sau |
|---|---|---|
| engine (`dotnet test engine`) | 143 xanh | **183 xanh, 0 fail**, `dotnet build --no-incremental` = 0 Warning / 0 Error |
| tv-overlay (`gradlew :app:testDebugUnitTest`) | 72 xanh | **75 xanh, 0 fail**, `assembleDebug` BUILD SUCCESSFUL, 0 warning |

- **ĐỎ** engine: `<fx>/fix3a-01-engine-red.txt` — 183 test, **28 fail** (API stub trước, chưa có logic). XANH: `<fx>/fix3a-02-engine-green.txt` (183/183). `fix3a-02-engine-green-try.txt` = lần chạy giữa chừng (3 test cũ còn mã hoá hành vi bị Rev 1.3 đổi, xem mục 3).
- **ĐỎ** TV: `<fx>/fix3a-03-tv-red.txt` — 75 test, 1 fail (`duplicate_ids_in_one_frame_keep_every_item_and_stay_stable`; tạm hoàn nguyên 1 dòng khoá trong `OverlayModel` để lấy đỏ, rồi khôi phục). XANH: `<fx>/fix3a-04-tv-green.txt` (75/75).
- Test mới của engine nằm ở `engine/tests/Engine.Tests/Fix3aTests.cs` (+40), cộng 3 test cũ được sửa (mục 3).

## 2. Từng blocking: đổi gì + test

**B1 — `id` trùng trong một gói.**
- `ItemId.Compute(box01, key, occurrence)`: băm(ô lưới + câu chuẩn hoá + **số thứ tự lần xuất hiện** của cặp (ô, câu)); `ItemBuilder` đếm theo thứ tự đọc (y rồi x). `occurrence = 0` giữ nguyên công thức cũ ⇒ id ổn định/ tương thích cho trường hợp không trùng.
- TV: `OverlayModel` khoá nội bộ `id`, `id#2`… nên engine lỗi có gửi trùng cũng **không mất item**. README (Giao thức) ghi "`id` duy nhất trong một gói".
- Test: `B1_ItemIdUniqueTests` ×2 (cùng ô cùng chữ ⇒ id khác nhau và ổn định; 2 hàng "Board"/"Today" cùng ô y=0,367/0,441 ⇒ 4 id khác nhau, giống nhau giữa hai khung); TV `duplicate_ids_in_one_frame_keep_every_item_and_stay_stable`.
- End-to-end: ảnh Reviewer `rv-a-inbox.png` ⇒ **16 item, 16 id khác nhau** (`<fx>/fix3a-14-b1-inbox-ids.txt`; trước: `dup ids {…: 2}`). Trên emulator, hàng đang chọn (chữ tối/nền sáng) và hàng thường (chữ sáng/nền tối) cùng hiện **đúng màu riêng**: `<fx>/fix3a-21-cache-rvinbox-2-later.png`.

**B2 — lỗi API không tạm thời đốt trần + khoá `rejected`.**
- Thêm `BackendOutcome.Fatal`. `ClaudeBackend`: transport ném lỗi không tạm thời ⇒ `Fatal` (không còn `Dropped`). `TranslationService` khi gặp `Fatal`: **không tách đôi, không `attempts`, hoàn ký tự đã giữ chỗ (`UsageCounter.Refund`), tắt backend cả phiên** (`BackendDisabled`/`DisabledReason`), log **đúng một dòng** `[llm] tắt backend LLM cho phiên này: …`; lần `ProcessAsync` sau trả 0 ngay (0 HTTP).
- `AnthropicSdkTransport.Classify(status)`: 429/5xx = tạm thời + hoàn ký tự; 408/409 = tạm thời (giữ ký tự); mọi 4xx còn lại (400/401/403/404…) = không tạm thời. `ClaudeTransportException(…, refundable)`.
- Test: `Http401_DisablesBackendForTheSession_OneRequest_NoAttempts_CharsRefunded` (40 câu, transport giả luôn 401 ⇒ **đúng 1 request**, `Used == 0`, **0 entry `rejected`**, 1 dòng log, lần 2 vẫn 1 request); `Fatal_DoesNotPoisonNextSession_AfterKeyFixed` (phiên mới sau khi sửa khoá dịch được 40/40 — trước fix: 0 HTTP, khoá vĩnh viễn); `ClaudeBackend_NonTransientTransportError_IsFatal`; `StatusClassification` ×9.

**B3 — nhấp nháy khi item nằm trên ô nền động.**
- Cách Leader chọn: với ô nền động có item đang vẽ, **chỉ so điểm ảnh trong bbox chữ**. `FrameSignature.RegionSignature` (lưới 8×2 trung bình xám của bbox gốc, tức hộp item co lại `boxPadPx`); `FramePipeline.UpdateTextSignatures` so với khung trước theo từng item; `MotionTracker.Update(..., textChangedCells)`: ô nền động có item chỉ tính "đổi" khi chữ của item đó **thật sự đổi**. Đổi chữ thật vẫn gỡ item ngay.
- Test: `B3_DynamicBackgroundUnderItemTests` — chuỗi khung **y hệt ca J/K của Reviewer** (10 s, 4 khung/s): ca J ⇒ **≤ 1** lần hiện↔mất (trước: 28 trong 10 s); ca K ⇒ ≤ 1 lần và khe gói < `ttlMs`; `CaseJ2` (nền động quanh chữ **và** chữ đổi thật ⇒ item bị gỡ ngay).

**B4 — `fontPx` < `minFontPx`.**
- Chọn: **TV bắt đầu từ `max(fontPx, minFontPx)`, engine không kẹp** (đúng Rev 1.3). Mã TV `AutoFit` **đã làm đúng như vậy từ 3b** (`max(fontPx×H/1080, minSize)`), nên 2 test TV mới (`fontPx_below_min_font_starts_at_min_not_at_fontPx` 18→24; `…scales_min_with_plane_height` 4K 18→48) **xanh ngay khi viết** — nói rõ: chúng khoá hành vi, không có đỏ riêng. Phần thiếu là **README**: đã ghi câu quy tắc (mục *Giao thức*, `display.minFontPx`).
- `fontScale` mặc định đổi **1,0** (`DisplayConfig`, README engine, 2 test cũ cập nhật).

## 3. NB — làm / bỏ

| NB | Quyết định | Chi tiết + test |
|---|---|---|
| NB1 | **làm** | `_triedThisSession`: câu bị loại chỉ thử lại ở **phiên sau** (`RejectedSentence_IsNotRetriedInTheSameSession`) |
| NB2 | **làm** | `AnthropicClient { MaxRetries = 0 }` (đọc code; không test được vì không gọi mạng) |
| NB3 | **làm** | 429/5xx hoàn ký tự, timeout giữ (`Transient429_RefundsChars_TimeoutKeepsThem`) |
| NB4 | **làm** | `usage` ghi temp+rename; file hỏng/ rỗng/ sai kiểu/ âm ⇒ **coi như chạm trần**, bắt mọi ngoại lệ (`CorruptUsageFile_IsFailClosed_NoThrow` ×5, `Usage_WritesAtomically…`) |
| NB5 | **làm** | `ITranslationBackend.IsFake`/`Provider`; `FakeTranslator` không ghi cache người dùng, không trừ trần, provider `fake` (`FakeTranslator_DoesNotWriteUserCache…`; smoke: file cache sau `replay --translator fake` có 0 entry, `usage` 0 file) |
| NB6 | **làm** | khoá quanh `Save` + hàng đợi dịch nền duy nhất trong `FramePipeline` (`ConcurrentSaves_DoNotThrow`, 200 `Save` song song) |
| NB7 | **làm** | chế độ trận cần ≥ `matchModeMinBeats` (3) nhịp trong `matchModeSustainMs` (3 s); đổi trang toàn màn một lần ⇒ ổn định sau `stableMs` (`SingleFullScreenPageChange_IsNotMatchMode`, `SustainedMotion_3BeatsIn3s_IsMatchMode`) |
| NB8 | **làm** | `mergeHeightTolerance` 0,20 → 0,35; test email 23/29 px gộp thành "Your contract offer has been accepted." (`Email_TwoLinesWithOcrHeightJitter_AreMerged`) |
| NB9 | **làm** (theo Rev 1.3: "engine giới hạn ≤ 200 item/gói") | `ItemBuilder` giữ 200 item đầu, cắt `text` về 1000 (`Frame_IsClampedTo200Items_AndText1000Chars`, 280 dòng). **Không làm** phần "tách tiêu đề khỏi cột khi cao lệch > 30%" của NB9 gốc — Rev 1.3 chỉ chốt fontScale + giới hạn, và chưa có số đo trên màn thật ⇒ để Bước 8 |
| NB10/12 | **làm** | README *Giao thức* sửa câu "Động ⇒ `items:[]`" (gói = trạng thái đầy đủ, chỉ bỏ item giao ô đổi); `display.coverMode` ngoài auto/solid/card/none bị `EngineConfig.Validate()` từ chối khi nạp (`CoverMode_*`) |
| NB11 | **làm** (sửa parser, không sửa doc) | `--no-send`/`--send`/`--yes`/`--verbose` nhận cả dạng trần (`BareBooleanFlag_IsAccepted`); cờ không phải boolean thiếu giá trị vẫn lỗi. **Phase doc không được tôi sửa** (ngoài phạm vi) — doc ghi `--no-send` trần giờ đúng |
| NB13 | **làm, không test đơn vị** | `run` bắt mọi ngoại lệ lạ trong vòng, log loại + thông điệp, `SendEmpty`, thử lại sau 2 s (cần cửa sổ WGC thật để kích — **chưa chạy**) |

3 test cũ được sửa vì encode đúng hành vi mà Rev 1.3 đã đổi, không nới kỳ vọng nào khác: `ConfigTests` ×2 (`FontScale` 0,78 → 1,0), `ClaudeBackendTests.TransientTransportError_IsTransient_Others_AreFatal` (Dropped → Fatal).

## 4. Vệt chữ gốc ở (1110, 545) — C1

- **Nguyên nhân** (đo ở 3b): hộp OCR hụt ~2 px so với nét chữ ⇒ `RoomFinder` ôm sát hộp (dải kế bên chứa nét chữ nên không "đồng nhất") ⇒ TV cắt vùng che trong `room` (N5) ⇒ còn sót 2 px nét cuối.
- **Sửa (engine)**: `ItemBuilder` **nới hộp OCR `tuning.boxPadPx` = 3 px/1080 mỗi phía** trước khi lấy `box` gửi TV, lấy mẫu màu và khởi tạo `room` ⇒ `room ⊇ hộp đã nới ⊇ nét chữ`. `id` vẫn tính từ hộp **chưa** nới (ổn định trước dao động OCR).
- **Test tái hiện**: `Sliver_C1_OriginalPixelsMustNotPeekOutTests.OcrBoxTwoPxShort_RoomAndBoxStillCoverAllInk` — nét chữ x 500..702, y 530..558, OCR báo 500..700 × 532..556; vùng che theo công thức README (hợp + lề 6 px, cắt trong room) phải phủ cả 500..702 × 530..558. Đỏ trước sửa, xanh sau.
- **Emulator**: ảnh `<fx>/fix3a-21-fake-inbox-2-later.png` — cùng `synth-inbox-list` + `--translator fake` như 3b: **không còn vệt** quanh (1110, 545) (so `3b-13-fake-2-overlay-later.png`).

## 5. Smoke lại

**Engine** (`<fx>/smoke-fix3a.ps1`; chạy bản build riêng `D:\Temp\engine-fix3a` vì `engine/src/Engine/bin` đang bị một tiến trình `Engine.exe pattern --send true --seconds 70` **không phải của tôi** (Reviewer chạy song song) khoá — tôi không đụng vào nó):
- Smoke 1 `dotnet test`: 183/183.
- Smoke 2 `replay --translator fake` (`fix3a-10`): dark 1 item, email 6, inbox 15 (4 ngày không đủ điều kiện LLM); `HTTP thật: 0`; cache fake **không ghi** gì (file có `entries: []`), 0 file `usage`.
- Smoke 3 `--translator cache` (`fix3a-11`): 0/3/9 item; `gọi backend: 0 · HTTP thật: 0`; `misses.json` **giống hệt** lần 3a.
- Smoke 6 A,A,B,B hold 10 s (`fix3a-12`): `01-a` đổi 2 lần, **`02-a` 0 lần**, `03-b` 3 lần, **`04-b` 0 lần** (như 3a).
- Smoke 10 (`fix3a-13`): khoá rỗng ⇒ 1 dòng cảnh báo, exit 0, 0 backend; `warm` không `--yes` ⇒ "10 câu, 322 ký tự" exit 1.

**Emulator `emulator-5556`** (AVD `vietsub_dev`, tôi khởi động; script `<fx>/smoke-fix3a-tv.ps1`, log `<fx>/fix3a-20-tv-smoke.txt`; engine = bản fix): APK debug cài `-r`, cấp overlay, `adb forward 47820`.
- Bước 4: màn chờ hiện mã `XPUK-7FVR`, focus ở "Hướng dẫn cho người cài ▸"; `pair` mã sai ⇒ exit 3 (`token`); mã đúng (thường, không `-`) ⇒ OK. (`fix3a-20-screen-waiting.png`)
- Bước 5: `replay --send` fake + cache lên phông; chữ Việt đè, vệt C1 hết, `...-3-after-engine-closed.png` thấy phông gốc lộ lại sau khi engine thoát; ảnh Reviewer có 2 nhãn "Board" cùng hiện (mục 2/B1).
- Bước 7: `pattern` — `fix3a-22-pattern-corners.png`: 4 ô TL/TR/BL/BR sát 4 góc.
- Bước 8 (phím remote thật): BACK về `MainActivity` khi lớp phủ bật ⇒ OFF, focus "Bật lại lớp phủ"; OK ⇒ sang màn chờ; engine nối ⇒ "● Đã kết nối", focus "Tắt lớp phủ"; RIGHT ⇒ focus "Ghép cặp lại", **3 s sau vẫn ở đó**; OK ⇒ mã mới `UD4R-NTW7`, hello mã cũ ⇒ `{"ok":false,"reason":"token"}`. (`fix3a-23/24/25-*.png/.xml`)
- Bước 9: `v=2` ⇒ `version`; sai mã ⇒ `token`; dòng rác ⇒ đóng không trả lời; hello đúng + 300 KB ⇒ TV đóng; hello đúng sau đó ⇒ `{"ok":true}`; `logcat -b crash` 0 dòng; service còn chạy. **Ghi chú trung thực**: trong script, biến `$s` (stream) đè `$S` (tham số adb) làm hai dòng kiểm cuối của log in sai; tôi chạy lại tay và ghi vào cuối `fix3a-20-tv-smoke.txt`: crash = 0, service = 1.

**Dọn dẹp**: `adb -s emulator-5556 emu kill`, `adb forward --remove tcp:47820`, `gradlew --stop`; sau đó không còn tiến trình emulator của tôi (PID cmd/emulator/qemu của tôi ghi ở `<fx>/fix3a-emulator-pids.txt` đều đã thoát). `adb devices` còn **`emulator-5558`** (AVD `vietsub_rv` — của Reviewer, tôi không đụng); không có `emulator-5556`.

## 6. Còn thấy / chưa chứng minh được

- **Quan sát trên ảnh `fix3a-21-cache-rvinbox-2-later.png` (code TV 3b, Reviewer đang review song song — tôi không sửa)**: nhãn nhiều dòng (vd "Ban lãnh / đạo", "Cập nhật / chấn thương") tràn xuống dưới thẻ hàng và có chỗ đè lên hàng kế (dòng thứ hai nằm ngoài `room` của thẻ). Nguyên nhân khả dĩ: engine gửi `maxLines = lines+2` nên TV cho phép wrap, trong khi `room` chỉ cao một hàng.
- B3 chỉ kiểm trên khung tổng hợp (như Reviewer); tần suất nền động trên FC 26 thật tuỳ menu — Bước 8.
- NB13, SDK `MaxRetries = 0` và phân loại lỗi SDK (`AnthropicApiException.StatusCode`) **chưa chạy với SDK thật** (không gọi mạng); `Classify` có test, phần map ngoại lệ chỉ biên dịch + đọc code.
- Lần gọi LLM thật đầu tiên: giữ nguyên đề xuất runbook của Reviewer (`warm --translator claude --yes true` trên **một** ảnh nhỏ, đọc dòng `[llm]` trước khi `run`).
- Vẫn chưa: `run` trên cửa sổ PS Remote Play thật (WGC), TV Sony thật, bản release.

## 7. `git status --short` (cuối fix3a, 68 dòng)

Thuộc fix3a (đã sửa/thêm lượt này):
```
 M engine/README.md
 M engine/src/Engine/Cli.cs
?? engine/src/Engine/Config/  Layout/  Pipeline/  Protocol/  Translation/  Commands3.cs      (sửa trong các thư mục untracked)
?? engine/tests/Engine.Tests/Fix3aTests.cs                                                   (mới)
?? engine/tests/Engine.Tests/{ClaudeTvTests,ProtocolConfigTests}.cs                          (sửa 3 test cũ)
 M tv-overlay/README.md
?? tv-overlay/app/src/main/java/vn/vietsub/ps5/tv/OverlayModel.kt                            (khoá nội bộ id#n)
?? tv-overlay/app/src/test/java/vn/vietsub/ps5/tv/{OverlayModelTest,AutoFitTest}.kt          (+1, +2 test)
?? project-docs/261114-fc26-vietsub-overlay/reviews/261114-epic1-phase3-dev-report-fix3a.md
?? project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/dev/fix3a/                 (bằng chứng)
```
Phần còn lại là của các lượt trước (3a, 3b, review, SA, Leader): `M` `engine/…/{WindowCapturer,Engine.csproj,OcrRunner,Program}.cs`, `tv-overlay/app/{build.gradle.kts, AndroidManifest.xml, MainActivity/OverlayService/OverlayView.kt, strings.xml}`, `project-docs/…/{261114-epic1,261114-workflow-log}.md`; `??` `engine/data/`, 6 file test engine cũ, `tv-overlay/app/src/debug/`, các file `.kt` TV (AppState, AutoFit, OverlayServer, PairingCode, Protocol + test), `_workers/*`, `reviews/*` của các lượt khác, `playtest/epic1-phase3/{dev,reviewer}/`. Không chạy lệnh git ghi nào.
