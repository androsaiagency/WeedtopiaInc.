"""Explicit user-approved local alpha extraction. RGB pixels and accepted sources never change."""
from pathlib import Path
import argparse, hashlib, json, os, shutil, time
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
os.environ['REMBG_HOME'] = str(ROOT / 'Tools/BuildingCutouts/models')
os.environ['OMP_NUM_THREADS'] = '4'
os.environ['NUMBA_CACHE_DIR'] = str(ROOT / 'Tools/BuildingCutouts/cache')
from PIL import Image, ImageDraw
import numpy as np

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--samples', action='store_true'); args = parser.parse_args()
    slots = json.loads((ROOT/'Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json').read_text(encoding='utf-8-sig'))['slots']
    out = ROOT/'Assets/Weedtopia/Production/Resources/WeedtopiaBuildings'
    evidence = ROOT/'Docs/LayoutRevision/Cutouts'; evidence.mkdir(parents=True,exist_ok=True)
    receipt_path=evidence/'manifest.json'
    previous=json.loads(receipt_path.read_text()) if receipt_path.exists() else {'slots':[]}
    receipts={s['key']:s for s in previous['slots']}
    approved={
        'town_hall/T_FIXED':'Assets/Weedtopia/Art/Buildings/Sprites/town_hall/WT_town_hall_T_FIXED_2D_v1.png',
        **{'coffee_shop/'+tier:'Assets/Weedtopia/Game/VisualReview/Sprites/coffee_shop_'+tier+'.png' for tier in ['T01_L001','T12_L600','T24_L5000']}}
    session=None
    for index,slot in enumerate(slots):
        key=slot['building_id']+'/'+slot['tier_id']; source=ROOT/slot['reference_image']; target=out/(key+'.png')
        if args.samples and key not in approved and not slot['tier_id'].startswith(('T00','T01')):continue
        if key in receipts and target.exists() and receipts[key]['sourceSha256']==sha(source) and receipts[key]['spriteSha256']==sha(target):continue
        started=time.monotonic(); target.parent.mkdir(parents=True,exist_ok=True)
        src=Image.open(source).convert('RGB')
        if key in approved:
            shutil.copy2(ROOT/approved[key],target); method='byte_identical_copy_of_approved_prototype'
            rgba=Image.open(target).convert('RGBA'); rgb_preserved=None
        else:
            if session is None:
                from rembg import new_session
                session=new_session('isnet-general-use',providers=['CPUExecutionProvider'])
            # Only the alpha mask comes from segmentation; no recoloring, resynthesis, resizing or retouching.
            mask=np.array(session.predict(src)[0],dtype=np.uint8)
            mask[mask<12]=0; mask[mask>243]=255
            rgba=src.convert('RGBA');rgba.putalpha(Image.fromarray(mask));rgba.save(target)
            rgb_preserved=bool(np.array_equal(np.array(rgba)[:,:,:3],np.array(src)))
            assert rgb_preserved
            method='local_isnet_general_use_alpha_only'
        alpha=np.asarray(rgba)[:,:,3]; bbox=rgba.getchannel('A').getbbox()
        if bbox is None:raise RuntimeError('Empty cutout '+key)
        edges=int(np.count_nonzero(alpha[0,:])+np.count_nonzero(alpha[-1,:])+np.count_nonzero(alpha[:,0])+np.count_nonzero(alpha[:,-1]))
        receipts[key]={'key':key,'buildingId':slot['building_id'],'tierId':slot['tier_id'],'source':slot['reference_image'],
            'sourceSha256':sha(source),'sprite':target.relative_to(ROOT).as_posix(),'spriteSha256':sha(target),
            'method':method,'rgbPixelsUnchanged':rgb_preserved,'width':rgba.width,'height':rgba.height,
            'alphaBounds':list(bbox),'edgeAlphaPixels':edges,'opaqueFraction':float(np.mean(alpha==255)),
            'technicalPassed':edges==0 and (bbox[2]-bbox[0])>50,'individualVisualReviewed':False,
            'worldHeight':height(slot),'utc':datetime.now(timezone.utc).isoformat()}
        receipt_path.write_text(json.dumps({'method':'Explicit user choice: local pixel-preserving cutout with individual review','model':'isnet-general-use','source':'https://github.com/danielgatis/rembg','sourceImagesModified':False,'slots':list(receipts.values())},indent=2),encoding='utf-8')
        print(f'{index+1}/251 {key} {time.monotonic()-started:.1f}s edge={edges}',flush=True)
    # QA contact sheets only. Production cutouts retain their original pixels and dimensions.
    for family in sorted({s['building_id'] for s in slots}):
        family_entries=[receipts[s['building_id']+'/'+s['tier_id']] for s in slots if s['building_id']==family and s['building_id']+'/'+s['tier_id'] in receipts]
        for start in range(0,len(family_entries),10):
            page=family_entries[start:start+10]; sheet=Image.new('RGB',(1500,340*((len(page)+4)//5)),(38,61,57));draw=ImageDraw.Draw(sheet)
            for i,entry in enumerate(page):
                img=Image.open(ROOT/entry['sprite']).convert('RGBA');img.thumbnail((288,294),Image.Resampling.LANCZOS)
                x=i%5*300+(300-img.width)//2;y=i//5*340+8+(294-img.height)//2
                sheet.paste(img,(x,y),img);draw.text((i%5*300+8,i//5*340+310),entry['tierId'],fill=(244,227,184))
            sheet.save(evidence/(family+f'_{start//10+1:02d}.png'))
    print('Prepared',len(receipts),'individually mapped cutouts; visual review remains explicit.',flush=True)

def height(slot):
    tier=int(slot['tier_id'][1:3]) if slot['tier_id']!='T_FIXED' else 24
    if slot['building_id']=='town_hall':return 22.0
    if slot['building_id']=='coffee_shop':return 5.5 if tier==0 else 8+(tier-1)*6/11 if tier<=12 else 14+(tier-12)*8/12
    if slot['building_id']=='cultivation_plot':return max(3.5,min(24,slot['height_m']+3))
    if slot['building_id']=='park':return max(6,min(20,slot['height_m']+5))
    return max(6,min(26,slot['height_m']*.72+5))

if __name__=='__main__':main()
