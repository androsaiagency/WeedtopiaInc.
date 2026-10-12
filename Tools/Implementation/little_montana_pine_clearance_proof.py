"""Prove the separately approved owned-pine repair without changing prior evidence."""
from __future__ import annotations

import datetime
import hashlib
import json
import math
import pathlib
import re

from little_montana_memory_grid_proof import POSITION, documents, world

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/LittleMontana"
BEFORE = DOC / "Validation/PineClearanceBefore"
VALLEY = "Assets/Weedtopia/Production/LittleMontana/Prefabs/ValleyPlaces.prefab"
SCENE = "Assets/Weedtopia/Game/Scenes/LittleMontana.unity"
PINE_GUID = "46b22a0e742a9aa4483f6b29af204cfc"
# Root-approved exact table from the independent 24-pine / 35-live-route audit.
APPROVED: dict[str, tuple[tuple[float, float], tuple[float, float]]] = {
    "ValleyPine_9": ((160, -51), (150, -51)),
    "ValleyPine_11": ((250, -44), (260, -44)),
}


def sha(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def distance(point: tuple[float, float], start: tuple[float, float], end: tuple[float, float]) -> float:
    vector = end[0] - start[0], end[1] - start[1]
    length = vector[0] ** 2 + vector[1] ** 2
    portion = max(0.0, min(1.0, ((point[0] - start[0]) * vector[0] + (point[1] - start[1]) * vector[1]) / length)) if length else 0.0
    return math.hypot(point[0] - start[0] - portion * vector[0], point[1] - start[1] - portion * vector[1])


def active_routes() -> dict[str, list[tuple[float, float]]]:
    scene = documents((ROOT / SCENE).read_bytes().decode("utf-8"))
    presentations = [doc for doc in scene.values() if "::Weedtopia.Unity.ProductionPresentation" in doc]
    assert len(presentations) == 1, "Exactly one actual ProductionPresentation is required"
    references = re.search(r"  routes:\r?\n((?:  - \{fileID: \d+\}\r?\n)+)", presentations[0])
    assert references, "Serialized live route references are missing"
    result = {}
    for identity in re.findall(r"fileID: (\d+)", references.group(1)):
        doc = scene[identity]
        identifier = re.search(r"  routeId: (lm_[^\r\n]+)", doc)
        points = re.search(r"  points:\r?\n((?:  - \{[^\r\n]+\}\r?\n)+)", doc)
        assert identifier and points, "Referenced route identity or polyline is missing"
        assert identifier.group(1) not in result, "Live route identities must be unique"
        vectors = re.findall(r"x: ([^,]+), y: ([^,]+), z: ([^}]+)", points.group(1))
        result[identifier.group(1)] = [(float(x), float(z)) for x, _, z in vectors]
        assert len(result[identifier.group(1)]) >= 2, "Route must have at least two authored points"
    assert len(result) == 35, "Exactly 35 live Little Montana routes are required"
    return result


def closest_route(vector: dict[str, float], routes: dict[str, list]) -> tuple[float, str]:
    return min((min(distance((vector["x"], vector["z"]), a, b) for a, b in zip(points, points[1:])), name)
               for name, points in routes.items())


def check_valley() -> dict:
    assert APPROVED, "An exact approved pine position table is required"
    before, after = BEFORE / "ValleyPlaces.prefab", ROOT / VALLEY
    old, new = documents(before.read_bytes().decode("utf-8")), documents(after.read_bytes().decode("utf-8"))
    assert list(old) == list(new), "Prefab document identities, order or header changed"
    routes = active_routes()
    pines, changed = [], []
    for identity, old_doc in old.items():
        new_doc = new[identity]
        name_match = re.search(r"      propertyPath: m_Name\r?\n      value: (ValleyPine_\d+)", old_doc)
        if not name_match:
            assert old_doc == new_doc, f"Non-pine document {identity} changed"
            continue
        name = name_match.group(1)
        source = re.search(r"  m_SourcePrefab: (\{[^\r\n]+\})", old_doc)
        assert source and f"guid: {PINE_GUID}," in source.group(1), f"Unexpected shared pine source for {name}"
        old_positions, new_positions = list(POSITION.finditer(old_doc)), list(POSITION.finditer(new_doc))
        assert len(old_positions) == len(new_positions) == 3, f"Exactly three root position overrides required for {name}"
        old_vector = {match.group(2): float(match.group(3)) for match in old_positions}
        new_vector = {match.group(2): float(match.group(3)) for match in new_positions}
        assert set(old_vector) == set(new_vector) == set("xyz")
        old_distance, old_route = closest_route(old_vector, routes)
        new_distance, new_route = closest_route(new_vector, routes)
        if name in APPROVED:
            old_plan, new_plan = APPROVED[name]
            expected_old, expected_new = world(old_plan), world(new_plan)
            assert all(abs(old_vector[axis] - expected_old[index]) <= .0001 for index, axis in enumerate("xyz")), f"Unexpected old authored position for {name}"
            assert all(abs(new_vector[axis] - expected_new[index]) <= .0001 for index, axis in enumerate("xyz")), f"Unexpected new approved position for {name}"
            assert old_distance < 3, f"Only actually colliding pines may move: {name}"
            assert new_distance >= 8 - .0001, f"Moved pine lacks approved clearance: {name}"
            replace = lambda match: match.group(1) + "@approved_position@" + match.group(4)
            assert POSITION.sub(replace, old_doc) == POSITION.sub(replace, new_doc), f"Other serialized data changed for {name}"
            assert old_doc != new_doc, f"Approved colliding pine was not repaired: {name}"
            changed.append(name)
        else:
            assert old_doc == new_doc, f"Unapproved pine {name} changed"
        assert new_distance >= 3 - .0001, f"An owned pine remains within collision clearance: {name}"
        pines.append({"documentId": identity, "name": name, "source": source.group(1),
                      "beforePosition": old_vector, "afterPosition": new_vector,
                      "beforeRouteDistance": old_distance, "beforeNearestRoute": old_route,
                      "afterRouteDistance": new_distance, "afterNearestRoute": new_route,
                      "changed": name in changed})
    assert len(pines) == 24 and {pine["name"] for pine in pines} == {f"ValleyPine_{index}" for index in range(1, 25)}
    assert set(changed) == set(APPROVED), "Exactly the approved colliding pines must move"
    return {"path": VALLEY, "beforeSha256": sha(before), "afterSha256": sha(after),
            "onlyApprovedPositionOverrideValuesChanged": True, "checkedPines": 24,
            "checkedActiveRoutes": len(routes), "changedPines": changed,
            "scenePath": SCENE, "sceneSha256": sha(ROOT / SCENE), "pines": pines}


def main() -> int:
    from little_montana_report import assembly_inputs, authored_inputs

    snapshot = json.loads((BEFORE / "hashes.json").read_text(encoding="utf-8"))
    issues = []
    valley = {}
    try:
        assert sha(BEFORE / "ValleyPlaces.prefab") == snapshot["valleySha256"], "Before prefab hash mismatch"
        valley = check_valley()
    except (AssertionError, OSError, ValueError, KeyError) as error:
        issues.append(str(error))
    changes = {}
    groups = {"authored": authored_inputs(), "playMode": assembly_inputs("Weedtopia.Game.PlayModeTests")[0],
              "domain": assembly_inputs("Weedtopia.Game.EditModeTests")[0]}
    for group, paths in groups.items():
        changes[group] = [name for name, expected in snapshot[group].items()
                          if not (group == "authored" and name == VALLEY)
                          and (not (ROOT / name).is_file() or sha(ROOT / name) != expected)]
        changes[group] += sorted({path.relative_to(ROOT).as_posix() for path in paths} - set(snapshot[group]))
    actor_changes = [name for name, expected in snapshot["actors"].items() if sha(ROOT / name) != expected]
    old_memory = json.loads((BEFORE / "memory_grid_asset_diff.json").read_text(encoding="utf-8"))
    value = {"utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
             "status": "passed" if valley and not issues and not any(changes.values()) and not actor_changes else "failed",
             "scope": "Only the exact approved colliding roots in the owned ValleyPlaces prefab may change numeric m_LocalPosition override values. All other prefab bytes, 617 other authored files, seven final actors and every Domain/Play runtime/test input remain exact.",
             "baseline": (BEFORE / "hashes.json").relative_to(ROOT).as_posix(), "baselineUtc": snapshot["utc"],
             "approvedPlans": APPROVED, "valley": valley, "issues": issues,
             "otherAuthoredChanges": changes["authored"], "playInputChanges": changes["playMode"],
             "domainInputChanges": changes["domain"], "actorChanges": actor_changes,
             "preservedOtherAuthoredFiles": len(snapshot["authored"]) - 1,
             "historicalMemoryProof": {"path": (BEFORE / "memory_grid_asset_diff.json").relative_to(ROOT).as_posix(),
                                       "sha256": sha(BEFORE / "memory_grid_asset_diff.json"), "utc": old_memory["utc"],
                                       "checkedMemories": old_memory["checkedMemories"], "changedMemoryTargets": old_memory["changedMemoryTargets"]}}
    (DOC / "pine_clearance_asset_diff.json").write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": value["status"], "checkedPines": valley.get("checkedPines"),
                      "changedPines": valley.get("changedPines"), "issues": issues, "inputChanges": changes}))
    return 0 if value["status"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
