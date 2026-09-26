"""Prepare one exact accepted-family showcase request, without service calls."""
import argparse
import json
from pathlib import Path
import validate_pipeline as pipeline
from ingest_generated_image import production_lock


def main(argv=None, root=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--family',required=True,choices=pipeline.ALL_FAMILIES)
    args=parser.parse_args(argv)
    root=Path(root).resolve() if root is not None else Path(__file__).resolve().parents[3]
    slots={pipeline.slot_key(s):s for s in pipeline.load_manifest(root)['slots']}
    ledger=pipeline.read_json(root/pipeline.QA,{'entries':{}})
    key='showcase:'+args.family
    if ledger['entries'].get(key,{}).get('status')=='accepted':
        raise ValueError('Accepted showcase is protected.')
    family_slots=[s for s in slots.values() if s['building_id']==args.family]
    for slot in family_slots:
        if slot['status']!='accepted' or not pipeline.qa_valid(root,ledger['entries'].get(pipeline.slot_key(slot)),root/slot['reference_image'],root/slot['image_prompt_file']):
            raise ValueError('Finish and accept the family first: '+pipeline.slot_key(slot))
    refs=[root/pipeline.ART/'References/ChosenStyle'/name for name in
          ('Reclaimed_Green_Haven.png','Burning_Hills_Afterglow.png')]
    tiers=['T_FIXED'] if args.family=='town_hall' else ['T01_L001','T12_L600','T24_L5000']
    refs.extend(root/slots[args.family+'/'+tier]['reference_image'] for tier in tiers)
    folder=f'{pipeline.ART}/Buildings/{args.family}'
    prompt_file=folder+'/family_showcase_prompt_en.txt'
    job={'key':key,'target':folder+'/family_showcase.png','prompt_file':prompt_file,
         'prompt':(root/prompt_file).read_text(encoding='utf-8-sig'),
         'referenced_image_paths':[str(p) for p in refs],
         'prompt_sha256':pipeline.sha256(root/prompt_file),
         'reference_hashes':{p.relative_to(root).as_posix():pipeline.sha256(p) for p in refs}}
    pipeline.atomic_json(root/pipeline.ART/'Validation/ImageRequests'/args.family/'showcase.json',
                         {**job,'prepared_at_utc':pipeline.now(),'status':'prepared_not_executed'})
    print(json.dumps(job,ensure_ascii=True))


def run(argv=None, root=None):
    with production_lock(root):
        main(argv, root)


if __name__=='__main__':
    run()
