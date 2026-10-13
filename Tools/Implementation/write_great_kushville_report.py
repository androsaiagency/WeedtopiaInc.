"""Generate the German integration report from the actual final summary only.

No productive source, separate receipt, screenshot, or test result is read here.
The closer verifies those inputs and records their evidence in validation_summary.
Missing, failed, or inconsistent summaries never produce a success report.
"""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Docs/GreatKushville"
SUMMARY = FOLDER / "validation_summary.json"
REPORT = FOLDER / "IntegrationReport.md"
RESIDENTS = (
    ("Selma Wehr", "gk_selma", "Kanalingenieurin; öffnet Wasserwissen und gemeinsame Verantwortung"),
    ("Aurel Blatt", "gk_aurel", "Gärtner; verbindet sein Gartenbuch mit dem gemeinsamen Wasserplan"),
    ("Daria Markt", "gk_daria", "Marktkoordinatorin; schafft faire Lieferfenster und offene Absprachen"),
    ("Leandro Vale", "gk_leandro", "Bäcker; verbindet alte Handwerksräume mit neuen Rezepten"),
    ("Kira Stieg", "gk_kira", "Terrassenarchitektin; hält gemeinsame Zugänge und Hofmitten offen"),
    ("Benno Bogen", "gk_benno", "Steinmetz; bewahrt den Gründerbogen und öffnet ihn für neue Geschichten"),
    ("Yuna Klang", "gk_yuna", "Chronistin; bewahrt Stimmen, Erinnerungen und gemeinsame Entscheidungen"),
    ("Oskar Takt", "gk_oskar", "Kurierplaner; organisiert zuverlässige Übergaben und geteilte Dienste"),
)


def cell(value):
    return str(value).replace("|", r"\|").replace("\r", " ").replace("\n", " ")


def number(value):
    return f"{int(value):,}".replace(",", ".")


def check(summary, name):
    return any(row.get("check") == name and row.get("passed") is True
               for row in summary.get("checks", []))


def gate(value):
    return "Im Summary bestätigt" if value is True else "Offen / nicht bestätigt"


def render(summary):
    population = summary["population"]
    game = summary["gameView"]
    capture = summary["finalCaptureRun"]
    rows = summary["tests"]
    screenshots = summary["screenshots"]
    matrix = game["matrix"]
    languages = ", ".join(game["languages"])
    resolutions = sorted({(entry["width"], entry["height"]) for entry in screenshots})
    sizes = ", ".join(f"{width} × {height}" for width, height in resolutions)
    full_domain = summary.get("fullDomainRegressionCompleted")
    regression = ("Die vollständige Domain-Regressionssuite wurde laut Summary abgeschlossen."
                  if full_domain is True else
                  "Die vollständige Domain-Regressionssuite ist nicht als abgeschlossen bestätigt. Die unten aufgeführten tatsächlich abgeschlossenen Läufe bestimmen den Umfang der Testaussage.")
    lines = [
        "# Great Kushville – Integrationsbericht", "",
        f"Quelle: `Docs/GreatKushville/validation_summary.json`, Stand `{cell(summary.get('utc', 'nicht angegeben'))}`. Dieser Bericht liest ausschließlich dieses abschließende Summary. Status: **`{cell(summary['status'])}`**.", "",
        "Great Kushville ist als elfte Stadt integriert. Die alte Kanal- und Gartenmetropole verlor bei ihrem schnellen Wachstum gemeinsame Wasserrechte, Warenwege und Begegnungsorte. Die vier Viertel Kushgärten, Kanalmarkt, Terrassenhöfe und Gründerbogen werden durch die dauerhaften Bewohnergeschichten wieder verbunden.", "",
        "Die Einladung erscheint bei **" + number(population["unlockResidents"]) + " gleichzeitig vorhandenen Einwohnern in den zehn bisherigen, per Stadturkunde angenommenen Städten**. Ein erreichter gemeinsamer Höchststand bleibt gespeichert. Die Stadturkunde vergibt einmalig 12.500 Geld und sieben Teamkarten; anschließend ist die Reise möglich. Geld, Sammlung und Team bleiben gemeinsam, Gebäude, Routen, Aufgaben, Aktivität und Bewohnergeschichten bleiben stadtspezifisch.", "",
        "## Geschichte und Bewohner", "",
        "Die Stadt besitzt 56 Bauplätze, 35 Routen, 18 Hauptaufgaben, acht dauerhaft bleibende Bewohner, 32 persönliche Kapitel, 24 wiederkehrende Aufgaben und drei Gemeinschaftsprojekte. Nach abgeschlossenen Kapiteln bleiben Personen, Entscheidungen und sichtbare Erinnerungen in der Stadt erhalten.", "",
        "| Bewohner | ID | Geschichte |", "| --- | --- | --- |",
    ]
    lines += [f"| {name} | `{identity}` | {description} |" for name, identity, description in RESIDENTS]
    lines += ["", "Der Wasserpakt (`gk_joint_water_compact`) folgt ab 2.000 lokalen Einwohnern den ersten Kapiteln aller acht Bewohner; der Umschlagplatz (`gk_joint_shared_exchange`) folgt ab 8.000 Einwohnern deren zweiten Kapiteln; der Gründerweg (`gk_joint_founders_walk`) folgt ab 22.000 Einwohnern deren vierten Kapiteln. Alle Orte zeigen separate gespeicherte Zustände für geschlossen, Wiederherstellung und abgeschlossen. Selmas drittes Kapitel entscheidet dauerhaft zwischen dem offenen Wasserlabor (`water_lab`) und der Kanalwerkstatt (`canal_workshop`).", "",
        "Die neue Szene liegt unter `Assets/Weedtopia/Game/Scenes/GreatKushville.unity`; Stadtassets unter `Assets/Weedtopia/Production/GreatKushville/`. Bewohner und Orte stehen in `Residents.prefab`, `ResidentStories.prefab` und `Prefabs/CanalCommons.prefab`. Acht eigene Architektur-Prefabs und originale transparente Bewohner-, Architektur- und Schildgrafiken ergänzen die feste isometrische 2D/2.5D-Darstellung. Die Grafikaufträge sind in `Docs/GreatKushville/ArtSource/prompts.json` dokumentiert.", "",
        "## Gezielte Wohnkapazitätsänderung", "",
        "Der einzige produktive Unterschied an bisherigen Gebäudedefinitionen betrifft den letzten Wohnhausbonus: **135 → 475**, wirksam ausschließlich auf **Stufe 5000**. Die Grundkapazität steigt dort von 260 auf 600; frühere Stufen bleiben gleich. Ein maximaler Bewohner-Betreuer erreicht den Faktor 51 und damit **" + number(population["maximumPerHouse"]) + " Einwohner je Haus**. Mit **" + number(population["housesPerCity"]) + " Häusern** und **" + number(population["mandatoryParksPerCity"]) + " erhaltenem Park je Stadt** erreichen die **" + number(population["sourceCityCount"]) + " bisherigen Städte** theoretisch **" + number(population["maximumPreviousCitiesPopulation"]) + " Einwohner**. Der im Summary geprüfte tatsächliche Simulations- und Save-Pfad bleibt maßgeblich für die Freischaltung.", "",
        "## Tatsächlich abgeschlossene Testläufe", "",
        f"Das Summary bestätigt **{number(summary['uniqueNUnitPassed'])} unterschiedliche bestandene NUnit-Fälle** bei **{number(summary['uniqueNUnitCases'])} unterschiedlichen Fällen**. Wiederholungen werden in dieser Zahl nicht doppelt gezählt. {regression}", "",
        "| Lauf | Modus | Gesamt | Bestanden | Fehlgeschlagen | Übersprungen | Unentschieden |", "| --- | --- | ---: | ---: | ---: | ---: | ---: |",
    ]
    lines += [f"| `{cell(row['run'])}` | {cell(row['mode'])} | {number(row['total'])} | {number(row['passed'])} | {number(row['failed'])} | {number(row['skipped'])} | {number(row['inconclusive'])} |" for row in rows]
    lines += ["", "Die Test-XMLs und ihre Hashes stehen im Summary. Die lokalen Tests umfassen die Bevölkerungsgrenze, einmalige Stadturkundenbelohnung, elf Städte, Save-/Migrations-/Offline-Verhalten, Bewohnerentscheidungen, die statischen Assets und die wiederholbare Installation. Ihr exakter Umfang ergibt sich aus den aufgeführten Läufen; ein nicht abgeschlossenes zusätzliches Gesamtpaket wird nicht als bestanden dargestellt.", "",
        "## Game View und Erhalt bestehender Arbeit", "",
        f"Der abgeschlossene Capture-Lauf `{cell(capture['run'])}` hat Status `{cell(capture['status'])}` und Exitcode `{cell(capture['exitCode'])}`. Das Summary enthält **{number(len(screenshots))} tatsächliche Aufnahmen** und **{number(len(matrix))} Matrixaufnahmen** für `{languages}` in den Auflösungen {sizes}. Die Matrix umfasst Anfangsaufgabe, letzte Reisezeile und Selmas Entscheidung. Die weiteren Aufnahmen belegen Ankunft, Ortsentwicklung, beide Entscheidungen, Offline-Fortsetzung und Reisen durch die Städte.", "",
        "| Runtime-Nachweis | Im Summary bestätigt |", "| --- | --- |",
    ]
    runtime_flags = (("Gespeicherte Geschichten", "savedStoryRoundtrip"), ("Wohnhausanzeige stimmt mit Simulation überein", "housingInspectionMatchesSimulation"), ("Rückreise zur bisherigen Stadt", "returnedToPreviousCity"), ("Alle elf Städte besucht", "allElevenCitiesVisited"), ("Elf-Städte-Offline-Fortsetzung", "elevenCityOfflineRoundtrip"), ("Beide Selma-Entscheidungen", "selmaChoiceVariantsVerified"), ("Unabhängige Bodenanker der Architektur", "architectureGroundAnchorsVerified"))
    lines += [f"| {label} | {gate(game.get(key))} |" for label, key in runtime_flags]
    preserve = summary.get("preservation", {})
    strict = preserve.get("strictPrefabPreservation", {})
    if preserve.get("status") == "passed" and strict.get("status") == "passed":
        campaign = strict["sharedCampaign"]
        lines += ["", f"Die strikte Preservation-Prüfung bestätigt **{number(strict['previousCityScenes'])} byte-identische bisherige Stadtszenen** sowie Bootstrap und unveränderte alte Prefab-Overrides. Das gemeinsame Kampagnen-Prefab erweitert seine Reiseliste von **{number(campaign['travelRowsBefore'])} auf {number(campaign['travelRowsAfter'])} Zeilen**. Nur die zusätzliche Referenz und zwei UI-Kinder samt **{number(campaign['addedDocuments'])} neuen verbundenen Dokumenten** wurden ergänzt. Alte Transformationen, Grafikreferenzen und Overrides bleiben unverändert. Insgesamt erfasst der Erhaltungsbericht **{number(preserve['protectedFiles'])} geschützte Dateien**."]
    else:
        lines += ["", "Das Summary bestätigt den allgemeinen Erhaltungscheck: " + gate(check(summary, "Working checkout preservation passed")) + ". Ein detaillierter strikter Prefab-Nachweis ist im Summary nicht ausgewiesen und wird hier nicht zusätzlich behauptet."]
    lines += ["", "## Prüfung wiederholen", "",
        "Die genaue Reihenfolge und Pfade stehen in `Docs/GreatKushville/README.md`. Die Installation läuft ausdrücklich über `Weedtopia/Cities/Install or Repair Great Kushville` oder den Batch-Runner. Der Batch-Runner startet nur ohne vorhandene Unity-Instanz oder Projektsperre. Eine vorhandene Preservation-Baseline bleibt erhalten.", "",
        "```powershell", "Set-Location 'D:\\Unity Games\\Weedtopia Inc\\WeedtopiaInc'", "python -X utf8 .\\Tools\\Implementation\\author_great_kushville.py", ".\\Tools\\Implementation\\Run-GreatKushville.ps1 -Mode Install -Label Integration", "# EditMode- und PlayMode-Testgruppen separat ausführen und Ergebnisdateien behalten.", ".\\Tools\\Implementation\\Run-GreatKushville.ps1 -Mode Capture -Label GameView", "python -X utf8 .\\Tools\\Implementation\\great_kushville_preservation.py verify", "python -X utf8 .\\Tools\\Implementation\\great_kushville_prefab_preservation.py", "# Erst nach close_great_kushville.py mit den tatsächlichen Testreceipts:", "python -X utf8 .\\Tools\\Implementation\\write_great_kushville_report.py", "```", "",
        "## Grenzen der Abnahme", "",
        "| Bereich | Tatsächlicher Summary-Status |", "| --- | --- |",
    ]
    gates = (("Lokale technische Integration", "technicalIntegrationComplete"), ("Vollständige Domain-Regressionssuite", "fullDomainRegressionCompleted"), ("Vollständiger menschlicher Spieldurchlauf", "fullHumanPlaythrough"), ("Physisches Gerät / Touch / Geräteaudio", "physicalDeviceAcceptance"), ("Ausdrückliche visuelle Nutzerfreigabe", "userArtApproval"), ("Muttersprachliche Prüfung", "nativeSpeakerReviewCompleted"), ("Release-Abnahme", "releaseAccepted"))
    lines += [f"| {label} | {gate(summary.get(key))} |" for label, key in gates]
    fixtures = "verwendet" if summary.get("diagnosticGrowthFixtures") is True else "im Summary nicht bestätigt"
    lines += ["", f"Beschleunigte diagnostische Wachstumsstände wurden {fixtures}. Die Aufnahmen zeigen tatsächlich gerenderte Spielzustände und prüfen produktive Runtime-/Save-Pfade; sie bestätigen keinen vollständigen menschlichen Langzeitdurchlauf. Editor- und automatisierte Bildprüfungen erteilen keine Nutzerfreigabe oder Geräte-/Release-Abnahme.", ""]
    return "\n".join(lines)


def main():
    if not SUMMARY.is_file():
        print(json.dumps({"status": "missing_validation_summary", "source": str(SUMMARY)}))
        return 2
    try:
        summary = json.loads(SUMMARY.read_text(encoding="utf-8-sig"))
        if summary.get("status") != "passed_local_integration":
            print(json.dumps({"status": "report_not_emitted", "summaryStatus": summary.get("status")}))
            return 2
        if summary.get("errors") or summary.get("technicalIntegrationComplete") is not True:
            raise ValueError("Success status contradicts failed integration evidence")
        report = render(summary)
    except (ValueError, KeyError, TypeError) as error:
        print(json.dumps({"status": "invalid_validation_summary", "error": str(error)}))
        return 2
    REPORT.write_text(report, encoding="utf-8")
    print(json.dumps({"status": "written", "report": str(REPORT), "source": str(SUMMARY)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
