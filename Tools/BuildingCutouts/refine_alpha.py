"""Color-guided alpha correction; original RGB is never modified. Explicitly authorized local workflow."""
from pathlib import Path
import json, hashlib, os, argparse
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd

ROOT=Path(__file__).resolve().parents[2]
os.environ['REMBG_HOME']=str(ROOT/'Tools/BuildingCutouts/models')
os.environ['OMP_NUM_THREADS']='4'
os.environ['NUMBA_CACHE_DIR']=str(ROOT/'Tools/BuildingCutouts/cache')
from rembg import new_session
session=new_session('isnet-general-use',providers=['CPUExecutionProvider'])
path=ROOT/'Docs/LayoutRevision/Cutouts/manifest.json'
data=json.loads(path.read_text())
parser=argparse.ArgumentParser();parser.add_argument('--families',nargs='*');args=parser.parse_args()
for e in data['slots']:
    if args.families and e['buildingId'] not in args.families:continue
    if e['method'].startswith('byte_identical'):continue
    target=ROOT/e['sprite'];image=Image.open(ROOT/e['source']).convert('RGBA');p=np.array(image)
    cache=ROOT/'Tools/BuildingCutouts/masks'/e['key'];cache=cache.with_suffix('.png');cache.parent.mkdir(parents=True,exist_ok=True)
    if not cache.exists():session.predict(image.convert('RGB'))[0].save(cache)
    a=np.array(Image.open(cache));rgb=p[:,:,:3].astype(np.int16)
    # Cream backdrops and their neutral shadows are separable from the source's colored surfaces.
    # Restore dark doors/plants misclassified as holes and solidify semitransparent roofs.
    color=(rgb.max(2)-rgb.min(2)>32)&(rgb.min(2)<210)
    dark=rgb.max(2)<155
    silhouette=nd.binary_fill_holes(a>8)
    candidate=(a>128)|((color|dark)&silhouette)
    labels,count=nd.label(candidate)
    sizes=np.bincount(labels.ravel());keep=sizes>120
    keep[0]=False;candidate=keep[labels]
    # Fill enclosed dark apertures while leaving the near-white gaps between pipes transparent.
    inner=nd.binary_erosion(candidate,iterations=1)
    boundary=np.clip(nd.gaussian_filter(candidate.astype(float),.55)*255,0,255).astype(np.uint8)
    boundary[inner]=255;boundary[boundary<12]=0
    p[:,:,3]=boundary;Image.fromarray(p).save(target)
    assert np.array_equal(p[:,:,:3],np.array(Image.open(ROOT/e['source']).convert('RGB')))
    e.update(spriteSha256=hashlib.sha256(target.read_bytes()).hexdigest(),alphaBounds=list(Image.fromarray(boundary).getbbox()),
        method='local_isnet_plus_color_guided_alpha_correction',rgbPixelsUnchanged=True,individualVisualReviewed=False)
    e['edgeAlphaPixels']=int(np.count_nonzero(boundary[0])+np.count_nonzero(boundary[-1])+np.count_nonzero(boundary[:,0])+np.count_nonzero(boundary[:,-1]))
    e['technicalPassed']=e['edgeAlphaPixels']==0
    print(e['key'],flush=True)
path.write_text(json.dumps(data,indent=2),encoding='utf-8')
print('Alpha correction complete; review required.')
