"""Capture the working checkout, then audit only the intended eleventh-city additions."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

from great_kushville_prefab_preservation import audit as strict_prefab_audit

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'Docs/GreatKushville/Preservation'
CONTENT = 'Assets/Weedtopia/Game/Content/BurningHills.json'
LOCALES = [f'Assets/Weedtopia/Game/Localization/{lang}.json' for lang in ('de', 'en', 'es', 'fr', 'ru')]
SAVE = 'Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs'
CAMPAIGN = 'Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab'

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))

def capture():
    FOLDER.mkdir(parents=True, exist_ok=True)
    manifest = FOLDER / 'baseline.json'
    if manifest.exists():
        return {'status': 'retained', 'baseline': str(manifest)}
    files = {p.relative_to(ROOT).as_posix(): digest(p)
             for parent in ('Assets', 'ProjectSettings') for p in (ROOT / parent).rglob('*')
             if p.is_file() and 'GreatKushville' not in p.as_posix()}
    important = [CONTENT, *LOCALES, SAVE, CAMPAIGN, 'ProjectSettings/EditorBuildSettings.asset']
    important += [p.relative_to(ROOT).as_posix() for p in (ROOT / 'Assets/Weedtopia/Game/Scenes').glob('*.unity')]
    for name in important:
        backup = FOLDER / 'Before' / name
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / name, backup)
    manifest.write_text(json.dumps(files, indent=2), encoding='utf-8')
    return {'status': 'captured', 'files': len(files)}

def additive(before, after, content=False):
    changes = []
    for key, old in before.items():
        if content and key == 'version':
            continue
        new = after.get(key)
        if isinstance(old, list) and old and isinstance(old[0], dict):
            identity = 'id' if 'id' in old[0] else 'key'
            current = {x[identity]: x for x in new}
            for item in old:
                expected = dict(item)
                if content and key == 'buildings' and item[identity] == 'residential_house':
                    expected['residentTierBonuses'] = list(item['residentTierBonuses'])
                    expected['residentTierBonuses'][-1] = 475
                if current.get(item[identity]) != expected:
                    changes.append(f'{key}:{item[identity]}')
        elif old != new:
            changes.append(key)
    return changes

def documents(p):
    return {m.group(1): m.group(0) for m in re.finditer(
        r'^--- !u!\d+ &(-?\d+).*?(?=^--- !u!|\Z)', p.read_text(encoding='utf-8'), re.M | re.S)}


def appended_travel_row(before, after, old, new, ui_guid, row_guid):
    """Only CityCampaignUi.travelRows may grow by its one connected new city row."""
    if not re.search(r"m_Script: \{[^\n]*guid: " + re.escape(ui_guid), before):
        return False
    pattern = r"\n  travelRows:.*?(?=\n  [A-Za-z_][^:\n]*:|\Z)"
    a, b = re.search(pattern, before, re.S), re.search(pattern, after, re.S)
    if not a or not b:
        return False
    ai, bi = re.findall(r"fileID: (-?\d+)", a.group()), re.findall(r"fileID: (-?\d+)", b.group())
    if len(ai) != 10 or len(bi) != 11 or bi[:-1] != ai or bi[-1] not in new.keys() - old.keys():
        return False
    row = new[bi[-1]]
    if not re.search(r"m_Script: \{[^\n]*guid: " + re.escape(row_guid), row) or "\n  cityId: great_kushville\n" not in row:
        return False
    return re.sub(pattern, "\n  travelRows: VERIFIED", before, flags=re.S) == re.sub(pattern, "\n  travelRows: VERIFIED", after, flags=re.S)



def regression_fixture_changes(baseline):
    """Recover baseline hashes by reversing only the approved version fixtures."""
    golden = 'Assets/Weedtopia/Scripts/Tests/EditMode/GoldenFlowerCampaignTests.cs'
    mary = 'Assets/Weedtopia/Scripts/Tests/EditMode/SaintMaryVilleCampaignTests.cs'
    current = (ROOT / golden).read_bytes()
    future = b'future.version = 13;'
    restored = current.replace(future, b'future.version = 12;')
    golden_ok = current.count(future) == 1 and hashlib.sha256(restored).hexdigest() == baseline[golden]
    text = (ROOT / mary).read_text(encoding='utf-8-sig')
    expected_fixture = """        private static GameDefinitions VersionElevenFixture(GameDefinitions current)
        {
            // Keep the original tenth-city and density migration contract executable after later cities are added.
            var previous = JsonUtility.FromJson<GameDefinitions>(JsonUtility.ToJson(current)); previous.version = 11;
            previous.Building("residential_house").residentTierBonuses[24] = 135;
            previous.cities = previous.cities.Where(c => c.introducedVersion <= 11).ToArray();
            previous.instances = previous.instances.Where(i => i.introducedVersion <= 11).ToArray();
            var ids = previous.instances.Select(i => i.id).ToHashSet();
            previous.routes = previous.routes.Where(r => ids.Contains(r.from) && ids.Contains(r.to)).ToArray();
            var cities = previous.cities.Select(c => c.id).ToHashSet();
            previous.quests = previous.quests.Where(q => cities.Contains(string.IsNullOrEmpty(q.cityId) ? "burning_hills" : q.cityId)).ToArray();
            previous.vipStories = previous.vipStories.Where(p => cities.Contains(p.cityId)).ToArray();
            previous.vipCollaborations = previous.vipCollaborations.Where(p => cities.Contains(p.cityId)).ToArray();
            previous.Validate(); return previous;
        }
"""
    setup = 'defs = VersionElevenFixture(ProgressionTests.Definitions());'
    fixture_ok = text.count(expected_fixture) == 1 and text.count(setup) == 1 and text.count('future.version = 13;') == 1
    restored_text = text.replace(expected_fixture, '').replace(setup, 'defs = ProgressionTests.Definitions();').replace('future.version = 13;', 'future.version = 12;')
    # Text edits may normalize line endings; require recovery of the original full-file digest.
    restored_bytes = [restored_text.encode('utf-8'), restored_text.replace('\n', '\r\n').encode('utf-8')]
    restored_bytes += [b'\xef\xbb\xbf' + value for value in restored_bytes]
    mary_ok = fixture_ok and any(hashlib.sha256(value).hexdigest() == baseline[mary] for value in restored_bytes)
    return {
        golden: {'passed': golden_ok, 'allowedChanges': ['Exact unsupported future version 12 -> 13'], 'baselineHashRecovered': golden_ok},
        mary: {'passed': mary_ok, 'allowedChanges': ['Exact version-eleven fixture and setup binding', 'Exact unsupported future version 12 -> 13'], 'baselineHashRecovered': mary_ok}
    }


def verify():
    baseline = read(FOLDER / 'baseline.json')
    changed = [n for n, h in baseline.items() if (ROOT / n).exists() and digest(ROOT / n) != h]
    removed = [n for n in baseline if not (ROOT / n).exists()]
    scenes = [n for n in baseline if n.startswith('Assets/Weedtopia/Game/Scenes/') and n.endswith('.unity')]
    allowed = {CONTENT, *LOCALES, SAVE, CAMPAIGN, 'ProjectSettings/EditorBuildSettings.asset', *scenes,
               'Assets/Weedtopia/Scripts/Tests/EditMode/SaintMaryVilleCampaignTests.cs',
               'Assets/Weedtopia/Scripts/Tests/EditMode/GoldenFlowerCampaignTests.cs',
               'Assets/Weedtopia/Scripts/Tests/EditorAssets/SaintMaryVilleAssetTests.cs',
               'Assets/Weedtopia/Scripts/Tests/PlayMode/SaintMaryVillePlayModeTests.cs'}
    regression_changes = regression_fixture_changes(baseline)
    content = additive(read(FOLDER / 'Before' / CONTENT), read(ROOT / CONTENT), True)
    locales = {n: additive(read(FOLDER / 'Before' / n), read(ROOT / n)) for n in LOCALES}
    migration = (ROOT / SAVE).read_text(encoding='utf-8-sig') == (FOLDER / 'Before' / SAVE).read_text(encoding='utf-8-sig').replace('definitions.version > 11', 'definitions.version > 12')
    ui_guids = [re.search(r'guid: (\w+)', (ROOT / f'Assets/Weedtopia/Scripts/Runtime/Unity/{n}.cs.meta').read_text()).group(1)
                for n in ('CityCampaignUi', 'CityTravelRow')]
    scene_reports = {}
    for n in [*scenes, CAMPAIGN]:
        old, new = documents(FOLDER / 'Before' / n), documents(ROOT / n)
        unexpected = []
        for i, doc in old.items():
            now = new.get(i, '')
            if doc == now:
                continue
            if appended_travel_row(doc, now, old, new, ui_guids[0], ui_guids[1]):
                continue
            if 'Transform:' in doc:
                pattern = r'\n  m_Children:.*?(?=\n  m_Father:)'
                a, b = re.search(pattern, doc, re.S), re.search(pattern, now, re.S)
                if a and b:
                    ai, bi = re.findall(r'fileID: (-?\d+)', a.group()), re.findall(r'fileID: (-?\d+)', b.group())
                    if bi[:len(ai)] == ai and all(x in new.keys() - old.keys() for x in bi[len(ai):]) and re.sub(pattern, '\n  m_Children: OMITTED', doc, flags=re.S) == re.sub(pattern, '\n  m_Children: OMITTED', now, flags=re.S):
                        continue
            if '\nSceneRoots:\n' in doc:
                pattern = r'\n  m_Roots:.*?(?=\n  [A-Za-z_][^:\n]*:|\Z)'
                a, b = re.search(pattern, doc, re.S), re.search(pattern, now, re.S)
                if a and b:
                    ai, bi = re.findall(r'fileID: (-?\d+)', a.group()), re.findall(r'fileID: (-?\d+)', b.group())
                    if bi[:len(ai)] == ai and all(x in new.keys() - old.keys() for x in bi[len(ai):]) and re.sub(pattern, '\n  m_Roots: VERIFIED', doc, flags=re.S) == re.sub(pattern, '\n  m_Roots: VERIFIED', now, flags=re.S):
                        continue
            unexpected.append(i)
        scene_reports[n] = {'byteIdentical': digest(FOLDER / 'Before' / n) == digest(ROOT / n),
                            'originalObjects': len(old), 'removedObjects': list(old.keys() - new.keys()),
                            'addedObjects': len(new.keys() - old.keys()), 'unexpectedChangedObjects': unexpected}
    try:
        strict_prefabs = strict_prefab_audit()
    except (ValueError, OSError, KeyError) as error:
        strict_prefabs = {'status': 'failed', 'error': str(error)}
    report = {'protectedFiles': len(baseline), 'changedFiles': changed, 'removedFiles': removed,
              'strictPrefabPreservation': strict_prefabs,
              'regressionFixtureChanges': regression_changes,
              'unexpectedChanges': [n for n in changed if n not in allowed],
              'previousContentChanges': content, 'previousLocaleChanges': locales,
              'migrationOnly': migration, 'housingChange': 'Only level 5000 density bonus 135 -> 475; capacity 260 -> 600; all earlier levels unchanged',
              'scenes': scene_reports}
    report['status'] = 'failed' if removed or report['unexpectedChanges'] or content or any(locales.values()) or not migration or strict_prefabs['status'] != 'passed' or not all(change['passed'] for change in regression_changes.values()) or any(s['removedObjects'] or s['unexpectedChangedObjects'] for s in scene_reports.values()) else 'passed'
    (FOLDER / 'result.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    return report

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('mode', choices=['capture', 'verify'])
    result = capture() if parser.parse_args().mode == 'capture' else verify()
    print(json.dumps(result, indent=2))
    raise SystemExit(result['status'] == 'failed')
