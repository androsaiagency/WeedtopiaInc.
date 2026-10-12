"""Compare authored identity, transforms, assets and settings with this revision's baseline."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Docs/LocalizationRevision/2026-10-06"
BASELINE = FOLDER / "Baseline"
HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+).*\n", re.MULTILINE)


def documents(text):
    matches = list(HEADER.finditer(text))
    return {
        match.group(2): (int(match.group(1)), text[match.end():matches[i+1].start() if i+1 < len(matches) else len(text)])
        for i, match in enumerate(matches)
    }


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    manifest = json.loads((BASELINE / "manifest.json").read_text(encoding="utf-8-sig"))
    failures = []
    changed = []
    transforms = game_objects = authored_files = 0
    normalized_prefab_references = 0
    label_guid = re.search(r"guid: (\w+)", (ROOT / "Assets/Weedtopia/Scripts/Runtime/Unity/LocalizedLabel.cs.meta").read_text()).group(1)
    prefab_paths = {}
    original_prefabs, current_prefabs = {}, {}
    for meta in BASELINE.rglob("*.prefab.meta"):
        guid = re.search(r"guid: (\w+)", meta.read_text()).group(1)
        prefab_paths[guid] = meta.with_suffix("").relative_to(BASELINE)

    def retained_prefab_source(body, visited=None):
        """Unity discards unused stripped local aliases after removing a label override.

        Their real prefab objects must still exist with their original IDs, through any
        nested prefab chain. A removed authored object does not satisfy this check.
        """
        match = re.search(r"m_CorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+)", body)
        if not match:
            return False
        source_id, guid = match.groups()
        marker = (guid, source_id)
        visited = set() if visited is None else visited
        if marker in visited or guid not in prefab_paths:
            return False
        visited.add(marker)
        relative = prefab_paths[guid]
        if guid not in original_prefabs:
            original_prefabs[guid] = documents((BASELINE / relative).read_text(encoding="utf-8-sig"))
            current_prefabs[guid] = documents((ROOT / relative).read_text(encoding="utf-8-sig"))
        original_source = original_prefabs[guid].get(source_id)
        if not original_source:
            return False
        if source_id in current_prefabs[guid]:
            return current_prefabs[guid][source_id][0] == original_source[0]
        return retained_prefab_source(original_source[1], visited)
    for entry in manifest:
        relative = entry["path"].replace("\\", "/")
        current = ROOT / relative
        if not current.exists():
            failures.append("Missing file: " + relative)
            continue
        if digest(current) == entry["hash"]:
            continue
        changed.append(relative)
        if relative.startswith("ProjectSettings/") or relative.endswith(".meta"):
            failures.append("Settings or GUID changed: " + relative)
            continue
        if current.suffix not in (".unity", ".prefab"):
            continue
        authored_files += 1
        original = documents((BASELINE / relative).read_text(encoding="utf-8-sig"))
        updated = documents(current.read_text(encoding="utf-8-sig"))
        for object_id, (class_id, body) in original.items():
            if object_id not in updated:
                # The explicit migration removes obsolete static bindings from presenter-owned values.
                if class_id == 114 and "guid: " + label_guid in body:
                    continue
                if retained_prefab_source(body):
                    normalized_prefab_references += 1
                    continue
                failures.append(f"Existing object removed: {relative}#{object_id}")
                continue
            updated_class, updated_body = updated[object_id]
            if updated_class != class_id:
                failures.append(f"Object type changed: {relative}#{object_id}")
            if class_id in (4, 224):
                transforms += 1
                if body != updated_body:
                    failures.append(f"Authored transform changed: {relative}#{object_id}")
            elif class_id == 1:
                game_objects += 1
                # Only additions/removal of localization/layout components are expected.
                strip_components = lambda value: re.sub(r"  m_Component:\n(?:  - component:.*\n)*", "", value)
                if strip_components(body) != strip_components(updated_body):
                    failures.append(f"GameObject identity or state changed: {relative}#{object_id}")
    result = {
        "passed": not failures,
        "baselineFiles": len(manifest),
        "changedFiles": changed,
        "changedAuthoredFilesInspected": authored_files,
        "existingTransformsCompared": transforms,
        "existingGameObjectIdentitiesCompared": game_objects,
        "normalizedUnusedPrefabAliasesWithRetainedSourceObjects": normalized_prefab_references,
        "failures": failures,
    }
    (FOLDER / "preservation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value for key, value in result.items() if key != "changedFiles"}, ensure_ascii=False))
    raise SystemExit(0 if result["passed"] else 1)


if __name__ == "__main__":
    main()
