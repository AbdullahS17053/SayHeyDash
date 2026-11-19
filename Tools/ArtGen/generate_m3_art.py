"""
Generates the Milestone 3 production pixel art for The Say Hey Dash.

All art is original and drawn from code so it is reproducible and easy to tweak:
  * Outfielder animation frames (32x32): idle, run, run_reach, catch, whiff, spin, throw
  * Ball spin frames (8x8)
  * Stadium environment (320x180) + animated crowd and flag layers
  * Title-screen backdrop and logo
  * UI kit: 9-slice panels/buttons, cursor, icons, rank badges, on-screen controls
  * Bitmap pixel font atlas (+ metrics JSON read by the Unity font builder)
  * Preview sheets for review (Docs/img/, not imported by Unity)

Usage (from the repository root):
    py -I Tools/ArtGen/generate_m3_art.py

No third-party packages required. Colours come only from the art-direction palette
(Docs/02_Art_Direction.html): the six GDD colours plus the six proposed extensions.
"""
import json
import math
import os
import struct
import sys
import zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Assets", "_Project", "Art")
SPRITES = os.path.join(ART, "Sprites")
PREVIEW_DIR = os.path.join(ROOT, "Docs", "img")

# --- Palette ---------------------------------------------------------------
MIDNIGHT = (0x17, 0x15, 0x2F, 255)
YELLOW = (0xF6, 0xCE, 0x4B, 255)
ORANGE = (0xF3, 0x8B, 0x37, 255)
GREEN = (0x3C, 0x76, 0x59, 255)
OFFWHITE = (0xF5, 0xF1, 0xD9, 255)
RED = (0xD9, 0x4D, 0x58, 255)
DUSK = (0x2E, 0x2A, 0x52, 255)
GREEN_DARK = (0x2C, 0x5A, 0x44, 255)
GREEN_LIGHT = (0x46, 0x86, 0x6A, 255)
DIRT = (0x9C, 0x6B, 0x3E, 255)
SKIN = (0x7A, 0x4A, 0x2E, 255)
LEATHER = (0xA8, 0x5F, 0x2C, 255)
SHADOW = (0x17, 0x15, 0x2F, 140)
CLEAR = (0, 0, 0, 0)
WHITE = (255, 255, 255, 255)  # font atlas only (tinted at runtime)

PALETTE = {
    "K": MIDNIGHT, "Y": YELLOW, "O": ORANGE, "G": GREEN, "W": OFFWHITE, "R": RED,
    "D": DUSK, "g": GREEN_DARK, "l": GREEN_LIGHT, "T": DIRT, "S": SKIN, "L": LEATHER,
    ".": CLEAR, "#": WHITE,
}


# --- PNG + canvas helpers ----------------------------------------------------
def write_png(path, img):
    h = len(img)
    w = len(img[0])
    raw = bytearray()
    for row in img:
        raw.append(0)
        for px in row:
            raw += bytes(px if px is not None else CLEAR)

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    os.makedirs(os.path.dirname(path), exist_ok=True)
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def new_img(w, h, color=CLEAR):
    return [[color for _ in range(w)] for _ in range(h)]


def put(img, x, y, color):
    if 0 <= y < len(img) and 0 <= x < len(img[0]):
        img[y][x] = color


def fill(img, x0, y0, x1, y1, color):
    for y in range(max(0, y0), min(len(img), y1)):
        for x in range(max(0, x0), min(len(img[0]), x1)):
            img[y][x] = color


def blit(dst, src, ox, oy):
    for y, row in enumerate(src):
        for x, c in enumerate(row):
            if c is not None and c[3] > 0:
                put(dst, ox + x, oy + y, c)


def from_rows(rows, key=PALETTE):
    w = max(len(r) for r in rows)
    return [[key[c] for c in r.ljust(w, ".")] for r in rows]


def flip_h(img):
    return [list(reversed(r)) for r in img]


def scale(img, k):
    out = []
    for row in img:
        big = []
        for c in row:
            big.extend([c] * k)
        for _ in range(k):
            out.append(list(big))
    return out


def outline(img, color=MIDNIGHT, diagonal=False):
    """1 px outline around every opaque pixel (returns a new image)."""
    h, w = len(img), len(img[0])
    out = [list(r) for r in img]
    n = [(1, 0), (-1, 0), (0, 1), (0, -1)]
    if diagonal:
        n += [(1, 1), (1, -1), (-1, 1), (-1, -1)]
    for y in range(h):
        for x in range(w):
            if img[y][x][3] > 0:
                continue
            for dx, dy in n:
                xx, yy = x + dx, y + dy
                if 0 <= xx < w and 0 <= yy < h and img[yy][xx][3] > 0:
                    out[y][x] = color
                    break
    return out


# --- Outfielder rig ----------------------------------------------------------
# Frames are drawn facing RIGHT on a 32x32 cell; feet sit on row 26 (the prefab
# offsets the sprite so the GameObject origin is the feet). Each body part is a
# set of capsules sharing one 1 px Midnight outline, composited back-to-front so
# overlapping limbs keep a readable internal outline.

CELL = 32

HEADS = {
    # cap (D) with brim, face/neck in skin, eye in Midnight. Anchor = bottom-centre.
    "side": [
        "..DDDD...",
        ".DDDDDD..",
        ".DDDDDDDD",
        ".SSSSSS..",
        ".SSSSKSS.",
        ".SSSSSS..",
        "..SSSS...",
    ],
    "up": [  # head tipped back, looking up (tracking the ball)
        "....DDD..",
        "..DDDDDD.",
        ".DDDDDDDD",
        ".SSSSSKS.",
        ".SSSSSSS.",
        ".SSSSSS..",
        "..SSSS...",
    ],
    "back": [  # looking back over the shoulder (face turned toward home plate)
        "...DDDD..",
        "..DDDDDD.",
        "DDDDDDDD.",
        "..SSSSSS.",
        ".SSKSSSS.",
        "..SSSSSS.",
        "...SSSS..",
    ],
    "front": [
        "..DDDD..",
        ".DDDDDD.",
        "DDDDDDDD",
        ".SSSSSS.",
        ".SKSSKS.",
        ".SSSSSS.",
        "..SSSS..",
    ],
}


def capsule(ax, ay, bx, by, r):
    pts = set()
    x0, x1 = int(math.floor(min(ax, bx) - r - 1)), int(math.ceil(max(ax, bx) + r + 1))
    y0, y1 = int(math.floor(min(ay, by) - r - 1)), int(math.ceil(max(ay, by) + r + 1))
    dx, dy = bx - ax, by - ay
    ll = dx * dx + dy * dy
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px, py = x + 0.5, y + 0.5
            t = 0.0 if ll == 0 else max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / ll))
            cx, cy = ax + dx * t, ay + dy * t
            if (px - cx) ** 2 + (py - cy) ** 2 <= r * r:
                pts.add((x, y))
    return pts


def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def stamp(img, layers):
    """layers: list of (pixel set, colour). One shared outline, then fills in order."""
    union = set()
    for pts, _ in layers:
        union |= pts
    for (x, y) in union:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if (x + dx, y + dy) not in union:
                put(img, x + dx, y + dy, MIDNIGHT)
    for pts, color in layers:
        for (x, y) in pts:
            put(img, x, y, color)


def head_layers(kind, neck, flip=False):
    rows = HEADS[kind]
    img = from_rows(rows)
    if flip:
        img = flip_h(img)
    h, w = len(img), len(img[0])
    ox = int(round(neck[0] - w / 2.0))
    oy = int(round(neck[1])) - h
    by_color = {}
    for y in range(h):
        for x in range(w):
            c = img[y][x]
            if c[3] > 0:
                by_color.setdefault(c, set()).add((ox + x, oy + y))
    # Stable order: cap, skin, then details (eye) last.
    order = [DUSK, SKIN, MIDNIGHT]
    return [(by_color[c], c) for c in order if c in by_color]


def leg_layers(hip, knee, foot, toe=1):
    mid = lerp(knee, foot, 0.45)
    return [
        (capsule(hip[0], hip[1], knee[0], knee[1], 1.6), OFFWHITE),
        (capsule(knee[0], knee[1], mid[0], mid[1], 1.35), OFFWHITE),
        (capsule(mid[0], mid[1], foot[0], foot[1], 1.15), RED),
        (capsule(foot[0], foot[1] + 0.3, foot[0] + 2.0 * toe, foot[1] + 0.3, 1.0), MIDNIGHT),
    ]


def arm_layers(sho, elb, hand, glove=False, ball=False):
    layers = [
        (capsule(sho[0], sho[1], elb[0], elb[1], 1.35), OFFWHITE),
        (capsule(elb[0], elb[1], hand[0], hand[1], 1.1), SKIN),
    ]
    if glove:
        g = capsule(hand[0], hand[1], hand[0], hand[1], 2.1)
        layers.append((g, LEATHER))
        if ball:
            bx, by = int(math.floor(hand[0])), int(math.floor(hand[1])) - 1
            layers.append(({(bx, by), (bx + 1, by), (bx, by + 1), (bx + 1, by + 1)}, OFFWHITE))
    else:
        layers.append((capsule(hand[0], hand[1], hand[0], hand[1], 1.2), SKIN))
        if ball:
            bx, by = int(math.floor(hand[0])), int(math.floor(hand[1])) - 1
            layers.append(({(bx, by), (bx + 1, by)}, OFFWHITE))
    return layers


def torso_layers(hip, sho):
    pts = capsule(hip[0], hip[1], sho[0], sho[1], 3.1)
    belt = set()
    for (x, y) in pts:
        # Belt: the bottom two rows of the torso around the hip.
        if abs((y + 0.5) - (hip[1] + 0.6)) < 0.8:
            belt.add((x, y))
    return [(pts, OFFWHITE), (belt, RED)]


def draw_pose(p):
    """p: dict of joints (pixel coords, y down) + flags. Returns a 32x32 image facing right."""
    img = new_img(CELL, CELL)
    o = p.get("offset", (0, 0))

    def j(name):
        x, y = p[name]
        return (x + o[0], y + o[1])

    stamp(img, arm_layers(j("shoF"), j("elbF"), j("handF"), glove=p.get("gloveFar", False),
                          ball=p.get("ballFar", False)))
    stamp(img, leg_layers(j("hip"), j("kneeF"), j("footF"), p.get("toeF", 1)))
    stamp(img, torso_layers(j("hip"), j("sho")))
    stamp(img, leg_layers(j("hip"), j("kneeN"), j("footN"), p.get("toeN", 1)))
    stamp(img, head_layers(p.get("head", "side"), j("neck"), p.get("headFlip", False)))
    if not p.get("nearArmBehind", False):
        stamp(img, arm_layers(j("shoN"), j("elbN"), j("handN"), glove=p.get("gloveNear", True),
                              ball=p.get("ballNear", False)))
    else:  # arm tucked behind the body (drawn again under the torso next frame-wise)
        pass
    return img


BASE = {
    "hip": (15.5, 17.0), "sho": (15.5, 11.6), "neck": (16.0, 9.6),
    "shoN": (16.2, 11.8), "shoF": (14.8, 11.8),
    "kneeN": (16.8, 21.3), "footN": (16.6, 25.3),
    "kneeF": (14.6, 21.3), "footF": (14.2, 25.3),
    "elbN": (17.4, 14.6), "handN": (18.4, 17.6),
    "elbF": (13.8, 14.6), "handF": (13.8, 17.6),
}


def pose(**kw):
    p = dict(BASE)
    p.update(kw)
    return p


def shift(p, dx, dy, keys):
    q = dict(p)
    for k in keys:
        q[k] = (p[k][0] + dx, p[k][1] + dy)
    return q


UPPER = ("sho", "neck", "shoN", "shoF", "elbN", "handN", "elbF", "handF")


def run_legs(i):
    """Six-frame run cycle for the legs (contact, down, pass, contact, down, pass)."""
    cycles = [
        # kneeN, footN, kneeF, footF, bob
        ((19.0, 20.6), (21.6, 24.6), (13.4, 21.6), (10.6, 24.0), 0),
        ((18.2, 21.0), (19.4, 25.3), (13.0, 20.6), (11.4, 22.4), 1),
        ((17.0, 20.2), (15.4, 24.4), (16.6, 19.8), (17.0, 23.6), -1),
        ((13.4, 21.6), (10.6, 24.0), (19.0, 20.6), (21.6, 24.6), 0),
        ((13.0, 20.6), (11.4, 22.4), (18.2, 21.0), (19.4, 25.3), 1),
        ((16.6, 19.8), (17.0, 23.6), (17.0, 20.2), (15.4, 24.4), -1),
    ]
    return cycles[i]


def run_frame(i, reach=False):
    kN, fN, kF, fF, bob = run_legs(i)
    p = pose(kneeN=kN, footN=fN, kneeF=kF, footF=fF)
    # Forward lean.
    p["sho"] = (16.6, 11.8)
    p["neck"] = (17.4, 9.8)
    p["shoN"] = (17.2, 12.0)
    p["shoF"] = (15.8, 12.0)
    swing = [1, 0.5, 0, -1, -0.5, 0][i]
    if not reach:
        # Arms pump opposite to the legs.
        p["elbN"] = (17.2 - 2.6 * swing, 14.8)
        p["handN"] = (18.6 - 4.2 * swing, 16.4 + abs(swing) * 0.6)
        p["elbF"] = (15.8 + 2.6 * swing, 14.8)
        p["handF"] = (16.8 + 4.0 * swing, 16.2)
        p["head"] = "side"
    else:
        # Over-the-shoulder: glove arm stretched forward and up, eyes back on the ball.
        p["elbN"] = (20.4, 9.6 + 0.3 * abs(swing))
        p["handN"] = (23.4, 7.6 + 0.3 * abs(swing))
        p["elbF"] = (15.8 + 2.6 * swing, 14.8)
        p["handF"] = (16.8 + 4.0 * swing, 16.2)
        p["head"] = "back"
    p = shift(p, 0, -bob * 1.0 if bob < 0 else bob * 0.6, UPPER + ("hip",))
    return p


def idle_frame(i):
    p = pose()
    bob = [0, 0, 1, 1][i]
    p = shift(p, 0, bob, UPPER)
    if i == 3:  # glove tap
        p["elbN"] = (17.6, 14.2)
        p["handN"] = (18.0, 15.6 + bob)
        p["elbF"] = (14.6, 14.4)
        p["handF"] = (17.0, 15.8 + bob)
    return p


def catch_frames():
    frames = []
    # 0: glove fully extended, ball arriving.
    p = run_frame(0, reach=True)
    p["ballNear"] = True
    frames.append(p)
    # 1: glove squeezes, still running.
    p = run_frame(1, reach=True)
    p["handN"] = (23.0, 8.4)
    p["elbN"] = (20.2, 10.2)
    p["ballNear"] = True
    frames.append(p)
    # 2: pulls the ball in, head comes round.
    p = run_frame(2)
    p["elbN"] = (19.4, 12.6)
    p["handN"] = (20.4, 11.0)
    p["ballNear"] = True
    p["head"] = "side"
    frames.append(p)
    # 3: plants the front foot hard (braking).
    p = pose(kneeN=(19.6, 21.0), footN=(21.4, 25.3), kneeF=(13.6, 21.6), footF=(11.2, 25.3))
    p["hip"] = (15.6, 18.0)
    p = shift(p, -0.6, 1.0, UPPER)
    p["elbN"] = (17.8, 13.2)
    p["handN"] = (18.6, 12.6)
    p["elbF"] = (13.0, 14.6)
    p["handF"] = (11.8, 16.4)
    p["ballNear"] = True
    frames.append(p)
    return frames


def whiff_frames():
    # Glove stabs at the ball and comes up empty, then shoulders slump.
    a = run_frame(0, reach=True)
    b = run_frame(1, reach=True)
    b["handN"] = (23.6, 9.4)
    c = pose(kneeN=(18.6, 21.4), footN=(20.0, 25.3), kneeF=(13.8, 21.6), footF=(12.0, 25.3))
    c = shift(c, 0.4, 1.4, UPPER)
    c["head"] = "side"
    c["elbN"] = (17.8, 16.0)
    c["handN"] = (18.4, 19.4)
    c["elbF"] = (14.2, 16.0)
    c["handF"] = (14.0, 19.4)
    return [a, b, c]


def spin_frames():
    """The spin toward the infield: plant (right) -> front -> facing left. Ball in glove."""
    frames = []
    p = catch_frames()[3]
    frames.append(p)
    # Front view: symmetric body, glove and hand together at the chest.
    f = pose(kneeN=(17.8, 21.2), footN=(18.6, 25.3), kneeF=(13.2, 21.2), footF=(12.4, 25.3),
             toeN=0, toeF=0)
    f["head"] = "front"
    f["sho"] = (15.5, 11.8)
    f["neck"] = (15.5, 9.8)
    f["shoN"] = (18.2, 12.2)
    f["shoF"] = (12.8, 12.2)
    f["elbN"] = (18.8, 15.0)
    f["handN"] = (16.8, 14.6)
    f["elbF"] = (12.2, 15.0)
    f["handF"] = (14.4, 14.8)
    f["ballNear"] = True
    frames.append(f)
    # Facing left (drawn mirrored): weight on the back foot, ready to throw.
    g = pose(kneeN=(18.2, 21.4), footN=(19.6, 25.3), kneeF=(13.4, 21.4), footF=(11.6, 25.3))
    g["head"] = "side"
    g["elbN"] = (17.0, 14.0)
    g["handN"] = (18.6, 13.0)
    g["elbF"] = (13.2, 13.6)
    g["handF"] = (12.4, 11.0)
    g["ballNear"] = True
    g["_mirror"] = True
    frames.append(g)
    return frames


def throw_frames():
    """Crow-hop throw, drawn facing right with the THROWING arm on the near side (so it reads
    clearly) and the glove on the far side. Frame 0 is the 'set' pose held during the meter."""
    frames = []

    def t(**kw):
        p = pose(**kw)
        p["gloveNear"] = False
        p["gloveFar"] = True
        return p

    # 0: set - glove points at the target, ball hand back and low.
    p = t(kneeN=(18.6, 21.4), footN=(20.0, 25.3), kneeF=(13.6, 21.4), footF=(11.6, 25.3))
    p = shift(p, -0.8, 0.6, UPPER)
    p["elbF"] = (18.4, 13.0)
    p["handF"] = (21.0, 12.6)
    p["elbN"] = (13.4, 14.8)
    p["handN"] = (11.0, 16.4)
    p["ballNear"] = True
    frames.append(p)
    # 1: arm cocked high behind the head.
    p = t(kneeN=(19.0, 21.2), footN=(20.8, 25.3), kneeF=(13.4, 21.4), footF=(11.2, 25.3))
    p = shift(p, -1.0, 0.4, UPPER)
    p["elbF"] = (18.6, 12.6)
    p["handF"] = (21.0, 11.8)
    p["elbN"] = (12.2, 11.6)
    p["handN"] = (10.8, 8.2)
    p["ballNear"] = True
    frames.append(p)
    # 2: stride, arm comes over the top.
    p = t(kneeN=(20.2, 21.0), footN=(22.6, 25.3), kneeF=(13.0, 21.6), footF=(10.4, 25.0))
    p["hip"] = (16.0, 17.4)
    p = shift(p, 0.6, 0.4, UPPER)
    p["elbF"] = (17.4, 15.0)
    p["handF"] = (16.4, 17.2)
    p["elbN"] = (16.0, 8.6)
    p["handN"] = (18.6, 6.6)
    p["ballNear"] = True
    frames.append(p)
    # 3: release - arm whips forward (the ball sprite takes over from here).
    p = t(kneeN=(20.4, 21.0), footN=(22.8, 25.3), kneeF=(13.4, 21.0), footF=(11.0, 24.0))
    p["hip"] = (16.4, 17.4)
    p = shift(p, 1.6, 0.8, UPPER)
    p["elbF"] = (16.0, 15.6)
    p["handF"] = (14.6, 17.6)
    p["elbN"] = (20.6, 11.8)
    p["handN"] = (23.8, 12.6)
    frames.append(p)
    # 4: follow-through, back leg swings round.
    p = t(kneeN=(20.0, 21.2), footN=(22.4, 25.3), kneeF=(16.4, 20.0), footF=(15.0, 23.0))
    p["hip"] = (16.8, 17.6)
    p = shift(p, 2.4, 1.8, UPPER)
    p["elbF"] = (16.0, 16.4)
    p["handF"] = (14.8, 18.6)
    p["elbN"] = (21.0, 15.4)
    p["handN"] = (21.0, 19.0)
    frames.append(p)
    return frames


def render(p):
    img = draw_pose(p)
    return flip_h(img) if p.get("_mirror") else img


CLIPS = {
    "idle": [idle_frame(i) for i in range(4)],
    "run": [run_frame(i) for i in range(6)],
    "run_reach": [run_frame(i, reach=True) for i in range(6)],
    "catch": catch_frames(),
    "whiff": whiff_frames(),
    "spin": spin_frames(),
    "throw": throw_frames(),
}

# --- Ball -------------------------------------------------------------------
BALL_FRAMES = [
    ["..KKKK..", ".KWWWWK.", "KWWRWWWK", "KWRWWWWK", "KWWWWRWK", "KWWWRWWK", ".KWWWWK.", "..KKKK.."],
    ["..KKKK..", ".KWWWWK.", "KWWWWWWK", "KRWWWWRK", "KWRWWRWK", "KWWWWWWK", ".KWWWWK.", "..KKKK.."],
    ["..KKKK..", ".KWWWWK.", "KWWWRWWK", "KWWWWRWK", "KWRWWWWK", "KWWRWWWK", ".KWWWWK.", "..KKKK.."],
    ["..KKKK..", ".KWWWWK.", "KWRWWRWK", "KWWRRWWK", "KWWWWWWK", "KWWWWWWK", ".KWWWWK.", "..KKKK.."],
]
SHADOW_ROWS = ["..SSSS..", ".SSSSSS.", ".SSSSSS.", "..SSSS.."]


def write_character():
    out_dir = os.path.join(SPRITES, "Character")
    sheet_rows = []
    for name, poses in CLIPS.items():
        frames = [render(p) for p in poses]
        for i, f in enumerate(frames):
            write_png(os.path.join(out_dir, "outfielder_%s_%02d.png" % (name, i)), f)
        sheet_rows.append((name, frames))
    for i, rows in enumerate(BALL_FRAMES):
        write_png(os.path.join(out_dir, "ball_spin_%02d.png" % i), from_rows(rows))
    write_png(os.path.join(out_dir, "ball_shadow.png"),
              from_rows(SHADOW_ROWS, {".": CLEAR, "S": SHADOW}))
    return sheet_rows


def preview_sheet(rows, path, k=6):
    cols = max(len(f) for _, f in rows)
    pad = 4
    w = cols * (CELL + pad) + pad
    h = len(rows) * (CELL + pad) + pad
    img = new_img(w, h, DUSK)
    for r, (_, frames) in enumerate(rows):
        for c, f in enumerate(frames):
            x, y = pad + c * (CELL + pad), pad + r * (CELL + pad)
            fill(img, x, y, x + CELL, y + CELL, GREEN if (r + c) % 2 == 0 else GREEN_LIGHT)
            blit(img, f, x, y)
    write_png(path, scale(img, k))
    # Unscaled sheet as well (the "exported sprite sheet" deliverable).
    sheet = new_img(cols * CELL, len(rows) * CELL)
    for r, (_, frames) in enumerate(rows):
        for c, f in enumerate(frames):
            blit(sheet, f, c * CELL, r * CELL)
    write_png(path.replace("_preview", "_sheet"), sheet)


# --- Bitmap font ---------------------------------------------------------------
# Original 5x7 capitals (variable width) with a 2-row descender area. The bold face is
# made by OR-ing each glyph with itself shifted 1 px right (classic arcade bold).
GLYPHS = {
    "A": [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "B": ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
    "C": [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
    "D": ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."],
    "E": ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
    "F": ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
    "G": [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####"],
    "H": ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
    "I": ["###", ".#.", ".#.", ".#.", ".#.", ".#.", "###"],
    "J": ["..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.."],
    "K": ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"],
    "L": ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
    "M": ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"],
    "N": ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
    "O": [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "P": ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
    "Q": [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"],
    "R": ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
    "S": [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
    "T": ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
    "U": ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
    "V": ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."],
    "W": ["#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#"],
    "X": ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
    "Y": ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."],
    "Z": ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"],
    "0": [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."],
    "1": [".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###"],
    "2": [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
    "3": ["####.", "....#", "....#", ".###.", "....#", "....#", "####."],
    "4": ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
    "5": ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
    "6": [".###.", "#....", "#....", "####.", "#...#", "#...#", ".###."],
    "7": ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
    "8": [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
    "9": [".###.", "#...#", "#...#", ".####", "....#", "....#", ".###."],
    ".": [".", ".", ".", ".", ".", ".", "#"],
    ",": ["..", "..", "..", "..", "..", ".#", ".#", "#."],
    ":": [".", ".", "#", ".", ".", "#", "."],
    ";": ["..", "..", ".#", "..", "..", ".#", ".#", "#."],
    "!": ["#", "#", "#", "#", "#", ".", "#"],
    "?": [".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."],
    "-": ["....", "....", "....", "####", "....", "....", "...."],
    "+": [".....", "..#..", "..#..", "#####", "..#..", "..#..", "....."],
    "=": ["....", "....", "####", "....", "####", "....", "...."],
    "/": ["....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...."],
    "(": ["..#", ".#.", "#..", "#..", "#..", ".#.", "..#"],
    ")": ["#..", ".#.", "..#", "..#", "..#", ".#.", "#.."],
    "[": ["###", "#..", "#..", "#..", "#..", "#..", "###"],
    "]": ["###", "..#", "..#", "..#", "..#", "..#", "###"],
    "\"": ["#.#", "#.#", "...", "...", "...", "...", "..."],
    "'": ["#", "#", ".", ".", ".", ".", "."],
    "*": ["...#...", "..###..", "#######", ".#####.", "..###..", ".##.##.", "#.....#"],  # star
    "#": [".#.#.", "#####", ".#.#.", ".#.#.", "#####", ".#.#.", "....."],
    "%": ["##..#", "##.#.", "...#.", "..#..", ".#...", ".#.##", "#..##"],
    "&": [".##..", "#..#.", ".##..", ".#...", "#.#.#", "#..#.", ".##.#"],
    "<": ["...#", "..#.", ".#..", "#...", ".#..", "..#.", "...#"],
    ">": ["#...", ".#..", "..#.", "...#", "..#.", ".#..", "#..."],
    "_": [".....", ".....", ".....", ".....", ".....", ".....", "#####"],
    "|": ["#", "#", "#", "#", "#", "#", "#"],
}
FONT_CELL_W, FONT_CELL_H = 9, 10   # atlas cell (glyph rows 0-8 + 1 px gutter)
FONT_ROWS = 9                       # 7 cap rows + 2 descender rows
FONT_COLUMNS = 14


def glyph_bitmap(ch, bold):
    rows = GLYPHS[ch]
    w = max(len(r) for r in rows)
    bmp = [[r.ljust(w, ".")[x] == "#" for x in range(w)] for r in rows]
    bmp += [[False] * w for _ in range(FONT_ROWS - len(bmp))]
    if bold:
        bmp = [[row[x] if x < w else False for x in range(w + 1)] for row in bmp]
        bmp = [[row[x] or (x > 0 and row[x - 1]) for x in range(w + 1)] for row in bmp]
    return bmp


def write_font():
    chars = sorted(GLYPHS.keys(), key=lambda c: ord(c))
    faces = [("Regular", False), ("Bold", True)]
    per_face_rows = int(math.ceil(len(chars) / float(FONT_COLUMNS)))
    atlas_w = 128
    atlas_h = 1
    while atlas_h < per_face_rows * FONT_CELL_H * len(faces):
        atlas_h *= 2
    atlas = new_img(atlas_w, atlas_h)
    meta = {"cellRows": FONT_ROWS, "ascent": 7, "descent": 2, "lineHeight": 10,
            "atlasWidth": atlas_w, "atlasHeight": atlas_h, "faces": []}
    for fi, (face, bold) in enumerate(faces):
        glyphs = []
        for i, ch in enumerate(chars):
            bmp = glyph_bitmap(ch, bold)
            gx = (i % FONT_COLUMNS) * FONT_CELL_W
            gy = fi * per_face_rows * FONT_CELL_H + (i // FONT_COLUMNS) * FONT_CELL_H
            for y, row in enumerate(bmp):
                for x, on in enumerate(row):
                    if on:
                        atlas[gy + y][gx + x] = WHITE
            w = len(bmp[0])
            glyphs.append({"code": ord(ch), "x": gx, "y": gy, "w": w, "h": FONT_ROWS, "advance": w + 1})
        glyphs.append({"code": 32, "x": 0, "y": 0, "w": 0, "h": 0, "advance": 4 if bold else 3})
        meta["faces"].append({"name": face, "glyphs": glyphs})
    out = os.path.join(ART, "Fonts")
    write_png(os.path.join(out, "sayhey_pixel_atlas.png"), atlas)
    with open(os.path.join(out, "sayhey_pixel_metrics.json"), "w") as f:
        json.dump(meta, f, indent=1)
    return meta


def text_bitmap(text, bold=False, spacing=1):
    """Renders text with the pixel font into a boolean bitmap (used for baked art)."""
    cols = []
    for ch in text:
        if ch == " ":
            cols.extend([[False] * FONT_ROWS] * (4 if bold else 3))
            continue
        bmp = glyph_bitmap(ch, bold)
        for x in range(len(bmp[0])):
            cols.append([bmp[y][x] for y in range(FONT_ROWS)])
        cols.extend([[False] * FONT_ROWS] * spacing)
    if cols:
        cols = cols[:-spacing] if spacing else cols
    w = len(cols)
    return [[cols[x][y] for x in range(w)] for y in range(FONT_ROWS)]


def draw_bitmap(img, bmp, x0, y0, color, k=1):
    for y, row in enumerate(bmp):
        for x, on in enumerate(row):
            if on:
                fill(img, x0 + x * k, y0 + y * k, x0 + (x + 1) * k, y0 + (y + 1) * k, color)


# Tiny 3x5 glyphs for in-world signage (scoreboard, wall markers).
MINI = {
    "0": ["###", "#.#", "#.#", "#.#", "###"], "2": ["###", "..#", "###", "#..", "###"],
    "3": ["###", "..#", "###", "..#", "###"], "4": ["#.#", "#.#", "###", "..#", "..#"],
    "8": ["###", "#.#", "###", "#.#", "###"], "1": [".#.", "##.", ".#.", ".#.", "###"],
    "V": ["#.#", "#.#", "#.#", "#.#", ".#."], "I": ["###", ".#.", ".#.", ".#.", "###"],
    "S": ["###", "#..", "###", "..#", "###"], "H": ["#.#", "#.#", "###", "#.#", "#.#"],
    "O": ["###", "#.#", "#.#", "#.#", "###"], "M": ["#.#", "###", "###", "#.#", "#.#"],
    "E": ["###", "#..", "##.", "#..", "###"], "N": ["##.", "#.#", "#.#", "#.#", "#.#"],
    "U": ["#.#", "#.#", "#.#", "#.#", "###"], "T": ["###", ".#.", ".#.", ".#.", ".#."],
    "P": ["##.", "#.#", "##.", "#..", "#.."],
    "-": ["...", "...", "###", "...", "..."], " ": ["...", "...", "...", "...", "..."],
}


def mini_text(img, x, y, text, color):
    for ch in text:
        for gy, line in enumerate(MINI[ch]):
            for gx, c in enumerate(line):
                if c == "#":
                    put(img, x + gx, y + gy, color)
        x += 4


# --- Environment -------------------------------------------------------------
WALL_TOP, WALL_BOTTOM, TRACK_BOTTOM = 70, 88, 98   # rows; gameplay depends on these
CROWD_TOP, CROWD_H = 26, 44
FLAGS_TOP, FLAGS_H = 0, 12


def stadium():
    w, h = 320, 180
    img = new_img(w, h, DUSK)
    fill(img, 0, 0, w, 6, MIDNIGHT)
    fill(img, 0, 8, w, 9, MIDNIGHT)
    # Light towers on the bleacher backs.
    for cx in (34, 86, 234, 286):
        fill(img, cx, 9, cx + 1, 26, MIDNIGHT)
        fill(img, cx - 6, 5, cx + 7, 10, MIDNIGHT)
        for lx in range(cx - 5, cx + 6, 2):
            put(img, lx, 6, OFFWHITE)
            put(img, lx, 8, OFFWHITE)
    # Bleacher back wall and seating band (people are a separate animated layer).
    fill(img, 0, 22, w, 26, GREEN_DARK)
    fill(img, 0, 22, w, 23, OFFWHITE)
    fill(img, 0, 26, w, WALL_TOP, MIDNIGHT)
    for y in range(29, WALL_TOP, 4):
        fill(img, 0, y, w, y + 1, DUSK)
    for ax in (42, 84, 236, 278):  # aisles
        fill(img, ax, 26, ax + 3, WALL_TOP, DUSK)
    # Centre-field clubhouse with the scoreboard (no team names or marks).
    cx0, cx1 = 122, 198
    fill(img, cx0, 10, cx1, WALL_TOP, GREEN_DARK)
    fill(img, cx0, 10, cx1, 12, OFFWHITE)
    fill(img, cx0 - 2, 12, cx1 + 2, 13, MIDNIGHT)
    for fx in (128, 191):  # flag poles (flags animate on their own layer)
        fill(img, fx, 1, fx + 1, 10, OFFWHITE)
    fill(img, 132, 15, 188, 39, MIDNIGHT)
    fill(img, 132, 15, 188, 16, YELLOW)
    fill(img, 132, 38, 188, 39, YELLOW)
    fill(img, 132, 15, 133, 39, YELLOW)
    fill(img, 187, 15, 188, 39, YELLOW)
    mini_text(img, 136, 18, "VIS", OFFWHITE)
    mini_text(img, 176, 18, "2", YELLOW)
    mini_text(img, 136, 25, "HOME", OFFWHITE)
    mini_text(img, 176, 25, "2", YELLOW)
    mini_text(img, 136, 32, "TOP 8", ORANGE)
    mini_text(img, 160, 32, "OUT 0", ORANGE)
    for row_y in (44, 54):
        for wx in range(128, 194, 8):
            fill(img, wx, row_y, wx + 4, row_y + 5, MIDNIGHT)
            fill(img, wx, row_y + 5, wx + 4, row_y + 6, OFFWHITE)
    fill(img, 154, 60, 166, WALL_TOP, MIDNIGHT)  # clubhouse door / steps
    for sy in range(62, WALL_TOP, 2):
        fill(img, 154, sy, 166, sy + 1, DUSK)
    # Outfield wall.
    fill(img, 0, WALL_TOP, w, WALL_BOTTOM, GREEN)
    fill(img, 0, WALL_TOP, w, WALL_TOP + 2, GREEN_DARK)
    fill(img, 0, WALL_BOTTOM - 1, w, WALL_BOTTOM, GREEN_DARK)
    for sx in range(16, w, 32):
        fill(img, sx, WALL_TOP + 2, sx + 1, WALL_BOTTOM - 1, GREEN_DARK)
    mini_text(img, 154, 77, "483", OFFWHITE)  # centre-field depth marker
    # Warning track + a small memorial stone in deep centre field.
    fill(img, 0, WALL_BOTTOM, w, TRACK_BOTTOM, DIRT)
    stone = from_rows([".W.", "WWD", "WWD", "WWD", "WWD", "WWD", "WWD"])
    blit(img, outline(pad(stone, 1)), 175, 84)
    # Grass: flat mowing bands that widen toward the camera (depth without gradients).
    fill(img, 0, TRACK_BOTTOM, w, TRACK_BOTTOM + 1, GREEN_DARK)
    y, band, i = TRACK_BOTTOM + 1, 7, 0
    while y < h:
        fill(img, 0, y, w, min(h, y + band), GREEN if i % 2 == 0 else GREEN_LIGHT)
        y += band
        band += 1
        i += 1
    return img


def pad(img, n):
    w = len(img[0]) + 2 * n
    out = new_img(w, len(img) + 2 * n)
    blit(out, img, n, n)
    return out


def crowd_frame(phase):
    """People in the bleachers as 1x2 px figures. phase 0/1 = idle shuffle, 2 = cheering."""
    img = new_img(320, CROWD_H)
    colours = [ORANGE, OFFWHITE, YELLOW, RED, OFFWHITE, ORANGE]
    aisles = [(42, 45), (84, 87), (236, 239), (278, 281)]
    for row, y in enumerate(range(3, CROWD_H - 2, 4)):
        for x in range((row * 2) % 3, 320, 3):
            if 122 <= x < 198 or any(a <= x < b for a, b in aisles):
                continue
            seed = (x * 73 + row * 151) % 97
            if seed < 12:
                continue  # empty seat
            up = 0
            if phase == 1 and seed % 4 == 0:
                up = 1
            if phase == 2 and seed % 3 != 0:
                up = 1
            c = colours[seed % len(colours)]
            put(img, x, y - up, SKIN if seed % 5 else c)
            put(img, x, y + 1 - up, c)
            if phase == 2 and seed % 7 == 0:
                put(img, x, y - 2, c)  # arms / hats in the air
    return img


def flags_frame(phase):
    img = new_img(320, FLAGS_H)
    shapes = [
        ["OOOOOO.", "OOOOOOO", "OOOOOO.", "OOOO..."],
        [".OOOOO.", "OOOOOOO", "OOOOOOO", "..OOO.."],
    ]
    for fx, colour in ((129, ORANGE), (192, YELLOW)):
        rows = shapes[phase]
        for y, r in enumerate(rows):
            for x, c in enumerate(r):
                if c == "O":
                    put(img, fx + x, 1 + y, colour)
    return img


# --- Title art ----------------------------------------------------------------
def title_backdrop():
    w, h = 320, 180
    img = new_img(w, h, MIDNIGHT)
    cx, cy = 160.0, 150.0
    for y in range(h):
        for x in range(w):
            a = math.atan2(y + 0.5 - cy, x + 0.5 - cx)
            if int(math.floor((a + math.pi) / (math.pi / 10.0))) % 2 == 0:
                img[y][x] = DUSK
    # Stadium silhouette along the bottom.
    fill(img, 0, 128, w, 150, MIDNIGHT)
    crowd = crowd_frame(0)
    for y in range(0, 22):
        for x in range(w):
            c = crowd[y + 8][x]
            if c[3] > 0:
                img[128 + y][x] = c
    fill(img, 122, 116, 198, 150, GREEN_DARK)  # clubhouse silhouette
    fill(img, 122, 116, 198, 118, OFFWHITE)
    for wx in range(128, 194, 8):
        fill(img, wx, 124, wx + 4, 129, MIDNIGHT)
        fill(img, wx, 136, wx + 4, 141, MIDNIGHT)
    fill(img, 0, 150, w, 166, GREEN)
    fill(img, 0, 150, w, 151, GREEN_DARK)
    mini_text(img, 154, 155, "483", OFFWHITE)
    fill(img, 0, 166, w, 172, DIRT)
    fill(img, 0, 172, w, h, GREEN_LIGHT)
    # Dotted flight of the ball.
    for i in range(0, 46):
        t = i / 45.0
        x = 20 + 236 * t
        y = 150 - 118 * (4 * t * (1 - t)) - 6 * t
        if i % 3 == 0:
            put(img, int(x), int(y), OFFWHITE)
    return img


def title_logo():
    """"THE" + "CATCH" in the bold pixel font: yellow face, midnight outline, red drop shadow."""
    the = text_bitmap("THE", bold=True)
    catch = text_bitmap("CATCH", bold=True, spacing=2)
    k_big, k_small = 4, 2
    w = len(catch[0]) * k_big + 12
    h = 7 * k_small + 4 + 7 * k_big + 10
    face = new_img(w, h)
    draw_bitmap(face, [r for r in the[:7]], (w - len(the[0]) * k_small) // 2, 2, OFFWHITE, k_small)
    draw_bitmap(face, [r for r in catch[:7]], 6, 7 * k_small + 6, YELLOW, k_big)
    shadow = new_img(w, h)
    for y in range(h):
        for x in range(w):
            if face[y][x][3] > 0:
                put(shadow, x + 2, y + 2, RED)
    img = new_img(w, h)
    blit(img, shadow, 0, 0)
    blit(img, face, 0, 0)
    img = outline(img, MIDNIGHT, diagonal=True)
    # Speed lines under the word (arcade flourish).
    base = 7 * k_small + 6 + 7 * k_big + 3
    for i, (x0, x1) in enumerate(((10, w - 10), (24, w - 24))):
        fill(img, x0, base + i * 2, x1, base + i * 2 + 1, ORANGE)
    return img


# --- UI kit ---------------------------------------------------------------------
def ui_panel(fill_c, border_c, inner_c=None):
    s = 12
    img = new_img(s, s, fill_c)
    for i in range(s):
        for (x, y) in ((i, 0), (i, s - 1), (0, i), (s - 1, i)):
            img[y][x] = border_c
    for (x, y) in ((0, 0), (s - 1, 0), (0, s - 1), (s - 1, s - 1)):
        img[y][x] = CLEAR
    if inner_c is not None:
        for i in range(2, s - 2):
            for (x, y) in ((i, 2), (i, s - 3), (2, i), (s - 3, i)):
                img[y][x] = inner_c
    return img


def ui_button(fill_c, border_c=OFFWHITE, pressed=False):
    """10x10 button with a 1 px drop shadow (bottom/right). Pressed = shifted, no shadow."""
    s = 10
    img = new_img(s, s)
    o = 1 if pressed else 0
    if not pressed:
        fill(img, 1, 1, s, s, MIDNIGHT)
    fill(img, o, o, s - 1 + o, s - 1 + o, border_c)
    fill(img, o + 1, o + 1, s - 2 + o, s - 2 + o, fill_c)
    put(img, o, o, CLEAR)
    put(img, s - 2 + o, o, CLEAR if not pressed else CLEAR)
    put(img, o, s - 2 + o, CLEAR)
    return img


def icon(rows):
    return from_rows(rows)


ICONS = {
    "icon_pause": ["KKKKKKKKK", "KWWKKKWWK", "KWWKKKWWK", "KWWKKKWWK", "KWWKKKWWK", "KWWKKKWWK",
                   "KWWKKKWWK", "KWWKKKWWK", "KKKKKKKKK"],
    "icon_sound_on": ["....KK.....", "...KWK..K..", "KKKWWK.K.K.", "KWWWWK..K.K", "KWWWWK..K.K",
                      "KWWWWK..K.K", "KKKWWK.K.K.", "...KWK..K..", "....KK....."],
    "icon_sound_off": ["....KK.....", "...KWK.....", "KKKWWK.R.R.", "KWWWWK..R..", "KWWWWK.R.R.",
                       "KWWWWK.....", "KKKWWK.....", "...KWK.....", "....KK....."],
    "icon_star": ["....K....", "...KYK...", "KKKKYKKKK", "KYYYYYYYK", ".KYYYYYK.", "..KYYYK..",
                  ".KYYKYYK.", ".KYK.KYK.", ".KK...KK."],
    "icon_star_empty": ["....K....", "...KDK...", "KKKKDKKKK", "KDDDDDDDK", ".KDDDDDK.", "..KDDDK..",
                        ".KDDKDDK.", ".KDK.KDK.", ".KK...KK."],
    "ui_cursor": ["KK....", "KYK...", "KYYK..", "KYYYK.", "KYYK..", "KYK...", "KK...."],
    "icon_ball": ["..KKKK..", ".KWWWWK.", "KWWRWWWK", "KWRWWWWK", "KWWWWRWK", "KWWWRWWK", ".KWWWWK.",
                  "..KKKK.."],
}


def badge(stars, colour):
    """24x20 pennant-shield rank badge; number of stars is the non-colour cue."""
    w, h = 26, 22
    img = new_img(w, h)
    for y in range(h - 4):
        for x in range(2, w - 2):
            img[y][x] = colour
    for i in range(4):  # pointed bottom
        fill(img, 2 + i * 3, h - 4 + i, w - 2 - i * 3, h - 3 + i, colour)
    img = outline(img, MIDNIGHT)
    fill(img, 4, 2, w - 4, 3, OFFWHITE)
    star = from_rows(ICONS["icon_star"])
    positions = {1: [(8, 6)], 2: [(3, 7), (14, 7)], 3: [(1, 8), (8, 4), (16, 8)]}[stars]
    for (x, y) in positions:
        blit(img, star, x, y)
    return img


def circle_img(d, fill_c, border_c, ring_only=False, label=None):
    img = new_img(d, d)
    c = (d - 1) / 2.0
    for y in range(d):
        for x in range(d):
            r = math.hypot(x - c, y - c)
            if r <= d / 2.0 - 0.3:
                if r >= d / 2.0 - 1.6:
                    img[y][x] = border_c
                elif not ring_only:
                    img[y][x] = fill_c
    if label:
        bmp = text_bitmap(label, bold=True)
        bw = len(bmp[0])
        draw_bitmap(img, bmp[:7], (d - bw) // 2, (d - 7) // 2, MIDNIGHT)
    return img


def write_ui():
    ui = os.path.join(SPRITES, "UI")
    write_png(os.path.join(ui, "ui_panel_slice3.png"), ui_panel(MIDNIGHT, OFFWHITE, DUSK))
    write_png(os.path.join(ui, "ui_board_slice3.png"), ui_panel(GREEN_DARK, OFFWHITE, GREEN))
    write_png(os.path.join(ui, "ui_chip_slice3.png"), ui_panel(MIDNIGHT, DUSK))
    write_png(os.path.join(ui, "ui_button_primary_slice3.png"), ui_button(ORANGE))
    write_png(os.path.join(ui, "ui_button_secondary_slice3.png"), ui_button(MIDNIGHT))
    write_png(os.path.join(ui, "ui_button_selected_slice3.png"), ui_button(YELLOW))
    write_png(os.path.join(ui, "ui_button_pressed_slice3.png"), ui_button(OFFWHITE, YELLOW, pressed=True))
    write_png(os.path.join(ui, "ui_button_disabled_slice3.png"), ui_button(DUSK, DUSK))
    write_png(os.path.join(ui, "ui_white.png"), new_img(4, 4, OFFWHITE))
    for name, rows in ICONS.items():
        write_png(os.path.join(ui, name + ".png"), icon(rows))
    write_png(os.path.join(ui, "badge_rookie.png"), badge(1, GREEN_LIGHT))
    write_png(os.path.join(ui, "badge_allstar.png"), badge(2, ORANGE))
    write_png(os.path.join(ui, "badge_legendary.png"), badge(3, RED))
    write_png(os.path.join(ui, "ui_stick_base.png"), circle_img(28, CLEAR, OFFWHITE, ring_only=True))
    write_png(os.path.join(ui, "ui_stick_knob.png"), outline(pad(circle_img(12, OFFWHITE, OFFWHITE), 1)))
    write_png(os.path.join(ui, "ui_action_button.png"), outline(pad(circle_img(22, ORANGE, OFFWHITE, label="A"), 1)))
    title = os.path.join(SPRITES, "Title")
    write_png(os.path.join(title, "title_backdrop.png"), title_backdrop())
    write_png(os.path.join(title, "title_logo.png"), title_logo())


def landing_ring(dashed):
    """32x32 ring (radius 1 world unit at 16 PPU). Dashed = outside the zone, solid = inside:
    the dash pattern is the non-colour cue."""
    size = 32
    img = new_img(size, size)
    c = (size - 1) / 2.0
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c)
            if 14.0 <= d <= 15.6:
                if dashed and int((math.degrees(math.atan2(y - c, x - c)) % 360) // 15) % 2 == 1:
                    continue
                img[y][x] = YELLOW
            elif 12.6 <= d < 14.0 and not dashed:
                img[y][x] = MIDNIGHT
    return img


def write_environment():
    gameplay = os.path.join(SPRITES, "Gameplay")
    write_png(os.path.join(gameplay, "landing_ring_dashed.png"), landing_ring(True))
    write_png(os.path.join(gameplay, "landing_ring_solid.png"), landing_ring(False))
    env = os.path.join(SPRITES, "Environment")
    base = stadium()
    write_png(os.path.join(env, "env_stadium.png"), base)
    crowd = [crowd_frame(i) for i in range(3)]
    for i, c in enumerate(crowd):
        write_png(os.path.join(env, "env_crowd_%02d.png" % i), c)
    flags = [flags_frame(i) for i in range(2)]
    for i, f in enumerate(flags):
        write_png(os.path.join(env, "env_flags_%02d.png" % i), f)
    return base, crowd[0], flags[0]


def gameplay_mockup(base, crowd, flags):
    """Composite preview: stadium + crowd + flags + outfielder + ball (Docs only)."""
    img = [list(r) for r in base]
    blit(img, crowd, 0, CROWD_TOP)
    blit(img, flags, 0, FLAGS_TOP)
    blit(img, render(CLIPS["run_reach"][0]), 190, 96)
    blit(img, from_rows(BALL_FRAMES[0]), 222, 70)
    blit(img, render(CLIPS["idle"][0]), 60, 130)
    write_png(os.path.join(PREVIEW_DIR, "m3_stadium_preview.png"), scale(img, 4))


def main():
    rows = write_character()
    preview_sheet(rows, os.path.join(PREVIEW_DIR, "m3_outfielder_preview.png"))
    base, crowd, flags = write_environment()
    gameplay_mockup(base, crowd, flags)
    write_ui()
    meta = write_font()
    title = [list(r) for r in title_backdrop()]
    logo = title_logo()
    blit(title, logo, (320 - len(logo[0])) // 2, 40)
    write_png(os.path.join(PREVIEW_DIR, "m3_title_preview.png"), scale(title, 4))
    print("character frames:", sum(len(f) for _, f in rows), "| font glyphs:", len(meta["faces"][0]["glyphs"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
