"""Prove the approved memory-grid change alters only seven owned prefab positions."""
from __future__ import annotations

import datetime
import hashlib
import json
import pathlib
import re
import struct

from little_montana_report import assembly_inputs, authored_inputs

ROOT = pathlib.Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/LittleMontana"
BEFORE = DOC / "Validation/MemoryGridBefore"
PLACEMENT_BEFORE = DOC / "Validation/MemoryPlacementBefore"
ORIGINAL_PLANS = [(6, -5), (12, -5), (6, -11), (12, -11)]


def first_grid_plans(person: str) -> list[tuple[float, float]]:
    return {
        "lm_ruth": [(3.1, -5), (3.1, -18), (3.1, -31), (3.1, -44)],
        "lm_tessa": [(-5, 3), (6, 5), (-10, 8), (15, 14)],
    }.get(person, [(6, -5), (15, -5), (6, -18), (15, -18)])


def final_plans(person: str) -> list[tuple[float, float]]:
    if person == "lm_ruth":
        return [(4.5, -1), (3.1, -30), (3.1, 3), (3.1, -44)]
    if person in ("lm_ellie", "lm_noah", "lm_ben"):
        return [(6, -5), (15, -5), (6, -18), (-5, -10)]
    return first_grid_plans(person)


def sha(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(path: pathlib.Path) -> str:
    return path.relative_to(ROOT).as_posix()


def f32(value: float) -> float:
    return struct.unpack("f", struct.pack("f", value))[0]


def world(plan: tuple[float, float]) -> tuple[float, float, float]:
    diagonal = f32(.70710678118)
    return f32((plan[0] + plan[1]) * diagonal), 0.0, f32((plan[1] - plan[0]) * diagonal)


def documents(value: str) -> dict[str, str]:
    parts = re.split(r"(?=^--- !u!)", value, flags=re.MULTILINE)
    result = {"header": parts[0]}
    for part in parts[1:]:
        match = re.match(r"--- !u!(\d+) &(-?\d+)", part)
        if not match:
            raise ValueError("Invalid Unity document header")
        result[match.group(2)] = part
    return result


POSITION = re.compile(
    r"(    - target: \{fileID: [-\d]+, guid: [a-f0-9]{32}, type: 3\}\r?\n"
    r"      propertyPath: m_LocalPosition\.([xyz])\r?\n      value: )([^\r\n]+)"
    r"(\r?\n      objectReference: \{fileID: 0\})"
)


def check_actor(before: pathlib.Path, after: pathlib.Path, person: dict, old_plans: list | None = None) -> dict:
    old_bytes, new_bytes = before.read_bytes(), after.read_bytes()
    old, new = documents(old_bytes.decode("utf-8")), documents(new_bytes.decode("utf-8"))
    assert list(old) == list(new), "Document identities or order changed"
    memories = []
    old_plans = ORIGINAL_PLANS if old_plans is None else old_plans
    new_plans = final_plans(person["id"])
    expected_names = {"Memory_" + str(chapter + 1) + "_" + choice["id"]: chapter
                      for chapter, definition in enumerate(person["chapters"])
                      for choice in (definition["choices"] if chapter == 2 else definition["choices"][:1])}
    for identity, old_doc in old.items():
        new_doc = new[identity]
        name_match = re.search(r"      propertyPath: m_Name\r?\n      value: (Memory_[^\r\n]+)", old_doc)
        if not name_match:
            assert old_doc == new_doc, f"Non-memory document {identity} changed"
            continue
        name = name_match.group(1)
        assert name in expected_names, f"Unexpected memory name {name}"
        chapter = expected_names[name]
        old_positions, new_positions = list(POSITION.finditer(old_doc)), list(POSITION.finditer(new_doc))
        assert len(old_positions) == len(new_positions) == 3, f"Expected three position overrides for {name}"
        old_vector = {match.group(2): float(match.group(3)) for match in old_positions}
        new_vector = {match.group(2): float(match.group(3)) for match in new_positions}
        assert len(old_vector) == len(new_vector) == 3
        expected_old, expected_new = world(old_plans[chapter]), world(new_plans[chapter])
        assert all(abs(old_vector[axis] - expected_old[index]) <= .00001 for index, axis in enumerate("xyz")), f"Unexpected before position for {name}"
        assert all(abs(new_vector[axis] - expected_new[index]) <= .00001 for index, axis in enumerate("xyz")), f"Unexpected after position for {name}"
        # Remove only numeric position values. Names, IDs, source links, scales,
        # rotations, active flags, components, ordering and line endings remain exact.
        replace = lambda match: match.group(1) + "@approved_position@" + match.group(4)
        assert POSITION.sub(replace, old_doc) == POSITION.sub(replace, new_doc), f"Other serialized data changed for {name}"
        source = re.search(r"  m_SourcePrefab: (\{[^\r\n]+\})", old_doc)
        assert source is not None, "Memory must retain its source-prefab link"
        memories.append({"documentId": identity, "name": name, "source": source.group(1),
                         "oldPlan": old_plans[chapter], "newPlan": new_plans[chapter],
                         "beforePosition": old_vector, "afterPosition": new_vector,
                         "changedAxes": [axis for axis in "xyz" if old_vector[axis] != new_vector[axis]]})
    assert len(memories) == 5 and {m["name"] for m in memories} == set(expected_names), "Exactly five authored memories required"
    return {"path": relative(after), "beforeSha256": sha(before), "afterSha256": sha(after),
            "onlyPositionOverridesChanged": True, "memories": memories}


def main() -> int:
    snapshot = json.loads((BEFORE / "hashes.json").read_text(encoding="utf-8"))
    content_path = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"
    content = json.loads(content_path.read_text(encoding="utf-8-sig"))
    people = {person["id"]: person for person in content["vipStories"]}
    actors, issues = [], []
    for path, expected in snapshot["actors"].items():
        before, after = BEFORE / pathlib.Path(path).name, ROOT / path
        try:
            assert sha(before) == expected, "Before snapshot bytes do not match recorded hash"
            actors.append(check_actor(before, after, people[after.stem]))
        except (AssertionError, ValueError, OSError) as error:
            issues.append({"path": path, "reason": str(error)})
    placement_snapshot = json.loads((PLACEMENT_BEFORE / "hashes.json").read_text(encoding="utf-8"))
    placement_actors, placement_issues = [], []
    for path, expected in placement_snapshot["actors"].items():
        before, after = PLACEMENT_BEFORE / pathlib.Path(path).name, ROOT / path
        try:
            assert sha(before) == expected, "Placement before bytes do not match recorded hash"
            placement_actors.append(check_actor(before, after, people[after.stem], first_grid_plans(after.stem)))
        except (AssertionError, ValueError, OSError) as error:
            placement_issues.append({"path": path, "reason": str(error)})
    placement = {"status": "passed" if len(placement_actors) == 7 and not placement_issues else "failed",
                 "baseline": relative(PLACEMENT_BEFORE / "hashes.json"), "baselineUtc": placement_snapshot["utc"],
                 "actors": placement_actors, "checkedMemories": sum(len(actor["memories"]) for actor in placement_actors),
                 "changedMemoryTargets": sum(bool(memory["changedAxes"]) for actor in placement_actors for memory in actor["memories"]),
                 "issues": placement_issues}
    if placement["checkedMemories"] != 35 or placement["changedMemoryTargets"] != 7:
        placement["status"] = "failed"
    outside_changes = [path for path, expected in snapshot["authoredHashes"].items()
                       if path not in snapshot["actors"] and (not (ROOT / path).is_file() or sha(ROOT / path) != expected)]
    outside_changes += sorted({relative(path) for path in authored_inputs()} - set(snapshot["authoredHashes"]))
    play_changes = [path for path, expected in snapshot["playInputs"].items()
                    if not (ROOT / path).is_file() or sha(ROOT / path) != expected]
    domain_changes = [path for path, expected in snapshot["domainInputs"].items()
                      if not (ROOT / path).is_file() or sha(ROOT / path) != expected]
    play_changes += sorted({relative(path) for path in assembly_inputs("Weedtopia.Game.PlayModeTests")[0]} - set(snapshot["playInputs"]))
    domain_changes += sorted({relative(path) for path in assembly_inputs("Weedtopia.Game.EditModeTests")[0]} - set(snapshot["domainInputs"]))
    value = {"utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
             "status": "passed" if len(actors) == 7 and not issues and not outside_changes and not play_changes and not domain_changes and placement["status"] == "passed" else "failed",
             "scope": "Seven owned resident prefabs; only approved exact-old-grid memory m_LocalPosition override values differ. Both original-to-final and first-grid-to-final corrections are checked separately; all other authored assets and Domain/Play runtime/test inputs remain byte-identical.",
             "baseline": relative(BEFORE / "hashes.json"), "baselineUtc": snapshot["utc"],
             "contentSha256": sha(content_path), "playRun": snapshot["playRun"],
             "playRunSha256": snapshot["playRunSha256"], "actors": actors,
             "placementCorrection": placement,
             "checkedMemories": sum(len(actor["memories"]) for actor in actors),
             "changedMemoryTargets": sum(bool(memory["changedAxes"]) for actor in actors for memory in actor["memories"]),
             "issues": issues, "outsideActorAuthoredChanges": outside_changes,
             "playInputChanges": play_changes, "domainInputChanges": domain_changes,
             "preservedAuthoredFiles": len(snapshot["authoredHashes"]) - len(snapshot["actors"])}
    (DOC / "memory_grid_asset_diff.json").write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: value[key] for key in ("status", "checkedMemories", "changedMemoryTargets", "issues", "outsideActorAuthoredChanges", "playInputChanges", "domainInputChanges")}))
    return 0 if value["status"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
