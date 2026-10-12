"""Validate city/locale preservation without editing assets; save the evidence receipt."""
from datetime import datetime, timezone
import hashlib
import json
from author_seven_hills import ROOT, PATH, LOCALES, LANGUAGES, CITY, validate


def main():
    data = json.loads(PATH.read_text(encoding='utf-8'))
    catalogs = {lang: json.loads((LOCALES / (lang + '.json')).read_text(encoding='utf-8')) for lang in LANGUAGES}
    result = validate(data, catalogs)
    baseline = ROOT / 'Docs/SevenHills/Preservation/BurningHills.v3.json'
    original = json.loads(baseline.read_text(encoding='utf-8'))
    for collection in ('cities', 'instances', 'routes', 'quests', 'vipStories', 'vipCollaborations', 'texts'):
        assert data[collection][:len(original[collection])] == original[collection], 'Original ' + collection + ' changed'
    allowed = {'version', 'cities', 'instances', 'routes', 'quests', 'vipStories', 'vipCollaborations', 'texts'}
    assert all(data[key] == original[key] for key in original.keys() - allowed), 'Original tuning changed'
    # Removing only the appended keys reconstructs each pre-change locale byte for byte.
    hashes = json.loads((baseline.parent / 'baseline.json').read_text(encoding='utf-8'))
    new_keys = {entry['key'] for entry in data['texts']} - {entry['key'] for entry in original['texts']}
    for lang in LANGUAGES:
        previous = dict(catalogs[lang], entries=[entry for entry in catalogs[lang]['entries'] if entry['key'] not in new_keys])
        previous_bytes = (json.dumps(previous, ensure_ascii=False, indent=2) + '\n').replace('\n', '\r\n').encode('utf-8')
        relative = 'Assets/Weedtopia/Game/Localization/' + lang + '.json'
        assert hashlib.sha256(previous_bytes).hexdigest() == hashes[relative], 'Original locale entries changed: ' + lang
    for collection in ('cities', 'instances', 'quests', 'vipStories', 'vipCollaborations'):
        additions = data[collection][len(original[collection]):]
        assert all(entry.get('cityId', entry.get('id')) == CITY for entry in additions), 'Unexpected addition to ' + collection
    result.update(originalCityContentPreserved=True, originalTuningPreserved=True, originalLocaleEntriesPreserved=True,
                  firstAppearances=[0, 50, 150, 300, 500, 800, 1200],
                  populationChapters=[250, 750, 1500, 3000, 5000],
                  note='Runtime acceptance, saves, scene rendering and touch are separate Unity/device gates.')
    result.update(utc=datetime.now(timezone.utc).isoformat(), status='passed',
                  contentSha256=hashlib.sha256(PATH.read_bytes()).hexdigest())
    report = ROOT / 'Docs/SevenHills/content_validation.json'
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return result


if __name__ == '__main__':
    main()
