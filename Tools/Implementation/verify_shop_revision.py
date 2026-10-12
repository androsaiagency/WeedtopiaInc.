"""Verify the additive shop catalog, object identities and protected artwork/audio against the owned baseline."""
import datetime
import hashlib
import json
import pathlib
import re
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/ShopRevision'
BASE = OUT / 'Preservation'
issues = []


def require(result, message):
    if not result:
        issues.append(message)
    return bool(result)


objects = {}
for relative in ['Assets/Weedtopia/Game/Scenes/BurningHills.unity',
                 'Assets/Weedtopia/Game/Scenes/BigRockCity.unity',
                 'Assets/Weedtopia/Production/Prefabs/WorldNavigationDock.prefab']:
    before = (BASE / (pathlib.Path(relative).name + '.before')).read_text(encoding='utf-8-sig')
    after = (ROOT / relative).read_text(encoding='utf-8-sig')
    ids = lambda text: set(re.findall(r'^--- !u!\d+ &(\d+)', text, re.M))
    old_ids, new_ids = ids(before), ids(after)
    objects[relative] = dict(before=len(old_ids), after=len(new_ids), retained=len(old_ids & new_ids), lost=sorted(old_ids - new_ids))
    require(old_ids <= new_ids, 'Existing serialized objects lost: ' + relative)

before = json.loads((BASE / 'BurningHills.json.before').read_text(encoding='utf-8-sig'))
after = json.loads((ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8-sig'))
approved = json.loads((OUT / 'catalog.json').read_text(encoding='utf-8-sig'))
require(after['shopProducts'] == approved and len(approved) == 17, 'Approved seventeen-product catalog changed')
require({k: v for k, v in before.items() if k not in ['texts', 'shopProducts']} ==
        {k: v for k, v in after.items() if k not in ['texts', 'shopProducts']}, 'Existing gameplay definitions changed')
old_texts = {t['key']: t for t in before['texts']}
new_texts = {t['key']: t for t in after['texts']}
require(all(new_texts.get(k) == v for k, v in old_texts.items()), 'Existing dialogue or quest text changed')
require(all(k.startswith('shop.') or k in ['reward.source.shop', 'reward.source.vipmembership']
            for k in new_texts.keys() - old_texts.keys()), 'Unexpected non-shop text added')
protected = json.loads((BASE / 'protected.json').read_text(encoding='utf-8'))
changed = [p['path'] for p in protected if not (ROOT / p['path']).is_file() or
           hashlib.sha256((ROOT / p['path']).read_bytes()).hexdigest() != p['sha256']]
require(not changed, 'Protected illustration/audio changed')
tests = {}
for name in ['EditMode', 'PlayMode', 'Assets']:
    path = OUT / 'Tests' / (name + '.xml')
    if require(path.is_file(), 'Missing ' + name + ' results'):
        run = ET.parse(path).getroot()
        tests[name] = {k: run.get(k) for k in ['result', 'total', 'passed', 'failed', 'start-time', 'end-time']}
        require(run.get('result') == 'Passed' and int(run.get('failed', 0)) == 0, name + ' tests failed')
capture = json.loads((OUT / 'capture_receipt.json').read_text(encoding='utf-8'))
require(capture['complete'] and not capture['issues'] and len(capture['screenshots']) == 96, 'Game View capture checks incomplete')
compile_receipt = json.loads((ROOT / 'Docs/Implementation/Validation/SourceCompile/result.json').read_text(encoding='utf-8'))
require(compile_receipt['status'] == 'passed', 'Source compilation failed')
for result in compile_receipt['results']:
    for path, sha in result['sources'].items():
        require(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == sha, 'Source changed after compile: ' + path)
receipt = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), passed=not issues,
               products=len(approved), serializedObjects=objects, protectedFiles=len(protected), changedProtectedFiles=changed,
               originalGameplayAndTextsPreserved=not any('gameplay' in i or 'text' in i for i in issues), tests=tests,
               compiledAssemblies=len(compile_receipt['results']), screenshots=len(capture['screenshots']), pointerAndLayoutChecks=capture['checks'],
               physicalDeviceVerified=False, userVisualApproval=False, liveBillingConnected=False, issues=issues)
(OUT / 'validation_receipt.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
print(json.dumps(receipt, indent=2))
raise SystemExit(0 if receipt['passed'] else 1)
