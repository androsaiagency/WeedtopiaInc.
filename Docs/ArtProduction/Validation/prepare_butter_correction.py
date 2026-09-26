"""Prepare the explicitly authorized butter-factory neutral-lighting correction."""
import argparse
import contextlib
import io
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Docs/ArtProduction/Tools'))
import prepare_image_job
import validate_pipeline as pipeline
from ingest_generated_image import production_lock

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tier', required=True, choices=pipeline.TIERS)
parser.add_argument('--upgrade-detail', default='')
args = parser.parse_args()
key = 'butter_factory/' + args.tier

with production_lock(ROOT):
    slots = {pipeline.slot_key(s): s for s in pipeline.load_manifest(ROOT)['slots']}
    slot = slots[key]
    if slot['status'] == 'accepted':
        raise ValueError('Never rewrite an accepted image prompt.')
    prompt_path = ROOT / slot['image_prompt_file']
    old = prompt_path.read_text(encoding='utf-8-sig')
    body = old[old.index('Use case:'):]
    neutral = ('MANDATORY NEUTRAL ASSET: all entry posts have smooth opaque sage or cream panels, NO LIGHT FIXTURES, no yellow/amber strips, no luminous slits. Interior is shaded neutral grey with ordinary reflective metal machinery, never warm yellow illumination. All green glass is dark non-emissive bottle-green; no luminous fill, bloom, halo, colored spill, artificial light sources or dramatic lighting anywhere. Brass may have ordinary neutral studio reflections but must not emit light. Render a matte off-white background and soft neutral contact shadow. Entire building inside the square frame with clear margin. ')
    archive = None
    if args.tier == 'T01_L001':
        old_request = pipeline.read_json(ROOT / pipeline.ART / 'Validation/ImageRequests/butter_factory/T01_L001.json')
        archive_key = old_request.get('archived_image')
        if not archive_key:
            raise ValueError('Original T01 must be explicitly archived first.')
        archive = pipeline.safe_path(ROOT, archive_key)
        prefix = ('Use image 4 as the exact building GEOMETRY edit target. Preserve its single cream-and-timber gable mixing hall, patched sage corrugated roof, low front awning, one front leaf emblem, short chimney, two attached cream round tanks on the RIGHT and thick pipe bridge. Change only lighting-related appearance: remove the luminous entry strips and replace them with smooth opaque wall panels; make the interior neutrally shaded. Do not add tiers, extra tanks, solar arrays, landscape or props. Images 1-3 are palette/material references only, NOT lighting or geometry targets. ')
        body = body.replace('For T01 or T_FIXED only the three style inputs are supplied.', 'For this T01 correction, image 4 is the archived original T01 geometry reference; it is not an accepted neutral-lighting example.')
    else:
        prefix = ('REFERENCE PRIORITY: image 4 is the corrected T01 family anchor; image 5 when present is the corrected immediately previous accepted tier and is the PRIMARY cumulative geometry reference. Preserve its existing functional volumes and add the current large architectural upgrade. For T00 derive repairable damage from image 4 only. Images 1-3 are palette/material inspiration only, not building layouts or lighting examples. ')
    prompt = neutral + '\n\n' + prefix + args.upgrade_detail + '\n\n' + body
    pipeline.atomic_text(prompt_path, prompt)
    capture = io.StringIO()
    with contextlib.redirect_stdout(capture):
        prepare_image_job.main(['--slot', key], ROOT)
    job = json.loads(capture.getvalue())
    if archive is not None:
        job['referenced_image_paths'].append(str(archive))
        job['reference_hashes'][archive.relative_to(ROOT).as_posix()] = pipeline.sha256(archive)
        job['supporting_original_anchor'] = archive.relative_to(ROOT).as_posix()
        request = ROOT / pipeline.ART / 'Validation/ImageRequests/butter_factory/T01_L001.json'
        pipeline.atomic_json(request, {**job, 'prepared_at_utc': pipeline.now(), 'status': 'prepared_not_executed'})
    print(json.dumps(job, ensure_ascii=True))
