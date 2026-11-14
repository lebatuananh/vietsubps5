# Giao việc cho worker — kỹ năng dùng ở mọi công đoạn

> **Không phải một công đoạn.** Leader dùng file này ở [02](../leader-stages/02-discovery.md) (BA/SA), [03](../leader-stages/03-write-docs.md) (SA gate), [04](../leader-stages/04-implement.md) (Dev, Reviewer, End User) và bất cứ lúc nào cần triệu hồi một vai.
>
> **Không phụ thuộc platform.** Cờ CLI và cơ chế chạy nền của từng nền tảng: [agent-platform/](../agent-platform/README.md).

---

## 1. Brief: context-by-reference

**Không viết prompt dài giải thích lại dự án.** Brief chỉ trỏ tới tài liệu:

```text
You are the <ROLE> for this work package.

Read fully:
1. CLAUDE.md
2. workflow/roles/<role>.md
3. project-docs/<batch>/<phase-or-entry-file>.md

Task: <one-paragraph pointer>. Write your report to project-docs/<batch>/<report-file>.md.
Follow the referenced source-of-truth documents and report with evidence.

Open question (answer it even if the list above looks complete):
What important thing did the Leader NOT ask? Which concept in this brief is merged or named wrong?
```

- Worker chỉ đọc **sổ tay vai của mình** + file brief trỏ tới. Chỉ Leader đọc trọn quy trình.
- **Brief viết bằng tiếng Anh** — tránh lỗi encoding giữa các CLI trên Windows. Report của worker: *"Vietnamese prose, identifiers verbatim in English"*.
- Ngắn: vai + required reading + task pointer. **Không kể lại nội dung file được trỏ.**
- Artifact được trỏ **phải tồn tại và đã cập nhật trước khi** gọi worker.
- Resume đúng session ⇒ chỉ nêu phần **đã đổi**.
- Brief ghi ra file `project-docs/<đợt>/_workers/briefs/<vai>-<việc>-brief.txt` — để tra lại được, và để truyền qua stdin/file thay vì argument (§2).
- Worker sẽ bật job nền dài (lô lab, runner tách rời) ⇒ brief ghi rõ *chờ job xong rồi mới viết report và dừng* — worker hay kết thúc lượt ngay sau khi bật job, không để lại report.

🔴 Mọi business rule, acceptance criterion, thiết kế, danh sách file **phải sống trong tài liệu được trỏ**, không sống trong brief. Còn phải chép spec vào brief ⇒ artifact của đợt chưa làm đúng vai source-of-truth.

🔴 **Brief không được thêm hay bớt phạm vi so với Phase doc.** Trước khi gửi, đối chiếu từng việc trong brief với các mục *không động / không sửa / ngoài phạm vi* của Phase doc. Muốn đổi phạm vi ⇒ sửa Phase doc (Rev) trước, brief sau.

🔴 **Brief luôn có mục mở không khoanh vùng** ([02-discovery §1b](../leader-stages/02-discovery.md)) — kể cả brief cho Dev và Reviewer.

### 🔴 Brief theo vai — đọc phần riêng trước khi viết

Khuôn trên là phần chung. Mỗi vai/lượt có thứ brief **bắt buộc** thêm, nằm ở công đoạn tương ứng:

| Vai / lượt | Brief phải có thêm | Đọc ở |
|---|---|---|
| BA/SA — discovery lượt 1 · lượt 2 | Đính bảng 6 ô làm ràng buộc · lượt 2 là chất vấn, lấp barem | [02-discovery §1, §1b, §2](../leader-stages/02-discovery.md) |
| SA — thẩm định Phase doc | 6 tiêu chí · **chép nguyên ba điều kiện BLOCKER** | [03-write-docs — Gate](../leader-stages/03-write-docs.md#-gate-bắt-buộc-sa-thẩm-định-phase-doc) · [ba-sa §5](../roles/ba-sa.md) |
| Dev — lượt 1 (phản hồi doc) · lượt 2 (code) | Lượt 1 cấm code · lượt 2: bảy thứ phải có trước khi gọi + khối `NOT YOURS` | [04-implement — Bảy thứ · Gate đồng thuận](../leader-stages/04-implement.md) |
| Reviewer — review · re-review | Session độc lập · re-review nêu *đã verify gì* + *rủi ro hồi quy của bản sửa* · bảng BINDING → `file:line` → ca test (Phase đụng tiền) · đối chiếu docs chính thức (API ngoài) · một dòng khớp/lệch cho **mỗi** màn mockup · `file:line` cho mọi claim ĐẠT | [04-implement — Giao Reviewer · Leader kiểm sau mỗi lượt](../leader-stages/04-implement.md) · [reviewer](../roles/reviewer.md) |
| End User | **Brief mù**: không Phase doc, diff, tiêu chí — chỉ dùng gì, ở đâu, dựng ngữ cảnh tới đâu | [04-implement — End User](../leader-stages/04-implement.md#khi-nào-gọi-end-user) · [end-user](../roles/end-user.md) |
| Phase đụng UI (mọi vai chạy test) | Gọi đích danh MCP server `playwright` + đường lưu ảnh evidence | [agent-platform/README](../agent-platform/README.md#browser-playwright-mcp--dùng-chung-mọi-platform) |
| **Theo platform/model sắp gọi** | Cách truyền brief, ký tự cấm, timeout · thứ model đó hay làm sai ⇒ brief phải đòi thêm gì | Profile của platform: mục *Harness / cách gọi* và **`## Kinh nghiệm`** (§2) |

### 🔴 Khối "tài nguyên không phải của bạn" — bắt buộc trong brief của vai chạy lệnh

Worker coi việc dọn dẹp, khôi phục, dựng state test là "giúp" — và đã giết server của Leader, xoá dữ liệu thật User đang dùng, tự `git checkout` file dùng chung, để fixture ở trạng thái công khai, trỏ harness vào API thật tốn tiền, **tự dò/vét mật khẩu để lấy quyền dựng môi trường test**. Câu chung chung kiểu *"clean up after yourself"* không chặn được. Mọi brief cho vai được chạy shell có khối này (tiếng Anh, điền giá trị thật):

```text
NOT YOURS — do not touch:
- Running processes owned by the Leader/User: <port → PID, purpose>. Stop only processes whose PID you recorded when YOU started them. Never kill by name, port or path pattern.
- Git: only status / diff / log / show. If you damage a shared file (log, doc), STOP and say so in the report; do not restore it yourself.
- Existing data: never modify or delete rows you did not create (<list sensitive tables>). Need a special state? Create your own fixture, keep it hidden/unpublished, delete it at the end, list its ids in the report.
- Paid services: harness and fake servers must point at <fake target>; never let a proxy or default fall back to <real paid endpoint>.
- Credentials: NEVER guess, brute-force, or try a list of passwords against any service or DB. If you lack access to set up a test environment, REPORT IT — or spin up your OWN throwaway database where you are superuser. Do not probe the Leader's/User's databases.
```

Kiểm được: mọi file trong `_workers/briefs/` của vai Dev/Reviewer/End User có khối `NOT YOURS`.

🔴 **Khi worker xong, Leader đọc kỹ MỌI file worker để lại** (`playtest/`, script, fixture — ở **mọi repo** bị đụng) **trước khi chuyển bước / đưa lệnh commit cho User** — không chỉ report. Script thử nghiệm bị gitignore ⇒ kiểm bằng `git -C <repo> status --ignored`, không chỉ `git status`. File dò mật khẩu / credential / thao tác nguy hiểm còn trong working tree là rủi ro thật (lộ bí mật, và với harness có safety classifier thì còn làm flag session Leader sau đó). Thấy ⇒ xoá, ghi lessons, không commit.

---

## 2. 🔴 Đọc profile platform NGAY TRƯỚC khi gọi — không gọi theo trí nhớ

**Chọn platform theo policy trước khi đọc profile:** vai nào chạy trên platform/model nào do `workflow/agent-platform/worker-policy.local.md` của người đang chạy Leader quyết (gitignore, mỗi người một bản; thiếu ⇒ hook chặn gọi worker). Policy phải tôn trọng luật chung ở [agent-platform/README](../agent-platform/README.md#-chọn-platform--model-cho-worker--policy-cá-nhân-bắt-buộc) và luật model riêng của dự án ở `CLAUDE.md`. Session cũ trên platform mà policy nay không cho **không được resume**; chuyển context qua artifact và ghi lý do đổi platform vào sổ session/workflow log.

**Đây là rule bị vi phạm nhiều nhất.** Ở đúng thời điểm sắp gọi, gõ lệnh theo trí nhớ bao giờ cũng nhanh hơn mở file.

| Cơ chế làm trí nhớ hỏng | Cụ thể |
|---|---|
| **Cờ giữa các CLI ngược nhau** | Read-only: Cursor `--mode plan` · Codex `-s read-only` · Antigravity **không có cờ nào** |
| **CLI đổi nhanh** | Bản cũ không có cờ X, bản mới có — workaround cũ áp lên CLI mới thì sai mà không báo |
| **Mọi lỗi gọi sai đều exit 0** | Không có tín hiệu nào ép đi tra lại. Worker im lặng, report rỗng, Leader tưởng worker kém |

### Năm câu phải trả lời được — bằng cách **đọc profile**, không bằng cách nhớ

```
[ ] Vai này cần ghi file hay chạy lệnh? Cờ/allow-rule nào cấp đúng quyền đó trên platform này?
[ ] Brief truyền qua đường nào — argument, stdin hay file?
[ ] Worker cần tool gì đặc biệt (browser, DB, runtime)? Có sẵn · dựng thêm được · hay không có?
[ ] CLI có TỰ CẮT lượt dài không? Timeout mặc định bao nhiêu, cờ nào nới?
[ ] Mục `## Kinh nghiệm` của profile nói gì về ĐÚNG vai + model này — lỗi đã gặp, brief phải đòi thêm gì?
```

🔴 **Không trả lời được cả năm ⇒ chưa gọi.** Ghi vào workflow log đã đọc profile nào, ngày rà soát của nó.

🔴 **Bẫy chung mọi CLI khi gọi từ PowerShell 5.1**: prompt truyền làm **argument** mà có **dấu nháy kép bên trong**, hoặc có dòng bắt đầu bằng `-`/`->`/`--` ⇒ bị tách thành nhiều argument hoặc hiểu thành cờ ⇒ CLI thoát, **exit 0 hoặc 2, không làm gì**. **Cách tránh**: ghi brief ra file, truyền **prompt một dòng không có nháy kép** trỏ tới file (hoặc qua stdin với Codex). Bullet trong brief dùng `*`, không dùng `-`.

### 🔴 Probe trước khi giao việc thật

Chạy **probe nhỏ nhất** xác nhận từng năng lực vai cần (chạy shell · đọc file · ghi file · browser) ở ba thời điểm:

- lượt đầu trên một platform;
- 🔴 **CLI vừa đổi version** — kết luận cũ về quyền/cờ có thể đã lật ngược;
- lượt đầu của một vai **bắt buộc chạy lệnh** (Dev, Reviewer) trong session mới.

🔴 **Probe bằng giá trị KHÔNG ĐOÁN ĐƯỢC.** Nhờ in hash commit (`git rev-parse --short=8 HEAD`) rồi đối chiếu giá trị thật; ghi file thì dùng token ngẫu nhiên. **Không** nhờ `echo "OK"` hay in tên nhánh — model có thể trả đúng mà không chạy gì. Ghi kết quả vào profile kèm version CLI.

### 🔴 Sẵn sàng cho smoke/blackbox web — kiểm TRƯỚC khi cần

Phase có smoke qua web ⇒ platform của **mọi vai sẽ chạy test** phải **thao tác được trình duyệt**. Kiểm ở kickoff đợt, và ngay trước lượt test nếu đã đổi platform hoặc CLI/MCP đổi version. Công thức probe browser: [agent-platform — Browser](../agent-platform/README.md#browser-playwright-mcp--dùng-chung-mọi-platform).

Probe hỏng ⇒ **vá trước, giao việc sau**. Vá cần quyền Leader không tự cấp được ⇒ trình User đúng dòng cần thêm, **không lách**. Không có trình duyệt thì **không** giao phần smoke UI cho platform đó rồi nhận kết quả "đã test API".

---

## 3. 🔴 Quota/cost preflight

**Không giao task dài, gate chặn tiến độ, hoặc fan-out chỉ vì CLI đang gọi được.** Kiểm quota của **đúng platform + model + pool** định dùng; quota ở platform khác không phải bằng chứng còn sức chứa.

| Mức evidence | Được ghi | Không được suy diễn |
|---|---|---|
| **Measured** | Dashboard/CLI chính thức trả usage/limit/remaining/reset **trước và sau** lượt, cùng pool | Không đổi đơn vị provider sang "% quota" nếu provider không cho tổng |
| **Estimated** | Ước lượng từ usage/token/cost chính thức cùng pool, nêu công thức + độ bất định | Không ghi như số đo thật |
| **Unknown** | Không có số đo, hoặc không có quyền đọc billing | Không quy đổi độ dài prompt, thời gian chạy, số tool call, exit code thành quota |

```text
[ ] Auth/billing mode: subscription, included credits hay API pay-as-you-go?
[ ] Pool thực tế: model này chung quota với model/platform nào?
[ ] Platform này khác platform đang làm Leader và hợp lệ cho vai này?
[ ] Snapshot trước lệnh (usage/remaining + limit + reset), hoặc ghi rõ unknown
[ ] Task sizing: nhỏ / vừa / dài, vì sao
[ ] Overage: Never, hay đã có ủy quyền ngân sách rõ ràng?
[ ] Không đủ/unknown cho gate dài ⇒ giảm scope thành checkpoint có artifact, đợi reset, hoặc đổi pool
```

Task dài ⇒ yêu cầu worker ghi **checkpoint giữa lượt** (report partial) để quota cạn vẫn còn handoff dùng được. Quota cạn ⇒ ghi nguyên văn lỗi + reset time vào log; **exit 0 không làm lỗi quota biến mất**.

🔴 **Sau 2–3 lượt lớn của một platform subscription (Codex, Claude Code…) trong một ngày, coi quota là rủi ro cho lượt kế** — nhất là gate dài (đọc nhiều doc). Worker chỉ ghi artifact ở bước cuối ⇒ hết quota giữa lượt = **mất cả lượt, không có output**. Chuẩn bị sẵn brief dự phòng **không phụ thuộc session** (handoff qua artifact, không resume) để chuyển platform ngay.

🔴 **Hết quota ⇒ đổi platform, không hạ model** khi mọi model của platform đó chung một allowance — hạ model không tạo thêm quota.

---

## 4. Quyền theo vai — cấp RỘNG đủ để làm trọn vai

> User: *"dev và reviewer đều phải có quyền khá rộng. Dev thì phải sửa được file, phải smoke test được; reviewer thì cũng phải viết báo cáo, phải smoke test, check git này nọ. Nếu bó chân bó tay nó thì làm sao nó làm tốt được."*

| Vai | Phải làm được | Thiếu quyền thì hỏng thế nào |
|---|---|---|
| **BA/SA** | Đọc repo · **chạy lệnh chỉ đọc** (`grep`, `git log/status/diff`, đọc DB) · ghi report + mockup trong thư mục đợt. Không sửa code | Không dẫn được evidence `lệnh + output` ⇒ claim thành suy đoán |
| **Dev** | Sửa file · chạy test/typecheck/build · dựng dữ liệu · bật/tắt server · smoke | Không đạt DoD ⇒ Leader chạy thay ⇒ mất trọn một vòng review |
| **Reviewer** | Đọc diff · chạy `git` · test/build · query DB/log để đối chiếu evidence của Dev · **tự dựng dữ liệu riêng + browser khi kế hoạch yêu cầu smoke lại** · ghi report | Chỉ đọc code chay ⇒ bỏ lọt đúng lớp lỗi "xanh hết nhưng sai ngầm" |
| **End User** | Chạy client · gọi API thật · browser | Nhận xét thành suy đoán |

🔴 **Quyền ghi file KHÔNG tự động kèm quyền chạy lệnh** — ở nhiều CLI là hai cờ/rule riêng, cấp thiếu thì worker sửa được code nhưng mọi lệnh bị từ chối, đôi khi im lặng. Ngược lại cũng có: chạy lệnh được mà **ghi file bị deny**.

🔴 **Platform không cấp được quyền vai đó cần ⇒ ĐỔI PLATFORM, đừng hạ chuẩn công việc.** Giao Reviewer cho platform không chạy được lệnh rồi nhận bản review đọc-chay là **tự lừa mình**: có artifact, có verdict, nhưng gate không được gác.

⚠️ Dev và Reviewer nên khác **pool quota**. Buộc dồn về một platform ⇒ **bắt buộc khác model**, ghi lý do vào log.

---

## 5. 🔴 Mọi worker chạy nền phải có phép quan sát độc lập

**Giao worker rồi chờ một sự kiện có thể không bao giờ đến là sai.** Tiến trình treo thì không bao giờ kết thúc, và tín hiệu "xong" không bao giờ đến.

⇒ **Ngay lúc giao worker, dựng phép quan sát** thoả bốn yêu cầu:

| Yêu cầu | Vì sao |
|---|---|
| Bám vào **artifact**, không bám tiến trình | Artifact phản ánh công việc thật; tiến trình còn sống không chứng minh gì |
| Chạy **song song, không chặn lượt của User** | User phải nhắn được cho Leader trong lúc chờ |
| Có **ngưỡng thời gian** dứt khoát | Không có ngưỡng thì "chờ thêm chút" kéo dài vô hạn |
| **Nhiều nhánh thoát**, không chỉ đường xong | Phân biệt "việc hỏng" với "phép đo hỏng" |

🔴 **Im lặng không bao giờ là thành công.** Worker xong im lặng y hệt worker treo — phép quan sát tồn tại để phân biệt hai cái đó.

| Nhánh | Khi nào | Hành động |
|---|---|---|
| `DONE` | Artifact có dấu hiệu hoàn thành / worker ghi dòng kết quả vào log | Dùng artifact |
| `FAILED` | Event lỗi tường minh của CLI, hoặc tiến trình biến mất mà chưa có kết quả | Đọc lỗi, quyết retry |
| `STUCK` | Quá ngưỡng, artifact **và** stream đều không tiến triển | Chẩn đoán treo |
| `POLL-BROKEN` | n lần liên tiếp không đọc được nguồn tín hiệu | Sửa cách đo, **không** kết luận việc hỏng |
| `PROGRESS` | Stream/artifact có đổi | Còn sống, đang tới đâu |

🔴 **`FAILED` neo vào cấu trúc event của CLI** (dòng `type: error/result` có `is_error`, hoặc stderr) — **không grep chuỗi tự do trên toàn stream**. Worker đọc file nào thì nội dung file đó vào stream ⇒ đọc workflow log có ghi lỗi cũ là monitor báo FAILED giả.

🔴 **`DONE` theo artifact có dấu hiệu đặc trưng, không theo chuỗi có sẵn trong brief.** CLI thường in lại prompt vào stream ⇒ chuỗi kết thúc nằm trong brief sẽ khớp ngay dòng 2. Neo vào event assistant/result, hoặc vào file report + dòng log worker ghi.

🔴 **Artifact đã đủ + stream đứng ⇒ dừng tiến trình, dùng artifact.** CLI có lúc đứng mãi ở *"thinking completed"* sau khi đã ghi đủ report.

🔴 **Phép quan sát chỉ lên tiếng ở nhánh kết thúc, ở mốc ngó lại (`CHECK`), hoặc khi cần Leader ra tay** (`DONE` · `FAILED` · `STUCK` · `POLL-BROKEN` · thiếu quyền · vượt trần chi phí). **Mỗi dòng nó in ra đánh thức Leader một lượt đầy đủ**, tức là tốn quota của model đắt nhất team. `PROGRESS`, session id, lỗi tool vặt (edit lệch chuỗi, đọc file không có) **gộp vào dòng kết thúc**, không báo từng cái. Session id lấy từ dòng `DONE` để ghi sổ.
- Ưu tiên kiểu **thoát khi có kết quả**: lệnh nền chạy tới nhánh kết thúc rồi exit ⇒ đúng một thông báo. `STUCK` cũng exit; Leader chẩn đoán xong thì bật lại.
- Đếm trần chi phí cũng vậy: chỉ báo khi **vượt trần**, không báo mỗi lần số đếm tăng.
- Ngó lại theo mốc giãn dần (bên dưới) thay cho báo từng sự kiện.

### Nhịp quan sát — như người giao việc: ngồi xem lúc đầu, ngó lại sau vài phút, rồi giãn dần

Giống người giao việc cho đồng nghiệp: lúc đầu ngồi lại xem cách làm và hướng đi có chuẩn không, vài phút sau ngó lại, ổn thì lâu sau mới xem còn chạy xuôi hay bị treo; có tín hiệu xong thì càng tốt. Cache vẫn giữ nên đánh thức thưa không đốt quota quá mức; cái lợi là bắt worker đi sai hướng từ đầu.

1. **Bật watcher** [`agent-platform/tools/watch-worker.sh`](../agent-platform/tools/watch-worker.sh) **ngay khi giao việc**, chọn chế độ theo cách Leader đang trả tiền:

   | Leader chạy bằng | Chế độ | Watcher đánh thức Leader khi | Vì sao |
   |---|---|---|---|
   | **Gói subscription** (Claude Code, Codex…) — mặc định | `sub` + `checkAt` | Mốc ngó lại `CHECK` giãn dần **120 s → 300 s → 900 s → 1800 s (lặp 1800 s)**; cộng các nhánh kết thúc `DONE` · `DONE-IDLE` · `FAILED` · `STUCK` (stream + report đứng `idleSec`, hoặc quá 115 phút) · `POLL-BROKEN` báo **ngay** khi xảy ra | Lúc đầu bắt worker đi sai hướng (đọc nhầm file, sai cờ, lỗi quyền); sau đó chỉ cần biết còn chạy xuôi hay kẹt. Cache giữ nên đánh thức thưa không đắt |
   | **API trả theo token, cache ngắn** (vd OpenRouter) | `api` | Như `sub` (không `CHECK`), thêm `TICK` sau ≤ 230 s ⇒ Leader kiểm nhanh (process, stream, lỗi) rồi đặt lại | Cache prompt hết hạn sau vài phút; chờ lâu rồi mới đánh thức thì phải nạp lại toàn bộ context theo giá đầy đủ |

2. **Mỗi lần `CHECK`**, Leader đọc **đuôi stream** (vài event cuối: tool đang chạy, text gần nhất) và đối chiếu với brief:

   | Mốc | Câu hỏi | Sai ⇒ |
   |---|---|---|
   | ~2 phút | Đọc đúng Required Reading? Probe/quyền ổn? Lệnh đầu đúng cờ? | **Ngắt ngay**, sửa brief, giao lại — đừng để nó đốt quota và làm bẩn môi trường |
   | ~5 phút | Hướng đi có khớp Phase doc không, hay bắt đầu "bơi" / làm ngoài phạm vi? | Ngắt hoặc ghi chú để soi ở review |
   | 15 → 30 phút | Còn chạy xuôi? Có lệnh nào đứng quá lâu (build treo, lệnh liệt kê khổng lồ)? | Chẩn đoán, dừng đúng lệnh kẹt |

   Ổn ⇒ bật lại watcher với mốc kế tiếp (dòng `CHECK` in sẵn `next checkAt=…`), **không** báo User. Có vấn đề ⇒ xử lý rồi báo User.
   `idleSec` (tham số 6) là lưới an toàn giữa hai mốc: lượt có build / emulator / smoke thiết bị dùng `600`, lượt đọc-viết giữ mặc định `1500` (25 phút).
   Truyền `pid` của CLI worker để `DONE` chỉ báo khi report xong **và** tiến trình đã thoát (report xong ≠ worker đã thoát, §8). Không tự viết vòng `sleep` riêng mỗi lượt, kiểu grep chuỗi lỗi trên toàn stream hay chốt xong theo tiến trình thoát.
3. **Rút bài học kép**: về worker/model (thông minh hay hallucinate, bẫy CLI nào) ⇒ cập nhật profile; về Leader (worker bơi vì doc mơ hồ? brief chung chung?) ⇒ ghi lesson cải tiến cách viết doc.

### Nguồn tín hiệu — theo thứ tự ưu tiên

| Ưu tiên | Nguồn |
|---|---|
| 1 | Dòng kết quả worker tự ghi vào workflow log |
| 2 | Stream chính thức của CLI (`stream-json`, `--json`) |
| 3 | Log do chính CLI ghi (có hợp đồng, để người đọc) |
| 4 | Tiến trình còn sống + CPU tích luỹ (thô) |
| ❌ | File/DB lưu trữ nội bộ của platform — không có hợp đồng, đổi lúc nào không báo |

⚠️ CPU tích luỹ thấp **không** chứng minh treo — worker chờ model thì CPU gần như đứng yên. Đối chiếu với nhịp gọi model trong stream/log. Treo thật: CPU thấp bất thường **và** stream + artifact đứng yên 30+ phút.

⚠️ **Output rỗng thì truy vì sao rỗng**, đừng đi tìm tín hiệu ở chỗ khác. 🔴 **Gọi worker chạy nền thì gọi một mình** — đừng nối `;` với lệnh khác, output trộn lẫn không còn đọc được của ai.

### Ownership server local

Server local là tài nguyên test tạm, **không phải hạ tầng dùng chung** giữa worker. Vai cần API/UI live phải tự: kiểm port → start bằng process nền tách khỏi CLI worker → ghi PID/port/log path (log trong `logs/` của module) → dùng đúng instance đó → sau smoke **dừng đúng process tree mình tạo**, xác nhận port đóng, ghi vào handoff.

- Không dựa vào server của Dev để làm evidence độc lập cho Reviewer. Nếu Leader chủ động bật server dùng chung (vd worker không tự bật được mà không treo) ⇒ brief nói rõ *"dùng server của Leader, không bật/tắt"*, ghi vào log.
- Không `kill` hàng loạt `node`. Không dừng process không sở hữu.
- 🔴 **Brief của mọi vai có bật/tắt server phải chép công thức tách rời** (vd PowerShell `Start-Process -FilePath cmd.exe -ArgumentList '/c','npm run dev > logs\x.log 2>&1' -WorkingDirectory <module> -WindowStyle Hidden -PassThru`, chờ port bằng vòng lặp có giới hạn) và câu cấm: *"NEVER run a server command directly in your shell tool. It never returns."* Worker chạy lệnh server trực tiếp ⇒ tool call không bao giờ trả về (đã gặp với agy).
- 🔴 Dừng worker đã khởi động server ⇒ **dừng cả server nó để lại** — server sống giữ port, giữ file lock, làm hỏng build sau. Trên Windows, dừng task nền **không** giết process `node` con do `npx`/`npm run` sinh ra ⇒ luôn kiểm lại port.

---

## 6. 🔴 Verify bằng trạng thái repo, không bằng exit code

**Exit 0 không chứng minh worker đã làm việc.** Thiếu quyền, sai tham số, thư mục sai, brief có ký tự bị hiểu là cờ — **mọi trường hợp đều exit 0**.

Sau mỗi lượt worker, **trước khi đọc report**:

```bash
for d in . project-docs <các repo module>; do   # MỌI repo, không chỉ repo được giao
  echo "== $d"; git -C "$d" status --short           # file nào đổi — khớp phạm vi được giao không?
done
ls -l <artifact worker phải tạo>     # artifact có tồn tại không, có nội dung không
```

🔴 **Mỗi module là một repo riêng** ⇒ `git status` ở root không thấy gì trong module. Phải chạy ở **từng repo**, kể cả repo nằm ngoài phạm vi giao — đó chính là chỗ bắt worker sửa lan.

Trạng thái không đổi ⇒ worker chưa làm gì, bất kể report nói gì. Vai read-only mà `git status` thấy file code đổi ⇒ vượt quyền, xử lý ngay.

🔴 **Worker có ghi DB** (smoke, fixture) ⇒ Leader **đếm số hàng theo loại trước và sau lượt** rồi đối chiếu với kế hoạch: kế hoạch có bước dọn ⇒ phải khớp baseline (câu "đã dọn" trong report không thay được phép đếm); không có ⇒ phần tăng thêm khớp dữ liệu đã ghi `test-accounts.md`. Khi dọn: theo **đối tượng test** (mọi bản ghi trỏ tới nó, kể cả bản ghi sinh ra trong lúc chạy), chỉ trên đối tượng do vai tạo — không bao giờ sweep theo đối tượng thật.

📘 **Worker báo "không làm được X" ⇒ probe trước khi phán.** Nghi oan worker tai hại ngang tin mù quáng. Nguyên nhân thường nằm ở **quyền Leader cấp khi gọi**.

---

## 7. Progressive disclosure — worker báo cáo thế nào

Ba lớp — chỉ lớp 1 mặc định vào context của Leader:

1. **Handoff capsule** — kết quả, phát hiện làm đổi quyết định, bất đồng/rủi ro, pointer tới evidence, đề xuất tiếp.
2. **Detailed report** — file artifact để soi sâu khi cần.
3. **Raw stream** — forensic khi có lỗi/tranh chấp; **không nạp vào context Leader** trong luồng bình thường.

- Worker trao đổi qua **artifact dùng chung**. Leader không làm "ống chuyển tiếp" copy output từ agent này sang agent khác.
- Resume ⇒ worker báo **delta**, không kể lại lịch sử.
- 🔴 **Capsule không thay thế evidence.** Mọi kết luận ảnh hưởng quyết định phải trỏ được tới artifact kiểm chứng.

---

## 8. Session sticky theo `đợt × vai`, đặt tên theo dự án

🔴 **Sổ tra: `project-docs/<đợt>/agent-sessions.md`** (khuôn: [templates/agent-sessions.md](../templates/agent-sessions.md)). Trước khi gọi worker, **tra sổ**: có session của vai đó trong đợt ⇒ **resume**, brief chỉ chứa delta. Chưa có ⇒ tạo mới rồi **ghi tên + id vào sổ ngay** sau lượt đầu.

🔴 **Tên session bắt đầu bằng tên dự án** — nhiều dự án chạy song song trên cùng máy dùng chung lịch sử session của CLI; bốc nhầm session của dự án khác là đã xảy ra. Định dạng `<Dự án>-<đợt>-<vai>` (vd `Acme-261101-SA`, `Acme-261101-Reviewer`); session tách đặc biệt thêm hậu tố lý do.

| Platform | Cách đặt tên |
|---|---|
| Codex | Không có cờ ⇒ **dòng đầu brief = tên session**; Codex lấy tin nhắn đầu làm `title` (đã probe) |
| Antigravity | Không có cờ (`--project <tên>` không có tác dụng nếu chưa tạo project) ⇒ **dòng đầu brief = tên session**; hiện ở `preview`, `title` do agy tự sinh sau (đã probe) |
| Cursor | Chưa probe ⇒ tạm dùng **dòng đầu brief = tên session**; lần dùng đầu kiểm cách CLI lưu tên rồi ghi vào [cursor.md](../agent-platform/cursor.md) |

- Ghi cả **tên và ID** vào sổ ngay sau lượt đầu. ID lấy từ event đầu trong stream của chính lượt gọi.
- Không dùng `--last` / `--continue` / `resume --last` — chúng lấy session gần nhất **của cả máy**, không phải của dự án.
- Sổ thiếu (sau compact, mất dòng) mà phải tìm trong lịch sử CLI ⇒ chỉ nhận session có tên/preview bắt đầu bằng `<Dự án>-`; còn nghi thì đối chiếu thư mục dự án CLI đã lưu (Codex `cwd` trong `state_5.sqlite`/file rollout, agy `workspace_uris` trong `conversation_summaries.db`).

Rule này đã hỏng một lần dù có tài liệu (4 session BA/SA trong một ngày, hết quota giữa lượt vì worker phải đọc lại Required Reading mỗi phase). Nên có **chốt chặn tất định**: hook `.claude/hooks/check-worker-session.js` chặn mọi lệnh mở session worker mới và in sổ ra; lệnh resume thì cho qua.

> **Dấu hiệu đang làm sai**: brief lại phải nhắc Required Reading. Session sống thì không cần.

- 🔴 **Report xong ≠ worker đã thoát.** Có CLI ghi xong report rồi vẫn chạy tiếp (sửa thêm, chạy lại smoke). Resume khi process cũ còn sống ⇒ hai agent cùng session sửa song song một working tree, chạy lại lệnh tốn tiền (đã gặp 3 lần). Trước khi resume: **không còn process nào mang session id đó** (đọc `CommandLine`; còn thì dừng đúng cây đó), rồi đổi dòng đầu report về `IN PROGRESS` — report cũ còn `DONE` thì phép quan sát báo xong ngay.
- Không giới hạn tuổi session. Đơn vị là **một vai trong một đợt**; chỉ tách khi có lý do (session quá dày, cần độc lập) và ghi lý do vào sổ.
- `BA-SA / x` **không** biến thành `Dev / x` khi sang thi công — gọi session Dev riêng. Thứ chuyển giữa vai là artifact, không phải danh tính agent.
- Với Dev, **một session chính** mỗi đợt thường hiệu quả hơn — code hiếm khi là bottleneck; fan-out làm phân mảnh context.
- 🔴 **Session Dev dày lên thì chậm** (đã gặp ~435k token, mỗi bước chậm dần tới treo) ⇒ tách session mới (hậu tố `-Dev-<việc>`, ghi lý do vào sổ), handoff qua artifact; các vòng sửa trong cùng một việc thì resume.
- 🔴 **Một session treo không chứng minh cả platform hỏng.** Sau khi watcher báo `STUCK`, xác minh PID/stream và trạng thái các lệnh tốn tiền; dừng đúng tiến trình của lượt đó. Nếu session cũ lặp lại lỗi, thử **một session mới trên cùng platform/model** với brief ngắn + artifact handoff và watcher riêng trước khi chuyển platform. Không chạy lại paid call có kết quả mơ hồ. Ghi lý do ngoại lệ session sticky vào sổ và workflow log. Chỉ đổi platform khi session mới cũng không đáp ứng, quota hết, hoặc quyền/năng lực thiếu.
- Cần review/test độc lập ⇒ session tách biệt để tránh anchoring.
- Sang đợt mới ⇒ **session mới**, kể cả cùng vai. Kiến thức kế thừa qua code và tài liệu, không qua session cũ.
- Context của worker là **working cache**, không phải source-of-truth.

---

## 9. Ánh xạ sang công cụ Claude Code (khi Leader chạy trên Claude Code)

| Yêu cầu | Cơ chế |
|---|---|
| Gọi worker chạy nền | `Bash`/`PowerShell` với `run_in_background: true` — **không** dùng `&`. `timeout` của tool không giết được process nền |
| Phép quan sát | `bash workflow/agent-platform/tools/watch-worker.sh <report> <done-prefix> <stream> sub <pid> <idleSec> <checkAt>` chạy **nền** (`run_in_background`); in một dòng rồi thoát. Chế độ `sub` khi Leader dùng gói subscription (mốc `checkAt` 120 → 300 → 900 → 1800), `api` khi qua OpenRouter (§5). 🔴 **Không dùng tool chặn lượt** (vd `Monitor`) — User sẽ không nhắn được cho Leader trong lúc chờ |
| Chẩn đoán treo | `Get-CimInstance Win32_Process` — đối chiếu CPU tích luỹ với thời gian chạy |
| Script gọi worker | `project-docs/<đợt>/_workers/runs/run-<vai>-<việc>.ps1` đọc brief từ file, ghi stream ra `_workers/streams/<vai>-<việc>-stream.jsonl` (thư mục `streams/` gitignore — chỉ giữ local để tra). Script **tự tạo thư mục stream** trước khi ghi (`New-Item -ItemType Directory -Force`): thiếu thư mục thì `Out-File` lỗi, worker không chạy mà task nền vẫn exit 0 |
| Probe độc lập | Script trong scratchpad của session hoặc `scripts/` của module, **xoá sau khi đo** |

Mẫu gọi watcher (PID lấy bằng `Get-CimInstance Win32_Process`, lọc `CommandLine` theo session id của lượt):

```bash
bash workflow/agent-platform/tools/watch-worker.sh \
  project-docs/<đợt>/reviews/<report>.md "<dòng đầu report khi xong>" \
  project-docs/<đợt>/_workers/streams/<vai>-<việc>-stream.jsonl sub <pid> 1500 120
# CHECK ⇒ đọc đuôi stream, ổn thì gọi lại với checkAt mà dòng CHECK in sẵn (next checkAt=…): 300, 900, rồi 1800 lặp
```

---

## 10. 🔴 Vệ sinh vận hành — năm luật kiểm được (từ lỗi thật ở dự án dùng template)

| # | Luật | Vì sao (đã xảy ra) |
|---|---|---|
| 1 | Brief kiểu *"dùng server đã bật sẵn"* phải kèm **đường dẫn file log** của server đó | Worker đọc nhầm log của vai khác, báo "không thấy dòng X" trong khi file đúng có đủ |
| 2 | **Secret không bao giờ nằm trong argv** (connection string, token, key). Truyền qua biến môi trường (`PGHOST/PGPORT/PGUSER/PGPASSWORD/PGDATABASE`) hoặc stdin. Chuỗi có dấu (Unicode) cũng không đi qua argv trên Windows. Lệnh lỗi chỉ in `stderr` đã lọc | Mọi thư viện spawn/exec in lại argv khi lệnh lỗi ⇒ mật khẩu DB hiện nguyên văn trong output |
| 3 | Dọn process: **đọc `CommandLine` ở một lệnh riêng, kill ở lệnh sau**; chỉ kill theo **PID chính mình ghi lúc bật**. "Đường dẫn nằm trong repo" **không** đủ — Leader và worker chung một repo nên bộ lọc theo đường dẫn khớp cả server của Leader (đã giết API + tunnel lúc User đang thử trên điện thoại). Máy dùng chung nhiều dự án: cổng inspector ngrok `:4040` có thể là tunnel của dự án khác ⇒ lấy URL tunnel từ **log của chính process vừa bật**, hoặc lọc theo `config.addr` = cổng của mình | Tắt nhầm `vite preview` của dự án khác vì đoán chủ process theo cổng; ghi URL tunnel của dự án khác vào `.env` và đăng ký webhook bên thứ ba bằng URL đó |
| 4 | **Công thức bật server dùng `>>`** (append) cho file log của vai; **trần chi phí gọi model đếm từ DB / sổ của chính hệ thống**, log chỉ là tín hiệu phụ. Watcher trần chỉ báo khi **vượt** | `>` ghi đè khi server restart ⇒ bộ đếm dòng log tụt về 0, mất số lượt vẽ tốn tiền |
| 5 | Sửa tài liệu bằng **loạt lệnh Edit song song** mà một lệnh báo lỗi tạm thời ⇒ **Grep xác nhận từng thay đổi** trước khi thử lại; chỉ thử lại đúng lệnh vắng mặt. Edit có `new_string` chứa nguyên `old_string` **không idempotent** | Thử lại nhầm lệnh ⇒ chèn câu lặp và bỏ sót thay đổi đúng, SA báo blocker "chưa đóng" |

---

## 11. 🔴 User muốn dừng giữa lượt để brainstorm lại

User nhắn *"dừng Dev P2 lại, tôi muốn bàn lại X"* — thứ User cần thường là **được brainstorm**, không phải **worker phải ngừng**. Mỗi lượt worker vốn kết thúc ở một điểm dừng tự nhiên (hết lượt, hoặc checkpoint ⛔ chờ Leader); giết giữa chừng để lại code sửa dở, server mồ côi, report nửa vời.

**Mặc định — brainstorm song song, áp ở điểm dừng kế tiếp:**

1. Brainstorm với User **ngay**, worker chạy tiếp. Nói rõ cho User: lượt đang làm gì, còn chừng bao lâu, ý mới có làm phí phần đang làm không.
2. Lượt kết thúc ⇒ **không** giao tiếp theo kế hoạch cũ. Kết quả brainstorm đổi phạm vi/thiết kế ⇒ ghi draft, sửa Phase doc thành **Rev mới, qua lại SA gate** (gate không miễn vì "đang dở tay"). Chỉ đổi thứ tự/chi tiết vặt doc đã cho phép ⇒ không cần Rev.
3. Resume **đúng session** của vai đó ([§8](#8-session-sticky-theo-đợt--vai-đặt-tên-theo-dự-án)), brief chỉ chứa delta: Rev mấy, đổi gì, phần nào đã làm **giữ** / **gỡ**, làm tiếp từ đâu.

**Dừng cứng — chỉ khi chờ tới điểm dừng là phí rõ ràng** (Dev đang code đúng phần sắp bỏ; lượt còn dài mà hướng chắc chắn đổi). Vai chỉ đọc (SA/BA review, discovery) gần như không bao giờ đáng dừng — để xong, report vẫn là đầu vào cho buổi brainstorm. Dừng cứng thì đủ năm bước, rồi mới brainstorm:

```text
[ ] Giết đúng cây process của lượt theo PID đã ghi (đọc CommandLine trước, kill lệnh sau — §10 luật 3). Không kill theo tên/cổng
[ ] Dọn thứ lượt đó bật: server/stack, cổng, fixture, dữ liệu test (§5 Ownership server local, §6 đếm DB)
[ ] Dòng đầu report ⇒ "STOPPED BY USER — at <checkpoint>" (không để IN PROGRESS/DONE làm watcher và người đọc hiểu nhầm)
[ ] git status MỌI repo ⇒ liệt kê file sửa dở. GIỮ NGUYÊN — không stash/checkout/revert (thao tác git cần User ủy quyền); brief resume sẽ nói giữ hay gỡ
[ ] Workflow log: dừng lượt nào, ở checkpoint nào, session id, danh sách file dở, lý do
```

Rồi đi tiếp như mặc định bước 2–3 — brief resume nói rõ *"lượt trước bị dừng ở CPn theo yêu cầu User"* và việc với từng file dở.

> Ví dụ: Dev P3 đang code CP6–CP7 (script backup). User muốn thêm *"mã hoá backup rồi đẩy ra ngoài VPS"* ⇒ chỉ **thêm** bước, phần đang code không phí ⇒ mặc định: brainstorm song song, Rev 3 + SA gate, resume với delta *"CP6–CP7 giữ nguyên, thêm Bước 4b"*. Nếu User muốn *"bỏ script tự viết, dùng backup của nhà cung cấp VPS"* ⇒ phần đang code sẽ bị gỡ ⇒ dừng cứng, đủ năm bước, brief resume ghi *"gỡ `deploy/scripts/backup.sh` (dở, chưa commit)"*.
