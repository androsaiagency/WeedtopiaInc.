"""Audit current localized sources without translating or sending project text anywhere.

Unlike the archived migration audit, this validates the current catalogs and authoring
receipt. Candidate lists are explicit review inputs, not claims of linguistic approval.
"""
from pathlib import Path
import argparse
import collections
import datetime
import json
import re
import string

ROOT = Path(__file__).resolve().parents[2]
LOCALES = ROOT / "Assets/Weedtopia/Game/Localization"
SCRIPTS = ROOT / "Assets/Weedtopia/Scripts/Runtime"
EXPECTED_LANGUAGES = {"de", "en", "es", "fr", "ru"}
PLACEHOLDER = re.compile(r"\{\d+(?:,[^}:]+)?(?::[^}]+)?\}")
GERMAN_WORDS = re.compile(
    r"\b(?:Bauzeit|Blüten|Beschleuniger|ausgeben|Ausbau|Aufträge|Belohnung|Einwohner|"
    r"Baustelle|Freischalten|Tagesboni|verbindlich|gutschreiben|Spielstand)\b", re.I
)
ENGLISH_GERMAN_UI = re.compile(r"\b(?:Play|Upgrade|Workers?|Rare|Items?|Freeplay|Spin|Level|Quests?)\b")
GERMAN_RUNTIME_WORDS = re.compile(
    r"[äöüßÄÖÜ]|\b(?:Std\.|Min\.|Sek\.|Geld|Bauzeit|Einwohner|Aufgaben|Ausbau|Karten|Mitarbeiter)\b"
)
TOKENS = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@?"(?:\\.|[^"\\])*"')
LOOKUP = re.compile(r'(?:GameText\.(?:Get|Format)|(?:Definitions|defs)\.(?:Text|Format)|\bT)\("([^"]+)"\s*[,)]')


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def location(path, text, position):
    return {"file": path.relative_to(ROOT).as_posix(), "line": text.count("\n", 0, position) + 1}


def catalog_audit():
    catalogs, errors = {}, []
    for path in sorted(LOCALES.glob("*.json")):
        document = read_json(path)
        language = document["language"]
        if language != path.stem:
            errors.append({"file": str(path), "error": "language code mismatch"})
        keys = [entry["key"] for entry in document["entries"]]
        for key, count in collections.Counter(keys).items():
            if count > 1:
                errors.append({"language": language, "key": key, "error": "duplicate key"})
        catalogs[language] = {entry["key"]: entry["value"] for entry in document["entries"]}
    if set(catalogs) != EXPECTED_LANGUAGES:
        errors.append({"error": "language registry differs", "languages": sorted(catalogs)})
    source = catalogs["de"]
    german_leaks, identical, english_german = [], [], []
    for language, values in catalogs.items():
        for key in sorted(set(source) - set(values)):
            errors.append({"language": language, "key": key, "error": "missing key"})
        for key in sorted(set(values) - set(source)):
            errors.append({"language": language, "key": key, "error": "unexpected key"})
        for key, value in values.items():
            if not key.strip() or not value.strip():
                errors.append({"language": language, "key": key, "error": "empty entry"})
            if "\ufffd" in value:
                errors.append({"language": language, "key": key, "error": "replacement character"})
            if key not in source:
                continue
            if sorted(PLACEHOLDER.findall(value)) != sorted(PLACEHOLDER.findall(source[key])):
                errors.append({"language": language, "key": key, "error": "placeholder mismatch"})
            if value.count("\n") != source[key].count("\n"):
                errors.append({"language": language, "key": key, "error": "line break mismatch"})
            try:
                # Validate braces and formats independently of the runtime service's regex.
                list(string.Formatter().parse(value))
            except ValueError as error:
                errors.append({"language": language, "key": key, "error": str(error)})
            if language != "de" and GERMAN_WORDS.search(value):
                german_leaks.append({"language": language, "key": key, "value": value})
            if language == "de" and ENGLISH_GERMAN_UI.search(value):
                english_german.append({"key": key, "value": value})
            if language != "de" and value == source[key] and re.search(r"[^\W\d_]{3}", value):
                reason = ("authored character or place name" if key.endswith(".name") or key.startswith("city.")
                          or key in {"game.title", "authored.burning_hills.f7918af8", "authored.rocko.33019496"}
                          else "named Grinder currency" if "grinder" in key.lower()
                          else "shared word or abbreviation; individually reviewed")
                identical.append({"language": language, "key": key, "value": value, "reviewReason": reason})
    return catalogs, errors, german_leaks, identical, english_german


def source_audit(source):
    runtime_files = sorted(SCRIPTS.rglob("*.cs"))
    missing, literals, german_literals = [], [], []
    for path in runtime_files:
        text = path.read_text(encoding="utf-8-sig")
        for match in LOOKUP.finditer(text):
            if match[1] not in source:
                missing.append({**location(path, text, match.start()), "key": match[1]})
        # Indirect formatters may return literals without assigning a Text component.
        for token in TOKENS.finditer(text):
            if not token[0].startswith('"'):
                continue
            try:
                value = json.loads(token[0])
            except ValueError:
                continue
            if value in source or re.search(r"(?:Get|Format|Text|T|Find)\($", text[max(0, token.start()-40):token.start()]):
                continue
            if GERMAN_RUNTIME_WORDS.search(value):
                german_literals.append({**location(path, text, token.start()), "literal": value})
        # Keep newlines and strings, discard comments for the display-assignment scan.
        code = TOKENS.sub(lambda m: "".join("\n" if c == "\n" else " " for c in m[0])
                          if m[0].startswith(("//", "/*")) else m[0], text)
        for assignment in re.finditer(r"\b\w+\.text\s*(?:\+=|=)\s*([^;]+);", code):
            expression = assignment[1]
            for token in TOKENS.finditer(expression):
                if not token[0].startswith('"'):
                    continue
                if re.search(r"(?:Get|Format|Text|T)\($", expression[:token.start()]):
                    continue
                try:
                    value = json.loads(token[0])
                except ValueError:
                    continue
                if (value in source or not re.search(r"[^\W\d_]{2}", value)
                        or value in {"WEEDTOPIA", "Dr. ", "N0", "0.#", "0.##", "Cards", "Grinders", "Money", "-Manager"}
                        or re.fullmatch(r"[a-z][a-z0-9_]*(?:\.[a-z0-9_]+)+", value)):
                    continue
                literals.append({**location(path, code, assignment.start()), "literal": value})

    content_path = ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json"
    content = read_json(content_path)
    content_references = []
    def walk(value, pointer=""):
        if isinstance(value, dict):
            for field, item in value.items():
                child = pointer + "/" + field
                if field.endswith("Key") and isinstance(item, str) and item:
                    content_references.append({"field": child, "key": item})
                    if item not in source:
                        missing.append({"file": content_path.relative_to(ROOT).as_posix(), "field": child, "key": item})
                if field != "texts":
                    walk(item, child)
        elif isinstance(value, list):
            for index, item in enumerate(value):
                walk(item, pointer + "/" + str(index))
    walk(content)
    fallback_differences = [{"key": entry["key"], "fallback": entry["de"], "catalog": source.get(entry["key"])}
                            for entry in content["texts"] if source.get(entry["key"]) != entry["de"]]

    label_guid = re.search(r"^guid: (\w+)", (SCRIPTS / "Unity/LocalizedLabel.cs.meta").read_text(), re.M)[1]
    authored_files = sorted(set((ROOT / "Assets/Weedtopia/Game/Scenes").glob("*.unity"))
                            | set((ROOT / "Assets/Weedtopia/Game/Prefabs").rglob("*.prefab"))
                            | set((ROOT / "Assets/Weedtopia/Production").rglob("*.prefab")))
    authored_references, authored_captions = [], []
    for path in authored_files:
        text = path.read_text(encoding="utf-8-sig")
        for match in re.finditer(r"^  m_Text: (.*)$", text, re.M):
            authored_captions.append({**location(path, text, match.start()), "serializedText": match[1]})
        for document in re.finditer(r"^--- !u!\d+ &[^\n]+\n(?:(?!^--- !u!).)*", text, re.M | re.S):
            if "guid: " + label_guid not in document[0]:
                continue
            key = re.search(r"^  key: (.+)$", document[0], re.M)
            if not key:
                missing.append({**location(path, text, document.start()), "key": "", "error": "empty authored key"})
                continue
            value = key[1].strip().strip('"')
            entry = {**location(path, text, document.start() + key.start()), "key": value}
            authored_references.append(entry)
            if value not in source:
                missing.append(entry)
    return {"runtimeFiles": [path.relative_to(ROOT).as_posix() for path in runtime_files],
            "authoredFiles": [path.relative_to(ROOT).as_posix() for path in authored_files],
            "contentReferenceCount": len(content_references), "contentReferences": content_references,
            "authoredKeyCount": len(authored_references), "authoredKeyReferences": authored_references,
            "serializedCaptionCount": len(authored_captions), "serializedCaptionInventory": authored_captions,
            "missingReferencedKeys": missing, "germanFallbackDifferences": fallback_differences,
            "runtimeDisplayLiteralCandidates": literals,
            "remainingGermanRuntimeLiteralCandidates": german_literals}


def construction_duration_audit(catalogs):
    """Compare item text quantities to gameplay seconds, independently of reviewed overrides."""
    content = read_json(ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json")
    units = {
        "de": {3600: "Std.", 60: "Min.", 1: "Sek."},
        "en": {3600: "h", 60: "min", 1: "s"},
        "es": {3600: "h", 60: "min", 1: "s"},
        "fr": {3600: "h", 60: "min", 1: "s"},
        "ru": {3600: "ч", 60: "мин", 1: "с"},
    }
    checks, errors = [], []
    items = [item for item in content["items"] if item["effect"] == 0]
    for item in items:
        seconds = item["value"]
        if not isinstance(seconds, int) or isinstance(seconds, bool) or seconds <= 0:
            errors.append({"item": item["id"], "error": "construction duration must be positive whole seconds", "value": seconds})
            continue
        divisor = next(unit for unit in (3600, 60, 1) if seconds % unit == 0)
        number = seconds // divisor
        for language in sorted(EXPECTED_LANGUAGES):
            unit = units[language][divisor]
            # Digit boundaries prevent 1 min matching 15 min; unit boundaries prevent
            # min matching a word. Match the real numeric data rather than any nonempty label.
            duration = re.compile(r"(?<![\w\d])" + str(number) + r"\s*" + re.escape(unit) + r"(?!\w)")
            for field in ("nameKey", "shortNameKey", "descriptionKey"):
                key = item[field]
                text = catalogs.get(language, {}).get(key, "")
                entry = {"item": item["id"], "language": language, "field": field, "key": key,
                         "valueSeconds": seconds, "expectedDuration": str(number) + " " + unit}
                checks.append(entry)
                if not duration.search(text):
                    errors.append({**entry, "error": "localized construction duration missing or differs from item value", "text": text})
                elif field == "shortNameKey" and not duration.fullmatch(text.split("\n")[-1]):
                    errors.append({**entry, "error": "compact tile duration must occupy its final line", "text": text})
    return {"constructionItemCount": len(items), "constructionDurationCheckCount": len(checks),
            "constructionDurationChecks": checks, "constructionDurationErrors": errors}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--require-authored", action="store_true", help="Require a current explicit authoring receipt with no unbound caption candidates.")
    args = parser.parse_args()
    catalogs, errors, leaks, identical, english_german = catalog_audit()
    sources = source_audit(catalogs["de"])
    construction = construction_duration_audit(catalogs)
    review = read_json(Path(__file__).with_name("reviewed_ui.json"))
    receipt_path = ROOT / "Docs/Localization/authoring_receipt.json"
    receipt = read_json(receipt_path) if receipt_path.exists() else None
    latest_source_time = max(path.stat().st_mtime for path in list(LOCALES.glob("*.json"))
                             + list((ROOT / "Assets/Weedtopia/Scripts").rglob("*.cs")))
    current_receipt = bool(receipt and datetime.datetime.fromisoformat(receipt["utc"]).timestamp() >= latest_source_time)
    unbound = receipt.get("unboundAuthoredCaptionCandidates") if receipt else None
    failures = (errors + sources["missingReferencedKeys"] + sources["germanFallbackDifferences"]
                + leaks + english_german + sources["remainingGermanRuntimeLiteralCandidates"]
                + construction["constructionDurationErrors"])
    if args.require_authored and (not current_receipt or unbound is None or unbound):
        failures.append({"error": "authoring receipt is stale, lacks caption audit or contains unbound captions"})
    result = {"utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "status": "passed" if not failures else "failed", "failures": failures,
              "languages": {language: len(values) for language, values in catalogs.items()},
              "catalogInvariantErrors": errors, "recognizableGermanLeakage": leaks,
              "sameAsGermanCandidates": identical,
              "englishUiWordsInGermanCandidates": english_german,
              "manuallyReviewedUiKeys": sorted(review["entries"]),
              "manuallyReviewedUiKeyCount": len(review["entries"]),
              "authoringReceiptCurrent": current_receipt,
              "unboundAuthoredCaptionCandidates": unbound,
              "linguisticReview": {"fullNativeSpeakerReviewCompleted": False,
                                   "identicalValuesIncludeIntentionalNamesBrandsAndSharedWords": True,
                                   "allCatalogEntriesReceivedTechnicalChecks": True},
              "externalProjectTextTransfers": False, **sources, **construction}
    destination = ROOT / "Docs/LocalizationRevision/2026-10-06"
    destination.mkdir(parents=True, exist_ok=True)
    (destination / "current_text_audit.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "languages": result["languages"],
                      "runtimeFiles": len(sources["runtimeFiles"]), "authoredFiles": len(sources["authoredFiles"]),
                      "contentReferences": sources["contentReferenceCount"], "authoredKeyReferences": sources["authoredKeyCount"],
                      "serializedCaptions": sources["serializedCaptionCount"], "errors": failures,
                      "manualUiKeys": len(review["entries"]), "runtimeLiteralCandidates": sources["runtimeDisplayLiteralCandidates"],
                      "constructionDurationChecks": construction["constructionDurationCheckCount"],
                      "remainingGermanRuntimeLiteralCandidates": sources["remainingGermanRuntimeLiteralCandidates"],
                      "authoringReceiptCurrent": current_receipt, "unboundAuthoredCaptionCandidates": unbound}, ensure_ascii=False))
    if failures:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
