"""Create short exploration foley from the project's supplied recordings.

Run with Python and numpy. Outputs mono 44.1 kHz PCM, with silence removed,
gentle tone shaping and fades. Original SFX files are never modified.
"""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/Sprites/SFX"
OUTPUT = ROOT / "Assets/Audio/CharacterFoley"
RATE = 44100


def read(name):
    with wave.open(str(SOURCE / (name + ".wav"))) as wav:
        assert wav.getsampwidth() == 3, "Expected supplied 24-bit PCM recordings"
        raw = np.frombuffer(wav.readframes(wav.getnframes()), np.uint8).reshape(-1, 3)
        values = raw[:, 0].astype(np.int32) | raw[:, 1].astype(np.int32) << 8 | raw[:, 2].astype(np.int32) << 16
        values = ((values ^ 8388608) - 8388608).astype(float) / 8388608
        return values.reshape(-1, wav.getnchannels()).mean(axis=1), wav.getframerate()


def render(source, name, pitch, cutoff, duration, peak):
    samples, rate = read(source)
    block = max(1, round(rate * .004))
    rms = np.sqrt(np.mean(samples[:len(samples) // block * block].reshape(-1, block) ** 2, axis=1))
    active = np.flatnonzero(rms > rms.max() * .025)
    samples = samples[max(0, active[0] * block - round(rate * .003)):
                      min(len(samples), (active[-1] + 1) * block + round(rate * .02))]
    # Preserve the transient and natural pitch; cap only the remaining tail.
    length = min(round(len(samples) / rate / pitch * RATE), round(duration * RATE))
    samples = np.interp(np.arange(length) * pitch * rate / RATE, np.arange(len(samples)), samples)
    frequencies = np.fft.rfftfreq(length, 1 / RATE)
    shaping = (frequencies / np.maximum(frequencies, 70)) ** 2
    shaping *= 1 / np.sqrt(1 + (frequencies / cutoff) ** 6)
    samples = np.fft.irfft(np.fft.rfft(samples) * shaping, n=length)
    samples -= samples.mean()
    attack = min(round(RATE * .002), length // 4)
    release = min(round(RATE * .035), length // 3)
    samples[:attack] *= np.linspace(0, 1, attack)
    samples[-release:] *= np.linspace(1, 0, release) ** 2
    samples *= peak / max(np.max(np.abs(samples)), .00001)
    assert np.isfinite(samples).all() and np.max(np.abs(samples)) < .95
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(np.round(samples * 32767).astype("<i2").tobytes())
    print(f"{name}: {length / RATE:.3f}s, peak {peak:.2f}")


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    profiles = [
        ("Warrior", "FEETMisc_STEP-Boots on Generic Ground 2", .90, 2800, .21, .58),
        ("Archer", "FEETMisc_STEP-Boots on Sand", 1.06, 3900, .16, .44),
        ("Mage", "FEETMisc_STEP-Sandals on Ground", .96, 2000, .22, .38),
    ]
    for hero, source, pitch, cutoff, duration, peak in profiles:
        for variant in range(1, 5):
            render(f"{source}_HY_PC-{variant:03}", f"{hero}_Step_{variant:02}",
                   pitch, cutoff, duration, peak)
    render("WHSH_MOVEMENT-Simple Whoosh_HY_PC-001", "Warrior_Dash", .82, 2300, .30, .58)
    render("WHSH_MOVEMENT-Simple Whoosh_HY_PC-003", "Archer_Dash", 1.16, 4600, .22, .47)
    render("WHSH_MOVEMENT-Windy Passby_HY_PC-001", "Mage_Dash", 1.02, 3200, .32, .43)
