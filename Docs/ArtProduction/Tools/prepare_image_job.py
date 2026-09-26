"""Print one resumable built-in image-tool request; never calls an image service."""
import argparse
import json
from pathlib import Path
import validate_pipeline as pipeline
from ingest_generated_image import production_lock


def main(argv=None, root=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--slot')
    parser.add_argument('--predecessor-first', action='store_true', help='Reorder the same mandatory references: predecessor, T01, then all three styles. Never omits the predecessor.')
    args=parser.parse_args(argv)
    root=Path(root).resolve() if root is not None else Path(__file__).resolve().parents[3]
    manifest=pipeline.load_manifest(root)
    progress=pipeline.read_json(root/pipeline.PROGRESS,{})
    key=args.slot or progress.get('next_slot')
    if not key:
        raise ValueError('No pending slot; consult pending_showcases or full validation.')
    slots={pipeline.slot_key(s):s for s in manifest['slots']}
    slot=slots[key]
    if slot['status']=='accepted':
        raise ValueError('Refusing to prepare a currently accepted slot for regeneration.')
    refs=[root/pipeline.ART/'References/ChosenStyle'/name for name in
          ('Reclaimed_Green_Haven.png','Herbal_Atom_Age.png','Burning_Hills_Afterglow.png')]
    family,tier=key.split('/')
    if family!='town_hall' and tier!='T01_L001':
        dependencies=[f'{family}/T01_L001']
        index=pipeline.TIERS.index(tier)
        if index>=2:
            dependencies.append(f'{family}/{pipeline.TIERS[index-1]}')
        ledger=pipeline.read_json(root/pipeline.QA,{'entries':{}})
        for dependency in dict.fromkeys(dependencies):
            source=slots[dependency]
            image=root/source['reference_image']
            if not image.is_file() or not pipeline.qa_valid(root,ledger['entries'].get(dependency),image,root/source['image_prompt_file']):
                raise ValueError('Required anchor/predecessor is not currently accepted: '+dependency)
            refs.append(image)
    if args.predecessor_first and len(refs)>3:
        geometry=refs[3:]
        refs=list(reversed(geometry))+refs[:3]
    if not all(p.is_file() for p in refs):
        raise ValueError('A required reference is missing.')
    prompt_path=root/slot['image_prompt_file']
    job={'key':key,'target':slot['reference_image'],'prompt_file':slot['image_prompt_file'],
         'predecessor_first':args.predecessor_first,
         'prompt':prompt_path.read_text(encoding='utf-8-sig'),'referenced_image_paths':[str(p) for p in refs],
         'prompt_sha256':pipeline.sha256(prompt_path),
         'reference_hashes':{str(p.relative_to(root)).replace('\\','/'):pipeline.sha256(p) for p in refs}}
    request=root/pipeline.ART/'Validation/ImageRequests'/family/(tier+'.json')
    pipeline.atomic_json(request,{**job,'prepared_at_utc':pipeline.now(),'status':'prepared_not_executed'})
    print(json.dumps(job,ensure_ascii=True))


def run(argv=None, root=None):
    # A finishing rejection writes its archived request last. Do not allow a new
    # request to be overwritten by that still-running ingestion process.
    with production_lock(root):
        main(argv, root)


if __name__=='__main__':
    run()
