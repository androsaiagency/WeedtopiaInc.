"""Merge explicitly recorded pixel reviews; refuse stale, duplicate or unreviewed screenshots."""
from __future__ import annotations
import datetime
import hashlib
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/CassaVadela'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    receipt = read(DOC / 'gameview_receipt.json')
    expected = {str(pathlib.Path(path).resolve()) for path in receipt['screenshots']}
    reviews = []
    errors = []
    sources = ['visual_review_root.json', 'visual_review_ui.json', 'visual_review_scene.json']
    for source in sources:
        review = read(DOC / source)
        if review['status'] != 'passed' or review['fingerprint'] != receipt['fingerprint']:
            errors.append('Incomplete or stale review: ' + source)
        for row in review.get('reviewedScreenshots', []):
            path = pathlib.Path(row['path']).resolve()
            if row.get('status') != 'passed' or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != row['sha256'].lower():
                errors.append('Failed or stale pixels: ' + str(path))
            reviews.append(dict(row, path=str(path), sourceReview=source))
    paths = [row['path'] for row in reviews]
    missing = sorted(expected - set(paths))
    unexpected = sorted(set(paths) - expected)
    duplicates = sorted(path for path in set(paths) if paths.count(path) > 1)
    if missing or unexpected or duplicates or len(expected) != 80:
        errors.append('Screenshot coverage does not match the 80 current Game Views')
    result = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), status='failed' if errors else 'passed',
        fingerprint=receipt['fingerprint'], mode='actual pixel inspection', filesReviewed=len(reviews),
        sourceReviews=sources, errors=errors, missing=missing, unexpected=unexpected, duplicates=duplicates,
        reviewedScreenshots=reviews, userAcceptance=False, physicalTouchVerified=False,
        diagnosticGrowthFixtures=True,
        limitations='Local screenshot and layout review does not constitute direct user art acceptance, a physical touch test, device audio verification, a player build or a human complete campaign playthrough.')
    (DOC / 'visual_review.json').write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(json.dumps({key: result[key] for key in ('status', 'filesReviewed', 'errors', 'missing', 'unexpected', 'duplicates')}, indent=2))
    return 0 if not errors else 1


if __name__ == '__main__':
    raise SystemExit(main())
