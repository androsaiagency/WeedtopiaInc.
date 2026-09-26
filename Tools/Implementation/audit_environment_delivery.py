"""Read-only preservation comparison against the baseline captured for the natural landscape revision."""
from pathlib import Path
from datetime import datetime,timezone
import hashlib,json,re,zipfile,collections
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/EnvironmentRevision'
before=json.loads((OUT/'baseline.json').read_text())['files']
missing=[];changed=[]
for name,digest in before.items():
 p=ROOT/name
 if not p.is_file():missing.append(name)
 elif hashlib.sha256(p.read_bytes()).hexdigest()!=digest:changed.append(name)
allowed={'Assets/Weedtopia/Game/Scenes/BurningHills.unity','Assets/Weedtopia/Scripts/Editor/GameSetup/LayoutVisualEvidence.cs'}
unexpected=[p for p in changed if p not in allowed]
with zipfile.ZipFile(OUT/'editable_originals.zip') as archive:
 original_scene=archive.read(next(n for n in archive.namelist() if n.endswith('/BurningHills.unity'))).decode('utf-8-sig')
def documents(text):
 return {m.group(2):(int(m.group(1)),m.group(3).strip()) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
old=documents(original_scene);new=documents((ROOT/'Assets/Weedtopia/Game/Scenes/BurningHills.unity').read_text(encoding='utf-8-sig'))
scene_changes=[(key,value[0]) for key,value in old.items() if new.get(key)!=value]
protected_changes=[key for key,kind in scene_changes if kind in (114,1001)]
missing_scene_objects=[key for key in old if key not in new]
result={'utc':datetime.now(timezone.utc).isoformat(),'baselineFiles':len(before),'missing':missing,'changedExistingFiles':changed,'unexpectedChanges':unexpected,'allOriginalArtAndBuildingWrappersUnchanged':not unexpected and not missing,'sceneChangedIntentionally':True,'originalSceneDocuments':len(old),'protectedGameplayComponentsAndPrefabInstances':sum(v[0] in (114,1001) for v in old.values()),'protectedSceneChanges':protected_changes,'missingOriginalSceneObjects':missing_scene_objects,'changedOriginalSceneTypes':dict(collections.Counter(kind for _,kind in scene_changes))}
(OUT/'preservation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
assert not missing and not unexpected and not protected_changes and not missing_scene_objects
