"""Copy original generated PNGs unchanged and record alpha bounds for Unity sprite import."""
import hashlib,json,shutil
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'Docs/TownVitalityRevision'
DEST=ROOT/'Assets/Weedtopia/Production/TownVitality'
(DEST/'Art').mkdir(parents=True,exist_ok=True)
sources=json.loads((REPORT/'generation_sources.json').read_text())
sprites=[]
for asset in sources['assets']:
    source=Path(asset['sourcePath']);target=DEST/'Art'/(asset['id']+'.png')
    digest=hashlib.sha256(source.read_bytes()).hexdigest()
    if target.exists() and hashlib.sha256(target.read_bytes()).hexdigest()!=digest:
        raise RuntimeError(f'Refusing to overwrite a different authored asset: {target}')
    shutil.copy2(source,target)
    with Image.open(target) as image:
        assert image.mode=='RGBA',f'{target}: expected actual alpha'
        alpha=image.getchannel('A');lo,hi=alpha.getextrema();assert lo==0 and hi>=250
        x0,y0,x1,y1=alpha.getbbox()
        assert 0 < (x1-x0)*(y1-y0) <= image.width*image.height
        sprites.append(dict(id=asset['id'],file=target.name,x=x0,y=image.height-y1,width=x1-x0,height=y1-y0,
                            worldHeight=asset['worldHeight'],foliage=asset['id'] in ['blossom_tree','rowan_tree','fern_garden','poppy_meadow','laundry_garden'],
                            imageWidth=image.width,imageHeight=image.height,sha256=digest))
(DEST/'art_manifest.json').write_text(json.dumps(dict(sprites=sprites),indent=2))
(REPORT/'alpha_validation.json').write_text(json.dumps(dict(passed=True,assetCount=len(sprites),assets=sprites),indent=2))
print(f'{len(sprites)} unchanged generated PNGs copied; real alpha and sprite rectangles verified.')
