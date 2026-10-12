"""Package actual Unity Game View frames; no generated art is modified."""
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg

ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / 'Docs/ConveyorRevision'
VALIDATION = ROOT / 'Docs/Implementation/Validation'
capture = json.loads((REPORT / 'gameview_receipt.json').read_text(encoding='utf-8-sig'))
assert capture['mode'] == 'actual_playmode_gameview'
assert len(capture['screenshots']) == 57
for i in range(48):
    frame = REPORT / f'Screenshots/motion_{i:03}.png'
    assert str(frame) in capture['screenshots'], frame
    assert frame.is_file()
output = REPORT / 'ConveyorMotion.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                '-framerate', '24', '-i', str(REPORT / 'Screenshots/motion_%03d.png'),
                '-frames:v', '48', '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p',
                '-movflags', '+faststart', str(output)], check=True)
preserved = json.loads((REPORT / 'preservation.json').read_text())
overflow = json.loads((ROOT / 'Docs/ProductionFinish/Evidence/ui_overflow.json').read_text(encoding='utf-8-sig'))
assert preserved['passed'] and overflow['passed']
assert capture['fingerprint'] == overflow['fingerprint']
tests = []
for suffix in ['PlayMode_ConveyorRevision', 'EditMode_ConveyorFinalRegression', 'EditMode_ConveyorFinalScene']:
    path = sorted(VALIDATION.glob('*_' + suffix + '.json'))[-1]
    result = json.loads(path.read_text(encoding='utf-8-sig'))
    assert result['status'] == 'passed' and result['failed'] == 0
    tests.append({'report': str(path.relative_to(ROOT)), 'passed': result['passed'], 'total': result['total']})
result = {'passed': True, 'fingerprint': capture['fingerprint'], 'tests': tests,
          'actualGameViewCaptures': len(capture['screenshots']), 'resolutions': overflow['resolutions'],
          'uiOverflowIssues': overflow['issues'], 'scenePreservationPassed': True,
          'motionVideo': str(output.relative_to(ROOT)), 'motionSeconds': 2, 'motionFps': 24,
          'targetDevicePerformanceMeasured': False, 'userVisualAcceptance': False}
(REPORT / 'validation.json').write_text(json.dumps(result, indent=2))
(REPORT / 'ui_overflow.json').write_text(json.dumps(overflow, indent=2))
print(json.dumps(result, indent=2))
