"""Combine dated editor evidence, keeping failed attempts and device gates explicit."""
import argparse
import datetime
import hashlib
import json
import pathlib
import re
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'Docs/PhoneUiRevision'
parser = argparse.ArgumentParser()
parser.add_argument('--build-id')
args = parser.parse_args()
utc = datetime.datetime.now(datetime.timezone.utc).isoformat()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def write(path, data):
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding='utf-8')


def combine(files):
    cases = {}
    runs = []
    for name in files:
        path = FOLDER / 'Tests' / (name + '.xml')
        tree = ET.parse(path)
        runs.append(dict(file=path.relative_to(ROOT).as_posix(),
                         result=tree.getroot().get('result'),
                         end=tree.getroot().get('end-time')))
        for case in tree.iter('test-case'):
            cases[case.get('fullname')] = dict(result=case.get('result'), source=name)
    failed = {name: value for name, value in cases.items() if value['result'] != 'Passed'}
    return dict(uniqueCases=len(cases), passed=len(cases) - len(failed),
                unresolved=failed, runs=runs, latestResultPerCase=cases,
                singleFullRunPassed=len(files) == 1 and not failed)


protected = read(FOLDER / 'Preservation/unchanged-baseline.json')
changed = [entry['path'] for entry in protected
           if not pathlib.Path(entry['path']).is_file()
           or hashlib.sha256(pathlib.Path(entry['path']).read_bytes()).hexdigest().upper() != entry['sha256'].upper()]
scenes = []
for name in ['BurningHills.unity', 'BigRockCity.unity']:
    before = FOLDER / 'Preservation' / (name + '.before')
    current = ROOT / ('Assets/Weedtopia/Game/Scenes/' + name)
    if name == 'BigRockCity.unity':
        current = ROOT / 'Assets/Weedtopia/Production/BigRockCity/BigRockCity.unity'
    if not current.is_file():
        matches = list((ROOT / 'Assets/Weedtopia').rglob(name))
        if len(matches) != 1:
            raise RuntimeError('Cannot identify current scene: ' + name)
        current = matches[0]
    object_ids = lambda path: set(re.findall(r'^--- !u!\d+ &(-?\d+)', path.read_text(encoding='utf-8-sig'), re.M))
    old, new = object_ids(before), object_ids(current)
    scenes.append(dict(name=name, beforeSerializedObjects=len(old), retained=len(old & new),
                       added=len(new - old), removed=sorted(old - new)))
preserved = not changed and all(not scene['removed'] for scene in scenes)
preservation = dict(utc=utc, status='passed' if preserved else 'failed', scenes=scenes,
                    protectedFiles=len(protected), changedProtectedFiles=changed)
write(FOLDER / 'Preservation/result.json', preservation)
tests = dict(utc=utc, playMode=combine(['PlayMode', 'PlayModeFocused']),
             editMode=combine(['EditMode', 'AssetRecheck']))
tests['passed'] = not tests['playMode']['unresolved'] and not tests['editMode']['unresolved']
write(FOLDER / 'Tests/combined-result.json', tests)
capture = read(FOLDER / 'capture_receipt.json')
source = read(ROOT / 'Docs/Implementation/Validation/SourceCompile/result.json')
summary = dict(utc=utc, status='passed' if preserved and tests['passed'] and capture['complete']
               and not capture['issues'] and source['status'] == 'passed' else 'failed',
               authoring=read(FOLDER / 'authoring_receipt.json'),
               sourceAssemblies=len(source['results']), sourceCompilation=source['status'],
               tests=dict(playModeUnique=tests['playMode']['uniqueCases'],
                          playModePassed=tests['playMode']['passed'],
                          editModeUnique=tests['editMode']['uniqueCases'],
                          editModePassed=tests['editMode']['passed'],
                          focusedRerunsUsed=True, receipt='Tests/combined-result.json'),
               captures=dict(utc=capture['utc'], count=len(capture['screenshots']),
                             checks=capture['checks'], issues=capture['issues']),
               preservation=preservation, physicalDeviceVerified=False,
               userVisualApproval=False, releaseAccepted=False)
if args.build_id:
    build_folder = ROOT / 'Builds/Android' / args.build_id
    summary['androidBuild'] = read(build_folder / 'build-result.json')
    summary['apkVerification'] = read(build_folder / 'apk-verification.json')
    if summary['androidBuild']['status'] != 'succeeded' or not summary['apkVerification']['passed']:
        summary['status'] = 'failed'
write(FOLDER / 'summary.json', summary)
print(json.dumps({key: value for key, value in summary.items()
                  if key in ['utc', 'status', 'sourceAssemblies', 'tests', 'captures', 'physicalDeviceVerified']}, indent=2))
raise SystemExit(0 if summary['status'] == 'passed' else 1)
