"""Keep existing population fixtures and the building inspector on the domain formula."""
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BEFORE = ROOT / 'Docs/GoldenFlower/Preservation/BeforeCapacityUnification'
TEST_NAMES = ['SevenHills', 'CassaVadela', 'LittleMontana', 'MarkWoodCity', 'GreenLeafVillage', 'PurpleGarden']
EVIDENCE_NAMES = ['CassaVadela', 'LittleMontana', 'MarkWoodCity', 'GreenLeafVillage', 'PurpleGarden']
HELPERS = [f'Assets/Weedtopia/Scripts/Tests/EditMode/{name}CampaignTests.cs' for name in TEST_NAMES]
HELPERS += [f'Assets/Weedtopia/Scripts/Editor/GameSetup/{name}Evidence.cs' for name in EVIDENCE_NAMES]
INSPECTOR = 'Assets/Weedtopia/Scripts/Runtime/Unity/BuildingInspectionMetrics.cs'

def projected(text, inspector=False):
    pattern = r'\.baseResidents\s*\*\s*\(1\s*\+\s*VisualTiers\.Index\(l\)\)' if inspector else r'\.baseResidents\s*\*\s*\(1\s*\+\s*tier\)'
    replacement = '.ResidentCapacity(l)' if inspector else '.ResidentCapacity(VisualTiers.Starts[tier])'
    return re.sub(pattern, replacement, text)

if __name__ == '__main__':
    changed = []
    for name in [*HELPERS, INSPECTOR]:
        path = ROOT / name
        text = path.read_text(encoding='utf-8-sig')
        updated = projected(text, name == INSPECTOR)
        if updated == text:
            continue
        backup = BEFORE / name
        if not backup.exists():
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, backup)
        path.write_text(updated, encoding='utf-8')
        changed.append(name)
    print(json.dumps({'status': 'passed', 'formula': 'BuildingDefinition.ResidentCapacity', 'changed': changed}, indent=2))
