"""Record and compare files outside the one authorized wrapper change."""
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[6]
REPORT = Path(__file__).parent / 'Restore2D'
WRAPPER = 'Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab'
MOVES = {
    'Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928':
        'Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928/Model',
    'Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928':
        'Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928/Review',
}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

if __name__ == '__main__':
    REPORT.mkdir(exist_ok=True)
    record = REPORT / 'preservation_before.json'
    if sys.argv[1] == 'before':
        if record.exists():
            raise RuntimeError('Do not overwrite the original baseline')
        files = set()
        for folder in ['Assets/Weedtopia/Art/Buildings/Prefabs', 'Assets/Weedtopia/Game', 'ProjectSettings', *MOVES]:
            files.update(p for p in (ROOT / folder).rglob('*') if p.is_file())
        for folder in MOVES:
            files.add(ROOT / (folder + '.meta'))
        sprite = ROOT / 'Assets/Weedtopia/Production/Resources/WeedtopiaBuildings/town_hall/T_FIXED.png'
        files.update([sprite, Path(str(sprite)+'.meta')])
        data = {p.relative_to(ROOT).as_posix(): digest(p) for p in sorted(files) if p.relative_to(ROOT).as_posix()!=WRAPPER}
        record.write_text(json.dumps(data, indent=2), encoding='utf-8')
        print(f'Baseline: {len(data)} files')
    else:
        data = json.loads(record.read_text(encoding='utf-8'))
        errors=[]
        moved=0
        for source, expected in data.items():
            target=source
            for old,new in MOVES.items():
                if source==old+'.meta' or source.startswith(old+'/'):
                    target=new+source[len(old):]
                    moved+=1
                    break
            p=ROOT/target
            if not p.is_file() or digest(p)!=expected:
                errors.append({'source':source,'current':target})
        def original_sprite_blocks(path):
            blocks = re.split(r'(?=^--- !u!)', path.read_text(encoding='utf-8'), flags=re.M)
            sprite = next(b for b in blocks if 'm_Name: ProductionSpriteVisual\n' in b)
            identity = re.search(r'^--- !u!1 &(\d+)', sprite).group(1)
            return [b for b in blocks if b==sprite or f'm_GameObject: {{fileID: {identity}}}' in b]
        original = original_sprite_blocks(Path(__file__).parent/'CityIntegration/Backup/WT_town_hall_T_FIXED.prefab')
        restored = original_sprite_blocks(ROOT/WRAPPER)
        sprite_exact = len(original)==5 and original==restored
        result={'checked_files':len(data),'archived_files_with_identical_bytes':moved,'unexpected_changes':errors,
                'original_sprite_and_four_component_blocks_identical':sprite_exact,'passed':not errors and sprite_exact}
        (REPORT/'preservation_after.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
        print(json.dumps(result,indent=2))
        sys.exit(not result['passed'])
