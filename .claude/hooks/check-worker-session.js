#!/usr/bin/env node
/**
 * PreToolUse hook — trước khi gọi worker, bắt Leader có POLICY và TRA SỔ SESSION.
 * Worker có thể là bất kỳ CLI nào trong WORKER_NAMES (kể cả Claude Code); vai nào chạy trên đâu
 * do workflow/agent-platform/worker-policy.local.md của từng người quyết.
 * Dùng CLI worker mới ⇒ thêm tên lệnh vào WORKER_NAMES.
 *
 * Vì sao tồn tại: session worker sticky theo `đợt × vai`
 * (workflow/practices/delegating-workers.md §8). Rule này đã hỏng một lần dù có tài liệu:
 * 4 session BA/SA trong một ngày ⇒ hết quota giữa lượt vì worker phải đọc lại
 * Required Reading mỗi phase. Lời nhắc không chặn được, vì trí nhớ sau compact trông
 * vẫn đầy đủ — không có khoảng trống nào kích hoạt việc đi tra lại.
 *
 * Cơ chế:
 *   - Thiếu worker-policy.local.md ⇒ DENY mọi lệnh gọi worker (kể cả resume), không có đường vượt.
 *   - Lệnh gọi CLI worker (trực tiếp, HOẶC qua script .ps1 mà nội dung có gọi CLI worker)
 *     mà không phải resume ⇒ lần đầu DENY, in sổ session đang sống.
 *   - Chạy lại ĐÚNG lệnh đó trong 10 phút ⇒ cho qua (Leader đã thấy sổ và quyết mở mới).
 *   - Lệnh resume, lệnh tra cứu (--help, --version, models, mcp) ⇒ cho qua ngay.
 *
 * Hook này không bao giờ trả "allow" tường minh — cho qua = thoát im lặng, quyền do harness quyết.
 *
 * Sửa hook: `node --check` + bơm input thử:
 *   echo '{"hook_event_name":"PreToolUse","tool_input":{"command":"agy -p x"}}' | node .claude/hooks/check-worker-session.js
 */
const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');

// CLI worker được gọi thật: tên đứng đầu, sau khoảng trắng/toán tử shell, HOẶC ở cuối một đường dẫn
// (`& "$env:LOCALAPPDATA\cursor-agent\cursor-agent.ps1" -p …` — cách gọi thật trên Windows),
// và BẮT BUỘC có tham số theo sau (`-…`, `exec`, `$biến`, `@biến` — splatting PowerShell `& claude @args`).
// Nhờ vế tham số, đường dẫn thư mục (`…/agy/bin/`, `.claude/hooks/`) hay nhắc tên trong echo/grep không khớp.
const WORKER_NAMES = '(codex|cursor-agent|cursor|agy|claude)(\\.(cmd|ps1|exe))?';
const WORKER_CLI = new RegExp('(^|[\\s"\'&|;(\\\\/])' + WORKER_NAMES + '["\']?\\s+["\']?(-|exec\\b|\\$|@[A-Za-z_])', 'im');
const READ_ONLY_CMD = new RegExp('(--list-models|--help|--version|\\b' + WORKER_NAMES + '\\s+(models|about|mcp)(\\s|$))', 'i');
const RESUMING = /(exec\s+resume|--resume|--continue|--conversation)(\s|=|$|["'])/i;
const SCRIPT_REF = /[\w:.\\\/-]+\.ps1/gi;
const SESSION_FILE = 'agent-sessions.md';
const POLICY_FILE = path.join('workflow', 'agent-platform', 'worker-policy.local.md');
const ACK_FILE = path.join(os.tmpdir(), 'claude-worker-session-ack.json');
const ACK_TTL_MS = 10 * 60 * 1000;
const NL = '\n';

function deny(reason) {
  process.stdout.write(
    JSON.stringify({
      hookSpecificOutput: {
        hookEventName: 'PreToolUse',
        permissionDecision: 'deny',
        permissionDecisionReason: reason,
      },
    })
  );
  process.exit(0);
}

function readAcks() {
  try {
    return JSON.parse(fs.readFileSync(ACK_FILE, 'utf8')) || {};
  } catch (e) {
    return {};
  }
}

function writeAcks(acks) {
  try {
    fs.writeFileSync(ACK_FILE, JSON.stringify(acks));
  } catch (e) {}
}

let raw = '';
process.stdin.on('data', (d) => (raw += d));
process.stdin.on('end', () => {
  let input;
  try {
    input = JSON.parse(raw);
  } catch (e) {
    process.exit(0);
  }

  // Chỉ dành cho Leader chạy trên Claude Code. Platform khác có thể đọc .claude/settings.json
  // và tự chạy hook với input khác shape ⇒ im lặng cho qua.
  if (input.hook_event_name !== 'PreToolUse') process.exit(0);

  const cmd = input && input.tool_input && input.tool_input.command;
  if (typeof cmd !== 'string') process.exit(0);

  const root = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();

  // Gộp nội dung các script .ps1 được lệnh tham chiếu — Leader hay gọi worker qua run-*.ps1
  let effective = cmd;
  for (const ref of cmd.match(SCRIPT_REF) || []) {
    const p = path.isAbsolute(ref) ? ref : path.join(root, ref);
    try {
      effective += NL + fs.readFileSync(p, 'utf8');
    } catch (e) {}
  }

  if (!WORKER_CLI.test(effective)) process.exit(0);
  if (READ_ONLY_CMD.test(effective) && !/\s-p\s|exec\s/i.test(effective)) process.exit(0);

  // Không phải repo theo template (không có workflow/) ⇒ im lặng cho qua.
  if (!fs.existsSync(path.join(root, 'workflow'))) process.exit(0);
  if (!fs.existsSync(path.join(root, POLICY_FILE))) {
    deny(
      'DỪNG: chưa có ' + POLICY_FILE.split(path.sep).join('/') + ' — policy chọn platform/model cho worker của người đang chạy Leader.' + NL +
        'Cùng User lập file từ workflow/agent-platform/worker-policy.sample.md (mẫu là cấu hình của một người, không phải chuẩn), rồi mới gọi worker.' + NL +
        'Luật chung policy không được phá: workflow/agent-platform/README.md.'
    );
  }

  if (RESUMING.test(effective)) process.exit(0);

  // Đã bị chặn một lần và chạy lại đúng lệnh trong TTL ⇒ cho qua
  const key = crypto.createHash('sha1').update(cmd).digest('hex');
  const now = Date.now();
  const acks = readAcks();
  for (const k of Object.keys(acks)) if (now - acks[k] > ACK_TTL_MS) delete acks[k];
  if (acks[key]) {
    delete acks[key];
    writeAcks(acks);
    process.exit(0);
  }
  acks[key] = now;
  writeAcks(acks);

  let books = [];
  try {
    const base = path.join(root, 'project-docs');
    books = fs
      .readdirSync(base, { withFileTypes: true })
      .filter((e) => e.isDirectory() && /^[0-9]{6,8}-/.test(e.name))
      .map((e) => path.join(base, e.name, SESSION_FILE))
      .filter((p) => fs.existsSync(p));
  } catch (e) {
    process.exit(0); // không có project-docs ⇒ không phải repo theo template
  }

  if (books.length === 0) {
    deny(
      'DỪNG: sắp mở session worker MỚI nhưng chưa đợt nào có sổ session.' + NL +
        'Tạo `project-docs/<đợt>/agent-sessions.md` theo workflow/templates/agent-sessions.md.' + NL +
        'Đã chắc cần session mới ⇒ chạy lại ĐÚNG lệnh này trong 10 phút, rồi ghi session id vào sổ NGAY sau lượt gọi.'
    );
  }

  const book = books
    .map((p) => {
      let m = 0;
      try {
        m = fs.statSync(p).mtimeMs;
      } catch (e) {}
      return { p, m };
    })
    .sort((a, b) => a.m - b.m)
    .pop().p;

  const rel = path.relative(root, book).split(path.sep).join('/');
  let rows = [];
  try {
    rows = fs
      .readFileSync(book, 'utf8')
      .split(NL)
      .filter((l) => /^\|/.test(l) && /`([0-9a-z_-]{8,})`/i.test(l))
      .map((l) => l.trim());
  } catch (e) {}

  deny(
    'DỪNG: đang mở session worker MỚI — tra sổ trước.' + NL + NL +
      'Sổ (sửa gần nhất): ' + rel + NL +
      (rows.length ? 'Session đang sống:' + NL + rows.join(NL) + NL + NL : 'Sổ chưa ghi session nào.' + NL + NL) +
      'Có session của vai này trong đợt (tên `<Dự án>-<đợt>-<vai>`) trên platform policy còn cho phép ⇒ RESUME (codex `exec resume <id>` · cursor `--resume <id>` · agy `--conversation <id>` · claude `--resume <id>`), brief chỉ chứa delta. Session trên platform policy nay không cho ⇒ chuyển context qua artifact.' + NL +
      'Thật sự cần session mới (vai mới · cần độc lập để tránh anchoring · đợt khác) ⇒ chạy lại ĐÚNG lệnh này trong 10 phút, rồi ghi id vào sổ NGAY sau lượt gọi.' + NL +
      'Dấu hiệu đang làm sai: brief lại phải nhắc Required Reading.'
  );
});
