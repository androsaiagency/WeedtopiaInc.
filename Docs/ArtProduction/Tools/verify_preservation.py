"""Read-only integrity check of original project files captured before this task."""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path


def main():
    root = Path(__file__).resolve().parents[3]
    evidence = root / 'Docs/ArtProduction/Validation'
    baseline = json.loads((evidence / 'original_project_hashes.json').read_text(encoding='utf-8-sig'))
    changed, missing = [], []
    for item in baseline:
        path = root / item['path']
        if not path.is_file():
            missing.append(item['path'])
        elif hashlib.sha256(path.read_bytes()).hexdigest().lower() != item['sha256'].lower():
            changed.append(item['path'])
    result = {'checked_at_utc': datetime.now(timezone.utc).isoformat(),
              'original_files_checked': len(baseline), 'changed': changed,
              'missing': missing, 'passed': not changed and not missing,
              'scope': 'Original Assets, Packages and ProjectSettings files; generated files are additive.'}
    (evidence / 'preservation_report.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
