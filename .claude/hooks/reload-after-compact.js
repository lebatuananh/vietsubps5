#!/usr/bin/env node
/**
 * SessionStart hook — NẠP NỘI DUNG SKILL + bản đồ quy trình vào mọi phiên.
 *
 * VÌ SAO TỒN TẠI (sự cố thật):
 *   Leader viết xong Phase doc rồi giao thẳng Dev, BỎ QUA gate "SA thẩm định Phase doc".
 *   Rule đó có ở hai chỗ (leader-stages/03 và skill write-implementation-docs) nhưng
 *   không chỗ nào tới được Leader: danh sách skill KHÔNG được nạp lại sau compact —
 *   chỉ skill đã gọi mới còn. Leader không phớt lờ skill; skill không hiện diện để mà nhớ.
 *
 * VÌ SAO NẠP CẢ NỘI DUNG, KHÔNG CHỈ NHẮC TÊN:
 *   Bản đầu chỉ in "hãy gọi /write-implementation-docs" — vẫn phụ thuộc Leader tự giác
 *   gọi, mà chỗ tự giác đó chính là chỗ vừa hỏng. Nạp thẳng nội dung thì dù chưa gọi,
 *   rule vẫn nằm trong context. Leader VẪN NÊN gọi skill khi vào công đoạn.
 *
 * CHẠY CHO CẢ startup: phiên mới cũng cần bản đồ y hệt phiên sau compact.
 *
 * Hook này CHỈ IN THÔNG TIN. Không chặn, không nới quyền, lỗi thì im lặng thoát.
 *
 * Sửa hook: `node --check` + bơm input thử, kiểm JSON hợp lệ trước khi tin là xong:
 *   echo '{"source":"compact","hook_event_name":"SessionStart"}' | node .claude/hooks/reload-after-compact.js
 */

const fs = require('fs');
const path = require('path');

// Skill nạp TOÀN VĂN — ngắn, và là thứ quyết định quy trình đúng/sai.
const FULL = ['brainstorm', 'prepare-code-branch', 'write-implementation-docs', 'delegate'];
// Skill nạp RÚT GỌN — dài hoặc chỉ dùng một lần mỗi đợt.
const HEAD = { 'kickoff-code-workflow': 40 };

let raw = '';
process.stdin.on('data', (d) => (raw += d));
process.stdin.on('end', () => {
  let input = {};
  try {
    input = JSON.parse(raw);
  } catch (e) {
    process.exit(0);
  }

  const source = input.source || '';
  if (!['compact', 'resume', 'startup', 'clear'].includes(source)) process.exit(0);

  const root = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();

  function readSkill(name, maxLines) {
    let txt;
    try {
      txt = fs.readFileSync(path.join(root, '.claude', 'skills', name, 'SKILL.md'), 'utf8');
    } catch (e) {
      return null;
    }
    txt = txt.replace(/^---[\s\S]*?\n---\r?\n/, '').trim();
    if (maxLines) {
      const ls = txt.split('\n');
      if (ls.length > maxLines) {
        txt = ls.slice(0, maxLines).join('\n') +
          '\n\n…(rút gọn — đọc trọn `.claude/skills/' + name + '/SKILL.md` khi dùng)';
      }
    }
    return txt;
  }

  const lines = [];
  const p = (s) => lines.push(s);
  const cut = source === 'compact' || source === 'resume';

  p(cut ? '# ⚠️ CONTEXT VỪA BỊ CẮT — QUY TRÌNH BẮT BUỘC, ĐỌC TRƯỚC KHI LÀM' : '# QUY TRÌNH BẮT BUỘC CỦA DỰ ÁN');
  p('');

  // Dự án vừa clone từ template, chưa bootstrap
  try {
    const cm = fs.readFileSync(path.join(root, 'CLAUDE.md'), 'utf8');
    if (cm.includes('ĐIỀN KHI CLONE')) {
      p('🔴 **`CLAUDE.md` còn mục `ĐIỀN KHI CLONE`** — dự án chưa bootstrap. Chạy skill `/bootstrap-project` trước khi mở đợt update nào.');
      p('');
    }
  } catch (e) {}

  // Policy chọn platform/model cho worker — của từng người, gitignore, bắt buộc
  if (
    fs.existsSync(path.join(root, 'workflow', 'agent-platform')) &&
    !fs.existsSync(path.join(root, 'workflow', 'agent-platform', 'worker-policy.local.md'))
  ) {
    p('🔴 **Chưa có `workflow/agent-platform/worker-policy.local.md`** — policy chọn platform/model cho worker của người đang chạy Leader (gitignore, mỗi người một bản). Hook sẽ chặn mọi lệnh gọi worker cho tới khi có. Trước lượt giao việc đầu tiên: cùng User lập file từ `worker-policy.sample.md`.');
    p('');
  }

  if (cut) {
    p('Danh sách skill KHÔNG được nạp lại sau compact ⇒ hook nạp thẳng nội dung skill dưới đây.');
    p('🔴 Trí nhớ đã bị tóm tắt mâu thuẫn với artifact có evidence ⇒ **artifact đúng**. Đọc lại `CLAUDE.md`, `workflow/roles/team-leader.md`, file công đoạn đang làm, và **đuôi** workflow log của đợt.');
  } else {
    p('Hook nạp sẵn nội dung skill quy trình để không phụ thuộc việc nhớ gọi.');
  }
  p('Vẫn nên gọi `/<skill>` khi bước vào công đoạn — để có bản đầy đủ và lưu vết.');
  p('');
  p('Trao đổi với User: **tiếng Việt có dấu**, hỏi bằng text thường (không `AskUserQuestion`), một câu mỗi lần, luôn kèm đề xuất có chủ kiến.');
  p('');
  p('## Bản đồ công đoạn — `workflow/leader-stages/`');
  p('');
  p('| Đang làm gì | Skill | File |');
  p('|---|---|---|');
  p('| User vừa nêu mục tiêu | `/brainstorm` | `01-brainstorm.md` |');
  p('| Cần hiểu hiện trạng (≥ 2 lượt với BA/SA) | *(qua `/delegate`)* | `02-discovery.md` |');
  p('| Epic/Phase đã chốt, cần nhánh ở từng repo | `/prepare-code-branch` | `workflow/guidelines/git.md` |');
  p('| Sắp viết Overview/Epic/Phase doc | `/write-implementation-docs` | `03-write-docs.md` |');
  p('| Phase 1 đã qua SA gate, bắt đầu thi công | `/kickoff-code-workflow` | `04-implement.md` |');
  p('| Sắp gọi worker (platform theo `worker-policy.local.md`) | `/delegate` | `workflow/practices/delegating-workers.md` |');
  p('| Hết Phase, bản local đã ngon ⇒ gói bàn giao rồi DỪNG | *(không có skill)* | `05-handover.md` |');
  p('| User chỉ định đóng đợt | *(không có skill)* | `06-close.md` |');
  p('');
  p('🔴 **Leader không bao giờ merge `dev`/`main` hay deploy** — dừng ở gói bàn giao. 🔴 **Mỗi module là một repo git riêng** — `git status` ở root không thấy module, dùng `git -C <repo>`.');
  p('');
  p('## 🔴 Thứ tự gate khi mở một Phase — chỗ dễ bỏ sót nhất');
  p('');
  p('```');
  p('brainstorm đủ 6 ô → BA/SA discovery (≥ 2 lượt, brief có mục mở) → Leader viết Phase doc');
  p('  → SA THẨM ĐỊNH PHASE DOC, verdict PASS          ← GATE BẮT BUỘC');
  p('  → báo User tóm tắt (dừng chờ chỉ khi va chạm định hướng / risk gate)');
  p('  → Dev lượt 1: review doc → Leader xác nhận đồng thuận  ← GATE RIÊNG, KHÔNG thay SA');
  p('  → Dev lượt 2: code + tự smoke → git status TỪNG REPO → Reviewer độc lập → Leader probe → PHASE_DONE');
  p('```');
  p('');
  p('Hai gate KHÁC NHAU: **SA** kiểm doc có đúng hiện trạng; **Dev** kiểm doc có code được.');
  p('');
  p('---');
  p('');
  p('# NỘI DUNG SKILL (nạp sẵn)');

  for (const name of FULL) {
    const txt = readSkill(name, 0);
    if (!txt) continue;
    p('');
    p('## `/' + name + '`');
    p('');
    p(txt);
  }
  for (const name of Object.keys(HEAD)) {
    const txt = readSkill(name, HEAD[name]);
    if (!txt) continue;
    p('');
    p('## `/' + name + '` *(rút gọn)*');
    p('');
    p(txt);
  }

  // Workflow log gần đây — mtime KHÔNG đáng tin trên Windows, chỉ liệt kê ứng viên
  try {
    const docsRoot = path.join(root, 'project-docs');
    const logs = [];
    for (const d of fs.readdirSync(docsRoot, { withFileTypes: true })) {
      if (!d.isDirectory() || !/^\d{6,8}-/.test(d.name)) continue;
      const dir = path.join(docsRoot, d.name);
      for (const f of fs.readdirSync(dir)) {
        if (/workflow-log\.md$/.test(f)) {
          const full = path.join(dir, f);
          logs.push({ f: full, m: fs.statSync(full).mtimeMs });
        }
      }
    }
    logs.sort((a, b) => b.m - a.m);
    if (logs.length) {
      p('');
      p('---');
      p('');
      p('## Workflow log gần đây (mtime không đáng tin — tự xác định đợt đang làm)');
      p('');
      for (const l of logs.slice(0, 4)) {
        p('- `' + path.relative(root, l.f).split(path.sep).join('/') + '`');
      }
      p('');
      p('**Đọc `tail` log đúng đợt** để biết đang ở đâu — đừng dựng lại trạng thái từ trí nhớ.');
    }
  } catch (e) {}

  process.stdout.write(
    JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'SessionStart',
        additionalContext: lines.join('\n'),
      },
    })
  );
  process.exit(0);
});
