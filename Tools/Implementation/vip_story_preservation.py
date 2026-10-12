"""Capture/compare the pre-existing authored scene, content, and protected asset references."""
import argparse, hashlib, json, pathlib, re

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/VipStories/Preservation'
SCENE = ROOT / 'Assets/Weedtopia/Game/Scenes/BurningHills.unity'
CONTENT = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'

def documents(text):
    return {m.group(1): m.group(0) for m in re.finditer(r'^--- !u!\d+ &(-?\d+).*?(?=^--- !u!|\Z)', text, re.M | re.S)}

def hash_file(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def capture():
    OUT.mkdir(parents=True, exist_ok=True)
    if (OUT / 'baseline.json').exists():
        raise RuntimeError('Preservation baseline already exists; do not overwrite it.')
    scene = SCENE.read_text(encoding='utf-8-sig')
    (OUT / 'BurningHills.before.unity.txt').write_text(scene, encoding='utf-8')
    (OUT / 'BurningHills.before.json').write_bytes(CONTENT.read_bytes())
    protected = []
    for folder in ['Assets/Weedtopia/Art/Buildings', 'Assets/Weedtopia/Production/Layout',
                   'Assets/Weedtopia/Production/Prefabs', 'Assets/Weedtopia/Production/TownVitality',
                   'Assets/Weedtopia/Production/WorldCorners', 'Assets/Weedtopia/Production/Conveyors',
                   'Assets/Weedtopia/Production/LuckyWheel', 'Assets/Weedtopia/Game/Audio', 'ProjectSettings']:
        protected += [p for p in (ROOT / folder).rglob('*') if p.is_file()]
    ids = documents(scene)
    (OUT / 'baseline.json').write_text(json.dumps({'originalObjects': len(ids),
        'protected': {p.relative_to(ROOT).as_posix(): hash_file(p) for p in protected}}, indent=2), encoding='utf-8')
    print(json.dumps({'baseline': 'captured', 'objects': len(ids), 'protectedFiles': len(protected)}))

def verify():
    before = json.loads((OUT / 'baseline.json').read_text(encoding='utf-8'))
    original = documents((OUT / 'BurningHills.before.unity.txt').read_text(encoding='utf-8'))
    current = documents(SCENE.read_text(encoding='utf-8-sig'))
    removed = sorted(set(original) - set(current))
    changed = {key: (original[key], current[key]) for key in original if key in current and original[key] != current[key]}
    # Only existing GameRoot/GameUi bindings, their owning transform, and root list may grow.
    root_guid = re.search(r'guid: (\w+)', (ROOT/'Assets/Weedtopia/Scripts/Runtime/Unity/GameRoot.cs.meta').read_text()).group(1)
    ui_guid = re.search(r'guid: (\w+)', (ROOT/'Assets/Weedtopia/Scripts/Runtime/Unity/GameUi.cs.meta').read_text()).group(1)
    allowed = {k for k, v in original.items() if root_guid in v or ui_guid in v or 'SceneRoots:' in v}
    ui_doc = next(v for v in original.values() if ui_guid in v)
    safe = re.search(r'safeArea: \{fileID: (-?\d+)\}', ui_doc).group(1)
    allowed.add(safe)
    journal = re.search(r'- id: journal\n\s+target: \{fileID: (-?\d+)\}', ui_doc).group(1)
    journal_transform = next(v for v in original.values() if 'RectTransform:' in v and f'm_GameObject: {{fileID: {journal}}}' in v)
    allowed.add(re.search(r'm_Father: \{fileID: (-?\d+)\}', journal_transform).group(1))
    journal_list = re.search(r'- id: journalList\n\s+target: \{fileID: (-?\d+)\}', ui_doc).group(1)
    allowed.update(k for k,v in original.items() if 'RectTransform:' in v and f'm_GameObject: {{fileID: {journal_list}}}' in v)
    added = set(current) - set(original)
    def without_additions(doc):
        doc = re.sub(r'^  (?:storyWorld|stories):.*\n', '', doc, flags=re.M)
        doc = re.sub(r'^  - id: (?:vipStories|storyJournal)\n    target:.*\n', '', doc, flags=re.M)
        doc = ''.join(line for line in doc.splitlines(True)
                      if not any(re.fullmatch(r'\s*- \{fileID: ' + key + r'\}\s*', line) for key in added))
        return doc.replace('  m_Children:\n  m_Father:', '  m_Children: []\n  m_Father:')
    unexpected = [k for k, (old, new) in changed.items() if k not in allowed or without_additions(new) != old]
    content_before = json.loads((OUT/'BurningHills.before.json').read_text(encoding='utf-8-sig'))
    content_after = json.loads(CONTENT.read_text(encoding='utf-8-sig'))
    for key in ['vipStories', 'vipCollaborations']:
        content_after.pop(key, None)
    content_after['texts'] = [t for t in content_after['texts'] if not t['key'].startswith('story.') and t['key'] != 'reward.source.vipstory']
    protected_changed = [p for p, h in before['protected'].items() if not (ROOT/p).is_file() or hash_file(ROOT/p) != h]
    result = {'status': 'passed' if not removed and not unexpected and not protected_changed and content_before == content_after else 'failed',
              'originalObjects': len(original), 'addedObjects': len(set(current)-set(original)),
              'removedOriginalObjects': removed, 'modifiedAllowedObjects': sorted(changed),
              'unexpectedModifiedObjects': unexpected, 'protectedAssetChanges': protected_changed,
              'existingContentPreserved': content_before == content_after}
    (OUT/'result.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
    if result['status'] != 'passed':
        raise SystemExit(1)

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['capture', 'verify'])
    args = parser.parse_args()
    (capture if args.mode == 'capture' else verify)()
