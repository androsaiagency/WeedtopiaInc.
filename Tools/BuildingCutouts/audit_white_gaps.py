"""Find neutral near-white opaque islands for manual alpha-only review; never change art."""
from pathlib import Path
import json
import numpy as np
from scipy import ndimage as nd
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[2]
folder=ROOT/'Docs/LayoutRevision/Cutouts/WhiteGapReview';folder.mkdir(exist_ok=True)
slots=json.loads((ROOT/'Docs/LayoutRevision/Cutouts/manifest.json').read_text())['slots']
candidates=[]
for e in slots:
    if e['method'].startswith('byte_identical'):continue
    im=Image.open(ROOT/e['sprite']).convert('RGBA');a=np.array(im);rgb=a[:,:,:3].astype(int)
    mask=(rgb.min(2)>242)&((rgb.max(2)-rgb.min(2))<8)&(a[:,:,3]>240)
    labels,count=nd.label(mask);sizes=np.bincount(labels.ravel())
    for i in np.flatnonzero(sizes>350):
        if i==0:continue
        ys,xs=np.where(labels==i);box=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)]
        candidates.append({'key':e['key'],'sprite':e['sprite'],'box':box,'pixels':int(sizes[i])})
for start in range(0,len(candidates),12):
    sheet=Image.new('RGB',(1800,1200),(40,65,62));d=ImageDraw.Draw(sheet)
    for j,e in enumerate(candidates[start:start+12]):
        im=Image.open(ROOT/e['sprite']).convert('RGBA');back=Image.new('RGBA',im.size,(40,110,100,255));back.alpha_composite(im)
        draw=ImageDraw.Draw(back);draw.rectangle(e['box'],outline=(255,0,180),width=5)
        back.thumbnail((435,340));x=j%4*450;y=j//4*400;sheet.paste(back,(x+(450-back.width)//2,y));d.text((x+6,y+350),str(start+j)+' '+e['key'],fill='white');d.text((x+6,y+370),str(e['box'])+' '+str(e['pixels']),fill='white')
    sheet.save(folder/f'candidates_{start//12+1:02d}.png')
(folder/'candidates.json').write_text(json.dumps(candidates,indent=2))
print(json.dumps(candidates,indent=2))
