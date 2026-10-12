"""Read-only check of catalog preservation against the authored baseline."""
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
BEFORE = ROOT / 'Docs/PurpleGarden/Preservation/Before'
PATH = pathlib.Path('Assets/Weedtopia/Game/Content/BurningHills.json')
old = json.loads((BEFORE / PATH).read_text(encoding='utf-8-sig'))
new = json.loads((ROOT / PATH).read_text(encoding='utf-8-sig'))
collections = {'cities', 'instances', 'routes', 'quests', 'vipStories', 'vipCollaborations', 'texts'}
assert old['version'] == 8 and new['version'] == 9
assert all(new[key][:len(old[key])] == old[key] for key in collections)
assert all(new[key] == old[key] for key in old.keys() - collections - {'version'})
checks = [dict(path=str(PATH), everyPriorEntryPreserved=True)]
bad = {chr(i) for i in (0xC2, 0xC3, 0xD0, 0xD1, 0xFFFD)} | {chr(i) for i in range(0x80, 0xA0)}
for lang in ('de', 'en', 'es', 'fr', 'ru'):
    path = pathlib.Path('Assets/Weedtopia/Game/Localization') / (lang + '.json')
    prior = json.loads((BEFORE / path).read_text(encoding='utf-8-sig'))
    current = json.loads((ROOT / path).read_text(encoding='utf-8-sig'))
    assert current['entries'][:len(prior['entries'])] == prior['entries'], lang
    assert all(current[key] == prior[key] for key in prior.keys() - {'entries'}), lang
    added = current['entries'][len(prior['entries']):]
    assert len(added) == 315, (lang, len(added))
    assert not any(any(char in bad for char in entry['value']) for entry in added), lang
    checks.append(dict(path=str(path), allPriorEntriesPreserved=True,
                       newEntries=len(added), mojibakeCheckPassed=True))
report = dict(baselineVersion=8, authoredVersion=9, allSevenCitiesPreserved=True,
              newCitySources=new['cities'][-1]['unlockSources'], files=checks)
(ROOT / 'Docs/PurpleGarden/preservation_validation.json').write_text(
    json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))
