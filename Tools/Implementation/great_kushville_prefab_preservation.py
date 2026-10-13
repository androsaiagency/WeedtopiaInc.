"""Strict, read-only proof of the actual Great Kushville travel-prefab addition.

Unlike the broad preservation audit, this check allows no changes whatsoever to
old PrefabInstance overrides, old scene documents, or old scene bytes. The only
permitted shared-prefab changes are one appended travel-row reference and the two
new UI children that belong to that row. Only its connected UI graph may be new.
"""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Docs/GreatKushville/Preservation"
BEFORE = FOLDER / "Before"
CAMPAIGN = "Assets/Weedtopia/Production/BigRockCity/CityCampaign.prefab"
REPORT = FOLDER / "prefab_result.json"
DOCUMENT = re.compile(r"^--- !u!\d+ &(-?\d+).*?(?=^--- !u!|\Z)", re.M | re.S)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def documents(path):
    return {match.group(1): match.group(0)
            for match in DOCUMENT.finditer(path.read_text(encoding="utf-8"))}


def field(document, name):
    match = re.search(r"\n  " + re.escape(name) +
                      r":.*?(?=\n  [A-Za-z_][^:\n]*:|\Z)", document, re.S)
    require(match is not None, "Missing serialized field: " + name)
    return match.group(0)


def refs(block):
    return re.findall(r"fileID: (-?\d+)", block)


def reference(document, name):
    values = refs(field(document, name))
    require(len(values) == 1, "Expected one reference in " + name)
    return values[0]


def without(document, name):
    return document.replace(field(document, name), "\n  " + name + ": VERIFIED")


def script_guid(name):
    path = ROOT / ("Assets/Weedtopia/Scripts/Runtime/Unity/" + name + ".cs.meta")
    return re.search(r"guid: (\w+)", path.read_text(encoding="utf-8")).group(1)


def audit():
    manifest = json.loads((FOLDER / "baseline.json").read_text(encoding="utf-8"))
    report = {"status": "unverified", "policy": "old scenes and prefab overrides exact; only one connected travel UI graph may be added", "scenes": []}
    scenes = sorted((BEFORE / "Assets/Weedtopia/Game/Scenes").glob("*.unity"))
    require(len(scenes) == 11, "Expected ten previous city scenes plus Bootstrap")
    for previous in scenes:
        relative = previous.relative_to(BEFORE).as_posix()
        current = ROOT / relative
        old, new = documents(previous), documents(current)
        prefab_ids = [identity for identity, document in old.items()
                      if "\nPrefabInstance:\n" in document]
        unchanged_overrides = all(new.get(identity) == old[identity] for identity in prefab_ids)
        byte_identical = previous.read_bytes() == current.read_bytes()
        meta = relative + ".meta"
        meta_identical = digest(ROOT / meta) == manifest.get(meta)
        entry = {"path": relative, "byteIdentical": byte_identical,
                 "metaIdentical": meta_identical, "oldSerializedDocuments": len(old),
                 "oldPrefabInstances": len(prefab_ids),
                 "allOldPrefabInstanceDocumentsIdentical": unchanged_overrides}
        report["scenes"].append(entry)
        require(byte_identical and meta_identical and unchanged_overrides,
                "Prior scene or prefab overrides changed: " + relative)

    old, new = documents(BEFORE / CAMPAIGN), documents(ROOT / CAMPAIGN)
    require(not old.keys() - new.keys(), "Shared campaign removed old objects")
    added = new.keys() - old.keys()
    changed = [identity for identity in old if old[identity] != new[identity]]
    prefab_ids = [identity for identity, document in old.items()
                  if "\nPrefabInstance:\n" in document]
    require(all(old[identity] == new[identity] for identity in prefab_ids),
            "Shared campaign changed an old PrefabInstance override")
    ui_ids = [identity for identity, document in old.items()
              if script_guid("CityCampaignUi") in document]
    require(len(ui_ids) == 1, "Expected exactly one shared CityCampaignUi")
    ui = ui_ids[0]
    prior_rows, current_rows = refs(field(old[ui], "travelRows")), refs(field(new[ui], "travelRows"))
    require(len(prior_rows) == 10 and len(current_rows) == 11 and
            current_rows[:-1] == prior_rows, "Travel list must append exactly one row")
    require(without(old[ui], "travelRows") == without(new[ui], "travelRows"),
            "Shared campaign changed another UI field")
    row_id = current_rows[-1]
    require(row_id in added and script_guid("CityTravelRow") in new[row_id] and
            "\n  cityId: great_kushville\n" in new[row_id], "New row has incorrect city identity")
    progress_id = reference(new[row_id], "progress")
    button_id = reference(new[row_id], "travel")
    progress_object = reference(new[row_id], "m_GameObject")
    require(progress_id in added and button_id in added and progress_object in added,
            "New travel row must own new UI components")
    require(reference(new[progress_id], "m_GameObject") == progress_object and
            "UnityEngine.UI.Text" in new[progress_id], "Progress reference must point to its own Text")
    button_object = reference(new[button_id], "m_GameObject")
    require(button_object in added and "UnityEngine.UI.Button" in new[button_id],
            "Travel reference must point to a new Button")
    require("\n  m_Name: GreatKushvilleProgress\n" in new[progress_object] and
            "\n  m_Name: TravelGreatKushville\n" in new[button_object], "Unexpected new root UI names")

    graph = set()
    root_transforms = []

    def walk(game_object):
        require(game_object in added, "New UI hierarchy reached an old object")
        require(game_object not in graph, "Repeated or cyclic new UI hierarchy")
        graph.add(game_object)
        components = re.findall(r"- component: \{fileID: (-?\d+)\}", new[game_object])
        require(all(identity in added for identity in components), "New UI reached an old component")
        graph.update(components)
        transforms = [identity for identity in components if "\nRectTransform:\n" in new[identity]]
        require(len(transforms) == 1, "New UI object must have one RectTransform")
        transform = transforms[0]
        for child in refs(field(new[transform], "m_Children")):
            require(child in added, "New UI reached an old child transform")
            walk(reference(new[child], "m_GameObject"))
        return transform

    root_transforms = [walk(progress_object), walk(button_object)]
    require(graph == added and len(added) == 14, "Unexpected extra shared-prefab additions")
    parents = {reference(new[identity], "m_Father") for identity in root_transforms}
    require(len(parents) == 1 and next(iter(parents)) in old, "New UI roots must share the existing container")
    parent = next(iter(parents))
    old_children, new_children = refs(field(old[parent], "m_Children")), refs(field(new[parent], "m_Children"))
    require(new_children == old_children + root_transforms, "Container may only append these two UI children")
    require(without(old[parent], "m_Children") == without(new[parent], "m_Children"),
            "Existing UI container transform/layout changed")
    require(set(changed) == {ui, parent}, "Another old shared-prefab document changed")
    require(digest(ROOT / (CAMPAIGN + ".meta")) == manifest.get(CAMPAIGN + ".meta"),
            "Shared campaign GUID/import metadata changed")
    report.update(status="passed", previousCityScenes=10, bootstrapByteIdentical=True,
                  sharedCampaign={"path": CAMPAIGN, "unchangedOldPrefabInstances": len(prefab_ids),
                                  "changedOldDocuments": changed, "addedDocuments": len(added),
                                  "travelRowsBefore": len(prior_rows), "travelRowsAfter": len(current_rows),
                                  "newCityId": "great_kushville", "newUiRoots": ["GreatKushvilleProgress", "TravelGreatKushville"],
                                  "unrelatedTransformArtOrPrefabOverrideChanges": []})
    return report


if __name__ == "__main__":
    try:
        result = audit()
    except (ValueError, OSError, KeyError) as error:
        result = {"status": "failed", "error": str(error)}
    REPORT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))
    raise SystemExit(result["status"] != "passed")
