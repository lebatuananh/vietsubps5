# Automation — server local, database, domain, tài khoản test

**Đọc trước mọi lượt blackbox/smoke** do AI agent thực hiện.

File này là **phương pháp chung**. Giá trị cụ thể của dự án nằm ở:

| Cần biết | Ở đâu |
|---|---|
| Module chạy lệnh gì, port nào, health check gì | `CLAUDE.md` mục *Cấu trúc repo* + `README.md` module |
| DB dev tên gì, trỏ đâu, dịch vụ ngoài nào dùng chung/tốn tiền | `CLAUDE.md` mục *Môi trường* |
| Tài khoản test có sẵn + cách dựng | `project-docs/automation/test-accounts.md` |
| Role DB dùng cho DDL, domain môi trường dev | `project-docs/automation/README.md` |
| Script dựng/dọn dữ liệu tái dùng được | `project-docs/automation/scripts/` (commit) — `scripts/` của module bị gitignore, chỉ chứa script một đợt |

---

## 🎯 Triết lý

- **Chủ động nhưng cẩn trọng** — đề xuất và giải thích (nhất là khi ghi DB hay chọn domain) **trước khi** thực thi.
- **Write-then-Verify** — mọi thay đổi qua API/script đều kiểm lại bằng query DB. `200` không phải bằng chứng.
- **Làm việc qua file** — script cho DB, file `.json` cho payload; tránh gõ trực tiếp vào terminal (lỗi escape, nhất là PowerShell).
- **Script gọn gàng** — đặt theo vòng đời ([03-write-docs — Script thử nghiệm đặt ở đâu](../leader-stages/03-write-docs.md#-script-thử-nghiệm-đặt-ở-đâu)): mặc định `project-docs/[đợt]/playtest/…`; cần import code module thì `scripts/` của module. Không rải ở root, không trong `src/` (server watch `src/` sẽ restart giữa chừng).

---

## Flow một lượt test

1. **Hiểu yêu cầu** — Phase doc + tài liệu hiện trạng liên quan.
2. **Environment preflight** — nhánh + commit, connection string DB trỏ đâu (che credential), PORT đọc từ cấu hình local của module (`.env` / `appsettings.*.json` / launch profile — không giả định).
3. **Khởi động server** (nếu test local) — §1. Đảm bảo **một instance**.
4. **Chọn tài khoản** — `test-accounts.md` trước; không có cái phù hợp mới dựng (§4).
5. **Chuẩn bị dữ liệu** — script có `DRY_RUN` (§2).
6. **Chọn domain** — §3.
7. **Gọi API/UI** — payload từ file.
8. **Verify 3 lớp** — response + DB + log. Đọc log sau mỗi call.
9. **Lưu kết quả** — vào file báo cáo trong thư mục đợt.
10. **Thu dọn** — tắt server của mình; cập nhật `test-accounts.md`; dọn dữ liệu test **chỉ khi kế hoạch có bước dọn**.

---

## 1. Server local

- **Đọc cấu hình local của module lấy PORT** (README module ghi chỗ), không giả định.
- 🔴 **Chỉ chạy MỘT instance.** Hai instance: request chia ngẫu nhiên ("lúc ăn lúc không"), listener xử lý message hai lần, log ra hai nguồn. **Kiểm port trước khi start**:
  ```powershell
  Get-NetTCPConnection -LocalPort <port> -State Listen -ErrorAction SilentlyContinue |
    ForEach-Object { Get-CimInstance Win32_Process -Filter "ProcessId=$($_.OwningProcess)" | Select-Object ProcessId, CommandLine }
  ```
- 🔴 **Trên Windows, dừng task nền không giết process `node` con do `npx`/`npm run`/`nodemon` sinh ra.** Sau khi dừng, kiểm lại port, xác nhận `CommandLine` của PID đúng là server mình bật, rồi mới `Stop-Process`. `nodemon` còn sống sẽ **tự hồi sinh server cũ** khi file thay đổi.
- **Log** ghi vào `logs/` của module (đã gitignore), đặt tên theo việc: `logs/smoke-<đợt>.log`.
- **Server nào mình bật thì mình tắt** — xem [delegating-workers §5](../practices/delegating-workers.md).
- Worker CLI tự bật server bằng lệnh **chặn** có thể treo vô hạn chờ lệnh đó kết thúc ⇒ bật server bằng process nền tách rời, hoặc Leader bật hộ rồi brief *"dùng server có sẵn, không bật/tắt"*.

### Lỗi kết nối hay gặp

| Triệu chứng | Nguyên nhân thường gặp |
|---|---|
| `ECONNREFUSED` | Server chưa lên, hoặc sai PORT |
| Timeout khi query DB | DB remote không tới được, hoặc pool connection đã cạn |
| Listener không nhận message | Mất kết nối queue, hoặc instance khác tiêu thụ mất |
| Route trả 404 dù code có | **Instance cũ ở nhánh khác vẫn giữ port** — kiểm trước khi nghi server |

---

## 2. Database

### 🔴 Quy tắc số 1: biết mình đang ghi vào đâu

Cấu hình local có thể trỏ **DB remote** — "chạy ở local" không có nghĩa là dữ liệu nằm trên máy. Trước mọi script **ghi**, in connection string đang dùng (che mật khẩu) — file nào chứa nó ghi ở README module, vd:

```powershell
# .NET: appsettings.Development.json (+ appsettings.Local.json ghi đè nếu có)
Select-String -Path <module>/src/Host/Host.Api/appsettings*.json -Pattern '"Database"' | ForEach-Object { $_.Line -replace 'Password=[^;"]*', 'Password=***' }
```

Đối chiếu với mục *Môi trường* của `CLAUDE.md`: đây có phải DB **dev** không. 🔴 **Không bao giờ ghi vào DB production** — production chỉ đọc, và chỉ khi điều tra sự cố theo yêu cầu User.

Quyền ghi DB dev (dữ liệu test, apply SQL của đợt) theo mục *Môi trường* / *Risk gate riêng* của dự án. Mặc định template: **agent được ghi DB dev** (sau khi xác nhận đúng DB, SQL đã append vào file đợt); dự án có DB dev dùng chung server với prod thì có thể siết lại ở `CLAUDE.md`.

### Nguyên tắc an toàn

- **Luôn giới hạn phạm vi** — mọi câu ghi có `where` khoá vào **đúng dữ liệu test của mình**. Không điều kiện rộng kiểu "mọi bản ghi tạo hôm nay".
- **Đếm trước, ghi sau** — `DRY_RUN: true` mặc định; in số bản ghi sẽ ảnh hưởng; số lệch kỳ vọng ⇒ filter sai ⇒ dừng.
- **Ghi rồi kiểm** — ORM không báo lỗi khi update khớp 0 bản ghi (`{ count: 0 }`), script vẫn "thành công".
- **Không sửa dữ liệu người dùng thật.** Cần điều tra ⇒ chỉ đọc.
- **Không có cascade** khi module không dùng khoá ngoại (README module backend nói rõ) ⇒ xoá tài khoản test không tự xoá dữ liệu con — xoá từng bảng; không chắc xoá hết thì **để lại và ghi vào `test-accounts.md`** (dữ liệu thừa rẻ hơn dữ liệu hỏng).

### Khung script

```typescript
const CONFIG = { ACCOUNT_ID: '<điền>', DRY_RUN: true };

async function main() {
  const url = process.env.DATABASE_URL ?? '';
  console.log('DB:', url.replace(/:\/\/[^@]*@/, '://***@').split('?')[0]);
  const target = await repo.findByAccount(CONFIG.ACCOUNT_ID);
  console.log(`Sẽ ảnh hưởng ${target.length} bản ghi`);
  if (CONFIG.DRY_RUN) return console.log('DRY_RUN — không ghi gì.');
  // ... ghi ...
  // ... query lại và in kết quả ...
}
main().catch(e => { console.error(e); process.exitCode = 1; }).finally(() => disconnect());
```

Chạy **từ thư mục module** (để `.env` và client DB resolve đúng). Luôn `disconnect()` ở `finally` — pool connection nhỏ, server đang chạy đã chiếm phần lớn; cần nhiều dữ liệu thì dump ra JSON **trước khi** start server.

### Đổi schema là việc khác

SQL viết tay → append vào `project-docs/<đợt>/[YYMMDD].sql` → apply lên DB dev → cập nhật schema của ORM → generate client. **Không** migration tự sinh. Chi tiết theo `README.md` module backend.

---

## 3. Chọn domain

| Domain | Code đang chạy | Dùng khi |
|---|---|---|
| **localhost** | Code trên máy, ngay lúc này | Mặc định — test thay đổi vừa code |
| **Domain dev** | Code đã deploy lên dev | Chỉ đọc để đối chiếu khi User yêu cầu — deploy dev là việc của dev phụ trách, team AI không verify sau deploy |
| **Production** | Code phục vụ người dùng | 🔴 **Chỉ đọc để điều tra sự cố**, và chỉ khi User yêu cầu |

- localhost **không phải môi trường cô lập**: DB có thể remote, LLM/sinh ảnh là API thật tốn tiền.
- Môi trường dev không phải lúc nào cũng chạy ⇒ không xác nhận được thì dùng localhost.
- Client thật (mobile) cần gọi code local ⇒ qua tunnel; ghi rõ trong báo cáo.

Phase doc đã chốt domain cho từng bước ⇒ làm theo, không hỏi lại. Chưa chốt ⇒ đề xuất (domain nào · vì sao · chạm dữ liệu gì · dọn thế nào) rồi chờ xác nhận.

---

## 4. Tài khoản test

- 🔴 **Mở `test-accounts.md` trước.** Tạo mới tốn thời gian (onboarding gọi AI thật) và làm phình DB.
- Chỉ tạo mới khi cần trạng thái **sạch** (test onboarding) hoặc cấu hình khác biệt.
- Đặt định danh dễ nhận ra: `auto-test-<YYMMDD>-<n>` — nhìn là biết do automation tạo, dễ cleanup.
- **Ghi–Kiểm**: tạo xong không tin response — query DB xác nhận.
- 🔴 **Ghi vào `test-accounts.md` ngay sau khi tạo**: khoá định danh để đăng nhập lại (thứ quan trọng nhất — mất nó là mất đường quay lại) · ID thực thể · trạng thái (đã onboarding tới đâu, có dữ liệu gì) · **bẫy đã vấp khi dựng** · giữ lại hay đã xoá.
- **Dev và Reviewer dùng tài khoản khác nhau.**

---

## 5. Trạng thái tài liệu automation của dự án

Bộ `project-docs/automation/` bồi đắp dần qua từng đợt. Phát hiện bẫy mới (lỗi lặp lại, bước hay quên, hành vi bất ngờ của môi trường) ⇒ **bổ sung vào đúng file**, không để trong transcript. Bẫy đúng với mọi dự án ⇒ bổ sung vào **file này ở template** ([06-close §4](../leader-stages/06-close.md)).
