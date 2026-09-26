"""Verify original preservation and bind current source/content hashes to fresh evidence."""
import datetime
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Implementation/Validation'
baseline = json.loads((OUT / 'm0_baseline.json').read_text(encoding='utf-8-sig'))
allowed = {
    'Assets/Weedtopia/Art/Buildings/Sprites/town_hall/WT_town_hall_T_FIXED_2D_v1.png.meta': 'Repair sprite import type/settings for isolated prototype',
    'Assets/Weedtopia/Scripts/Editor/BuildingArtPipeline/BuildingSpritePrototypeUtility.cs': 'Correct importer setting order and preserve existing prototype GUID',
    'ProjectSettings/EditorBuildSettings.asset': 'Add Bootstrap and BurningHills, preserve SampleScene',
    'Docs/ArtProduction/PROGRESS.json': 'Record fresh prototype evidence and keep explicit user approval pending',
}
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

changed, missing, wrappers = [], [], []
for name, before in baseline['hashes'].items():
    path = ROOT / name
    if not path.exists():
        missing.append(name)
        continue
    after = digest(path)
    if after != before:
        changed.append(dict(path=name, before=before, after=after, reason=allowed.get(name)))
    if name.startswith('Assets/Weedtopia/Art/Buildings/Prefabs/') and path.suffix == '.prefab':
        wrappers.append(dict(path=name, unchanged=before == after))
unexpected = [x['path'] for x in changed if not x['reason']]
receipt = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), baselineFiles=len(baseline['hashes']),
               changed=changed, missing=missing, unexpected=unexpected, wrappers=wrappers,
               productionWrappersUnchanged=all(x['unchanged'] for x in wrappers),
               status='passed' if not missing and not unexpected and len(wrappers) == 251 else 'failed')
(OUT / 'preservation_final.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')

sources = sorted((ROOT / 'Assets/Weedtopia/Scripts').rglob('*.cs'))
sources += sorted((ROOT / 'Assets/Weedtopia/Scripts').rglob('*.asmdef'))
sources += [ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json']
latest_source = max(path.stat().st_mtime for path in sources)
test_receipts = []
for mode in ('EditMode', 'PlayMode'):
    candidates = []
    for path in OUT.glob('*_' + mode + '_*.json'):
        item = json.loads(path.read_text(encoding='utf-8-sig'))
        if 'startedUtc' in item:
            candidates.append(item)
    item = max(candidates, key=lambda x: x['startedUtc'])
    result = ET.parse(item['xml']).getroot()
    current = datetime.datetime.fromisoformat(item['startedUtc'].replace('Z', '+00:00')).timestamp() >= latest_source
    if item['status'] != 'passed' or result.attrib['result'] != 'Passed' or not current:
        raise RuntimeError('Missing passing current ' + mode + ' evidence')
    test_receipts.append(dict(run=item['run'], start=item['startedUtc'], total=int(result.attrib['total']),
                              passed=int(result.attrib['passed']), currentSourcesPredateRun=current,
                              xmlSha256=digest(Path(item['xml']))))
source_compile = json.loads((OUT / 'SourceCompile/result.json').read_text())
for assembly in source_compile['results']:
    for name, hash_value in assembly['sources'].items():
        if digest(ROOT / name) != hash_value:
            raise RuntimeError('Source compilation is stale: ' + name)
manifest = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), tests=test_receipts,
                sourceCompileAssemblies=len(source_compile['results']),
                sourceHashes={p.relative_to(ROOT).as_posix(): digest(p) for p in sources},
                status='passed', note='Hash snapshot plus source timestamps; test XML records real execution, not a substitute for manual gates')
(OUT / 'evidence_manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(json.dumps(dict(preservation=receipt['status'], baselineFiles=receipt['baselineFiles'],
                     changed=len(changed), missing=len(missing), wrappers=len(wrappers), tests=test_receipts), indent=2))
if receipt['status'] != 'passed':
    raise SystemExit(1)
