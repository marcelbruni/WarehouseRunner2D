import numpy as np

from make_music import (OUT, SR, Track, additive, brass, env_adsr, epiano, flute, hat, hz, kick, level_theme, midi,
                        pad, shaker, slap_bass, snare, soft_bass, write_wav)

MODES = {
    "major": [0, 2, 4, 5, 7, 9, 11],
    "minor": [0, 2, 3, 5, 7, 8, 10],
    "dorian": [0, 2, 3, 5, 7, 9, 10],
    "mixolydian": [0, 2, 4, 5, 7, 9, 10],
}
QUALITIES = {"": [0, 4, 7], "m": [0, 3, 7], "7": [0, 4, 7, 10], "m7": [0, 3, 7, 10], "maj7": [0, 4, 7, 11], "sus": [0, 5, 7]}


def chord(symbol):
    name = symbol[0] + ("b" if symbol[1:2] == "b" else "#" if symbol[1:2] == "#" else "")
    quality = symbol[len(name):]
    return midi(name + "2") % 12, QUALITIES[quality]


def square(f, dur, vel):
    n = int((dur + 0.15) * SR)
    t = np.arange(n) / SR
    curve = f * (1 + 0.004 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.2) / 0.2, 0, 1))
    tone = additive(curve, lambda _: [(k, 1 / k) for k in range(1, 16, 2)], n)
    return vel * 0.55 * tone * env_adsr(n, 0.005, 0.3, 0.7, dur, 0.05)


def marimba(f, dur, vel):
    n = int(0.6 * SR)
    t = np.arange(n) / SR
    tone = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.35) + 0.35 * np.sin(2 * np.pi * f * 3.93 * t) * np.exp(-t / 0.05)
    return vel * tone * np.clip(t / 0.002, 0, 1)


def steel(f, dur, vel):
    n = int(0.9 * SR)
    t = np.arange(n) / SR
    partials = [(1.0, 1.0, 0.6), (2.0, 0.5, 0.35), (3.02, 0.3, 0.2), (4.1, 0.15, 0.1)]
    tone = sum(a * np.sin(2 * np.pi * f * r * t) * np.exp(-t / d) for r, a, d in partials)
    return vel * tone * np.clip(t / 0.004, 0, 1)


def pluck(f, dur, vel):
    n = int((min(dur, 0.6) + 0.1) * SR)
    curve = np.full(n, f)
    tone = additive(curve, lambda tt: [(k, (1 / k) * np.exp(-tt * k * 4)) for k in range(1, 12)], n)
    return vel * tone * env_adsr(n, 0.002, 0.25, 0.2, min(dur, 0.6), 0.04)


def organ(f, dur, vel):
    n = int((dur + 0.08) * SR)
    curve = np.full(n, f)
    tone = additive(curve, lambda _: [(1, 1.0), (2, 0.7), (3, 0.5), (4, 0.35), (6, 0.2), (8, 0.15)], n)
    return vel * 0.45 * tone * env_adsr(n, 0.008, 1.0, 1.0, dur, 0.04)


def reese(f, dur, vel):
    n = int((dur + 0.1) * SR)
    t = np.arange(n) / SR
    wobble = 0.5 + 0.5 * np.sin(2 * np.pi * 0.7 * t)
    out = np.zeros(n)
    for detune in (-0.012, 0.012):
        out += additive(np.full(n, f * (1 + detune)), lambda _: [(k, (1 / k) * np.exp(-k * (0.35 + 0.25 * wobble))) for k in range(1, 18)], n)
    sub = np.sin(2 * np.pi * f * t)
    return vel * (0.5 * out + 0.6 * sub) * env_adsr(n, 0.01, 1.0, 1.0, dur, 0.05)


def sub_bass(f, dur, vel):
    n = int((dur + 0.06) * SR)
    t = np.arange(n) / SR
    return vel * (np.sin(2 * np.pi * f * t) + 0.1 * np.sin(4 * np.pi * f * t)) * env_adsr(n, 0.005, 1.0, 1.0, dur, 0.03)


def clap(vel):
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    noise = np.convolve(np.diff(np.random.default_rng(7).standard_normal(n + 1)), np.ones(3) / 3, mode="same")
    bursts = sum(np.exp(-np.clip(t - d, 0, None) / 0.008) * (t >= d) for d in (0, 0.01, 0.02)) / 3
    return vel * noise * (bursts + 0.5 * np.exp(-t / 0.09))


def crash(vel):
    n = int(1.4 * SR)
    t = np.arange(n) / SR
    noise = np.diff(np.random.default_rng(9).standard_normal(n + 1)) * 0.4
    return vel * noise * np.exp(-t / 0.5)


DRUMS = {
    "pop": {"kick": [0, 6, 8], "snare": [4, 12], "hat": [0, 2, 4, 6, 8, 10, 12, 14]},
    "shuffle": {"kick": [0, 8, 11], "snare": [4, 12], "hat": [0, 2, 4, 6, 8, 10, 12, 14]},
    "dnb": {"kick": [0, 10], "snare": [4, 12], "ghost": [7, 9, 15], "hat": list(range(0, 16, 2)), "hat_soft": list(range(1, 16, 2))},
    "amen": {"kick": [0, 2, 10, 11], "snare": [4, 7, 9, 12, 15], "ghost": [14], "hat": list(range(0, 16, 2))},
    "liquid": {"kick": [0, 10, 11], "snare": [4, 12], "ghost": [3, 7, 14], "hat": list(range(16)), "shaker": list(range(0, 16, 2))},
    "funk": {"kick": [0, 3, 10], "snare": [4, 12], "ghost": [7, 9, 14], "hat": list(range(16))},
    "march": {"kick": [0, 8], "snare": [4, 12, 14, 15], "ghost": [2, 6, 10], "hat": []},
    "electro": {"kick": [0, 4, 8, 12], "clap": [4, 12], "hat": [2, 6, 10, 14], "hat_soft": [1, 3, 5, 7, 9, 11, 13, 15]},
    "calypso": {"kick": [0, 6, 8, 14], "snare": [4, 10, 12], "shaker": list(range(16))},
    "ska": {"kick": [0, 8], "snare": [4, 12], "hat": [2, 6, 10, 14]},
    "rock": {"kick": [0, 6, 8, 10], "snare": [4, 12], "hat": list(range(0, 16, 2))},
}

BASS = {
    "octave": [(s, i, 2) for s, i in zip(range(0, 16, 2), [0, 12, 7, 12] * 2)],
    "walking": [(0, 0, 4), (4, 1, 4), (8, 2, 4), (12, 1, 4)],
    "reese": [(0, 0, 10), (10, 0, 6)],
    "sub": [(0, 0, 6), (10, 0, 3), (14, 12, 2)],
    "funk": [(0, 0, 2), (3, 12, 1), (6, 0, 2), (8, 7, 1), (10, 10, 1), (11, 12, 2), (14, 0, 1)],
    "march": [(0, 0, 4), (4, 7, 4), (8, 0, 4), (12, 7, 4)],
    "electro": [(2, 0, 2), (6, 0, 2), (10, 0, 2), (14, 12, 2)],
    "calypso": [(0, 0, 3), (3, 0, 2), (6, 7, 2), (8, 0, 3), (11, 0, 2), (14, 7, 2)],
    "drive": [(s, 0, 2) for s in range(0, 16, 2)],
}

RHYTHMS = {
    "busy": [[.5, .5, 1, .5, .5, 1], [.75, .75, .5, 1, 1], [.5, .5, .5, .5, 1, 1], [1, .5, .5, 2],
             [.25, .25, .5, .5, .5, 1, 1], [1.5, .5, 1, 1], [.5, 1, .5, 1, 1], [-.5, .5, .5, .5, 1, 1]],
    "long": [[1.5, 1.5, 1], [.75, .75, .5, 2], [2, 1, 1], [.5, .5, .5, 1.5, 1], [3, 1], [-1, 1, 1, 1], [1, 1, 2]],
    "swing": [[.5, .5, 1, 1, 1], [1, .5, .5, 2], [.5, .5, .5, .5, 2], [-.5, .5, 1, .5, 1.5]],
}
CADENCES = {"busy": [[.5, .5, 1, 2], [1, 1, 2]], "long": [[1, 3], [4]], "swing": [[1, 1, 2], [.5, .5, 1, 2]]}


class SwingTrack(Track):
    def __init__(self, bpm, bars, tail_seconds, swing=0.0):
        super().__init__(bpm, bars, tail_seconds)
        self.swing = swing

    def add(self, beat_pos, signal):
        if self.swing:
            whole = np.floor(beat_pos)
            frac = beat_pos - whole
            delay = self.swing / 6
            frac = frac * (0.5 + delay) / 0.5 if frac < 0.5 else 0.5 + delay + (frac - 0.5) * (0.5 - delay) / 0.5
            beat_pos = whole + frac
        super().add(beat_pos, signal)


class Song:
    def __init__(self, bpm, key, mode, sections, form, drums, bass, bass_inst, comp, comp_inst, lead, lead_low,
                 rhythm="busy", seed=1, swing=0.0, wet=0.2, decay=1.1, counter=None, bass_octave=2):
        self.__dict__.update(locals())
        self.rng = np.random.default_rng(seed)
        self.key_pc = midi(key + "4") % 12
        self.scale = [(self.key_pc + d) % 12 for d in MODES[mode]]

    def bar_chords(self, entry):
        return [chord(part) for part in entry.split("/")]

    def chord_at(self, chords, beat_in_bar):
        return chords[0] if len(chords) == 1 or beat_in_bar < 2 else chords[1]

    def nearest(self, pitch, allowed, direction=0):
        candidates = [p for p in range(self.lead_low - 3, self.lead_low + 20) if p % 12 in allowed]
        if direction > 0:
            candidates = [p for p in candidates if p >= pitch] or candidates
        elif direction < 0:
            candidates = [p for p in candidates if p <= pitch] or candidates
        return min(candidates, key=lambda p: (abs(p - pitch), p))

    def step(self, pitch, steps):
        scale_pitches = [p for p in range(self.lead_low - 5, self.lead_low + 22) if p % 12 in self.scale]
        idx = min(range(len(scale_pitches)), key=lambda i: abs(scale_pitches[i] - pitch))
        idx = int(np.clip(idx + steps, 0, len(scale_pitches) - 1))
        return scale_pitches[idx]

    def motif(self):
        pool = RHYTHMS[self.rhythm]
        rhythms = [pool[i] for i in self.rng.choice(len(pool), 3, replace=False)]
        contours = [[int(self.rng.choice([-2, -1, -1, 1, 1, 2, 3, -3])) for _ in r] for r in rhythms]
        offsets = [int(self.rng.integers(-1, 5)) for _ in rhythms]
        cadence = CADENCES[self.rhythm][int(self.rng.integers(len(CADENCES[self.rhythm])))]
        return rhythms, contours, offsets, cadence

    def phrase(self, progression, motif):
        rhythms, contours, offsets, cadence = motif
        center = self.lead_low + 5
        events = []
        for bar, entry in enumerate(progression):
            chords = self.bar_chords(entry)
            cadence_bar = bar % 4 == 3
            if cadence_bar:
                rhythm, contour, offset = cadence, [-1] * len(cadence), len(cadence)
            else:
                pick = 2 if bar == 6 else bar % 2
                rhythm, contour, offset = rhythms[pick], contours[pick], offsets[pick]
            pos = 0.0
            pitch = None
            for i, (beats, move) in enumerate(zip(rhythm, contour)):
                length = abs(beats)
                if beats < 0:
                    events.append((None, length))
                    pos += length
                    continue
                root, quality = self.chord_at(chords, pos)
                tones = [(root + q) % 12 for q in quality]
                if pitch is None:
                    pitch = self.nearest(self.step(center, offset), tones)
                else:
                    previous = pitch
                    pitch = self.step(pitch, move)
                    if pos == int(pos):
                        pitch = self.nearest(pitch, tones, move)
                    if pitch == previous:
                        pitch = self.step(pitch, 1 if move > 0 else -1)
                if cadence_bar and i == len(rhythm) - 1:
                    pitch = self.nearest(pitch, [root] if bar == 7 else tones)
                events.append((pitch, length))
                pos += length
        return events

    def render(self):
        bars_total = sum(len(self.sections[name]) for name in self.form)
        track = SwingTrack(self.bpm, bars_total, 2.5, self.swing)
        motifs = {name: self.motif() for name in self.sections}
        melodies = {name: self.phrase(prog, motifs[name]) for name, prog in self.sections.items()}
        bar = 0
        for section_index, name in enumerate(self.form):
            progression = self.sections[name]
            start_beat = bar * 4
            lead_vel = 0.3 if self.lead is not square else 0.34
            track.melody(start_beat, melodies[name], self.lead, lead_vel)
            if self.counter and section_index % 2 == 1:
                instrument, transpose, vel = self.counter
                track.melody(start_beat, melodies[name], instrument, vel, transpose)
            for i, entry in enumerate(progression):
                self.render_bar(track, bar, self.bar_chords(entry), last_of_phrase=i == len(progression) - 1,
                                section_start=i == 0, sparse=section_index == 0 and i < 2)
                bar += 1
        return track.render(wet=self.wet, decay=self.decay, predelay=0.02)

    def render_bar(self, track, bar, chords, last_of_phrase, section_start, sparse):
        start = bar * 4
        sixteenth = 0.25
        pattern = DRUMS[self.drums]
        for name, steps in pattern.items():
            for s in steps:
                if last_of_phrase and s >= 12 and name in ("kick", "snare", "ghost", "clap"):
                    continue
                pos = start + s * sixteenth
                if name == "kick":
                    track.add(pos, kick(0.65))
                elif name == "snare":
                    track.add(pos, snare(0.45))
                elif name == "ghost":
                    track.add(pos, snare(0.12, 0.06))
                elif name == "clap":
                    track.add(pos, clap(0.5))
                elif name == "hat":
                    track.add(pos, hat(0.09 if s % 4 else 0.12))
                elif name == "hat_soft":
                    track.add(pos, hat(0.04))
                elif name == "shaker":
                    track.add(pos, shaker(0.05))
        if last_of_phrase:
            for i in range(4):
                track.add(start + 3 + i * sixteenth, snare(0.2 + 0.07 * i, 0.07))
        if section_start and self.drums in ("rock", "march", "liquid", "dnb", "amen"):
            track.add(start, crash(0.12))

        for half, (root, quality) in enumerate(chords):
            span = (0, 16) if len(chords) == 1 else (half * 8, half * 8 + 8)
            low = midi("E1") if self.bass_octave == 1 else midi("A1")
            bass_root = low + (root - low) % 12
            for s, interval, length in BASS[self.bass]:
                if not span[0] <= s < span[1]:
                    continue
                if self.bass == "walking":
                    interval = [0, quality[1], quality[2], quality[1]][s // 4]
                self.bass_inst_play(track, start + s * sixteenth, hz(bass_root + interval), length * sixteenth * 0.92)
            tones = [midi("C4") + (root - midi("C4")) % 12 + q for q in quality]
            tones = [t - 12 if t > midi("G4") else t for t in tones]
            self.comp_play(track, start, span, sorted(tones), sparse)

    def bass_inst_play(self, track, pos, f, beats):
        track.add(pos, self.bass_inst(f, beats * track.beat, 0.42))

    def comp_play(self, track, start, span, tones, sparse):
        beat = track.beat
        inst = self.comp_inst
        if self.comp == "offbeat":
            for s in range(span[0] + 2, span[1], 4):
                for t in tones:
                    track.add(start + s / 4, inst(hz(t), 0.18 * beat, 0.07))
        elif self.comp == "pad":
            for t in tones:
                track.add(start + span[0] / 4, pad(hz(t), (span[1] - span[0]) / 4 * beat, 0.055))
        elif self.comp == "arp16":
            seq = tones + [t + 12 for t in tones]
            order = seq + seq[-2:0:-1]
            for i, s in enumerate(range(span[0], span[1])):
                if sparse and s % 4:
                    continue
                track.add(start + s / 4, inst(hz(order[i % len(order)] + 12), 0.2 * beat, 0.08))
            for t in tones:
                track.add(start + span[0] / 4, pad(hz(t), (span[1] - span[0]) / 4 * beat, 0.035))
        elif self.comp == "pump":
            for s in range(span[0], span[1], 4):
                for t in tones:
                    track.add(start + s / 4, inst(hz(t), 0.7 * beat, 0.06))
        elif self.comp == "skank":
            for s in range(span[0] + 2, span[1], 4):
                for t in tones:
                    track.add(start + s / 4, inst(hz(t + 12), 0.15 * beat, 0.07))


SONGS = {
    "level02": Song(138, "G", "major", {
        "A": ["G", "Em", "C", "D", "G", "Em", "Am", "D"],
        "B": ["C", "D", "Bm", "Em", "Am", "D", "G", "D7"],
    }, "AABA", "shuffle", "walking", soft_bass, "offbeat", epiano, marimba, midi("G4"), rhythm="swing", seed=2,
        swing=0.7, counter=(flute, 12, 0.1)),
    "level03": Song(174, "A", "minor", {
        "A": ["Am", "Am", "F", "F", "C", "C", "G", "G"],
        "B": ["Dm", "Dm", "Am", "Am", "F", "F", "E", "E"],
    }, "AABA", "dnb", "reese", reese, "arp16", square, square, midi("A4"), rhythm="long", seed=3,
        wet=0.25, decay=1.6, counter=(pad, 0, 0.06), bass_octave=1),
    "level04": Song(124, "E", "dorian", {
        "A": ["Em7", "A7", "Em7", "A7", "Em7", "A7", "Cmaj7", "B7"],
        "B": ["Cmaj7", "Bm7", "Am7", "D7", "Gmaj7", "Cmaj7", "Am7", "B7"],
    }, "AABA", "funk", "funk", slap_bass, "offbeat", pluck, brass, midi("E5"), seed=4, counter=(organ, -12, 0.08)),
    "level05": Song(144, "Bb", "major", {
        "A": ["Bb", "F", "Gm", "Eb", "Bb", "F", "Eb", "F"],
        "B": ["Eb", "F", "Dm", "Gm", "Cm", "F", "Bb", "F7"],
    }, "AABA", "march", "march", slap_bass, "pad", pad, brass, midi("Bb4"), seed=5, counter=(flute, 12, 0.12)),
    "level06": Song(132, "D", "minor", {
        "A": ["Dm", "Bb", "C", "Am", "Dm", "Bb", "C", "C"],
        "B": ["Gm", "Bb", "F", "C", "Gm", "Bb", "Am", "A7"],
    }, "AABA", "electro", "electro", sub_bass, "arp16", square, square, midi("D5"), seed=6,
        wet=0.18, counter=(square, -12, 0.12)),
    "level07": Song(124, "C", "major", {
        "A": ["C", "F", "G", "C", "Am", "F", "G", "C"],
        "B": ["F", "G", "Em", "Am", "Dm", "G", "C", "G7"],
    }, "AABA", "calypso", "calypso", slap_bass, "offbeat", marimba, steel, midi("C5"), seed=7, counter=(flute, 0, 0.1)),
    "level08": Song(168, "E", "major", {
        "A": ["E", "A", "B", "E", "E", "C#m", "A/B", "E"],
        "B": ["A", "B", "G#m", "C#m", "A", "B", "E", "B7"],
    }, "AABA", "ska", "drive", slap_bass, "skank", pluck, brass, midi("E5"), seed=8, counter=(organ, -12, 0.08)),
    "level09": Song(168, "F", "minor", {
        "A": ["Fm", "Fm", "Db", "Db", "Eb", "Eb", "Cm", "Cm"],
        "B": ["Bbm", "Bbm", "Fm", "Fm", "Db", "Eb", "C", "C"],
    }, "AABA", "amen", "reese", reese, "pad", pad, flute, midi("F5"), rhythm="long", seed=9,
        wet=0.3, decay=1.8, counter=(epiano, 0, 0.08), bass_octave=1),
    "level10": Song(150, "E", "minor", {
        "A": ["Em", "C", "D", "Bm", "Em", "C", "D", "D"],
        "B": ["C", "D", "Em", "Em", "C", "D", "B", "B7"],
    }, "AABA", "rock", "octave", slap_bass, "pump", organ, brass, midi("E5"), seed=10, counter=(square, -12, 0.12)),
    "level11": Song(176, "D", "major", {
        "A": ["Dmaj7", "Dmaj7", "Bm7", "Bm7", "Gmaj7", "Gmaj7", "A", "A"],
        "B": ["Gmaj7", "A", "F#m", "Bm7", "Em7", "A", "Dmaj7", "A7"],
    }, "AABA", "liquid", "sub", sub_bass, "arp16", epiano, flute, midi("D5"), rhythm="long", seed=11,
        wet=0.3, decay=1.8, counter=(epiano, -12, 0.08), bass_octave=1),
}

if __name__ == "__main__":
    write_wav(OUT / "level01.wav", level_theme())
    for level_id, song in SONGS.items():
        write_wav(OUT / f"{level_id}.wav", song.render())
