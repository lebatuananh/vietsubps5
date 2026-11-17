REVIEW-RE3A DONE
# 261114 — Epic 1 Phase 3 — Review lượt 3a (`engine/` Bước 1–4 + `tv-overlay/README.md` *Giao thức*)
Verdict: PASS (re-review fix 3a — 0 blocking; review 3a ban đầu: FAIL 4 blocking)

Reviewer: session độc lập.
- **Nguồn chân lý**: [Phase 3 doc](../261114-epic1-phase3.md) — bảng *Quyết định sau SA gate* Rev 1.1/1.2 là ràng buộc, đè lên thân doc.
- **Đã đọc kèm**: [SA review](261114-epic1-phase3-sa-review.md), [dev feedback](261114-epic1-phase3-dev-feedback.md), [dev report 3a](261114-epic1-phase3-dev-report-3a.md).
- **Evidence**: `project-docs/261114-fc26-vietsub-overlay/playtest/epic1-phase3/reviewer/` (viết tắt `<rv>/`).
- **Harness của Reviewer**: một console project **ngoài repo** (scratchpad), tham chiếu `Engine.csproj` và chạy **lớp thật** của engine với transport/ OCR/ khung giả. Bản sao mã: `<rv>/tools/rv3a-harness.cs.txt`; output: `<rv>/3a-02-harness.txt`.
- **Không gọi API thật.** Mọi smoke chạy với `ANTHROPIC_API_KEY=''`, `VIETSUB_HOME` = thư mục tạm riêng, và `HTTPS_PROXY=http://127.0.0.1:9` (proxy chết, có HTTP nào đi ra sẽ lỗi ngay). `%LOCALAPPDATA%\VietSub` thật **không tồn tại** trước lẫn sau review.

## Blocking

1. **B1 — `id` trùng nhau trong cùng một gói** — `Layout/Layout.cs:167-173` (`ItemId.Compute`), dùng ở `Pipeline/FramePipeline.cs:98`; README `tv-overlay/README.md:89`.
   - **Hiện tượng**: `id` = băm(ô 16×9 + câu gốc). Hai hàng Hộp thư liền nhau (cách ~80 px) có **cùng người gửi/ cùng ngày** — rất thường gặp ("Board", "Today") — rơi vào cùng một ô ⇒ cùng `id`.
   - **Evidence end-to-end** (ảnh của mình `rv-a-inbox.png`, `<rv>/3a-05-smoke3-clean.txt`): `dup ids {'b6819182771a': 2}` — "Ban lãnh đạo" ở y=0.367 (hàng **đang chọn**, chữ `#FF14181E` trên nền `#FFE1E6EB`) và y=0.441 (hàng thường, `#FFEBEBEB` trên `#FF1A2638`) **cùng `id`**. Harness ca I: 8 item, chỉ 6 `id` khác nhau.
   - **Vì sao chặn**: README nói `id` là khoá để TV quyết định không vẽ lại/ fade. Lượt 3b gần như chắc sẽ dùng map theo `id` ⇒ mất một nhãn, hoặc tráo màu hàng chọn/ hàng thường. Giao thức không nói `id` duy nhất trong gói ⇒ 3b phải đoán.
   - **Nguyên nhân**: công thức P3R2-D2 không tính tới chữ trùng — lỗi doc, Dev làm đúng chữ.
   - **Sửa**: `id` = băm(ô + câu + **số thứ tự lần xuất hiện** của cặp (ô, câu) theo thứ tự đọc). Thêm vào README: "`id` **duy nhất trong một gói**". Thêm test 2 hàng cùng chữ cùng ô.
2. **B2 — Lỗi API không tạm thời (400/401/403) bị coi là "lô bị bỏ"** ⇒ dây chuyền tách đôi, đốt trần, và khoá `rejected` vĩnh viễn — `Translation/Claude.cs:106`. Code: `return new(ex.Transient ? BackendOutcome.Transient : BackendOutcome.Dropped, [], ex.Message);`, nối vào `Translation/Translation.cs:360-366` (tách đôi) và `:388-398` (`Reject`).
   - **Harness ca A** (transport giả luôn ném `ClaudeTransportException(false, "401")`, 40 câu khác nhau, 2400 ký tự):
     - ProcessAsync lần 1 ⇒ **79 HTTP**, trừ trần **15.360 ký tự** (6,4× lô).
     - Lần 2 **cùng phiên** ⇒ thêm 79 HTTP, tổng 30.720; mọi câu `attempts=2`.
     - Phiên mới **sau khi sửa khoá** ⇒ `HTTP=0` — 40 câu bị khoá `rejected` cho tới khi đổi `promptVersion`/`glossaryVersion`.
   - **Vì sao chặn**: 400/401 không tính tiền, nhưng ~3 màn × 40 câu là hết **~92% trần ngày** trong vài phút. Mọi câu đã thấy trong lúc lỗi bị đầu độc qua phiên. Đây chính là kịch bản dễ xảy ra nhất ở **lần gọi thật đầu tiên** (Bước 7/8): SDK chưa chạy lần nào (dev report §4.2), `OutputConfig`/`Effort`/schema sai ⇒ 400; khoá dán sai ⇒ 401. Trái mục đích B1/D10 (chống lãng phí lượt trả phí, trần có ý nghĩa).
   - **Nguyên nhân**: B1/P3R2-D3 chỉ định nghĩa thất bại **do đầu ra mô hình**; lỗi cấu hình rơi vào cùng nhánh.
   - **Sửa**: thêm outcome `Fatal` cho lỗi transport không tạm thời (4xx trừ 408/409/429) ⇒ **ngắt backend cả phiên** (log một lần, không tách đôi, không `attempts`, không tính hoặc hoàn lại ký tự đã giữ). Test: 401 ⇒ đúng 1 HTTP, 0 entry `rejected`.
3. **B3 — Nhấp nháy khi item nằm trên ô nền động** (CLAUDE.md: "không để chữ … nhấp nháy") — `Pipeline/Motion.cs:94-95` (ô nền vẫn tính là "đổi" nếu được bảo vệ), `Pipeline/FramePipeline.cs:144` (ô bảo vệ = ô của item đang vẽ), `:160-166` (gỡ item giao ô đổi).
   - **Harness ca J**: một ô có nền đổi liên tục (vd nền menu chuyển động), chữ đứng yên trong ô. Kết quả: **28 lần item hiện↔mất trong 10 s** (~1,4 Hz), OCR chạy 14 lần, trạng thái không bao giờ ổn định.
   - **Vòng lặp**: item vẽ ⇒ ô được bảo vệ ⇒ nền đổi tính là chuyển động ⇒ gỡ item ⇒ ô hết được bảo vệ ⇒ đứng yên sau 250 ms ⇒ OCR ⇒ vẽ lại ⇒ …
   - Ca K (vùng động ở ô **khác**): ổn — 1 lần hiện, khe gói tối đa 1000 ms < ttl.
   - **Nguyên nhân**: thực thi đúng chữ N3 "trừ ô chứa item đang vẽ" — lỗi thiết kế trong doc.
   - **Sửa** (Leader chọn): với ô nền động có item, chỉ so **điểm ảnh trong bbox chữ** thay vì trung bình cả ô; hoặc sau khi gỡ item vì ô nền, không OCR lại ô đó trong `BackgroundCellMs`. Test: chuỗi khung như ca J ⇒ ≤ 1 lần đổi trong 10 s.
   - Lưu ý: mới thấy trên khung tổng hợp; tần suất trên FC 26 thật tuỳ nền menu (Bước 8 kiểm).
4. **B4 — Giao thức không nói TV làm gì khi `fontPx` < `display.minFontPx`**, trong khi engine **thường xuyên** gửi như vậy — `tv-overlay/README.md:83,93`; `Layout/Layout.cs:129`.
   - **Evidence**: nhãn danh sách chữ 32 px nguồn ⇒ `fontPx=18`, `minFontPx=24` (`<rv>/3a-05-smoke3-clean.txt`, mọi item `col-1`).
   - "minFontPx = cỡ nhỏ nhất mà auto-fit được giảm tới" không trả lời được: TV bắt đầu ở 18 hay nâng lên 24? Lượt 3b phải đoán.
   - **Sửa**: một câu trong README, ví dụ "TV bắt đầu từ `max(fontPx, minFontPx)`", **hoặc** engine kẹp `fontPx ≥ minFontPx` trước khi gửi (chọn một, ghi rõ). Kèm theo NB9 về độ lớn của `fontPx`.

## Ưu tiên 1 — An toàn gọi trả phí (B1 SA, D10, N4, P3R2-D3)

| Hạng mục | Kết quả | Chỗ / evidence |
|---|---|---|
| Khoá API | Chỉ đọc từ `ANTHROPIC_API_KEY` (`Commands3.cs:44`), chỉ truyền vào `new AnthropicClient { ApiKey = apiKey }` (`AnthropicSdkTransport.cs:16`). Grep `ApiKey\|ANTHROPIC_API_KEY` trong `src`/`tests`: chỉ 4 dòng đó + 1 comment + 1 log cảnh báo **không có giá trị**. Không ghi file, không log. Khoá rỗng ⇒ không tạo transport ⇒ 0 HTTP | `<rv>/3a-03-smoke.txt` (smoke 10: `HTTP thật: 0`) |
| `inFlight` | OK — 3 `ProcessAsync` song song cùng 40 câu ⇒ **1 HTTP** | harness ca D; `Translation.cs:309-321,337-340` |
| Thử lại qua phiên ≤ 2 | OK cho lỗi đầu ra (slot sai, refusal): dừng ở `attempts=2` | harness ca C, F; `Translation.cs:195-200` |
| Tách đôi lô bị bỏ (P3R2-D3) | Đúng thiết kế; **chi phí**: refusal toàn lô ⇒ 79 HTTP, 6,4× ký tự cho lần đầu (tính tiền) | harness ca F |
| Lỗi tạm thời | Lùi 30 s, không `attempts`; lặp mãi thì chỉ bị chặn bởi trần (41 lô ≈ 98% trần) | harness ca B — NB3 |
| Lỗi không tạm thời | **B2** | harness ca A |
| Trần 100k/ngày | Giữ chỗ **trước** mỗi lời gọi (`Translation.cs:347-348`), kể cả các lần tách đôi ⇒ trần đúng nghĩa trên ký tự đã gửi. File đọc hỏng ⇒ fail-**open** (NB4) | harness ca E |
| SDK tự retry | `MaxRetries` mặc định **2** (gồm timeout, 408, 409, 429, 5xx — đọc từ `Anthropic.xml` của gói 13.9.0) ⇒ một lần giữ chỗ có thể thành 3 request; timeout có thể đã tính tiền. Engine đã có lùi riêng | NB2 |
| N4 không gọi LLM cho dòng hoa ≤ 3 từ / ≤ 4 ký tự | OK — `LlmEligibility` (`Translation.cs:58-70`); ngày `{N1}` ⇒ `not-eligible` | `<rv>/replay-cache-clean/misses.json` |
| Giấy phép `Anthropic` 13.9.0 | Apache-2.0 (`.nuspec`) | — |

## Ưu tiên 2 — Độ chính xác giao thức

So serializer (`Protocol/Protocol.cs:9-34`, camelCase, `WhenWritingNull`) với README *Giao thức*:
- **Khớp**: `v`, `sessionId`, `seq`, `ttlMs`, `display{minFontPx,holdMs}` — **không có `fontScale`** (`DisplaySpec` chỉ 2 trường); `items[]`: `id`, `box{x,y,w,h}`, `text`, `alt`, `fontPx`, `group`, `maxLines`, `align`, `bold`, `textColor`, `cover{mode,color}`, `room`.
- `alt`/`room` null ⇒ **bỏ khỏi JSON** (README cho phép "vắng hoặc null").
- Màu `#AARRGGBB` hoa (`ColorSampler.Hex`); hello `{"hello":{"v":1,"token":…}}`, token đã chuẩn hoá hoa; reply `ok/reason`; cổng 47820; dòng ≤ 262144 byte — đều khớp.
- **Lệch / mơ hồ:**
  - **B1** (`id` trùng).
  - **B4** (`fontPx` < `minFontPx`).
  - NB10: engine **không tự giữ giới hạn** ≤ 200 item / `text` ≤ 1000. Harness ca M: 280 item, 106.380 byte ⇒ TV bỏ **cả gói**, màn trống.
  - NB12: README:108 viết "Động ⇒ `items:[]`" nhưng engine (N3) gửi gói **chỉ bỏ các item giao ô đổi**. TV vẫn đúng vì mỗi gói là trạng thái đầy đủ, nhưng câu chữ phải sửa.
  - `cover.mode` lấy từ cấu hình được `ToLowerInvariant()` rồi gửi thẳng; giá trị lạ (`"foo"`) ra dây, README không nói TV làm gì ⇒ engine nên kiểm cấu hình (NB12).
  - README:94 `solo-<id>` vs engine `solo-<n>` — vô hại.

## Ưu tiên 3 — Logic nhấp nháy / bố cục (N1, N3, P3R2-D2, N5, D7)

| Hạng mục | Kết quả |
|---|---|
| N1 tách hàng 3 cột bị gộp | OK — `rv-a-inbox`: mỗi hàng ra 3 item `col-1/2/3` |
| Chuỗi A,A,B,B giữ 10 s/ảnh (smoke 6) | `02-a`: **bộ item đổi 0 lần** (11 gói nhịp tim); `04-b`: **0 lần** ⇒ đứng yên không nhấp nháy (`<rv>/3a-03-smoke.txt`, `<rv>/3a-04-smoke6-verbose.txt`) |
| Ô nền động | **B3** khi item nằm trong ô động; ổn khi ô động ở chỗ khác (ca K) |
| Chế độ trận | Một lần đổi trang **toàn màn** (menu → menu) cũng kích hoạt (log `chế độ trận` ở mọi lần A→B trong smoke), item hiện lại sau **1500 ms** thay vì 250 ms (harness ca L) — NB7 |
| `id` ổn định (P3R2-D2) | OK giữa các khung (02-a, 04-b không đổi); **B1** trong cùng khung |
| D7 lấy mẫu màu | OK — hàng chọn ra chữ `#FF14181E` / che `#FFE1E6EB`; hàng thường `#FFEBEBEB` / `#FF1A2638` (nền thật `#1A2638`, khớp tuyệt đối); `solid` vì vành đồng nhất |
| N5 `room` | Có cho mọi item (`Y`), không kiểm chi tiết hình học |
| D6 gộp đoạn văn | **Hỏng trên email của mình**: hai dòng "Your contract offer has been" (OCR cao 23 px) / "accepted." (29 px) lệch 20,7% > `MergeHeightTolerance` 0,20 ⇒ không gộp ⇒ câu seed "Your contract offer has been accepted." trượt; LLM sẽ nhận 2 mảnh câu (`<rv>/3a-06-email-merge.txt`) — NB8 |
| Nhóm cột | Tiêu đề "Inbox" (46 px đậm) bị gộp vào `col-1` với tên người gửi (32 px) vì `GroupHeightTolerance` 0,30 ⇒ tiêu đề co về cùng `fontPx` 18 — NB9 |

## Ưu tiên 4 — An toàn dữ liệu

| Hạng mục | Kết quả |
|---|---|
| Cache ghi nguyên tử | OK — temp + `File.Move(overwrite)` (`Translation.cs:182-192`); `engine.local.json` cũng vậy (`EngineConfig.cs` `Save`) |
| `reviewed` không bị ghi đè | OK — `Put(machine)=False`, `Put(rejected)=False`, file sau `Save` vẫn `reviewed` (harness ca G); seed `reviewed` thắng cache người dùng (`Translation.cs:148`) |
| `Save` đồng thời | `Task.Run` dịch nền có thể chạy chồng nhau (`FramePipeline.cs:212`) ⇒ 2 `Save` cùng `fc26.json.tmp`: lần chạy đầu của harness có **1 IOException/400**, file không hỏng; lần sau 0/400. Không mất dữ liệu (entry vẫn trong bộ nhớ), nhưng `ProcessAsync` bỏ dở các lô còn lại — NB6 |
| `usage/*.json` | Ghi **không** nguyên tử (`Translation.cs:243`). File cụt/ rỗng ⇒ `Used=0` (trần **reset**). File sai kiểu (`[1]`, `{"chars":"x"}`) ⇒ `InvalidOperationException` không bắt (`:225` chỉ bắt `JsonException/KeyNotFound/IO`) ⇒ ném ở `rt.Usage.TakeCapWarning()` trong vòng `run` (`Commands3.cs:240`) ⇒ `run` chết (harness ca E) — NB4 |
| `VIETSUB_HOME` | Mọi đường dữ liệu đi qua `VietSubHome.Resolve()` (`Commands3.cs:30-34,82`); test dùng `Directory.CreateTempSubdirectory`; smoke của mình chỉ ghi `cache/fc26.json` + `usage/2026-11-18.json` trong home tạm; `%LOCALAPPDATA%\VietSub` thật không tồn tại |
| **Bản dịch giả lọt vào cache thật** | `--translator fake` ghi `"[VI] Club Secretary"` vào cache người dùng với `provider: anthropic`, `model: claude-sonnet-5-5`, `status: machine` (`Translation.cs:376-381`), và **trừ trần ngày** (144 ký tự). Chạy `replay --translator fake` mà quên `VIETSUB_HOME` ⇒ cache thật bị bẩn ⇒ `run` sau đó vẽ "[VI] …" lên TV (`<rv>/3a-05-smoke3-clean.txt`) — NB5 |

## Test · build · smoke

- `dotnet build engine --no-incremental`: `0 Warning(s)`, `0 Error(s)`. `dotnet test engine`: `Passed!  - Failed:     0, Passed:   143` (`<rv>/3a-01-build-test.txt`). Khớp dev report.
- **Smoke 1–3, 6, 10** (ảnh **của mình**, `<rv>/synthetic/`, không phải FC 26/R-D) — `<rv>/3a-03-smoke.txt`, `<rv>/3a-05-smoke3-clean.txt`:
  - smoke 2 fake: 19 + 7 item, `HTTP thật: 0`.
  - smoke 3 cache (home sạch): 16 + 3 item; câu lạ không có item (`misses` lý do `no-translator`); `gọi backend: 0 · HTTP thật: 0`.
  - smoke 6: như bảng ưu tiên 3.
  - smoke 10: `[llm] cảnh báo: không có ANTHROPIC_API_KEY — chỉ dùng cache/glossary (không gọi LLM).`, exit 0, `HTTP thật: 0`; `warm` không `--yes` ⇒ in số câu/ký tự rồi exit 1.
- **Cờ dòng lệnh**: `--no-send` trần (như Phase doc D4/Blackbox) ⇒ `LỖI (1): Thiếu giá trị cho --no-send.` — parser đòi `--no-send true`. README engine đã ghi `--no-send true`; Phase doc ghi sai (NB11).

## Non-blocking

| # | Chỗ | Vấn đề | Đề xuất |
|---|---|---|---|
| NB1 | `Translation.cs:195-200` vs D10 | Câu bị loại được thử lại **ngay trong cùng phiên** (attempts 1→2, harness ca C). D10 ghi "tối đa 1 lần/`sourceKey`/phiên"; B1 ghi "tổng ≤ 2". Có chặn trên, nhưng doc mâu thuẫn | Leader chốt: giữ một tập "đã thử phiên này" để lần 2 chỉ xảy ra ở phiên sau |
| NB2 | `AnthropicSdkTransport.cs:16` | SDK tự retry 2 lần (gồm timeout) ngoài vòng lùi của engine | `MaxRetries = 0` (engine tự lùi 30 s) |
| NB3 | `Translation.cs:347-357` | Lỗi tạm thời vẫn bị trừ trần ⇒ mạng chập chờn cả tối đốt hết ngày (ca B) | Hoàn lại ký tự cho 429/5xx (không tính tiền); giữ cho timeout |
| NB4 | `Translation.cs:225,243`; `Commands3.cs:240` | `usage` ghi không nguyên tử; hỏng ⇒ reset trần hoặc ném làm chết `run` | temp+rename; parse lỗi ⇒ **coi như đã chạm trần** (fail-closed) và bắt mọi ngoại lệ |
| NB5 | `Translation.cs:376-381`; `Commands3.cs:41` | Fake ghi vào cache người dùng với provider `anthropic`, trừ trần | Provider lấy từ backend; fake không `Save`/ không trừ trần, hoặc dùng file cache riêng |
| NB6 | `Translation.cs:182-192`; `FramePipeline.cs:212` | `Save` không khoá file; dịch nền chạy chồng | Khoá quanh ghi file; hoặc một hàng đợi dịch duy nhất |
| NB7 | `Motion.cs:116-121` (N4) | Đổi trang menu toàn màn = chế độ trận ⇒ +1,25 s mỗi lần chuyển màn | Đòi chuyển động **kéo dài** (vd ≥ 2 lần đổi liên tiếp hoặc ≥ 1 s) mới vào chế độ trận |
| NB8 | `EngineConfig.cs:48`, `Layout.cs:61` | `MergeHeightTolerance` 0,20 quá chặt so với độ dao động hộp OCR (Dev đã nới `GroupHeightTolerance` lên 0,30 vì đúng lý do này) | Nới lên ~0,30, hoặc so theo cao chữ x/ trung vị từ |
| NB9 | `Layout.cs:129`, `EngineConfig.cs:31` | `fontPx` = cao hộp OCR × 0,78: chữ 32 px nguồn ⇒ 18 (56%); tiêu đề 46 px đậm ⇒ 34 hoặc bị kéo về 18 do nhóm cột | Tối nay chỉnh `display.fontScale` (~1,0–1,1) bằng `engine.local.json`; tách tiêu đề khỏi cột khi cao lệch > 30% |
| NB10 | `FramePipeline.cs:224-233` | Không giữ giới hạn ≤ 200 item / `text` ≤ 1000 ⇒ TV bỏ cả gói (ca M) | Kẹp ở `Send` (giữ item trên xuống), cắt `text` |
| NB11 | Phase doc D4 + Blackbox bước 2 | `--no-send` không giá trị bị từ chối | Sửa doc thành `--no-send true` (hoặc parser nhận cờ trần) |
| NB12 | README:94,108; `Layout.cs`/`ColorSampler.DecideCoverMode` | Câu "Động ⇒ `items:[]`" không còn đúng; `cover.mode` cấu hình lạ đi thẳng ra dây | Sửa câu; kiểm `coverMode` ∈ auto/solid/card/none khi nạp cấu hình |
| NB13 | `Commands3.cs:244` | Vòng `run` chỉ bắt `EngineException` 2/3/4/6 + huỷ; ngoại lệ COM/IO lạ từ chụp/OCR làm `run` thoát. `CaptureSession` đã quy đổi các ca biết trước (mất cửa sổ ⇒ 2, thu nhỏ ⇒ 3, không khung ⇒ 4, DWM/kích thước ⇒ 6) — **chưa thấy** đường crash cụ thể | Bắt mọi ngoại lệ trong vòng, log loại + thông điệp, thử lại sau 2 s |

## Chưa kiểm được

- **`run` trên cửa sổ thật** (WGC + tạo lại pool khi đổi cỡ + NB8/NB9 Phase 1): không bật cửa sổ chụp trong lượt này; chỉ đọc code `CaptureSession` (`WindowCapturer.cs` 50–107).
- **LLM thật**: cố ý không gọi (risk gate). Schema/`OutputConfig`/caching chỉ đọc code.
- TV (3b): chưa có phía nhận; giao thức chỉ đối chiếu văn bản ↔ serializer.
- B3/NB7/NB8/NB9 chỉ trên khung tổng hợp; mức độ trên FC 26 thật phải đo ở Bước 8.

## Câu hỏi mở — tối nay, trên cửa sổ PS Remote Play thật, cái gì vỡ trước?

1. **Cỡ chữ**: chữ Việt sẽ **nhỏ hơn rõ** chữ Anh nó che (~56% với nhãn danh sách, NB9) và dưới `minFontPx` (B4). Chuẩn bị chỉnh `display.fontScale` ngay từ khung đầu.
2. **Hộp thư lặp người gửi/ ngày** ⇒ `id` trùng (B1) ⇒ nhãn mất hoặc tráo màu hàng chọn — ở chính màn được kiểm chất lượng.
3. **Mỗi lần chuyển trang menu chờ ~1,5 s** (NB7). Nền menu FC 26 có chuyển động sau chữ ⇒ nhấp nháy (B3).
4. **Email**: đoạn văn hai dòng bị cắt thành mảnh (NB8) ⇒ bản dịch rời rạc; ngày giữ tiếng Anh.
5. **Lần gọi LLM thật đầu tiên**: nếu ra 400/401 thì trong vài phút mất phần lớn trần ngày và mọi câu đã thấy bị khoá `rejected` (B2). Đề xuất runbook: chạy `warm --translator claude --yes true` trên **một** ảnh nhỏ và đọc dòng `[llm]` **trước** khi `run` — làm việc này dù B2 đã sửa hay chưa.

---

## Re-review fix 3a

**Verdict: PASS** — 4 blocking của review 3a đã đóng. Không có blocking mới trong phạm vi `engine/`.

Đối chiếu [dev report fix3a](261114-epic1-phase3-dev-report-fix3a.md) với dòng "Sau review 3a — Rev 1.3" của Phase doc.
- **Điều kiện chạy**: `VIETSUB_HOME` tạm, `ANTHROPIC_API_KEY=''`, proxy chết, **0 HTTP thật**.
- **Harness**: chạy lại harness ngoài repo của Reviewer, **không sửa một dòng**, trên mã đã sửa.
- **Build · test**: `dotnet build engine --no-incremental` ⇒ `0 Warning(s)`, `0 Error(s)`; `dotnet test engine` ⇒ `Passed: 183` (`<rv>/re3a-01-build-test.txt`).

| Mục | Kết quả (harness `<rv>/re3a-02-harness.txt`, smoke `<rv>/re3a-03-smoke.txt`) |
|---|---|
| **B1** `id` trùng | Ca I: **8 item / 8 `id` khác nhau** (trước: 6/8). End-to-end `rv-a-inbox`: 19 item, `dup ids: {}` |
| **B2** lỗi không tạm thời | Ca A (401 mọi lời gọi, 40 câu): **1 HTTP**, trừ trần **0** ký tự, **0 entry `rejected`**, lần 2–3 cùng phiên 0 HTTP thêm; phiên mới sau khi sửa khoá **được thử lại** (trước: 158 HTTP, 30.720 ký tự, khoá vĩnh viễn). Map thật: `AnthropicSdkTransport.cs:23` `Classify`, `:51-57`; `MaxRetries = 0` ở `:17` (NB2) |
| **B3** nhấp nháy trên ô nền động | Ca J: **1** lần hiện↔mất trong 10 s, OCR 1 lần, trạng thái cuối `Stable` (trước: 28 lần, OCR 14). Ca K không đổi (1 lần, khe gói 1000 ms < ttl). Ca "chữ đổi thật" Dev có test `CaseJ2` — không chạy lại |
| **B4** `fontPx` < `minFontPx` | README *Giao thức* ghi quy tắc `max(fontPx, minFontPx)`; TV đã làm từ 3b (`AutoFit.kt:54`). `fontScale` mặc định 1,0 |
| NB1 thử lại cùng phiên | Ca C: lần 2–3 cùng phiên **không** gọi lại (HTTP=1, attempts=1) |
| NB3 hoàn ký tự 429/5xx | Đường thật truyền `refundable: true` (`AnthropicSdkTransport.cs:51-52`). Ca B của harness vẫn trừ trần vì harness tự ném `ClaudeTransportException(true, …)` không `refundable` — dùng hàm khởi tạo cũ, **không phải** lỗi của mã |
| NB4 file usage hỏng | Ca E: cụt / rỗng / `[1]` / sai kiểu ⇒ **coi như chạm trần** (`Used=100000`, `TryReserve=False`), không ném |
| NB5 fake | `replay --translator fake` home sạch: `cache/fc26.json` = `"entries": []`, không có file `usage`, ký tự 0 |
| NB6 `Save` đồng thời | Ca H: 0/400 IOException |
| NB7 đổi trang = chế độ trận | Ca L: item hiện lại sau **250 ms** (trước 1500 ms) |
| NB8 gộp đoạn email | Item `'Đề nghị hợp đồng của ông đã được chấp nhận.'` (`maxLines=4`) — gộp đúng, trúng seed |
| NB10 ≤ 200 item | Ca M: 280 dòng ⇒ gửi **200 item, 74.996 byte** |
| NB11 cờ trần | `replay … --no-send` (không giá trị) chạy được |
| C1 vệt chữ gốc (`boxPadPx`) | `BoxPadPx = 3` (`EngineConfig.cs:60`), nới hộp ở `FramePipeline.cs:74-75`; `id` vẫn tính từ hộp chưa nới. Ảnh review 3b đã không thấy vệt; test `Sliver_C1…` của Dev đỏ→xanh — không chạy lại trên emulator |
| Ca F refusal toàn lô | Không đổi, đúng thiết kế P3R2-D3: lần đầu 79 HTTP, 6,4× ký tự; lần sau cùng phiên **0** (NB1) |

**Non-blocking còn lại:**
1. Engine vẫn gửi `maxLines = số dòng gốc + 2` cho **nhãn một dòng** (`'Cập nhật hợp đồng' 3`, mọi nhãn = 3). Đây là phần engine của blocking B1 trong [review 3b](261114-epic1-phase3-review-3b.md) (nhãn bị bẻ dòng, tràn khỏi hàng) — cần chốt ở vòng sửa 3b (engine gửi 1 cho nhãn, hoặc TV co chữ trước khi xuống dòng).
2. NB9 phần "tách tiêu đề khỏi cột" chưa làm. Dev để Bước 8 — chấp nhận.
3. NB13 (vòng `run` bắt mọi ngoại lệ) chỉ đọc code — cần cửa sổ WGC thật.
