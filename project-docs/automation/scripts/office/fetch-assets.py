"""Tải nhân vật + chuyển động Microsoft Rocketbox (MIT) cho chế độ "Văn phòng" của worker-viewer.

Chạy từ root repo:  python project-docs/automation/scripts/office/fetch-assets.py
Cần: Python 3 + Pillow (pip install pillow). Tải ~350 MB một lần (texture TGA 2048 gốc),
chỉ giữ lại ~25 MB: texture được thu về 1024 px (JPG/PNG), TGA gốc không ghi ra đĩa.
Chạy lại an toàn: file đã có thì bỏ qua. Kết quả ở office/assets/ (gitignore).
"""
import io
import json
import re
import sys
import urllib.request
from pathlib import Path

from PIL import Image

RAW = "https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/master/Assets"
OUT = Path(__file__).resolve().parent / "assets"

# Vai ⇒ nhân vật. Đổi tên ở đây để đổi người (danh sách: Assets/Avatars/Professions/Business_*).
AVATARS = {
    "Leader": "Business_Male_01",
    "BA/SA": "Business_Female_01",
    "Dev": "Business_Male_02",
    "Reviewer": "Business_Female_03",
    "End User": "Business_Female_02",
    "Bạn": "Business_Male_03",  # chính User: vào họp với Leader khi vừa nhắn trong phiên Leader
}

# Tên logic ⇒ (thư mục chuyển động, tên file không tiền tố f_/m_). Nam thiếu "sit_table_idle_stretch arms" nên dùng bản đứng.
CLIPS = {
    "stand": ("static", "idle_neutral_01"),
    "look": ("static", "idle_look_around_01"),
    "walk": ("xy", "walk_neutral_01"),
    "sit": ("static", "sit_table_idle_neutral_01"),
    "sit2": ("static", "sit_table_idle_neutral_02"),
    "think": ("static", "sit_table_gestic_thoughtful"),
    "shrug": ("static", "sit_table_gestic_shrug_01"),
    "yawn": ("static", "sit_table_idle_yawn"),
    "talk": ("static", "gestic_talk_neutral_01"),
    "listen": ("static", "gestic_listen_neutral_01"),
    "drink": ("static", "drink_drinking"),
    "cheer": ("static", "cheer_01"),
    "scratch": ("static", "idle_scratch_head_01"),
    "docs": ("static", "documents_check"),
    "stretch": ("static", "idle_stretch_arms_01"),
}
ANIM_DIR = {"static": "all_animations_max_motextr_static", "xy": "all_animations_max_motextr_xy"}


def get(url: str) -> bytes:
    with urllib.request.urlopen(url.replace(" ", "%20"), timeout=120) as r:
        return r.read()


def save_once(path: Path, fetch) -> bool:
    if path.exists() and path.stat().st_size > 0:
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    data = fetch()
    tmp = path.with_suffix(path.suffix + ".part")
    tmp.write_bytes(data)
    tmp.replace(path)
    return True


def texture(url: str, kind: str) -> bytes:
    img = Image.open(io.BytesIO(get(url)))
    img = img.resize((1024, 1024), Image.LANCZOS) if img.width > 1024 else img
    buf = io.BytesIO()
    if kind == "opacity":  # tóc/mi: cần kênh alpha
        img.convert("RGBA").save(buf, "PNG", optimize=True)
    else:
        img.convert("RGB").save(buf, "JPEG", quality=88 if kind == "color" else 92)
    return buf.getvalue()


def main() -> int:
    manifest = {"avatars": {}, "clips": {g: {} for g in "fm"}}
    for role, name in AVATARS.items():
        base = f"{RAW}/Avatars/Professions/{name}"
        # bản _facial có 175 blendshape (viseme nói, chớp mắt, cười, cau mày) — dùng cho nét mặt khi nói chuyện
        fbx = OUT / "avatars" / name / f"{name}_facial.fbx"
        if save_once(fbx, lambda: get(f"{base}/Export/{name}_facial.fbx")):
            print("tải", fbx.name)
        prefix = re.search(rb"([fm]\d{3})_body_color\.tga", fbx.read_bytes()).group(1).decode()
        tex = {}
        for part, kind, ext in [("body_color", "color", "jpg"), ("body_normal", "normal", "jpg"),
                                ("head_color", "color", "jpg"), ("head_normal", "normal", "jpg"),
                                ("opacity_color", "opacity", "png")]:
            out = fbx.parent / f"{part}.{ext}"
            if save_once(out, lambda: texture(f"{base}/Textures/{prefix}_{part}.tga", kind)):
                print("  texture", name, part)
            tex[part] = out.name
        manifest["avatars"][role] = {"name": name, "gender": "f" if "Female" in name else "m",
                                     "fbx": f"avatars/{name}/{name}_facial.fbx", "textures": {k: f"avatars/{name}/{v}" for k, v in tex.items()}}
    for g in "fm":
        for key, (kind, clip) in CLIPS.items():
            out = OUT / "anims" / g / f"{key}.fbx"
            if save_once(out, lambda: get(f"{RAW}/Animations/{ANIM_DIR[kind]}/{g}_{clip}.max.fbx")):
                print("chuyển động", g, key)
            manifest["clips"][g][key] = f"anims/{g}/{key}.fbx"
    (OUT / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1), encoding="utf-8")
    size = sum(p.stat().st_size for p in OUT.rglob("*") if p.is_file())
    print(f"Xong: {OUT} — {size / 1e6:.0f} MB")
    return 0


if __name__ == "__main__":
    sys.exit(main())
