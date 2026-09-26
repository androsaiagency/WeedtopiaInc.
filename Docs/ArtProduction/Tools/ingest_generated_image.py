"""Persist a visually reviewed image-tool output with exact request provenance."""
import argparse
import json
from pathlib import Path
import shutil
import time
from contextlib import contextmanager
import validate_pipeline as pipeline


@contextmanager
def production_lock(root=None):
    """Serialize shared ledger/manifest/progress writes; OS releases on process exit."""
    import msvcrt
    root=Path(root).resolve() if root is not None else Path(__file__).resolve().parents[3]
    path=root/pipeline.ART/'Validation/.production.lock'
    path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('a+b') as stream:
        if stream.seek(0,2)==0:
            stream.write(b'0');stream.flush()
        deadline=time.monotonic()+60
        while True:
            stream.seek(0)
            try:
                msvcrt.locking(stream.fileno(),msvcrt.LK_NBLCK,1)
                break
            except OSError:
                if time.monotonic()>=deadline:
                    raise TimeoutError('Production ledger busy; retry after other ingestion completes.')
                time.sleep(.1)
        try:
            yield
        finally:
            stream.seek(0);msvcrt.locking(stream.fileno(),msvcrt.LK_UNLCK,1)


def verify_request_references(root, request):
    """Reject any changed/missing generation input before copying or accepting output."""
    paths=request.get('referenced_image_paths')
    expected=request.get('reference_hashes')
    if not isinstance(paths,list) or not paths or len(paths)>5 or not isinstance(expected,dict):
        raise ValueError('Request must contain one to five reference paths and their hashes.')
    actual={}
    for value in paths:
        path=Path(value).resolve()
        try:
            key=path.relative_to(root).as_posix()
        except ValueError as exc:
            raise ValueError('Generation reference escapes the project.') from exc
        pipeline.safe_path(root,key)
        if key in actual:
            raise ValueError('Duplicate generation reference path.')
        if not path.is_file():
            raise ValueError('Missing generation reference: '+key)
        actual[key]=pipeline.sha256(path)
    if actual!=expected:
        raise ValueError('Generation reference hashes changed after request preparation.')


def main(argv=None, root=None):
    parser=argparse.ArgumentParser(description=__doc__)
    identity=parser.add_mutually_exclusive_group(required=True)
    identity.add_argument('--slot')
    identity.add_argument('--showcase', choices=pipeline.ALL_FAMILIES)
    parser.add_argument('--source',required=True,type=Path)
    parser.add_argument('--notes',required=True)
    parser.add_argument('--reject',action='store_true')
    parser.add_argument('--reopen',action='store_true',help='With --reject only: explicitly withdraw and archive an accepted image; never replaces it.')
    args=parser.parse_args(argv)
    if args.reopen and not args.reject:
        parser.error('--reopen is only permitted with --reject')
    root=Path(root).resolve() if root is not None else Path(__file__).resolve().parents[3]
    slots={pipeline.slot_key(s):s for s in pipeline.load_manifest(root)['slots']}
    ledger=pipeline.read_json(root/pipeline.QA,{'entries':{}})
    if args.slot:
        slot=slots[args.slot]
        accepted=slot['status']=='accepted' or ledger['entries'].get(args.slot,{}).get('status')=='accepted'
        if accepted and not args.reopen:
            raise ValueError('Accepted slot is protected; no image was overwritten.')
        family,tier=args.slot.split('/')
    else:
        family,tier=args.showcase,'showcase'
        accepted=ledger['entries'].get('showcase:'+family,{}).get('status')=='accepted'
        if accepted and not args.reopen:
            raise ValueError('Accepted showcase is protected; no image was overwritten.')
        folder=f'{pipeline.ART}/Buildings/{family}'
        slot={'image_prompt_file':folder+'/family_showcase_prompt_en.txt','reference_image':folder+'/family_showcase.png'}
    request_path=root/pipeline.ART/'Validation/ImageRequests'/family/(tier+'.json')
    request=pipeline.read_json(request_path)
    if args.reopen and not accepted:
        raise ValueError('--reopen requires a previously accepted image.')
    expected_key=args.slot or 'showcase:'+args.showcase
    if request.get('key')!=expected_key or request.get('target')!=slot['reference_image'] or request.get('prompt_file')!=slot['image_prompt_file']:
        raise ValueError('Prepared request does not match this slot identity and paths.')
    if not args.reopen and pipeline.sha256(root/slot['image_prompt_file'])!=request['prompt_sha256']:
        raise ValueError('Prompt changed after preparing generation request.')
    if not args.reject:
        verify_request_references(root,request)
    pipeline.decode_png(args.source,showcase=bool(args.showcase))
    target=root/slot['reference_image']
    if args.reopen and not target.is_file():
        raise ValueError('Accepted image must exist before it can be reopened.')
    if target.exists():
        if pipeline.sha256(target)!=pipeline.sha256(args.source):
            raise ValueError('Existing output is protected; review/archive it explicitly before replacing.')
        # Resume a verified identical copy after validation was interrupted.
    else:
        shutil.copy2(args.source,target)
    qa_args=argparse.Namespace(command='reject' if args.reject else 'accept',slot=args.slot,showcase=args.showcase,
       notes=args.notes,source_image=str(args.source.resolve()),prompt_file=str(root/slot['image_prompt_file']),
       reference=request['referenced_image_paths'])
    try:
        progress=pipeline.record_qa(root,qa_args)
    except Exception:
        # Leave the actual generated image reviewable; never claim acceptance.
        pipeline.sync_progress(root)
        raise
    request.update(status='rejected' if args.reject else 'accepted',source_image=str(args.source.resolve()),
                   source_image_sha256=pipeline.sha256(args.source),review_notes=args.notes)
    if args.reject:
        archive=root/pipeline.ART/'Validation/Rejected'/family/tier
        archive.mkdir(parents=True,exist_ok=True)
        n=1
        while (archive/f'attempt_{n:03d}.png').exists(): n+=1
        final=archive/f'attempt_{n:03d}.png'
        target.rename(final)
        shutil.copy2(root/slot['image_prompt_file'],archive/f'attempt_{n:03d}_prompt_en.txt')
        pipeline.atomic_text(archive/f'attempt_{n:03d}_prepared_prompt_en.txt',request['prompt'])
        request['reopened_acceptance']=bool(args.reopen)
        request['archived_image']=str(final.relative_to(root)).replace('\\','/')
        pipeline.atomic_json(archive/f'attempt_{n:03d}_request.json',request)
        progress=pipeline.sync_progress(root)
    pipeline.atomic_json(request_path,request)
    print(json.dumps({k:progress[k] for k in ('generated_slots','validated_slots','failed_slots','next_slot','validated_showcases')}))


def run(argv=None, root=None):
    with production_lock(root):
        main(argv, root)


if __name__=='__main__':
    run()
