"""Audit Great Sativadam's imported assets and local Unity GUID references.

Reads asset data only. Writes the validation receipt under Docs/GreatSativadam;
never imports, repairs, deletes or changes Unity assets. Run after installation.
"""
from __future__ import annotations

import datetime
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'Assets/Weedtopia/Production/GreatSativadam'
SCENE = ROOT / 'Assets/Weedtopia/Game/Scenes/GreatSativadam.unity'
GUID = re.compile(r'\bguid:\s*([0-9a-f]{32})\b', re.I)
OWN_GUID = re.compile(r'^guid:\s*([0-9a-f]{32})\s*$', re.M | re.I)
TEXT_EXTENSIONS = {'.asset', '.prefab', '.unity', '.mat', '.controller', '.overridecontroller', '.anim', '.shader', '.cs'}
SCRIPTS = [ROOT / path for path in (
    'Assets/Weedtopia/Scripts/Editor/GameSetup/GreatSativadamAuthoring.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/GreatSativadamCampaignAuthoring.cs',
    'Assets/Weedtopia/Scripts/Editor/GameSetup/GreatSativadamEvidence.cs',
    'Assets/Weedtopia/Scripts/Runtime/Unity/GreatSativadamStamp.cs',
    'Assets/Weedtopia/Scripts/Tests/EditorAssets/GreatSativadamAssetTests.cs',
    'Assets/Weedtopia/Scripts/Tests/PlayMode/GreatSativadamPlayModeTests.cs',
    'Assets/Weedtopia/Scripts/Tests/EditMode/GreatSativadamCampaignTests.cs',
)]
ARCHITECTURE = (
    'TideGateCommons', 'SeedQuayHall', 'FerryLanding', 'CanalArchive',
    'HouseboatGardenCourt', 'RepairWharf', 'TideMusicPavilion', 'WaterDispatchHouse',
)
PEOPLE = ('gs_noor', 'gs_bram', 'gs_esra', 'gs_daan', 'gs_fenna', 'gs_joost')


def relative(path: pathlib.Path) -> str:
    return path.relative_to(ROOT).as_posix()


def main() -> int:
    production_paths = sorted(FOLDER.rglob('*')) if FOLDER.is_dir() else []
    assets = [path for path in production_paths if path.is_file() and path.suffix != '.meta']
    folders = [FOLDER] + [path for path in production_paths if path.is_dir()]
    required = [SCENE] + SCRIPTS + [FOLDER / path for path in (
        'BuildingVisuals.asset', 'ResidentVisuals.asset', 'ResidentStories.prefab',
        'Residents.prefab', 'DistrictMap.asset', 'Prefabs/TideCommons.prefab',
        'Art/resident_atlas.png', 'Art/architecture_atlas.png', 'Art/town_sign.png',
    )] + [FOLDER / ('Prefabs/' + name + '.prefab') for name in ARCHITECTURE + PEOPLE]
    missing_assets = sorted(relative(path) for path in required if not path.is_file())
    scoped_assets = sorted(set(assets + folders + [SCENE] + SCRIPTS))
    missing_meta = sorted(relative(path) + '.meta' for path in scoped_assets
                          if path.exists() and not pathlib.Path(str(path) + '.meta').is_file())
    scoped_metas = {pathlib.Path(str(path) + '.meta') for path in scoped_assets}
    scoped_metas.update(path for path in production_paths if path.is_file() and path.suffix == '.meta')
    orphan_meta = sorted(relative(meta) for meta in scoped_metas
                         if meta.is_file() and not pathlib.Path(str(meta)[:-5]).exists())
    invalid_meta = []
    registry = {}
    registry_read_errors = []
    for directory in ('Assets', 'Packages', 'Library/PackageCache'):
        for meta in (ROOT / directory).rglob('*.meta'):
            try:
                match = OWN_GUID.search(meta.read_text(encoding='utf-8-sig'))
            except (OSError, UnicodeError) as error:
                registry_read_errors.append({'path': relative(meta), 'error': str(error)})
                continue
            if match:
                registry.setdefault(match.group(1).lower(), []).append(meta)
            elif meta in scoped_metas:
                invalid_meta.append(relative(meta))
    duplicates = {guid: sorted(relative(path) for path in paths)
                  for guid, paths in registry.items()
                  if len(paths) > 1 and any(path in scoped_metas for path in paths)}
    reference_locations = {}
    text_read_errors = []
    for path in sorted(set(assets + [SCENE] + SCRIPTS)):
        if not path.is_file() or path.suffix.lower() not in TEXT_EXTENSIONS:
            continue
        try:
            contents = path.read_text(encoding='utf-8-sig')
        except (OSError, UnicodeError) as error:
            text_read_errors.append({'path': relative(path), 'error': str(error)})
            continue
        for guid in GUID.findall(contents):
            reference_locations.setdefault(guid.lower(), set()).add(relative(path))
    dangling = sorted(guid for guid in reference_locations
                      if guid not in registry and not guid.startswith('0000000000000000'))
    source_references = sorted(guid for guid in reference_locations if guid in registry
                              and not any(path in scoped_metas for path in registry[guid]))
    failed = any((missing_assets, missing_meta, orphan_meta, invalid_meta,
                  duplicates, dangling, text_read_errors, registry_read_errors))
    result = {
        'utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'status': 'failed' if failed else 'passed',
        'cityId': 'great_sativadam', 'contentVersion': 14,
        'newAssets': len(assets), 'scopedFolders': len(folders),
        'scopedScriptFiles': len(SCRIPTS), 'checkedUniqueReferences': len(reference_locations),
        'resolvedSourceGuidReferences': len(source_references),
        'danglingLocalGuids': dangling,
        'danglingReferenceLocations': {guid: sorted(reference_locations[guid]) for guid in dangling},
        'duplicateGuids': duplicates,
        'missingMeta': missing_meta, 'missingMetas': missing_meta,
        'orphanMetas': orphan_meta, 'invalidScopedMetas': sorted(invalid_meta),
        'missingRequiredAssets': missing_assets,
        'assetReadErrors': text_read_errors, 'registryReadErrors': registry_read_errors,
        'scope': 'GreatSativadam production files, directories, scene and city scripts; GUID resolution through Assets, Packages and imported PackageCache',
        'modifiedUnityAssets': False,
    }
    report = ROOT / 'Docs/GreatSativadam/asset_reference_receipt.json'
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 1 if failed else 0


if __name__ == '__main__':
    raise SystemExit(main())