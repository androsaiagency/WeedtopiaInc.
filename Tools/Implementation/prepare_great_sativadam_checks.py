"""Prepare the strict preservation audit for the additive thirteenth city."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
source = (ROOT / "Tools/Implementation/skunkfurt_prefab_preservation.py").read_text(encoding="utf-8")
replacements = {
    "Skunkfurt": "GreatSativadam", "skunkfurt": "great_sativadam",
    "twelfth": "thirteenth",
    "len(scenes) == 12": "len(scenes) == 13",
    "eleven previous city scenes": "twelve previous city scenes",
    "len(prior_rows) == 11 and len(current_rows) == 12": "len(prior_rows) == 12 and len(current_rows) == 13",
    "previousCityScenes=11": "previousCityScenes=12",
}
for old, new in replacements.items():
    source = source.replace(old, new)
target = ROOT / "Tools/Implementation/great_sativadam_prefab_preservation.py"
target.write_text(source, encoding="utf-8")
print(target)
