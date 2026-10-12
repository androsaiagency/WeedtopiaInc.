"""Verify this scene extension retains every pre-existing serialized object and source image byte."""
import difflib,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'Docs/WorldCornersRevision'
scene=ROOT/'Assets/Weedtopia/Game/Scenes/BurningHills.unity'
def blocks(path):
    return {m.group(1):m.group(0) for m in re.finditer(r'^--- !u!\d+ &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',path.read_text(),re.M|re.S)}
before=blocks(REPORT/'Originals/BurningHills.unity');after=blocks(scene)
removed=sorted(before.keys()-after.keys())
changed=[k for k in before.keys()&after.keys() if before[k]!=after[k]]
assert not removed,removed
# The world's child list and existing shared actor array are the only integration points.
assert set(changed)=={'1911104533','352797082'},changed
changes=[]
for key in changed:
    matcher=difflib.SequenceMatcher(None,before[key].splitlines(),after[key].splitlines(),autojunk=False)
    edits=[(tag,i,j,a,b) for tag,i,j,a,b in matcher.get_opcodes() if tag!='equal']
    assert all(e[0]=='insert' for e in edits),(key,edits)
    changes.append(dict(objectId=key,onlyAdditions=True,insertedLines=sum(e[4]-e[3] for e in edits)))
manifest=json.loads((ROOT/'Assets/Weedtopia/Production/WorldCorners/art_manifest.json').read_text())
for item in manifest['sprites']:
    path=ROOT/'Assets/Weedtopia/Production/WorldCorners/Art'/item['file']
    assert hashlib.sha256(path.read_bytes()).hexdigest()==item['sha256']
result=dict(passed=True,originalObjects=len(before),addedObjects=len(after.keys()-before.keys()),removedOriginalObjects=0,
            existingObjectChanges=changes,unchangedGeneratedPngs=len(manifest['sprites']),sceneSha256=hashlib.sha256(scene.read_bytes()).hexdigest())
(REPORT/'preservation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
