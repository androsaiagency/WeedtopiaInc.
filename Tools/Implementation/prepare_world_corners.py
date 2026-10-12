"""Preserve imagegen PNGs byte-for-byte and record alpha bounds for Unity's sprite importer."""
import hashlib,json,shutil
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'Docs/WorldCornersRevision'
DEST=ROOT/'Assets/Weedtopia/Production/WorldCorners'
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
        assert image.mode=='RGBA',f'{target}: genuine alpha required'
        alpha=image.getchannel('A');lo,hi=alpha.getextrema();assert lo==0 and hi>=250
        x0,y0,x1,y1=alpha.getbbox()
        # No opaque rectangular background can pass this gate.
        assert sum(1 for a in alpha.getdata() if a==0)/float(image.width*image.height)>.12
        sprites.append(dict(id=asset['id'],file=target.name,x=x0,y=image.height-y1,width=x1-x0,height=y1-y0,
                            worldHeight=asset['worldHeight'],foliage=asset['id'] in ['orchard_apple','butterfly_garden'],
                            imageWidth=image.width,imageHeight=image.height,sha256=digest))
(DEST/'art_manifest.json').write_text(json.dumps(dict(sprites=sprites),indent=2))
(REPORT/'alpha_validation.json').write_text(json.dumps(dict(passed=True,assetCount=len(sprites),assets=sprites),indent=2))
print(f'{len(sprites)} original PNGs copied unchanged; alpha and Unity sprite bounds verified.')
