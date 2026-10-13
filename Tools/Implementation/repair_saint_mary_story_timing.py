"""Explicit exact-text repair for optional Saint Mary Ville project order.

--check is read-only. --apply changes only the known Saint text values and the
corresponding literal values in its authoring source, then writes a receipt.
Every expected old/new value is recorded in the adjacent documentation manifest.
"""
import argparse
import copy
import hashlib
import json
import os
import pathlib
import string


ROOT = pathlib.Path(__file__).resolve().parents[2]
MANIFEST = ROOT / 'Docs/SaintMaryVille/story_timing_repair_manifest.json'
REPORT = ROOT / 'Docs/SaintMaryVille/story_timing_repair_receipt.json'
CONTENT = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
AUTHOR = ROOT / 'Tools/Implementation/author_saint_mary_ville.py'
LANGUAGES = ('de', 'en', 'es', 'fr', 'ru')
EXPECTED_KEYS = frozenset([
    'sm.story.mara.chapter.2.body', 'sm.story.elias.chapter.2.body',
    'sm.story.finn.chapter.2.body', 'sm.story.amira.chapter.2.body',
    'sm.story.mara.chapter.3.body', 'sm.story.rosa.chapter.3.body',
    'sm.story.finn.chapter.3.body', 'sm.story.hugo.chapter.3.body',
    'sm.story.mara.chapter.4.body', 'sm.story.elias.chapter.4.body',
    'sm.story.rosa.chapter.4.body', 'sm.story.finn.chapter.4.body',
    'sm.story.amira.chapter.4.body', 'sm.story.hugo.chapter.4.body',
    'sm.choice.elias.ferry_workshop.outcome',
    'sm.choice.elias.river_classroom.outcome',
    'sm.quest.home.body', 'sm.quest.home.body.dialogue',
])


def sha256(value):
    return hashlib.sha256(value).hexdigest()


def placeholders(value):
    return sorted(field for _, field, _, _ in string.Formatter().parse(value) if field is not None)


def prepare():
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    if manifest['version'] != 1 or manifest['city'] != 'saint_mary_ville' or tuple(manifest['languages']) != LANGUAGES:
        raise ValueError('Unexpected Saint Mary Ville repair manifest.')
    repairs = manifest['repairs']
    if len(repairs) != len(EXPECTED_KEYS) or {r['key'] for r in repairs} != EXPECTED_KEYS:
        raise ValueError('Unexpected targeted Saint Mary Ville keys.')
    replacements = manifest['authorSourceReplacements']
    if len(replacements) != 85 or len({p['old'] for p in replacements}) != len(replacements):
        raise ValueError('Unexpected authoring-source replacement set.')
    for repair in repairs:
        for version in ('old', 'new'):
            if set(repair[version]) != set(LANGUAGES) or any(not text.strip() for text in repair[version].values()):
                raise ValueError('Incomplete localized repair: ' + repair['key'])
        if any(placeholders(repair['old'][lang]) != placeholders(repair['new'][lang]) for lang in LANGUAGES):
            raise ValueError('Repair changes formatting placeholders: ' + repair['key'])

    paths = [CONTENT] + [ROOT / ('Assets/Weedtopia/Game/Localization/' + lang + '.json') for lang in LANGUAGES] + [AUTHOR]
    original_bytes = {path: path.read_bytes() for path in paths}
    originals = {path: json.loads(original_bytes[path].decode('utf-8-sig')) for path in paths if path != AUTHOR}
    updated = copy.deepcopy(originals)
    pending_entries = []

    def replace_entries(path, collection, lang, field):
        before = originals[path][collection]
        entries = updated[path][collection]
        lookup = {entry['key']: entry for entry in entries}
        if len(lookup) != len(entries):
            raise ValueError('Duplicate text keys: ' + str(path))
        for repair in repairs:
            key = repair['key']
            if key not in lookup:
                raise ValueError('Missing targeted Saint text: ' + key)
            entry = lookup[key]
            if entry[field] == repair['new'][lang]:
                continue
            if entry[field] != repair['old'][lang]:
                raise ValueError('Independently edited Saint text; no files written: ' + lang + ':' + key)
            entry[field] = repair['new'][lang]
            pending_entries.append({'language': lang, 'collection': collection, 'key': key})
        if len(before) != len(entries):
            raise ValueError('Repair changed collection length.')
        for prior, current in zip(before, entries):
            if prior['key'] not in EXPECTED_KEYS:
                if prior != current:
                    raise ValueError('Older text entry changed: ' + prior['key'])
            elif set(prior) != set(current) or prior['key'] != current['key']:
                raise ValueError('Repair changed text schema.')
        for name in originals[path].keys() - {collection}:
            if originals[path][name] != updated[path][name]:
                raise ValueError('Repair changed non-text content: ' + name)

    replace_entries(CONTENT, 'texts', 'de', 'de')
    for lang, path in zip(LANGUAGES, paths[1:-1]):
        replace_entries(path, 'entries', lang, 'value')
    source = original_bytes[AUTHOR].decode('utf-8')
    pending_source = 0
    for replacement in replacements:
        old_count, new_count = source.count(replacement['old']), source.count(replacement['new'])
        if old_count == 0 and new_count == 1:
            continue
        if old_count != 1 or new_count != 0:
            raise ValueError('Unexpected independently edited authoring literal; no files written.')
        source = source.replace(replacement['old'], replacement['new'], 1)
        pending_source += 1
    compile(source, str(AUTHOR), 'exec')
    final_bytes = {path: (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
                   for path, value in updated.items() if value != originals[path]}
    if pending_source:
        final_bytes[AUTHOR] = source.encode('utf-8')
    return original_bytes, final_bytes, pending_entries, pending_source


def run(apply):
    original_bytes, final_bytes, pending_entries, pending_source = prepare()
    receipt = {
        'mode': 'apply' if apply else 'check',
        'valid': True,
        'changed': bool(final_bytes),
        'targetedSaintTextKeys': len(EXPECTED_KEYS),
        'pendingTextEntries': len(pending_entries),
        'pendingAuthorSourceValues': pending_source,
        'changedFiles': [str(path.relative_to(ROOT)) for path in final_bytes],
        'olderTextEntriesPreserved': True,
        'allNonTextContentPreserved': True,
        'idsGoalsDependenciesRewardsAndMemoryKeysPreserved': True,
        'languages': list(LANGUAGES),
        'beforeHashes': {str(path.relative_to(ROOT)): sha256(value) for path, value in original_bytes.items()},
        'afterHashes': {str(path.relative_to(ROOT)): sha256(final_bytes.get(path, value)) for path, value in original_bytes.items()},
    }
    if apply:
        # Validate all inputs again before any write, so work performed after
        # preparation cannot be overwritten by a stale copy.
        for path, value in original_bytes.items():
            if path.read_bytes() != value:
                raise ValueError('File changed during preparation; rerun the repair: ' + str(path))
        for path, value in final_bytes.items():
            temporary = path.with_name(path.name + '.saint-story-timing.tmp')
            temporary.write_bytes(value)
            os.replace(temporary, path)
        REPORT.write_text(json.dumps(receipt, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(receipt, ensure_ascii=False, indent=2))
    return receipt


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group(required=True)
    modes.add_argument('--check', action='store_true', help='Validate exact old/new values without writing any file.')
    modes.add_argument('--apply', action='store_true', help='Apply known Saint-only text replacements and write a receipt.')
    args = parser.parse_args()
    run(args.apply)
