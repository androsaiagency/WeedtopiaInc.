"""Verify unchanged built-in ImageGen outputs and the title wordmark alpha channel."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path('C:/Users/Mein-PC/.codex/generated_images/01a10039-42fa-7ee1-a61e-434fc38f86f5')
pairs=[('exec-39738f94-f4bd-4e99-bd55-be19b5c92475.png','WeedtopiaTown-v1.png'),
       ('exec-a2f3badc-804f-4798-8fd4-3ab880dadfe8.png','WeedtopiaLogo-v1.png')]
assets=[]
for original,name in pairs:
    source=SOURCE/original
    target=ROOT/'Assets/Weedtopia/Production/Art/Title'/name
    image=Image.open(target)
    unchanged=source.read_bytes()==target.read_bytes()
    alpha=image.getchannel('A').getextrema() if 'A' in image.getbands() else None
    assets.append({'source':str(source),'asset':str(target.relative_to(ROOT)),'dimensions':image.size,
                   'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'unchanged':unchanged,'alphaRange':alpha})
passed=all(a['unchanged'] for a in assets) and assets[1]['alphaRange']==(0,255)
receipt={'generationMode':'built-in ImageGen','promptFile':'Docs/UiPolishRevision/ArtPrompts.md','assets':assets,'passed':passed}
(ROOT/'Docs/UiPolishRevision/art_verification.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print(json.dumps(receipt,indent=2))
if not passed:raise SystemExit(1)
