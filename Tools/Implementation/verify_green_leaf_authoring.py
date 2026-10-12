"""Verify Green Leaf content contracts on isolated copies of the preserved v7 input."""
import contextlib
import copy
import hashlib
import io
import json
import pathlib
import tempfile
import types

ROOT = pathlib.Path(__file__).resolve().parents[2]
AUTHOR = ROOT / 'Tools/Implementation/author_green_leaf_village.py'
BASELINE = ROOT / 'Docs/GreenLeafVillage/Preservation/Before'
REPORT = ROOT / 'Docs/GreenLeafVillage/content_authoring_contracts.json'
CONTENT = pathlib.Path('Assets/Weedtopia/Game/Content/BurningHills.json')
LOCALES = pathlib.Path('Assets/Weedtopia/Game/Localization')
LANGUAGES = ('de', 'en', 'es', 'fr', 'ru')
FILES = (CONTENT,) + tuple(LOCALES / (lang + '.json') for lang in LANGUAGES)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def hashes(root):
    return {path.as_posix(): hashlib.sha256((root / path).read_bytes()).hexdigest() for path in FILES}


def isolated_author(root):
    module = types.ModuleType('isolated_green_leaf_authoring')
    module.__file__ = str(AUTHOR)
    exec(compile(AUTHOR.read_text(encoding='utf-8'), str(AUTHOR), 'exec'), module.__dict__)
    module.PATH = root / CONTENT
    module.LOCALES = root / LOCALES
    module.REPORT = root / 'isolated_authoring_receipt.json'
    return module


def make_case(parent, name):
    root = parent / name
    for path in FILES:
        destination = root / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes((BASELINE / path).read_bytes())
    return root


def run(module):
    with contextlib.redirect_stdout(io.StringIO()):
        return module.author()


def unchanged_refusal(root, module, name, expected_error):
    before = hashes(root)
    try:
        run(module)
    except ValueError as error:
        reason = str(error)
        assert expected_error in reason, (name, reason)
    else:
        raise AssertionError(name + ': conflicting input was accepted')
    after = hashes(root)
    assert before == after, name + ': an input file changed before refusal'
    assert not module.REPORT.exists(), name + ': a success receipt was written'
    return dict(case=name, passed=True, refused=True, reason=reason,
                allSixInputFilesByteIdentical=True, successReceiptAbsent=True,
                inputSha256=before, outputSha256=after)


def verify():
    assert read(BASELINE / CONTENT)['version'] == 7, 'Expected protected six-city v7 baseline'
    live_before = hashes(ROOT)
    cases = []
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='authoring_contracts_', dir=str(REPORT.parent)) as temporary:
        parent = pathlib.Path(temporary).resolve()
        # The context manager removes only this verified workspace-local temporary root.
        assert parent.parent == REPORT.parent.resolve() and parent.is_relative_to(ROOT.resolve())

        root = make_case(parent, 'v7_success_and_idempotence')
        module = isolated_author(root)
        original = read(root / CONTENT)
        first = run(module)
        created = read(root / CONTENT)
        assert first['changed'] and created['version'] == 8
        assert len(created['cities']) == len(original['cities']) + 1
        assert first['priorContentPreserved'] and first['priorLocaleEntriesPreserved']
        before_second = hashes(root)
        second = run(module)
        after_second = hashes(root)
        assert not second['changed'] and before_second == after_second
        cases.append(dict(case='v7_success_and_idempotence', passed=True,
                          baselineVersion=7, outputVersion=8, firstExecutionChanged=True,
                          secondExecutionChanged=False, allSixInputFilesByteIdentical=True,
                          contentEntriesPreserved=True, localeEntriesPreserved=True))

        root = make_case(parent, 'future_v9')
        module = isolated_author(root)
        data = read(root / CONTENT)
        data['version'] = 9
        future_metadata = {'tag': 'preserve unknown future extension', 'values': [2, 4, 9]}
        data['futureExtension'] = copy.deepcopy(future_metadata)
        write(root / CONTENT, data)
        future = run(module)
        result = read(root / CONTENT)
        city = next(city for city in result['cities'] if city['id'] == 'green_leaf_village')
        assert future['version'] == result['version'] == 9
        assert result['futureExtension'] == future_metadata and city['introducedVersion'] == 8
        cases.append(dict(case='future_v9', passed=True, inputVersion=9, outputVersion=9,
                          newCityIntroducedVersion=8, unknownFutureMetadataPreserved=True,
                          existingContentAndLocalesPreserved=True))

        root = make_case(parent, 'conflicting_city')
        module = isolated_author(root)
        data = read(root / CONTENT)
        data['cities'].append({'id': 'green_leaf_village', 'unlockResidents': 1})
        write(root / CONTENT, data)
        cases.append(unchanged_refusal(root, module, 'conflicting_city',
                                      'Refusing to overwrite existing content: green_leaf_village'))

        for language in LANGUAGES:
            name = 'conflicting_locale_' + language
            root = make_case(parent, name)
            module = isolated_author(root)
            locale = read(root / LOCALES / (language + '.json'))
            locale['entries'].append({'key': 'city.green_leaf_village', 'value': 'Conflicting city name'})
            write(root / LOCALES / (language + '.json'), locale)
            cases.append(unchanged_refusal(root, module, name,
                                          'Refusing to overwrite existing content: city.green_leaf_village'))

        root = make_case(parent, 'missing_sixth_source')
        module = isolated_author(root)
        data = read(root / CONTENT)
        data['cities'] = [city for city in data['cities'] if city['id'] != 'mark_wood_city']
        write(root / CONTENT, data)
        cases.append(unchanged_refusal(root, module, 'missing_sixth_source',
                                      'requires all six existing campaign cities'))

    live_after = hashes(ROOT)
    assert live_before == live_after, 'Contract verifier modified live content or locales'
    receipt = dict(city='green_leaf_village', generator=AUTHOR.relative_to(ROOT).as_posix(),
                   baseline=BASELINE.relative_to(ROOT).as_posix(), isolatedCopiesOnly=True,
                   tests=len(cases), passed=len(cases), failed=0,
                   liveContentAndAllFiveLocalesByteIdentical=True,
                   liveBeforeSha256=live_before, liveAfterSha256=live_after, cases=cases)
    write(REPORT, receipt)
    print(json.dumps({key: value for key, value in receipt.items() if key != 'cases'}, ensure_ascii=False, indent=2))
    return receipt


if __name__ == '__main__':
    verify()
