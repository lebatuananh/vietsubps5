// Nói chuyện với `codex app-server` qua stdio (JSON-RPC, mỗi dòng một JSON).
// Mặc định CHỈ ĐỌC: initialize → account/rateLimits/read. Tiêu lượt reset chỉ khi truyền `--consume` (cần User đồng ý).
// Không in token/credential nào — response rate limit không chứa secret.
const { spawn } = require('child_process');
const crypto = require('crypto');
const consume = process.argv.includes('--consume');
const exe = process.env.LOCALAPPDATA + '\\CodexTools\\codex.cmd';
const child = spawn(exe, ['app-server'], { shell: true, stdio: ['pipe', 'pipe', 'pipe'] });
let buf = '', nextId = 1; const waiters = new Map();
child.stdout.on('data', (d) => {
  buf += d.toString('utf8'); let i;
  while ((i = buf.indexOf('\n')) >= 0) {
    const line = buf.slice(0, i).trim(); buf = buf.slice(i + 1);
    if (!line) continue;
    let msg; try { msg = JSON.parse(line); } catch { continue; }
    if (msg.id !== undefined && waiters.has(msg.id)) { waiters.get(msg.id)(msg); waiters.delete(msg.id); }
  }
});
child.stderr.on('data', () => {});
const call = (method, params) => new Promise((resolve, reject) => {
  const id = nextId++; waiters.set(id, resolve);
  child.stdin.write(JSON.stringify({ jsonrpc: '2.0', id, method, params }) + '\n');
  setTimeout(() => { if (waiters.has(id)) { waiters.delete(id); reject(new Error('timeout ' + method)); } }, 30000);
});
const fmt = (t) => (t ? new Date(t * 1000).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : '-');
(async () => {
  try {
    const init = await call('initialize', { clientInfo: { name: 'team-leader', version: '0.1.0' } });
    if (init.error) throw new Error('initialize: ' + JSON.stringify(init.error));
    child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'initialized' }) + '\n');
    const r = await call('account/rateLimits/read', {});
    if (r.error) throw new Error('rateLimits/read: ' + JSON.stringify(r.error));
    const res = r.result || {};
    const show = (label, s) => { if (!s) return; for (const k of ['primary', 'secondary']) { const w = s[k]; if (w) console.log(`${label}.${k}: used ${w.usedPercent}% · window ${w.windowDurationMins} min · resets ${fmt(w.resetsAt)}`); } if (s.rateLimitReachedType) console.log(`${label}.reached: ${s.rateLimitReachedType}`); };
    show('rateLimits', res.rateLimits);
    for (const [k, s] of Object.entries(res.rateLimitsByLimitId || {})) show(`byLimit[${k}]`, s);
    console.log('ordinaryUsageAllowed:', res.ordinaryUsageAllowed);
    const c = res.rateLimitResetCredits;
    console.log('resetCredits.availableCount:', c ? c.availableCount : 'n/a');
    for (const x of (c && c.credits) || []) console.log(`  credit: ${x.title || '-'} · type ${x.resetType} · status ${x.status} · granted ${fmt(x.grantedAt)} · expires ${fmt(x.expiresAt)}`);
    if (consume) {
      const key = crypto.randomUUID();
      const out = await call('account/rateLimitResetCredit/consume', { idempotencyKey: key });
      console.log('CONSUME result:', JSON.stringify(out.result || out.error));
      const r2 = await call('account/rateLimits/read', {});
      show('after', (r2.result || {}).rateLimits);
      console.log('after.availableCount:', ((r2.result || {}).rateLimitResetCredits || {}).availableCount);
    }
  } catch (e) { console.error('ERR', e.message); }
  child.kill();
  process.exit(0);
})();
