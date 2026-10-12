import json,hashlib,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent
PROJECT=next(p for p in ROOT.parents if (p/'ProjectSettings').exists())
OUT=ROOT/'UnityReview'
OUT.mkdir(exist_ok=True)
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
baseline=OUT/'preservation_before.json'
if sys.argv[-1]=='before':
    files=[p for folder in ['Assets','ProjectSettings','Packages'] for p in (PROJECT/folder).rglob('*') if p.is_file() and p.suffix in ['.unity','.prefab','.asset','.meta','.json'] and 'Prototypes3D' not in p.parts]
    baseline.write_text(json.dumps({p.relative_to(PROJECT).as_posix():digest(p) for p in files},indent=2),encoding='utf-8')
    print('Recorded existing files',len(files))
else:
    data=json.loads(baseline.read_text())
    changed=[k for k,v in data.items() if not (PROJECT/k).is_file() or digest(PROJECT/k)!=v]
    result={'files_checked':len(data),'changed_or_missing':changed,'passed':not changed}
    (OUT/'preservation_after.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result,indent=2))
