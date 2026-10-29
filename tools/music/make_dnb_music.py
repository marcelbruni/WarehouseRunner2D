import numpy as np

from make_level_music import Song, brass, crash, epiano, flute, marimba, pad, square, steel, sub_bass
from make_music import OUT, SR, Track, additive, env_adsr, hz, midi, shaker, write_wav

NOISE = np.random.default_rng(174)


def dnb_kick(vel):
    n = int(0.28 * SR)
    t = np.arange(n) / SR
    f = 52 + 140 * np.exp(-t / 0.022)
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.13)
    click = 0.5 * NOISE.standard_normal(n) * np.exp(-t / 0.0025)
    return vel * np.tanh(1.6 * (body + click))


def dnb_snare(vel, length=0.13):
    n = int(0.32 * SR)
    t = np.arange(n) / SR
    noise = np.convolve(np.diff(NOISE.standard_normal(n + 1)), np.ones(3) / 3, mode="same")
    tone = (np.sin(2 * np.pi * 205 * t) + 0.5 * np.sin(2 * np.pi * 330 * t)) * np.exp(-t / 0.045)
    return vel * np.tanh(1.4 * (0.8 * noise * np.exp(-t / length) + 0.7 * tone))


def dnb_hat(vel, length=0.03):
    n = int(0.2 * SR)
    t = np.arange(n) / SR
    noise = np.diff(np.diff(NOISE.standard_normal(n + 2))) * 0.25
    return vel * noise * np.exp(-t / length)


def ride(vel):
    n = int(0.6 * SR)
    t = np.arange(n) / SR
    metal = sum(np.sign(np.sin(2 * np.pi * f * t)) for f in (3120, 4370, 5230, 6890)) / 4
    return vel * 0.35 * metal * np.exp(-t / 0.25) * np.clip(t / 0.001, 0, 1)


def reese_synced(rate):
    def reese(f, dur, vel):
        n = int((dur + 0.08) * SR)
        t = np.arange(n) / SR
        wobble = 0.5 + 0.5 * np.sin(2 * np.pi * rate * t)
        out = np.zeros(n)
        for detune in (-0.011, 0.0, 0.013):
            out += additive(np.full(n, f * (1 + detune)), lambda _: [(k, (1 / k) * np.exp(-k * (0.28 + 0.3 * wobble))) for k in range(1, 20)], n)
        sub = np.sin(2 * np.pi * f * t)
        return vel * np.tanh(0.4 * out + 0.7 * sub) * env_adsr(n, 0.012, 1.0, 1.0, dur, 0.04)
    return reese


DRUM_STYLES = {
    "twostep": {
        "main": {"kick": [0, 10], "snare": [4, 12], "ghost": [7, 15], "hat": [0, 2, 4, 6, 8, 10, 12, 14], "hat_soft": [1, 3, 5, 9, 11, 13]},
        "vary": {"kick": [0, 6, 10], "snare": [4, 12, 14], "ghost": [7, 9], "hat": [0, 2, 4, 6, 8, 10, 12, 14], "hat_soft": [3, 11, 13]},
    },
    "jungle": {
        "main": {"kick": [0, 2, 10, 11], "snare": [4, 7, 9, 12, 15], "ghost": [14], "ride": [0, 4, 8, 12], "hat": [2, 6, 10, 14]},
        "vary": {"kick": [0, 2, 10], "snare": [4, 7, 9, 12, 13, 14, 15], "ghost": [6], "ride": [0, 4, 8, 12], "hat": [2, 6, 10]},
    },
    "liquid": {
        "main": {"kick": [0, 10], "snare": [4, 12], "ghost": [3, 6, 9, 14], "shaker": list(range(16)), "hat": [2, 6, 10, 14]},
        "vary": {"kick": [0, 7, 10], "snare": [4, 12], "ghost": [3, 9, 11, 14, 15], "shaker": list(range(16)), "hat": [2, 6, 10, 14]},
    },
}

BASS_STYLES = {
    "twostep": [(0, 0, 8), (8, 0, 4), (12, 12, 2), (14, 7, 2)],
    "jungle": [(0, 0, 10), (10, 0, 6)],
    "liquid": [(0, 0, 6), (10, 0, 3), (14, 12, 2)],
}


class DnbSong(Song):
    def __init__(self, style, bpm, key, mode, sections, lead, lead_low, seed, comp="pad", comp_inst=pad,
                 counter=None, wet=0.28, decay=1.7, echo=True):
        super().__init__(bpm, key, mode, sections, "AABA", "dnb", "reese", None, comp, comp_inst, lead, lead_low,
                         rhythm="long", seed=seed, wet=wet, decay=decay, counter=counter, bass_octave=1)
        self.style = style
        self.echo = echo
        self.bass_sound = reese_synced(bpm / 60 / 2) if style != "liquid" else sub_bass

    def render(self):
        motifs = {name: self.motif() for name in self.sections}
        melodies = {name: self.phrase(prog, motifs[name]) for name, prog in self.sections.items()}
        plan = [("intro", self.sections["A"][:4])] + [(name, self.sections[name]) for name in self.form]
        bars_total = sum(len(prog) for _, prog in plan)
        drums = Track(self.bpm, bars_total, 2.5)
        music = Track(self.bpm, bars_total, 2.5)
        kicks = []
        bar = 0
        for index, (name, progression) in enumerate(plan):
            if name != "intro":
                lead_vel = 0.3 if name != "B" else 0.26
                self.lead_line(music, bar * 4, melodies[name], lead_vel)
                if self.counter and index in (2, 4):
                    instrument, transpose, vel = self.counter
                    music.melody(bar * 4, melodies[name], instrument, vel, transpose)
            for i, entry in enumerate(progression):
                part = "intro" if name == "intro" else "breakdown" if name == "B" and i < 4 else "full"
                build = (name == "intro" and i == len(progression) - 1) or (name == "B" and i == 3)
                variation = i % 4 == 3
                self.drum_bar(drums, bar, part, variation, build, section_start=i == 0, kicks=kicks)
                self.music_bar(music, bar, self.bar_chords(entry), part)
                bar += 1
        duck = np.ones(len(music.buffer))
        for k in kicks:
            start = int(round(k * music.beat * SR))
            t = np.arange(int(0.25 * SR)) / SR
            end = min(start + len(t), len(duck))
            duck[start:end] = np.minimum(duck[start:end], 1 - 0.45 * np.exp(-t / 0.09)[: end - start])
        music.buffer = music.buffer * duck + drums.buffer
        return music.render(wet=self.wet, decay=self.decay, predelay=0.02)

    def lead_line(self, track, start_beat, events, vel):
        track.melody(start_beat, events, self.lead, vel)
        if self.echo:
            track.melody(start_beat + 0.75, events, self.lead, vel * 0.3)
            track.melody(start_beat + 1.5, events, self.lead, vel * 0.12)

    def drum_bar(self, track, bar, part, variation, build, section_start, kicks):
        start = bar * 4
        pattern = DRUM_STYLES[self.style]["vary" if variation else "main"]
        for name, steps in pattern.items():
            for s in steps:
                if build and s >= 8 and name in ("kick", "snare", "ghost"):
                    continue
                pos = start + s / 4
                if part == "intro" and name in ("kick", "snare"):
                    continue
                if part == "breakdown" and name == "kick":
                    continue
                if part == "breakdown" and name == "snare":
                    if s == 8:
                        track.add(pos, dnb_snare(0.4, 0.2))
                    continue
                if name == "kick":
                    track.add(pos, dnb_kick(0.75))
                    kicks.append(pos)
                elif name == "snare":
                    track.add(pos, dnb_snare(0.55))
                elif name == "ghost":
                    track.add(pos, dnb_snare(0.1, 0.05))
                elif name == "hat":
                    track.add(pos, dnb_hat(0.1 if part != "intro" else 0.07))
                elif name == "hat_soft":
                    track.add(pos, dnb_hat(0.045))
                elif name == "ride":
                    track.add(pos, ride(0.07))
                elif name == "shaker":
                    track.add(pos, shaker(0.035 if s % 2 else 0.055))
        if build:
            for i in range(8):
                track.add(start + 2 + i * 0.25, dnb_snare(0.12 + 0.05 * i, 0.06))
        if section_start and part == "full":
            track.add(start, crash(0.13))

    def music_bar(self, track, bar, chords, part):
        start = bar * 4
        for half, (root, quality) in enumerate(chords):
            span = (0, 16) if len(chords) == 1 else (half * 8, half * 8 + 8)
            low = midi("E1")
            bass_root = low + (root - low) % 12
            if part == "full":
                for s, interval, length in BASS_STYLES[self.style]:
                    if span[0] <= s < span[1]:
                        track.add(start + s / 4, self.bass_sound(hz(bass_root + interval), length / 4 * track.beat * 0.94, 0.45))
            elif part == "breakdown":
                track.add(start + span[0] / 4, sub_bass(hz(bass_root), (span[1] - span[0]) / 4 * track.beat * 0.95, 0.3))
            tones = [midi("C4") + root % 12 + q for q in quality]
            tones = sorted(t - 12 if t > midi("G4") else t for t in tones)
            self.comp_play(track, start, span, tones, sparse=part == "intro")


SONGS = {
    "level01": DnbSong("liquid", 174, "E", "major", {
        "A": ["Emaj7", "Emaj7", "C#m7", "C#m7", "Amaj7", "Amaj7", "B", "B"],
        "B": ["Amaj7", "B", "G#m7", "C#m7", "F#m7", "B", "Emaj7", "B7"],
    }, flute, midi("E5"), seed=21, comp="arp16", comp_inst=epiano, counter=(epiano, -12, 0.08)),
    "level02": DnbSong("twostep", 172, "G", "minor", {
        "A": ["Gm", "Gm", "Eb", "Eb", "Bb", "Bb", "F", "F"],
        "B": ["Cm", "Cm", "Gm", "Gm", "Eb", "F", "D", "D7"],
    }, square, midi("G4"), seed=22, comp="arp16", comp_inst=marimba, counter=(pad, 0, 0.06)),
    "level03": DnbSong("twostep", 174, "A", "minor", {
        "A": ["Am", "Am", "F", "F", "C", "C", "G", "G"],
        "B": ["Dm", "Dm", "Am", "Am", "F", "F", "E", "E"],
    }, square, midi("A4"), seed=3, comp="arp16", comp_inst=square, counter=(pad, 0, 0.06)),
    "level04": DnbSong("jungle", 170, "C", "minor", {
        "A": ["Cm", "Cm", "Ab", "Ab", "Bb", "Bb", "Gm", "Gm"],
        "B": ["Fm", "Fm", "Cm", "Cm", "Ab", "Bb", "G", "G"],
    }, brass, midi("C5"), seed=24, counter=(flute, 12, 0.08)),
    "level05": DnbSong("liquid", 174, "Bb", "major", {
        "A": ["Bbmaj7", "Bbmaj7", "Gm7", "Gm7", "Ebmaj7", "Ebmaj7", "F", "F"],
        "B": ["Ebmaj7", "F", "Dm7", "Gm7", "Cm7", "F", "Bbmaj7", "F7"],
    }, epiano, midi("Bb4"), seed=25, comp="arp16", comp_inst=marimba, counter=(flute, 12, 0.08)),
    "level06": DnbSong("twostep", 174, "D", "minor", {
        "A": ["Dm", "Dm", "Bb", "Bb", "C", "C", "Am", "Am"],
        "B": ["Gm", "Gm", "Bb", "Bb", "F", "C", "A", "A7"],
    }, square, midi("D5"), seed=26, comp="arp16", comp_inst=square, counter=(square, -12, 0.1)),
    "level07": DnbSong("liquid", 172, "C", "major", {
        "A": ["Cmaj7", "Cmaj7", "Am7", "Am7", "Fmaj7", "Fmaj7", "G", "G"],
        "B": ["Fmaj7", "G", "Em7", "Am7", "Dm7", "G", "Cmaj7", "G7"],
    }, steel, midi("C5"), seed=27, comp="arp16", comp_inst=marimba, counter=(flute, 0, 0.08)),
    "level08": DnbSong("jungle", 172, "E", "minor", {
        "A": ["Em", "Em", "C", "C", "D", "D", "Bm", "Bm"],
        "B": ["Am", "Am", "Em", "Em", "C", "D", "B", "B7"],
    }, flute, midi("E5"), seed=28, counter=(brass, -12, 0.1)),
    "level09": DnbSong("jungle", 168, "F", "minor", {
        "A": ["Fm", "Fm", "Db", "Db", "Eb", "Eb", "Cm", "Cm"],
        "B": ["Bbm", "Bbm", "Fm", "Fm", "Db", "Eb", "C", "C"],
    }, flute, midi("F5"), seed=9, counter=(epiano, 0, 0.08)),
    "level10": DnbSong("twostep", 174, "B", "minor", {
        "A": ["Bm", "Bm", "G", "G", "D", "D", "A", "A"],
        "B": ["Em", "Em", "Bm", "Bm", "G", "A", "F#", "F#7"],
    }, brass, midi("B4"), seed=30, comp="arp16", comp_inst=epiano, counter=(square, 12, 0.07)),
    "level11": DnbSong("liquid", 176, "D", "major", {
        "A": ["Dmaj7", "Dmaj7", "Bm7", "Bm7", "Gmaj7", "Gmaj7", "A", "A"],
        "B": ["Gmaj7", "A", "F#m7", "Bm7", "Em7", "A", "Dmaj7", "A7"],
    }, flute, midi("D5"), seed=11, comp="arp16", comp_inst=epiano, counter=(epiano, -12, 0.08)),
    "level12": DnbSong("twostep", 176, "F#", "minor", {
        "A": ["F#m", "F#m", "D", "D", "A", "A", "E", "E"],
        "B": ["Bm", "Bm", "F#m", "F#m", "D", "E", "C#", "C#7"],
    }, square, midi("F#4"), seed=42, comp="arp16", comp_inst=marimba, counter=(pad, 0, 0.06)),
    "level13": DnbSong("jungle", 172, "G", "minor", {
        "A": ["Gm", "Gm", "Eb", "Eb", "Bb", "Bb", "F", "F"],
        "B": ["Cm", "Cm", "Gm", "Gm", "Eb", "F", "D", "D7"],
    }, flute, midi("G5"), seed=13, counter=(brass, -12, 0.1)),
    "level14": DnbSong("liquid", 174, "A", "major", {
        "A": ["Amaj7", "Amaj7", "F#m7", "F#m7", "Dmaj7", "Dmaj7", "E", "E"],
        "B": ["Dmaj7", "E", "C#m7", "F#m7", "Bm7", "E", "Amaj7", "E7"],
    }, epiano, midi("A4"), seed=14, comp="arp16", comp_inst=marimba, counter=(flute, 12, 0.08)),
    "level15": DnbSong("twostep", 174, "E", "minor", {
        "A": ["Em", "Em", "C", "C", "G", "G", "D", "D"],
        "B": ["Am", "Am", "Em", "Em", "C", "D", "B", "B7"],
    }, square, midi("E5"), seed=15, comp="arp16", comp_inst=epiano, counter=(pad, 0, 0.06)),
}

if __name__ == "__main__":
    for level_id, song in SONGS.items():
        write_wav(OUT / f"{level_id}.wav", song.render())
