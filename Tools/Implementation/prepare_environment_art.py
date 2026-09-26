"""Measure intact generated alpha for Unity sprite rectangles; never alter image pixels."""
from pathlib import Path
from datetime import datetime,timezone
import json,hashlib
import numpy as np
from PIL import Image
from scipy.ndimage import label,find_objects

ROOT=Path(__file__).resolve().parents[2]
folder=ROOT/'Assets/Weedtopia/Production/Environment'
specs={
 'Woodland': [('oak',20,True),('birch',22,True),('maple',19,True),('willow',22,True)],
 'Gardens': [('hydrangea',6,True),('flowering_shrub',6,True),('rocks',5,False),('reeds',7,True),
             ('wildflowers',3.5,True),('lavender',4.5,True),('stone_wall',5,False),('hedge',5.5,True),
             ('benches',4.5,False),('pergola',10,False),('footbridge',7,False),('fountain',10,False)]}
entries=[];sources=[]
for name,items in specs.items():
 p=folder/'Art'/f'{name}.png';im=Image.open(p);a=np.asarray(im)[:,:,3];labs,_=label(a>40)
 components=[]
 for i,s in enumerate(find_objects(labs),1):
  if int((labs[s]==i).sum())>1000:components.append([s[1].start,s[0].start,s[1].stop,s[0].stop])
 assert len(components)==len(items),(name,components)
 cols=2 if name=='Woodland' else 4
 components.sort(key=lambda b:(int(((b[1]+b[3])*.5)/im.height*(len(items)//cols)),(b[0]+b[2])*.5))
 for spec,box in zip(items,components):
  x0,y0,x1,y1=box;x0=max(0,x0-5);y0=max(0,y0-5);x1=min(im.width,x1+5);y1=min(im.height,y1+5)
  entries.append(dict(file=p.name,id=spec[0],x=x0,y=im.height-y1,width=x1-x0,height=y1-y0,worldHeight=spec[1],foliage=spec[2]))
 sources.append(dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),size=list(im.size),alphaPreserved=True,agentAtlasReviewed=True))
p=folder/'Art/Ground.png';im=Image.open(p);sources.append(dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),size=list(im.size),agentAtlasReviewed=True))
(folder/'art_manifest.json').write_text(json.dumps({'sprites':entries},indent=2),encoding='utf-8')
(ROOT/'Docs/EnvironmentRevision/art_provenance.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'tool':'built-in image_gen','sources':sources,'pixelsEdited':False,'method':'Unmodified generated atlases; connected alpha components measured for serialized Unity import rectangles.','userAcceptance':False},indent=2),encoding='utf-8')
print('Measured',len(entries),'sprite rectangles. Original generated pixel and alpha channels unchanged.')
