"""
Generates the Milestone 3 sound set for The Say Hey Dash: original chiptune-style
sound effects, crowd beds, result jingles and a short title-music loop.

Everything is synthesised here from oscillators and seeded noise, so the output is
original, owned by the project and reproducible (licensing: see Docs/ThirdPartyLicenses.md).

Usage (from the repository root):
    py -I Tools/AudioGen/generate_audio.py

Output: Assets/_Project/Audio/SFX/*.wav and Assets/_Project/Audio/Music/*.wav
(22,050 Hz, mono, 16-bit). No third-party packages required.
"""
import math
import os
import random
import struct
import sys
import wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Audio")
RATE = 22050


def secs(n):
    return int(n * RATE)


def silence(d):
    return [0.0] * secs(d)


def write(name, samples, folder="SFX", peak=0.85):
    m = max(1e-6, max(abs(s) for s in samples))
    k = peak / m if m > peak else 1.0
    path = os.path.join(OUT, folder, name + ".wav")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s * k)) * 32767)) for s in samples))
    print("wrote", os.path.relpath(path, ROOT), "%.2fs" % (len(samples) / float(RATE)))


# --- oscillators / helpers ------------------------------------------------------
def square(f, t, duty=0.5):
    return 1.0 if (f * t) % 1.0 < duty else -1.0


def triangle(f, t):
    p = (f * t) % 1.0
    return 4.0 * abs(p - 0.5) - 1.0


def env_adsr(i, n, a=0.005, d=0.05, s=0.6, r=0.05):
    t = i / float(RATE)
    total = n / float(RATE)
    if t < a:
        return t / a
    if t < a + d:
        return 1.0 - (1.0 - s) * (t - a) / d
    if t > total - r:
        return s * max(0.0, (total - t) / r)
    return s


def mix(dst, src, at=0, gain=1.0):
    start = secs(at)
    need = start + len(src)
    if len(dst) < need:
        dst.extend([0.0] * (need - len(dst)))
    for i, s in enumerate(src):
        dst[start + i] += s * gain
    return dst


def lowpass(samples, a):
    y, out = 0.0, []
    for s in samples:
        y += a * (s - y)
        out.append(y)
    return out


def tone(f, d, wave_fn="square", duty=0.5, vol=0.5, a=0.004, dcy=0.04, s=0.7, r=0.04, slide=0.0):
    n = secs(d)
    out = []
    for i in range(n):
        t = i / float(RATE)
        ff = f * (1.0 + slide * t / d)
        v = square(ff, t, duty) if wave_fn == "square" else triangle(ff, t)
        out.append(v * vol * env_adsr(i, n, a, dcy, s, r))
    return out


def noise(d, vol, rng, decay=None, attack=0.0):
    n = secs(d)
    out = []
    for i in range(n):
        t = i / float(RATE)
        e = 1.0
        if decay:
            e = math.exp(-t / decay)
        if attack and t < attack:
            e *= t / attack
        out.append((rng.random() * 2 - 1) * vol * e)
    return out


NOTE = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}


def hz(name):
    """'A4' -> 440 Hz; supports '#' (e.g. 'F#4')."""
    letter, rest = name[0], name[1:]
    semis = NOTE[letter]
    if rest.startswith("#"):
        semis += 1
        rest = rest[1:]
    octave = int(rest)
    return 440.0 * 2 ** ((semis + (octave - 4) * 12) / 12.0)


# --- cues ---------------------------------------------------------------------------
def bat_crack(rng):
    out = noise(0.22, 0.9, rng, decay=0.03)
    click = [math.sin(2 * math.pi * 2200 * i / RATE) * math.exp(-i / (RATE * 0.01)) for i in range(secs(0.05))]
    knock = [math.sin(2 * math.pi * 180 * i / RATE) * 0.7 * math.exp(-i / (RATE * 0.05)) for i in range(secs(0.2))]
    mix(out, click, 0, 0.8)
    mix(out, knock, 0, 0.8)
    return out


def crowd_bed(rng, d=4.0):
    raw = noise(d + 0.5, 1.0, rng)
    murmur = lowpass(lowpass(raw, 0.08), 0.12)
    n = secs(d)
    out = []
    for i in range(n):
        t = i / float(RATE)
        wobble = 0.75 + 0.25 * math.sin(2 * math.pi * 0.5 * t) * math.sin(2 * math.pi * 0.23 * t + 1.0)
        out.append(murmur[i] * wobble)
    # Seamless loop: crossfade the tail into the head.
    fade = secs(0.4)
    for i in range(fade):
        k = i / float(fade)
        out[i] = out[i] * k + murmur[n + i] * (1 - k) * 0.75
    return out


def crowd_cheer(rng):
    d = 2.2
    raw = lowpass(noise(d, 1.0, rng), 0.25)
    n = len(raw)
    out = []
    for i in range(n):
        t = i / float(RATE)
        e = min(1.0, t / 0.25) * (1.0 if t < 1.2 else max(0.0, 1 - (t - 1.2) / 1.0))
        out.append(raw[i] * e)
    for k in range(6):  # a few "whoo!" voices
        f = 520 + rng.random() * 300
        v = tone(f, 0.5, "triangle", vol=0.08, a=0.08, dcy=0.2, s=0.6, r=0.2, slide=0.3)
        mix(out, v, 0.15 + k * 0.12)
    return out


def blip(f, d=0.07, duty=0.25, vol=0.5):
    return tone(f, d, "square", duty=duty, vol=vol, a=0.002, dcy=0.02, s=0.6, r=0.02)


def glove_pop(rng):
    out = lowpass(noise(0.12, 1.0, rng, decay=0.02), 0.35)
    thump = [math.sin(2 * math.pi * (140 - 60 * i / secs(0.15)) * i / RATE) * math.exp(-i / (RATE * 0.04))
             for i in range(secs(0.15))]
    mix(out, thump, 0, 1.0)
    return out


def whiff():
    return tone(330, 0.45, "square", duty=0.5, vol=0.4, a=0.005, dcy=0.1, s=0.7, r=0.15, slide=-0.55)


def throw_whoosh(rng):
    d = 0.35
    raw = noise(d, 1.0, rng)
    out, y = [], 0.0
    n = len(raw)
    for i, s in enumerate(raw):
        t = i / float(n)
        a = 0.05 + 0.4 * t
        y += a * (s - y)
        out.append(y * math.sin(math.pi * t) * 1.2)
    return out


def jingle(notes, step, wave_fn="square", vol=0.35, tail=0.3):
    out = []
    t = 0.0
    for name, beats in notes:
        if name != "-":
            mix(out, tone(hz(name), step * beats * 0.95, wave_fn, duty=0.25, vol=vol, r=0.03), t)
            mix(out, tone(hz(name) / 2, step * beats * 0.95, "triangle", vol=vol * 0.8, r=0.03), t)
        t += step * beats
    out.extend([0.0] * secs(tail))
    return out


def success_jingle():
    notes = [("C5", 1), ("E5", 1), ("G5", 1), ("C6", 2), ("-", 0.5), ("G5", 0.5), ("C6", 3)]
    return jingle(notes, 0.11)


def fail_jingle():
    notes = [("G4", 1.5), ("D#4", 1.5), ("C4", 4)]
    return jingle(notes, 0.14, vol=0.3)


def title_loop(rng):
    """Original 4-bar chiptune loop (120 bpm, 8 s), C major pentatonic."""
    beat = 0.5
    melody = [
        ("E5", 1), ("G5", 1), ("A5", 1), ("G5", 0.5), ("E5", 0.5),
        ("D5", 1), ("C5", 1), ("D5", 1), ("E5", 1),
        ("G5", 1), ("A5", 0.5), ("C6", 0.5), ("A5", 1), ("G5", 1),
        ("E5", 1), ("D5", 1), ("C5", 2),
    ]
    bass = ["C3", "C3", "G2", "G2", "A2", "A2", "G2", "G2", "F2", "F2", "C3", "C3", "F2", "G2", "C3", "C3"]
    total = secs(16 * beat)
    out = [0.0] * total
    t = 0.0
    for name, beats in melody:
        mix(out, tone(hz(name), beat * beats * 0.9, "square", duty=0.25, vol=0.22, r=0.05), t)
        t += beat * beats
    for i, name in enumerate(bass):
        mix(out, tone(hz(name), beat * 0.9, "triangle", vol=0.35, r=0.05), i * beat)
    for i in range(32):  # hi-hat on every eighth note
        hat = lowpass(noise(0.03, 0.2 if i % 2 else 0.12, rng, decay=0.01), 0.9)
        mix(out, hat, i * beat / 2)
    return out[:total]


def main():
    rng = random.Random(1954)
    write("sfx_bat_crack", bat_crack(rng))
    write("sfx_crowd_loop", crowd_bed(rng), peak=0.5)
    write("sfx_crowd_cheer", crowd_cheer(rng), peak=0.7)
    write("sfx_ready", blip(880, 0.07))
    write("sfx_now", blip(1320, 0.06) + silence(0.03) + blip(1320, 0.08))
    write("sfx_catch", glove_pop(rng))
    write("sfx_whiff", whiff())
    write("sfx_throw", throw_whoosh(rng))
    write("ui_move", blip(660, 0.035, vol=0.3))
    write("ui_confirm", blip(784, 0.05, vol=0.35) + blip(1175, 0.07, vol=0.35))
    write("ui_back", blip(587, 0.05, vol=0.3) + blip(392, 0.07, vol=0.3))
    write("jingle_success", success_jingle(), "Music")
    write("jingle_fail", fail_jingle(), "Music")
    write("music_title_loop", title_loop(rng), "Music", peak=0.6)
    return 0


if __name__ == "__main__":
    sys.exit(main())
