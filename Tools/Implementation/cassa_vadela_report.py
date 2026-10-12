"""Collect current Cassa Vadela evidence without equating local validation with release acceptance."""
from __future__ import annotations
import datetime
import hashlib
import json
import pathlib
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/CassaVadela'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def latest(folder, pattern):
    paths = list(folder.glob(pattern))
    if not paths:
        raise RuntimeError('Missing evidence ' + pattern)
    return read(max(paths, key=lambda path: path.stat().st_mtime))

def native_tests(receipts):
    cases = {}
    for receipt in receipts:
        for case in ET.parse(receipt['xml']).getroot().iter('test-case'):
            cases[case.get('fullname')] = case.get('result')
    return dict(total=len(cases), passed=sum(value == 'Passed' for value in cases.values()),
                failed=[name for name, value in cases.items() if value != 'Passed'], receipts=receipts)

def report():
    source = read(ROOT / 'Docs/Implementation/Validation/SourceCompile/result.json')
    stale = [path for result in source['results'] for path, expected in result['sources'].items()
             if not (ROOT / path).is_file() or hashlib.sha256((ROOT / path).read_bytes()).hexdigest() != expected]
    content = read(DOC / 'content_validation.json')
    preservation = read(DOC / 'Preservation/result.json')
    references = read(DOC / 'asset_reference_validation.json')
    art = read(DOC / 'art_visual_review.json')
    foundation = read(DOC / 'foundation_simulation.json')
    install = latest(DOC / 'Validation', '*Install_FinalStages.json')
    validations = ROOT / 'Docs/Implementation/Validation'
    domain = native_tests([latest(validations, '*EditMode_CassaVadelaDomain.json'),
                           latest(validations, '*EditMode_CassaVadelaFinalContent.json')])
    assets = native_tests([latest(validations, '*EditMode_CassaVadelaAssets.json')])
    play_receipts = [latest(validations, '*PlayMode_CassaVadelaPlay.json')]
    fountain_final = list(validations.glob('*PlayMode_CassaVadelaFountainFinal.json'))
    if fountain_final:
        play_receipts.append(read(max(fountain_final, key=lambda path: path.stat().st_mtime)))
    visual_final_tests = list(validations.glob('*PlayMode_CassaVadelaVisualFinal.json'))
    if visual_final_tests:
        play_receipts.append(read(max(visual_final_tests, key=lambda path: path.stat().st_mtime)))
    play = native_tests(play_receipts)
    capture = latest(DOC / 'Validation', '*Capture_FinalGameViews.json')
    gameview = read(DOC / 'gameview_receipt.json')
    layout = read(DOC / 'ui_overflow.json')
    visual = read(DOC / 'visual_review.json')
    art_errors = [row['path'] for row in art['files'] if
        not (ROOT / row['path']).is_file() or
        hashlib.sha256((ROOT / row['path']).read_bytes()).hexdigest() != row['sha256'].lower()]
    reviewed = visual.get('reviewedScreenshots', [])
    expected_paths = {str(pathlib.Path(path).resolve()) for path in gameview['screenshots']}
    reviewed_paths = [str(pathlib.Path(row['path']).resolve()) for row in reviewed]
    visual_errors = [row['path'] for row in reviewed if row.get('status') != 'passed' or
        not pathlib.Path(row['path']).is_file() or
        hashlib.sha256(pathlib.Path(row['path']).read_bytes()).hexdigest() != row['sha256'].lower()]
    missing_visual = sorted(expected_paths - set(reviewed_paths))
    unexpected_visual = sorted(set(reviewed_paths) - expected_paths)
    dimensions = gameview['captureDimensions']
    dimension_errors = [row['path'] for row in dimensions if
        row['requestedWidth'] != row['renderedWidth'] or row['requestedHeight'] != row['renderedHeight'] or
        row['pngWidth'] != row['renderedWidth'] or row['pngHeight'] != row['renderedHeight']]
    current_hash = hashlib.sha256((ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json').read_bytes().decode('utf-8-sig').encode('utf-8')).hexdigest().upper()
    gates = {
        'sourceCompile': source['status'] == 'passed' and not stale,
        'content': content.get('idempotenceVerified', False) and content.get('priorFourCitySnapshotPreserved', False),
        'unityAuthoring': install['status'] == 'passed',
        'domain': domain['total'] == 429 and not domain['failed'],
        'editorAssets': assets['total'] >= 20 and not assets['failed'],
        'playMode': play['total'] >= 52 and not play['failed'],
        'foundation': foundation['status'] == 'passed' and foundation['contentHash'] == current_hash and
            all(foundation.get(key) for key in ('noPremiumSpend', 'noRecovery', 'noPreviousCityIncome', 'previousWorkersPreserved')),
        'assetReferences': references['status'] == 'passed',
        'sourceArt': art['status'] == 'passed' and art['filesReviewed'] == 12 and not art_errors,
        'preservation': preservation['status'] == 'passed',
        'gameView': capture['status'] == 'passed' and len(gameview['screenshots']) == 80 and not dimension_errors,
        'uiLayout': layout['passed'] and layout['fingerprint'] == gameview['fingerprint'],
        'visualReview': visual['status'] == 'passed' and visual['fingerprint'] == gameview['fingerprint'] and
            len(reviewed_paths) == len(set(reviewed_paths)) == 80 and not
            (visual_errors or missing_visual or unexpected_visual),
    }
    result = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        implementationStatus='verified' if all(gates.values()) else 'incomplete', gates=gates,
        sourceAssemblies=len(source['results']), staleSources=stale, domain=domain, editorAssets=assets, playMode=play,
        foundation=foundation, capture=capture, screenshots=80, fingerprint=gameview['fingerprint'],
        artHashErrors=art_errors, visualHashErrors=visual_errors, missingVisualReviews=missing_visual,
        unexpectedVisualReviews=unexpected_visual,
        testAttemptHistory=[read(path) for pattern in ('*EditMode_CassaVadelaAssets.json', '*PlayMode_CassaVadelaPlay.json',
                            '*PlayMode_CassaVadelaFountainFinal.json', '*PlayMode_CassaVadelaVisualFinal.json')
                            for path in sorted(validations.glob(pattern))],
        historicalVisualEvidence='Docs/CassaVadela/Validation/20261008_065205_VisualBeforeRepair',
        protectedFiles=preservation['protectedFiles'], preservation=preservation,
        releaseAccepted=False, userVisualAcceptance=False, physicalTouchVerified=False,
        deviceAudioVerified=False, playerBuildVerified=False, humanCompletePlaythroughVerified=False,
        note='Visual growth fixtures demonstrate authored states and persistence; the separate public-command foundation proves local starting-budget viability.')
    (DOC / 'validation_summary.json').write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps({key: result[key] for key in ('implementationStatus','gates','sourceAssemblies','staleSources','protectedFiles','screenshots')}, indent=2))
    return 0 if all(gates.values()) else 1

if __name__ == '__main__':
    raise SystemExit(report())
