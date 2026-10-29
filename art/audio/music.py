"""HADAL's soundtrack, composed in code (original melodies, all synthesized).

    python art/audio/music.py [base world battle menu victory defeat]

Writes OGG Vorbis loops into Assets/_Hadal/Resources/Audio/Music. Loops wrap their own reverb tail onto
their start, so they repeat without a seam; stings are one-shots.
"""
import os
import sys

from synth import (RNG, Mix, SR, bass, brass, celesta, choir, cymbal, export, flute, harp, hat, master, midi, pad,
                   piano, ping, reverb_ir, shaker, snare, strings, swell, synth_pluck, taiko, timpani, timpani_roll, tom)

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "_Hadal", "Resources", "Audio", "Music")

# --- Notation helpers -----------------------------------------------------------------------

_QUALITY = {"m": (0, 3, 7), "": (0, 4, 7), "sus4": (0, 5, 7), "maj7": (0, 4, 7, 11), "m7": (0, 3, 7, 10)}


def chord(name, octave=3):
    """'Dm' -> MIDI notes of the root-position chord with the root in `octave`."""
    root = name[0]
    rest = name[1:]
    acc = 0
    if rest[:1] in ("#", "b"):
        acc = 1 if rest[0] == "#" else -1
        rest = rest[1:]
    base = midi(f"{root}{octave}") + acc
    return [base + i for i in _QUALITY[rest]]


def third(name):
    rest = name[1:].lstrip("#b")
    return 3 if rest.startswith("m") else 5 if rest == "sus4" else 4


def melody(text):
    """'D5:1.5 E5:.5 r:1' -> [(beat offset, beats, midi or None)]."""
    notes, at = [], 0.0
    for token in text.split():
        name, beats = token.split(":")
        beats = float(beats)
        notes.append((at, beats, None if name == "r" else midi(name)))
        at += beats
    return notes


def hv(v, spread=0.08):
    """Humanized velocity."""
    return v * (1 + RNG.uniform(-spread, spread))


class Song:
    def __init__(self, bpm, bars, tail=6.0):
        self.beat = 60.0 / bpm
        self.bar = self.beat * 4
        self.bars = bars
        self.mix = Mix(self.bar * bars, tail)

    def at(self, bar, beat=0.0):
        return bar * self.bar + beat * self.beat

    def play(self, inst, notes, bar, gain=1.0, pan=0.0, rev=0.25, transpose=0, legato=1.0, **kw):
        for offset, beats, note in notes:
            if note is None:
                continue
            sig = inst(note + transpose, beats * self.beat * legato, **kw)
            self.mix.add(sig, self.at(bar, offset), pan, gain, rev, jitter=0.006)


# --- Base theme: "Hearth of the Abyss" --------------------------------------------------------
# D minor, 84 bpm, 32 bars. Warm piano and harp under a singing string melody; the third section
# lifts it an octave with horns and a soft taiko heartbeat, the fourth hands it to the cellos.


def base_theme():
    s = Song(84, 32)
    prog = ("Dm Bb F C Dm Bb Gm A " * 2 + "Bb C Dm F Gm C F A " + "Dm Bb F C Gm Bb Asus4 A").split()
    mel_b = [
        "D5:1.5 E5:.5 F5:1 A5:1", "G5:2 F5:1 D5:1", "C5:1.5 D5:.5 F5:1 A5:1", "G5:3 E5:1",
        "D5:1.5 E5:.5 F5:1 A5:1", "Bb5:1.5 A5:.5 G5:1 F5:1", "G5:1 F5:1 E5:1 D5:1", "E5:3 C#5:1"]
    mel_c = [
        "F5:1 Bb5:2 A5:1", "G5:1 C6:2 Bb5:.5 A5:.5", "A5:3 F5:1", "C6:2 A5:1 F5:1",
        "Bb5:1.5 A5:.5 G5:1 D5:1", "E5:1 G5:1 C6:1 Bb5:1", "A5:2 G5:1 F5:1", "E5:2 C#5:1 E5:1"]
    mel_d = [
        "D4:1.5 E4:.5 F4:1 A4:1", "G4:2 F4:1 D4:1", "C4:1.5 D4:.5 F4:1 A4:1", "G4:4",
        "Bb4:2 A4:1 G4:1", "F4:2 D4:2", "D4:2 E4:2", "C#4:3 E4:1"]
    sparkle = {1: "A5:.5 D6:.5 E6:2", 3: "G5:.5 C6:.5 E6:2", 5: "F5:.5 Bb5:.5 D6:2", 7: "E5:.5 A5:.5 C#6:2"}

    for bar, name in enumerate(prog):
        section = bar // 8
        notes = chord(name, 3)
        # Pad: the chord with an added octave root, spread wide.
        for i, n in enumerate(notes + [notes[0] + 12]):
            s.mix.add(pad(n, s.bar + 0.1, 0.5 if section != 2 else 0.6, cutoff=1500), s.at(bar), (-0.6, 0.6, -0.3, 0.3)[i], 0.9, 0.45)

        # Piano: broken chord in eighths (root, fifth, octave, tenth, twelfth, tenth, octave, fifth).
        r = notes[0] - 12 if notes[0] >= midi("E3") else notes[0]
        pattern = [r, r + 7, r + 12, r + 12 + third(name), r + 19, r + 12 + third(name), r + 12, r + 7]
        pv = {0: 0.42, 1: 0.36, 2: 0.3, 3: 0.4}[section]
        for i, n in enumerate(pattern):
            s.mix.add(piano(n, s.beat * 0.9, hv(pv if i % 4 else pv * 1.15)), s.at(bar, i * 0.5), -0.25, 0.8, 0.3, 0.006)

        # Bass from bar 4: whole notes, then halves in the fuller sections.
        if bar >= 4:
            root = chord(name, 2)[0]
            if section in (1, 2):
                for half in (0, 2):
                    s.mix.add(bass(root, s.beat * 1.9, hv(0.55)), s.at(bar, half), 0, 0.8, 0.08)
            else:
                s.mix.add(bass(root, s.bar * 0.95, hv(0.45)), s.at(bar), 0, 0.8, 0.08)

        # Harp: rising quarter notes from section B on.
        if section >= 1:
            up = chord(name, 4)
            for i, n in enumerate(up + [up[0] + 12]):
                s.mix.add(harp(n, 1.0, hv(0.35)), s.at(bar, i), 0.45, 0.7, 0.35, 0.004)

        # Celesta sparkles in the intro.
        if section == 0 and bar in sparkle:
            s.play(celesta, melody(sparkle[bar]), bar, 0.5, 0.55, 0.5)

        # Soft taiko heartbeat and shaker in section C.
        if section == 2:
            s.mix.add(taiko(hv(0.5), 0.9), s.at(bar), 0, 0.55, 0.25)
            s.mix.add(taiko(hv(0.35), 0.9), s.at(bar, 2), 0, 0.5, 0.25)
            for i in range(8):
                s.mix.add(shaker(hv(0.25 if i % 2 else 0.35)), s.at(bar, i * 0.5), 0.3, 0.6, 0.15, 0.004)
            # Horns hold the chord's third under the melody.
            horn = chord(name, 4)[1] - 12
            s.mix.add(brass(horn, s.bar * 0.95, 0.42, attack=0.25, release=0.6), s.at(bar), -0.35, 0.55, 0.45)

    for i, line in enumerate(mel_b):
        s.play(strings, melody(line), 8 + i, 0.85, -0.1, 0.4, attack=0.18)
        s.play(flute, melody(line), 8 + i, 0.25, 0.15, 0.4, vel=0.5)
    for i, line in enumerate(mel_c):
        s.play(strings, melody(line), 16 + i, 0.95, -0.1, 0.45, attack=0.15, voices=3)
        s.play(strings, melody(line), 16 + i, 0.5, 0.2, 0.45, transpose=-12, attack=0.2)
    for i, line in enumerate(mel_d):
        s.play(strings, melody(line), 24 + i, 0.9, 0.0, 0.4, attack=0.22, cutoff=2600)

    # Lift into section C, settle out of it.
    s.mix.add(timpani_roll("D2", s.beat * 2, 0.5), s.at(15, 2), 0, 0.7, 0.3)
    s.mix.add(swell(s.beat * 2, 0.4), s.at(15, 2), 0, 0.6, 0.4)
    s.mix.add(cymbal(0.45), s.at(16), 0.2, 0.6, 0.4)
    s.mix.add(timpani("D2", 0.5), s.at(24), 0, 0.6, 0.3)
    s.mix.add(timpani_roll("A1", s.beat * 2, 0.35), s.at(31, 2), 0, 0.5, 0.3)

    return s.mix.render(reverb_ir(3.0, damp=4200)), -18.0


# --- World theme: "Open Waters" ---------------------------------------------------------------
# E dorian/minor, 96 bpm, 32 bars. Ping-pong synth pulse, sonar pings in the distance, a horn melody
# that the strings take up an octave higher at the end.


def world_theme():
    s = Song(96, 32)
    prog = ("Em C G D Em C Am B " + "Em C G D Am C Em B " + "C D Em Em C D B B " + "Em C G D Am C Em B").split()
    mel_b = ["E4:1.5 F#4:.5 G4:1 B4:1", "C5:2 B4:1 G4:1", "D5:1.5 B4:.5 G4:1 D4:1", "F#4:3 A4:1",
             "C5:1.5 B4:.5 A4:1 E4:1", "G4:2 E5:2", "D5:1 B4:1 G4:1 E4:1", "F#4:4"]
    mel_c = ["E5:2 G5:2", "F#5:2 A5:1 F#5:1", "G5:3 E5:1", "B4:4",
             "E5:1 G5:1 C6:2", "A5:2 F#5:1 D5:1", "D#5:2 F#5:2", "B4:4"]

    for bar, name in enumerate(prog):
        section = bar // 8
        notes = chord(name, 3)
        for i, n in enumerate(notes):
            s.mix.add(pad(n + 12 * (i == 0), s.bar + 0.1, 0.45, cutoff=1300, attack=1.4), s.at(bar), (-0.7, 0.7, 0)[i], 0.85, 0.5)

        # Ping-pong pulse in eighths.
        root = chord(name, 4)[0] - 12 if chord(name, 4)[0] > midi("A4") else chord(name, 4)[0]
        steps = [0, 7, 12, third(name) + 12, 12, 7, 12, 7]
        pv = 0.32 if section == 0 else 0.4
        for i, st in enumerate(steps):
            s.mix.add(synth_pluck(root + st, 0.18, hv(pv), decay=7, cutoff=2600 + 600 * section),
                      s.at(bar, i * 0.5), -0.55 if i % 2 else 0.55, 0.6, 0.35, 0.003)

        # Sub bass in a dotted rhythm.
        low = chord(name, 2)[0]
        s.mix.add(bass(low, s.beat * 1.4, hv(0.5)), s.at(bar), 0, 0.85, 0.05)
        s.mix.add(bass(low, s.beat * 2.3, hv(0.42)), s.at(bar, 1.5), 0, 0.85, 0.05)

        # Distant sonar every four bars.
        if bar % 4 == 0:
            s.mix.add(ping("B5", 3.0, 0.32), s.at(bar, 0.02), RNG.uniform(-0.6, 0.6), 0.6, 0.85)
            s.mix.add(ping("B5", 3.0, 0.12), s.at(bar, 1.0), RNG.uniform(-0.8, 0.8), 0.6, 0.95)

        # Percussion grows with the sections.
        if section >= 1:
            for beat, v in ((0, 0.55), (1.5, 0.35), (2, 0.45), (3.5, 0.3)):
                s.mix.add(tom("E2", hv(v)), s.at(bar, beat), -0.1, 0.55, 0.25, 0.004)
            for i in range(8):
                s.mix.add(hat(hv(0.22 if i % 2 else 0.3)), s.at(bar, i * 0.5), 0.35, 0.55, 0.15, 0.003)
        if section >= 2:
            s.mix.add(taiko(hv(0.55), 0.85), s.at(bar), 0, 0.6, 0.25)
            s.mix.add(taiko(hv(0.4), 1.0), s.at(bar, 2), 0, 0.5, 0.25)
        if section == 3:
            s.mix.add(snare(hv(0.3)), s.at(bar, 1), 0.05, 0.4, 0.3)
            s.mix.add(snare(hv(0.3)), s.at(bar, 3), 0.05, 0.4, 0.3)

        # Choir "oo" over section C, celesta arpeggios answer it.
        if section == 2:
            for i, n in enumerate(chord(name, 4)):
                s.mix.add(choir(n, s.bar * 0.98, 0.42, "oo", attack=0.8), s.at(bar), (-0.4, 0.4, 0)[i], 0.6, 0.5)
            arp = chord(name, 5)
            for i, n in enumerate(arp + [arp[0] + 12, arp[2], arp[1]]):
                s.mix.add(celesta(n, 0.4, hv(0.3)), s.at(bar, 2 + i * 0.5 * 2 / 3), 0.6, 0.45, 0.55)

    for i, line in enumerate(mel_b):
        s.play(brass, melody(line), 8 + i, 0.75, -0.15, 0.45, vel=0.55, attack=0.12, release=0.4)
    for i, line in enumerate(mel_c):
        s.play(flute, melody(line), 16 + i, 0.5, 0.2, 0.5, vel=0.55)
    for i, line in enumerate(mel_b):
        s.play(strings, melody(line), 24 + i, 0.85, -0.1, 0.45, transpose=12, attack=0.18)
        s.play(brass, melody(line), 24 + i, 0.5, 0.25, 0.45, vel=0.5, attack=0.15, release=0.4)

    s.mix.add(swell(s.beat * 2, 0.4), s.at(15, 2), 0, 0.6, 0.4)
    s.mix.add(cymbal(0.4), s.at(16), -0.2, 0.55, 0.45)
    s.mix.add(swell(s.beat * 2, 0.4), s.at(23, 2), 0, 0.6, 0.4)
    s.mix.add(cymbal(0.4), s.at(24), 0.2, 0.55, 0.45)

    return s.mix.render(reverb_ir(3.4, damp=3600)), -18.0


# --- Battle theme: "Pressure Breach" ---------------------------------------------------------
# C minor, 138 bpm, 32 bars. Driving string ostinato, taiko and toms, brass stabs and a heroic brass
# line; a choir section; the strings take the climax.


def battle_theme():
    s = Song(138, 32, tail=4.0)
    prog = ("Cm Ab Eb Bb Cm Ab Eb Bb " + "Cm Ab Fm G Cm Ab Fm G " + "Ab Bb Cm Cm Ab Bb G G " + "Cm Ab Eb Bb Fm Ab G G").split()
    mel_b = ["C5:1 Eb5:.5 G5:1.5 G5:1", "Ab5:2 G5:1 Eb5:1", "F5:1.5 Eb5:.5 D5:1 C5:1", "D5:3 B4:1",
             "C5:1 Eb5:.5 G5:1.5 C6:1", "Bb5:1.5 Ab5:.5 G5:1 Eb5:1", "F5:1 Ab5:1 G5:1 F5:1", "G5:4"]
    mel_c = ["Eb5:4", "F5:4", "G5:3 Eb5:1", "C5:4", "Eb5:2 Ab5:2", "F5:2 D5:2", "B4:2 D5:2", "G5:4"]
    mel_d = ["G5:2 C6:2", "Eb6:2 C6:1 Ab5:1", "Bb5:3 G5:1", "F5:2 Bb5:2",
             "Ab5:1.5 G5:.5 F5:1 C5:1", "Eb5:2 Ab5:2", "G5:2 B5:2", "D6:2 B5:1 G5:1"]

    for bar, name in enumerate(prog):
        section = bar // 8
        root3 = chord(name, 3)[0]
        if root3 > midi("F#3"):
            root3 -= 12
        th = third(name)

        # String ostinato in sixteenths, accents on a 3-3-2 grid.
        steps = [0, 0, 12, 0, 0, 7, 12, 7, 0, 0, 12, 0, th, 7, 12, 7]
        for i, st in enumerate(steps):
            accent = i % 8 in (0, 3, 6)
            s.mix.add(strings(root3 + st, s.beat * 0.2, hv(0.62 if accent else 0.42), attack=0.006, release=0.09,
                              voices=2, cutoff=4500, vibrato=0),
                      s.at(bar, i * 0.25), -0.3 if i % 2 else 0.3, 0.75, 0.2, 0.003)

        # Drums.
        for beat in (0, 0.75, 2):
            s.mix.add(taiko(hv(0.85), 0.8), s.at(bar, beat), 0, 0.75, 0.2, 0.003)
        for beat in (1, 3):
            s.mix.add(snare(hv(0.6)), s.at(bar, beat), 0.05, 0.6, 0.25)
            s.mix.add(taiko(hv(0.5), 1.3), s.at(bar, beat), -0.1, 0.5, 0.25)
        for i in range(8):
            s.mix.add(hat(hv(0.3 if i % 2 else 0.4)), s.at(bar, i * 0.5), 0.4, 0.5, 0.1, 0.003)
        if bar % 8 == 7:
            for i, n in enumerate(("D3", "C3", "A2", "G2", "F2", "E2", "D2", "C2")):
                s.mix.add(tom(n, hv(0.7)), s.at(bar, 2 + i * 0.25), (i - 3.5) / 5, 0.6, 0.25)

        # Brass stabs (power chords) and bass.
        power = [root3, root3 + 7, root3 + 12]
        stabs = (0,) if section in (0, 2) else (0, 1.5)
        for beat in stabs:
            for n in power:
                s.mix.add(brass(n, s.beat * 0.7, hv(0.75), attack=0.02), s.at(bar, beat), -0.2, 0.45, 0.3)
        if section >= 1:
            for i in range(8):
                s.mix.add(bass(root3 - 12, s.beat * 0.4, hv(0.55 if i % 2 else 0.65)), s.at(bar, i * 0.5), 0, 0.8, 0.05)

        # Choir section.
        if section == 2:
            for i, n in enumerate(chord(name, 4)):
                s.mix.add(choir(n, s.bar * 0.98, 0.55, "ah", attack=0.35), s.at(bar), (-0.5, 0.5, 0)[i], 0.65, 0.45)
            if bar % 2 == 0:
                s.mix.add(timpani(chord(name, 2)[0], 0.8), s.at(bar), 0, 0.7, 0.3)

        if bar % 8 == 0:
            s.mix.add(cymbal(0.6), s.at(bar), 0.25, 0.6, 0.35)

    for i, line in enumerate(mel_b):
        s.play(brass, melody(line), 8 + i, 0.85, 0.0, 0.35, vel=0.85)
        s.play(brass, melody(line), 8 + i, 0.4, 0.3, 0.35, transpose=-12, vel=0.75)
    for i, line in enumerate(mel_c):
        s.play(brass, melody(line), 16 + i, 0.6, -0.2, 0.4, transpose=-12, vel=0.7, attack=0.15, release=0.4)
    for i, line in enumerate(mel_d):
        s.play(strings, melody(line), 24 + i, 0.95, 0.0, 0.4, attack=0.06, voices=3)
        s.play(brass, melody(line), 24 + i, 0.55, 0.2, 0.35, transpose=-12, vel=0.8)

    s.mix.add(swell(s.beat * 2, 0.5), s.at(7, 2), 0, 0.6, 0.3)
    s.mix.add(swell(s.beat * 2, 0.5), s.at(23, 2), 0, 0.6, 0.3)

    return s.mix.render(reverb_ir(2.2, damp=5000)), -16.0


# --- Menu / event loop: "Lantern Below" ------------------------------------------------------
# The base theme's melody, slowed and dimmed: celesta and harp over a dark pad and soft choir.


def menu_theme():
    s = Song(66, 16)
    prog = "Dm Bb F C Gm Bb Dm A Dm Bb F C Gm Bb Asus4 A".split()
    mel = ["D5:1.5 E5:.5 F5:1 A5:1", "G5:2 F5:1 D5:1", "C5:1.5 D5:.5 F5:1 A5:1", "G5:3 E5:1",
           "D5:1.5 E5:.5 F5:1 A5:1", "Bb5:1.5 A5:.5 G5:1 F5:1", "G5:1 F5:1 E5:1 D5:1", "E5:3 C#5:1"]

    for bar, name in enumerate(prog):
        notes = chord(name, 3)
        for i, n in enumerate(notes):
            s.mix.add(pad(n, s.bar + 0.1, 0.5, cutoff=1000, attack=1.8, release=2.5), s.at(bar), (-0.6, 0.6, 0)[i], 0.9, 0.55)
        s.mix.add(strings(chord(name, 2)[0], s.bar * 0.98, 0.4, attack=0.8, release=1.2, cutoff=1200), s.at(bar), 0, 0.7, 0.4)
        up = chord(name, 4)
        for i, n in enumerate(up + [up[0] + 12]):
            s.mix.add(harp(n, 1.5, hv(0.3)), s.at(bar, i), 0.4, 0.65, 0.45, 0.004)
        if bar >= 8:
            for i, n in enumerate(chord(name, 4)):
                s.mix.add(choir(n, s.bar * 0.98, 0.32, "oo", attack=1.2, release=1.8), s.at(bar), (-0.5, 0.5, 0)[i], 0.55, 0.55)
        if bar % 4 == 2:
            s.mix.add(ping("A5", 3.0, 0.22), s.at(bar, 2), RNG.uniform(-0.7, 0.7), 0.6, 0.9)

    for i, line in enumerate(mel):
        s.play(celesta, melody(line), 8 + i, 0.7, -0.15, 0.55, vel=0.55)
    for bar in (1, 3, 5):
        s.play(celesta, melody("A5:.5 D6:.5 E6:2"), bar, 0.4, 0.5, 0.6)

    return s.mix.render(reverb_ir(3.8, damp=3200)), -19.0


# --- Stings ---------------------------------------------------------------------------------


def victory_sting():
    m = Mix(5.0, 3.0)
    b = 0.16
    for i, n in enumerate(("G4", "C5", "E5")):
        m.add(brass(midi(n), b * 0.9, 0.85), i * b, -0.1, 0.8, 0.3)
    hit = 3 * b
    m.add(brass(midi("G5"), 2.4, 0.9, release=0.8), hit, 0, 0.8, 0.35)
    for n, p in (("C4", -0.4), ("E4", 0.4), ("G4", 0)):
        m.add(brass(midi(n), 2.4, 0.75, release=0.8), hit, p, 0.55, 0.35)
    for n, p in (("C5", -0.5), ("E5", 0.5), ("G5", -0.2), ("C6", 0.2)):
        m.add(strings(midi(n), 2.6, 0.7, attack=0.05, release=1.0), hit, p, 0.6, 0.4)
    m.add(timpani("C2", 0.8), 0, 0, 0.7, 0.3)
    m.add(timpani("C2", 1.0), hit, 0, 0.8, 0.3)
    m.add(taiko(0.9, 0.8), hit, 0, 0.6, 0.3)
    m.add(cymbal(0.75, 4.0), hit, 0.2, 0.7, 0.4)
    for i, n in enumerate(("C6", "E6", "G6", "C7")):
        m.add(celesta(midi(n), 0.6, 0.45), hit + 0.12 + i * 0.09, 0.5, 0.5, 0.5)
    return m.render(reverb_ir(2.6), loop=False), -15.0


def defeat_sting():
    m = Mix(5.5, 3.0)
    b = 0.42
    for i, n in enumerate(("G3", "F3", "Eb3", "D3")):
        m.add(strings(midi(n), b * 1.05, 0.7, attack=0.08, release=0.3, cutoff=2400), i * b, 0, 0.8, 0.35)
        m.add(brass(midi(n) - 12, b * 1.05, 0.55, attack=0.08), i * b, -0.2, 0.4, 0.35)
    hit = 4 * b
    for n, p in (("C3", 0), ("Eb3", -0.4), ("G3", 0.4)):
        m.add(strings(midi(n), 2.8, 0.6, attack=0.15, release=1.2, cutoff=1800), hit, p, 0.7, 0.45)
    for n, p in (("C4", -0.3), ("Eb4", 0.3)):
        m.add(choir(midi(n), 2.6, 0.45, "oo", attack=0.4), hit, p, 0.55, 0.5)
    m.add(pad(midi("C2"), 3.0, 0.6, cutoff=600, attack=0.3), hit, 0, 0.8, 0.3)
    m.add(timpani("C2", 1.0), hit, 0, 0.85, 0.35)
    m.add(taiko(0.8, 0.6), hit, 0, 0.6, 0.35)
    return m.render(reverb_ir(3.2, damp=2500), loop=False), -16.0


TRACKS = {"base": base_theme, "world": world_theme, "battle": battle_theme, "menu": menu_theme,
          "victory": victory_sting, "defeat": defeat_sting}


def main(names):
    for name in names or TRACKS:
        audio, lufs = TRACKS[name]()
        loop = name not in ("victory", "defeat")
        audio = master(audio, lufs, loop=loop)
        stats = export(audio, os.path.join(OUT, f"{name}.ogg"), quality=4)
        print(f"{name:8s} {stats[2]:6.1f}s  {stats[0]:6.1f} LUFS  peak {stats[1]:5.1f} dBTP")


if __name__ == "__main__":
    main(sys.argv[1:])
