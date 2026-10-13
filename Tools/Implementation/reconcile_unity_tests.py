"""Derive transparent latest-per-case evidence from a full run and targeted retests.

Original JSON/XML/log files remain untouched. The output is composite evidence,
not a claim that one Unity process completed the combined set of tests.
Usage: python reconcile_unity_tests.py --runs original.json retest.json --output combined.json
"""
from __future__ import annotations

import argparse
import copy
import datetime
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
COUNT_KEYS = ('total', 'passed', 'failed', 'skipped', 'inconclusive')


def resolve(value):
    path = Path(value)
    return path if path.is_absolute() else ROOT / path


def count(value):
    if isinstance(value, bool):
        raise ValueError('Boolean is not a test count')
    number = int(value)
    if number < 0 or str(value).strip() != str(number):
        raise ValueError('Invalid integer test count: ' + str(value))
    return number


def utc(value, label):
    if not isinstance(value, str):
        raise ValueError(label + ' is missing')
    parsed = datetime.datetime.fromisoformat(value.replace('Z', '+00:00'))
    if parsed.tzinfo is None or parsed.utcoffset() != datetime.timedelta(0):
        raise ValueError(label + ' must include UTC timezone')
    return parsed


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def case_counts(cases):
    results = [case.get('result') for case in cases]
    unsupported = {result for result in results if result not in ('Passed', 'Failed')}
    if unsupported:
        raise ValueError('Skipped, inconclusive or unsupported case results: ' + str(unsupported))
    return {'total': len(results), 'passed': results.count('Passed'), 'failed': results.count('Failed'),
            'skipped': 0, 'inconclusive': 0}


def load_run(path, position):
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    if not isinstance(data, dict):
        raise ValueError('Run receipt must be a JSON object: ' + str(path))
    if data.get('evidenceKind') == 'composite-evidence':
        raise ValueError('Use original Unity receipts; nested composites are not accepted: ' + str(path))
    xml_path = resolve(data['xml'])
    xml = ET.parse(xml_path).getroot()
    if xml.tag != 'test-run':
        raise ValueError('Expected NUnit test-run XML: ' + str(xml_path))
    if xml.get('evidence-kind') == 'composite-evidence':
        raise ValueError('XML is already derived evidence: ' + str(xml_path))
    cases = list(xml.iter('test-case'))
    names = [case.get('fullname') for case in cases]
    if not cases or any(not name or not name.strip() for name in names) or len(set(names)) != len(names):
        raise ValueError('Every original run must contain unique, named test cases: ' + str(path))
    actual = case_counts(cases)
    raw_counts = {key: count(data[key]) if key in data else 0 for key in COUNT_KEYS}
    for required in ('total', 'passed', 'failed', 'skipped'):
        if required not in data:
            raise ValueError('Missing JSON count ' + required + ': ' + str(path))
    xml_counts = {key: count(xml.get(key, '0')) for key in COUNT_KEYS}
    if actual != raw_counts or actual != xml_counts:
        raise ValueError('JSON/header/actual-case counts disagree: ' + str(path) +
                         '\nJSON=' + str(raw_counts) + '\nXML=' + str(xml_counts) + '\nCases=' + str(actual))
    expected_status = 'passed' if actual['failed'] == 0 else 'failed'
    if data.get('status') != expected_status:
        raise ValueError('Original JSON status disagrees with its cases: ' + str(path))
    expected_xml = xml.get('result', '') == 'Passed' if expected_status == 'passed' else xml.get('result', '').startswith('Failed')
    if not expected_xml:
        raise ValueError('XML root result disagrees with its cases: ' + str(path))
    exit_code = data.get('exitCode')
    if exit_code not in (0, 2) or (expected_status == 'passed' and exit_code != 0):
        raise ValueError('Unsupported original Unity process exit code: ' + str(path))
    start, end = utc(data.get('startedUtc'), 'startedUtc'), utc(data.get('endedUtc'), 'endedUtc')
    if end < start:
        raise ValueError('Run end precedes start: ' + str(path))
    mode = data.get('mode', data.get('stage'))
    if mode not in ('EditMode', 'PlayMode'):
        raise ValueError('Only original EditMode or PlayMode Unity runs are accepted: ' + str(path))
    source = {'inputOrder': position, 'receipt': str(path), 'xml': str(xml_path),
              'receiptSha256': digest(path), 'xmlSha256': digest(xml_path),
              'run': data.get('run', path.stem), 'mode': mode, 'originalStatus': data['status'],
              'originalXmlResult': xml.get('result'), 'originalExitCode': exit_code,
              'startedUtc': data['startedUtc'], 'endedUtc': data['endedUtc'],
              'originalCounts': raw_counts, 'jsonAndXmlCountsMatched': True,
              'originalLog': data.get('log'),
              'failedCases': [case.get('fullname') for case in cases if case.get('result') == 'Failed']}
    return source, cases, start, end


def reconcile(paths, output):
    if len(paths) < 2:
        raise ValueError('Provide the original run and at least one recorded targeted retest.')
    xml_output = output.with_suffix('.xml')
    if output.resolve() == xml_output.resolve():
        raise ValueError('--output must be a JSON filename, distinct from the derived XML.')
    sources, latest, history = [], {}, {}
    original_names = None
    last_end = None
    mode = None
    protected = set()
    for position, path in enumerate(paths):
        if path.resolve() in protected:
            raise ValueError('Duplicate source run: ' + str(path))
        source, cases, start, end = load_run(path, position)
        protected.update((path.resolve(), resolve(source['xml']).resolve()))
        if last_end is not None and end < last_end:
            raise ValueError('Input order must follow endedUtc; source receipts are not silently reordered.')
        if last_end is not None and start < last_end:
            raise ValueError('Targeted retest started before the preceding run completed.')
        if mode is not None and source['mode'] != mode:
            raise ValueError('Cannot reconcile different Unity test modes.')
        mode, last_end = source['mode'], end
        names = {case.get('fullname') for case in cases}
        if original_names is None:
            original_names = names
        elif not names <= original_names:
            raise ValueError('A targeted retest introduced unknown case names: ' + str(sorted(names - original_names)))
        sources.append(source)
        for case in cases:
            name = case.get('fullname')
            event = {'inputOrder': position, 'run': source['run'], 'receipt': source['receipt'],
                     'xml': source['xml'], 'result': case.get('result'), 'endedUtc': source['endedUtc']}
            if case.get('result') == 'Failed':
                event['failureMessages'] = [item.text or '' for item in case.findall('./failure/message')]
            history.setdefault(name, []).append(event)
            latest[name] = (copy.deepcopy(case), event)
    if output.resolve() in protected or xml_output.resolve() in protected:
        raise ValueError('Output must not overwrite any original receipt or XML.')
    unique_counts = case_counts([case for case, _ in latest.values()])
    corrected = []
    unresolved = []
    for name, events in history.items():
        failures = [event for event in events if event['result'] == 'Failed']
        if failures and events[-1]['result'] == 'Passed':
            corrected.append({'fullname': name, 'originalFailures': failures, 'latestPassedEvidence': events[-1]})
        elif events[-1]['result'] == 'Failed':
            unresolved.append({'fullname': name, 'latestFailure': events[-1]})
    status = 'passed' if unique_counts['failed'] == 0 else 'failed'
    now = datetime.datetime.now(datetime.timezone.utc).isoformat()
    run = ET.Element('test-run', {'id': 'composite-evidence', 'result': 'Passed' if status == 'passed' else 'Failed',
                                  'evidence-kind': 'composite-evidence',
                                  'evidence-policy': 'latest actual recorded test-case per fullname in verified input time order',
                                  'testcasecount': str(unique_counts['total']),
                                  **{key: str(value) for key, value in unique_counts.items()}})
    properties = ET.SubElement(run, 'properties')
    ET.SubElement(properties, 'property', {'name': 'evidenceKind', 'value': 'composite-evidence'})
    ET.SubElement(properties, 'property', {'name': 'singleUnityProcess', 'value': 'false'})
    ET.SubElement(properties, 'property', {'name': 'originalReceiptsUnchanged', 'value': 'true'})
    ET.SubElement(properties, 'property', {'name': 'jsonProvenance', 'value': str(output)})
    provenance = ET.SubElement(run, 'composite-evidence', {'generatedUtc': now, 'mode': mode})
    for source in sources:
        ET.SubElement(provenance, 'source-run', {'input-order': str(source['inputOrder']), 'receipt': source['receipt'],
                                               'xml': source['xml'], 'original-status': source['originalStatus'],
                                               'original-exit-code': str(source['originalExitCode']),
                                               'original-passed': str(source['originalCounts']['passed']),
                                               'original-failed': str(source['originalCounts']['failed']),
                                               'receipt-sha256': source['receiptSha256'], 'xml-sha256': source['xmlSha256']})
    suite = ET.SubElement(run, 'test-suite', {'type': 'CompositeEvidence', 'name': output.stem,
                                            'fullname': output.stem, 'result': run.get('result'),
                                            'testcasecount': str(unique_counts['total']),
                                            **{key: str(value) for key, value in unique_counts.items()}})
    # Copy the actual latest case element, including its original timing, failure
    # or output children. Never manufacture a Passed result or rewrite a case.
    for name in sorted(latest):
        suite.append(latest[name][0])
    summary = {'run': output.stem, 'mode': mode + ' (combined recorded runs)',
               'evidenceKind': 'composite-evidence', 'singleUnityProcess': False,
               'startedUtc': sources[0]['startedUtc'], 'endedUtc': sources[-1]['endedUtc'],
               'generatedUtc': now, 'status': status, 'exitCode': 0 if status == 'passed' else 1,
               **unique_counts, 'xml': str(xml_output), 'inputOrderPreserved': True,
               'endedUtcOrderVerified': True, 'sourceRawRuns': sources,
               'originalRunStatuses': [{'run': s['run'], 'status': s['originalStatus'],
                                        'exitCode': s['originalExitCode'], **s['originalCounts']} for s in sources],
               'rawReceipts': [s['receipt'] for s in sources], 'correctedFailures': corrected,
               'unresolvedFailures': unresolved,
               'uniqueCases': [{'fullname': name, 'latestResult': event['result'],
                                'latestSourceReceipt': event['receipt'], 'latestSourceXml': event['xml'],
                                'latestRun': event['run'], 'recordedHistory': history[name]}
                               for name, (_, event) in sorted(latest.items())],
               'reason': 'The original run and its failures remain linked. Only actual later recorded results supersede the same named case; runtime or test-source equivalence is not asserted by this tool.'}
    # Check that all read source files still have their original hashes before
    # the first output write, protecting against concurrent changes or misuse.
    for source in sources:
        if digest(resolve(source['receipt'])) != source['receiptSha256'] or digest(resolve(source['xml'])) != source['xmlSha256']:
            raise ValueError('An original source changed during reconciliation: ' + source['receipt'])
    output.parent.mkdir(parents=True, exist_ok=True)
    ET.indent(run)
    ET.ElementTree(run).write(xml_output, encoding='utf-8', xml_declaration=True)
    summary['derivedXmlSha256'] = digest(xml_output)
    output.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    return summary


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runs', nargs='+', required=True, metavar='ORIGINAL_JSON',
                        help='Original full run followed by actual targeted retests in chronological order.')
    parser.add_argument('--output', required=True, metavar='COMBINED_JSON')
    args = parser.parse_args(argv)
    try:
        summary = reconcile([resolve(path) for path in args.runs], resolve(args.output))
        print(json.dumps({'status': summary['status'], 'evidenceKind': summary['evidenceKind'],
                          'total': summary['total'], 'passed': summary['passed'], 'failed': summary['failed'],
                          'originalRunStatuses': summary['originalRunStatuses'],
                          'correctedFailures': [row['fullname'] for row in summary['correctedFailures']],
                          'xml': summary['xml'], 'output': str(resolve(args.output))}, ensure_ascii=False, indent=2))
        return 0 if summary['status'] == 'passed' else 1
    except (OSError, ValueError, KeyError, ET.ParseError) as error:
        print('Cannot reconcile Unity evidence: ' + str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
