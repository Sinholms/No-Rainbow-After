"""Generate an original, short spatial electrical discharge sample (no external assets)."""
import math
import random
import struct
import wave
from pathlib import Path

root=Path(__file__).resolve().parents[2]
target=root/'Assets/_NoRainbowAfter/Audio/SFX/AUD_C1_ElectricTick.wav'
rate=22050
rng=random.Random(41)
samples=[]
for i in range(int(rate*.10)):
    t=i/rate
    envelope=math.exp(-t*52)*(1 if i%811<370 else .2)
    value=(rng.uniform(-1,1)*.7+math.sin(2*math.pi*1730*t)*.3)*envelope*.55
    samples.append(struct.pack('<h',int(value*32767)))
with wave.open(str(target),'wb') as output:
    output.setnchannels(1)
    output.setsampwidth(2)
    output.setframerate(rate)
    output.writeframes(b''.join(samples))
print(target)
