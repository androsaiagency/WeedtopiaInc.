"""Gather current Mark Wood City evidence without turning unrun gates into acceptance."""
import datetime
import hashlib
import json
import pathlib
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/MarkWoodCity'
DOMAIN_CASE_COUNT = 450
DOMAIN_RETEST_CLASS = 'MarkWoodCityCampaignTests'
DOMAIN_RETEST_CASE_COUNT = 21


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig')) if path.is_file() else None


def latest(label):
    candidates = sorted((ROOT / 'Docs/Implementation/Validation').glob('*' + label + '.json'))
    return read(candidates[-1]) if candidates else None


def test_cases(result):
    """Read case identities, retaining the run receipt and failure messages separately."""
    issues = []
    if not result or not result.get('xml'):
        return {}, ['Missing run receipt or XML path']
    path = pathlib.Path(result['xml'])
    if not path.is_absolute():
        path = ROOT / path
    if not path.is_file():
        return {}, ['Missing XML: ' + str(path)]
    try:
        xml = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as error:
        return {}, ['Unreadable XML: ' + str(error)]
    cases = {}
    for case in xml.iter('test-case'):
        fullname = case.get('fullname')
        if not fullname:
            issues.append('Test case has no fullname')
            continue
        if fullname in cases:
            issues.append('Duplicate test fullname: ' + fullname)
            continue
        cases[fullname] = {
            'fullname': fullname, 'className': case.get('classname', ''),
            'result': case.get('result', ''), 'run': result.get('run'),
            'failureMessage': case.findtext('failure/message'),
        }
    return cases, issues


def case_counts(cases):
    return {
        'total': len(cases),
        'passed': sum(case['result'] == 'Passed' for case in cases.values()),
        'failed': sum(case['result'] == 'Failed' for case in cases.values()),
        'notPassed': sorted(name for name, case in cases.items() if case['result'] != 'Passed'),
    }


def is_domain_retest_case(case):
    return case['className'].split('.')[-1] == DOMAIN_RETEST_CLASS


def aggregate_domain(full_run, retest):
    """A successful targeted rerun supersedes only the same named cases from the full run.

    The failed full run remains evidence. Passing requires the complete 450-case
    baseline and a newer, successful rerun of all 21 corrected-class cases.
    """
    original, original_issues = test_cases(full_run)
    repeated, retest_issues = test_cases(retest)
    issues = ['Full run: ' + issue for issue in original_issues]
    issues.extend('Retest: ' + issue for issue in retest_issues)
    expected_retests = {name for name, case in original.items() if is_domain_retest_case(case)}
    originally_failed = {name for name, case in original.items() if case['result'] == 'Failed'}
    unexpected_retests = set(repeated) - expected_retests
    missing_retests = expected_retests - set(repeated)
    if len(original) != DOMAIN_CASE_COUNT:
        issues.append(f'Full run must contain {DOMAIN_CASE_COUNT} unique test fullnames')
    if len(expected_retests) != DOMAIN_RETEST_CASE_COUNT:
        issues.append(f'Full run must contain {DOMAIN_RETEST_CASE_COUNT} {DOMAIN_RETEST_CLASS} cases')
    if unexpected_retests:
        issues.append('Retest contains unexpected fullnames')
    if missing_retests:
        issues.append('Retest does not cover the complete corrected class')
    if not retest or retest.get('status') != 'passed':
        issues.append('Retest run receipt is not passed')
    for label, receipt, cases in [('Full run', full_run, original), ('Retest', retest, repeated)]:
        counts = case_counts(cases)
        if receipt and any(receipt.get(key) != counts[key] for key in ('total', 'passed', 'failed')):
            issues.append(label + ' receipt counts do not match its XML cases')
    retest_is_newer = False
    try:
        full_end = datetime.datetime.fromisoformat(full_run['endedUtc'].replace('Z', '+00:00'))
        retest_start = datetime.datetime.fromisoformat(retest['startedUtc'].replace('Z', '+00:00'))
        retest_end = datetime.datetime.fromisoformat(retest['endedUtc'].replace('Z', '+00:00'))
        retest_is_newer = retest_start >= full_end and retest_end >= retest_start
    except (AttributeError, TypeError, KeyError, ValueError):
        pass
    if not retest_is_newer:
        issues.append('Retest must have completed after the selected full run')
    successful_retests = {name for name, case in repeated.items() if case['result'] == 'Passed'}
    uncovered_failures = originally_failed - successful_retests
    if uncovered_failures:
        issues.append('Original failures are not covered by successful retests')
    final = {name: dict(case, evidence='fullRun') for name, case in original.items()}
    # Stale retests and cases outside the corrected class cannot supersede evidence.
    replaced = set(repeated) & expected_retests if retest_is_newer else set()
    for name in replaced:
        final[name] = dict(repeated[name], evidence='targetedRetest')
    counts = case_counts(final)
    passed = not issues and counts['total'] == DOMAIN_CASE_COUNT and counts['passed'] == DOMAIN_CASE_COUNT
    return {
        'status': 'passed' if passed else 'failed' if counts['notPassed'] else 'unverified',
        'aggregation': 'latest_result_per_unique_test_fullname',
        'total': counts['total'], 'passed': counts['passed'], 'failed': counts['failed'],
        'rawFullRun': full_run, 'targetedRetest': retest,
        'rawFullSummary': case_counts(original), 'retestSummary': case_counts(repeated),
        'expectedUniqueCases': DOMAIN_CASE_COUNT, 'expectedRetestCases': DOMAIN_RETEST_CASE_COUNT,
        'originallyFailedCases': sorted(originally_failed),
        'uncoveredOriginalFailures': sorted(uncovered_failures),
        'missingRetestCases': sorted(missing_retests), 'unexpectedRetestCases': sorted(unexpected_retests),
        'retestIsNewer': retest_is_newer, 'supersededCases': sorted(replaced),
        'verificationIssues': issues, 'cases': [final[name] for name in sorted(final)],
        'note': 'The full domain run is retained unchanged, including its failure. '
                'The corrected test preparation was rerun as a complete targeted class. '
                'Only newer results with the same test fullname supersede prior results; '
                'the final pass requires 450 unique passed cases, a complete successful '
                '21-case current retest, and coverage of every original failure.',
    }


def main():
    source = read(ROOT / 'Docs/Implementation/Validation/SourceCompile/result.json')
    stale = []
    if source:
        for assembly in source.get('results', []):
            for name, expected in assembly.get('sources', {}).items():
                path = ROOT / name
                if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                    stale.append(name)
        (DOC / 'source_compile.json').write_text(json.dumps(source, indent=2), encoding='utf-8')
    domain = aggregate_domain(latest('MarkWoodCityFullDomain'), latest('MarkWoodCityDomainCorrected'))
    assets = latest('MarkWoodCityAssets')
    play = latest('MarkWoodCityFullPlay')
    gameview = read(DOC / 'gameview_receipt.json')
    overflow = read(DOC / 'ui_overflow.json')
    visual_review = read(DOC / 'visual_review.json')
    capture_runs = sorted((DOC / 'Validation').glob('*_Capture_*.json'))
    capture = read(capture_runs[-1]) if capture_runs else None
    preservation = read(DOC / 'Preservation/result.json')
    references = read(DOC / 'asset_reference_validation.json')
    authoring = read(DOC / 'authoring_receipt.json')
    content = read(DOC / 'content_validation.json')
    idempotence = read(DOC / 'content_idempotence.json')
    contracts = read(DOC / 'content_authoring_contracts.json')
    content_ok = bool(content and content.get('priorContentPreserved') and content.get('priorLocaleEntriesPreserved') and content.get('placeholderParity'))
    gates = {
        'content': 'passed' if content_ok else 'unverified',
        'contentIdempotence': 'passed' if idempotence and idempotence.get('changed') is False else 'unverified',
        'sourceCompile': 'passed' if source and source.get('status') == 'passed' and not stale else 'unverified',
        'authoring': authoring.get('status', 'unverified') if authoring else 'unverified',
        'domain': domain.get('status', 'unverified') if domain else 'unverified',
        'editorAssets': assets.get('status', 'unverified') if assets else 'unverified',
        'playMode': play.get('status', 'unverified') if play else 'unverified',
        'gameView': capture.get('status', 'unverified') if capture else 'unverified',
        'uiLayout': 'passed' if overflow and overflow.get('passed') and gameview and overflow.get('fingerprint') == gameview.get('fingerprint') else 'unverified',
        'visualReview': 'passed' if visual_review and visual_review.get('status') == 'passed' and gameview and visual_review.get('fingerprint') == gameview.get('fingerprint') else 'unverified',
        'preservation': preservation.get('status', 'unverified') if preservation else 'unverified',
        'assetReferences': references.get('status', 'unverified') if references else 'unverified',
    }
    domain_cases = [case for case in domain['cases'] if is_domain_retest_case(case)]
    new_tests = {'domain': {'total': len(domain_cases), 'passed': sum(case['result'] == 'Passed' for case in domain_cases)}}
    for label, result, class_name in [('assets', assets, 'MarkWoodCityAssetTests'), ('playMode', play, 'MarkWoodCityPlayModeTests')]:
        if not result or not result.get('xml'):
            continue
        path = pathlib.Path(result['xml'])
        if not path.is_file():
            continue
        cases = [case for case in ET.parse(path).getroot().iter('test-case') if class_name in case.get('fullname', '')]
        new_tests[label] = {'total': len(cases), 'passed': sum(case.get('result') == 'Passed' for case in cases)}
    report = {
        'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'implementationStatus': 'verified_in_editor' if all(value == 'passed' for value in gates.values()) else 'verification_in_progress',
        'city': 'mark_wood_city', 'unlockResidents': 100000,
        'unlockSources': ['burning_hills', 'big_rock_city', 'seven_hills', 'little_montana', 'cassa_vadela'],
        'gates': gates, 'staleSources': stale, 'newTests': new_tests,
        'domain': domain, 'editorAssets': assets, 'playMode': play, 'authoring': authoring,
        'content': content, 'contentAuthoringContracts': contracts,
        'gameView': {'screenshots': len(gameview.get('screenshots', [])), 'resolutions': ['1920x1080', '1366x768', '2560x1080', '1080x1920'], 'diagnosticGrowthFixtures': True, 'fullEconomyPlayed': False} if gameview else None,
        'preservation': preservation, 'assetReferences': references, 'visualReview': visual_review,
        'acceptance': {'userVisualApproval': False, 'physicalDeviceTouch': False, 'deviceAudio': False, 'androidPlayerBuild': False, 'webglPlayerBuild': False, 'fullHumanCampaignPlaythrough': False},
        'foundationEvidence': 'Domain test advances the first twelve local main quests using real economy and assigned workers, without previous-city income, premium spending or recovery. Later story and visual coverage uses diagnostic fixtures.',
    }
    (DOC / 'validation_summary.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'implementationStatus': report['implementationStatus'], 'gates': gates, 'newTests': new_tests, 'staleSources': stale}, indent=2))
    return 0 if report['implementationStatus'] == 'verified_in_editor' else 1


if __name__ == '__main__':
    raise SystemExit(main())
