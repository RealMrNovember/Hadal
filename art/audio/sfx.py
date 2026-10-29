"""HADAL's sound effects, synthesized (no samples).

    python art/audio/sfx.py [names...]

Writes mono OGG files into Assets/_Hadal/Resources/Audio/Sfx, peak-normalized then trimmed by a
per-sound level so taps sit well under rewards and explosions.
"""
import os
import sys

import numpy as np
from scipy import signal

from synth import (RNG, SR, bandpass, brass, celesta, export, highpass, hz, lowpass, midi, noise, ping, reverb_ir,
                   strings, taiko, timpani, tom)

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "_Hadal", "Resources", "Audio", "Sfx")


def t_(n):
    return np.arange(n) / SR


def buf(seconds):
    return np.zeros(int(seconds * SR))


def put(dst, sig, at, gain=1.0):
    i = int(at * SR)
    m = min(len(dst) - i, len(sig))
    if m > 0:
        dst[i:i + m] += sig[:m] * gain
    return dst


def glide(f0, f1, seconds, rate=None, shape="sine"):
    """A tone gliding f0 -> f1 (exponential when rate is given, else linear)."""
    n = int(seconds * SR)
    t = t_(n)
    f = f1 + (f0 - f1) * np.exp(-t * rate) if rate else f0 + (f1 - f0) * t / seconds
    ph = 2 * np.pi * np.cumsum(f) / SR
    if shape == "square":
        return np.tanh(np.sin(ph) * 4) * 0.7
    if shape == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    return np.sin(ph)


def decay(n, rate, attack=0.002):
    t = t_(n)
    return np.minimum(1, t / attack) * np.exp(-t * rate)


def bubble(f=600, seconds=0.12, vel=1.0):
    """One bubble: a sine that chirps upward as it rings out."""
    n = int(seconds * SR)
    t = t_(n)
    f_t = f * (1 + 0.9 * t / seconds)
    ph = 2 * np.pi * np.cumsum(f_t) / SR
    return np.sin(ph) * decay(n, 6 / seconds, 0.003) * vel


def bubbles(dst, start, length, count, lo=300, hi=1400, vel=0.5):
    for _ in range(count):
        at = start + RNG.uniform(0, length)
        f = np.exp(RNG.uniform(np.log(lo), np.log(hi)))
        put(dst, bubble(f, RNG.uniform(0.04, 0.12), RNG.uniform(0.3, 1.0) * vel), at)
    return dst


def whoosh(seconds, lo, hi, vel=1.0, up=True):
    n = int(seconds * SR)
    t = t_(n)
    x = noise(n)
    out = np.zeros(n)
    # Sweep a band through the noise in short blocks.
    block = 512
    for i in range(0, n, block):
        p = i / n if up else 1 - i / n
        c = lo * (hi / lo) ** p
        seg = x[max(0, i - 256):i + block]
        y = bandpass(seg, c * 0.7, c * 1.4, 1)
        out[i:i + block] = y[-len(out[i:i + block]):]
    env = np.sin(np.pi * np.clip(t / seconds, 0, 1)) ** 1.5
    return out * env * vel


def clank(f=420, seconds=0.5, vel=1.0):
    """Struck metal: inharmonic partials."""
    n = int(seconds * SR)
    t = t_(n)
    out = sum(a * np.sin(2 * np.pi * f * r * t) * np.exp(-t * d)
              for r, a, d in ((1, 1, 9), (2.76, 0.6, 12), (5.4, 0.4, 16), (8.93, 0.25, 22)))
    out += 0.4 * highpass(noise(n), 2000) * np.exp(-t * 80)
    return out * np.minimum(1, t / 0.001) * vel


def verb(x, seconds=1.2, wet=0.3, damp=5000):
    ir = reverb_ir(seconds, 0.01, damp)[0]
    w = signal.fftconvolve(x, ir)[: len(x)]
    return x + w * wet * 0.15


def pad_to(x, seconds):
    n = int(seconds * SR)
    return np.concatenate([x, np.zeros(max(0, n - len(x)))])[:n]


def chime(note, seconds=1.0, vel=1.0):
    return celesta(note, seconds * 0.3, vel)[: int(seconds * SR)].astype(np.float64)


# --- Effects --------------------------------------------------------------------------------


def ui_tap():
    n = int(0.09 * SR)
    x = glide(700, 1100, 0.09, rate=60) * decay(n, 55)
    x += 0.25 * highpass(noise(n), 3000) * decay(n, 400)
    return x, -20


def ui_toggle():
    x = buf(0.12)
    put(x, glide(1300, 1300, 0.04) * decay(int(0.04 * SR), 80), 0)
    put(x, glide(1750, 1750, 0.05) * decay(int(0.05 * SR), 70), 0.045)
    return x, -19


def panel_open():
    x = buf(0.35)
    put(x, whoosh(0.28, 350, 2400, 0.8), 0)
    put(x, bubble(500, 0.14, 0.6), 0.05)
    put(x, bubble(820, 0.1, 0.4), 0.11)
    return verb(x, 0.6, 0.4), -19


def panel_close():
    x = buf(0.3)
    put(x, whoosh(0.22, 300, 1800, 0.7, up=False), 0)
    put(x, bubble(380, 0.12, 0.5), 0.07)
    return verb(x, 0.5, 0.3), -21


def reward():
    x = buf(1.4)
    for i, n in enumerate(("C6", "E6", "G6", "C7")):
        put(x, chime(n, 1.2, 0.9), i * 0.07)
    put(x, chime("G7", 0.8, 0.35), 0.3)
    sh = highpass(noise(int(0.9 * SR)), 7000) * decay(int(0.9 * SR), 5, 0.05) * 0.25
    put(x, sh, 0.1)
    return verb(x, 1.2, 0.5), -14


def chest_open():
    x = buf(1.9)
    put(x, taiko(0.7, 1.4).astype(np.float64), 0, 0.6)
    put(x, clank(1600, 0.12, 0.5), 0.02)
    put(x, clank(2100, 0.1, 0.4), 0.12)
    rise = glide(400, 1600, 0.5) * np.sin(np.pi * np.linspace(0, 1, int(0.5 * SR))) * 0.25
    put(x, rise, 0.15)
    for i, n in enumerate(("G5", "C6", "E6", "G6", "C7")):
        put(x, chime(n, 1.2, 0.8), 0.3 + i * 0.06)
    sh = highpass(noise(int(1.2 * SR)), 6000) * decay(int(1.2 * SR), 3, 0.1) * 0.3
    put(x, sh, 0.3)
    bubbles(x, 0.3, 0.6, 10, 600, 2000, 0.25)
    return verb(x, 1.4, 0.5), -13


def collect():
    x = buf(0.5)
    put(x, chime("B6", 0.4, 0.9), 0)
    put(x, chime("E7", 0.4, 0.9), 0.07)
    bubbles(x, 0.0, 0.15, 3, 700, 1500, 0.3)
    return verb(x, 0.6, 0.3), -16


def build_start():
    x = buf(0.9)
    put(x, clank(380, 0.4, 0.9), 0)
    put(x, clank(510, 0.4, 0.7), 0.16)
    n = int(0.35 * SR)
    drill = np.tanh(np.sin(2 * np.pi * 95 * t_(n)) * 3) * (0.6 + 0.4 * np.sin(2 * np.pi * 28 * t_(n)))
    put(x, lowpass(drill, 1800) * decay(n, 6, 0.03) * 0.35, 0.34)
    bubbles(x, 0.3, 0.4, 5, 250, 800, 0.35)
    return verb(x, 0.8, 0.3), -13


def build_complete():
    x = buf(1.6)
    put(x, clank(440, 0.5, 0.9), 0)
    for i, n in enumerate(("C4", "E4", "G4")):
        put(x, brass(midi(n) + 12, 0.12 if i < 2 else 0.7, 0.8).astype(np.float64), 0.12 + i * 0.09, 0.8)
    put(x, chime("C7", 1.0, 0.6), 0.32)
    put(x, chime("G6", 1.0, 0.5), 0.36)
    return verb(x, 1.2, 0.45), -14


def research_complete():
    x = buf(1.4)
    for i, n in enumerate(("A5", "C#6", "E6", "A6", "C#7")):
        blip = glide(hz(n), hz(n), 0.18, shape="tri") * decay(int(0.18 * SR), 18)
        put(x, blip, i * 0.06, 0.6)
    glow = strings(midi("A4"), 0.6, 0.6, attack=0.1, release=0.5).astype(np.float64)
    put(x, glow, 0.3, 0.5)
    put(x, strings(midi("E5"), 0.6, 0.5, attack=0.1, release=0.5).astype(np.float64), 0.3, 0.4)
    put(x, chime("A7", 0.8, 0.4), 0.32)
    return verb(x, 1.3, 0.5), -14


def march_launch():
    x = buf(1.6)
    put(x, brass(midi("G3"), 0.18, 0.85).astype(np.float64), 0, 0.8)
    put(x, brass(midi("C4"), 0.5, 0.9).astype(np.float64), 0.2, 0.9)
    put(x, brass(midi("G4"), 0.5, 0.7).astype(np.float64), 0.2, 0.5)
    rumble = lowpass(noise(int(1.2 * SR)), 260) * np.sin(np.pi * np.linspace(0, 1, int(1.2 * SR))) * 1.8
    put(x, rumble, 0.25)
    bubbles(x, 0.3, 1.0, 26, 200, 1200, 0.4)
    return verb(x, 1.2, 0.4), -14


def sub_travel():
    x = buf(1.8)
    n = int(1.7 * SR)
    t = t_(n)
    prop = lowpass(noise(n), 180) * (0.7 + 0.3 * np.sin(2 * np.pi * 9 * t)) * 2.2
    env = np.minimum(1, t / 0.3) * np.minimum(1, (1.7 - t) / 0.6)
    put(x, prop * env, 0)
    put(x, whoosh(1.4, 200, 900, 0.5), 0.1)
    bubbles(x, 0.1, 1.5, 34, 250, 1600, 0.45)
    return verb(x, 1.0, 0.3), -16


def sonar_ping():
    x = buf(2.6)
    put(x, ping("B5", 2.5, 1.0).astype(np.float64), 0)
    put(x, ping("B5", 2.0, 0.25).astype(np.float64), 0.55)
    return verb(x, 2.2, 0.9, 3500), -13


def teleport():
    x = buf(1.6)
    n = int(0.9 * SR)
    t = t_(n)
    sweep = glide(180, 2200, 0.9) * (0.6 + 0.4 * np.sin(2 * np.pi * 22 * t)) * np.sin(np.pi * t / 0.9) ** 0.7
    put(x, sweep, 0, 0.45)
    put(x, whoosh(0.9, 300, 6000, 0.7), 0)
    shimmer = highpass(noise(int(0.8 * SR)), 5000) * decay(int(0.8 * SR), 4, 0.05) * 0.4
    put(x, shimmer, 0.7)
    put(x, lowpass(noise(int(0.4 * SR)), 300) * decay(int(0.4 * SR), 9) * 1.5, 0.85)
    put(x, chime("E7", 0.7, 0.5), 0.88)
    bubbles(x, 0.8, 0.6, 14, 500, 2200, 0.35)
    return verb(x, 1.4, 0.5), -14


def battle_hit():
    x = buf(0.6)
    n = int(0.5 * SR)
    thump = np.sin(2 * np.pi * np.cumsum(55 + 140 * np.exp(-t_(n) * 30)) / SR) * decay(n, 10)
    put(x, np.tanh(thump * 2), 0)
    put(x, bandpass(noise(n), 800, 6000) * decay(n, 35), 0, 0.8)
    put(x, clank(330, 0.4, 0.5), 0.005)
    return verb(x, 0.6, 0.3), -12


def explosion():
    x = buf(2.4)
    n = int(2.2 * SR)
    t = t_(n)
    boom = np.sin(2 * np.pi * np.cumsum(28 + 70 * np.exp(-t * 7)) / SR) * decay(n, 2.2, 0.004)
    put(x, np.tanh(boom * 2.5), 0)
    body = lowpass(noise(n), 400 + 1600 * np.exp(-0.0)) * decay(n, 3, 0.01)
    put(x, lowpass(body, 1500), 0, 1.2)
    crackle = np.tanh(bandpass(noise(int(0.8 * SR)), 300, 2500) * 3) * decay(int(0.8 * SR), 5, 0.005)
    put(x, crackle, 0.01, 0.5)
    put(x, bandpass(noise(int(0.2 * SR)), 1000, 6000) * decay(int(0.2 * SR), 25), 0, 0.6)
    bubbles(x, 0.15, 1.8, 40, 150, 900, 0.45)
    return verb(x, 1.8, 0.4, 2500), -10


def level_up():
    x = buf(1.8)
    for i, n in enumerate(("C5", "E5", "G5")):
        put(x, brass(midi(n), 0.11, 0.85).astype(np.float64), i * 0.1, 0.7)
    for n in ("C5", "E5", "G5", "C6"):
        put(x, brass(midi(n), 0.8, 0.8, release=0.5).astype(np.float64), 0.3, 0.4)
    for i, n in enumerate(("C6", "E6", "G6", "C7", "E7")):
        put(x, chime(n, 1.0, 0.6), 0.3 + i * 0.05)
    put(x, timpani("C2", 0.9).astype(np.float64), 0.3, 0.7)
    return verb(x, 1.4, 0.5), -13


def vip_claim():
    x = buf(2.0)
    swell_n = int(0.6 * SR)
    put(x, highpass(noise(swell_n), 4000) * np.linspace(0, 1, swell_n) ** 2 * 0.3, 0)
    for i, n in enumerate(("E6", "G#6", "B6", "E7", "G#7")):
        put(x, chime(n, 1.4, 0.8), 0.55 + i * 0.045)
    for n in ("E4", "B4", "E5", "G#5"):
        put(x, strings(midi(n), 0.9, 0.5, attack=0.05, release=0.8).astype(np.float64), 0.55, 0.45)
    put(x, clank(2600, 0.6, 0.25), 0.55)
    return verb(x, 1.6, 0.6), -13


def error():
    x = buf(0.36)
    put(x, glide(240, 240, 0.12, shape="square") * decay(int(0.12 * SR), 10), 0)
    put(x, glide(180, 175, 0.18, shape="square") * decay(int(0.18 * SR), 9), 0.13)
    return lowpass(x, 2500), -15


def speedup():
    x = buf(0.7)
    put(x, whoosh(0.45, 600, 5000, 0.8), 0)
    put(x, glide(500, 1800, 0.35, shape="tri") * decay(int(0.35 * SR), 6, 0.02), 0.05, 0.35)
    put(x, chime("A6", 0.4, 0.5), 0.35)
    return verb(x, 0.6, 0.3), -17


EFFECTS = {f.__name__: f for f in (ui_tap, ui_toggle, panel_open, panel_close, reward, chest_open, collect, build_start,
                                   build_complete, research_complete, march_launch, sub_travel, sonar_ping, teleport,
                                   battle_hit, explosion, level_up, vip_claim, error, speedup)}


def presence(x):
    """Loudest 100 ms of the sound in dB, weighted like a phone speaker hears it (little below 150 Hz)."""
    y = highpass(x, 150) ** 2
    w = int(0.1 * SR)
    rms = np.sqrt(np.convolve(y, np.ones(w) / w, "same").max())
    return 20 * np.log10(rms + 1e-12)


def main(names):
    for name in names or EFFECTS:
        x, level = EFFECTS[name]()
        x = highpass(x, 40)
        # Fade the last 20 ms so nothing clicks.
        f = int(0.02 * SR)
        x[-f:] *= np.linspace(1, 0, f)
        x = x * 10 ** ((level - presence(x)) / 20)
        # Soft ceiling at -1 dBFS for the few transients that poke above it.
        c = 10 ** (-1 / 20)
        x = np.tanh(x / c) * c
        lufs, peak, sec = export(x, os.path.join(OUT, f"{name}.ogg"), quality=5)
        print(f"{name:18s} {sec:4.2f}s  {lufs:6.1f} LUFS  peak {peak:5.1f} dBTP")


if __name__ == "__main__":
    main(sys.argv[1:])
