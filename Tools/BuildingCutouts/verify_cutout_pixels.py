"""Read-only full-pixel/source-hash verification of the explicitly authorized 251 sprites."""
from pathlib import Path
from datetime import datetime,timezone
import json,hashlib
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
manifest=json.loads((ROOT/'Docs/LayoutRevision/Cutouts/manifest.json').read_text())
approved={'town_hall/T_FIXED':'Assets/Weedtopia/Art/Buildings/Sprites/town_hall/WT_town_hall_T_FIXED_2D_v1.png',**{'coffee_shop/'+t:'Assets/Weedtopia/Game/VisualReview/Sprites/coffee_shop_'+t+'.png' for t in ['T01_L001','T12_L600','T24_L5000']}}
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
checks=[]
for e in manifest['slots']:
    source=ROOT/e['source'];target=ROOT/e['sprite'];im=Image.open(target).convert('RGBA');p=np.asarray(im)
    assert sha(source)==e['sourceSha256'],e['key']+' source'
    assert sha(target)==e['spriteSha256'],e['key']+' sprite'
    assert list(im.size)==[e['width'],e['height']] and list(im.getchannel('A').getbbox())==e['alphaBounds']
    a=p[:,:,3];assert max(a[0].max(),a[-1].max(),a[:,0].max(),a[:,-1].max())<=8,e['key']+' edge'
    if e['key'] in approved:assert sha(target)==sha(ROOT/approved[e['key']]),e['key']+' approved original'
    else:
        rgb=np.asarray(Image.open(source).convert('RGB'));pad=e.get('canvasPadding',0)
        assert np.array_equal(p[pad:pad+rgb.shape[0],pad:pad+rgb.shape[1],:3],rgb),e['key']+' RGB'
    checks.append({'key':e['key'],'sha256':e['spriteSha256'],'sourceUnchanged':True,'approvedBytesOrSourceRgbPreserved':True,'boundsMatch':True,'visibleEdgeClear':True})
assert len(checks)==251
receipt={'utc':datetime.now(timezone.utc).isoformat(),'passed':True,'count':251,'approvedPrototypesByteIdentical':4,'remainingSourceRgbIdentical':247,'canvasPaddingCount':sum(bool(e.get('canvasPadding')) for e in manifest['slots']),'checks':checks}
(ROOT/'Docs/LayoutRevision/Cutouts/pixel_verification.json').write_text(json.dumps(receipt,indent=2))
print('Verified 251 hashes, dimensions, alpha bounds and visible edges; 4 approved PNGs byte-identical, 247 source RGB arrays identical.')
