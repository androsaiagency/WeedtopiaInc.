"""Two authored, original UI cue assets. Development audio, not a device-audio acceptance."""
import pathlib,wave,math,struct
root=pathlib.Path(__file__).resolve().parents[2]/'Assets/Weedtopia/Game/Audio'
root.mkdir(parents=True,exist_ok=True)
for name,frequency in [('accept',660),('blocked',220)]:
    path=root/f'development_{name}.wav'
    if path.exists():continue
    rate=44100;duration=.12
    data=b''.join(struct.pack('<h',int(4500*math.sin(2*math.pi*frequency*i/rate)*math.sin(math.pi*i/(rate*duration))**2)) for i in range(int(rate*duration)))
    with wave.open(str(path),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(data)
    print(path)
