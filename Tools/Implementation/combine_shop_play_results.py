"""Combine an unchanged-runtime full run with explicit navigation-expectation/shop retests; retain both original XMLs."""
import copy
import json
import pathlib
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/ShopRevision/Tests'
baseline_path = OUT / 'PlayMode-attempt-01.xml'
focused_path = OUT / 'PlayModeFocused.xml'
tree = ET.parse(baseline_path)
root = tree.getroot()
focused = ET.parse(focused_path).getroot()
assert focused.get('result') == 'Passed' and focused.get('failed') == '0'
cases = {c.get('fullname'): c for c in root.iter('test-case')}
rechecks = {c.get('fullname'): c for c in focused.iter('test-case')}
assert len(cases) == 89 and len(rechecks) == 4 and rechecks.keys() <= cases.keys()
failures = {k for k, v in cases.items() if v.get('result') != 'Passed'}
assert failures == {'Weedtopia.Tests.PlayMode.BuildingInteractionTests.FreeWorldHasNoPersistentPanelsOrPositionScrollbar'}
parents = {child: parent for parent in root.iter() for child in parent}
for name, replacement in rechecks.items():
    previous = cases[name]
    parent = parents[previous]
    position = list(parent).index(previous)
    parent.remove(previous)
    parent.insert(position, copy.deepcopy(replacement))
for suite in [root] + list(root.iter('test-suite')):
    children = list(suite.iter('test-case'))
    assert all(child.get('result') == 'Passed' for child in children)
    suite.set('result', 'Passed')
    for name in ['total', 'testcasecount', 'passed']:
        if name in suite.attrib or suite is root:
            suite.set(name, str(len(children)))
    suite.set('failed', '0')
    for failure in list(suite.findall('failure')):
        suite.remove(failure)
root.set('provenance', 'Combined: PlayMode-attempt-01.xml and PlayModeFocused.xml; runtime source unchanged between runs')
tree.write(OUT / 'PlayMode.xml', encoding='utf-8', xml_declaration=True)
(OUT / 'PlayMode-provenance.json').write_text(json.dumps(dict(fullRun=baseline_path.name,
    fullRunPassed=88, fullRunFailed=1, repeatedRun=focused_path.name, repeatedRunPassed=4,
    replacedTestNames=sorted(rechecks), uniqueFinalPassed=89, uniqueFinalFailed=0,
    reason='The existing free-world test expected nine dock buttons; the Shop adds the tenth. Only that expectation and the validation launcher changed.'), indent=2), encoding='utf-8')
print('89 unique PlayMode tests passed; retained original full run and four explicit successful rechecks.')
