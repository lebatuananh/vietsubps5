# Git — nhiều repo, nhánh, ai chạy lệnh

Đọc trước khi tạo nhánh, commit, hay đối chiếu trạng thái nhánh.

---

## 1. Mỗi module là một repo riêng

Workspace dự án **không phải** một repo duy nhất:

| Repo | Chứa gì | Nhánh |
|---|---|---|
| **root** (wrapper) | `CLAUDE.md` · `workflow/` · `.claude/` · `.cursor/` · `.mcp.json` | Chỉ `main` |
| **`project-docs/`** | Hiện trạng + tài liệu các đợt | 🔴 **Chỉ `main`** — không tách nhánh, không có `dev`, tài liệu commit thẳng |
| **mỗi module** (`api-service/`, `admin-console/`…) | Code | `main` · `dev` · nhánh đợt (§2) |

Danh sách repo thật của dự án, remote, và repo nào có nhánh chuẩn **khác** `main`: `CLAUDE.md` mục *Cấu trúc repo*.

Root `.gitignore` chặn các thư mục module + `project-docs/` ⇒ root không bao giờ track nhầm code của repo con.

**Hệ quả thực tế**:
- `git status` ở root **không** thấy thay đổi trong `api-service/`. Mọi lệnh git chạy theo từng repo: `git -C <repo> …`.
- Một đợt đụng nhiều module ⇒ **tạo nhánh ở từng repo bị đụng, cùng một tên**.
- Không có commit nào gom được nhiều module. Merge làm độc lập từng repo.
- Verify worker bằng `git status` **ở từng repo** có thể bị đụng — kể cả repo không nằm trong phạm vi giao (đó chính là chỗ bắt worker vượt phạm vi).

```bash
for d in api-service admin-console project-docs; do
  printf "%-16s %-40s %s\n" "$d" "$(git -C "$d" branch --show-current)" "$(git -C "$d" status --short | wc -l) file đổi"
done
```

---

## 2. Nhánh

| Nhánh | Vai trò |
|---|---|
| **`main`** | **= production = code chuẩn.** Mốc tách nhánh mới và để đối chiếu |
| `dev` | Môi trường dev. Dev phụ trách merge nhánh đợt vào để test |
| `<prefix>/<YYMMDD>-<slug>` | Nhánh làm việc của một đợt |

**Tên nhánh**: `<features|enhancements|bugs>/<YYMMDD>-<slug>` — chỉ ba tiền tố:

| Prefix | Khi |
|---|---|
| `features/` | Tính năng mới |
| `enhancements/` | Cải tiến thứ đã có |
| `bugs/` | Sửa lỗi |

- `YYMMDD` = ngày bắt đầu đợt, **trùng** prefix thư mục đợt trong `project-docs/`.
- `slug` viết thường, gạch nối, tiếng Anh, ngắn.
- **Cùng một tên** ở mọi repo bị đụng.

Ví dụ: `features/261015-content-studio` · `enhancements/261020-guest-login` · `bugs/261022-refund-duplicate`.

Tạo nhánh theo skill `/prepare-code-branch`.

---

## 3. Luồng

```
main ──► <prefix>/<YYMMDD>-<slug> ──► dev    (dev phụ trách merge để test trên môi trường dev)
                                  └──► main  (test ổn ⇒ merge CHÍNH nhánh đó vào main)
```

1. Tách nhánh từ `main` (sau `git pull`) ở từng repo bị đụng, working tree sạch.
2. Code + tự test ở local — **phần của Leader và team AI dừng ở đây** ([05-handover](../leader-stages/05-handover.md)).
3. Dev phụ trách merge nhánh vào `dev`, deploy môi trường dev, tester test.
4. Test ổn ⇒ dev merge **chính nhánh tính năng đó** vào `main`.

🔴 **Bất biến: thứ merge vào `main` luôn là nhánh tính năng.** **Không bao giờ merge `dev` vào `main`** — `dev` tích tụ nhiều thứ chưa nghiệm thu; merge nó là kéo tất cả lên production.

### `dev` đi trước `main` rất xa — đó là bình thường

`git rev-list --count main..dev` ra hàng trăm **không** có nghĩa `main` lạc hậu hay ai commit sai chỗ. `main` vẫn là chuẩn.

🔴 **Đừng dùng `git diff dev..main` để xem mình vừa làm gì** — nó trả hiệu giữa hai nhánh, phần lớn là thứ `dev` có thêm, hiện ra dưới dạng "xoá hàng nghìn dòng". Xem việc vừa làm bằng commit của chính nhánh:

```bash
git -C <repo> log --oneline main..HEAD
git -C <repo> diff --stat main...HEAD
```

---

## 4. 🔴 Ai chạy lệnh git

**Mặc định mọi thao tác git thuộc User**: tạo nhánh, commit, merge, push, xoá nhánh.

- Agent **khảo sát, đề xuất tên nhánh, đưa khối lệnh sẵn** để User dán — rồi **chờ User xác nhận**.
- Sau khi User báo xong ⇒ agent verify ở từng repo (`git -C <repo> branch --show-current`, `status --short`, `log --oneline -3`).
- User ủy quyền rõ (*"em tự tạo nhánh đi"*, *"commit rồi push luôn"*) ⇒ agent làm **đúng việc đó, trong đợt đó**. Ghi **nguyên văn** vào workflow log. 🔴 Không suy rộng: được tạo nhánh không có nghĩa được commit; được một đợt không có nghĩa đợt sau.
- 🔴 **Merge `dev`/`main` và deploy không bao giờ là việc của agent**, kể cả khi được ủy quyền git khác.
- Worker (Dev, Reviewer…) **không bao giờ** tự commit/push/merge — trừ khi Leader chuyển tiếp ủy quyền rõ ràng.
- Không bao giờ `push --force`, `reset --hard`, xoá nhánh, hay đổi lịch sử khi chưa có lệnh rõ.
- 🔴 **Kiểm secret trước commit là một lệnh riêng**, chạy trên đúng file sắp commit (`git -C <repo> diff --cached --name-only`), đọc kết quả, rồi mới commit ở lệnh sau. Không nối bằng `;` / `||` khiến commit vẫn chạy khi bước kiểm có khớp.

---

## 5. Environment preflight

Trước lượt ghi/test đầu tiên của mỗi đợt, và mỗi khi môi trường đổi, ghi vào log: **nhánh + commit từng repo bị đụng** (`git -C <repo> rev-parse --short HEAD`), DB đích (không ghi secret), baseline test.

> Server local đang chạy code của nhánh khác ⇒ route trả 404, rất dễ chẩn đoán nhầm thành "server hỏng" và mất hàng giờ đi sai hướng. Nhiều repo thì càng dễ: một repo đã sang nhánh đợt, repo kia vẫn ở `main`.

---

## 6. `.gitignore` phải chặn sẵn

Ở **mỗi** repo: secrets (`*.pem`, `*.key`) · bản cấu hình ghi đè riêng máy (`appsettings.Local.json`, `.env.local`) · `bin/`, `obj/`, `node_modules/`, `dist/` · log (`logs/**`, `*.log`) · artifact của công cụ agent (`.playwright-mcp/`, `probe-out/`).
Ở root thêm: các thư mục repo con · cấu hình máy cá nhân (`.claude/settings.local.json`).
Ở `project-docs/` thêm: stream thô của worker (`*/_workers/streams/`). Mật khẩu tài khoản test local/dev thì **commit** (`automation/test-accounts.md`) — cả team phải đăng nhập được.

🔴 **Cấu hình local của module (`appsettings.Development.json`, hoặc `.env` với module Node) được commit có chủ ý** — chứa giá trị chạy local cho cả team (DB dev, key dev), để pull về là chạy được. Hệ quả: (1) secret **production** không bao giờ vào `.env` hay bất kỳ repo nào — giá trị local/dev (kể cả mật khẩu tài khoản test) thì commit — production inject lúc chạy container; (2) bước kiểm secret trước commit (§4) coi `.env` là file được phép, nhưng vẫn soi giá trị mới thêm vào có phải secret production không; (3) đổi giá trị cho riêng máy mình ⇒ `.env.local`, không sửa `.env` chung.
