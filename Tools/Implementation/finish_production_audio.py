"""Non-destructive mastering of original local Stable Audio source clips."""
from pathlib import Path
import json,hashlib
import numpy as np
import soundfile as sf
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'Assets/Audio/Source/BurningHills';out=ROOT/'Assets/Audio/Approved/BurningHills';out.mkdir(parents=True,exist_ok=True)
records=[]
for i,name in enumerate(('confirm','unavailable','upgrade','delivery')):
    path=source/f'action_{i}.wav';x,sr=sf.read(path,always_2d=True)
    active=np.where(np.max(np.abs(x),axis=1)>.005)[0]
    if not len(active):raise ValueError('Silent generated audio: '+name)
    x=x[max(0,active[0]-int(.02*sr)):min(len(x),active[-1]+int(.08*sr))]
    fade=min(int(.012*sr),len(x)//4);x[:fade]*=np.linspace(0,1,fade)[:,None];x[-fade:]*=np.linspace(1,0,fade)[:,None]
    peak=np.max(np.abs(x));x*=min(1,.7/peak)
    target=out/(name+'.wav');sf.write(target,x,sr,subtype='PCM_24')
    records.append(dict(id=name,source=str(path.relative_to(ROOT)),output=str(target.relative_to(ROOT)),sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),sampleRate=sr,samples=len(x),duration=len(x)/sr,channels=x.shape[1],peak=float(np.max(np.abs(x))),rms=float(np.sqrt(np.mean(x*x))),model='small-sfx',seed=420927,steps=8,cfg=1,device='cuda',auditioned=False))
x,sr=sf.read(out/'afterglow_loop_60s.wav',always_2d=True)
assert sr==44100 and len(x)==2646000 and x.shape[1]==2
records.append(dict(id='afterglow_music',source='Assets/Audio/Source/BurningHills/afterglow_62s.wav',output='Assets/Audio/Approved/BurningHills/afterglow_loop_60s.wav',sampleRate=sr,samples=len(x),duration=60,channels=2,subtype='PCM_24',peak=float(np.max(np.abs(x))),rms=float(np.sqrt(np.mean(x*x))),boundaryJump=float(np.max(np.abs(x[-1]-x[0]))),model='small-music',seed=420926,steps=8,cfg=1,device='cuda',auditioned=False))
(ROOT/'Docs/ProductionFinish/audio_provenance.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records,indent=2))
