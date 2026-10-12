"""Collect native VIP validation receipts; never replace a missing gate with an exit code."""
import json
from pathlib import Path
from datetime import datetime, timezone

root = Path(__file__).resolve().parents[2]
out = root / 'Docs/VipStories'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def latest(folder, pattern):
    files = sorted((root / folder).glob(pattern))
    if not files:
        raise RuntimeError('Missing validation report: ' + pattern)
    result = read(files[-1])
    result['report'] = files[-1].relative_to(root).as_posix()
    return result

gates = {
    'domainAndPersistence': latest('Docs/Implementation/Validation', '*_EditMode_VipStoriesFinalDomainRegression.json'),
    'playModeInteractions': latest('Docs/Implementation/Validation', '*_PlayMode_VipStoriesPostPresentation.json'),
    'authoredAssetsAndIdempotence': latest('Docs/Implementation/Validation', '*_EditMode_VipStoriesFinalAuthoredAssets.json'),
    'renderedGameView': latest('Docs/VipStories/Validation', '*_Capture_FinalDelivery.json'),
    'preservation': read(out / 'Preservation/result.json')
}
content = read(root / 'Assets/Weedtopia/Game/Content/BurningHills.json')
view = read(out / 'gameview_receipt.json')
layout = read(out / 'ui_overflow.json')
baseline = read(out / 'Preservation/baseline.json')
result = {
    'utc': datetime.now(timezone.utc).isoformat(),
    'status': 'passed' if all(g['status'] == 'passed' for g in gates.values()) and layout['passed'] else 'failed',
    'people': len(content['vipStories']),
    'chapters': sum(len(v['chapters']) for v in content['vipStories']),
    'recurringVariants': sum(len(v['recurring']) for v in content['vipStories']),
    'collaborations': len(content['vipCollaborations']),
    'gates': gates,
    'renderMode': view['mode'], 'screenshots': len(view['screenshots']),
    'resolutions': layout['resolutions'], 'textOrSafeAreaIssues': len(layout['issues']),
    'protectedFiles': len(baseline['protected']),
    'userArtAcceptance': False, 'physicalDeviceTouchTest': False,
    'notes': 'Game View uses a dedicated diagnostic save. Existing player saves were not edited. Visual inspection is separate from user art acceptance.'
}
(out / 'validation_summary.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: result[k] for k in ['status','people','chapters','recurringVariants','collaborations','screenshots','textOrSafeAreaIssues','protectedFiles']}))
if result['status'] != 'passed':
    raise SystemExit(1)
