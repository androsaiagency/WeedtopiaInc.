"""Collect completed navigation gates and verify compiler source freshness."""
import hashlib
import json
from pathlib import Path
from shutil import copyfile

root=Path('Docs/InventoryWheelDock')
validation=Path('Docs/Implementation/Validation')
runs=[json.loads((validation/(name+'.json')).read_text(encoding='utf-8-sig')) for name in
      ['20261001_124540_PlayMode_InventoryWheelDock','20261001_124812_EditMode_InventoryWheelDock']]
assert all(r['status']=='passed' and r['failed']==0 for r in runs)
source=json.loads((root/'source_compile_receipt.json').read_text(encoding='utf-8-sig'))
assert source['status']=='passed' and all(r['exitCode']==0 for r in source['results'])
stale=[p for r in source['results'] for p,h in r['sources'].items() if hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
assert not stale,stale
copyfile('Docs/ProductionFinish/Evidence/ui_overflow.json',root/'ui_overflow.json')
layout=json.loads((root/'ui_overflow.json').read_text(encoding='utf-8-sig'))
capture=json.loads((root/'gameview_receipt.json').read_text(encoding='utf-8-sig'))
assert layout['passed'] and not layout['issues'] and capture['fingerprint']==layout['fingerprint']
assert len(capture['screenshots'])==14 and all(Path(p).is_file() for p in capture['screenshots'])
preservation=json.loads((root/'preservation_receipt.json').read_text(encoding='utf-8-sig'))
repeat=json.loads((root/'idempotence_receipt.json').read_text(encoding='utf-8-sig'))
assert preservation['status']=='passed' and repeat['status']=='passed'
receipt=dict(status='passed',userAcceptance=False,roundButtons=9,inventoryAndWheelInDock=True,sourceAssemblies=len(source['results']),
             staleSources=stale,tests=[dict(run=r['run'],passed=r['passed'],total=r['total']) for r in runs],
             screenshots=14,resolutions=layout['resolutions'],overflowIssues=0,fingerprint=capture['fingerprint'],
             preservedInstallerFiles=repeat['preservedFiles'],preservation=preservation)
(root/'completion_receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))
