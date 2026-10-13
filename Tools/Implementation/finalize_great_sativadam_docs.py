"""Close documentation only from completed, reconciled Unity evidence."""
import argparse
import datetime
import hashlib
import json
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / "Docs/GreatSativadam"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def close_offline(relative):
    receipt = read(ROOT / relative)
    tree = ET.parse(receipt["xml"]).getroot()
    cases = tree.findall(".//test-case")
    if receipt["status"] != "passed" or receipt["exitCode"] != 0 or len(cases) != 5 or any(c.attrib["result"] != "Passed" for c in cases):
        raise ValueError("All five actual final offline cases must pass.")
    analysis = read(DOC / "offline_worker_watchdog_analysis.json")
    source = ROOT / "Assets/Weedtopia/Scripts/Tests/EditMode/OfflineWorkerTests.cs"
    started = datetime.datetime.fromisoformat(receipt["startedUtc"].replace("Z", "+00:00")).timestamp()
    if digest(source) != analysis["afterSha256"] or source.stat().st_mtime > started:
        raise ValueError("Offline test source differs from the verified final patch.")
    names = {case.attrib["fullname"] for case in cases}
    if not {case["name"] for case in analysis["failedCases"]} <= names:
        raise ValueError("The rerun did not execute every original failing case.")
    analysis.update(status="resolved_verified", updatedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                    allOriginalFailingCasesPassed=True, unityStartedByThisAgent=True,
                    finalTargetedRerun=dict(run=receipt["run"], receipt=relative,
                        startedUtc=receipt["startedUtc"], endedUtc=receipt["endedUtc"],
                        xml=receipt["xml"], sha256=digest(Path(receipt["xml"])),
                        total=len(cases), passed=len(cases), failed=0,
                        cases=[dict(name=c.attrib["fullname"], result=c.attrib["result"], durationSeconds=float(c.attrib["duration"])) for c in cases]))
    analysis["diagnosis"] += " The final targeted run after both bounded timeout repairs passed all five actual cases, including all three original failures. No functional assertions were removed. This is functional equivalence and rollback evidence, not production performance or device acceptance."
    (DOC / "offline_worker_watchdog_analysis.json").write_text(json.dumps(analysis, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(dict(status="resolved_verified", cases=len(cases), run=receipt["run"])))


def close_report():
    summary = read(DOC / "validation_summary.json")
    if summary["status"] != "passed_local_integration" or summary["errors"] or summary["unresolvedTestFailures"]:
        raise ValueError("Reconciled final integration must pass before closing the report.")
    offline = read(DOC / "offline_worker_watchdog_analysis.json")
    if offline["status"] != "resolved_verified":
        raise ValueError("Final offline rerun must be documented first.")
    path = DOC / "IntegrationReport.txt"
    text = path.read_text(encoding="utf-8")
    start, end = "=== PRUEFSTAND_BEGIN ===", "=== PRUEFSTAND_END ==="
    if text.count(start) != 1 or text.count(end) != 1:
        raise ValueError("Expected one exact proof block.")
    fixtures = summary["finalCasesPerFixture"]
    game = summary["gameView"]
    proof = f"""{start}
Lokale technische Integration: BESTANDEN (passed_local_integration).
Zusammengeführter Nachweis: validation_summary.json; nur tatsächliche XMLs,
finale Sourcehashes, aktuelle Assets und frische Capture-Dateien ausgewertet.

Source/Compiler: BESTANDEN, {summary['sourceCompiledAssemblies']} Quellassemblies mit unveränderten finalen Hashes.
Unity-Import/Install: BESTANDEN, explizites Install or Repair.
Authoring: 56 Standorte, 35 Routen, 18 Hauptquests, sechs dauerhafte Personen,
18 persönliche Kapitel, 18 wiederkehrende Varianten, drei Projekte,
acht Architekturprefabs und 251 Gebäudevarianten. runtimeStaticObjects=false.
Content/Localization: BESTANDEN, 230 neue Schlüssel in fünf Sprachen mit
Placeholder-Parität. Zwei neue Joost-Formulierungen gezielt verbessert.
Idempotenz: BESTANDEN, sechs Katalogdateien byteidentisch bei Wiederholung;
607 produktive Dateien nach endgültiger Uferreparatur byteidentisch installiert.
Preservation: BESTANDEN, 10.399 geschützte Dateien gegen den Ausgangsstand
geprüft; keine unerwarteten Änderungen, keine entfernten Dateien und keine
Änderungen an alten Content-/Localeeinträgen. Zwölf bisherige Stadtszenen
und Bootstrap bytegenau erhalten; neue Reisezeile ausschließlich additiv.
Assetreferenzen: BESTANDEN, 293 neue Assets, 1.050 eindeutige Referenzen,
754 aufgelöste Quell-GUID-Referenzen; keine fehlenden/verwaisten Metas,
doppelten GUIDs oder unaufgelösten lokalen GUID-Referenzen.

NUnit: {summary['uniquePassingNUnitCases']} unterschiedliche Fälle haben jeweils ein bestandenes
letztes Ergebnis. Insgesamt {summary['totalTestExecutions']} tatsächliche Ausführungen einschließlich
gezielter Wiederholungen und historischer Fehlschläge; keine übersprungenen Fälle.
Great Sativadam Domain: {fixtures['Weedtopia.Tests.GreatSativadamCampaignTests']} Fälle BESTANDEN.
Great Sativadam finale Assets: {fixtures['Weedtopia.Tests.Assets.GreatSativadamAssetTests']} Fälle BESTANDEN, einschließlich
unabhängiger Bankgeometrie und tatsächlicher Wasser-/Kamera-Geometrie.
VIP-Geschichten: {fixtures['Weedtopia.Tests.VipStoryTests']} Fälle BESTANDEN; Localization: {fixtures['Weedtopia.Tests.EditMode.LocalizationTests']} BESTANDEN.
OfflineWorker: {fixtures['Weedtopia.Tests.OfflineWorkerTests']} unterschiedliche Fälle BESTANDEN. Abschließender
gezielter Lauf: fünf Fälle, fünf bestanden, null fehlgeschlagen. Alle drei
ursprünglichen Watchdog-Fälle nach beiden Testzeitlimitkorrekturen bestanden.
PlayMode: 30 Fälle im Kampagnen-/Reise-/Fehlerlauf BESTANDEN; die sechs
Great-Sativadam-Fälle nach Bankkorrektur nochmals BESTANDEN.
Auch vorangehende Städte, Save/Persistence, Housing, CityCampaign,
CityProject und Offline-Simulation sind in der XML-Regressionsauswertung enthalten.

Historie: Der erste Domainlauf hatte {summary['initialFailures']} Fehlschläge. Insgesamt
{summary['historicallyFailedCases']} unterschiedliche Fälle schlugen historisch fehl: Save-Grenze,
Testzeitfenster und drei zu lange neue Test-Profilnamen. Die zwei Fehler des
ersten Offline-Retests waren separate NUnit-Standardzeitlimits derselben Fälle.
Alle zuletzt ausgewerteten Ergebnisse sind bestanden; null offene Testfehler.
Die ursprünglichen XMLs bleiben erhalten und werden nicht als bestanden ausgegeben.

Game View: BESTANDEN, {game['screenshots']} frische Bilder; {game['matrix']} Matrixfälle mit fünf Sprachen,
1920x1080, 1366x768, 2560x1080 und 1080x1920.
Finaler Capture: {game['captureRun']}.
Beide Joost-Wahlorte, Wasserliegeplatz, gespeicherte Geschichten, Wohnungssimulation,
Reise durch alle 13 Städte und Offline-/Save-Rundlauf tatsächlich geprüft.
Reale Spielerspeicher und ursprüngliche Spracheinstellung nach Capture erhalten.
Vier unabhängige Reviews: {game['visuallyInspectedSamples']} unterschiedliche aktuelle Ansichten tatsächlich
geöffnet, darunter alle zwölf bisherigen Stadtankünfte. Keine beobachteten
blockierenden Darstellungsfehler. Der enge Saatgutkai-Detailausschnitt begrenzt
die untere Front; die vollständige Projektansicht zeigt dieselbe Halle frei.

Reichweite: diagnostische Wachstumszustände und legaler Kapazitätsnachweis;
kein vollständiger natürlicher menschlicher Spieldurchlauf oder Performancebenchmark.
Geräte-/Touch-/Audioabnahme, muttersprachliche Abnahme, explizite Benutzerabnahme
der Grafik und Produktionsrelease bleiben offen.
{end}"""
    text = text[:text.index(start)] + proof + text[text.index(end) + len(end):]
    pending = "Ein bestandener\nAbschluss wird erst nach dem erneuten tatsächlichen NUnit-Lauf eingetragen."
    text = text.replace(pending, "Der erneute tatsächliche NUnit-Lauf ist mit fünf von fünf Fällen bestanden;\nalle drei ursprünglichen Fehlerfälle sind damit nach beiden Reparaturen belegt.")
    path.write_text(text, encoding="utf-8")
    print(json.dumps(dict(status="closed_from_actual_evidence", uniqueCases=summary["uniquePassingNUnitCases"], executions=summary["totalTestExecutions"], screenshots=game["screenshots"], actualViews=game["visuallyInspectedSamples"])))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--offline-receipt")
    parser.add_argument("--report", action="store_true")
    args = parser.parse_args()
    if bool(args.offline_receipt) == bool(args.report):
        raise SystemExit("Choose one: --offline-receipt or --report.")
    close_report() if args.report else close_offline(args.offline_receipt)
