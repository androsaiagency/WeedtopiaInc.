"""Merge actual image-inspection notes; this script itself performs no visual review."""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/SevenHills'


def main():
    capture = json.loads((DOC / 'gameview_receipt.json').read_text(encoding='utf-8-sig'))
    shots = capture['screenshots']
    reviewed = {}
    observations = []
    reviewers = []
    errors = []
    for path in sorted((DOC / 'VisualReview').glob('*.json')):
        report = json.loads(path.read_text(encoding='utf-8-sig'))
        reviewers.append({'report': path.relative_to(ROOT).as_posix(),
                          'reviewer': report.get('reviewer'), 'status': report.get('status')})
        if report.get('status') != 'passed' or report.get('actualImageInspection') is not True:
            errors.append('Image inspection incomplete or failed: ' + str(path))
        if report.get('captureFingerprint') != capture['fingerprint']:
            errors.append('Different capture fingerprint: ' + str(path))
        for name, expected in report.get('reviewedScreenshots', {}).items():
            if name not in shots:
                errors.append('Unknown screenshot: ' + name)
                continue
            if name in reviewed:
                errors.append('Duplicate screenshot review: ' + name)
                continue
            actual = hashlib.sha256(Path(name).read_bytes()).hexdigest()
            # PowerShell renders hexadecimal SHA-256 values in upper case;
            # compare their canonical representation without changing the bytes proof.
            canonical_expected = expected.lower() if isinstance(expected, str) else expected
            if actual != canonical_expected:
                errors.append('Screenshot changed after inspection: ' + name)
            reviewed[name] = canonical_expected
        observations.extend(report.get('observations', []))
    missing = sorted(set(shots) - set(reviewed))
    if missing:
        errors.append('Some captured images have not been inspected')
    result = {'utc': datetime.now(timezone.utc).isoformat(),
              'status': 'passed' if not errors and shots else 'failed',
              'fingerprint': capture['fingerprint'], 'reviewers': reviewers,
              'reviewedScreenshots': reviewed, 'unreviewedScreenshots': missing,
              'observations': observations, 'errors': errors,
              'userAcceptance': False, 'physicalTouchVerified': False,
              'deviceAudioVerified': False,
              'scope': 'Assistant inspection of the actual rendered capture pixels; separate from direct user approval and device testing.'}
    (DOC / 'visual_review.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'status': result['status'], 'reviewed': len(reviewed),
                      'captured': len(shots), 'errors': errors}, ensure_ascii=False))
    return 0 if result['status'] == 'passed' else 1


if __name__ == '__main__':
    raise SystemExit(main())
