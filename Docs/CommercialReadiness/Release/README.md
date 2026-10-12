# Releasebindung und Gesamtabnahme

Stand: 5. Oktober 2026. Diese Umsetzung verbessert die ReleaseidentitÃ¤t und Diagnosewerkzeuge. Sie erteilt keine GerÃ¤te-, HÃ¶r-, menschliche oder Storefreigabe.

## Implementiert

- Fingerprint-Schema 2 mit vollstÃ¤ndigen Projekteinstellungen einschlieÃŸlich `ProjectSettings.asset`, konkretem BuildTarget/BuildOptions, aktiver Buildprofil-/Szenenliste, Unity-Version, Backend, Stripping, Definitionssymbolen und Grafik-/Android-Bauparametern.
- Nicht direkt in Szenen referenzierte Resources/StreamingAssets und Importmetadaten sind Teil der EingÃ¤nge. Virtuelle Packagepfade werden Ã¼ber die versionierten Package-Manifeste gebunden. Fehlende Pflichtinputs werden ausdrÃ¼cklich dokumentiert und blockieren Release.
- SHA-256 wird Ã¼ber Streams berechnet, damit groÃŸe Bilddateien nicht zum Hashen komplett in Arbeitsspeicher kopiert werden.
- `production_preflight.json` und `release_inputs.json` besitzen denselben Fingerprint/Buildkontext. Der Buildguard verwendet die tatsÃ¤chlich gemeldete Buildplattform und Bauoptionen. Development-Diagnostik ist an ihre Windows-Plattform gebunden.
- Preflight schÃ¼tzt ungespeicherte Szenen und aktive Buildprofile vor Ã–ffnen anderer Szenen.
- Editor-Profiling hat getrennte Messfenster, korrekte GC-Deltas, Spitzen-Allokationen, Maximalframe und FramebudgetÃ¼berschreitungen fÃ¼r 30/60 FPS. Der Bericht erklÃ¤rt Editor-/Capture-Overhead und weist `targetAccepted=false`/`releaseAccepted=false` aus.
- Gamedesign und Architektur beschreiben die vorhandenen zwei StÃ¤dte, 112 BauplÃ¤tze, 70 Warenwege, Bewohnergeschichten, fÃ¼nf Sprachen, Weedtopia Grove, Spritecache und Shopgrenzen.

## AusgefÃ¼hrte Verifikation

`source_compile.json` enthÃ¤lt die erfolgreiche separate Kompilierung von Editorcode und Assettests gegen die installierten Unity-Bibliotheken. `fingerprint_checks.json` enthÃ¤lt acht tatsÃ¤chlich ausgefÃ¼hrte PrÃ¼fungen der produktiven Fingerprintlogik mit isolierten Dateien in .NET. Das sind keine nativen Unity-TestlÃ¤ufe.

Die ergÃ¤nzten `ReleaseFingerprintTests` prÃ¼fen Determinismus, geÃ¤nderte PlayerSettings, fehlende/wiederhergestellte Inputs, Plattform-/Buildoptionen-/Profilwechsel sowie echte Projekt-Resources und ihre Metadaten. Projekt-/API-PrÃ¼fungen benÃ¶tigen Unity und zÃ¤hlen erst nach einem zugehÃ¶rigen nativen XML als bestanden.

## Explizite Gesamttests im vorhandenen Editor

`CommercialReadinessTestJob.Begin` fÃ¼hrt zuerst die vollstÃ¤ndigen EditMode-Assemblies `Weedtopia.Game.EditModeTests`, `Weedtopia.Game.AssetTests` und `Weedtopia.Editor.BuildingArtPipeline.Tests` aus. Nur ein ausdrÃ¼cklich bestandener EditMode-Gesamtlauf startet anschlieÃŸend die vollstÃ¤ndigen `Weedtopia.Game.PlayModeTests`. Abbruch, Fehler, inconclusive oder leere Ergebnisse Ã¶ffnen die Folgestufe nicht.

Der bereits vorhandene Layout-Dateijob kennt genau den zusÃ¤tzlichen Befehl `commercial-tests`. Er startet und beendet keine Unity-Instanz. Vor Beginn sind Benutzer-Playmode, ungespeicherte Szenen, laufende Test-/Capturejobs, Testrunner, Import/Compilation und Builds geschÃ¼tzt. SessionState hÃ¤lt den eigenen Ablauf Ã¼ber Domain Reloads. Gesichert und nach Ende wiederhergestellt werden Szene-/Active-Scene-Setup, Objektauswahl, Game-View-GrÃ¶ÃŸe, runInBackground, Pausezustand und Spracheinstellung. Eine neue ungespeicherte manuelle SzenenÃ¤nderung blockiert das Wiederherstellen und wird nicht verworfen.

Ergebnisse liegen unter `Docs/CommercialReadiness/2026-10-05/Tests/<run-id>/` mit Original-XMLs und JSON-ZÃ¤hlern pro Stufe. `Tests/latest.json` zeigt den aktuellen Lauf. Jeder Lauf hat einen eigenen Ordner, `source_inputs.json` bindet seine Skripte. Bestehende Tests benutzen isolierte Saves/Ranglisten; der Runner verÃ¤ndert keine persÃ¶nlichen Spielstandsdateien. Native Tests sind noch keine physische Touch-/Audio-/Leistungsabnahme.

## UnabhÃ¤ngige Releasegates

| Gate | Erforderlicher Nachweis |
| --- | --- |
| `ui_overflow` | Aktueller vollstÃ¤ndiger Layoutlauf fÃ¼r die vorgesehenen Plattform-/AuflÃ¶sungsbedingungen |
| `target_profile` | Akzeptiertes Profiling des tatsÃ¤chlichen Playerbuilds auf ZielgerÃ¤ten |
| `human_playthrough` | Menschlicher vollstÃ¤ndiger Einstieg und nachvollziehbare PrÃ¼fung der Spielfolge |
| `audio_listening` | PersÃ¶nliche HÃ¶rabnahme einschlieÃŸlich Wiederholung und GerÃ¤temischung |
| `audio_license` | Vorhandene NutzerbestÃ¤tigung, passende Quellen/Lizenzen und Zuordnung zum tatsÃ¤chlichen Releasebestand |

Eine Ã„nderung des Fingerprintverfahrens macht alte Belege absichtlich ungÃ¼ltig. Technische Ergebnisse dÃ¼rfen die fÃ¼nf Abnahmen nicht selbst erklÃ¤ren. Die vorhandene separate GebÃ¤udekunstfreigabe und dokumentierte Stable-Audio-Berechtigung bleiben erhalten.

Die Storekonfiguration, echte Zahlungsanbindung, Storepreise/Kaufbelege, Kaufwiederherstellung, Produkt-/Datenschutzangaben, Release-Signierung, physische GerÃ¤teabnahme und der finale Releasebuild bleiben offene externe beziehungsweise menschliche Arbeiten.
