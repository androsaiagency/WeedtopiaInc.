"""Capture current authored assets before Seven Hills; compare all prior content by identity."""
import hashlib, json, pathlib, re, sys, subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/SevenHills/Preservation'

# Reviewed integration points. Every other baseline file, including both prior scenes,
# must retain its exact bytes; prior catalog entries are also compared separately.
ALLOWED_CHANGES = {
    'Assets/Weedtopia/Game/Content/BurningHills.json',
    'Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab',
    'ProjectSettings/EditorBuildSettings.asset',
}
ALLOWED_CHANGES.update('Assets/Weedtopia/Game/Localization/' + code + '.json' for code in ['de', 'en', 'es', 'fr', 'ru'])
ALLOWED_CHANGES.update('Assets/Weedtopia/Scripts/Runtime/' + path for path in [
    'Application/CityCampaignService.cs', 'Application/GameSession.cs', 'Application/GameStateTransactionSnapshot.cs',
    'Domain/CityCampaign.cs', 'Domain/GameState.cs', 'Domain/VipStories.cs', 'Persistence/LocalSaveStore.cs',
    'Unity/BuildingInteractionUi.cs', 'Unity/CityCampaignUi.cs', 'Unity/CityMandateActor.cs', 'Unity/GameUi.cs', 'Unity/ModulePanelLayout.cs',
    'Unity/VipStoriesUi.cs',
])
ALLOWED_CHANGES.update('Assets/Weedtopia/Scripts/Tests/' + path for path in [
    'EditMode/CityCampaignTests.cs', 'EditMode/OfflineWorkerTests.cs', 'EditMode/SimulationOfflineOptimizationTests.cs',
    'PlayMode/CityCampaignUiTests.cs', 'PlayMode/CitySceneFailureUiTests.cs', 'PlayMode/GameFlowTests.cs',
    'PlayMode/ResponsiveStartupBenchmarkTests.cs', 'PlayMode/VipStoryUiTests.cs', 'EditMode/OfflineStartupTests.cs',
    'EditorAssets/BigRockCityAssetTests.cs', 'EditorAssets/NativeOfflineModelBenchmarkTests.cs',
    'EditorAssets/PremiumConfirmationAssetTests.cs', 'EditorAssets/ProductionPresentationTests.cs',
    'EditorAssets/ShopAssetTests.cs', 'EditorAssets/SupportDiagnosticsAssetTests.cs', 'EditorAssets/TypographyAssetTests.cs',
    'EditorAssets/UiExperienceAssetTests.cs', 'EditorAssets/UiRefinementAssetTests.cs',
])

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def capture():
    DOC.mkdir(parents=True, exist_ok=True)
    baseline = DOC / 'baseline.json'
    if baseline.exists():
        raise SystemExit('Baseline already exists; refusing to overwrite preservation evidence.')
    folders = ['Assets/Weedtopia', 'ProjectSettings']
    files = {p.relative_to(ROOT).as_posix(): digest(p) for folder in folders
             for p in (ROOT / folder).rglob('*') if p.is_file() and 'SevenHills' not in p.as_posix()}
    baseline.write_text(json.dumps(files, indent=2), encoding='utf-8')
    for relative in ['Assets/Weedtopia/Game/Content/BurningHills.json',
                     'Assets/Weedtopia/Game/Scenes/BurningHills.unity',
                     'Assets/Weedtopia/Game/Scenes/BigRockCity.unity']:
        target = DOC / pathlib.Path(relative).name
        target.write_bytes((ROOT / relative).read_bytes())
    status = subprocess.run(['git', '-c', 'safe.directory='+ROOT.as_posix(), 'status', '--porcelain=v1'],
                            cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
    (DOC / 'git-status-before.txt').write_text(status.stdout, encoding='utf-8')
    print(json.dumps({'capturedFiles': len(files), 'gitChangesBefore': len(status.stdout.splitlines())}))

def verify():
    baseline = json.loads((DOC / 'baseline.json').read_text(encoding='utf-8'))
    changed = [p for p, h in baseline.items() if not (ROOT / p).exists() or digest(ROOT / p) != h]
    before = json.loads((DOC / 'BurningHills.json').read_text(encoding='utf-8'))
    after = json.loads((ROOT / 'Assets/Weedtopia/Game/Content/BurningHills.json').read_text(encoding='utf-8'))
    content_errors = []
    for key, original in before.items():
        if key == 'version':
            continue
        current = after.get(key)
        if isinstance(original, list) and original and isinstance(original[0], dict):
            identity = 'key' if key == 'texts' else 'id'
            by_id = {x.get(identity): x for x in current or []}
            for entry in original:
                if by_id.get(entry.get(identity)) != entry:
                    content_errors.append(key + ':' + str(entry.get(identity)))
        elif current != original:
            content_errors.append(key)
    scene_results = {}
    for name in ['BurningHills', 'BigRockCity']:
        def documents(path):
            source = path.read_text(encoding='utf-8-sig')
            return {m.group(1): m.group(0) for m in re.finditer(
                r'^--- !u!\d+ &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)', source, re.M | re.S)}
        old = documents(DOC / (name + '.unity'))
        new = documents(ROOT / ('Assets/Weedtopia/Game/Scenes/' + name + '.unity'))
        scene_results[name] = {'originalObjects': len(old), 'removedObjects': sorted(old.keys()-new.keys()),
                               'changedObjects': [i for i in old if old[i] != new.get(i)],
                               'addedObjects': len(new.keys()-old.keys())}
    report = {'protectedFiles': len(baseline), 'changedFiles': changed,
              'unexpectedChanges': [p for p in changed if p not in ALLOWED_CHANGES],
              'unchangedProtectedFiles': len(baseline) - len(changed),
              'removedFiles': [p for p in baseline if not (ROOT / p).exists()],
              'priorContentChanges': content_errors, 'scenes': scene_results}
    report['status'] = 'passed' if not report['removedFiles'] and not report['unexpectedChanges'] and not content_errors and not any(
        x['removedObjects'] for x in scene_results.values()) else 'failed'
    (DOC / 'result.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))
    if report['status'] != 'passed':
        raise SystemExit(1)

if __name__ == '__main__':
    capture() if sys.argv[1] == 'capture' else verify()
