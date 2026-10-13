"""Verify Golden Flower evidence and write its bounded integration report.

This tool reads project assets and receipts, and only writes two documents under
Docs/GoldenFlower. It does not run Unity, tests, authoring or production gameplay.
Example: python Tools/Implementation/close_golden_flower.py --tests run1.json run2.json
"""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import struct
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/GoldenFlower'
CONTENT = ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json'
BASELINE = DOC / 'Preservation/Before/Assets/Weedtopia/Game/Content/BurningHills.json'
EXPECTED_SOURCES = ['burning_hills', 'big_rock_city', 'seven_hills', 'little_montana',
                    'cassa_vadela', 'mark_wood_city', 'green_leaf_village', 'purple_garden']
EXPECTED_PEOPLE = ['gf_alma', 'gf_ida', 'gf_milan', 'gf_nora', 'gf_theo']
EXPECTED_RESOLUTIONS = {(1920, 1080), (1366, 768), (2560, 1080), (1080, 1920)}
EXPECTED_BONUSES = [0] * 24 + [20]
TIER_STARTS = [0, 1, 5, 10, 25, 50, 100, 150, 200, 300, 400, 500, 600,
               700, 800, 900, 1000, 1250, 1500, 1750, 2000, 2500, 3000, 4000, 5000]


def resolve(value):
    path = Path(value)
    return path if path.is_absolute() else ROOT / path


def link(path):
    path = Path(path).resolve()
    return '[' + path.name + '](<' + path.as_posix() + '>)'


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def content_digest(value):
    raw = json.dumps(value, sort_keys=True, ensure_ascii=False, separators=(',', ':'))
    return hashlib.sha256(raw.encode('utf-8')).hexdigest()


class Audit:
    def __init__(self):
        self.checks = []
        self.errors = []

    def check(self, name, condition, detail=''):
        passed = bool(condition)
        self.checks.append({'check': name, 'passed': passed, 'detail': detail})
        if not passed:
            self.errors.append(name + (': ' + detail if detail else ''))
        return passed

    def read(self, path):
        try:
            value = json.loads(path.read_text(encoding='utf-8-sig'))
            if not isinstance(value, dict):
                raise ValueError('Expected a JSON object')
            self.check('Readable evidence: ' + str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else 'Readable evidence: ' + str(path), True)
            return value
        except (OSError, ValueError) as error:
            self.check('Readable evidence: ' + str(path), False, str(error))
            return {}


def nonnegative_int(value):
    if isinstance(value, bool):
        raise ValueError('Boolean is not a test count')
    number = int(value)
    if number < 0 or str(value).strip() != str(number):
        raise ValueError('Expected a nonnegative integer count')
    return number


def tests(audit, paths):
    results = []
    seen = set()
    for raw in paths:
        path = resolve(raw)
        if path.resolve() in seen:
            audit.check('Distinct test receipts', False, str(path))
            continue
        seen.add(path.resolve())
        value = audit.read(path)
        row = {'receipt': str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),
               'run': value.get('run', path.stem), 'mode': value.get('mode', value.get('stage', 'unspecified')),
               'reportedStatus': value.get('status'), 'countsVerified': False, 'foundationTwelveMainQuestsPassed': False}
        try:
            passed = nonnegative_int(value['passed'])
            failed = nonnegative_int(value['failed'])
            skipped = nonnegative_int(value.get('skipped', 0))
            inconclusive = nonnegative_int(value.get('inconclusive', 0))
            total = nonnegative_int(value.get('total', passed + failed + skipped + inconclusive))
            row.update(total=total, passed=passed, failed=failed, skipped=skipped, inconclusive=inconclusive)
            valid = (value.get('status') == 'passed' and passed > 0 and total == passed and
                     failed == 0 and skipped == 0 and inconclusive == 0 and value.get('exitCode', 0) == 0)
            if value.get('xml'):
                xml_path = resolve(value['xml'])
                xml = ET.parse(xml_path).getroot()
                xml_counts = {key: nonnegative_int(xml.attrib.get(key, 0)) for key in ('passed', 'failed', 'skipped', 'inconclusive')}
                xml_total = nonnegative_int(xml.attrib.get('total', sum(xml_counts.values())))
                valid = valid and xml.attrib.get('result') == 'Passed' and xml_total == total
                valid = valid and all(xml_counts[key] == row[key] for key in xml_counts)
                row['xml'] = str(xml_path.relative_to(ROOT)) if xml_path.is_relative_to(ROOT) else str(xml_path)
                row['xmlCountsMatched'] = valid
                row['foundationTwelveMainQuestsPassed'] = valid and any(
                    'LocalFoundationCompletesTwelveMainQuestsUsingOnlyTheCharterBridge' in
                    (case.attrib.get('fullname', '') + case.attrib.get('name', '')) and
                    case.attrib.get('result') == 'Passed' for case in xml.iter('test-case'))
            row['countsVerified'] = audit.check('All test cases passed: ' + row['run'], valid,
                                               f'{passed}/{total}, failed={failed}, skipped={skipped}, inconclusive={inconclusive}')
        except (KeyError, ValueError, OSError, ET.ParseError) as error:
            audit.check('Verified test counts: ' + row['run'], False, str(error))
        results.append(row)
    audit.check('At least one verified test run', bool(results) and all(r['countsVerified'] for r in results))
    return results


def housing_capacity(building, level):
    tier = max(i for i, start in enumerate(TIER_STARTS) if start <= level)
    bonuses = building.get('residentTierBonuses', [])
    return building.get('baseResidents', 5) * (1 + tier) + (bonuses[tier] if tier < len(bonuses) else 0)


def check_catalog(audit, catalog, before, content_receipt):
    cities = catalog.get('cities', [])
    destination = next((c for c in cities if c.get('id') == 'golden_flower'), {})
    audit.check('Nine campaign cities', len(cities) == 9 and len({c.get('id') for c in cities}) == 9)
    audit.check('One-million global unlock', destination.get('unlockResidents') == 1000000 and
                destination.get('unlockSources') == EXPECTED_SOURCES)
    audit.check('Golden Flower content version', catalog.get('version') == 10 and destination.get('introducedVersion') == 10)
    building = next((b for b in catalog.get('buildings', []) if b.get('id') == 'residential_house'), {})
    previous = next((b for b in before.get('buildings', []) if b.get('id') == 'residential_house'), {})
    audit.check('Final housing tier adds exactly twenty', building.get('residentTierBonuses') == EXPECTED_BONUSES)
    unchanged = bool(building and previous) and all(housing_capacity(building, level) == housing_capacity(previous, level) for level in range(1, 5000))
    audit.check('Housing population unchanged before level 5000', unchanged)
    max_per_house = housing_capacity(building, 5000) * 51 if building else 0
    real_max = 8 * 17 * max_per_house
    audit.check('Million reachable with seventeen houses and one mandatory park per source city', real_max == 1005720,
                f'8 source cities × 17 houses × {max_per_house} residents = {real_max}')
    source_plots = {city: sum(i.get('cityId', 'burning_hills') == city and i.get('family') == 'settlement_plot'
                              for i in catalog.get('instances', [])) for city in EXPECTED_SOURCES}
    audit.check('Eighteen settlement plots in each source city', all(n == 18 for n in source_plots.values()), str(source_plots))
    people = [p for p in catalog.get('vipStories', []) if p.get('cityId') == 'golden_flower']
    audit.check('Five named permanent inhabitants', [p.get('id') for p in people] == EXPECTED_PEOPLE)
    audit.check('Fifteen personal chapters and fifteen recurring variants',
                len(people) == 5 and all(len(p.get('chapters', [])) == 3 and len(p.get('recurring', [])) == 3 for p in people))
    audit.check('Four community projects', len([p for p in catalog.get('vipCollaborations', []) if p.get('cityId') == 'golden_flower']) == 4)
    audit.check('Eighteen Golden Flower main quests', len([q for q in catalog.get('quests', []) if q.get('cityId') == 'golden_flower']) == 18)
    audit.check('Current content agrees with its receipt', content_receipt.get('city') == 'golden_flower' and
                content_receipt.get('version') == catalog.get('version') and content_receipt.get('contentDigest') == content_digest(catalog))
    audit.check('Content receipt has all five languages', content_receipt.get('languages') == ['de', 'en', 'es', 'fr', 'ru'])
    audit.check('Content reference and translation checks passed', all(content_receipt.get(key) is True for key in
                ('placeholderParity', 'freshProgressGoals', 'prerequisiteGraphAcyclic', 'priorLocaleEntriesPreserved')))
    return {'maximumPerHouse': max_per_house, 'mandatoryParksPerCity': 1, 'housesPerCity': 17,
            'sourceCityCount': 8, 'maximumPreviousCitiesPopulation': real_max,
            'earlyPopulationBandsPreserved': unchanged, 'residentTierBonuses': building.get('residentTierBonuses')}


def check_authoring(audit, value):
    audit.check('Unity authoring succeeded', value.get('status') == 'passed')
    expected = {'buildings': 56, 'routes': 35, 'mainQuests': 18, 'inhabitants': 5,
                'chapters': 15, 'recurring': 15, 'landmarks': 4, 'architecturePrefabs': 4, 'buildingVariants': 251}
    for key, count in expected.items():
        audit.check('Authored ' + key, value.get(key) == count, str(value.get(key)))
    audit.check('Static city objects are authored assets', value.get('runtimeStaticObjects') is False)


def check_capture(audit, value, layout):
    audit.check('Actual PlayMode Game View receipt passed', value.get('status') == 'passed' and
                value.get('mode') == 'actual_playmode_gameview_with_diagnostic_growth')
    audit.check('Rendered city and NPC counts', value.get('cities') == 9 and value.get('inhabitants') == 5)
    audit.check('Saved story progress and return travel', value.get('savedStoryRoundtrip') is True and
                value.get('returnedToPreviousCity') is True)
    audit.check('Housing inspector and final tier preview match the simulation', value.get('housingInspectionMatchesSimulation') is True)
    audit.check('Captured completed personal and community progress', value.get('completedPersonalChapters') == 15 and
                value.get('completedCommunityProjects') == 4)
    paths = value.get('screenshots', [])
    dimensions = value.get('dimensions', [])
    audit.check('Fifteen unique Game View screenshots', len(paths) == 15 and len(set(paths)) == 15 and len(dimensions) == 15)
    observed = set()
    records = []
    for raw in paths:
        path = resolve(raw)
        try:
            with path.open('rb') as stream:
                header = stream.read(24)
            if len(header) != 24 or header[:8] != b'\x89PNG\r\n\x1a\n' or header[12:16] != b'IHDR':
                raise ValueError('Not a valid PNG header')
            width, height = struct.unpack('>II', header[16:24])
            matches = [d for d in dimensions if resolve(d.get('path', '')).resolve() == path.resolve()]
            valid = len(matches) == 1 and matches[0].get('width') == width and matches[0].get('height') == height
            audit.check('PNG matches captured dimensions: ' + path.name, valid)
            observed.add((width, height))
            records.append({'path': str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),
                            'width': width, 'height': height, 'sha256': sha256(path)})
        except (OSError, ValueError, struct.error) as error:
            audit.check('Readable captured PNG: ' + str(path), False, str(error))
    audit.check('All four required resolutions rendered', EXPECTED_RESOLUTIONS <= observed, str(sorted(observed)))
    required_labels = {f'{w}x{h}' for w, h in EXPECTED_RESOLUTIONS}
    audit.check('UI overflow inspection passed at all four resolutions', layout.get('passed') is True and
                required_labels <= set(layout.get('resolutions', [])) and layout.get('issues') == [])
    return records


def check_preservation(audit, value):
    audit.check('Preservation report passed', value.get('status') == 'passed')
    for key in ('removedFiles', 'unexpectedChanges', 'previousContentChanges'):
        audit.check('Preservation has no ' + key, value.get(key) == [])
    locale = value.get('previousLocaleChanges', {})
    audit.check('Previous locale entries preserved', len(locale) == 5 and all(changes == [] for changes in locale.values()))
    audit.check('Save migration changes only its supported version ceiling', value.get('migrationOnly') is True)
    scenes = value.get('scenes', {})
    audit.check('Existing scene objects preserved', len(scenes) >= 8 and all(
        s.get('removedObjects') == [] and s.get('unexpectedChangedObjects') == [] for s in scenes.values()))


def report(summary):
    status = summary['status']
    lines = ['Golden Flower – Integrationsabschluss', '', 'Technischer Integrationsstatus: ' + status,
             'Stand (UTC): ' + summary['utc'], '',
             ('Golden Flower ist als neunte Stadt ab insgesamt 1.000.000 Einwohnern in den acht bisherigen Städten integriert.'
              if status == 'passed' else 'Golden Flowers technische Integration ist noch nicht vollständig verifiziert.'),
             'Die fünf dauerhaften Bewohner sind Alma Sonnenberg, Ida Kern, Milan Sommer, Nora Winter und Theo Licht.',
             'Integrationsumfang: 56 Gebäude, 35 Lieferwege, 18 Hauptquests, 15 persönliche Kapitel, 15 wiederkehrende Aufgaben und vier Gemeinschaftsprojekte.', '',
             'Einwohnerkapazität und Erhaltung:',
             'Jede bisherige Stadt besitzt 18 Siedlungsplätze; die Spielregeln verlangen mindestens einen Park. Mit 17 Wohnhäusern je Stadt beträgt die maximale Gesamtbevölkerung 1.005.720: 8 × 17 × (125 + 20) × 51.',
             'Der Faktor 51 setzt jeweils das Einwohnerattribut eines Hausmeisters auf Teamstufe 100 und Rang 10 voraus. Der zusätzliche Wohnhaustierbonus gilt ausschließlich ab Gebäudestufe 5.000. Alle früheren Einwohnerstufen werden gegen den Ausgangskatalog verglichen.', '',
             'Testbelege:']
    for row in summary['tests']:
        path = resolve(row['receipt'])
        if row.get('countsVerified'):
            lines.append(f"- {row['mode']} / {row['run']}: {row['passed']}/{row['total']} bestanden, {row['failed']} fehlgeschlagen, {row['skipped']} übersprungen. " + link(path))
        else:
            lines.append('- ' + str(row['run']) + ': kein gültiger vollständiger Testbeleg. ' + link(path))
    lines += ['', 'Zusammengesetzte Testbelege verwenden je eindeutigem Testfall das jüngste tatsächlich aufgezeichnete Ergebnis. Ursprüngliche Fehlerläufe, Nachtests und ihre Prüfsummen bleiben verlinkt erhalten; überlappende Fälle werden nicht doppelt gezählt.', '',
              'Sichtbare Integration und Speichern:',
              f"Verifizierte PNG-Dateien: {len(summary['screenshots'])}. Die geforderten Ansichten umfassen 1920×1080, 1366×768, 2560×1080 und 1080×1920.",
              'Das Game-View-Receipt prüft Einladung, Stadtwechsel, fünf sichtbare Bewohner, abgeschlossene persönliche und gemeinsame Fortschrittszustände, Save-Roundtrip und Rückkehr nach Burning Hills.', '',
              'Belegdateien:']
    for name in ('content_receipt.json', 'authoring_receipt.json', 'gameview_receipt.json', 'ui_overflow.json', 'Preservation/result.json', 'validation_summary.json'):
        lines.append('- ' + link(DOC / name))
    for row in summary['screenshots']:
        lines.append('- ' + link(resolve(row['path'])))
    foundation = ('Der Foundation-Test ist im verlinkten XML als bestanden nachgewiesen: Die ersten zwölf Hauptquests wurden ausschließlich über öffentliche Spielbefehle mit der Charter-Starthilfe simuliert.'
                  if summary['naturallySimulatedMainQuestCount'] == 12 else
                  'Der Test für die ersten zwölf Hauptquests über öffentliche Spielbefehle mit der Charter-Starthilfe ist in den angegebenen Ergebnissen noch nicht ausdrücklich als bestanden nachgewiesen.')
    lines += ['', 'Grenzen der Abnahme:',
              'Die Wachstums- und Storyzustände der Game-View-Diagnostik werden gezielt vorbereitet. Die Bilder und der Speichervergleich belegen Integration und Darstellung dieser Zustände, keinen natürlichen vollständigen Durchlauf.',
              foundation,
              'Ein menschlicher Durchlauf aller 18 Hauptquests, der acht vorherigen Städte bis zur Million sowie eine vollständige Prüfung der Langzeit-Spielzeit wurden hier nicht durchgeführt.',
              'Physische Touchbedienung, Zielgeräte-, Release- und ausdrückliche Nutzerfreigabe der neuen Kunst sind offen. Automatische Game-View-, Test- und UI-Prüfungen erteilen diese Freigaben nicht.']
    if summary['errors']:
        lines += ['', 'Noch fehlende oder ungültige Belege:'] + ['- ' + item for item in summary['errors']]
    return '\n'.join(lines) + '\n'


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tests', nargs='+', required=True, metavar='RESULT_JSON',
                        help='Completed test receipt JSONs; every submitted run must be fully passed.')
    args = parser.parse_args(argv)
    audit = Audit()
    content_receipt = audit.read(DOC / 'content_receipt.json')
    authoring = audit.read(DOC / 'authoring_receipt.json')
    capture = audit.read(DOC / 'gameview_receipt.json')
    layout = audit.read(DOC / 'ui_overflow.json')
    preservation = audit.read(DOC / 'Preservation/result.json')
    catalog, before = audit.read(CONTENT), audit.read(BASELINE)
    test_rows = tests(audit, args.tests)
    population = check_catalog(audit, catalog, before, content_receipt)
    check_authoring(audit, authoring)
    screenshots = check_capture(audit, capture, layout)
    check_preservation(audit, preservation)
    summary = {'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
               'status': 'passed' if not audit.errors else 'failed', 'scope': 'technical_city_integration',
               'tests': test_rows, 'population': population, 'screenshots': screenshots,
               'checks': audit.checks, 'errors': audit.errors,
               'diagnosticGrowthFixtures': True,
               'naturallySimulatedMainQuestCount': 12 if any(r['foundationTwelveMainQuestsPassed'] for r in test_rows) else 0,
               'fullHumanPlaythrough': False, 'physicalDeviceAcceptance': False,
               'releaseAccepted': False, 'userArtApproval': False}
    DOC.mkdir(parents=True, exist_ok=True)
    (DOC / 'validation_summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (DOC / 'IntegrationReport.txt').write_text(report(summary), encoding='utf-8')
    print(json.dumps({'status': summary['status'], 'validTestRuns': sum(r['countsVerified'] for r in test_rows),
                      'screenshots': len(screenshots), 'maximumPreviousCitiesPopulation': population['maximumPreviousCitiesPopulation'],
                      'failedChecks': audit.errors, 'summary': str(DOC / 'validation_summary.json'),
                      'report': str(DOC / 'IntegrationReport.txt')}, ensure_ascii=False, indent=2))
    return 0 if summary['status'] == 'passed' else 1


if __name__ == '__main__':
    sys.exit(main())
