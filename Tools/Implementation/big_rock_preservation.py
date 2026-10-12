"""Snapshot existing production assets before adding the second city; never edits originals."""
import hashlib, json, pathlib, sys, re

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/BigRockCity/Preservation'

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def capture():
    DOC.mkdir(parents=True, exist_ok=True)
    paths = [p for folder in ['Assets/Weedtopia/Art', 'Assets/Weedtopia/Production',
                             'Assets/Weedtopia/Game/Audio', 'Assets/Weedtopia/Game/Content']
             for p in (ROOT/folder).rglob('*') if p.is_file()]
    files = {p.relative_to(ROOT).as_posix(): digest(p) for p in paths
             if 'BigRockCity' not in p.as_posix()}
    scene = ROOT/'Assets/Weedtopia/Game/Scenes/BurningHills.unity'
    (DOC/'BurningHills.before.unity.txt').write_bytes(scene.read_bytes())
    content = ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json'
    (DOC/'BurningHills.v2.json').write_bytes(content.read_bytes())
    (DOC/'baseline.json').write_text(json.dumps(files, indent=2), encoding='utf-8')
    print('Captured', len(files), 'existing asset hashes and the production scene.')

def verify():
    baseline = json.loads((DOC/'baseline.json').read_text(encoding='utf-8'))
    allowed = {'Assets/Weedtopia/Game/Content/BurningHills.json'}
    changed = [p for p,h in baseline.items() if p not in allowed and
               (not (ROOT/p).exists() or digest(ROOT/p) != h)]
    before = json.loads((DOC/'BurningHills.v2.json').read_text(encoding='utf-8'))
    after = json.loads((ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8'))
    unchanged = all(after[k] == v for k,v in before.items()
                    if k not in {'version','instances','routes','quests','texts','vipStories','vipCollaborations'})
    for key in ['instances','routes','quests','texts','vipStories','vipCollaborations']:
        original = before[key]
        current = [dict(x) for x in after[key][:len(original)]]
        if key == 'quests':
            for q in current: q.pop('cityId', None)
        unchanged &= current == original
    scene = verify_scene()
    result = {'status': 'passed' if not changed and unchanged and scene['passed'] else 'failed',
              'protectedAssets': len(baseline)-1, 'changedProtectedAssets': changed,
              'existingContentPreserved': unchanged, 'originalScene': scene}
    (DOC/'result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
    if result['status'] != 'passed': raise SystemExit(1)

def verify_scene():
    def documents(path):
        text = path.read_text(encoding='utf-8-sig')
        return {m.group(1): m.group(0) for m in re.finditer(
            r'^--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)', text, re.M|re.S)}
    before = documents(DOC/'BurningHills.before.unity.txt')
    after = documents(ROOT/'Assets/Weedtopia/Game/Scenes/BurningHills.unity')
    added = after.keys()-before.keys()
    unexpected = []
    permitted = {'29198349','114684939','441830748','467636740','1003569181','1602684457','1790791983'}
    for identifier, original in before.items():
        current = after.get(identifier, '')
        if current == original: continue
        if identifier not in permitted:
            unexpected.append(identifier); continue
        # Only explicit campaign controls, a Rocko child sign, and their default scene fields may be added.
        if identifier == '29198349':
            current = re.sub(r'^  - id: city(?:Campaign|Travel|Journal)\n    target: \{fileID: (-?\d+)\}\n',
                lambda m: '' if m.group(1) in added else m.group(0), current, flags=re.M)
            current = re.sub(r'^  campaign: \{fileID: (-?\d+)\}\n',
                lambda m: '' if m.group(1) in added else m.group(0), current, flags=re.M)
        if identifier in {'29198349','467636740','1790791983'}:
            current = re.sub(r'^  - \{fileID: (-?\d+)\}\n',
                lambda m: '' if m.group(1) in added else m.group(0), current, flags=re.M)
        if identifier in {'114684939','1602684457'}:
            current = re.sub(r'    m_AddedGameObjects:\n    - targetCorrespondingSourceObject: \{[^\n]+\}\n      insertIndex: -1\n      addedObject: \{fileID: (-?\d+)\}\n',
                lambda m: '    m_AddedGameObjects: []\n' if m.group(1) in added else m.group(0), current)
        if identifier == '441830748': current = current.replace('  cityLabel: Burning Hills\n','')
        if identifier == '1003569181': current = current.replace('  sceneCityId: burning_hills\n','')
        if current != original: unexpected.append(identifier)
    removed = sorted(before.keys()-after.keys())
    return {'passed': not removed and not unexpected, 'preservedOriginalObjects':len(before),
            'addedDocuments':len(added),'removedObjects':removed,'unexpectedChanges':unexpected,
            'permittedAdditions':'Campaign panel, header and journal links, Rocko quest sign, default city labels'}

if __name__ == '__main__':
    capture() if sys.argv[1] == 'capture' else verify()
