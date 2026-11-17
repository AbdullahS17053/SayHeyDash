"""
Generates the Milestone 1 placeholder / art-direction study sprites for The Say Hey Dash.

PROTOTYPE ART - NOT FINAL. These files exist so the M1 technical proof of concept
and the art-direction sheet use the proposed palette and pixel grid. Final pixel art
is produced in Milestone 3.

Usage (from the repository root):
    python -I Tools/ArtGen/generate_placeholder_art.py

No third-party packages required (minimal PNG writer below).
"""
import os
import struct
import sys
import zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Sprites", "Prototype")

# --- Palette ---------------------------------------------------------------
# Core palette from the GDD (pending client approval).
MIDNIGHT = (0x17, 0x15, 0x2F, 255)
YELLOW = (0xF6, 0xCE, 0x4B, 255)
ORANGE = (0xF3, 0x8B, 0x37, 255)
GREEN = (0x3C, 0x76, 0x59, 255)
OFFWHITE = (0xF5, 0xF1, 0xD9, 255)
RED = (0xD9, 0x4D, 0x58, 255)
# Proposed extension colours (needed for skin, leather, dirt, depth). Pending approval.
DUSK = (0x2E, 0x2A, 0x52, 255)
GREEN_DARK = (0x2C, 0x5A, 0x44, 255)
GREEN_LIGHT = (0x46, 0x86, 0x6A, 255)
DIRT = (0x9C, 0x6B, 0x3E, 255)
SKIN = (0x7A, 0x4A, 0x2E, 255)
LEATHER = (0xA8, 0x5F, 0x2C, 255)
SHADOW = (0x17, 0x15, 0x2F, 140)
CLEAR = (0, 0, 0, 0)


def write_png(path, width, height, pixels):
    """pixels: list of rows, each a list of RGBA tuples."""
    raw = bytearray()
    for row in pixels:
        raw.append(0)
        for r, g, b, a in row:
            raw += bytes((r, g, b, a))

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)
    print("wrote", os.path.relpath(path, ROOT))


def from_charmap(rows, key, width):
    out = []
    for r in rows:
        r = r.ljust(width, ".")[:width]
        out.append([key[c] for c in r])
    return out


# --- Outfielder (32x32, facing right, idle) ---------------------------------
OUTFIELDER = [
    "................................",
    "................................",
    "............KKKKKK..............",
    "...........KWWWWWWK.............",
    "..........KWWWWWWWWK............",
    "..........KKKKKKKKKKKKK.........",
    "..........KSSSSSSSSK............",
    "..........KSSSSSKSSK............",
    "..........KSSSSSSSSK............",
    "...........KSSSSSSK.............",
    "............KKSSKK..............",
    ".........KKKWWWWWWKKK...........",
    "........KWWWWWWWWWWWWK..........",
    "........KWWWWWWWWWWWWWK.........",
    ".......KWWKWWWWWWWWKWWK.........",
    ".......KWWKWWWWWWWWKWWK.........",
    ".......KSSKWWWWWWWWKLLLK........",
    ".......KSSKWWWWWWWWKLLLLK.......",
    "........KK.KRRRRRRRK.KLLK.......",
    "...........KWWWWWWWK..KK........",
    "...........KWWWKWWWK............",
    "...........KWWWKWWWK............",
    "...........KWWWKWWWK............",
    "...........KWWWKWWWK............",
    "...........KRRRKRRRK............",
    "...........KRRRKRRRK............",
    "...........KKKKKKKKK............",
    "..........KKKKK.KKKKK...........",
    "................................",
    "................................",
    "................................",
    "................................",
]
OUTFIELDER_KEY = {
    ".": CLEAR, "K": MIDNIGHT, "W": OFFWHITE, "S": SKIN, "L": LEATHER, "R": RED,
}

BALL = [
    "..KKKK..",
    ".KWWWWK.",
    "KWWRWWWK",
    "KWRWWWWK",
    "KWWWWRWK",
    "KWWWRWWK",
    ".KWWWWK.",
    "..KKKK..",
]
BALL_KEY = {".": CLEAR, "K": MIDNIGHT, "W": OFFWHITE, "R": RED}

SHADOW_MAP = [
    "..SSSS..",
    ".SSSSSS.",
    ".SSSSSS.",
    "..SSSS..",
]
SHADOW_KEY = {".": CLEAR, "S": SHADOW}

# 3x5 glyphs for the scoreboard / wall markers.
GLYPHS = {
    "0": ["###", "#.#", "#.#", "#.#", "###"],
    "2": ["###", "..#", "###", "#..", "###"],
    "3": ["###", "..#", "###", "..#", "###"],
    "4": ["#.#", "#.#", "###", "..#", "..#"],
    "8": ["###", "#.#", "###", "#.#", "###"],
    "-": ["...", "...", "###", "...", "..."],
    " ": ["...", "...", "...", "...", "..."],
}


def draw_text(px, x, y, text, color):
    for ch in text:
        g = GLYPHS[ch]
        for gy, line in enumerate(g):
            for gx, c in enumerate(line):
                if c == "#":
                    px[y + gy][x + gx] = color
        x += 4


def fill(px, x0, y0, x1, y1, color):
    for y in range(max(0, y0), min(len(px), y1)):
        for x in range(max(0, x0), min(len(px[0]), x1)):
            px[y][x] = color


def field_background():
    w, h = 320, 180
    px = [[DUSK for _ in range(w)] for _ in range(h)]

    # Bleachers with a sparse, low-texture crowd pattern.
    fill(px, 0, 26, w, 70, MIDNIGHT)
    crowd = [ORANGE, OFFWHITE, YELLOW, RED]
    for y in range(29, 68, 4):
        for x in range((y // 4) % 3, w, 5):
            px[y][x] = crowd[(x * 7 + y) % len(crowd)]
    # Bleacher roof line.
    fill(px, 0, 26, w, 28, GREEN_DARK)

    # Centre-field clubhouse + scoreboard (no team marks).
    fill(px, 128, 14, 192, 70, GREEN_DARK)
    fill(px, 128, 14, 192, 16, OFFWHITE)
    for wx in range(132, 190, 8):
        fill(px, wx, 46, wx + 4, 52, MIDNIGHT)
    fill(px, 138, 20, 182, 40, MIDNIGHT)
    fill(px, 138, 20, 182, 21, YELLOW)
    fill(px, 138, 39, 182, 40, YELLOW)
    draw_text(px, 147, 24, "2-2", YELLOW)   # score tied 2-2
    draw_text(px, 167, 24, "8", ORANGE)     # 8th inning
    draw_text(px, 143, 32, "0 0", OFFWHITE)

    # Outfield wall.
    fill(px, 0, 70, w, 88, GREEN)
    fill(px, 0, 70, w, 72, GREEN_DARK)
    fill(px, 0, 86, w, 88, GREEN_DARK)
    draw_text(px, 154, 76, "483", OFFWHITE)  # Polo Grounds centre-field depth marker

    # Warning track.
    fill(px, 0, 88, w, 98, DIRT)

    # Grass stripes.
    for y in range(98, h):
        stripe = ((y - 98) // 12) % 2
        for x in range(w):
            px[y][x] = GREEN if stripe == 0 else GREEN_LIGHT
    return w, h, px


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    write_png(os.path.join(OUT_DIR, "proto_outfielder_idle.png"), 32, 32,
              from_charmap(OUTFIELDER, OUTFIELDER_KEY, 32))
    write_png(os.path.join(OUT_DIR, "proto_ball.png"), 8, 8, from_charmap(BALL, BALL_KEY, 8))
    write_png(os.path.join(OUT_DIR, "proto_ball_shadow.png"), 8, 4,
              from_charmap(SHADOW_MAP, SHADOW_KEY, 8))
    w, h, px = field_background()
    write_png(os.path.join(OUT_DIR, "proto_field_background.png"), w, h, px)
    return 0


if __name__ == "__main__":
    sys.exit(main())
