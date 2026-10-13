"""Snapshot the actual working checkout and verify the additive twelfth city."""
import hashlib, json, re, shutil, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'Docs/Skunkfurt/Preservation'
CONTENT = 'Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES = [f'Assets/Weedtopia/Game/Localization/{lang}.json' for lang in ('de','en','es','fr','ru')]
SAVE = 'Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs'
CAMPAIGN = 'Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab'
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def capture():
    FOLDER.mkdir(parents=True, exist_ok=True)
    manifest = FOLDER / 'baseline.json'
    if manifest.exists(): return {'status':'retained'}
    files = {p.relative_to(ROOT).as_posix():digest(p) for parent in ('Assets','ProjectSettings') for p in (ROOT/parent).rglob('*') if p.is_file() and 'Skunkfurt' not in p.as_posix()}
    important=[CONTENT,*LOCALES,SAVE,CAMPAIGN,'ProjectSettings/EditorBuildSettings.asset']
    important += [p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/Weedtopia/Game/Scenes').glob('*.unity')]
    important += [p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/Weedtopia/Scripts/Tests').rglob('*.cs')]
    for name in important:
        dest=FOLDER/'Before'/name; dest.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(ROOT/name,dest)
    manifest.write_text(json.dumps(files,indent=2),encoding='utf-8')
    return {'status':'captured','files':len(files)}
def additive(before,after,content=False):
    errors=[]
    for key,old in before.items():
        if key=='version': continue
        new=after.get(key)
        if isinstance(old,list) and old and isinstance(old[0],dict):
            identity='id' if 'id' in old[0] else 'key'
            current={item[identity]:item for item in new}
            for item in old:
                expected=dict(item)
                if content and key=='buildings' and item[identity]=='residential_house':
                    expected['residentTierBonuses']=list(item['residentTierBonuses']);expected['residentTierBonuses'][24]=1000
                if current.get(item[identity])!=expected:errors.append(f'{key}:{item[identity]}')
        elif old!=new: errors.append(key)
    return errors
def verify():
    baseline=read(FOLDER/'baseline.json')
    changed=[name for name,h in baseline.items() if (ROOT/name).exists() and digest(ROOT/name)!=h]
    removed=[name for name in baseline if not (ROOT/name).exists()]
    allowed={CONTENT,*LOCALES,SAVE,CAMPAIGN,'ProjectSettings/EditorBuildSettings.asset'}
    fixture_changes=[]
    for name in changed:
        if name.startswith('Assets/Weedtopia/Scripts/Tests/'):
            before=(FOLDER/'Before'/name).read_text(encoding='utf-8-sig'); after=(ROOT/name).read_text(encoding='utf-8-sig')
            recovered=after.replace('future.version = 14;', 'future.version = 13;')
            if name.endswith('CityProjectTests.cs'):
                recovered=recovered.replace('        // Whole-campaign simulations retain their tick limits; twelve cities need a larger wall-clock budget.\n        [Timeout(600000)]\n','')
            if name.endswith('GreatKushvilleCampaignTests.cs'):
                recovered=re.sub(r'        private static GameDefinitions VersionTwelveFixture\(GameDefinitions current\)\n        \{.*?\n        \}\n', '', recovered, flags=re.S)
                recovered=recovered.replace('defs = VersionTwelveFixture(ProgressionTests.Definitions());','defs = ProgressionTests.Definitions();')
            if name.endswith('GreatKushvilleAssetTests.cs'):
                recovered=recovered.replace('definitions.cities.Length, Is.GreaterThanOrEqualTo(11)','definitions.cities.Length, Is.EqualTo(11)')
                recovered=recovered.rstrip('\n');before=before.rstrip('\n')
            if name.endswith('GreatKushvillePlayModeTests.cs'):
                recovered=re.sub(r'            foreach \(var definition in root.Definitions.cities.Where\(destination => destination.introducedVersion > 12 && CityCampaignRules.HasCharter\(destination\)\)\)\n            \{.*?\n            \}\n            root.Session.State.revision\+\+;\n','',recovered,flags=re.S)
                recovered=recovered.replace('state.cities.Count, Is.EqualTo(root.Definitions.cities.Length)','cities.Length, Is.EqualTo(11)')
                recovered=recovered.replace('root.Ui.Campaign.travelRows.Length, Is.EqualTo(root.Definitions.cities.Length)','root.Ui.Campaign.travelRows.Length, Is.EqualTo(11)')
                recovered=recovered.replace('            if (root.SceneCityId != City) { Assert.That(root.TravelToCity(City), Is.EqualTo(Failure.None)); yield return WaitCity(City); root.Ui.Close(); }\n','')
            if recovered==before:
                allowed.add(name);fixture_changes.append(name)
    content_errors=additive(read(FOLDER/'Before'/CONTENT),read(ROOT/CONTENT),True)
    locale_errors={n:additive(read(FOLDER/'Before'/n),read(ROOT/n)) for n in LOCALES}
    migration=(ROOT/SAVE).read_text(encoding='utf-8-sig')==(FOLDER/'Before'/SAVE).read_text(encoding='utf-8-sig').replace('definitions.version > 12','definitions.version > 13')
    old_build=read_build(FOLDER/'Before/ProjectSettings/EditorBuildSettings.asset');new_build=read_build(ROOT/'ProjectSettings/EditorBuildSettings.asset')
    build_additive=all(item in new_build for item in old_build) and len(new_build)==len(old_build)+1 and any('Skunkfurt.unity' in item for item in new_build)
    result={'status':'passed','changed':changed,'removed':removed,'unexpectedChanges':sorted(set(changed)-allowed),'contentChangesToExisting':content_errors,'localeChangesToExisting':locale_errors,'migrationGuardOnly':migration,'buildSettingsAdditive':build_additive,'regressionFixtureChanges':fixture_changes,'userVisualApproval':False,'physicalTouch':False}
    if removed or result['unexpectedChanges'] or content_errors or any(locale_errors.values()) or not migration or not build_additive:result['status']='failed'
    (FOLDER/'preservation_receipt.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    return result
def read_build(p): return re.findall(r'  - enabled:.*?(?=\n  - enabled:|\n  m_configObjects:|\Z)',p.read_text(encoding='utf-8'),re.S)
if __name__=='__main__':
    result=capture() if sys.argv[1]=='capture' else verify();print(json.dumps(result,indent=2));sys.exit(0 if result['status'] in ('captured','retained','passed') else 1)
