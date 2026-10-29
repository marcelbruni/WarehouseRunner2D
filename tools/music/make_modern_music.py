import pathlib
import sys
from functools import lru_cache

import numpy as np
import soundfile as sf
from scipy.signal import butter, lfilter, sosfilt

from make_dnb_music import SONGS as DNB_SONGS
from make_level_music import brass, epiano as old_epiano, flute, square, steel

SR = 44100
OUT = pathlib.Path(__file__).resolve().parents[2] / "assets" / "music"
RNG = np.random.default_rng(2026)
NAMES = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"]


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def midi(name):
    base = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}[name[0]]
    rest = name[1:]
    while rest and rest[0] in "b#":
        base += -1 if rest[0] == "b" else 1
        rest = rest[1:]
    return base + 12 * (int(rest) + 1)


@lru_cache(maxsize=None)
def sos(kind, cutoff, order=2):
    if kind == "band":
        return butter(order, cutoff, "bandpass", fs=SR, output="sos")
    return butter(order, cutoff, kind, fs=SR, output="sos")


def filt(x, kind, cutoff, order=2):
    return sosfilt(sos(kind, cutoff, order), x, axis=0)


def env(n, attack, release_at, release, decay=None, sustain=1.0):
    t = np.arange(n) / SR
    e = np.clip(t / max(attack, 1e-4), 0, 1)
    if decay:
        e = e * (sustain + (1 - sustain) * np.exp(-np.clip(t - attack, 0, None) / decay))
    after = t > release_at
    e[after] *= np.exp(-(t[after] - release_at) / release)
    return e


def saw(freq, n, detune_cents=0.0):
    f = np.broadcast_to(freq * 2 ** (detune_cents / 1200), (n,)).astype(float)
    dt = f / SR
    phase = (RNG.random() + np.cumsum(dt)) % 1.0
    out = 2 * phase - 1
    low = phase < dt
    t = phase[low] / dt[low]
    out[low] -= t + t - t * t - 1
    high = phase > 1 - dt
    t = (phase[high] - 1) / dt[high]
    out[high] -= t * t + t + t + 1
    return out


def pan(mono, position):
    angle = (position + 1) * np.pi / 4
    return np.stack([mono * np.cos(angle), mono * np.sin(angle)], axis=1)


def stereo(left, right):
    return np.stack([left, right], axis=1)


# ---------- drums ----------

@lru_cache(maxsize=None)
def kick(variant=0):
    rng = np.random.default_rng(10 + variant)
    n = int(0.32 * SR)
    t = np.arange(n) / SR
    f = 47 + 150 * np.exp(-t / 0.028)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.2)
    click = filt(rng.standard_normal(n), "high", 2500) * np.exp(-t / 0.004) * 0.6
    return pan(np.tanh(2.2 * (body + click)) * 0.9, 0)


@lru_cache(maxsize=None)
def snare(variant=0, length=0.15):
    rng = np.random.default_rng(20 + variant)
    n = int(0.4 * SR)
    t = np.arange(n) / SR
    body = (np.sin(2 * np.pi * 188 * t) + 0.45 * np.sin(2 * np.pi * 291 * t)) * np.exp(-t / 0.05)
    noise = filt(rng.standard_normal(n), "band", (1400, 9000)) * np.exp(-t / length)
    bursts = sum((t >= d) * np.exp(-np.clip(t - d, 0, None) / 0.007) for d in (0, 0.011, 0.023))
    clap = filt(rng.standard_normal(n), "band", (900, 3200)) * (bursts / 3 + 0.4 * np.exp(-t / 0.07))
    mono = np.tanh(1.5 * (0.8 * body + 1.1 * noise + 0.9 * clap))
    return stereo(mono * 0.97, mono)


@lru_cache(maxsize=None)
def hat(variant=0, length=0.035):
    rng = np.random.default_rng(30 + variant)
    n = int(max(0.25, length * 6) * SR)
    t = np.arange(n) / SR
    noise = filt(rng.standard_normal(n), "high", 7500, 4) * np.exp(-t / length)
    return pan(noise * 0.9, 0.25)


@lru_cache(maxsize=None)
def ride():
    rng = np.random.default_rng(40)
    n = int(0.9 * SR)
    t = np.arange(n) / SR
    metal = sum(np.sin(2 * np.pi * f * t + rng.random() * 6) for f in (3150, 4410, 5270, 7020, 8330)) / 5
    noise = filt(rng.standard_normal(n), "high", 6000) * 0.4
    return pan((metal + noise) * np.exp(-t / 0.35) * 0.45, -0.3)


@lru_cache(maxsize=None)
def shaker(variant=0):
    rng = np.random.default_rng(50 + variant)
    n = int(0.12 * SR)
    t = np.arange(n) / SR
    noise = filt(rng.standard_normal(n), "high", 5500, 4)
    return pan(noise * np.clip(t / 0.012, 0, 1) * np.exp(-t / 0.035) * 0.6, 0.35)


@lru_cache(maxsize=None)
def crash():
    rng = np.random.default_rng(60)
    n = int(2.2 * SR)
    t = np.arange(n) / SR
    left = filt(rng.standard_normal(n), "high", 3500) * np.exp(-t / 0.9)
    right = filt(rng.standard_normal(n), "high", 3500) * np.exp(-t / 0.9)
    return stereo(left, right) * 0.5


@lru_cache(maxsize=None)
def impact():
    n = int(1.2 * SR)
    t = np.arange(n) / SR
    f = 38 + 60 * np.exp(-t / 0.05)
    return pan(np.tanh(1.8 * np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.45)), 0)


def riser(seconds):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    progress = t / seconds
    out = np.zeros((n, 2))
    segments = 12
    for side in range(2):
        noise = RNG.standard_normal(n)
        layer = np.zeros(n)
        for i in range(segments):
            centre = 300 * (40 ** (i / (segments - 1)))
            filtered = filt(noise, "band", (centre * 0.7, min(centre * 1.4, SR / 2 - 100)))
            weight = np.clip(1 - np.abs(progress * (segments - 1) - i), 0, 1)
            layer += filtered * weight
        out[:, side] = layer
    return out * (progress ** 2)[:, None] * 0.5


# ---------- synths ----------

def reese(rate):
    def play(f, dur, vel):
        n = int((dur + 0.05) * SR)
        t = np.arange(n) / SR
        left = saw(f, n, -14) + 0.35 * saw(2 * f, n, 4)
        right = saw(f, n, 14) + 0.35 * saw(2 * f, n, -4)
        sweep = 0.5 + 0.5 * np.sin(2 * np.pi * rate * t - np.pi / 2)
        voices = stereo(left, right)
        dark, bright = filt(voices, "low", 260), filt(voices, "low", 1500)
        mixed = dark * (1 - sweep[:, None]) + bright * sweep[:, None]
        driven = filt(np.tanh(2.6 * mixed), "high", 90)
        return vel * driven * env(n, 0.006, dur, 0.03)[:, None]
    return play


def sub(f, dur, vel):
    n = int((dur + 0.04) * SR)
    t = np.arange(n) / SR
    return pan(vel * np.sin(2 * np.pi * f * t) * env(n, 0.004, dur, 0.025), 0)


def supersaw(freqs, dur, vel, cutoff=2400):
    n = int((dur + 0.8) * SR)
    left, right = np.zeros(n), np.zeros(n)
    for f in freqs:
        for i, cents in enumerate((-21, -11, -4, 4, 11, 21)):
            voice = saw(f, n, cents)
            if i % 2:
                left += voice * (0.9 if i < 3 else 0.6)
                right += voice * (0.6 if i < 3 else 0.9)
            else:
                right += voice * (0.9 if i < 3 else 0.6)
                left += voice * (0.6 if i < 3 else 0.9)
    voices = filt(stereo(left, right), "low", cutoff) / (6 * len(freqs)) ** 0.5
    return vel * voices * env(n, 0.35, dur, 0.55)[:, None]


def pluck(f, dur, vel, bright_hz=5200, decay=0.14):
    n = int((dur + 0.25) * SR)
    t = np.arange(n) / SR
    voices = stereo(saw(f, n, -7) + 0.4 * saw(f / 2, n), saw(f, n, 7) + 0.4 * saw(f / 2, n))
    dark, bright = filt(voices, "low", 700), filt(voices, "low", bright_hz)
    amount = np.exp(-t / decay)[:, None]
    tone = dark * (1 - amount) + bright * amount
    return vel * 0.6 * tone * env(n, 0.003, dur, 0.12, decay=0.35, sustain=0.55)[:, None]


def saw_lead(f, dur, vel):
    n = int((dur + 0.15) * SR)
    t = np.arange(n) / SR
    vibrato = f * (1 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.18) / 0.25, 0, 1))
    voices = stereo(saw(vibrato, n, -6) + 0.5 * saw(vibrato * 2, n, 3), saw(vibrato, n, 6) + 0.5 * saw(vibrato * 2, n, -3))
    tone = filt(voices, "low", 3800)
    return vel * 0.45 * tone * env(n, 0.012, dur, 0.08)[:, None]


def rhodes(f, dur, vel):
    n = int((dur + 1.0) * SR)
    t = np.arange(n) / SR
    index = 0.35 + 1.8 * np.exp(-t / 0.3)
    tone = np.sin(2 * np.pi * f * t + index * np.sin(2 * np.pi * f * t)) + 0.2 * np.exp(-t / 0.015) * np.sin(2 * np.pi * f * 14 * t)
    tremolo = 0.5 + 0.35 * np.sin(2 * np.pi * 4.2 * t)
    mono = vel * tone * env(n, 0.003, dur, 0.15, decay=1.1, sustain=0.0)
    return stereo(mono * tremolo, mono * (1 - tremolo))


def bell(f, dur, vel):
    n = int(1.4 * SR)
    t = np.arange(n) / SR
    index = 2.4 * np.exp(-t / 0.18)
    tone = np.sin(2 * np.pi * f * t + index * np.sin(2 * np.pi * f * 3.5 * t)) * np.exp(-t / 0.55)
    return pan(vel * tone * np.clip(t / 0.002, 0, 1), RNG.uniform(-0.4, 0.4))


LEADS = {square: saw_lead, brass: saw_lead, flute: pluck, old_epiano: rhodes, steel: bell}
STYLE_SOUND = {
    "twostep": {"arp": lambda f, d, v: pluck(f, d, v, 3000, 0.05), "arp_vel": 0.16, "pad_cutoff": 1800},
    "jungle": {"arp": None, "arp_vel": 0.0, "pad_cutoff": 1500},
    "liquid": {"arp": bell, "arp_vel": 0.12, "pad_cutoff": 2600},
}


# ---------- arrangement ----------

class Mix:
    def __init__(self, bpm, bars):
        self.beat = 60.0 / bpm
        self.length = int(round(bars * 4 * self.beat * SR))
        size = self.length + 6 * SR
        self.buses = {name: np.zeros((size, 2)) for name in ("drums", "sub", "music", "reverb", "delay")}
        self.kicks = []
        self.reverb_decay = 2.3
        self.reverb_level = 0.9

    def add(self, bus, beat_pos, signal, reverb=0.0, delay=0.0, nudge_ms=0.0):
        start = int(round((beat_pos * self.beat + nudge_ms / 1000) * SR))
        start = max(start, 0)
        end = min(start + len(signal), self.length + 6 * SR)
        if end <= start:
            return
        chunk = signal[: end - start]
        self.buses[bus][start:end] += chunk
        if reverb:
            self.buses["reverb"][start:end] += chunk * reverb
        if delay:
            self.buses["delay"][start:end] += chunk * delay

    def render(self):
        n = len(self.buses["music"])
        duck = np.ones(n)
        t = np.arange(int(0.22 * SR)) / SR
        shape = 1 - 0.55 * np.exp(-t / 0.085)
        for k in self.kicks:
            start = int(round(k * self.beat * SR))
            end = min(start + len(t), n)
            duck[start:end] = np.minimum(duck[start:end], shape[: end - start])
        music = self.buses["music"] * duck[:, None]
        sub_bus = self.buses["sub"] * (0.35 + 0.65 * duck)[:, None]

        delay = np.zeros_like(music)
        for seconds, side, gain in ((0.75 * self.beat, 0, 0.42), (1.5 * self.beat, 1, 0.3), (2.25 * self.beat, 0, 0.16), (3.0 * self.beat, 1, 0.08)):
            shift = int(seconds * SR)
            delay[shift:, side] += self.buses["delay"][:-shift].sum(axis=1) * gain
        delay = filt(filt(delay, "low", 4500), "high", 300)

        reverb = convolve_reverb(self.buses["reverb"] + delay * 0.3, self.reverb_decay)
        mix = self.buses["drums"] + sub_bus + music + delay + reverb * self.reverb_level
        mix = filt(mix, "high", 25)

        loop = mix[: self.length].copy()
        overflow = mix[self.length:]
        loop[: len(overflow)] += overflow[: self.length]

        rms = np.sqrt(np.mean(loop ** 2))
        loop = np.tanh(loop * (0.22 / rms))
        return loop / np.max(np.abs(loop)) * 0.84


def convolve_reverb(signal, decay):
    n_ir = int(decay * 1.5 * SR)
    t = np.arange(n_ir) / SR
    out = np.zeros_like(signal)
    size = 1 << int(np.ceil(np.log2(len(signal) + n_ir)))
    for side in range(2):
        ir = filt(RNG.standard_normal(n_ir), "low", 7000) * np.exp(-t * 6.9 / decay)
        ir[: int(0.025 * SR)] = 0
        ir /= np.sqrt(np.sum(ir ** 2))
        out[:, side] = np.fft.irfft(np.fft.rfft(signal[:, side], size) * np.fft.rfft(ir, size), size)[: len(signal)]
    return out


DRUMS = {
    "twostep": {
        "main": {"kick": [0, 10], "snare": [4, 12], "ghost": [7, 15], "hat": [0, 2, 4, 6, 8, 10, 12, 14], "hat_soft": [1, 3, 5, 9, 11, 13]},
        "vary": {"kick": [0, 6, 10], "snare": [4, 12, 14], "ghost": [7, 9], "hat": [0, 2, 4, 6, 8, 10, 12, 14], "open": [14]},
    },
    "jungle": {
        "main": {"kick": [0, 2, 10, 11], "snare": [4, 7, 9, 12, 15], "ghost": [14], "ride": [0, 4, 8, 12], "hat": [2, 6, 10, 14]},
        "vary": {"kick": [0, 2, 10], "snare": [4, 7, 9, 12, 13, 14, 15], "ghost": [6], "ride": [0, 4, 8, 12], "hat": [2, 6, 10]},
    },
    "liquid": {
        "main": {"kick": [0, 10], "snare": [4, 12], "ghost": [3, 6, 9, 14], "shaker": list(range(16)), "hat": [2, 6, 10, 14]},
        "vary": {"kick": [0, 7, 10], "snare": [4, 12], "ghost": [3, 9, 11, 14, 15], "shaker": list(range(16)), "open": [14]},
    },
}
BASS = {
    "twostep": [(0, 0, 8), (8, 0, 4), (12, 12, 2), (14, 7, 2)],
    "jungle": [(0, 0, 10), (10, 0, 6)],
    "liquid": [(0, 0, 6), (10, 0, 3), (14, 12, 2)],
}


def render_song(song):
    song.rng = np.random.default_rng(song.seed)
    motifs = {name: song.motif() for name in song.sections}
    melodies = {name: song.phrase(prog, motifs[name]) for name, prog in song.sections.items()}
    plan = [("intro", song.sections["A"][:4])] + [(name, song.sections[name]) for name in song.form]
    mix = Mix(song.bpm, sum(len(p) for _, p in plan))
    sound = STYLE_SOUND[song.style]
    lead = LEADS[song.lead]
    bass = reese(song.bpm / 60 / 2) if song.style != "liquid" else None
    bar = 0
    for index, (name, progression) in enumerate(plan):
        if name != "intro":
            vel = 0.5 if name != "B" else 0.42
            play_line(mix, bar * 4, melodies[name], lead, vel, reverb=0.25, delay=0.3)
            if index in (2, 4):
                play_line(mix, bar * 4, melodies[name], rhodes, 0.16, reverb=0.3, delay=0.0, transpose=-12)
        for i, entry in enumerate(progression):
            part = "intro" if name == "intro" else "breakdown" if name == "B" and i < 4 else "full"
            build = (name == "intro" and i == len(progression) - 1) or (name == "B" and i == 3)
            if build:
                mix.add("music", bar * 4 - 4, riser(8 * mix.beat), reverb=0.2)
            drop = (name == "A" and i == 0 and index == 1) or (name == "B" and i == 4)
            if drop:
                mix.add("drums", bar * 4, impact())
                mix.add("drums", bar * 4, crash(), reverb=0.15)
            drum_bar(mix, song.style, bar, part, variation=i % 4 == 3, build=build, crash_now=i == 0 and part == "full" and not drop)
            music_bar(mix, song, sound, bass, bar, song.bar_chords(entry), part)
            bar += 1
    return mix.render()


def play_line(mix, start_beat, events, instrument, vel, reverb, delay, transpose=0):
    pos = start_beat
    for note, beats in events:
        if note is not None:
            mix.add("music", pos, instrument(hz(note + transpose), beats * mix.beat * 0.92, vel), reverb=reverb, delay=delay)
        pos += beats


def drum_bar(mix, style, bar, part, variation, build, crash_now):
    start = bar * 4
    pattern = DRUMS[style]["vary" if variation else "main"]
    for name, steps in pattern.items():
        for s in steps:
            pos = start + s / 4
            nudge = RNG.normal(0, 2.5) if name not in ("kick",) else 0
            if build and s >= 8 and name in ("kick", "snare", "ghost"):
                continue
            if part == "intro" and name in ("kick", "snare", "ghost"):
                continue
            if part == "breakdown" and name in ("kick", "ghost", "ride"):
                continue
            if part == "breakdown" and name == "snare":
                if s == 8:
                    mix.add("drums", pos, snare(1, 0.25) * 0.7, reverb=0.35)
                continue
            variant = int(RNG.integers(3))
            if name == "kick":
                mix.add("drums", pos, kick(variant) * 0.95)
                mix.kicks.append(pos)
            elif name == "snare":
                mix.add("drums", pos, snare(variant) * 0.8, reverb=0.12, nudge_ms=nudge)
            elif name == "ghost":
                mix.add("drums", pos, snare(variant, 0.06) * 0.16, nudge_ms=nudge)
            elif name == "hat":
                mix.add("drums", pos, hat(variant) * (0.32 if s % 4 == 2 else 0.24) * (0.7 if part == "intro" else 1), nudge_ms=nudge)
            elif name == "hat_soft":
                mix.add("drums", pos, hat(variant) * 0.12, nudge_ms=nudge)
            elif name == "open":
                mix.add("drums", pos, hat(variant, 0.16) * 0.22)
            elif name == "ride":
                mix.add("drums", pos, ride() * 0.5, reverb=0.08)
            elif name == "shaker":
                mix.add("drums", pos, shaker(variant) * (0.22 if s % 2 else 0.32), nudge_ms=nudge)
    if build:
        for i in range(8):
            mix.add("drums", start + 2 + i * 0.25, snare(i % 3, 0.06) * (0.2 + 0.08 * i), reverb=0.1)
    if crash_now:
        mix.add("drums", start, crash() * 0.7, reverb=0.1)


def music_bar(mix, song, sound, bass, bar, chords, part):
    start = bar * 4
    for half, (root, quality) in enumerate(chords):
        span = (0, 16) if len(chords) == 1 else (half * 8, half * 8 + 8)
        span_beats = (span[1] - span[0]) / 4
        low = midi("E1")
        bass_root = low + (root - low) % 12
        if part == "full":
            for s, interval, length in BASS[song.style]:
                if span[0] <= s < span[1]:
                    seconds = length / 4 * mix.beat * 0.94
                    mix.add("sub", start + s / 4, sub(hz(bass_root + interval), seconds, 0.62))
                    if bass:
                        mix.add("music", start + s / 4, bass(hz(bass_root + interval + 12), seconds, 0.3))
        elif part == "breakdown":
            mix.add("sub", start + span[0] / 4, sub(hz(bass_root), span_beats * mix.beat * 0.95, 0.4))
        tones = [midi("C4") + root % 12 + q for q in quality]
        tones = sorted(t - 12 if t > midi("G4") else t for t in tones)
        pad_vel = 0.2 if part != "full" else 0.15
        mix.add("music", start + span[0] / 4, supersaw([hz(t) for t in tones], span_beats * mix.beat, pad_vel, sound["pad_cutoff"]), reverb=0.35)
        if sound["arp"] and part != "breakdown":
            seq = tones + [t + 12 for t in tones]
            order = seq + seq[-2:0:-1]
            for i, s in enumerate(range(span[0], span[1])):
                if part == "intro" and s % 2:
                    continue
                mix.add("music", start + s / 4, sound["arp"](hz(order[i % len(order)] + 12), 0.2 * mix.beat, sound["arp_vel"]), reverb=0.2, delay=0.2)


def write_ogg(name, audio):
    path = OUT / f"{name}.ogg"
    with sf.SoundFile(str(path), "w", SR, 2, format="OGG", subtype="VORBIS") as file:
        for offset in range(0, len(audio), 4096):
            file.write(audio[offset: offset + 4096].astype(np.float32))
    print(f"{path.name}: {len(audio) / SR:.1f} s, {path.stat().st_size / 1e6:.2f} MB")


if __name__ == "__main__":
    wanted = sys.argv[1:] or list(DNB_SONGS)
    for level_id in wanted:
        write_ogg(level_id, render_song(DNB_SONGS[level_id]))
