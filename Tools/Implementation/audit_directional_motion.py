"""Fresh preservation comparison against the pre-animation project snapshot."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib,json,re,zipfile
ROOT=Path(__file__).resolve().parents[2];DOC=ROOT/'Docs/DirectionalMotion'
baseline=json.loads((DOC/'baseline.json').read_text(encoding='utf-8-sig'))['files']
changed=[p for p,h in baseline.items() if (ROOT/p).is_file() and hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=h]
missing=[p for p in baseline if not (ROOT/p).is_file()]
allowed={
 'Assets/Weedtopia/Game/Scenes/BurningHills.unity',
 'Assets/Weedtopia/Scripts/Runtime/Unity/LivingTownPresentation.cs',
 'Assets/Weedtopia/Scripts/Runtime/Unity/DeliveryView.cs',
 'Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs',
 'Assets/Weedtopia/Scripts/Editor/GameSetup/ProductionVisualPreflight.cs',
 'Assets/Weedtopia/Scripts/Tests/PlayMode/GameFlowTests.cs',
 'Assets/Weedtopia/Production/Prefabs/DeliveryTruck.prefab',
 *('Assets/Weedtopia/Production/LivingHills/Prefabs/Living_'+key+'_0.prefab' for key in ('resident_a','resident_b','resident_c','cyclist','swallow','butterfly'))}
def docs(s):return {m[2]:(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',s,re.M|re.S)}
scene='Assets/Weedtopia/Game/Scenes/BurningHills.unity'
with zipfile.ZipFile(DOC/'editable_originals.zip') as archive:
 old=docs(archive.read(scene).decode('utf-8-sig'))
new=docs((ROOT/scene).read_text(encoding='utf-8-sig'))
life_guid=re.search(r'guid: (\w+)',(ROOT/'Assets/Weedtopia/Scripts/Runtime/Unity/LivingTownPresentation.cs.meta').read_text())[1]
life_id=next(k for k,v in old.items() if v[0]==114 and 'guid: '+life_guid in v[1])
scene_changes=[k for k,v in old.items() if new.get(k)!=v]
unexpected_scene=[k for k in scene_changes if k!=life_id]
wrappers=[p for p in baseline if p.startswith('Assets/Weedtopia/Art/Buildings/Prefabs/') and p.endswith('.prefab')]
old_art=[p for p in baseline if p.lower().endswith(('.png','.jpg','.wav','.mp3','.fbx','.blend'))]
report=dict(utc=datetime.now(timezone.utc).isoformat(),baselineFiles=len(baseline),changedOriginalFiles=changed,missingFiles=missing,
 unexpectedOriginalChanges=[p for p in changed if p not in allowed],originalSceneBlocks=len(old),addedSceneBlocks=len(new)-len(old),
 changedSceneBlocks=scene_changes,unexpectedSceneChanges=unexpected_scene,missingSceneBlocks=[k for k in old if k not in new],
 preservedBuildingWrappers=len(wrappers)-len(set(wrappers)&set(changed+missing)),preservedOriginalArt=len(old_art)-len(set(old_art)&set(changed+missing)),
 gameplayAndLogisticsUnchanged=not any(any(s in p for s in ('/Runtime/Domain/','/Runtime/Application/','/Runtime/Persistence/','/Game/Content/','BurningHillsLayout.json')) for p in changed))
report['passed']=not (missing or report['unexpectedOriginalChanges'] or unexpected_scene or report['missingSceneBlocks']) and report['preservedBuildingWrappers']==251 and report['gameplayAndLogisticsUnchanged']
(DOC/'preservation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2));assert report['passed']
