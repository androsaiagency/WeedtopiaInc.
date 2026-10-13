"""Explicit guarded repair of exactly two new Great Sativadam locale values.

Run only after the active Unity validation has ended. This tool does not import
authoring modules, alter game content, run on Unity import, or change C# sources.
It preflights both catalogs before writing and preserves every other raw byte.
"""
import copy
import hashlib
import json
import os
import pathlib
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
LOCALES = ROOT / 'Assets/Weedtopia/Game/Localization'
REPORT = ROOT / 'Docs/GreatSativadam/localization_polish_receipt.json'

FRENCH_PREFIX = ('Joost tient le chantier par son métier et ses souvenirs. '
                 'Son père a perdu un bateau faute d’entretien ; il ne fait confiance qu’à lui-même. '
                 'Le conflit foncier menace le ponton. ')
POLISH = (
    dict(language='es', key='gs.story.joost.greeting',
         old='Una buena herramienta debe conocer más de unas manos.',
         new='Una buena herramienta está hecha para pasar de mano en mano.'),
    dict(language='fr', key='gs.story.joost.biography',
         old=FRENCH_PREFIX + 'Il doit transmettre la sécurité pour vivre sans son contrôle quotidien.',
         new=FRENCH_PREFIX + 'Il doit transmettre son savoir-faire pour que le chantier reste sûr sans dépendre de sa surveillance quotidienne.'),
)
assert len(POLISH) == 2 and len({(p['language'], p['key']) for p in POLISH}) == 2


def sha256(raw):
    return hashlib.sha256(raw).hexdigest()


def reject_constant(value):
    raise ValueError('Non-JSON numeric constant: ' + value)


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('Duplicate JSON object key: ' + key)
        result[key] = value
    return result


def catalog(raw, language):
    """Validate the complete current catalog shape and every entry identity."""
    data = json.loads(raw.decode('utf-8-sig'), object_pairs_hook=unique_object,
                      parse_constant=reject_constant)
    if not isinstance(data, dict) or set(data) != {'language', 'entries'}:
        raise ValueError('Unexpected locale root structure: ' + language)
    if data['language'] != language or not isinstance(data['entries'], list):
        raise ValueError('Invalid locale identity/entries: ' + language)
    seen = set()
    for entry in data['entries']:
        if not isinstance(entry, dict) or set(entry) != {'key', 'value'}:
            raise ValueError('Unexpected locale entry structure: ' + language)
        if not isinstance(entry['key'], str) or not entry['key'].strip() or not isinstance(entry['value'], str):
            raise ValueError('Invalid locale entry: ' + language)
        if entry['key'] in seen:
            raise ValueError('Duplicate locale entry key: ' + entry['key'])
        seen.add(entry['key'])
    return data


def plan_repair(spec):
    path = LOCALES / (spec['language'] + '.json')
    before = path.read_bytes()
    original = catalog(before, spec['language'])
    matches = [entry for entry in original['entries'] if entry['key'] == spec['key']]
    if len(matches) != 1:
        raise ValueError('Expected exactly one known target: ' + spec['key'])
    current = matches[0]['value']
    if current not in (spec['old'], spec['new']):
        raise ValueError('Independently changed target; inspect before repair: ' + spec['key'])

    after = before
    changed = current == spec['old']
    if changed:
        raw_text = before.decode('utf-8-sig')
        old_token = json.dumps(spec['old'], ensure_ascii=False)
        new_token = json.dumps(spec['new'], ensure_ascii=False)
        if raw_text.count(old_token) != 1:
            raise ValueError('Expected one exact raw JSON value token: ' + spec['key'])
        offset = raw_text.index(old_token)
        updated = raw_text[:offset] + new_token + raw_text[offset + len(old_token):]
        # Verify byte preservation outside the known value token, including BOM
        # and CR/LF formatting. Existing new values are accepted without writes.
        encoding = 'utf-8-sig' if before.startswith(b'\xef\xbb\xbf') else 'utf-8'
        after = updated.encode(encoding)
        restored = updated[:offset] + old_token + updated[offset + len(new_token):]
        if restored.encode(encoding) != before:
            raise ValueError('Bytes outside the authorized token changed: ' + spec['key'])

    proposed = catalog(after, spec['language'])
    expected = copy.deepcopy(original)
    next(entry for entry in expected['entries'] if entry['key'] == spec['key'])['value'] = spec['new']
    if proposed != expected:
        raise ValueError('Unexpected JSON structure/value change: ' + spec['key'])
    other_before = [entry for entry in original['entries'] if entry['key'] != spec['key']]
    other_after = [entry for entry in proposed['entries'] if entry['key'] != spec['key']]
    if other_before != other_after:
        raise ValueError('Another locale entry changed: ' + spec['key'])
    return dict(spec=spec, path=path, before=before, after=after, changed=changed,
                original=original, expected=expected, unchangedOtherEntries=True)


def atomic_write(path, raw):
    """Stage under Docs so Unity never imports temporary locale files."""
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(dir=REPORT.parent, prefix='locale-polish-',
                                         suffix='.tmp', delete=False) as output:
            temporary = pathlib.Path(output.name)
            output.write(raw)
            output.flush()
            os.fsync(output.fileno())
        os.replace(temporary, path)
        temporary = None
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()


def polish():
    # Preflight every required target before any mutation. Keep hashes of all
    # six catalogs so game data and the other three languages remain accounted for.
    tracked = [ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'] + [
        LOCALES / (language + '.json') for language in ('de', 'en', 'es', 'fr', 'ru')]
    before_hashes = {str(path.relative_to(ROOT)): sha256(path.read_bytes()) for path in tracked}
    plans = [plan_repair(spec) for spec in POLISH]
    if len({plan['path'] for plan in plans}) != 2:
        raise ValueError('Expected exactly the Spanish and French catalogs.')
    for plan in plans:
        if plan['path'].read_bytes() != plan['before']:
            raise ValueError('Catalog changed during preflight: ' + str(plan['path']))

    written = []
    try:
        for plan in plans:
            if plan['changed']:
                atomic_write(plan['path'], plan['after'])
                written.append(plan)
        for plan in plans:
            actual = plan['path'].read_bytes()
            actual_catalog = catalog(actual, plan['spec']['language'])
            if actual != plan['after'] or actual_catalog != plan['expected']:
                raise ValueError('Written catalog differs from checked proposal: ' + str(plan['path']))
            key = plan['spec']['key']
            plan['unchangedOtherEntries'] = (
                [entry for entry in plan['original']['entries'] if entry['key'] != key] ==
                [entry for entry in actual_catalog['entries'] if entry['key'] != key])
            if not plan['unchangedOtherEntries']:
                raise ValueError('Actual comparison found another changed entry: ' + key)
        after_hashes = {str(path.relative_to(ROOT)): sha256(path.read_bytes()) for path in tracked}
        allowed_paths = {str(plan['path'].relative_to(ROOT)) for plan in plans}
        if any(before_hashes[path] != after_hashes[path] for path in before_hashes if path not in allowed_paths):
            raise ValueError('An unmodified catalog changed concurrently; inspect external activity.')
    except Exception:
        # Only restore bytes this tool itself wrote, and never replace a file
        # which another process has independently changed in the meantime.
        for plan in reversed(written):
            if plan['path'].read_bytes() == plan['after']:
                atomic_write(plan['path'], plan['before'])
        raise

    receipt = dict(
        status='passed', explicitTool=True,
        changedPaths=[str(plan['path'].relative_to(ROOT)) for plan in plans if plan['changed']],
        changedKeys=[dict(language=plan['spec']['language'], key=plan['spec']['key']) for plan in plans if plan['changed']],
        checkedKeys=[dict(language=plan['spec']['language'], key=plan['spec']['key'],
                          changed=plan['changed'], beforeHash=sha256(plan['before']),
                          afterHash=sha256(plan['after'])) for plan in plans],
        unchangedOtherEntries=all(plan['unchangedOtherEntries'] for plan in plans),
        unchangedOtherRawBytes=True, duplicateKeysRejected=True, exactOldValueGuards=True,
        existingNewValuesAccepted=True, gameDataUnchanged=True,
        beforeHashes=before_hashes, afterHashes=after_hashes,
        authoringExecuted=False, contentVersionChanged=False, mainQuestsChanged=False)
    atomic_write(REPORT, (json.dumps(receipt, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
    print(json.dumps(receipt, ensure_ascii=True))
    return receipt


if __name__ == '__main__':
    polish()
