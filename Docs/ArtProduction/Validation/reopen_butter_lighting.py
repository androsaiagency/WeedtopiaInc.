"""Explicit, scoped withdrawal for the confirmed butter-factory lighting defect."""
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Docs/ArtProduction/Tools'))
import ingest_generated_image as ingest
import validate_pipeline as pipeline

if (ROOT / pipeline.ART / 'Validation/butter_neutral_correction_complete.json').exists():
    raise SystemExit('This one-time lighting correction is complete. Refusing to withdraw the new accepted assets.')

reason = ('Final independent QA confirmed illuminated amber entry strips and warm interior light in the butter_factory T01 anchor and T00/T08/T16/T24 samples. Neutral image-to-3D references require all fixtures switched off and no baked emission/light spill. This accepted image is explicitly withdrawn and archived for a complete corrected family reference chain; geometry and actual request provenance remain preserved in the archive.')
slots = [s for s in pipeline.load_manifest(ROOT)['slots'] if s['building_id'] == 'butter_factory']
order = sorted([s for s in slots if s['tier_id'] not in ('T00_RUIN', 'T01_L001')], key=lambda s: s['tier_id'], reverse=True)
order += [next(s for s in slots if s['tier_id'] == t) for t in ('T00_RUIN', 'T01_L001')]
jobs = [(['--showcase', 'butter_factory'], 'Docs/ArtProduction/Buildings/butter_factory/family_showcase.png', 'showcase:butter_factory')]
jobs += [(['--slot', pipeline.slot_key(s)], s['reference_image'], pipeline.slot_key(s)) for s in order]
for identity, source, key in jobs:
    ledger = pipeline.read_json(ROOT / pipeline.QA, {'entries': {}})
    if ledger['entries'].get(key, {}).get('status') == 'rejected' and not (ROOT / source).exists():
        print(json.dumps({'already_archived': key}), flush=True)
        continue
    request_path = ROOT / pipeline.ART / 'Validation/ImageRequests/butter_factory/T01_L001.json'
    if key == 'butter_factory/T01_L001' and not request_path.exists():
        # Early T01 used direct ledger acceptance, before the prepare helper existed.
        # Reconstruct only verified historical data, explicitly marked as such.
        with ingest.production_lock(ROOT):
            entry = pipeline.read_json(ROOT / pipeline.QA)['entries'][key]
            slot = next(s for s in slots if pipeline.slot_key(s) == key)
            prompt_path = ROOT / slot['image_prompt_file']
            if pipeline.sha256(prompt_path) != entry['prompt_sha256'] or pipeline.sha256(ROOT / source) != entry['image_sha256']:
                raise ValueError('Historical anchor bytes no longer match the original QA ledger.')
            refs = entry['references']
            for ref in refs:
                if pipeline.sha256(pipeline.safe_path(ROOT, ref['path'])) != ref['sha256']:
                    raise ValueError('Historical anchor reference no longer matches original QA.')
            request = {'key': key, 'target': source, 'prompt_file': slot['image_prompt_file'],
                'prompt': prompt_path.read_text(encoding='utf-8-sig'), 'prompt_sha256': entry['prompt_sha256'],
                'referenced_image_paths': [str(ROOT / r['path']) for r in refs],
                'reference_hashes': {r['path']: r['sha256'] for r in refs},
                'source_image': entry['source_image'], 'source_image_sha256': entry['source_image_sha256'],
                'status': 'historical_direct_acceptance_reconstructed_for_explicit_withdrawal',
                'reconstructed_from_qa_at_utc': pipeline.now(), 'original_reviewed_at_utc': entry['reviewed_at_utc']}
            pipeline.atomic_json(request_path, request)
    ingest.run(identity + ['--source', str(ROOT / source), '--reject', '--reopen', '--notes', reason], ROOT)
    print(json.dumps({'archived': key}), flush=True)
