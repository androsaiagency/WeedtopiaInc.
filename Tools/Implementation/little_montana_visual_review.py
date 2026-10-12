"""Verify actual image-review records against the latest rendered Game Views.

This tool validates coverage and hashes; it does not inspect pixels or approve art.
Review records must be written only after their image files have been viewed.
"""
import hashlib
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/LittleMontana'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    receipt = json.loads((DOC / 'gameview_receipt.json').read_text(encoding='utf-8-sig'))
    expected = {pathlib.Path(p).resolve(): p for p in receipt['screenshots']}
    if not expected or len(expected) != len(receipt['screenshots']):
        raise ValueError('Missing or duplicate capture paths')
    reviewed, reports = {}, []
    for file in sorted((DOC / 'VisualReview').glob('*.json')):
        report = json.loads(file.read_text(encoding='utf-8-sig'))
        if report.get('fingerprint') != receipt['fingerprint']:
            raise ValueError('Review fingerprint differs from current capture: ' + str(file))
        if report.get('status') != 'passed':
            raise ValueError('Visual review still has unresolved findings: ' + str(file))
        reports.append(file.relative_to(ROOT).as_posix())
        for entry in report['checkedImages']:
            path = pathlib.Path(entry['path']).resolve()
            if path not in expected or path in reviewed:
                raise ValueError('Unexpected or duplicate reviewed image: ' + str(path))
            if entry.get('status') != 'passed' or entry.get('sha256') != sha(path):
                raise ValueError('Failed review or modified image: ' + str(path))
            reviewed[path] = entry
    if reviewed.keys() != expected.keys():
        raise ValueError('Not all current images have actual pixel reviews: ' + ', '.join(str(p) for p in expected.keys() - reviewed.keys()))
    report = dict(status='passed', fingerprint=receipt['fingerprint'], screenshots=len(expected),
                  actualPixelReview=True, userAcceptance=False, reviewRecords=reports,
                  checkedImages=[reviewed[path] for path in expected],
                  scope='Actual PNG pixels reviewed for city composition, resident visibility, saved memories, campaign UI, four resolutions and five languages. Growth fixtures do not prove human playtime.')
    (DOC / 'visual_review.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(status=report['status'], screenshots=report['screenshots'], userAcceptance=False)))


if __name__ == '__main__':
    main()
