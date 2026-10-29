import pathlib
import wave

import numpy as np

SR = 32000
OUT = pathlib.Path(__file__).resolve().parents[2] / "assets" / "music"
RNG = np.random.default_rng(64)

NOTE_INDEX = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def midi(name):
    base = NOTE_INDEX[name[0]]
    rest = name[1:]
    while rest and rest[0] in "b#":
        base += -1 if rest[0] == "b" else 1
        rest = rest[1:]
    return base + 12 * (int(rest) + 1)


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def parse(line):
    events = []
    for token in line.split():
        name, beats = token.split(":")
        events.append((None if name == "r" else midi(name), float(beats)))
    return events


def env_adsr(n, attack, decay, sustain, release_start, release):
    t = np.arange(n) / SR
    e = np.where(t < attack, t / max(attack, 1e-4), sustain + (1 - sustain) * np.exp(-(t - attack) / max(decay, 1e-4)))
    tail = t > release_start
    e[tail] *= np.exp(-(t[tail] - release_start) / max(release, 1e-4))
    return e


def additive(freq_curve, weights_fn, n):
    phase = 2 * np.pi * np.cumsum(freq_curve) / SR
    t = np.arange(n) / SR
    out = np.zeros(n)
    nyquist = SR / 2 - 1000
    for k, w in weights_fn(t):
        if freq_curve.max() * k < nyquist:
            out += w * np.sin(k * phase)
    return out


def epiano(f, dur, vel):
    n = int((dur + 1.2) * SR)
    t = np.arange(n) / SR
    index = 0.4 + 2.2 * np.exp(-t / 0.25)
    mod = index * np.sin(2 * np.pi * f * t)
    tine = 0.25 * np.exp(-t / 0.02) * np.sin(2 * np.pi * f * 14 * t)
    body = np.sin(2 * np.pi * f * t + mod) + tine
    return vel * body * env_adsr(n, 0.004, 0.9, 0.0, dur, 0.08)


def pad(f, dur, vel):
    n = int((dur + 0.8) * SR)
    out = np.zeros(n)
    for detune in (-0.006, 0.0, 0.0065):
        curve = np.full(n, f * (1 + detune))
        out += additive(curve, lambda t: [(k, 1 / k ** 1.6) for k in range(1, 10)], n)
    return vel * out / 3 * env_adsr(n, 0.45, 1.0, 1.0, dur, 0.5)


def flute(f, dur, vel):
    n = int((dur + 0.4) * SR)
    t = np.arange(n) / SR
    vibrato = 1 + 0.005 * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.25) / 0.3, 0, 1)
    curve = f * vibrato
    tone = additive(curve, lambda _: [(1, 1.0), (2, 0.18), (3, 0.06)], n)
    breath = 0.04 * RNG.standard_normal(n) * np.exp(-t / 0.08)
    return vel * (tone + breath) * env_adsr(n, 0.06, 1.0, 1.0, dur, 0.18)


def brass(f, dur, vel):
    n = int((dur + 0.25) * SR)
    t = np.arange(n) / SR
    scoop = 1 - 0.03 * np.exp(-t / 0.03)
    curve = f * scoop
    bright = 0.55 + 0.45 * np.clip(t / 0.05, 0, 1) * (0.75 + 0.25 * np.exp(-t / 0.3))
    tone = additive(curve, lambda tt: [(k, (1 / k) * np.exp(-k * (1.15 - bright) * 0.9)) for k in range(1, 16)], n)
    return vel * 0.8 * tone * env_adsr(n, 0.018, 0.4, 0.75, dur, 0.07)


def slap_bass(f, dur, vel):
    n = int((dur + 0.15) * SR)
    t = np.arange(n) / SR
    curve = np.full(n, f)
    tone = additive(curve, lambda tt: [(k, (1 / k) * np.exp(-tt * k * 9)) if k > 1 else (1, np.ones_like(tt)) for k in range(1, 9)], n)
    return vel * tone * env_adsr(n, 0.003, 0.35, 0.35, dur * 0.85, 0.04)


def soft_bass(f, dur, vel):
    n = int((dur + 0.5) * SR)
    t = np.arange(n) / SR
    tone = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(4 * np.pi * f * t)
    return vel * tone * env_adsr(n, 0.04, 1.5, 0.7, dur, 0.3)


def kick(vel):
    n = int(0.35 * SR)
    t = np.arange(n) / SR
    f = 48 + 110 * np.exp(-t / 0.035)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.18)
    click = 0.3 * RNG.standard_normal(n) * np.exp(-t / 0.003)
    return vel * (body + click)


def snare(vel, length=0.16):
    n = int(0.3 * SR)
    t = np.arange(n) / SR
    noise = np.convolve(np.diff(RNG.standard_normal(n + 1)), np.ones(4) / 4, mode="same")
    tone = np.sin(2 * np.pi * 185 * t) * np.exp(-t / 0.05)
    return vel * (0.7 * noise * np.exp(-t / length) + 0.6 * tone)


def hat(vel, length=0.035):
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    noise = np.diff(np.diff(RNG.standard_normal(n + 2))) * 0.3
    return vel * noise * np.exp(-t / length)


def shaker(vel):
    n = int(0.12 * SR)
    t = np.arange(n) / SR
    noise = np.diff(RNG.standard_normal(n + 1)) * 0.4
    return vel * noise * np.clip(t / 0.015, 0, 1) * np.exp(-t / 0.04)


class Track:
    def __init__(self, bpm, bars, tail_seconds):
        self.beat = 60.0 / bpm
        self.length = int(round(bars * 4 * self.beat * SR))
        self.buffer = np.zeros(self.length + int(tail_seconds * SR) + SR * 3)

    def add(self, beat_pos, signal):
        start = int(round(beat_pos * self.beat * SR))
        end = min(start + len(signal), len(self.buffer))
        self.buffer[start:end] += signal[: end - start]

    def melody(self, start_beat, events, instrument, vel, transpose=0):
        pos = start_beat
        for note, beats in events:
            if note is not None:
                self.add(pos, instrument(hz(note + transpose), beats * self.beat * 0.95, vel))
            pos += beats
        return pos

    def render(self, wet, decay, predelay):
        ir_len = int(decay * 1.6 * SR)
        t = np.arange(ir_len) / SR
        ir = RNG.standard_normal(ir_len) * np.exp(-t * 6.9 / decay)
        ir = np.convolve(ir, np.ones(12) / 12, mode="same")
        ir[: int(predelay * SR)] = 0
        ir /= np.sqrt(np.sum(ir ** 2))
        size = 1 << int(np.ceil(np.log2(len(self.buffer) + ir_len)))
        wet_signal = np.fft.irfft(np.fft.rfft(self.buffer, size) * np.fft.rfft(ir, size), size)[: len(self.buffer)]
        mix = self.buffer + wet * wet_signal
        loop = mix[: self.length].copy()
        overflow = mix[self.length:]
        for offset in range(0, len(overflow), self.length):
            chunk = overflow[offset: offset + self.length]
            loop[: len(chunk)] += chunk
        loop = np.tanh(loop / np.percentile(np.abs(loop), 99.9) * 0.8)
        return loop / np.max(np.abs(loop)) * 0.89


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    data = (samples * 32767).astype("<i2").tobytes()
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)
    print(f"{path.name}: {len(samples) / SR:.1f} s, {len(data) / 1e6:.1f} MB")


def notes(text):
    return [midi(n) for n in text.split()]


def menu_theme():
    bars = [
        ("Db2", "Db3 F3 Ab3 C4 Eb4 F4 Ab4 C5 Eb5", "F3 Ab3 C4 Eb4"),
        ("F2", "F3 Ab3 C4 Eb4 F4 Ab4 C5 Eb5 F5", "Ab3 C4 Eb4 F4"),
        ("Gb2", "Gb3 Bb3 Db4 F4 Gb4 Bb4 Db5 F5 Gb5", "Bb3 Db4 F4 Gb4"),
        ("Ab2", "Ab3 Db4 Eb4 Gb4 Ab4 Db5 Eb5 Gb5 Ab5", "Db4 Eb4 Gb4 Ab4"),
        ("Db2", "Db3 F3 Ab3 C4 Eb4 F4 Ab4 C5 Eb5", "F3 Ab3 C4 Eb4"),
        ("Bb1", "Bb2 Db3 F3 Ab3 C4 Db4 F4 Ab4 C5", "Db4 F4 Ab4 C5"),
        ("Eb2", "Eb3 Gb3 Bb3 Db4 F4 Gb4 Bb4 Db5 F5", "Gb3 Bb3 Db4 F4"),
        ("Ab2", "Ab3 C4 Eb4 F4 Gb4 Ab4 C5 Eb5 F5", "C4 Eb4 F4 Gb4"),
        ("Gb2", "Gb3 Bb3 Db4 F4 Gb4 Bb4 Db5 F5 Gb5", "Bb3 Db4 F4 Gb4"),
        ("F2", "F3 Ab3 C4 Eb4 F4 Ab4 C5 Eb5 F5", "Ab3 C4 Eb4 F4"),
        ("Eb2", "Eb3 Gb3 Bb3 Db4 F4 Gb4 Bb4 Db5 F5", "Gb3 Bb3 Db4 F4"),
        ("Ab2", "Ab3 C4 Eb4 Gb4 Ab4 C5 Eb5 Gb5 Ab5", "C4 Eb4 Gb4 Ab4"),
        ("Gb2", "Gb3 Bb3 C4 Db4 F4 Gb4 Bb4 C5 Db5", "Bb3 C4 Db4 F4"),
        ("F2", "F3 Ab3 C4 Eb4 F4 Ab4 C5 Eb5 F5", "Ab3 C4 Eb4 F4"),
        ("Eb2", "Eb3 Gb3 Bb3 Db4 F4 Gb4 Bb4 Db5 F5", "Gb3 Bb3 Db4 F4"),
        ("Ab2", "Ab3 Db4 Eb4 Gb4 Ab4 Db5 Eb5 Gb5 Ab5", "Db4 Eb4 Gb4 Ab4"),
    ]
    split_bars = {13: ("Bb1", "Bb2 Db3 F3 Ab3 C4 Db4 F4 Ab4 C5", "Db4 F4 Ab4 Bb4")}
    track = Track(bpm=76, bars=16, tail_seconds=4)
    arp_order = list(range(9)) + list(range(7, 0, -1))
    for bar, (root, arp, chord) in enumerate(bars):
        start = bar * 4
        halves = [(root, arp, chord, 0, 4)]
        if bar in split_bars:
            halves = [(root, arp, chord, 0, 2), (*split_bars[bar], 2, 2)]
        for h_root, h_arp, h_chord, offset, beats in halves:
            arp_notes = notes(h_arp)
            order = arp_order if beats == 4 else [0, 2, 4, 6, 8, 6, 4, 2]
            for i, idx in enumerate(order):
                vel = 0.16 if i % 4 == 0 else 0.11
                track.add(start + offset + i * 0.25, epiano(hz(arp_notes[idx] + 12), 0.3 * track.beat, vel))
            for m in notes(h_chord):
                track.add(start + offset, pad(hz(m), beats * track.beat, 0.05))
            r = midi(h_root)
            track.add(start + offset, soft_bass(hz(r), beats * 0.5 * track.beat, 0.32))
            track.add(start + offset + beats * 0.5, soft_bass(hz(r + 7), beats * 0.5 * track.beat, 0.22))
        for eighth in range(8):
            track.add(start + eighth * 0.5, shaker(0.03 if eighth % 2 else 0.05))
        if bar >= 8:
            track.add(start, kick(0.18))
            track.add(start + 2.5, kick(0.12))

    melody_a = parse(
        "F5:3 Eb5:.5 Db5:.5 C5:2 Ab4:1 C5:1 Db5:1.5 Bb4:.5 F5:2 Eb5:3 r:1 "
        "Ab5:2 F5:1 Gb5:1 F5:2 Db5:1 C5:1 Bb4:1 Db5:1 Gb5:2 F5:3 r:1")
    melody_b = parse(
        "Bb5:2 Ab5:1 F5:1 Ab5:2 Eb5:2 Gb5:1.5 F5:.5 Eb5:1 Db5:1 Eb5:4 "
        "C6:2 Bb5:1 F5:1 Ab5:1.5 F5:.5 Db5:2 Eb5:1 F5:1 Gb5:1 Bb5:1 Ab5:3 r:1")
    track.melody(0, melody_a, flute, 0.22)
    track.melody(32, melody_b, flute, 0.22)
    track.melody(32, melody_b, epiano, 0.07, transpose=-12)
    return track.render(wet=0.55, decay=2.6, predelay=0.03)


def level_theme():
    chords = {
        "F": ("F2", "A3 C4 F4"), "Dm": ("D2", "A3 D4 F4"), "Bb": ("Bb1", "Bb3 D4 F4"),
        "C": ("C2", "G3 C4 E4"), "Am": ("A1", "A3 C4 E4"), "Gm7": ("G1", "Bb3 D4 F4"),
        "C7": ("C2", "Bb3 E4 G4"),
    }
    section_a = ["F", "Dm", "Bb", "C", "F", "Am", ("Bb", "C"), "F"]
    section_b = ["Bb", "C", "Am", "Dm", "Gm7", "C7", "F", "C7"]
    form = section_a + section_a + section_b + section_a
    track = Track(bpm=152, bars=len(form), tail_seconds=2)

    for bar, chord in enumerate(form):
        start = bar * 4
        halves = [(chord, 0, 4)] if isinstance(chord, str) else [(chord[0], 0, 2), (chord[1], 2, 2)]
        for name, offset, beats in halves:
            root, stab = chords[name]
            r = midi(root)
            for i in range(int(beats * 2)):
                step = [0, 12, 7, 12][i % 4]
                track.add(start + offset + i * 0.5, slap_bass(hz(r + step), 0.42 * track.beat, 0.42 if i % 2 == 0 else 0.3))
            for i in range(int(beats)):
                for m in notes(stab):
                    track.add(start + offset + i + 0.5, epiano(hz(m), 0.18 * track.beat, 0.075))
        last_of_phrase = bar % 8 == 7
        for eighth in range(8):
            pos = start + eighth * 0.5
            if eighth in (0, 3, 4) and not (last_of_phrase and eighth > 4):
                track.add(pos, kick(0.6))
            if eighth in (2, 6) and not (last_of_phrase and eighth == 6):
                track.add(pos, snare(0.42))
            track.add(pos, hat(0.06 if eighth % 2 else 0.09, 0.1 if eighth == 7 and bar % 2 else 0.035))
        if last_of_phrase:
            for i in range(6):
                track.add(start + 3 + i * (1 / 6), snare(0.22 + 0.04 * i, 0.08))

    melody_a = parse(
        "C5:.5 F5:.5 A5:1 G5:.5 F5:.5 C5:1 D5:.5 F5:.5 A5:.5 G5:.5 F5:1 D5:1 "
        "Bb4:.5 D5:.5 F5:.5 Bb5:.5 A5:.5 G5:.5 F5:.5 D5:.5 E5:1 G5:.5 E5:.5 C5:2 "
        "C5:.5 F5:.5 A5:1 C6:.75 A5:.25 F5:1 E5:.5 A5:.5 C6:.5 B5:.5 A5:1 E5:1 "
        "D5:.5 F5:.5 Bb5:1 E5:.5 G5:.5 C6:1 A5:.5 F5:.5 C5:.5 F5:.5 F5:2")
    melody_b = parse(
        "F5:.5 G5:.5 A5:.5 Bb5:1.5 A5:.5 G5:.5 G5:.5 A5:.5 Bb5:.5 C6:1.5 Bb5:.5 A5:.5 "
        "A5:1 E5:.5 A5:.5 C6:1 B5:.5 A5:.5 F5:1 D5:.5 F5:.5 A5:2 "
        "Bb5:.5 A5:.5 G5:.5 F5:.5 G5:1 D5:1 E5:.5 F5:.5 G5:.5 Bb5:.5 C6:1 Bb5:.5 G5:.5 "
        "A5:1.5 C6:.5 F6:1 C6:1 r:.5 G5:.5 A5:.5 Bb5:.5 C6:.5 D6:.5 E6:1")
    scale = [midi(n) % 12 for n in "F4 G4 A4 Bb4 C5 D5 E5".split()]

    def third_below(events):
        out = []
        for note, beats in events:
            if note is None:
                out.append((None, beats))
                continue
            if note % 12 not in scale:
                out.append((note - 3, beats))
                continue
            idx = scale.index(note % 12)
            target = scale[(idx - 2) % 7]
            out.append((note - ((note % 12 - target) % 12), beats))
        return out

    track.melody(0, melody_a, brass, 0.3)
    track.melody(32, melody_a, brass, 0.3)
    track.melody(32, third_below(melody_a), flute, 0.13)
    track.melody(64, melody_b, flute, 0.26)
    track.melody(64, melody_b, brass, 0.12, transpose=-12)
    track.melody(96, melody_a, brass, 0.3)
    track.melody(96, melody_a, flute, 0.12, transpose=12)
    return track.render(wet=0.2, decay=1.1, predelay=0.02)


if __name__ == "__main__":
    write_wav(OUT / "menu_theme.wav", menu_theme())
    write_wav(OUT / "level01.wav", level_theme())
