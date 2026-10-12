import hashlib,json
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[2]
report=root/'Docs/UiRefinementRevision'
manifest=json.loads((report/'AvatarPrompts.json').read_text())
rows=[]
for entry in manifest:
    source=Path(entry['source']);asset=root/entry['asset']
    image=Image.open(asset)
    alpha=image.getchannel('A').getextrema() if image.mode=='RGBA' else None
    identical=hashlib.sha256(source.read_bytes()).digest()==hashlib.sha256(asset.read_bytes()).digest()
    rows.append({'id':entry['id'],'gender':entry['gender'],'asset':entry['asset'],'size':image.size,'alpha':alpha,'nativeBytesPreserved':identical,'sha256':hashlib.sha256(asset.read_bytes()).hexdigest()})
result={'passed':len(rows)==10 and all(r['nativeBytesPreserved'] and r['alpha']==(0,255) for r in rows),'mode':'built-in image_gen','male':sum(r['gender']=='Male' for r in rows),'female':sum(r['gender']=='Female' for r in rows),'avatars':rows}
(report/'art_verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='avatars'}))
if not result['passed']:raise SystemExit(1)
