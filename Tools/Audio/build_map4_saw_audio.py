"""Build a seamless saw rasp from supplied metallic SFX; requires numpy."""
from pathlib import Path
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RATE = 44100
rng = np.random.default_rng(4510)


def read(name):
    path = ROOT / "Assets/Sprites/SFX" / (name + "_HY_PC-001.wav")
    with wave.open(str(path)) as wav:
        assert wav.getsampwidth() == 3
        raw = np.frombuffer(wav.readframes(wav.getnframes()), np.uint8).reshape(-1, 3)
        values = raw[:, 0].astype(np.int32) | raw[:, 1].astype(np.int32) << 8 | raw[:, 2].astype(np.int32) << 16
        values = ((values ^ 8388608) - 8388608).astype(float) / 8388608
        mono = values.reshape(-1, wav.getnchannels()).mean(axis=1)
        return np.interp(np.arange(round(len(mono) * RATE / wav.getframerate())) *
                         wav.getframerate() / RATE, np.arange(len(mono)), mono)


if __name__ == "__main__":
    textures = [read("DSGNMisc_HIT-Hit Rattle"), read("DSGNMisc_HIT-Mecha Geared Punch")]
    result = np.zeros(RATE)
    # Overlapping metallic grains create blade chatter, wrapped for a seamless loop.
    for index, start in enumerate(range(0, RATE, 735)):
        source = textures[index % len(textures)]
        peak = int(np.argmax(np.abs(source)))
        offset = max(0, peak - 220 + int(rng.integers(0, 350)))
        grain = source[offset:offset + 2205].copy()
        grain *= np.hanning(len(grain))
        result[(start + np.arange(len(grain))) % RATE] += grain * rng.uniform(.7, 1)
    frequencies = np.fft.rfftfreq(RATE, 1 / RATE)
    shaping = (frequencies / np.maximum(frequencies, 160)) ** 2
    shaping *= 1 / np.sqrt(1 + (frequencies / 4600) ** 8)
    result = np.fft.irfft(np.fft.rfft(result) * shaping, n=RATE)
    result -= result.mean()
    result *= .65 / max(np.max(np.abs(result)), .00001)
    assert np.isfinite(result).all() and np.max(np.abs(result)) < .7
    output = ROOT / "Assets/Audio/Traps/Map4Saw_GrindLoop.wav"
    output.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(output), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes((result * 32767).astype("<i2").tobytes())
    print(f"Wrote {output}: 1 second seamless metallic saw rasp")
