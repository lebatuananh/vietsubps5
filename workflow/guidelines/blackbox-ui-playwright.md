# Blackbox UI với Playwright MCP

**Đọc khi**: Phase đụng giao diện (web app, admin console) và vai của bạn phải smoke/verify UI — Dev, Reviewer, End User.

Đọc trước: [blackbox-testing](./blackbox-testing.md) (chiến lược, 3 lớp verify) · [automation](./automation.md) (server local, DB, tài khoản). Cài đặt/cấp quyền Playwright MCP từng platform: [agent-platform — Browser](../agent-platform/README.md#browser-playwright-mcp--dùng-chung-mọi-platform).

Phần **đặc thù từng module** (cách giả lập đăng nhập, SDK giả nào có sẵn, port) nằm trong `README.md` của module — vd mục *Giao diện* (đoạn "Kiểm") và bảng bẫy trong `admin-console/README.md`.

---

## 0. Trước khi mở trình duyệt

- [ ] Platform của vai đã **probe browser đạt** ở kickoff (token hiện sau click + ảnh đúng đường dẫn). Chưa ⇒ probe trước, không giao việc thật.
- [ ] Brief nói rõ: dùng MCP server **`playwright`** (gọi đích danh — global có thể có server trùng tên của dự án khác), thư mục lưu ảnh evidence.
- [ ] Server backend + frontend **do chính vai này bật** (hoặc brief ghi rõ dùng server Leader bật) — ghi port, PID, commit.
- [ ] Mockup của Phase (`[YYMMDD]-mockup-*.html`) đã đọc — đó là thứ để đối chiếu UI.
- [ ] Tài khoản test **riêng của vai** (Dev ≠ Reviewer).
- [ ] 🔴 **Xoá `localStorage` + `sessionStorage` của origin là thao tác trình duyệt ĐẦU TIÊN và CUỐI CÙNG** (và trước mỗi fixture độc lập). Profile Playwright MCP **dùng chung** giữa các vai/platform ⇒ token đăng nhập của vai trước còn đó, mở app là bắn request bằng tài khoản người khác. *(Ca thật: gặp hai lần, hai chiều Dev ↔ Reviewer.)*

---

## 1. Dựng trang — một lần gọi, đúng thứ tự

Mọi thứ phải có **trước** `goto` thì làm trong **một** lần `browser_run_code_unsafe`, trên cùng `page`:

```js
async (page) => {
  // 1. Route SDK/script bên thứ ba sang bản giả (nếu Phase cần) — TRƯỚC goto
  await page.route("https://<third-party>/sdk.js", (route) =>
    route.fulfill({ contentType: "application/javascript", path: "<abs path to fake>" }));
  // 2. Danh tính đăng nhập giả lập — init script do harness của module sinh ra
  await page.addInitScript({ path: "<abs path from identity helper>" });
  // 3. Viewport đúng thiết bị đích (mobile web)
  await page.setViewportSize({ width: 390, height: 844 });
  // 4. Vào trang
  await page.goto("http://localhost:<port>/");
  return page.evaluate(() => ({ ready: document.readyState, hashCleared: location.hash === "" }));
}
```

- 🔴 **Thứ tự quan trọng**: route và init script đăng ký sau `goto` thì trang đã load bản thật / chưa có danh tính.
- Đổi danh tính ⇒ `browser_close`, xoá `localStorage` (token của user trước còn đó) **trước** `goto` mới.
- SDK giả có state/cache theo trang (controller khởi tạo một lần) ⇒ muốn đổi bản giả thì **mở page mới**, không đổi route trên trang đang sống.
- `route.fulfill({ path })` thay vì đọc file trong code chạy trên trang — process Playwright có thể không có `fs`.

---

## 2. Danh tính đăng nhập giả lập

Client xác thực qua nền tảng ngoài (OAuth, chữ ký của nền tảng ngoài…) ⇒ module có **harness Node-only** sinh danh tính cho playtest. Rule chung:

- 🔴 **Harness nằm ngoài bundle** (`<module>/playtest/`), không import từ `src/`. Secret dùng để ký **không bao giờ** vào frontend.
- Harness ký bằng **cùng thuật toán** backend dùng để verify — không bypass xác thực ở backend.
- File danh tính sinh ra là **credential thật** (đăng nhập được) ⇒ nằm trong thư mục gitignore (`playtest/.out/`), **xoá sau mỗi lượt**.
- 🔴 **Không in** token, chữ ký, JWT, hash ra report, ảnh, hay log. Playwright MCP **in nguyên `Page URL` sau mỗi thao tác** ⇒ init script phải **xoá hash/query chứa credential ở `DOMContentLoaded`** (sau khi SDK đã đọc).
- ⚠️ Harness ký bằng **đồng hồ máy local** ⇒ lỗi lệch giờ (`auth_date` bị từ chối) **không bao giờ lộ** trong playtest. Chỉ client thật mới lộ — xem §7.

---

## 3. SDK bên thứ ba giả lập (quảng cáo, thanh toán…)

Không tải SDK thật trong playtest (tốn tiền, không tất định, không ép được ca lỗi). Bản giả nằm ở `<module>/playtest/fake-*.js`:

- **Mặc định fail-closed** — không có kế hoạch thì trả lỗi, không bao giờ tự "thành công" (vd không tự cấp thưởng).
- **Đặt kết quả ngay trước thao tác gọi SDK**: `window.__<sdk>Plan = ["closed"]` — các outcome phủ đủ nhánh: thành công · huỷ · không có hàng · lỗi · **treo** (`hold` + hàm release).
- **Ghi lại mọi lần gọi** lên `window.__<sdk>Calls` để assert số lần gọi.
- Có bản giả riêng cho ca **SDK tự ném lỗi** — để chứng minh app vẫn chạy khi SDK hỏng.

🔴 Bản giả chỉ chứng minh app **xử lý đúng các outcome**. Nó **không** chứng minh app gọi SDK thật đúng contract ⇒ Leader/Reviewer **probe độc lập trên SDK thật** (đọc script thật, đối chiếu tên event, tham số) theo docs nhà cung cấp.

---

## 4. Mock backend chọn lọc

`page.route` cho request API để ép **nhánh lỗi / chậm** mà backend thật khó tạo: 500, timeout, response treo.

- 🔴 **Ghi rõ trong report hàng nào mock, hàng nào thật.** Kết quả trên response mock không chứng minh backend.
- 🔴 **Ít nhất một luồng chính chạy thật end-to-end** — request thật tới backend local, verify DB + log server. Tính năng tốn tiền (sinh ảnh, LLM) ⇒ chạy thật **đúng số lượt Phase doc cho phép**, không cắt về 0.
- Mock chỉ để ép nhánh, không để né việc chạy thật.

---

## 5. Verify cái gì — mỗi bước

| Lớp | Công cụ MCP | Kiểm |
|---|---|---|
| **DOM sau render** | `browser_snapshot` | Đúng khối, đúng text/copy, nút enabled/disabled đúng lúc |
| **Network** | `browser_network_requests` | **Số lượng, thứ tự, status** request — vd *"huỷ quảng cáo ⇒ 0 POST ảnh"*, *"bấm 2 lần khi đang treo ⇒ vẫn 1 lần gọi"* |
| **Console** | `browser_console_messages` | Không có error mới |
| **Trạng thái SDK giả** | `page.evaluate(() => window.__<sdk>Calls)` | Số lần gọi, outcome |
| **DB + log server** | query / đọc `logs/` | Luồng thật ghi đúng; đúng nhánh code |
| **Trực quan** | `browser_take_screenshot` | Ảnh evidence cho các trạng thái chính |

🔴 **Network là bằng chứng mạnh nhất** cho logic chặn/cho phép ("không gửi", "gửi đúng một lần"). DOM có thể đúng mà request vẫn bắn.

### Phải đi qua

- **Bốn trạng thái** của mockup: Loading · Empty · Error · Overflow (text dài, danh sách dài).
- **Thao tác "sai"**: bấm hai lần, bấm khi đang chờ, back giữa chừng, reload, đổi màn trong lúc đang xử lý (unmount).
- 🔴 **Phần tử bấm được phải thấy được**: menu, popover, nút trong lớp phủ ⇒ kiểm hit-test `document.elementFromPoint(<tâm phần tử>)` trả đúng phần tử (hoặc con của nó). Click qua DOM/`force`/`evaluate` vẫn "chạy" khi phần tử bị `overflow: hidden` cắt hay bị lớp khác (sidebar, overlay) che — smoke xanh mà người dùng bấm không thấy gì.
- **Hồi quy** các màn cũ dùng chung component/state vừa sửa.
- 🔴 Chạy lại harness của **Phase trước** để kiểm hồi quy ⇒ ghi ra **thư mục output mới** (vd `…/regression-<phase>/`) hoặc qua tham số `OUT_DIR`; evidence của Phase đã commit là bất biến, không ghi đè.

⚠️ Trạng thái chuyển tiếp quá nhanh để `waitFor` bắt được (spinner, "Loading…") ⇒ ghi rõ là không chụp được, chứng minh bằng network/state thay thế — **không** bỏ trống, không ghi "đạt" suông.

⚠️ Chụp ngay khi phần tử vào DOM là chụp **giữa animation** (sheet/drawer trượt, fade) ⇒ nội dung phía sau lộ ra trông như lỗi z-index/trong suốt. Chờ animation xong (`document.getAnimations()` rỗng, hoặc ≥ thời lượng animation) rồi mới chụp.

---

## 6. Evidence & report

- Ảnh: **đặt tên tường minh** `NN-<mô-tả-ngắn>.png`, lưu `project-docs/<đợt>/playtest/<epic-phase>/<vai>/` (vd `playtest/epic2-phase3/dev/`) (đường dẫn tương đối từ root project). Không đặt `--output-dir`; file tự đặt tên rơi vào `.playwright-mcp/` (gitignore, rác).
- Ảnh không được lộ credential, dữ liệu thật của người dùng.
- Report ghi vào phần smoke của `reviews/[YYMMDD]-epic[X]-phase[Y]-dev-report.md` (Dev) / của review (Reviewer):

```markdown
## Môi trường
commit · port backend/frontend · DB · tài khoản (id, không token) · SDK giả nào · viewport

## Bảng blackbox — mọi hàng là output Playwright thật
| # | Màn / nút | Thao tác + outcome giả lập | Request (số, status) | UI (copy / trạng thái nút) | Mock hay thật | Ảnh |
|---|---|---|---|---|---|---|

## Luồng thật end-to-end
request → DB (query + kết quả) → log (dòng nào)

## Chưa kiểm được
<trạng thái không chụp được · phần cần SDK thật · phần cần client thật — và vì sao>
```

---

## 7. Giới hạn của harness — thứ playtest không bao giờ thấy

| Không thấy | Vì sao | Lấp bằng |
|---|---|---|
| Lệch đồng hồ máy chạy backend | Harness ký bằng chính đồng hồ đó | Preflight trước khi User test thật: `w32tm /stripchart /computer:time.windows.com /samples:1` |
| Hành vi SDK thật (tên event, contract) | Đang dùng bản giả | Probe độc lập trên script SDK thật |
| Môi trường client thật (WebView, iOS Safari…) | Chạy Chromium desktop | Tester thử trên máy thật — ghi vào hướng dẫn test manual của gói bàn giao |
| Duyệt/chính sách của nền tảng (store, quảng cáo) | Không nằm trong code | Ghi vào gói bàn giao để dev/tester kiểm |

Lỗi HTTP khi User test qua tunnel ⇒ đọc **body response thật** (vd ngrok inspector `http://127.0.0.1:4040/api/requests/http`) **trước khi** đoán nguyên nhân.

---

## 8. Dọn sau lượt

- [ ] `browser_close`.
- [ ] Xoá file danh tính trong `playtest/.out/`.
- [ ] Tắt đúng server mình bật, kiểm port trống.
- [ ] Cập nhật `test-accounts.md` (tài khoản đã dùng/tạo, bẫy đã vấp).
- [ ] Không để lại `.playwright-mcp/`, `probe-out/` ngoài thư mục gitignore.
