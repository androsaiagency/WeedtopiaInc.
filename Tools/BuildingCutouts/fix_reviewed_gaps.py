"""Alpha-only corrections for individually inspected negative spaces, never automatic white deletion."""
from pathlib import Path
from collections import defaultdict
from datetime import datetime,timezone
import json,hashlib
import numpy as np
from scipy import ndimage as nd
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];folder=ROOT/'Docs/LayoutRevision/Cutouts/WhiteGapReview'
candidates=json.loads((folder/'candidates.json').read_text())
# Reviewed in six full-art sheets and five detail sheets against the source images.
selected=set(range(3,21))|{22,44,45,46,59,60,61,62,63,66}
groups=defaultdict(list)
for i,e in enumerate(candidates):
    e['decision']='remove_confirmed_background_gap' if i in selected else 'preserve_authored_wall_or_specular_highlight'
    if i in selected:groups[e['sprite']].append(e)
manifest_path=ROOT/'Docs/LayoutRevision/Cutouts/manifest.json';manifest=json.loads(manifest_path.read_text());entries={e['sprite']:e for e in manifest['slots']}
changes=[]
for path,items in groups.items():
    target=ROOT/path;before=target.read_bytes();im=Image.open(target).convert('RGBA');p=np.array(im);rgb=p[:,:,:3].astype(int);remove=np.zeros(p.shape[:2],bool)
    for e in items:
        x0,y0,x1,y1=e['box'];roi=np.zeros(p.shape[:2],bool);roi[max(0,y0-8):y1+8,max(0,x0-8):x1+8]=True
        neutral=(rgb.min(2)>230)&((rgb.max(2)-rgb.min(2))<12)&roi
        seed=(rgb.min(2)>242)&((rgb.max(2)-rgb.min(2))<8)&(p[:,:,3]>240)&roi
        seed[:y0]=False;seed[y1:]=False;seed[:,:x0]=False;seed[:,x1:]=False
        remove|=nd.binary_propagation(seed,mask=neutral)
    if not remove.any():continue
    backup=folder/'Before'/entries[path]['key'];backup=backup.with_suffix('.png');backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists():backup.write_bytes(before)
    removal=nd.gaussian_filter(nd.binary_dilation(remove,iterations=1).astype(float),.55)
    alpha=np.minimum(p[:,:,3],np.clip((1-removal)*255,0,255).astype(np.uint8));alpha[alpha<12]=0;p[:,:,3]=alpha
    assert np.array_equal(p[:,:,:3],np.array(im)[:,:,:3]);Image.fromarray(p).save(target)
    entry=entries[path];entry.update(spriteSha256=hashlib.sha256(target.read_bytes()).hexdigest(),individualVisualReviewed=False,negativeSpaceCorrection='Individual source-confirmed white openings; alpha only; WhiteGapReview/decisions.json')
    assert list(Image.fromarray(alpha).getbbox())==entry['alphaBounds'],path
    changes.append({'key':entry['key'],'sprite':path,'beforeSha256':hashlib.sha256(before).hexdigest(),'afterSha256':entry['spriteSha256'],'alphaPixelsChanged':int(np.count_nonzero(alpha!=np.array(im)[:,:,3])),'rgbUnchanged':True})
manifest_path.write_text(json.dumps(manifest,indent=2))
(folder/'decisions.json').write_text(json.dumps({'utc':datetime.now(timezone.utc).isoformat(),'candidates':candidates,'changes':changes,'userAcceptance':False},indent=2))
for start in range(0,len(changes),8):
    sheet=Image.new('RGB',(1600,1000),(25,65,55));draw=ImageDraw.Draw(sheet)
    for i,e in enumerate(changes[start:start+8]):
        im=Image.open(ROOT/e['sprite']).convert('RGBA');bg=Image.new('RGBA',im.size,(45,115,75,255));bg.alpha_composite(im);bg.thumbnail((390,450));x=i%4*400;y=i//4*500;sheet.paste(bg,(x+(400-bg.width)//2,y));draw.text((x+5,y+465),e['key'],fill='white')
    sheet.save(folder/f'corrected_{start//8+1:02d}.png')
print(f'Corrected {len(changes)} sprites; {len(selected)} individually confirmed gaps. Original RGB unchanged.')
