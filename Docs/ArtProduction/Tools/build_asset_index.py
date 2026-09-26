"""Build a portable Markdown navigation index without copying acceptance status."""
from collections import OrderedDict
import json
import os
from pathlib import Path


def main():
    root = Path(__file__).resolve().parents[3]
    output = root / "Docs/ArtProduction/BUILDING_ASSET_INDEX.md"
    manifest = json.loads((output.parent / "BUILDING_ASSET_MANIFEST.json").read_text(encoding="utf-8-sig"))
    slots = manifest["slots"]
    families = OrderedDict()
    for slot in slots:
        families.setdefault(slot["building_id"], []).append(slot)
    if len(slots) != 251 or len({(s["building_id"], s["tier_id"]) for s in slots}) != 251:
        raise ValueError("The navigation index requires exactly 251 unique manifest slots.")
    if len(families) != 11 or any(len(rows) != (1 if family == "town_hall" else 25) for family, rows in families.items()):
        raise ValueError("The index requires ten 25-slot families and one fixed town hall.")

    def link(label, project_path):
        relative = os.path.relpath(root / project_path, output.parent).replace("\\", "/")
        return f"[{label}](<{relative}>)"

    lines = [
        "# Gebäude-Asset-Index",
        "",
        "Diese Schnellübersicht verlinkt die **251 vorgesehenen Asset-Slots in elf Gebäudefamilien** direkt aus dem Manifest. Sie lässt sich zusammen mit dem Projekt verschieben; sämtliche Dateiverweise sind relativ zu dieser Datei.",
        "",
        "**Verbindlicher aktueller Stand:** [PROGRESS.json](PROGRESS.json) führt Fortschritt und fehlende Ergebnisse; [BUILDING_ASSET_MANIFEST.json](BUILDING_ASSET_MANIFEST.json) führt die Slotdaten und ihren aktuellen Status. Die [visuelle Abnahme](Validation/visual_acceptance.json) bindet akzeptierte Bilder an ihre tatsächlichen Datei-, Prompt- und Referenzhashes. Dieser Index kopiert keine Statuswerte und behauptet keine vollständige oder endgültige Bildabnahme. Während einer laufenden Korrektur können einzelne Bildziele vorübergehend fehlen.",
        "",
        "`PNG` öffnet das einzelne Meshy-Eingabebild. `Meshy` und `Textur` öffnen die kopierbaren englischen Prompts. `Spezifikation` enthält die deutsche Beschreibung; `Settings` die zugehörigen Produktionsvorgaben. `Bildprompt` dokumentiert den tatsächlich verwendeten Generierungstext. `Prefab` verweist auf den vorbereiteten Unity-Wrapper; der Link weist keine bereits zugewiesene 3D-Geometrie nach. Die Familien-Showcases dienen der Stilkontrolle und sind keine Meshy-Eingabebilder.",
        "",
        "Der manuelle Ablauf ist im [Meshy-/Unity-Workflow](MESHY_WORKFLOW.md) beschrieben. Die getrennten Unity-Prüfgates stehen im [Unity-Abnahmebericht](Validation/UNITY_ACCEPTANCE.de.md).",
        "",
        "| Familie | Vorgesehene Slots | Stilkontrolle | Familienbeschreibung |",
        "|---|---:|---|---|",
    ]
    for family, rows in families.items():
        name = rows[0]["building_name_de"].replace("|", "\\|")
        lines.append(f"| [{name}](#{family}) | {len(rows)} | {link('Showcase', f'Docs/ArtProduction/Buildings/{family}/family_showcase.png')} | {link('Familien-DNA', f'Docs/ArtProduction/Buildings/{family}/family_spec.de.md')} |")
    for family, rows in families.items():
        name = rows[0]["building_name_de"]
        lines.extend(["", f'<a id="{family}"></a>', "", f"## {name}", "", f"Familien-ID: `{family}`. " + ("Ein fester Rathaus-Slot; keine Ausbaustufen." if family == "town_hall" else "25 Zustände einschließlich des Tutorial-Ausgangszustands T00."), "", "| Tier | Levelband | Einzelbild | Meshy-Prompt | Texturprompt | Vorgaben | Unity |", "|---|---|---|---|---|---|---|"])
        for slot in sorted(rows, key=lambda item: (item["min_level"], item["max_level"])):
            low, high = slot["min_level"], slot["max_level"]
            levels = str(low) if low == high else f"{low}–{high}"
            if family == "town_hall":
                levels = f"fest ({levels})"
            elif low == 0:
                levels = "0 · Ruine"
            image = link("PNG", slot["reference_image"])
            image_prompt = link("Bildprompt", slot["image_prompt_file"])
            meshy = link("Meshy", slot["meshy_prompt_file"])
            texture = link("Textur", slot["texture_prompt_file"])
            spec = link("Spezifikation", slot["asset_spec_file"])
            settings = link("Settings", slot["meshy_settings_file"])
            prefab = link("Prefab", slot["unity_prefab_target"])
            lines.append(f"| `{slot['tier_id']}` | {levels} | {image} · {image_prompt} | {meshy} | {texture} | {spec} · {settings} | {prefab} |")
        lines.extend(["", "[Zur Familienübersicht](#gebäude-asset-index)"])
    lines.extend(["", "Die Links werden mit `python Docs/ArtProduction/Tools/build_asset_index.py` aus dem Manifest aktualisiert. Dabei werden keine Bilder generiert, keine Modelle zugeordnet und keine Fortschritts- oder Abnahmestände geändert.", ""])
    output.write_text("\n".join(lines), encoding="utf-8")
    print(json.dumps({"index": str(output), "families": len(families), "slots": len(slots), "copied_status_values": False}, ensure_ascii=False))


if __name__ == "__main__":
    main()
