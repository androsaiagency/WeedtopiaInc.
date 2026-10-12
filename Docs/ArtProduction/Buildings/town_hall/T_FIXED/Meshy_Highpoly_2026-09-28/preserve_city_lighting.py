import hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent
PROJECT=next(p for p in ROOT.parents if (p/'ProjectSettings').exists())
OUT=ROOT/'CityLighting'
BASELINE=OUT/'asset_hashes_before.json'
MODEL='Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928/WT_TownHall_LOD.prefab'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
if sys.argv[-1]=='before':
    if BASELINE.exists():raise SystemExit('Existing baseline preserved')
    files=[p for folder in ['Assets','ProjectSettings','Packages'] for p in (PROJECT/folder).rglob('*') if p.is_file() and p.suffix!='.cs']
    BASELINE.write_text(json.dumps({p.relative_to(PROJECT).as_posix():digest(p) for p in files},indent=2),encoding='utf-8')
    (OUT/'WT_TownHall_LOD_before.prefab').write_bytes((PROJECT/MODEL).read_bytes())
    print('Recorded',len(files),'existing assets/settings')
else:
    before=json.loads(BASELINE.read_text())
    changed=[k for k,v in before.items() if not (PROJECT/k).is_file() or digest(PROJECT/k)!=v]
    report=dict(checked=len(before),changed=changed,unexpected=[k for k in changed if k!=MODEL])
    report['passed']=not report['unexpected']
    (OUT/'preservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report,indent=2))
