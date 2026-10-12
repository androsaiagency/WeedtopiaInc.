"""Prepare the explicitly requested final visual-review text corrections locally."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]
REVIEW_PATH = Path(__file__).with_name("reviewed_ui.json")


def main():
    review = json.loads(REVIEW_PATH.read_text(encoding="utf-8"))
    catalogs = {path.stem: {entry["key"]: entry["value"] for entry in json.loads(path.read_text(encoding="utf-8"))["entries"]}
                for path in (ROOT / "Assets/Weedtopia/Game/Localization").glob("*.json")}
    content = json.loads((ROOT / "Assets/Weedtopia/Game/Content/BurningHills.json").read_text(encoding="utf-8-sig"))
    languages = review["languages"]
    entries = review["entries"]

    entries["inventory.waste"][0] = "Davon verfallen {0}"
    entries["ui.premium_title"][languages.index("en")] = "Spend golden Grinders"
    entries["ui.premium"][languages.index("fr")] = "Dépenser {0} Grinder pour « {1} » ? Le montant sera déduit uniquement après ta confirmation."
    for key in ("wheel.entry", "wheel.title"):
        entries[key] = ["Glücksrad", "Lucky Wheel", "Rueda de la suerte", "Roue de la chance", "Колесо удачи"]
    entries["wheel.entry_free"] = ["Glücksrad · Gratis", "Lucky Wheel · Free", "Rueda de la suerte · Gratis", "Roue de la chance · Tour gratuit", "Колесо удачи · Бесплатно"]
    entries["wheel.free"] = ["Kostenlos drehen", "Spin for free", "Girar gratis", "Tourner gratuitement", "Вращать бесплатно"]

    construction = [item for item in content["items"] if item["effect"] == 0]
    assert sorted(item["value"] for item in construction) == [60, 300, 900, 1800, 3600, 18000, 36000, 86400]
    for item in construction:
        hours = item["value"] % 3600 == 0
        number = item["value"] // (3600 if hours else 60)
        for field in ("nameKey", "shortNameKey", "descriptionKey"):
            key = item[field]
            translations = []
            for language in languages:
                if language == "de":
                    translations.append(catalogs[language][key])
                    continue
                unit = ("ч" if hours else "мин") if language == "ru" else ("h" if hours else "min")
                duration = str(number) + " " + unit
                if field == "shortNameKey":
                    title = {"en": "Build time", "es": "Obra", "fr": "Travaux", "ru": "Стройка"}[language]
                    value = title + "\n" + duration
                elif field == "nameKey":
                    title = {"en": "Construction booster", "es": "Acelerador de obra", "fr": "Accélérateur de travaux", "ru": "Ускоритель стройки"}[language]
                    value = title + " · " + duration
                else:
                    template = {
                        "en": "Reduces the remaining construction time of a selected building by up to {duration}. Single use. Unused time is lost.",
                        "es": "Reduce en un máximo de {duration} el tiempo restante de la obra elegida. Se usa una sola vez. El tiempo sobrante se pierde.",
                        "fr": "Réduit le temps de construction restant du chantier choisi de {duration} au maximum. Usage unique. Le temps inutilisé est perdu.",
                        "ru": "Сокращает оставшееся время выбранной стройки максимум на {duration}. Одноразовый предмет. Неиспользованное время пропадает."
                    }[language]
                    value = template.format(duration=duration)
                translations.append(value)
            entries[key] = translations
    review["entries"] = {key: entries[key] for key in sorted(entries)}
    REVIEW_PATH.write_text(json.dumps(review, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"constructionItemsReviewed": len(construction), "durationTextKeysReviewed": len(construction) * 3,
                      "reviewedKeys": len(entries), "numericItemValuesChanged": False}))


if __name__ == "__main__":
    main()
