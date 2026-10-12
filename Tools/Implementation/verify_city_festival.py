"""Collect final chapter evidence; preserve distinction between automation and human acceptance."""
import datetime
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/CityFestivalRevision'
CONTENT = 'Assets/Weedtopia/Game/Content/BurningHills.json'

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

before = read(OUT / 'Originals' / CONTENT)
after = read(ROOT / CONTENT)
preserved = {key: after[key] == value for key,value in before.items() if key != 'texts'}
new_texts = {t['key']: t['de'] for t in after['texts']}
preserved['existingTexts'] = all(new_texts[t['key']] == t['de'] for t in before['texts'])
assets = [dict(path=p['Path'], unchanged=sha(p['Path']) == p['Hash'])
          for p in read(OUT / 'preservation-before.json')]
receipts = []
for mode in ('EditMode', 'PlayMode'):
    candidates = sorted((ROOT / 'Docs/Implementation/Validation').glob('*_' + mode + '_CityFestival*.json'))
    full = [read(p) | dict(receipt=str(p.relative_to(ROOT))) for p in candidates]
    full = [r for r in full if r.get('total', 0) >= (105 if mode == 'EditMode' else 44)]
    if not full:
        raise RuntimeError('Missing full ' + mode + ' suite')
    receipts.append(full[-1])
compile_result = read(ROOT / 'Docs/Implementation/Validation/SourceCompile/result.json')
compile_current = all(sha(ROOT / path).lower() == digest.lower()
                      for assembly in compile_result['results'] for path,digest in assembly['sources'].items())
simulations = [read(p) for p in sorted(OUT.glob('simulation_*.json'))]
source_paths = set(str(p.relative_to(OUT / 'Originals')).replace('\\','/')
                   for p in (OUT / 'Originals').rglob('*') if p.is_file())
source_paths.update(str(p.relative_to(ROOT)).replace('\\','/') for p in (ROOT / 'Assets/Weedtopia/Scripts').rglob('CityProject*.cs'))
source_paths.add('Tools/Implementation/author_city_festival.py')
result = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
              checksPassed=all(preserved.values()) and all(p['unchanged'] for p in assets)
                  and all(r['status'] == 'passed' for r in receipts) and compile_current
                  and compile_result['status'] == 'passed' and len(simulations) == 4 and all(s['completed'] for s in simulations),
              tutorialAndBalancePreservation=preserved, authoredAssets=assets,
              unityTests=receipts, sourceCompileCurrent=compile_current,
              simulations=simulations, files={p:sha(ROOT / p) for p in sorted(source_paths)},
              humanPlaythroughAccepted=False, visualAcceptance=False,
              note='Simulated durations are diagnostic player timings, not observed human playtime. No user save was edited.')
(OUT / 'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:result[k] for k in ('checksPassed','sourceCompileCurrent','humanPlaythroughAccepted','visualAcceptance')}))
if not result['checksPassed']:
    raise SystemExit(1)
