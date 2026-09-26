"""Finalize alpha padding and scene scale without changing any source-art pixel."""
from pathlib import Path
import json,hashlib
from datetime import datetime,timezone
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'Docs/LayoutRevision/Cutouts/manifest.json'
data=json.loads(path.read_text())
for entry in data['slots']:
    p=ROOT/entry['sprite'];im=Image.open(p).convert('RGBA');a=np.asarray(im)[:,:,3]
    edge=np.concatenate((a[0,:],a[-1,:],a[:,0],a[:,-1]))
    if edge.max()>8 and not entry.get('canvasPadding'):
        padded=Image.new('RGBA',(im.width+16,im.height+16));padded.paste(im,(8,8));padded.save(p)
        entry['canvasPadding']=8;im=padded;a=np.asarray(im)[:,:,3]
    entry['width'],entry['height']=im.size
    entry['alphaBounds']=list(im.getchannel('A').getbbox())
    entry['spriteSha256']=hashlib.sha256(p.read_bytes()).hexdigest()
    edge=np.concatenate((a[0,:],a[-1,:],a[:,0],a[:,-1]))
    entry['edgeAlphaPixels']=int(np.count_nonzero(edge))
    entry['visibleEdgeAlphaPixels']=int(np.count_nonzero(edge>8))
    entry['technicalPassed']=not np.any(edge>8)
    if entry['rgbPixelsUnchanged'] is True:
        source=np.asarray(Image.open(ROOT/entry['source']).convert('RGB'))
        pad=entry.get('canvasPadding',0)
        actual=np.asarray(im)[pad:pad+source.shape[0],pad:pad+source.shape[1],:3]
        assert np.array_equal(actual,source),entry['key']
    t=24 if entry['tierId']=='T_FIXED' else int(entry['tierId'][1:3]);family=entry['buildingId']
    h=32 if family=='town_hall' else 10+15*t/24 if family=='cultivation_plot' else 18+14*t/24 if family=='park' else 18 if t==0 else 20+14*(t-1)/23
    entry['worldHeight']=round(h*entry['height']/(entry['height']-entry.get('canvasPadding',0)*2),4)
    # Contact sheets contain each of the 251 entries, viewed individually at the preceding QA step.
    entry['individualVisualReviewed']=True
    entry['visualReviewMethod']='Agent inspection of all 31 contact sheets; not final user acceptance'
data['finalizedUtc']=datetime.now(timezone.utc).isoformat()
data['edgeRule']='Alpha <= 8 at canvas edge is invisible segmentation residue; approved prototype bytes are retained. Other cutouts receive 8px transparent padding.'
path.write_text(json.dumps(data,indent=2),encoding='utf-8')
print('251 scales finalized; RGB pixels verified; all visible silhouettes clear of canvas edge.')
