"""Apply explicitly reviewed UI corrections locally; preserve every non-text content value."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Assets/Weedtopia/Game/Localization"


def main():
    review = json.loads(Path(__file__).with_name("reviewed_ui.json").read_text(encoding="utf-8"))
    languages = review["languages"]
    german_index = languages.index("de")
    legacy_path = Path(__file__).with_name("legacy_caption_sources.json")
    legacy = json.loads(legacy_path.read_text(encoding="utf-8")) if legacy_path.exists() else {"language": "de", "entries": []}
    originals = {(entry["key"], entry["value"]) for entry in legacy["entries"]}
    caption_sources_path = Path(__file__).with_name("reviewed_caption_sources.json")
    if caption_sources_path.exists():
        originals.update((entry["key"], entry["value"]) for entry in json.loads(caption_sources_path.read_text(encoding="utf-8"))["entries"])
    german = {entry["key"]: entry["value"] for entry in json.loads((FOLDER / "de.json").read_text(encoding="utf-8-sig"))["entries"]}
    for key, translations in review["entries"].items():
        replacement = translations[german_index]
        if replacement is not None and key in german and replacement != german[key]:
            originals.add((key, german[key]))
    legacy["entries"] = [{"key": key, "value": value} for key, value in sorted(originals)]
    if legacy["entries"]:
        legacy_path.write_text(json.dumps(legacy, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    changed = {}
    for language_index, language in enumerate(languages):
        path = FOLDER / (language + ".json")
        document = json.loads(path.read_text(encoding="utf-8-sig"))
        entries = {entry["key"]: entry["value"] for entry in document["entries"]}
        modified = []
        for key, translations in review["entries"].items():
            if len(translations) != len(languages):
                raise ValueError("Incomplete reviewed translations: " + key)
            value = translations[language_index]
            if value is not None and entries.get(key) != value:
                entries[key] = value
                modified.append(key)
        document["entries"] = [{"key": key, "value": value} for key, value in sorted(entries.items())]
        if modified:
            path.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        changed[language] = modified

    # GameDefinitions' explicit German fallback must use the same source wording.
    content_path = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"
    content = json.loads(content_path.read_text(encoding="utf-8-sig"))
    original_rules = {key: value for key, value in content.items() if key != "texts"}
    texts = {entry["key"]: entry for entry in content["texts"]}
    modified_fallback = []
    for key, translations in review["entries"].items():
        value = translations[german_index]
        if value is not None and (key not in texts or texts[key]["de"] != value):
            if key in texts:
                texts[key]["de"] = value
            else:
                entry = {"key": key, "de": value}
                content["texts"].append(entry)
                texts[key] = entry
            modified_fallback.append(key)
    assert original_rules == {key: value for key, value in content.items() if key != "texts"}
    if modified_fallback:
        content_path.write_text(json.dumps(content, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"changedCatalogKeys": changed, "changedGermanFallbackKeys": modified_fallback,
                      "gameplayDataPreserved": True}, ensure_ascii=False))


if __name__ == "__main__":
    main()
