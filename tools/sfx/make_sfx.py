import pathlib
import sys
import wave

import numpy as np

SR = 44100
OUT = pathlib.Path(__file__).resolve().parents[2] / "assets" / "sfx"
RNG = np.random.default_rng(7)


def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


def sweep(f0, f1, seconds, curve=1.0):
    t = t_axis(seconds)
    freq = f0 + (f1 - f0) * (t / seconds) ** curve
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def env(n, attack, release_power=2.0):
    a = max(1, int(SR * attack))
    e = np.ones(n)
    e[:a] = np.linspace(0, 1, a)
    e[a:] = np.linspace(1, 0, n - a) ** release_power
    return e


def lowpass(signal, start_hz, end_hz=None):
    end_hz = start_hz if end_hz is None else end_hz
    cutoff = np.linspace(start_hz, end_hz, len(signal))
    alpha = 1 - np.exp(-2 * np.pi * cutoff / SR)
    out = np.zeros_like(signal)
    y = 0.0
    for i, (x, a) in enumerate(zip(signal, alpha)):
        y += a * (x - y)
        out[i] = y
    return out


def noise(seconds):
    return RNG.uniform(-1, 1, int(SR * seconds))


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[: len(p)] += p
    return out


def jump():
    s = sweep(220, 560, 0.13, 0.7)
    return (s + 0.25 * np.sign(s)) * env(len(s), 0.004)


def double_jump():
    s = sweep(420, 980, 0.14, 0.6)
    sparkle = sweep(1300, 2100, 0.1) * 0.25
    return mix(s, sparkle) * env(int(SR * 0.14), 0.003)


def slide_jump():
    s = sweep(160, 760, 0.22, 1.4)
    return (s + 0.2 * np.sign(s)) * env(len(s), 0.01, 1.5)


def dash():
    n = noise(0.24)
    body = lowpass(n, 600, 3200) - lowpass(n, 150)
    return body * env(len(n), 0.05, 1.6) * 2.2


def wall_kick():
    thud = sweep(150, 60, 0.07) * env(int(SR * 0.07), 0.001)
    click = lowpass(noise(0.025), 4000) * env(int(SR * 0.025), 0.001)
    rise = sweep(300, 700, 0.12, 0.8) * env(int(SR * 0.12), 0.01) * 0.6
    return mix(thud, click * 0.8, np.concatenate([np.zeros(int(SR * 0.02)), rise]))


def roll():
    t = t_axis(0.28)
    n = lowpass(noise(0.28), 700)
    tumble = 0.6 + 0.4 * np.sin(2 * np.pi * 14 * t)
    return n * tumble * env(len(t), 0.02, 1.2) * 2.5


def land():
    thump = sweep(95, 45, 0.11) * env(int(SR * 0.11), 0.002)
    grit = lowpass(noise(0.06), 1500) * env(int(SR * 0.06), 0.001) * 0.7
    return mix(thump, grit)


def perfect():
    def bell(freq, seconds):
        t = t_axis(seconds)
        return (np.sin(2 * np.pi * freq * t) + 0.3 * np.sin(2 * np.pi * freq * 2.01 * t)) * np.exp(-t * 9)

    second = np.concatenate([np.zeros(int(SR * 0.07)), bell(1976, 0.3)])
    return mix(bell(1319, 0.3), second) * 0.6


SOUNDS = dict(jump=jump, double_jump=double_jump, slide_jump=slide_jump, dash=dash,
              wall_kick=wall_kick, roll=roll, land=land, perfect=perfect)


def write(name, signal):
    signal = signal / max(1e-9, np.max(np.abs(signal))) * 0.9
    fade = min(len(signal), int(SR * 0.005))
    signal[-fade:] *= np.linspace(1, 0, fade)
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / f"{name}.wav"), "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes((signal * 32767).astype(np.int16).tobytes())


for sound in sys.argv[1:] or list(SOUNDS):
    write(sound, SOUNDS[sound]())
    print(f"{sound}.wav")
