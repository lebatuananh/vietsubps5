// Văn phòng 3D cho worker-viewer: mỗi vai là một người (Microsoft Rocketbox, MIT), hành vi lấy từ dữ liệu thật của viewer (window.wb).
// Tài nguyên: python project-docs/automation/scripts/office/fetch-assets.py ⇒ office/assets/ (gitignore).
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { CSS2DRenderer, CSS2DObject } from 'three/addons/renderers/CSS2DRenderer.js';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';

const BASE = '/office/assets/';
// Tên hiển thị theo vai — đổi tuỳ thích.
const NAMES = { 'Leader': 'Minh', 'BA/SA': 'Lan', 'Dev': 'Huy', 'Reviewer': 'Thảo', 'End User': 'Vy', 'Bạn': 'Bạn' };
// "Bạn" (chính User) vào họp với Leader khi vừa nhắn trong phiên Leader; im lặng quá chừng này thì ra về.
const USER_MEET_MS = 3 * 60e3;
const DOOR = new THREE.Vector3(8.2, 0, -5.45);
// Camera: toàn cảnh mặc định; chế độ "Theo dõi" lia tới người trong cảnh (nhìn từ phía trước, ngang tầm mặt) rồi lùi về.
const HOME = { pos: new THREE.Vector3(0.9, 6.9, 10.2), target: new THREE.Vector3(-0.9, 0.3, -0.9) };
const FOLLOW_DIR = new THREE.Vector3(0.35, 0.42, 1).normalize(), FOLLOW_DIST = 4.3;
let follow = null, followOn = true;
try { followOn = localStorage.getItem('wb-follow') !== '0'; } catch {}
// Chỗ ngồi làm việc (x, z của hông khi ngồi); mọi bàn quay về phía camera (+z) để thấy mặt người.
const DESKS = {
  'BA/SA': { x: -4.2, z: -3.4 }, 'Leader': { x: 0, z: -3.4, w: 1.8 }, 'Reviewer': { x: 4.2, z: -3.4 },
  'Dev': { x: -2.6, z: -0.3 }, 'End User': { x: 2.6, z: -0.3 },
};
const MEET = { x: -4.6, z: 3.0, r: 0.85 };
const STATUS = { work: 'đang làm', review: 'đang đọc / thẩm định', meeting: 'đang họp', stale: 'có vẻ treo', idle: 'rảnh' };

let host, renderer, labels, scene, camera, controls, clock, raf = 0, visible = false, ready = null;
const chars = {};
const hitProxies = [];
let prevRunning = null, board = null, clockTex = null;

export function show(el) {
  host = el; visible = true;
  ready ??= init().catch(e => { message('Lỗi khi dựng văn phòng: ' + e.message); throw e; });
  ready.then(() => { resize(); cancelAnimationFrame(raf); clock.getDelta(); loop(); });
}
export function hide() { visible = false; cancelAnimationFrame(raf); }

function message(text) {
  let m = host.querySelector('.msg');
  if (!m) { m = document.createElement('div'); m.className = 'msg'; host.appendChild(m); }
  m.textContent = text; m.hidden = !text;
}

// ---------------------------------------------------------------- khởi tạo

async function init() {
  message('Đang dựng văn phòng…');
  const res = await fetch(BASE + 'manifest.json');
  if (!res.ok) { message('Chưa có nhân vật 3D.\nChạy một lần từ root repo:\npython project-docs/automation/scripts/office/fetch-assets.py'); throw new Error('no assets'); }
  const manifest = await res.json();

  renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
  renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
  renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 0.92;
  host.appendChild(renderer.domElement);
  labels = new CSS2DRenderer(); labels.domElement.className = 'office-labels'; host.appendChild(labels.domElement);
  injectCss();

  scene = new THREE.Scene();
  scene.background = new THREE.Color('#cfd5dc');
  scene.fog = new THREE.Fog('#cfd5dc', 24, 42);
  const pmrem = new THREE.PMREMGenerator(renderer);
  scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
  scene.environmentIntensity = 0.45;

  camera = new THREE.PerspectiveCamera(36, 1, 0.1, 100);
  camera.position.copy(HOME.pos);
  controls = new OrbitControls(camera, renderer.domElement);
  controls.target.copy(HOME.target);
  controls.enableDamping = true; controls.maxPolarAngle = 1.4; controls.minDistance = 2.5; controls.maxDistance = 24;
  controls.update();
  controls.addEventListener('start', () => { follow = null; });   // người xem tự kéo camera ⇒ nhường, không giành lại
  const btn = document.createElement('button'); btn.className = 'ofollow';
  const btnText = () => (btn.textContent = `🎥 Theo dõi: ${followOn ? 'bật' : 'tắt'}`);
  btn.title = 'Tự lia camera tới khi có giao việc, báo cáo, họp'; btnText();
  btn.onclick = () => { followOn = !followOn; btnText(); try { localStorage.setItem('wb-follow', followOn ? '1' : '0'); } catch {} if (!followOn && follow) follow = { home: true, until: performance.now() / 1000 + 3 }; };
  host.appendChild(btn);
  const snd = document.createElement('button'); snd.className = 'ofollow'; snd.style.left = '150px';
  const sndText = () => (snd.textContent = soundOn ? '🔊 Âm thanh: bật' : '🔈 Âm thanh: tắt');
  snd.title = 'Tiếng gõ phím, ting khi PASS, tiếng trầm khi BLOCKER, chuông cửa khi Bạn vào họp'; sndText();
  snd.onclick = () => { soundOn = !soundOn; sndText(); try { localStorage.setItem('wb-sound', soundOn ? '1' : '0'); } catch {} if (soundOn) { ac(); sfx('task'); } };
  host.appendChild(snd);
  host.addEventListener('pointerdown', () => { if (soundOn) ac(); });   // đã bật từ lần trước ⇒ mở lại âm thanh ở cú bấm đầu
  clock = new THREE.Clock();

  buildRoom();
  const qh = new URLSearchParams(location.search).get('hour'); if (qh !== null && qh !== '') fixedHour = +qh;
  applyTime();

  const loader = new FBXLoader();
  let done = 0; const total = Object.keys(manifest.avatars).length + Object.values(manifest.clips).reduce((s, g) => s + Object.keys(g).length, 0);
  const tick = () => message(`Đang tải nhân vật và chuyển động… ${++done}/${total}`);
  const clips = { f: {}, m: {} };
  await pool(Object.entries(manifest.clips).flatMap(([g, list]) => Object.entries(list).map(([k, f]) => async () => {
    const o = await loader.loadAsync(BASE + f); clips[g][k] = prepClip(o.animations[0], k); tick();
  })), 4);
  const texLoader = new THREE.TextureLoader();
  await pool(Object.entries(manifest.avatars).map(([role, a]) => async () => {
    const model = await loader.loadAsync(BASE + a.fbx);
    await dressModel(model, a.textures, texLoader);
    chars[role] = new Character(role, model, clips[a.gender]);
    if (role === 'Bạn') chars[role].root.visible = false;
    tick();
  }), 3);

  message('');
  window.addEventListener('wb-data', director);
  window.__office = { chars, handoff, report, camera, controls, focusOn, audioState: () => audio?.state ?? 'none', setHour: h => { fixedHour = h; skyState = ''; applyTime(); } };   // để thử cảnh từ console: __office.handoff(__office.chars.Dev, {label…})
  renderer.domElement.addEventListener('pointermove', onHover);
  renderer.domElement.addEventListener('click', onClick);
  new ResizeObserver(resize).observe(host);
  director();
}

async function pool(tasks, n) {
  let i = 0;
  await Promise.all(Array.from({ length: n }, async () => { while (i < tasks.length) await tasks[i++](); }));
}

// Bỏ tịnh tiến ngang của gốc (nhân vật do code di chuyển), bỏ track scale/Footsteps và vị trí xương con (lệch tỉ lệ giữa các nhân vật).
function prepClip(clip, key) {
  const tracks = []; let speed = 0;
  for (const t of clip.tracks) {
    const [node, prop] = t.name.split('.');
    if (prop === 'scale' || node === 'Bip01_Footsteps' || !node.startsWith('Bip01')) continue;
    if (prop === 'position' && node === 'Bip01') {
      const v = t.values, n = v.length / 3, nv = v.slice();
      speed = Math.hypot(v[(n - 1) * 3] - v[0], v[(n - 1) * 3 + 2] - v[2]) / clip.duration / 100;
      for (let i = 0; i < n; i++) { nv[i * 3] = 0; nv[i * 3 + 2] = 0; }
      tracks.push(new THREE.VectorKeyframeTrack(t.name, t.times, nv)); continue;
    }
    if (prop === 'position' && node !== 'Bip01_Pelvis') continue;
    tracks.push(t);
  }
  const c = new THREE.AnimationClip(key, clip.duration, tracks);
  c.userData = { speed: key === 'walk' ? speed : 0 };
  return c;
}

async function dressModel(model, tex, texLoader) {
  const load = (f, srgb) => texLoader.loadAsync(BASE + f).then(t => { if (srgb) t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; return t; });
  const [bc, bn, hc, hn, op] = await Promise.all([load(tex.body_color, true), load(tex.body_normal), load(tex.head_color, true), load(tex.head_normal), load(tex.opacity_color, true)]);
  const body = new THREE.MeshStandardMaterial({ map: bc, normalMap: bn, roughness: 0.78, metalness: 0 });
  const head = new THREE.MeshStandardMaterial({ map: hc, normalMap: hn, roughness: 0.55, metalness: 0 });
  const hair = new THREE.MeshStandardMaterial({ map: op, roughness: 0.7, alphaTest: 0.45, side: THREE.DoubleSide });
  model.scale.setScalar(0.01);
  model.traverse(o => {
    if (!o.isMesh) return;
    o.castShadow = true; o.receiveShadow = true; o.frustumCulled = false;
    o.material = [].concat(o.material).map(m => /_head/.test(m.name) ? head : /_opacity/.test(m.name) ? hair : body);
    if (o.material.length === 1) o.material = o.material[0];
  });
}

// ---------------------------------------------------------------- căn phòng

const M = {};
function mats() {
  M.floor = new THREE.MeshStandardMaterial({ map: woodTexture('#9a7048', 10), roughness: 0.55 });
  M.floor.map.repeat.set(5, 3.5);
  M.oak = new THREE.MeshStandardMaterial({ map: woodTexture('#a5764b', 5), roughness: 0.5 });
  M.wall = new THREE.MeshStandardMaterial({ color: '#efede9', roughness: 0.95 });
  M.white = new THREE.MeshStandardMaterial({ color: '#f5f5f3', roughness: 0.35 });
  M.black = new THREE.MeshStandardMaterial({ color: '#1f2226', roughness: 0.4, metalness: 0.6 });
  M.fabric = new THREE.MeshStandardMaterial({ color: '#2d3239', roughness: 0.95 });
  M.alu = new THREE.MeshStandardMaterial({ color: '#c3c6ca', roughness: 0.3, metalness: 0.9 });
  const skyC = document.createElement('canvas'); skyC.width = 512; skyC.height = 256;
  const skyT = new THREE.CanvasTexture(skyC); skyT.colorSpace = THREE.SRGBColorSpace;
  M.sky = new THREE.MeshBasicMaterial({ map: skyT }); M.sky.userData = { c: skyC, t: skyT };
  M.rug = new THREE.MeshStandardMaterial({ color: '#59616b', roughness: 1 });
  M.leaf = new THREE.MeshStandardMaterial({ color: '#3f7a46', roughness: 0.8, side: THREE.DoubleSide });
  M.pot = new THREE.MeshStandardMaterial({ color: '#d8d2c8', roughness: 0.7 });
  M.screen = { off: new THREE.MeshStandardMaterial({ color: '#0d0f12', roughness: 0.2 }),
    code: screenMat('code'), doc: screenMat('doc'), stale: screenMat('stale') };
}

function woodTexture(base, planks) {
  const c = document.createElement('canvas'); c.width = c.height = 1024;
  const g = c.getContext('2d'); const h = 1024 / planks;
  const col = new THREE.Color(base);
  for (let i = 0; i < planks; i++) {
    const k = 0.9 + Math.random() * 0.18;
    g.fillStyle = `#${col.clone().multiplyScalar(k).getHexString()}`; g.fillRect(0, i * h, 1024, h);
    g.globalAlpha = 0.08;
    for (let s = 0; s < 40; s++) { g.strokeStyle = Math.random() < 0.5 ? '#000' : '#fff'; g.lineWidth = 1 + Math.random() * 2; g.beginPath(); const y = i * h + Math.random() * h; g.moveTo(0, y); g.bezierCurveTo(300, y + Math.random() * 6 - 3, 700, y + Math.random() * 6 - 3, 1024, y); g.stroke(); }
    g.globalAlpha = 0.5; g.fillStyle = '#3b2a1c'; g.fillRect(0, i * h, 1024, 2);
    const cut = Math.random() * 1024; g.fillRect(cut, i * h, 2, h);
    g.globalAlpha = 1;
  }
  const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8;
  return t;
}

function screenMat(kind) {
  const c = document.createElement('canvas'); c.width = 256; c.height = 160;
  const g = c.getContext('2d');
  g.fillStyle = kind === 'doc' ? '#f3f4f6' : kind === 'stale' ? '#2a1d10' : '#16191f'; g.fillRect(0, 0, 256, 160);
  for (let y = 14; y < 150; y += 11) {
    const ind = Math.random() * 40, w = 40 + Math.random() * 150;
    g.fillStyle = kind === 'doc' ? '#9aa3af' : kind === 'stale' ? '#f0883e' : ['#5fc4d8', '#c792ea', '#c3e88d', '#f78c6c'][Math.floor(Math.random() * 4)];
    g.fillRect(12 + (kind === 'doc' ? 0 : ind), y, kind === 'doc' ? 232 - Math.random() * 60 : w, 5);
  }
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace;
  return new THREE.MeshStandardMaterial({ map: t, emissive: '#ffffff', emissiveMap: t, emissiveIntensity: 0.75, roughness: 0.25 });
}

// Màn hình laptop hiện việc thật của lượt: các lệnh/câu gần nhất (item.last), terminal cho người code, tài liệu cho người đọc.
// Cùng canvas đó hiện dưới nhãn tên khi rê chuột vào người (màn 3D quay về phía người ngồi, camera thường thấy lưng laptop).
const SCR = {};
function screenCanvas(role) {
  const c = document.createElement('canvas'); c.width = 512; c.height = 330; c.className = 'scr';
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4;
  SCR[role] = { c, t, mat: new THREE.MeshStandardMaterial({ map: t, emissive: '#ffffff', emissiveMap: t, emissiveIntensity: 0.8, roughness: 0.25 }), hist: [], sig: '', itemId: null };
}
function updateScreen(role, it, mode) {
  const s = SCR[role], scr = screens[role]; if (!s || !scr) return;
  if (!it) { scr.material = mode === 'stale' ? M.screen.stale : M.screen.off; s.c.classList.remove('on'); return; }
  if (it.id !== s.itemId) { s.itemId = it.id; s.hist = []; }
  for (const a of it.recent || []) if (!s.hist.includes(a)) s.hist.push(a);   // 8 việc gần nhất từ viewer, nối dần
  if (s.hist.length > 14) s.hist.splice(0, s.hist.length - 14);
  const doc = mode === 'review' || mode === 'meeting';
  const sig = `${mode}|${it.id}|${s.hist.length}|${s.hist[s.hist.length - 1] || ''}`;
  scr.material = s.mat; s.c.classList.add('on');
  if (sig === s.sig) return; s.sig = sig;
  const g = s.c.getContext('2d'), W = 512, H = 330;
  g.fillStyle = doc ? '#f6f7f9' : '#0f1218'; g.fillRect(0, 0, W, H);
  g.fillStyle = doc ? '#e3e6ea' : '#1c212b'; g.fillRect(0, 0, W, 34);
  for (const [i, col] of ['#ff5f57', '#febc2e', '#28c840'].entries()) { g.fillStyle = col; g.beginPath(); g.arc(18 + i * 18, 17, 6, 0, 7); g.fill(); }
  g.fillStyle = doc ? '#374151' : '#aab3c0'; g.font = '600 17px "Segoe UI", sans-serif';
  g.fillText(role === 'Leader' ? 'Leader · điều phối' : window.wb.label(it), 80, 23);
  const fit = (txt, w) => { if (g.measureText(txt).width <= w) return txt; while (txt.length > 1 && g.measureText(txt + '…').width > w) txt = txt.slice(0, -1); return txt + '…'; };
  const lines = s.hist.length ? s.hist.slice(-11) : ['…'];
  g.font = `${doc ? '' : ''}16px ${doc ? '"Segoe UI", sans-serif' : 'Consolas, "Cascadia Mono", monospace'}`;
  lines.forEach((l, i) => {
    const y = 60 + i * 24, last = i === lines.length - 1, tool = /^(\w+):/.exec(l)?.[1];
    if (doc) { g.fillStyle = last ? '#1d4ed8' : '#6b7280'; g.fillText(fit(l, W - 30), 15, y); return; }
    const prefix = tool === 'Bash' || tool === 'PowerShell' ? '$ ' : tool ? '› ' : '  ';
    g.fillStyle = tool === 'Bash' || tool === 'PowerShell' ? '#7ee787' : tool === 'Edit' || tool === 'Write' ? '#79c0ff' : tool ? '#8b949e' : '#e6edf3';
    if (!last) g.globalAlpha = 0.75;
    g.fillText(fit(prefix + l.replace(/^(Bash|PowerShell): /, ''), W - 24), 12, y); g.globalAlpha = 1;
  });
  if (!doc) { g.fillStyle = '#7ee787'; g.fillRect(12, 60 + lines.length * 24 - 14, 9, 17); }   // con trỏ
  s.t.needsUpdate = true;
}

function add(geo, mat, x, y, z, parent = scene, cast = true) {
  const m = new THREE.Mesh(geo, mat); m.position.set(x, y, z); m.castShadow = cast; m.receiveShadow = true; parent.add(m); return m;
}
const box = (w, h, d, mat, x, y, z, parent, cast) => add(new THREE.BoxGeometry(w, h, d), mat, x, y, z, parent, cast);
const rbox = (w, h, d, r, mat, x, y, z, parent) => add(new RoundedBoxGeometry(w, h, d, 3, r), mat, x, y, z, parent);
const cyl = (rt, rb, h, mat, x, y, z, parent, seg = 24) => add(new THREE.CylinderGeometry(rt, rb, h, seg), mat, x, y, z, parent);

const SKY = {};                                            // ngày/đêm: đèn, trời, đèn bàn — applyTime() chỉnh theo giờ
function buildRoom() {
  mats();
  const sun = SKY.sun = new THREE.DirectionalLight('#fff1dc', 1.8);
  sun.position.set(-6, 11, 7); sun.target.position.set(0, 0, -1);
  sun.castShadow = true; sun.shadow.mapSize.set(2048, 2048);
  Object.assign(sun.shadow.camera, { left: -12, right: 12, top: 10, bottom: -10, near: 1, far: 30 });
  sun.shadow.bias = -0.0004; sun.shadow.normalBias = 0.025;
  SKY.hemi = new THREE.HemisphereLight('#f2f6ff', '#8a7360', 0.35);
  scene.add(sun, sun.target, SKY.hemi);
  SKY.lamps = [];

  const floor = add(new THREE.PlaneGeometry(18, 12), M.floor, 0, 0, 0, scene, false); floor.rotation.x = -Math.PI / 2;
  box(18, 3.2, 0.2, M.wall, 0, 1.6, -6.1, scene, false);
  box(0.2, 3.2, 12.4, M.wall, -9.1, 1.6, 0, scene, false);
  box(18, 0.1, 0.04, M.black, 0, 0.05, -5.98); box(0.04, 0.1, 12, M.black, -8.98, 0.05, 0);
  for (const wx of [-5.4, 0, 5.4]) {                     // cửa sổ lớn trên tường sau
    box(3.6, 2.0, 0.02, M.sky, wx, 1.75, -5.99, scene, false);
    for (const dx of [-1.8, -0.6, 0.6, 1.8]) box(0.05, 2.0, 0.06, M.black, wx + dx, 1.75, -5.96);
    box(3.65, 0.06, 0.08, M.black, wx, 0.75, -5.95); box(3.65, 0.06, 0.08, M.black, wx, 2.75, -5.95);
    box(3.65, 0.04, 0.3, M.white, wx, 0.72, -5.85);
  }

  box(1.0, 2.2, 0.04, new THREE.MeshStandardMaterial({ color: '#5b4636', roughness: 0.6 }), DOOR.x, 1.1, -5.98, scene, false);   // cửa ra vào
  for (const dx of [-0.53, 0.53]) box(0.06, 2.26, 0.08, M.black, DOOR.x + dx, 1.13, -5.96);
  box(1.12, 0.06, 0.08, M.black, DOOR.x, 2.26, -5.96); cyl(0.02, 0.02, 0.14, M.alu, DOOR.x + 0.38, 1.0, -5.93, scene, 8).rotation.z = Math.PI / 2;
  for (const [role, d] of Object.entries(DESKS)) buildDesk(role, d);
  buildMeeting();
  buildCoffee();
  buildWhiteboard();
  buildClock();
  for (const [x, z, s] of [[-8.3, -5.3, 1.2], [8.4, -2.6, 1.1], [-8.3, 5.3, 0.9], [7.0, 1.6, 1.0], [-1.5, -5.4, 0.8]]) plant(x, z, s);
  const rug = add(new THREE.CircleGeometry(2.1, 48), M.rug, MEET.x, 0.005, MEET.z, scene, false); rug.rotation.x = -Math.PI / 2;
  floorLamp(MEET.x - 1.6, MEET.z - 1.4); floorLamp(-7.7, -2.9);
}

// Đèn: bóng phát sáng + PointLight ấm (không đổ bóng); tắt ban ngày, bật khi trời tối.
function makeLamp(x, y, z, power) {
  const bulb = add(new THREE.SphereGeometry(0.05, 12, 8), new THREE.MeshStandardMaterial({ color: '#fff3d6', emissive: '#ffcf8a', emissiveIntensity: 0 }), x, y, z, scene, false);
  const light = new THREE.PointLight('#ffc98a', 0, 6, 2); light.position.set(x, y, z); scene.add(light);
  SKY.lamps.push({ bulb, light, power });
}
function deskLamp(x, z) {
  cyl(0.07, 0.08, 0.02, M.black, x, 0.765, z, scene, 16);
  const arm = box(0.015, 0.42, 0.015, M.black, x, 0.97, z); arm.rotation.x = 0.25;
  const shade = cyl(0.05, 0.1, 0.1, M.black, x, 1.16, z + 0.08, scene, 16); shade.rotation.x = 0.35;
  makeLamp(x, 1.11, z + 0.1, 2.2);
}
function floorLamp(x, z) {
  cyl(0.16, 0.18, 0.03, M.black, x, 0.015, z, scene, 24); cyl(0.015, 0.015, 1.6, M.black, x, 0.8, z, scene, 8);
  cyl(0.18, 0.24, 0.3, new THREE.MeshStandardMaterial({ color: '#efe6d6', roughness: 0.9, side: THREE.DoubleSide }), x, 1.7, z, scene, 24);
  makeLamp(x, 1.62, z, 5);
}

// Giờ thật (hoặc ?hour=) ⇒ nắng, trời, đèn. day: 0 đêm … 1 ban ngày; gold: nắng chiều vàng.
let fixedHour = null;
const lerpC = (a, b, t) => new THREE.Color(a).lerp(new THREE.Color(b), THREE.MathUtils.clamp(t, 0, 1));
function applyTime() {
  const now = new Date(), h = fixedHour ?? now.getHours() + now.getMinutes() / 60;
  const ss = THREE.MathUtils.smoothstep;
  const day = ss(h, 5.5, 7.5) * (1 - ss(h, 17.8, 19.3));
  const gold = ss(h, 15.5, 17.5) * (1 - ss(h, 18.6, 19.3)) + ss(h, 5.5, 6.5) * (1 - ss(h, 7, 8)) * 0.7;
  const a = THREE.MathUtils.clamp((h - 6) / 13, 0, 1) * Math.PI;   // cung mặt trời 6h → 19h
  SKY.sun.position.set(-Math.cos(a) * 9, Math.max(1.5, Math.sin(a) * 11), 7);
  SKY.sun.intensity = 0.06 + day * 1.75;
  SKY.sun.color.copy(day < 0.15 ? new THREE.Color('#9db4ff') : lerpC('#fff1dc', '#ffae5c', gold));
  SKY.hemi.intensity = 0.07 + day * 0.28;
  scene.environmentIntensity = 0.1 + day * 0.35;
  renderer.toneMappingExposure = 1.05 - day * 0.13;
  const bg = lerpC('#121722', '#cfd5dc', day); scene.background.copy(bg); scene.fog.color.copy(bg);
  for (const l of SKY.lamps) { const on = 1 - day; l.light.intensity = on * l.power; l.bulb.material.emissiveIntensity = on * 2.5; }
  drawSky(day, gold);
}

// Trời ngoài cửa sổ: chuyển màu theo giờ + dãy nhà; ban đêm nhà lên đèn
let skyState = '';
function drawSky(day, gold) {
  const key = day.toFixed(2) + gold.toFixed(2); if (key === skyState) return; skyState = key;
  const { c, t } = M.sky.userData, g = c.getContext('2d');
  const gr = g.createLinearGradient(0, 0, 0, 256);
  gr.addColorStop(0, `#${lerpC(lerpC('#0b1020', '#9cc5ea', day), '#f08a4b', gold * 0.6).getHexString()}`);
  gr.addColorStop(0.75, `#${lerpC(lerpC('#1b2440', '#dcebf7', day), '#ffd29a', gold).getHexString()}`);
  gr.addColorStop(1, `#${lerpC('#232c45', '#eef3f6', day).getHexString()}`);
  g.fillStyle = gr; g.fillRect(0, 0, 512, 256);
  const rnd = mulberry(7);                                // cùng một dãy nhà mỗi lần vẽ
  for (let x = 0; x < 512;) {
    const w = 18 + rnd() * 40, h = 40 + rnd() * 110;
    g.fillStyle = `#${lerpC('#0e1220', '#7f93a8', day).getHexString()}`; g.globalAlpha = 0.35 + (1 - day) * 0.6; g.fillRect(x, 256 - h, w, h); g.globalAlpha = 1;
    if (day < 0.6) for (let wy = 256 - h + 6; wy < 250; wy += 9) for (let wx = x + 4; wx < x + w - 4; wx += 7)
      if (rnd() < 0.35) { g.fillStyle = `rgba(255,214,140,${(0.6 - day) * 1.4})`; g.fillRect(wx, wy, 3, 4); }
    x += w + 2 + rnd() * 8;
  }
  t.needsUpdate = true;
}
function mulberry(a) { return () => { a |= 0; a = (a + 0x6D2B79F5) | 0; let t = Math.imul(a ^ (a >>> 15), 1 | a); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; }

// Ghế xoay: trả về group, tâm ghế tại gốc, mặt ghế ở 0.47 m; trượt ra sau khi người đứng dậy.
function officeChair() {
  const g = new THREE.Group();
  rbox(0.5, 0.08, 0.48, 0.03, M.fabric, 0, 0.47, 0, g);
  rbox(0.46, 0.55, 0.07, 0.03, M.fabric, 0, 0.82, -0.24, g).rotation.x = -0.08;
  box(0.05, 0.3, 0.03, M.black, 0, 0.56, -0.23, g);
  cyl(0.025, 0.025, 0.36, M.alu, 0, 0.25, 0, g, 12);
  for (let i = 0; i < 5; i++) { const leg = box(0.3, 0.025, 0.04, M.black, 0, 0.07, 0, g); leg.rotation.y = (i / 5) * Math.PI * 2; leg.translateX(0.15); cyl(0.022, 0.022, 0.04, M.black, Math.cos(-(i / 5) * Math.PI * 2) * 0.29, 0.03, Math.sin(-(i / 5) * Math.PI * 2) * 0.29, g, 8); }
  scene.add(g); return g;
}

function makeSeat(x, z, rot, chair) {
  const f = new THREE.Vector3(Math.sin(rot), 0, Math.cos(rot)), r = new THREE.Vector3(f.z, 0, -f.x);
  const seat = { pos: new THREE.Vector3(x, 0, z), rot, f, r, chair, chairIn: new THREE.Vector3(x - f.x * 0.06, 0, z - f.z * 0.06), occupied: null };
  seat.chairOut = seat.chairIn.clone().addScaledVector(f, -0.45);
  seat.chairTarget = seat.chairIn.clone();
  chair.position.copy(seat.chairIn); chair.rotation.y = rot;
  return seat;
}

const seats = {}; const meetSeats = []; const screens = {}; const obstacles = [];
function buildDesk(role, d) {
  const w = d.w || 1.4, cz = d.z + 0.12 + 0.35;
  const g = new THREE.Group(); scene.add(g);
  rbox(w, 0.035, 0.7, 0.01, M.white, d.x, 0.74, cz, g);
  for (const sx of [-1, 1]) {                             // khung chân kim loại mảnh
    for (const sz of [-1, 1]) box(0.035, 0.72, 0.035, M.black, d.x + sx * (w / 2 - 0.07), 0.36, cz + sz * 0.28, g);
    box(0.035, 0.035, 0.6, M.black, d.x + sx * (w / 2 - 0.07), 0.69, cz, g);
  }
  box(w - 0.14, 0.035, 0.035, M.black, d.x, 0.69, cz + 0.28, g);
  // laptop: đế + màn mở nghiêng về phía người ngồi
  const lx = d.x, lz = d.z + 0.36;
  rbox(0.34, 0.018, 0.24, 0.006, M.alu, lx, 0.767, lz, g);
  const lid = new THREE.Group(); lid.position.set(lx, 0.776, lz + 0.12); lid.rotation.x = -0.32; g.add(lid);
  rbox(0.34, 0.23, 0.012, 0.006, M.alu, 0, 0.115, 0, lid);
  screens[role] = add(new THREE.PlaneGeometry(0.31, 0.2), M.screen.off, 0, 0.118, -0.007, lid); screens[role].rotation.y = Math.PI;
  screenCanvas(role);
  if (role === 'Leader') { rbox(0.55, 0.32, 0.03, 0.01, M.black, d.x + 0.55, 0.98, d.z + 0.7, g); box(0.05, 0.2, 0.05, M.black, d.x + 0.55, 0.85, d.z + 0.7, g); }
  cyl(0.04, 0.035, 0.09, M.white, d.x - w / 2 + 0.2, 0.8, d.z + 0.45, g, 16);  // cốc
  deskLamp(role === 'Leader' ? d.x - 0.42 : d.x + w / 2 - 0.18, d.z + 0.66);
  seats[role] = makeSeat(d.x, d.z, 0, officeChair());
  obstacles.push([d.x - w / 2, cz - 0.35, d.x + w / 2, cz + 0.35]);
}

function buildMeeting() {
  const top = cyl(MEET.r, MEET.r, 0.04, M.oak, MEET.x, 0.74, MEET.z, scene, 64);
  cyl(0.08, 0.1, 0.7, M.black, MEET.x, 0.37, MEET.z); cyl(0.4, 0.45, 0.03, M.black, MEET.x, 0.015, MEET.z, scene, 32);
  top.receiveShadow = true;
  for (let i = 0; i < 5; i++) {
    const a = (i / 5) * Math.PI * 2 + 0.3, rr = MEET.r + 0.13;
    const x = MEET.x + Math.sin(a) * rr, z = MEET.z + Math.cos(a) * rr;
    meetSeats.push(makeSeat(x, z, a + Math.PI, officeChair()));
  }
  meetSeats.sort((a, b) => a.pos.z - b.pos.z);            // ghế phía xa camera trước ⇒ người họp quay mặt về phía người xem
  obstacles.push([MEET.x - MEET.r, MEET.z - MEET.r, MEET.x + MEET.r, MEET.z + MEET.r]);
}

const COFFEE = [];
function buildCoffee() {
  const x = -8.55, z0 = -5.6, len = 2.4;
  box(0.6, 0.86, len, M.oak, x, 0.43, z0 + len / 2); box(0.64, 0.04, len + 0.04, M.white, x, 0.88, z0 + len / 2);
  rbox(0.3, 0.42, 0.34, 0.03, M.black, x, 1.11, z0 + 0.6); box(0.16, 0.06, 0.12, M.alu, x + 0.12, 1.0, z0 + 0.6);
  for (let i = 0; i < 4; i++) cyl(0.04, 0.035, 0.09, M.white, x + 0.1, 0.95, z0 + 1.3 + i * 0.13, scene, 16);
  box(0.02, 0.9, 1.6, M.oak, -8.98, 2.1, z0 + 1.2);
  COFFEE.push({ pos: new THREE.Vector3(-7.75, 0, -4.9), rot: -Math.PI / 2 }, { pos: new THREE.Vector3(-7.75, 0, -4.0), rot: -Math.PI / 2 });
  obstacles.push([x - 0.32, z0, x + 0.32, z0 + len]);
}

function buildWhiteboard() {
  const c = document.createElement('canvas'); c.width = 1024; c.height = 576;
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8;
  box(0.04, 1.2, 2.1, new THREE.MeshStandardMaterial({ map: t, emissive: '#ffffff', emissiveMap: t, emissiveIntensity: 0.35, roughness: 0.3 }), -8.97, 1.55, -0.5, scene, false);
  box(0.05, 1.26, 2.16, M.alu, -8.99, 1.55, -0.5, scene, false);
  board = { c, t };
}
function drawBoard() {
  const { c, t } = board, g = c.getContext('2d'), it = window.wb.items(), b = window.wb.batch();
  g.fillStyle = '#fbfbf9'; g.fillRect(0, 0, c.width, c.height);
  g.fillStyle = '#1f2937'; g.font = 'bold 44px "Segoe UI", sans-serif'; g.fillText(b || '—', 40, 70);
  const ws = it.filter(i => i.batch === b && i.role !== 'Leader');
  const phases = [...new Set(ws.map(window.wb.phaseOf))].sort();
  g.font = '34px "Segoe UI", sans-serif'; let y = 140;
  for (const p of phases) {
    const list = ws.filter(i => window.wb.phaseOf(i) === p).sort((a, z) => new Date(a.created) - new Date(z.created));
    const last = list[list.length - 1], run = list.some(i => i.running), v = [...list].reverse().find(i => i.verdict)?.verdict;
    g.fillStyle = '#374151'; g.fillText(p, 40, y);
    g.fillStyle = run ? '#2563eb' : v === 'PASS' ? '#127a3a' : v ? '#b42318' : '#6b7280';
    g.fillText(run ? `⟳ ${window.wb.label(last)}` : v ? `${v === 'PASS' ? '✔' : '✘'} ${v} · ${list.length} lượt` : `${list.length} lượt`, 230, y);
    y += 56; if (y > 540) break;
  }
  t.needsUpdate = true;
}

function buildClock() {
  const c = document.createElement('canvas'); c.width = c.height = 256;
  clockTex = { c, t: new THREE.CanvasTexture(c), last: -1 }; clockTex.t.colorSpace = THREE.SRGBColorSpace;
  const face = add(new THREE.CircleGeometry(0.28, 48), new THREE.MeshStandardMaterial({ map: clockTex.t, roughness: 0.4 }), 3.2, 2.55, -5.98, scene, false);
  cyl(0.3, 0.3, 0.04, M.black, 3.2, 2.55, -6.0, scene, 48).rotation.x = Math.PI / 2;
  face.position.z = -5.97;
  drawClock();
}
function drawClock() {
  const now = new Date(); if (now.getMinutes() === clockTex.last) return; clockTex.last = now.getMinutes();
  if (SKY.sun && renderer) applyTime();                   // mỗi phút: chỉnh nắng/trời/đèn theo giờ
  const g = clockTex.c.getContext('2d'); g.fillStyle = '#fafafa'; g.fillRect(0, 0, 256, 256);
  g.translate(128, 128); g.strokeStyle = '#111';
  for (let i = 0; i < 12; i++) { g.save(); g.rotate(i * Math.PI / 6); g.lineWidth = i % 3 ? 3 : 7; g.beginPath(); g.moveTo(0, -112); g.lineTo(0, -92); g.stroke(); g.restore(); }
  const hand = (a, len, w) => { g.save(); g.rotate(a); g.lineWidth = w; g.lineCap = 'round'; g.beginPath(); g.moveTo(0, 12); g.lineTo(0, -len); g.stroke(); g.restore(); };
  hand(((now.getHours() % 12) + now.getMinutes() / 60) * Math.PI / 6, 60, 9); hand(now.getMinutes() * Math.PI / 30, 92, 6);
  g.setTransform(1, 0, 0, 1, 0, 0); clockTex.t.needsUpdate = true;
}

function plant(x, z, s) {
  const g = new THREE.Group(); g.position.set(x, 0, z); g.scale.setScalar(s); scene.add(g);
  cyl(0.2, 0.15, 0.4, M.pot, 0, 0.2, 0, g);
  const leafGeo = new THREE.SphereGeometry(0.5, 12, 8, 0, Math.PI * 2, 0, Math.PI / 2); leafGeo.scale(0.32, 0.06, 0.13);
  for (let i = 0; i < 22; i++) {
    const a = i * 2.4, h = 0.45 + (i / 22) * 0.75, l = add(leafGeo, M.leaf, 0, h, 0, g);
    l.rotation.set(0, a, 0.5 + Math.random() * 0.6); l.translateX(0.16); l.rotateZ(-0.2);
  }
  obstacles.push([x - 0.3 * s, z - 0.3 * s, x + 0.3 * s, z + 0.3 * s]);
}

// ---------------------------------------------------------------- tìm đường (A* trên lưới 0.25 m)

const G = { x0: -8.8, z0: -5.8, cs: 0.25, nx: 70, nz: 47 };
let grid = null;
function buildGrid() {
  grid = new Uint8Array(G.nx * G.nz);
  for (const [x1, z1, x2, z2] of obstacles)
    for (let i = 0; i < G.nx; i++) for (let j = 0; j < G.nz; j++) {
      const x = G.x0 + i * G.cs, z = G.z0 + j * G.cs, p = 0.3;
      if (x > x1 - p && x < x2 + p && z > z1 - p && z < z2 + p) grid[j * G.nx + i] = 1;
    }
}
const cellOf = v => [Math.max(0, Math.min(G.nx - 1, Math.round((v.x - G.x0) / G.cs))), Math.max(0, Math.min(G.nz - 1, Math.round((v.z - G.z0) / G.cs)))];
const free = (i, j) => i >= 0 && j >= 0 && i < G.nx && j < G.nz && !grid[j * G.nx + i];
function nearestFree([i, j]) {
  for (let r = 0; r < 12; r++) for (let di = -r; di <= r; di++) for (let dj = -r; dj <= r; dj++) if (free(i + di, j + dj)) return [i + di, j + dj];
  return [i, j];
}
function findPath(from, to) {
  if (!grid) buildGrid();
  const s = nearestFree(cellOf(from)), e = nearestFree(cellOf(to)), key = (i, j) => j * G.nx + i;
  const gs = new Map([[key(...s), 0]]), came = new Map(), open = [[0, ...s]], closed = new Set();
  while (open.length) {
    open.sort((a, b) => a[0] - b[0]);
    const [, i, j] = open.shift(), k = key(i, j);
    if (closed.has(k)) continue; closed.add(k);
    if (i === e[0] && j === e[1]) break;
    for (const [di, dj] of [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, 1], [-1, -1]]) {
      const ni = i + di, nj = j + dj; if (!free(ni, nj) || (di && dj && (!free(i + di, j) || !free(i, j + dj)))) continue;
      const nk = key(ni, nj), cost = gs.get(k) + (di && dj ? 1.414 : 1);
      if (cost < (gs.get(nk) ?? Infinity)) { gs.set(nk, cost); came.set(nk, k); open.push([cost + Math.hypot(e[0] - ni, e[1] - nj), ni, nj]); }
    }
  }
  const pts = []; let k = key(...e);
  if (!came.has(k) && k !== key(...s)) return [to.clone()];
  while (k !== undefined) { pts.unshift(new THREE.Vector3(G.x0 + (k % G.nx) * G.cs, 0, G.z0 + Math.floor(k / G.nx) * G.cs)); k = came.get(k); }
  pts.push(to.clone());
  const out = [pts[0]];                                   // kéo thẳng: bỏ điểm giữa khi nhìn thẳng tới được
  for (let a = 0; a < pts.length - 1;) {
    let b = pts.length - 1; while (b > a + 1 && !sight(pts[a], pts[b])) b--;
    out.push(pts[b]); a = b;
  }
  return out.slice(1);
}
function sight(a, b) {
  const n = Math.ceil(a.distanceTo(b) / 0.1);
  for (let t = 1; t < n; t++) { const p = a.clone().lerp(b, t / n); if (!free(...cellOf(p))) return false; }
  return true;
}

// ---------------------------------------------------------------- nhân vật

const _q = new THREE.Quaternion(), _q2 = new THREE.Quaternion(), _up = new THREE.Vector3();
class Character {
  constructor(role, model, clips) {
    this.role = role; this.model = model; this.clips = clips;
    this.root = new THREE.Group(); this.root.add(model); scene.add(this.root);
    this.mixer = new THREE.AnimationMixer(model); this.actions = {}; this.cur = null;
    this.speed = clips.walk?.userData.speed || 1.2;
    this.tasks = []; this.task = null; this.seat = null; this.mode = 'idle'; this.nextIdle = 0; this.scripted = false; this.typing = false; this.item = null;
    this.initFace(model);
    this.neck = model.getObjectByName('Bip01_Neck'); this.head = model.getObjectByName('Bip01_Head'); this.lookAt = null; this.lookYaw = 0;
    this.hands = ['Bip01_L_Hand', 'Bip01_R_Hand'].map(n => model.getObjectByName(n));
    this.fingers = []; model.traverse(o => o.isBone && /Finger\d$/.test(o.name) && this.fingers.push(o));
    const proxy = new THREE.Mesh(new THREE.CylinderGeometry(0.35, 0.35, 1.8, 8), new THREE.MeshBasicMaterial({ visible: false }));
    proxy.position.y = 0.9; proxy.userData.char = this; this.root.add(proxy); hitProxies.push(proxy);
    this.tag = document.createElement('div'); this.tag.className = 'otag';
    this.tag.innerHTML = `<b>${NAMES[role] || role}</b>${NAMES[role] && NAMES[role] !== role ? ` <span class="r">${role}</span>` : ''}<div class="s"></div>`;
    if (SCR[role]) this.tag.appendChild(SCR[role].c);       // rê chuột vào người ⇒ thấy màn hình của họ
    this.tagObj = new CSS2DObject(this.tag); this.tagObj.position.y = 2.0; this.root.add(this.tagObj);
    this.bub = document.createElement('div'); this.bub.className = 'obub'; this.bub.hidden = true;
    this.bubObj = new CSS2DObject(this.bub); this.bubObj.position.y = 2.35; this.root.add(this.bubObj);
    this.play('stand');
  }
  play(name, fade = 0.4) {
    const clip = this.clips[name] || this.clips.stand; if (this.cur?.getClip() === clip) return;
    const a = this.actions[name] ??= this.mixer.clipAction(clip);
    a.reset(); a.time = Math.random() * clip.duration * (name === 'walk' ? 0 : 0.6); a.play();
    if (this.cur) this.cur.crossFadeTo(a, fade, false);
    this.cur = a;
  }
  // Nét mặt (blendshape của bản _facial): chớp mắt, khẩu hình khi nói, cười/cau mày theo tâm trạng.
  initFace(model) {
    model.traverse(o => { if (o.morphTargetDictionary && !this.face) this.face = o; });
    if (!this.face) return;
    // Chỉ giữ ~20 blendshape dùng tới (bản gốc 175 ⇒ ~80 MB GPU mỗi người, shader duyệt cả 175 mỗi đỉnh)
    const USED = ['AK_09_EyeBlinkLeft', 'AK_10_EyeBlinkRight', 'AK_25_JawOpen', 'AK_44_MouthSmileLeft', 'AK_45_MouthSmileRight',
      'AK_01_BrowDownLeft', 'AK_02_BrowDownRight', 'AK_30_MouthFrownLeft', 'AK_31_MouthFrownRight',
      'AA_VI_10_aa', 'AA_VI_11_E', 'AA_VI_12_I', 'AA_VI_13_O', 'AA_VI_14_U', 'AA_VI_01_PP', 'AA_VI_08_nn', 'AA_VI_07_SS', 'AA_VI_02_FF'];
    const src = this.face.morphTargetDictionary, geo = this.face.geometry, dict = {}, keep = [];
    for (const n of USED) { const k = Object.keys(src).find(k => k.endsWith('.' + n) || k === n); if (k !== undefined) { dict[n] = keep.length; keep.push(src[k]); } }
    for (const a of Object.keys(geo.morphAttributes)) geo.morphAttributes[a] = keep.map(i => geo.morphAttributes[a][i]);
    this.face.morphTargetDictionary = dict; this.face.morphTargetInfluences = keep.map(() => 0);
    const idx = s => dict[s] ?? -1;
    this.fx = {
      blink: [idx('AK_09_EyeBlinkLeft'), idx('AK_10_EyeBlinkRight')], jaw: idx('AK_25_JawOpen'),
      smile: [idx('AK_44_MouthSmileLeft'), idx('AK_45_MouthSmileRight')], brow: [idx('AK_01_BrowDownLeft'), idx('AK_02_BrowDownRight')],
      frown: [idx('AK_30_MouthFrownLeft'), idx('AK_31_MouthFrownRight')],
      vis: ['AA_VI_10_aa', 'AA_VI_11_E', 'AA_VI_12_I', 'AA_VI_13_O', 'AA_VI_14_U', 'AA_VI_01_PP', 'AA_VI_08_nn', 'AA_VI_07_SS', 'AA_VI_02_FF'].map(idx).filter(i => i >= 0),
    };
    this.blinkIn = 1 + Math.random() * 4; this.vis = -1; this.visAmp = 0; this.visNext = 0;
    this.talkUntil = 0; this.mood = 0; this.moodUntil = 0;
  }
  updateFace(dt) {
    if (!this.face) return;
    const f = this.fx, inf = this.face.morphTargetInfluences, t = performance.now() / 1000;
    const set = (i, v, k) => { if (i >= 0) inf[i] += (v - inf[i]) * Math.min(1, dt * k); };
    this.blinkIn -= dt; let b = 0;                         // chớp: nhắm 70 ms, mở 80 ms; thỉnh thoảng chớp đôi
    if (this.blinkIn < 0) { const p = -this.blinkIn; b = p < 0.07 ? p / 0.07 : p < 0.15 ? 1 - (p - 0.07) / 0.08 : 0; if (p > 0.15) this.blinkIn = Math.random() < 0.15 ? 0.25 : 2 + Math.random() * 4; }
    for (const i of f.blink) if (i >= 0) inf[i] = b;
    const talking = t < this.talkUntil || this.cur?.getClip().name === 'talk';
    if (talking && t > this.visNext) {                     // đổi khẩu hình mỗi 80–180 ms, có quãng ngắt hơi
      const pause = Math.random() < 0.12;
      this.vis = pause ? -1 : pick(f.vis); this.visAmp = 0.45 + Math.random() * 0.5; this.visNext = t + (pause ? 0.15 + Math.random() * 0.2 : 0.08 + Math.random() * 0.1);
    }
    for (const i of f.vis) set(i, talking && i === this.vis ? this.visAmp : 0, 18);
    set(f.jaw, talking && this.vis >= 0 ? 0.1 + this.visAmp * 0.2 : 0, 15);
    const m = t < this.moodUntil ? this.mood : 0;
    for (const i of f.smile) set(i, 0.08 + Math.max(0, m) * 0.7, 3);
    for (const i of [...f.brow, ...f.frown]) set(i, Math.max(0, -m) * 0.55, 3);
  }
  // Quay cổ + đầu nhìn người đang nói chuyện (lookAt: Character | null), tối đa ~60°, chồng lên chuyển động đang chạy.
  updateLook(dt) {
    let want = 0;
    if (this.lookAt?.root?.visible) {
      const d = this.lookAt.root.position.clone().sub(this.root.position), a = Math.atan2(d.x, d.z) - this.root.rotation.y;
      want = THREE.MathUtils.clamp(Math.atan2(Math.sin(a), Math.cos(a)), -1.05, 1.05);
    }
    this.lookYaw += (want - this.lookYaw) * Math.min(1, dt * 4);
    if (Math.abs(this.lookYaw) < 0.01 || !this.head) return;
    for (const [bone, part] of [[this.neck, 0.4], [this.head, 0.6]]) {
      if (!bone) continue;
      bone.parent.getWorldQuaternion(_q).invert();
      bone.quaternion.premultiply(_q2.setFromAxisAngle(_up.set(0, 1, 0).applyQuaternion(_q), this.lookYaw * part));
    }
  }
  say(text, kind = '', secs = 6) {
    const now = performance.now() / 1000;
    if (text) {
      this.talkUntil = now + Math.min(secs, 1.2 + text.length * 0.07);
      if (kind === 'ok' || kind === 'bad') { this.mood = kind === 'ok' ? 1 : -1; this.moodUntil = now + secs + 2; }
    }
    this.bub.textContent = text; this.bub.className = 'obub ' + kind; this.bub.hidden = !text;
    clearTimeout(this.bubTimer); if (text && secs) this.bubTimer = setTimeout(() => (this.bub.hidden = true), secs * 1000);
  }
  setStatus(text) { this.tag.querySelector('.s').textContent = text; }
  place(pos, rot) { this.root.position.copy(pos); this.root.rotation.y = rot; }
  // ----- việc trong hàng đợi: mỗi việc { start?, tick(dt) ⇒ true khi xong }
  do(...tasks) { this.tasks.push(...tasks); return this; }
  clear() {
    this.tasks = []; this.task = null; this.scripted = false; if (!this.seat) this.play('stand');
    for (const s of COFFEE) if (s.by === this) s.by = null;   // bị gọi đi giữa chừng ⇒ trả chỗ cà phê
  }
  update(dt) {
    if (!this.task && this.tasks.length) { this.task = this.tasks.shift(); this.task.start?.(this); }
    if (this.task && this.task.tick(this, dt)) this.task = null;
    if (!this.task && !this.tasks.length) { this.scripted = false; behave(this); }
    this.mixer.update(dt);
    this.updateFace(dt);
    this.updateLook(dt);
    const ty = this.seat ? 1.55 : 2.0;                   // nhãn ngay trên đầu: thấp khi ngồi, cao khi đứng
    this.tagObj.position.y += (ty - this.tagObj.position.y) * Math.min(1, dt * 4); this.bubObj.position.y = this.tagObj.position.y + 0.35;
    if (this.typing && this.seat) {                       // gõ phím: lắc nhẹ cổ tay + ngón, chồng lên tư thế ngồi bàn
      const t = performance.now() / 1000;
      if (soundOn && (this.keyIn = (this.keyIn ?? 0) - dt) < 0) {   // tiếng gõ: từng tràng, có lúc ngừng tay
        sfx('key', this); this.keyIn = Math.random() < 0.08 ? 0.6 + Math.random() * 1.4 : 0.07 + Math.random() * 0.12;
      }
      this.hands.forEach((h, i) => h && h.rotateZ(Math.sin(t * 11 + i * 2.1) * 0.09 + Math.sin(t * 17 + i) * 0.04));
      this.fingers.forEach((f, i) => f.rotateZ(Math.max(0, Math.sin(t * 14 + i * 1.7)) * 0.35));
    }
  }
}

// Việc cơ bản
const T = {
  wait: s => ({ start: c => (c.left = s), tick: (c, dt) => (c.left -= dt) <= 0 }),
  until: fn => ({ tick: () => fn() }),
  anim: (name, s) => ({ start: c => { c.play(name); c.left = s; }, tick: (c, dt) => (c.left -= dt) <= 0 }),
  call: fn => ({ tick: c => (fn(c), true) }),
  face: target => ({ tick: (c, dt) => turnTo(c, typeof target === 'function' ? target() : target, dt * 6) }),
  standUp: () => ({
    start: c => { if (c.seat) { c.seat.chairTarget = c.seat.chairOut; c.seat.occupied = null; c.seat = null; c.typing = false; c.play('stand', 0.5); c.left = 0.5; } else c.left = 0; },
    tick: (c, dt) => (c.left -= dt) <= 0,
  }),
  walk: dest => ({                                        // dest: Vector3 hoặc hàm trả Vector3 (đích động)
    start: c => { const d = typeof dest === 'function' ? dest() : dest; c.path = c.root.position.distanceTo(d) < 0.05 ? [] : findPath(c.root.position, d); if (c.path.length) c.play('walk', 0.3); },
    tick: (c, dt) => {
      if (!c.path.length) return true;
      const p = c.root.position, n = c.path[0], dir = n.clone().sub(p); dir.y = 0;
      const dist = dir.length(), step = c.speed * dt;
      turnTo(c, Math.atan2(dir.x, dir.z), dt * 7);
      if (dist <= step) { p.copy(n); c.path.shift(); } else p.addScaledVector(dir.normalize(), step);
      if (!c.path.length) { c.play('stand', 0.35); return true; }
      return false;
    },
  }),
  sit: (seat, anim = 'sit') => ({
    start: c => { seat.chairTarget = seat.chairOut; c.left = 0.35; c.seat0 = c.root.position.clone(); },
    tick: (c, dt) => {
      c.left -= dt; const k = 1 - Math.max(0, c.left) / 0.35;
      c.root.position.lerpVectors(c.seat0, seat.pos, k); turnTo(c, seat.rot, dt * 10);
      if (c.left > 0) return false;
      c.place(seat.pos, seat.rot); c.seat = seat; seat.occupied = c; seat.chairTarget = seat.chairIn; c.play(anim, 0.45);
      return true;
    },
  }),
};
function turnTo(c, ang, k) {
  let d = ang - c.root.rotation.y; d = Math.atan2(Math.sin(d), Math.cos(d));
  c.root.rotation.y += d * Math.min(1, k); return Math.abs(d) < 0.05;
}
const approach = seat => seat.pos.clone().addScaledVector(seat.r, -0.95).addScaledVector(seat.f, -0.15);
// Đi tới ghế rồi ngồi (đứng dậy trước nếu đang ngồi chỗ khác)
function goSit(c, seat, anim) {
  if (c.seat === seat) { c.do(T.call(() => c.play(anim))); return; }
  if (seat.occupied && seat.occupied !== c) return;
  seat.occupied = c;
  c.do(T.standUp(), T.walk(approach(seat)), T.sit(seat, anim));
}
const pick = a => a[Math.floor(Math.random() * a.length)];

// Hành vi theo chế độ khi hàng đợi trống
function behave(c) {
  const now = performance.now() / 1000;
  if (now < c.nextIdle) return;
  if (c.role === 'Bạn') return behaveUser(c, now);
  if (c.mode !== 'meeting') c.lookAt = null;
  const desk = seats[c.role];
  switch (c.mode) {
    case 'work':
      c.typing = true; c.nextIdle = now + 10 + Math.random() * 10;
      if (c.seat !== desk) goSit(c, desk, 'sit'); else c.play(Math.random() < 0.8 ? 'sit' : 'think');
      break;
    case 'review':
      c.typing = false; c.nextIdle = now + 8 + Math.random() * 8;
      if (c.seat !== desk) goSit(c, desk, 'think'); else c.play(pick(['think', 'sit', 'think', 'sit2']));
      break;
    case 'meeting': {
      c.typing = false; c.nextIdle = now + 6 + Math.random() * 6;
      const s = c.seat && meetSeats.includes(c.seat) ? c.seat : meetSeats.find(x => !x.occupied);
      if (s && c.seat !== s) goSit(c, s, 'sit2'); else if (s) c.play(pick(['shrug', 'think', 'sit2', 'sit']));
      if (Math.random() < 0.55) c.talkUntil = now + 2 + Math.random() * 4;   // họp: thay nhau nói
      c.lookAt = pick(meetSeats.filter(x => x.occupied && x.occupied !== c).map(x => x.occupied).concat([null]));
      break;
    }
    case 'stale':
      c.typing = false; c.nextIdle = now + 12;
      if (c.seat !== desk) goSit(c, desk, 'yawn'); else c.play('yawn');
      c.say('Zzz… lâu rồi chưa thấy động tĩnh', 'warn', 8);
      break;
    default: {                                            // rảnh: ngồi bàn, cà phê, ngắm cửa sổ, vươn vai
      c.typing = false; c.nextIdle = now + 18 + Math.random() * 25;
      const r = Math.random();
      if (r < 0.35) goSit(c, desk, 'sit2');
      else if (r < 0.65 && COFFEE.some(s => !s.by)) {        // mỗi chỗ cà phê một người
        const s = COFFEE.find(x => !x.by); s.by = c;
        c.do(T.standUp(), T.walk(s.pos), T.face(s.rot), T.anim('drink', 9), T.anim('stand', 2), T.call(() => { s.by = null; }));
      }
      else if (r < 0.85) { const p = new THREE.Vector3(-5 + Math.random() * 10, 0, -5.1); c.do(T.standUp(), T.walk(p), T.face(Math.PI), T.anim('look', 8)); }
      else c.do(T.standUp(), T.walk(approach(desk)), T.anim('stretch', 4));
    }
  }
}

// "Bạn": họp ⇒ bước vào từ cửa, ngồi bàn họp cạnh Leader; hết họp ⇒ ra cửa rồi biến mất.
function behaveUser(c, now) {
  c.nextIdle = now + 6 + Math.random() * 5;
  if (c.mode === 'meeting') {
    if (!c.root.visible) { c.place(DOOR, 0); c.root.visible = true; c.say('Chào cả nhà!', 'info', 4); focusOn(c, 14); sfx('door', c); }
    const s = c.seat && meetSeats.includes(c.seat) ? c.seat : meetSeats.find(x => !x.occupied);
    if (s && c.seat !== s) goSit(c, s, 'sit2'); else if (s) c.play(pick(['shrug', 'think', 'sit2', 'shrug']));
    if (Math.random() < 0.6) c.talkUntil = now + 2 + Math.random() * 5;
    c.lookAt = chars.Leader;
  } else if (c.root.visible) {
    c.do(T.standUp(), T.walk(DOOR), T.face(Math.PI), T.call(() => { c.root.visible = false; }));
    c.nextIdle = now + 1e9;                               // ở ngoài cho tới lần họp sau (director đặt lại nextIdle)
  }
}

// ---------------------------------------------------------------- đạo diễn: dữ liệu thật ⇒ chế độ + cảnh

function director() {
  if (!Object.keys(chars).length) return;
  const items = window.wb.items(), latest = window.wb.batches()[0];
  const running = items.filter(i => i.running);
  const runOf = role => running.filter(i => i.role === role).sort((a, b) => new Date(b.modified) - new Date(a.modified))[0];
  const staleOf = role => items.find(i => i.role === role && i.batch === latest && !i.running && !i.ended && Date.now() - new Date(i.modified) < 2 * 3600e3);
  const discovery = running.find(i => i.role === 'BA/SA' && window.wb.phaseOf(i) === 'Discovery');
  const userMeet = items.filter(i => i.role === 'Leader' && i.userAt && Date.now() - new Date(i.userAt) < USER_MEET_MS)
    .sort((a, b) => new Date(b.userAt) - new Date(a.userAt))[0];

  for (const [role, c] of Object.entries(chars)) {
    if (role === 'Bạn') {
      const mode = userMeet ? 'meeting' : 'away';
      if (mode !== c.mode) { c.mode = mode; c.nextIdle = 0; if (!c.scripted) c.tasks = []; }
      c.item = userMeet || null; c.setStatus(userMeet ? 'đang họp với Leader' : '');
      continue;
    }
    const it = runOf(role);
    let mode = 'idle';
    if (role === 'Leader') mode = discovery || userMeet ? 'meeting' : it ? 'work' : 'idle';
    else if (it) mode = role === 'BA/SA' && discovery === it ? 'meeting' : role === 'Dev' || role === 'End User' ? 'work' : 'review';
    else if (staleOf(role)) mode = 'stale';
    if (mode !== c.mode) { c.mode = mode; c.nextIdle = 0; if (!c.scripted) { c.tasks = []; } }
    c.item = it || (role === 'Leader' ? userMeet : null) || null;
    c.setStatus(role === 'Leader' && userMeet ? 'đang họp với Bạn' : it ? `${STATUS[mode]} · ${role === 'Leader' ? 'điều phối' : window.wb.label(it)}` : STATUS[mode]);
    updateScreen(role, it, mode);
  }

  const ids = new Set(running.map(i => i.id));
  if (prevRunning) {
    for (const i of running) if (!prevRunning.has(i.id) && chars[i.role] && i.role !== 'Leader') handoff(chars[i.role], i);
    for (const id of prevRunning) if (!ids.has(id)) { const i = items.find(x => x.id === id); if (i && chars[i.role] && i.role !== 'Leader') report(chars[i.role], i); }
  } else placeInitial();
  prevRunning = ids;
  if (board) drawBoard();
}

// Lần đầu: đặt mọi người thẳng vào chỗ theo chế độ, không bắt cả phòng đi bộ.
function placeInitial() {
  for (const c of Object.values(chars)) {
    if (c.role === 'Bạn' && c.mode !== 'meeting') { c.root.visible = false; c.place(DOOR, 0); c.nextIdle = Infinity; continue; }
    const seat = c.mode === 'meeting' ? meetSeats.find(x => !x.occupied) : seats[c.role];
    seat.occupied = c; c.seat = seat; c.place(seat.pos, seat.rot); seat.chairTarget = seat.chairIn;
    c.play(c.mode === 'stale' ? 'yawn' : c.mode === 'review' ? 'think' : c.mode === 'meeting' ? 'sit2' : 'sit', 0);
    c.typing = c.mode === 'work'; c.nextIdle = performance.now() / 1000 + 4 + Math.random() * 12;
  }
}

// Cảnh giao việc: Leader tới bàn người được giao, nói vài câu rồi về.
function handoff(w, item) {
  const L = chars.Leader; if (!L) return;
  const desk = seats[w.role];
  const spot = desk.pos.clone().addScaledVector(desk.r, -1.15).addScaledVector(desk.f, 0.2);
  w.clear(); w.scripted = true; goSit(w, desk, 'think');
  w.do(T.until(() => L.root.position.distanceTo(spot) < 0.3), T.call(() => { w.lookAt = L; w.say(`Nhận việc: ${window.wb.label(item)}`, 'info', 6); }),
    T.anim('think', 7), T.call(() => (w.lookAt = null)));
  if (!L.scripted) L.clear();                             // Leader đang bận cảnh khác ⇒ xếp sau, không cắt ngang
  L.scripted = true;
  L.do(T.call(() => focusOn(L, 16)), T.standUp(), T.walk(spot), T.face(() => Math.atan2(desk.pos.x - L.root.position.x, desk.pos.z - L.root.position.z)),
    T.call(() => { L.lookAt = w; L.say(`Giao ${window.wb.label(item)}`, 'info', 6); sfx('task', L); }), T.anim('talk', 6), T.call(() => (L.lookAt = null)), T.anim('stand', 1));
}

// Cảnh báo cáo: người vừa xong lượt tới chỗ Leader báo kết quả.
function report(w, item) {
  const L = chars.Leader; if (!L) return;
  const v = item.verdict, ok = !v || v === 'PASS';
  const spotOf = () => {
    const lp = L.root.position, dir = w.root.position.clone().sub(lp).setY(0).normalize();
    return lp.clone().addScaledVector(dir.lengthSq() ? dir : new THREE.Vector3(1, 0, 0), 1.1);
  };
  const faceL = () => Math.atan2(L.root.position.x - w.root.position.x, L.root.position.z - w.root.position.z);
  w.clear(); w.scripted = true; w.arrived = false;
  w.do(
    T.until(() => !L.scripted),                           // chờ Leader rảnh rồi mới tới, rồi giữ Leader đứng yên chờ
    T.call(() => {
      focusOn(w, 18);
      L.clear(); L.scripted = true;
      L.do(T.until(() => w.arrived), T.call(() => (L.lookAt = w)),
        T.wait(2.5), T.call(() => L.say(ok ? 'Tốt, đi tiếp!' : 'Ừ, sửa vòng nữa nhé', ok ? 'ok' : 'bad', 6)), T.wait(6), T.call(() => (L.lookAt = null)));
    }),
    T.standUp(), T.walk(spotOf), T.face(faceL), T.call(() => { w.arrived = true; w.lookAt = L; }),
    T.call(() => { w.say(v ? `${window.wb.label(item)}: ${v}` : `Xong ${window.wb.label(item)}`, v ? (ok ? 'ok' : 'bad') : 'info', 7); sfx(ok ? 'pass' : 'fail', w); }),
    T.anim('talk', 6), ok ? T.anim('cheer', 3) : T.anim('scratch', 4), T.call(() => (w.lookAt = null)), T.anim('stand', 1));
}

// ---------------------------------------------------------------- âm thanh (WebAudio tổng hợp, mặc định tắt)

let audio = null, master = null, noiseBuf = null, soundOn = false;
try { soundOn = localStorage.getItem('wb-sound') === '1'; } catch {}
function ac() {                                           // trình duyệt chỉ cho phát sau thao tác của người xem ⇒ tạo khi bấm
  if (!audio) {
    audio = new AudioContext(); master = audio.createGain(); master.gain.value = 0.6; master.connect(audio.destination);
    noiseBuf = audio.createBuffer(1, audio.sampleRate * 0.03, audio.sampleRate);
    const d = noiseBuf.getChannelData(0); for (let i = 0; i < d.length; i++) d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / d.length, 3);
  }
  if (audio.state === 'suspended') audio.resume();
  return audio;
}
const distVol = c => THREE.MathUtils.clamp(5 / Math.max(1, camera.position.distanceTo(c.root.position)), 0.12, 1);
function tones(freqs, { type = 'sine', vol = 0.12, gap = 0.13, dur = 0.45 } = {}) {
  const t0 = audio.currentTime;
  freqs.forEach((f, i) => {
    const o = audio.createOscillator(), g = audio.createGain(), t = t0 + i * gap;
    o.type = type; o.frequency.value = f; g.gain.setValueAtTime(0, t); g.gain.linearRampToValueAtTime(vol, t + 0.01); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g).connect(master); o.start(t); o.stop(t + dur + 0.05);
  });
}
function keyClick(vol) {
  const s = audio.createBufferSource(), f = audio.createBiquadFilter(), g = audio.createGain();
  s.buffer = noiseBuf; s.playbackRate.value = 0.8 + Math.random() * 0.5; f.type = 'bandpass'; f.frequency.value = 2500 + Math.random() * 1800; f.Q.value = 1.2;
  g.gain.value = vol * (0.25 + Math.random() * 0.2); s.connect(f).connect(g).connect(master); s.start();
}
function sfx(name, c) {
  if (!soundOn || !audio || audio.state !== 'running') return;
  const v = c ? distVol(c) : 1;
  if (name === 'pass') tones([880, 1318.5], { vol: 0.12 * v });
  else if (name === 'fail') tones([311, 233], { type: 'triangle', vol: 0.14 * v, gap: 0.18, dur: 0.5 });
  else if (name === 'task') tones([659.3], { vol: 0.06 * v, dur: 0.3 });
  else if (name === 'door') tones([784, 622.3], { vol: 0.09 * v, gap: 0.22, dur: 0.7 });
  else if (name === 'key') keyClick(v);
}

// ---------------------------------------------------------------- vòng vẽ, camera theo dõi, chuột

function focusOn(c, secs) { if (followOn) follow = { c, until: performance.now() / 1000 + secs }; }
const _t = new THREE.Vector3(), _p = new THREE.Vector3();
function updateFollow(dt) {
  if (!follow) return;
  const now = performance.now() / 1000;
  if (!follow.home && now > follow.until) follow = { home: true, until: now + 3.5 };
  if (follow.home) { _t.copy(HOME.target); _p.copy(HOME.pos); }
  else {
    _t.copy(follow.c.root.position); _t.y = follow.c.seat ? 1.0 : 1.25;
    _p.copy(_t).addScaledVector(FOLLOW_DIR, FOLLOW_DIST);
  }
  const k = Math.min(1, dt * 1.6);
  controls.target.lerp(_t, k); camera.position.lerp(_p, k);
  if (follow.home && now > follow.until) follow = null;
}

function loop() {
  if (!visible) return;
  raf = requestAnimationFrame(loop);
  if (document.hidden) return;
  const dt = Math.min(clock.getDelta(), 0.05) * (window.__office?.timeScale || 1);
  for (const c of Object.values(chars)) c.update(dt);
  for (const s of [...Object.values(seats), ...meetSeats]) s.chair.position.lerp(s.chairTarget, Math.min(1, dt * 5));
  drawClock();
  updateFollow(dt);
  controls.update();
  renderer.render(scene, camera); labels.render(scene, camera);
}
function resize() {
  if (!renderer) return;
  const w = host.clientWidth, h = host.clientHeight; if (!w || !h) return;
  renderer.setSize(w, h); labels.setSize(w, h); camera.aspect = w / h; camera.updateProjectionMatrix();
}
const ray = new THREE.Raycaster(), ndc = new THREE.Vector2();
function hit(e) {
  const r = renderer.domElement.getBoundingClientRect();
  ndc.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
  ray.setFromCamera(ndc, camera);
  return ray.intersectObjects(hitProxies.filter(p => p.userData.char.root.visible), false)[0]?.object.userData.char;
}
let hovered = null;
function onHover(e) {
  const c = hit(e); if (c === hovered) return;
  hovered?.tag.classList.remove('hot'); hovered = c; c?.tag.classList.add('hot');
  renderer.domElement.style.cursor = c ? 'pointer' : '';
}
// Bấm vào người ⇒ mở khung chi tiết của lượt đang chạy (hoặc lượt gần nhất) của vai đó.
function onClick(e) {
  const c = hit(e); if (!c) return;
  const items = window.wb.items(), b = window.wb.batch();
  const mine = items.filter(i => i.role === c.role).sort((a, z) => new Date(z.modified) - new Date(a.modified));
  const it = c.item || mine.find(i => i.batch === b) || mine[0] || (c.role === 'Leader' ? items.find(i => i.role === 'Leader') : null);
  if (it) window.wb.open(it); else c.say('Chưa có lượt nào', '', 3);
}

function injectCss() {
  const s = document.createElement('style');
  s.textContent = `
  .office-labels { position:absolute; top:0; left:0; pointer-events:none }
  .ofollow { position:absolute; top:12px; left:12px; z-index:2; font:13px system-ui, "Segoe UI", sans-serif; color:#fff; background:rgba(17,20,26,.72); border:1px solid rgba(255,255,255,.18); border-radius:8px; padding:6px 12px; cursor:pointer }
  .ofollow:hover { background:rgba(91,82,232,.92) }
  .otag { font:12px/1.3 system-ui, "Segoe UI", sans-serif; color:#fff; background:rgba(17,20,26,.72); padding:3px 8px; border-radius:8px; text-align:center; white-space:nowrap; transform:translateY(-4px) }
  .otag .r { color:#aab3c0; font-size:11px } .otag .s { color:#cfd6df; font-size:11px; max-width:220px; overflow:hidden; text-overflow:ellipsis }
  .otag.hot { background:rgba(91,82,232,.92) }
  .otag .scr { display:none; width:300px; margin:6px auto 2px; border-radius:6px; box-shadow:0 6px 18px rgba(0,0,0,.45) }
  .otag.hot .scr.on { display:block }
  .obub { font:600 12.5px/1.35 system-ui, "Segoe UI", sans-serif; background:#fff; color:#1f2937; padding:6px 10px; border-radius:12px; max-width:240px; text-align:center; box-shadow:0 4px 14px rgba(0,0,0,.25); transform:translateY(-6px) }
  .obub.ok { background:#127a3a; color:#fff } .obub.bad { background:#b42318; color:#fff } .obub.warn { background:#f0883e; color:#fff } .obub.info { background:#2563eb; color:#fff }`;
  document.head.appendChild(s);
}
