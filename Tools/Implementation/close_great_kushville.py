"""Verify actual Great Kushville receipts and report bounded local integration.

Reads productive assets, NUnit XML, Unity receipts and preservation evidence.
Only writes Docs/GreatKushville/validation_summary.json after all required inputs
exist. It does not run Unity, tests, authoring, or a production playthrough.
Example: python Tools/Implementation/close_great_kushville.py --tests run1.json run2.json
"""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/GreatKushville'
CITY = 'great_kushville'
CONTENT = 'Assets/Weedtopia/Game/Content/BurningHills.json'
SAVE = 'Assets/Weedtopia/Scripts/Runtime/Persistence/LocalSaveStore.cs'
LANGUAGES = ('de', 'en', 'es', 'fr', 'ru')
CATALOGS = {CONTENT, *(f'Assets/Weedtopia/Game/Localization/{code}.json' for code in LANGUAGES)}
PEOPLE = ['gk_selma', 'gk_aurel', 'gk_daria', 'gk_leandro', 'gk_kira', 'gk_benno', 'gk_yuna', 'gk_oskar']
SOURCES = ['burning_hills', 'big_rock_city', 'seven_hills', 'little_montana', 'cassa_vadela',
           'mark_wood_city', 'green_leaf_village', 'purple_garden', 'golden_flower', 'saint_mary_ville']
RESOLUTIONS = {(1920, 1080), (1366, 768), (2560, 1080), (1080, 1920)}
REQUIRED_FIXTURES = {'Weedtopia.Tests.GreatKushvilleCampaignTests',
                     'Weedtopia.Tests.Assets.GreatKushvilleAssetTests',
                     'Weedtopia.Tests.PlayMode.GreatKushvillePlayModeTests'}


def resolve(value):
    path = Path(value)
    return path if path.is_absolute() else ROOT / path


def relative(path):
    return path.resolve().relative_to(ROOT).as_posix()


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def semantic_digest(value):
    raw = json.dumps(value, sort_keys=True, ensure_ascii=False, separators=(',', ':'))
    return hashlib.sha256(raw.encode('utf-8')).hexdigest()


def utc_timestamp(value):
    return datetime.datetime.fromisoformat(value.replace('Z', '+00:00')).timestamp()


class Audit:
    def __init__(self):
        self.checks = []
        self.errors = []

    def check(self, name, condition, detail=''):
        passed = bool(condition)
        self.checks.append(dict(check=name, passed=passed, detail=detail))
        if not passed:
            self.errors.append(name + (': ' + detail if detail else ''))
        return passed


def verify_previous_content(audit, before, after):
    differences = []
    for key, old in before.items():
        if key == 'version':
            continue
        current = after.get(key)
        if isinstance(old, list) and old and isinstance(old[0], dict):
            identity = 'id' if 'id' in old[0] else 'key'
            entries = {entry[identity]: entry for entry in current}
            for entry in old:
                expected = dict(entry)
                if key == 'buildings' and entry[identity] == 'residential_house':
                    expected['residentTierBonuses'] = list(entry['residentTierBonuses'])
                    expected['residentTierBonuses'][-1] = 475
                if entries.get(entry[identity]) != expected:
                    differences.append(key + ':' + entry[identity])
        elif current != old:
            differences.append(key)
    audit.check('All original content preserved except the final housing bonus', not differences, ', '.join(differences))


def verify_strict_preservation(audit, preserve, previous_catalog):
    strict = preserve.get('strictPrefabPreservation', {})
    scenes = strict.get('scenes', [])
    expected_paths = {city['scenePath'] for city in previous_catalog['cities']}
    expected_paths.add('Assets/Weedtopia/Game/Scenes/Bootstrap.unity')
    shared = strict.get('sharedCampaign', {})
    condition = (strict.get('status') == 'passed' and strict.get('previousCityScenes') == 10 and
                 strict.get('bootstrapByteIdentical') is True and len(scenes) == len(expected_paths) and
                 {scene.get('path') for scene in scenes} == expected_paths and
                 all(scene.get('byteIdentical') is True and scene.get('metaIdentical') is True and
                     scene.get('allOldPrefabInstanceDocumentsIdentical') is True for scene in scenes) and
                 shared.get('unchangedOldPrefabInstances') == 3 and shared.get('addedDocuments') == 14 and
                 shared.get('travelRowsBefore') == 10 and shared.get('travelRowsAfter') == 11 and
                 shared.get('newCityId') == CITY and
                 set(shared.get('newUiRoots', [])) == {'GreatKushvilleProgress', 'TravelGreatKushville'} and
                 shared.get('unrelatedTransformArtOrPrefabOverrideChanges') == [])
    audit.check('Strict prior scene and prefab preservation passed', condition,
                'Ten previous city scenes and Bootstrap, including metas and old prefab documents; '
                'one connected 14-document travel UI addition.')


def verify_tests(audit, paths, latest_catalog_write):
    cases = {}
    fixtures = set()
    rows = []
    seen = set()
    full_domain_completed = False
    for value in paths:
        path = resolve(value).resolve()
        audit.check('Distinct NUnit receipt ' + path.name, path not in seen)
        seen.add(path)
        receipt = load(path)
        xml_path = resolve(receipt['xml']).resolve()
        xml = ET.parse(xml_path).getroot()
        counts = {key: int(xml.get(key, '0')) for key in ('total', 'passed', 'failed', 'skipped', 'inconclusive')}
        label = receipt['run']
        audit.check('NUnit XML agrees with receipt ' + label,
                    all(counts[key] == receipt[key] for key in ('total', 'passed', 'failed', 'skipped')))
        run_cases = list(xml.iter('test-case'))
        valid = receipt['status'] == 'passed' and receipt['exitCode'] == 0 and xml.get('result') == 'Passed'
        valid = valid and counts['total'] > 0 and counts['passed'] == counts['total'] == len(run_cases)
        valid = valid and counts['failed'] == counts['skipped'] == counts['inconclusive'] == 0
        valid = valid and all(case.get('result') == 'Passed' for case in run_cases)
        audit.check('Every submitted NUnit case passed ' + label, valid)
        started = utc_timestamp(receipt['startedUtc'])
        audit.check('NUnit used the final productive catalogs ' + label, started >= latest_catalog_write)
        audit.check('NUnit XML belongs to its completed run ' + label, xml_path.stat().st_mtime >= started)
        for case in run_cases:
            fixtures.add(case.get('classname'))
            cases[case.get('fullname')] = case.get('result')
        for suite in xml.iter('test-suite'):
            if (suite.get('type') == 'Assembly' and suite.get('name') == 'Weedtopia.Game.EditModeTests.dll'
                    and valid and int(suite.get('testcasecount', '0')) == int(suite.get('total', '-1'))):
                full_domain_completed = True
        rows.append(dict(receipt=relative(path), xml=relative(xml_path), run=label,
                         mode=receipt['mode'], sha256=digest(xml_path), **counts))
    audit.check('Great Kushville domain, asset and PlayMode fixtures executed', REQUIRED_FIXTURES <= fixtures)
    audit.check('Previous-city regression executed', any(name and name.endswith('SaintMaryVilleCampaignTests') for name in fixtures))
    return rows, cases, full_domain_completed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tests', nargs='+', required=True, metavar='RESULT_JSON')
    args = parser.parse_args()
    required = [DOC / name for name in ('content_receipt.json', 'content_idempotence_receipt.json',
                'authoring_receipt.json', 'gameview_receipt.json', 'ui_overflow.json', 'visual_review.json', 'Preservation/result.json')]
    required += [resolve(value) for value in args.tests]
    missing = [relative(path) for path in required if not path.is_file()]
    captures = []
    for path in (DOC / 'Validation').glob('*.json'):
        data = load(path)
        if data.get('mode') == 'Capture':
            captures.append(data)
    if missing or not captures:
        print(json.dumps(dict(status='missing_evidence', missing=missing,
                              completedCaptureRunFound=bool(captures)), indent=2))
        return 2

    audit = Audit()
    catalog = load(ROOT / CONTENT)
    charter = next(city for city in catalog['cities'] if city['id'] == CITY)
    audit.check('Version 12 and eleven playable cities', catalog['version'] == 12 and len(catalog['cities']) == 11)
    audit.check('Five million simultaneous residents across ten previous cities',
                charter['unlockResidents'] == 5000000 and charter['unlockSources'] == SOURCES)
    audit.check('One-time charter bridge', charter['bridgeMoney'] == 12500 and charter['bridgeCards'] == 7)
    people = [person for person in catalog['vipStories'] if person['cityId'] == CITY]
    projects = [project for project in catalog['vipCollaborations'] if project['cityId'] == CITY]
    audit.check('Eight authored permanent residents', [person['id'] for person in people] == PEOPLE)
    audit.check('32 personal chapters and 24 recurring variants',
                sum(len(person['chapters']) for person in people) == 32 and sum(len(person['recurring']) for person in people) == 24)
    audit.check('Three local community projects', {project['id'] for project in projects} ==
                {'gk_joint_water_compact', 'gk_joint_shared_exchange', 'gk_joint_founders_walk'})
    ids = {site['id'] for site in catalog['instances'] if site['cityId'] == CITY}
    routes = [route for route in catalog['routes'] if route['from'] in ids]
    audit.check('56 sites and 35 city-local routes', len(ids) == 56 and len(routes) == 35 and all(route['to'] in ids for route in routes))
    audit.check('18 local main quests', len([quest for quest in catalog['quests'] if quest.get('cityId') == CITY]) == 18)
    selma = next(person for person in people if person['id'] == 'gk_selma')
    audit.check('Two durable Selma choices', {choice['id'] for choice in selma['chapters'][2]['choices']} == {'water_lab', 'canal_workshop'})

    previous = load(DOC / 'Preservation/Before' / CONTENT)
    verify_previous_content(audit, previous, catalog)
    house = next(building for building in catalog['buildings'] if building['id'] == 'residential_house')
    old_house = next(building for building in previous['buildings'] if building['id'] == 'residential_house')
    audit.check('Lower housing levels preserved', house['baseResidents'] == old_house['baseResidents'] and
                house['residentTierBonuses'][:-1] == old_house['residentTierBonuses'][:-1])
    audit.check('Only final density bonus 135 to 475', old_house['residentTierBonuses'] == [0] * 24 + [135] and house['residentTierBonuses'] == [0] * 24 + [475])
    max_house = int((house['baseResidents'] * 25 + house['residentTierBonuses'][-1]) * (1 + catalog['workerAttributeStep'] * 100 * 10))
    maximum = max_house * 17 * len(SOURCES)
    audit.check('Requested threshold physically reachable with a park per city', max_house == 30600 and maximum == 5202000)
    old_save = (DOC / 'Preservation/Before' / SAVE).read_text(encoding='utf-8-sig')
    save = (ROOT / SAVE).read_text(encoding='utf-8-sig')
    audit.check('Migration guard 12 is the only save-code change', save == old_save.replace('definitions.version > 11', 'definitions.version > 12'))

    content = load(DOC / 'content_receipt.json')
    idempotence = load(DOC / 'content_idempotence_receipt.json')
    author = load(DOC / 'authoring_receipt.json')
    canonical_digest = semantic_digest(catalog)
    audit.check('Content receipt describes the final productive catalog', content['version'] == 12 and content['permanentResidents'] == 8 and content['contentDigest'] == canonical_digest)
    audit.check('Final semantic content digest matches productive catalog', idempotence['finalContentDigest'] == canonical_digest)
    final_files = {str(path).replace('\\', '/'): value for path, value in idempotence['files'].items()}
    audit.check('Exactly content plus five locale catalogs in final hash receipt', set(final_files) == CATALOGS)
    audit.check('All six final catalog hashes match productive files', all((ROOT / path).is_file() and digest(ROOT / path) == value for path, value in final_files.items()))
    audit.check('Content authoring is byte-idempotent', idempotence.get('byteIdenticalOnSecondRun') is True and idempotence.get('changedOnSecondRun') is False)
    audit.check('Explicit city installation completed', author['status'] == 'passed' and author['contentVersion'] == 12 and
                (author['buildings'], author['routes'], author['mainQuests'], author['inhabitants']) == (56, 35, 18, 8) and author.get('runtimeStaticObjects') is False)
    latest_catalog_write = max((ROOT / path).stat().st_mtime for path in CATALOGS)
    tests, cases, full_domain_completed = verify_tests(audit, args.tests, latest_catalog_write)

    preserve = load(DOC / 'Preservation/result.json')
    audit.check('Working checkout preservation passed', preserve['status'] == 'passed' and not preserve['removedFiles'] and
                not preserve['unexpectedChanges'] and not preserve['previousContentChanges'] and preserve['migrationOnly'] is True and
                not any(preserve['previousLocaleChanges'].values()) and
                not any(scene['removedObjects'] or scene['unexpectedChangedObjects'] for scene in preserve['scenes'].values()))
    verify_strict_preservation(audit, preserve, previous)
    game = load(DOC / 'gameview_receipt.json')
    overflow = load(DOC / 'ui_overflow.json')
    current_capture = max(captures, key=lambda receipt: receipt['startedUtc'])
    capture_start = utc_timestamp(current_capture['startedUtc'])
    audit.check('Final owned Unity rendered capture completed', current_capture['status'] == 'passed' and current_capture['exitCode'] == 0 and capture_start >= latest_catalog_write)
    audit.check('Actual rendered PlayMode diagnostic growth evidence', game['status'] == 'passed' and game['mode'] == 'actual_playmode_gameview_with_diagnostic_growth')
    audit.check('Fresh rendered receipt and overflow receipt', (DOC / 'gameview_receipt.json').stat().st_mtime >= capture_start and (DOC / 'ui_overflow.json').stat().st_mtime >= capture_start)
    audit.check('Four-resolution UI overflow check', overflow['passed'] is True and not overflow['issues'])
    screenshots = []
    resolutions = set()
    for entry in game['dimensions']:
        path = resolve(entry['path'])
        data = path.read_bytes()
        valid_png = len(data) >= 24 and data[:8] == b'\x89PNG\r\n\x1a\n'
        width, height = struct.unpack('>II', data[16:24]) if valid_png else (0, 0)
        resolutions.add((width, height))
        audit.check('Fresh PNG dimensions ' + path.name, valid_png and width == entry['width'] and height == entry['height'] and path.stat().st_mtime >= capture_start)
        screenshots.append(dict(path=entry['path'], width=width, height=height, sha256=digest(path)))
    screenshot_paths = {entry['path'] for entry in screenshots}
    audit.check('84 distinct actual screenshots', len(screenshots) == 84 and len(screenshot_paths) == 84 and set(game['screenshots']) == screenshot_paths)
    audit.check('Required four capture resolutions', resolutions >= RESOLUTIONS)
    for flag in ('savedStoryRoundtrip', 'housingInspectionMatchesSimulation', 'returnedToPreviousCity',
                 'allElevenCitiesVisited', 'elevenCityOfflineRoundtrip', 'selmaChoiceVariantsVerified', 'architectureGroundAnchorsVerified'):
        audit.check('Game View ' + flag, game.get(flag) is True)
    audit.check('All eleven authored cities visited', set(game['visitedCities']) == {CITY, *SOURCES} and game['cities'] == 11)
    audit.check('Both authored choices rendered', set(game['verifiedChoices']) == {'water_lab', 'canal_workshop'})
    audit.check('All five locales rendered', set(game['languages']) == set(LANGUAGES))
    matrix = game['matrix']
    expected_matrix = {(scenario, code, width, height) for scenario in ('initial_quest', 'travel_final_row', 'selma_choice')
                       for code in LANGUAGES for width, height in RESOLUTIONS}
    actual_matrix = {(entry['scenario'], entry['language'], entry['width'], entry['height']) for entry in matrix}
    audit.check('Complete 60-case locale and resolution matrix', len(matrix) == 60 and actual_matrix == expected_matrix and all(entry['path'] in screenshot_paths for entry in matrix))
    visual = load(DOC / 'visual_review.json')
    audit.check('Final Game View samples actually visually inspected', visual.get('gameViewReviewed') is True and
                visual.get('captureRun') == current_capture['run'] and not visual.get('blockingGameViewIssuesObserved', []) and not visual.get('blockingArtIssuesObserved', []))

    result = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  status='passed_local_integration' if not audit.errors else 'failed',
                  technicalIntegrationComplete=not audit.errors, uniqueNUnitPassed=sum(value == 'Passed' for value in cases.values()),
                  uniqueNUnitCases=len(cases), tests=tests, fullDomainRegressionCompleted=full_domain_completed,
                  population=dict(maximumPerHouse=max_house, mandatoryParksPerCity=1, housesPerCity=17, sourceCityCount=10,
                                  maximumPreviousCitiesPopulation=maximum, unlockResidents=5000000),
                  finalContentDigest=canonical_digest, finalCatalogHashes=final_files, finalCaptureRun=current_capture,
                  gameView=game, screenshots=screenshots, preservation=preserve, checks=audit.checks, errors=audit.errors,
                  diagnosticGrowthFixtures=True,
                  evidenceScope='Actual rendered PlayMode scenes and accelerated diagnostic growth fixtures; no complete human playthrough.',
                  fullHumanPlaythrough=False, physicalDeviceAcceptance=False, releaseAccepted=False,
                  userArtApproval=False, nativeSpeakerReviewCompleted=False)
    (DOC / 'validation_summary.json').write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding='utf-8')
    print(json.dumps(dict(status=result['status'], uniqueNUnitPassed=result['uniqueNUnitPassed'], uniqueNUnitCases=len(cases),
                         checks=len(audit.checks), screenshots=len(screenshots), fullDomainRegressionCompleted=full_domain_completed,
                         errors=audit.errors), indent=2))
    return int(bool(audit.errors))


if __name__ == '__main__':
    raise SystemExit(main())