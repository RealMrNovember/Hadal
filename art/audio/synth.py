"""Small offline synthesizer for HADAL's music and sound effects.

Everything here is generated from oscillators and noise (no samples), so the audio is ours to ship.
Instruments return mono float32 arrays at SR; Mix places them in stereo with a shared reverb and can
wrap the tail of a loop back onto its start so music repeats without a seam.
"""
import json
import os
import re
import subprocess
import tempfile
import wave

import numpy as np
from scipy import signal

SR = 44100
RNG = np.random.default_rng(7)

_NOTE = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(name):
    """'C#5' / 'Bb3' -> MIDI number (A4 = 69)."""
    m = re.fullmatch(r"([A-G])([#b]?)(-?\d)", name)
    if not m:
        raise ValueError(name)
    acc = {"#": 1, "b": -1, "": 0}[m.group(2)]
    return 12 * (int(m.group(3)) + 1) + _NOTE[m.group(1)] + acc


def hz(note):
    n = midi(note) if isinstance(note, str) else note
    return 440.0 * 2 ** ((n - 69) / 12)


def _t(n):
    return np.arange(n, dtype=np.float64) / SR


def envelope(n, attack, hold, release, curve=4.0):
    """Linear attack, flat sustain until `hold` seconds, exponential-ish release."""
    t = _t(n)
    env = np.minimum(1.0, t / max(attack, 1e-4))
    rel = np.clip((t - hold) / max(release, 1e-4), 0, 1)
    env *= np.where(t > hold, (1 - rel) ** curve, 1.0)
    return env


def _phase(f, n, vib_rate=0.0, vib_depth=0.0, vib_delay=0.25, glide=None):
    t = _t(n)
    inst = np.full(n, f, dtype=np.float64)
    if glide is not None:
        inst = glide(t, f)
    if vib_depth:
        depth = vib_depth * np.clip((t - vib_delay) / 0.6, 0, 1)
        inst = inst * (1 + depth * np.sin(2 * np.pi * vib_rate * t + RNG.uniform(0, 6.28)))
    return 2 * np.pi * np.cumsum(inst) / SR


def _harmonics(ph, f, weights, limit=0.45):
    """Sum of harmonics; weights is a list of floats or (n,) arrays per harmonic."""
    out = np.zeros(len(ph))
    for k, w in enumerate(weights, 1):
        if f * k > SR * limit:
            break
        out += w * np.sin(k * ph + RNG.uniform(0, 6.28))
    return out


def lowpass(x, cutoff, order=2):
    sos = signal.butter(order, min(cutoff, SR * 0.45), "low", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def highpass(x, cutoff, order=2):
    sos = signal.butter(order, cutoff, "high", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def bandpass(x, lo, hi, order=2):
    sos = signal.butter(order, [lo, min(hi, SR * 0.45)], "band", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def noise(n):
    return RNG.standard_normal(n)


# --- Pitched instruments --------------------------------------------------------------------


def piano(note, dur, vel=0.8):
    f = hz(note)
    n = int((dur + 2.2) * SR)
    t = _t(n)
    out = np.zeros(n)
    scale = (f / 261.6) ** 0.45
    for k in range(1, 18):
        fk = k * f * np.sqrt(1 + 0.0004 * k * k)
        if fk > SR * 0.45:
            break
        amp = (1 / k ** 1.25) * (0.55 + 0.45 * vel) ** (k * 0.4)
        out += amp * np.exp(-t * (0.9 + 0.55 * k) * scale) * np.sin(2 * np.pi * fk * t + RNG.uniform(0, 6.28))
    out += 0.04 * vel * lowpass(noise(n), 3000) * np.exp(-t * 60)
    env = envelope(n, 0.003, dur, 0.35, 2)
    return (out * env * vel).astype(np.float32)


def harp(note, dur=1.5, vel=0.8):
    f = hz(note)
    n = int((max(dur, 1.0) + 1.8) * SR)
    t = _t(n)
    out = np.zeros(n)
    for k in range(1, 14):
        if k * f > SR * 0.45:
            break
        amp = abs(np.sin(np.pi * k * 0.18)) / k ** 1.1
        out += amp * np.exp(-t * (1.1 + 0.9 * k)) * np.sin(2 * np.pi * k * f * t)
    env = np.minimum(1, t / 0.002)
    return (out * env * vel).astype(np.float32)


def celesta(note, dur=1.0, vel=0.7):
    f = hz(note)
    n = int((dur + 2.0) * SR)
    t = _t(n)
    index = 1.6 * np.exp(-t * 7)
    mod = np.sin(2 * np.pi * f * 4.0 * t)
    out = np.sin(2 * np.pi * f * t + index * mod) * np.exp(-t * 2.2)
    out += 0.25 * np.sin(2 * np.pi * f * 2 * t) * np.exp(-t * 4)
    env = np.minimum(1, t / 0.002) * envelope(n, 0.001, dur + 0.6, 1.2, 2)
    return (out * env * vel).astype(np.float32)


def pad(note, dur, vel=0.6, cutoff=1800, attack=1.0, release=2.0):
    f = hz(note)
    n = int((dur + release + 0.2) * SR)
    out = np.zeros(n)
    for cents in (-7, -2.5, 2.5, 7):
        fv = f * 2 ** (cents / 1200)
        ph = _phase(fv, n, vib_rate=0.3 + RNG.uniform(0, 0.2), vib_depth=0.0015, vib_delay=0)
        out += _harmonics(ph, fv, [1 / k for k in range(1, 22)])
    out = lowpass(out, cutoff)
    env = envelope(n, attack, dur, release, 2)
    return (out * env * vel * 0.18).astype(np.float32)


def strings(note, dur, vel=0.7, attack=0.25, release=0.5, cutoff=3800, voices=3, vibrato=0.0045):
    f = hz(note)
    n = int((dur + release + 0.1) * SR)
    out = np.zeros(n)
    detunes = (-6, 0, 6)[:voices] if voices <= 3 else np.linspace(-8, 8, voices)
    for cents in detunes:
        fv = f * 2 ** (cents / 1200)
        ph = _phase(fv, n, vib_rate=5.0 + RNG.uniform(-0.4, 0.4), vib_depth=vibrato, vib_delay=0.2)
        out += _harmonics(ph, fv, [1 / k ** 1.05 for k in range(1, 26)])
    out = highpass(lowpass(out, cutoff), 90)
    env = envelope(n, attack, dur, release, 2)
    return (out * env * vel * 0.2).astype(np.float32)


def brass(note, dur, vel=0.8, attack=0.05, release=0.25):
    f = hz(note)
    n = int((dur + release + 0.1) * SR)
    t = _t(n)
    bright = vel * (0.62 + 0.38 * np.exp(-t * 3.5)) * np.minimum(1, t / attack)
    roll = 1.25 - 0.95 * bright
    out = np.zeros(n)
    for cents in (-4, 4):
        fv = f * 2 ** (cents / 1200)
        ph = _phase(fv, n, vib_rate=5.2, vib_depth=0.003, vib_delay=0.45)
        weights = [(1 / k) * np.exp(-(k - 1) * roll * 0.38) for k in range(1, 22)]
        out += _harmonics(ph, fv, weights)
    out = lowpass(out, 5000)
    env = envelope(n, attack, dur, release, 2)
    return (out * env * vel * 0.32).astype(np.float32)


_AH = ((730, 1.0, 130), (1090, 0.55, 150), (2440, 0.22, 220))
_OO = ((330, 1.0, 110), (870, 0.35, 140), (2250, 0.12, 200))


def choir(note, dur, vel=0.6, vowel="ah", attack=0.6, release=1.0):
    f = hz(note)
    n = int((dur + release + 0.1) * SR)
    formants = _AH if vowel == "ah" else _OO
    out = np.zeros(n)
    for cents in (-9, -3, 3, 9):
        fv = f * 2 ** (cents / 1200)
        ph = _phase(fv, n, vib_rate=4.8 + RNG.uniform(-0.5, 0.5), vib_depth=0.005, vib_delay=0.3)
        weights = []
        for k in range(1, 40):
            fk = k * fv
            w = 0.08 / k ** 2 + sum(a * np.exp(-((fk - c) / bw) ** 2) for c, a, bw in formants)
            weights.append(w)
        out += _harmonics(ph, fv, weights)
    out += 0.05 * bandpass(noise(n), 600, 3000) * np.max(np.abs(out))
    env = envelope(n, attack, dur, release, 2)
    return (out * env * vel * 0.28).astype(np.float32)


def flute(note, dur, vel=0.7, attack=0.08, release=0.3):
    f = hz(note)
    n = int((dur + release + 0.1) * SR)
    ph = _phase(f, n, vib_rate=5.4, vib_depth=0.006, vib_delay=0.25)
    out = np.sin(ph) + 0.22 * np.sin(2 * ph) + 0.08 * np.sin(3 * ph) + 0.03 * np.sin(4 * ph)
    out += 0.06 * bandpass(noise(n), f * 1.5, f * 4)
    env = envelope(n, attack, dur, release, 2)
    return (out * env * vel * 0.5).astype(np.float32)


def bass(note, dur, vel=0.8, release=0.15):
    f = hz(note)
    n = int((dur + release + 0.05) * SR)
    ph = _phase(f, n)
    out = np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.12 * np.sin(3 * ph)
    out = np.tanh(out * 1.4) / 1.2
    env = envelope(n, 0.008, dur, release, 2)
    return (out * env * vel * 0.55).astype(np.float32)


def synth_pluck(note, dur=0.3, vel=0.7, decay=5.0, cutoff=4000):
    f = hz(note)
    n = int((dur + 0.8) * SR)
    t = _t(n)
    roll = 0.15 + 2.2 * (1 - np.exp(-t * 9))
    ph = _phase(f, n)
    out = _harmonics(ph, f, [(1 / k) * np.exp(-(k - 1) * roll * 0.5) for k in range(1, 24)])
    out = lowpass(out, cutoff)
    env = np.minimum(1, t / 0.003) * np.exp(-t * decay) * envelope(n, 0.001, dur, 0.2, 2)
    return (out * env * vel * 0.5).astype(np.float32)


def ping(note, length=2.5, vel=0.7, drop=0.985):
    """Sonar ping: a pure tone that sags a hair in pitch and rings out."""
    f = hz(note)
    n = int(length * SR)
    ph = _phase(f, n, glide=lambda t, f0: f0 * (drop + (1 - drop) * np.exp(-t * 6)))
    t = _t(n)
    out = (np.sin(ph) + 0.18 * np.sin(2.01 * ph)) * np.exp(-t * 2.4) * np.minimum(1, t / 0.002)
    return (out * vel).astype(np.float32)


# --- Percussion -----------------------------------------------------------------------------


def _sweep(n, f0, f1, rate):
    t = _t(n)
    inst = f1 + (f0 - f1) * np.exp(-t * rate)
    return 2 * np.pi * np.cumsum(inst) / SR


def kick(vel=1.0):
    n = int(0.6 * SR)
    t = _t(n)
    out = np.sin(_sweep(n, 130, 46, 28)) * np.exp(-t * 7)
    out += 0.3 * lowpass(noise(n), 4000) * np.exp(-t * 90)
    return (np.tanh(out * 1.5) * vel).astype(np.float32)


def taiko(vel=1.0, pitch=1.0):
    n = int(1.4 * SR)
    t = _t(n)
    out = np.sin(_sweep(n, 150 * pitch, 62 * pitch, 18)) * np.exp(-t * 4.2)
    out += 0.45 * lowpass(noise(n), 900) * np.exp(-t * 14)
    out += 0.15 * bandpass(noise(n), 1500, 5000) * np.exp(-t * 60)
    return (np.tanh(out * 1.6) * vel).astype(np.float32)


def snare(vel=1.0):
    n = int(0.45 * SR)
    t = _t(n)
    out = 0.75 * bandpass(noise(n), 1200, 8000) * np.exp(-t * 16)
    out += 0.55 * np.sin(_sweep(n, 260, 185, 30)) * np.exp(-t * 22)
    return (out * vel).astype(np.float32)


def tom(note="G2", vel=1.0):
    f = hz(note)
    n = int(0.9 * SR)
    t = _t(n)
    out = np.sin(_sweep(n, f * 1.7, f, 14)) * np.exp(-t * 6.5)
    out += 0.2 * lowpass(noise(n), 1500) * np.exp(-t * 30)
    return (np.tanh(out * 1.3) * vel).astype(np.float32)


def hat(vel=0.5, open_=False):
    n = int((0.5 if open_ else 0.12) * SR)
    t = _t(n)
    out = highpass(noise(n), 7000, 4) * np.exp(-t * (9 if open_ else 55))
    return (out * vel * 0.6).astype(np.float32)


def shaker(vel=0.4):
    n = int(0.16 * SR)
    t = _t(n)
    env = np.minimum(1, t / 0.02) * np.exp(-t * 30)
    return (bandpass(noise(n), 4000, 11000) * env * vel).astype(np.float32)


def cymbal(vel=0.8, length=3.5):
    n = int(length * SR)
    t = _t(n)
    metal = sum(np.sign(np.sin(2 * np.pi * f * t)) for f in (296, 457, 593, 777, 1003, 1301)) / 6
    out = highpass(noise(n) * 0.7 + metal * 0.5, 3200, 2) * np.exp(-t * 2.2)
    out *= np.minimum(1, t / 0.003)
    return (out * vel * 0.3).astype(np.float32)


def swell(length=2.0, vel=0.7):
    """Reversed cymbal: rises into the downbeat."""
    return cymbal(vel, length + 0.5)[::-1][-int(length * SR):].copy()


def timpani(note="D2", vel=1.0):
    f = hz(note)
    n = int(2.5 * SR)
    t = _t(n)
    out = np.zeros(n)
    for ratio, amp, dec in ((1.0, 1.0, 1.8), (1.504, 0.5, 2.6), (1.742, 0.3, 3.2), (2.0, 0.25, 3.5)):
        out += amp * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t * dec)
    out += 0.3 * lowpass(noise(n), 1200) * np.exp(-t * 40)
    return (out * np.minimum(1, t / 0.002) * vel * 0.6).astype(np.float32)


def timpani_roll(note, length, vel=0.8, crescendo=True):
    n = int((length + 2.5) * SR)
    out = np.zeros(n, dtype=np.float32)
    hits = int(length * 14)
    for i in range(hits):
        g = (0.25 + 0.75 * i / max(1, hits - 1)) if crescendo else 1.0
        s = timpani(note, vel * g * RNG.uniform(0.85, 1.0))
        start = int(i / 14 * SR)
        end = min(n, start + len(s))
        out[start:end] += s[: end - start] * 0.55
    return out


# --- Mixing ---------------------------------------------------------------------------------


def reverb_ir(seconds=2.8, predelay=0.02, damp=4500, width=1.0):
    n = int(seconds * SR)
    t = _t(n)
    decay = np.exp(-t * 6.9 / seconds)
    chans = []
    for _ in range(2):
        x = noise(n) * decay
        # Highs die faster than lows.
        bright = lowpass(x, damp) * 0.7 + x * 0.3 * np.exp(-t * 6)
        chans.append(bright)
    left, right = chans
    mid = (left + right) / 2
    left, right = mid + (left - mid) * width, mid + (right - mid) * width
    pre = np.zeros(int(predelay * SR))
    ir = np.stack([np.concatenate([pre, left]), np.concatenate([pre, right])])
    return ir / np.sqrt(np.sum(ir ** 2) / 2)


class Mix:
    def __init__(self, seconds, tail=5.0):
        self.length = int(round(seconds * SR))
        self.n = self.length + int(tail * SR)
        self.dry = np.zeros((2, self.n))
        self.wet = np.zeros((2, self.n))

    def add(self, sig, at, pan=0.0, gain=1.0, rev=0.2, jitter=0.0):
        if jitter:
            at += RNG.uniform(-jitter, jitter)
        i = max(0, int(round(at * SR)))
        if i >= self.n:
            return
        seg = sig[: self.n - i].astype(np.float64) * gain
        angle = (np.clip(pan, -1, 1) + 1) * np.pi / 4
        lr = np.array([np.cos(angle), np.sin(angle)])[:, None] * np.sqrt(2)
        self.dry[:, i:i + len(seg)] += lr * seg * (1 - rev * 0.5)
        if rev:
            self.wet[:, i:i + len(seg)] += lr * seg * rev

    def render(self, ir, loop=True, wet_gain=0.9):
        out = self.dry.copy()
        for c in range(2):
            out[c] += signal.fftconvolve(self.wet[c], ir[c])[: self.n] * wet_gain
        out = highpass(out, 28)
        if not loop:
            # Trim trailing silence below -70 dB.
            level = np.max(np.abs(out), axis=0)
            last = np.nonzero(level > 10 ** (-70 / 20) * level.max())[0]
            return out[:, : (last[-1] + 1) if len(last) else self.n]
        body = out[:, : self.length].copy()
        tail = out[:, self.length:]
        # Wrap the tail (reverb, held notes) onto the start so the loop has no seam.
        while tail.shape[1] > 0:
            m = min(self.length, tail.shape[1])
            body[:, :m] += tail[:, :m]
            tail = tail[:, m:]
        return body


def compress(x, threshold_db=-18.0, ratio=2.5, attack=0.02, release=0.25, loop=True):
    """Gentle bus compressor. For loops it runs over two passes so the start sees the end's state."""
    src = np.concatenate([x, x], axis=1) if loop else x
    level = np.max(np.abs(src), axis=0)
    a_att = np.exp(-1 / (attack * SR))
    a_rel = np.exp(-1 / (release * SR))
    # One-pole follower with separate attack/release: approximate with two filters and a max.
    fast = signal.lfilter([1 - a_att], [1, -a_att], level)
    slow = signal.lfilter([1 - a_rel], [1, -a_rel], level)
    env = np.maximum(fast, slow) + 1e-9
    thr = 10 ** (threshold_db / 20)
    gain = np.where(env > thr, (env / thr) ** (1 / ratio - 1), 1.0)
    y = src * gain
    return y[:, x.shape[1]:] if loop else y


def limit(x, ceiling_db=-1.0):
    c = 10 ** (ceiling_db / 20)
    return np.tanh(x / c) * c


def _write_wav(path, x):
    x = np.atleast_2d(x)
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(x.shape[0])
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.T.tobytes())


def loudness(x):
    """Integrated loudness (LUFS) and true peak via ffmpeg's EBU R128 meter."""
    with tempfile.TemporaryDirectory() as tmp:
        path = os.path.join(tmp, "m.wav")
        _write_wav(path, x)
        proc = subprocess.run(
            ["ffmpeg", "-hide_banner", "-nostats", "-i", path, "-af", "loudnorm=print_format=json", "-f", "null", "-"],
            capture_output=True, text=True)
        err = proc.stderr
        data = json.loads(err[err.rindex("{"):err.rindex("}") + 1])
        return float(data["input_i"]), float(data["input_tp"])


def master(x, lufs, ceiling_db=-1.0, loop=True, comp=True):
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.5
    if comp:
        x = compress(x, threshold_db=-14.0, loop=loop)
        x = x / (np.max(np.abs(x)) + 1e-9) * 0.5
    for _ in range(3):
        measured, _peak = loudness(limit(x, ceiling_db))
        x = x * 10 ** ((lufs - measured) / 20)
    x = limit(x, ceiling_db)
    return x


def export(x, path, quality=4):
    """Writes an OGG Vorbis file (stereo or mono) and returns (LUFS, true peak, seconds)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    x = np.atleast_2d(x)
    with tempfile.TemporaryDirectory() as tmp:
        wav = os.path.join(tmp, "o.wav")
        _write_wav(wav, x)
        subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis",
                        "-q:a", str(quality), path], check=True)
    lufs, peak = loudness(x)
    return lufs, peak, x.shape[1] / SR
