# Little Montana

Little Montana ist die vierte spielbare Stadt von Weedtopia. Der Aufbauauftrag erscheint, sobald **Burning Hills, Big Rock City und Seven Hills gleichzeitig zusammen mindestens 15.000 Einwohner** haben. 5.000 + 5.000 + 5.000 erfüllt die Grenze; 5.000 + 5.000 + 4.999 erfüllt sie nicht. Little Montanas eigene Bevölkerung zählt für diese Grenze nicht mit. Einzelne historische Einwohnerhöchststände der drei Städte werden nicht addiert.

Der Auftrag nutzt die vorhandene Regierungsauftragsoberfläche bei Rocko und in der Stadtauswahl. Nach Annahme ist Little Montana über die bestehende Reiseoberfläche erreichbar. Die einmalige Aufbauhilfe beträgt **3.500 Geld und drei Teamkarten**. Geld, Sammlung und Team gehören weiterhin zum Unternehmen; Gebäude, Warenwege, Hauptaufträge, Bewohnergeschichten und Ortsfortschritt gehören zur jeweiligen Stadt. Bestehende Mitarbeiterzuweisungen bleiben erhalten und alle freigeschalteten Städte laufen im bestehenden Simulations- und Offline-System weiter.

Eine einmal tatsächlich erreichte gemeinsame Schwelle wird gespeichert. Das Angebot verschwindet anschließend nicht bei einem vorübergehenden Bevölkerungsrückgang. Die Annahme wird separat gespeichert; mehrfaches Annehmen zahlt keine zweite Aufbauhilfe aus. Die Summe wird über exakt die drei deklarierten `unlockSources` berechnet. Noch nicht angenommene Städte tragen keine fiktive Bevölkerung bei.

## Stadt und gemeinsame Geschichte

Ein schwerer Sturm hat den alten Bahnhof und den Sägewerkhof geschlossen. Die Zurückgebliebenen halten das Tal durch kleine Gefälligkeiten zusammen. Der Wiederaufbau soll weder alte Rollen erzwingen noch ein leeres Tourismusversprechen verkaufen. Versorgung, Arbeitsplätze und bewohnte Höfe geben Rückkehrern eine Wahl. Der Bahnhof dient als Treffpunkt und Warenabholort; die vorhandenen Lieferfahrzeuge bewegen die Waren. Es gibt kein zusätzliches Eisenbahn-Simulationssystem.

| Viertel | Funktion und Charakter |
| --- | --- |
| Kiefernwiesen | Bestehende Anbau-, Verarbeitungs- und Farmlagerketten zwischen Wald und Wiesen |
| Bahnhofsviertel | Stadtlager, Cafés, offener Vorplatz und Ankunftstafel |
| Adlerhang | Wohnhäuser, Parks, Rastpunkte und die Erinnerungen der Bewohner |
| Alter Sägewerkhof | Butterwerk, Supermarkt, Bäckereien und gemeinsam genutzte Werkstatt |

Little Montana hat **56 eigene Gebäudestandorte, 35 eigene Lieferwege und 18 Hauptaufträge**. Die ersten zwölf Aufträge verwenden dieselben bewährten öffentlichen Spielkommandos und Produktionsverträge wie die vorhandene dritte Stadt, mit eigener Erzählung und eigenen IDs. Danach folgen Bevölkerungskapitel in Little Montana selbst: Bahnhofsvorplatz bei 250, Rückkehrer bei 750, Werkhof bei 1.500, Archiv bei 3.000 sowie Talweg und freier Ausbau bei 6.000 Einwohnern. Hohe Bevölkerung überspringt keine Hauptaufträge. Gemeinschaftsprojekte und spätere persönliche Geschichten sind eigenständige Fortschrittsketten.

## Dauerhafte Bewohner

Die sieben Bewohner bleiben sichtbar, nachdem ein Auftrag abgeschlossen ist. Neue Kapitel verlangen persönlichen Fortschritt und die Einwohnerzahl **in Little Montana**. Die Bevölkerung der anderen Städte schaltet ihre Kapitel nicht vorzeitig frei.

| Bewohner-ID | Figur und persönliche Geschichte | Erstes Auftreten | Weitere Kapitel |
| --- | --- | ---: | --- |
| `lm_mara` | **Mara Fichten**, Försterin: letzte Vorräte → gemeinsamer Pflanztag → Wegpflege teilen → ein eigener freier Nachmittag | Ankunft / 0 | 250 / 1.500 / 6.000 |
| `lm_jon` | **Jon Mercer**, früherer Bahnhofsvorsteher: Abholbank → Namen statt Nummern → offener Wartesaal → Ankommen ohne unerfüllbare Zugversprechen | 50 | 750 / 2.000 / 6.000 |
| `lm_ellie` | **Ellie Bach**, zurückgekehrte Bäckerin: kalter Ofen → eigenes Rezept statt bloßer Kopie → gemeinsames Backen → eigenes Festbrot | 150 | 1.000 / 2.500 / 6.000 |
| `lm_noah` | **Noah Funke**, Mechaniker: erste Werkbank → Ersatzteile mit Herkunft → Reparatur- oder Lernwerkstatt → Verantwortung weitergeben | 300 | 1.250 / 3.000 / 6.000 |
| `lm_ruth` | **Ruth Calder**, Chronistin: Briefkoffer → fehlende schwierige Erinnerungen → Archiv mit vielen Stimmen → offener Brief an die nächsten Nachbarn | 500 | 1.500 / 3.500 / 6.000 |
| `lm_tessa` | **Tessa Vale**, Musikerin: ein Lied für Ellie → Fehler zulassen → gemeinsame Musik oder offene Bühne → das Tal singt mit | 800 | 2.000 / 4.000 / 6.000 |
| `lm_ben` | **Ben Adler**, Wanderführer: erreichbarer Rastpunkt → ehrlicher Umweg → Wegweiser oder gemeinsamer Ausgangspunkt → andere führen lassen | 1.200 | 2.500 / 4.500 / 6.000 |

Jede Figur hat vier persönliche Kapitel und drei rotierende Alltagsvarianten: **28 Kapitel und 21 wiederkehrende Varianten**. Wiederkehrende Aufträge skalieren über das vorhandene Storysystem und warten nach Abschluss 300 Simulationssekunden. Angenommene Geschichten laufen nicht durch eine Spielpause ab. Es gilt die bestehende Grenze von höchstens drei gleichzeitig aktiven Bewohner- oder Gemeinschaftsaufträgen pro Stadt.

Die dritte persönliche Geschichte bietet jeder Figur zwei gespeicherte Entscheidungen. Bei Noah lauten sie **`repair` / `teach`**. Im abgeschlossenen Werkhof erscheinen je nach Entscheidung eine gemeinsame Reparaturwerkstatt oder eine offene Lernwerkstatt. Die übrigen Entscheidungen heißen **`share` / `meet`** und besitzen eigene sichtbare Erinnerungsvarianten in den Bewohnerprefabs. Alle Figuren erhalten je vier dauerhaft gespeicherte Erinnerungen; ein Reload oder Stadtwechsel verwirft diese Fortschritte nicht.

Produktion, Verkäufe, gelieferte Waren und abgeschlossene Lieferungen zählen bei persönlichen Geschichten und Gemeinschaftsprojekten erst **ab Annahme**. Gebäudeziele prüfen den Bestand der aktiven Stadt. Bereits vor Annahme produzierte Waren erfüllen keine neue Liefergeschichte.

## Gemeinschaftsprojekte

| ID und Ort | Einwohner in Little Montana | Beteiligte | Neue Leistung nach Annahme |
| --- | ---: | --- | --- |
| `lm_joint_station_square` – Unser Bahnhofsvorplatz | 1.250 | Mara, Jon, Ellie | 80 Caféverkäufe |
| `lm_joint_open_workyard` – Der offene Werkhof | 3.000 | Noah, Ruth, Tessa | 180 gelieferte Waren |
| `lm_joint_valley_trail` – Unser Talweg | 6.000 | Alle sieben Bewohner | 20 abgeschlossene Lieferungen |

Jedes Projekt setzt die abgeschlossene erste persönliche Geschichte aller Beteiligten voraus. Das Hauptquestende hängt nicht davon ab, dass alle Projekte und persönlichen Kapitel zuvor abgeschlossen werden. Die Orte besitzen authored Zustände für Sturmfolgen, laufenden Wiederaufbau und gemeinsamen Abschluss. Das Ortsvisual liest gespeicherte Einwohnermeilensteine, Projektabschlüsse und Entscheidungen; es erzeugt keine statischen Weltobjekte zur Laufzeit.

## Integration und Assets

Die produktiven Inhalte liegen im vorhandenen gemeinsamen Katalog `Assets/Weedtopia/Game/Content/BurningHills.json`. Version 5 ergänzt die neue Stadt, ihre Instanzen, Lieferwege, Hauptaufträge und Geschichten. Frühere Katalogeinträge bleiben unverändert. Die Save-Migration prüft alte Kataloge vor Ergänzung der vierten Stadt. Fehlerhafte alte Gebäude werden dabei nicht durch neu erstellte Daten verdeckt. Der letzte aktive Stadtstand bleibt erhalten.

**263 neue Textschlüssel** liegen vollständig in `de`, `en`, `es`, `fr` und `ru`: Regierungseinladung und Dreistädteschwelle, Viertel, Hauptaufträge, Dialogvarianten, Bewohner, Kapitel, Entscheidungen, Erinnerungen und Storyziele. Ein neuer allgemeiner Hinweis erklärt den Weiterbetrieb aller übernommenen Städte. Ein additiver Reiseaktionsschlüssel korrigiert missverständliche bisherige Beschriftungen in vier Sprachen. Vorherige Übersetzungseinträge bleiben erhalten.

Die Szene heißt `Assets/Weedtopia/Game/Scenes/LittleMontana.unity`; die eigenen Prefabs, Kataloge, Materialien und Illustrationen liegen unter `Assets/Weedtopia/Production/LittleMontana`. Der gemeinsame Stadtwechsel erhält eine vierte authored Reisezeile. Bestehende UI-, Shop-, Navigations- und Kameraverträge werden weitergenutzt. Die Kamera bleibt fest isometrisch mit den vorhandenen 2D/2.5D-Gebäudedarstellungen.

Die zehn neuen transparenten Illustrationen wurden über das eingebaute ImageGen-Werkzeug erzeugt: sieben Bewohner, ein Stadtschild, ein alter Holzdepotbau und ein Bergkamm. `art_prompts.json` dokumentiert die tatsächlichen Prompts und die gespeicherten Assets. Es wird kein kostenpflichtiger CLI-Fallback verwendet.

## Explizite Installation und Nachweise

`Tools/Implementation/author_little_montana.py` ergänzt Inhalte und Übersetzungen ausdrücklich, additiv und wiederholbar. Vorhandene IDs mit abweichendem Inhalt führen zu einer Fehlermeldung vor dem Schreiben. Der wiederholte Lauf verändert den Katalog und die Sprachdateien nicht. Die produktiven Gebäude-Tunings und ersten drei Stadtkataloge werden nicht neu erzeugt.

Das Unity-Menü **Weedtopia / Cities / Install or Repair Little Montana** führt das eigene Authoring aus. Import, Spielstart und Domain-Reload reparieren die Stadt nicht automatisch. Bestehende manuelle Objekte werden nicht pauschal ersetzt. Der Batchlauf startet nur ohne offenen Unity-Editor und ohne Lockdatei; ein vorhandener Editor wird nicht beendet.

Die Erinnerungen sind verschachtelte Instanzen vorhandener Weltprefabs. Die ausdrückliche Reparatur korrigiert ihre Größe und Anordnung nur bei erkannten ursprünglichen Little-Montana-Instanzen. Eigene manuelle Positionen, Größen, Namen, Fremdlinks und ausgepackte Prefabs bleiben erhalten. Die Bewohner-, Gebäude- und Ortsanker werden dabei nicht versetzt. Ein isolierter nativer Assettest prüft diese Erhaltungsregeln und die wiederholbare Reparatur.

Die tatsächliche Bildprüfung fand außerdem zwei neu platzierte Kiefern auf Lieferwegen. Der Abgleich aller 24 eigenen Kiefern mit den 35 tatsächlich referenzierten Routen begrenzt die Korrektur auf `ValleyPine_9` und `ValleyPine_11`: Planposition `(160,-51)` → `(150,-51)` und `(250,-44)` → `(260,-44)`. Der Stammabstand wächst dabei von 1,74 beziehungsweise 0 auf 10,27 beziehungsweise 10 Planeinheiten bei einer halben Breite des geprüften Lieferkorridors von 3. Die explizite Reparatur erkennt Name, Quellprefab und ursprüngliche Position; manuell versetzte oder umbenannte Bäume bleiben erhalten. `pine_clearance_asset_diff.json` dokumentiert diesen separaten Nachweis nach dem Bewohnerlayout.

```powershell
python Tools/Implementation/author_little_montana.py
python Tools/Implementation/compile_sources.py
& Tools/Implementation/Run-LittleMontana.ps1 -Mode Install -Label LittleMontanaInstall
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.EditModeTests -Label LittleMontanaDomain
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.AssetTests -TestFilter 'BigRockCityAssetTests;SevenHillsAssetTests;LittleMontanaAssetTests' -Label LittleMontanaAssets
& Tools/Implementation/Run-UnityValidation.ps1 -Mode PlayMode -Assembly Weedtopia.Game.PlayModeTests -TestFilter 'CityCampaignUiTests;CitySceneFailureUiTests;VipStoryUiTests;GameFlowTests' -Label LittleMontanaPlay
& Tools/Implementation/Run-LittleMontana.ps1 -Mode MemoryProbe -Label LittleMontanaMemories
& Tools/Implementation/Run-LittleMontana.ps1 -Mode Capture -Label LittleMontanaGameViews
python Tools/Implementation/little_montana_preservation.py verify
python Tools/Implementation/little_montana_report.py
```

`validation_summary.json` ist der aktuelle Nachweisstand. Source-Compile, Unity-Import/Authoring, Domain, Assettests, PlayMode, tatsächliche Game Views und Dateierhaltung sind getrennte Gates. Vorbereitete Wachstums- und Storyzustände belegen Darstellung und Fortschrittsspeicherung, keine menschlich gespielte Aufbauzeit. Geräte-Touch, Geräteaudio, ein neuer Player-/WebGL-Build und die persönliche visuelle Abnahme sind eigene Prüfungen und werden ohne tatsächlichen Lauf nicht als bestanden ausgewiesen.

`MemoryProbe` ist eine ausdrückliche, optionale Darstellungsprobe. Sie führt denselben isolierten Fortschrittsablauf aus und rendert ausschließlich die sieben Bewohnerorte mit allen vier gespeicherten Erinnerungen in 1.920 × 1.080. Die Bilder liegen separat in `MemoryProbeScreenshots`; der vollständige Game-View-Nachweis wird dadurch nicht ersetzt oder überschrieben. Der vollständige `Capture` rendert 77 Ansichten und verlangt alle vier Auflösungen. Eine tatsächlich fehlgeschlagene erste Probe wegen der zunächst zu breit angesetzten Auflösungsprüfung bleibt in `Validation/ProbeCoverageFailure` als Historie erhalten.

Die native Aufbauprüfung verwendet ausschließlich die Little-Montana-Aufbauhilfe als Anfangsbudget: **3.500 Geld und drei Karten**. Über öffentliche Spielkommandos erreicht sie zwölf abgeschlossene Hauptaufträge nach **1.856 Simulationssekunden** (30 Minuten 56 Sekunden); anschließend wartet `lm_station_square`. Der Endstand beträgt 3.673 Geld und sechs Karten. Die Premiumbilanz ist exakt 50 Anfangsgrinders plus ein verdienter Grinder, also 51; kein Premiumkauf und keine Notfallhilfe finanzieren den Aufbau. Alle vorherigen Mitarbeiterzuweisungen bleiben erhalten und die ersten drei Städte liefern im Test kein zusätzliches Einkommen. `foundation_simulation.json` gehört zum frischen nativen Lauf der 24 Little-Montana-Fälle. Dies ist ein reproduzierbarer Simulationsnachweis, keine gemessene menschliche Spielzeit.

Die Domain-Regression umfasst 401 unterschiedliche native Fälle. Die 24 Little-Montana-Fälle wurden nach Verschärfung der Aufbauprüfung erneut vollständig ausgeführt und ersetzen im Gesamtbericht ihre früheren Ergebnisse; sie werden nicht zusätzlich gezählt. Alle zwölf Source-Assemblies kompilieren.

Die ausdrücklich gefilterte Assetprüfung umfasst **zwölf bestandene Stadt- und Erhaltungsfälle**, ohne Fehler oder übersprungene Tests. Nach der visuellen Anordnungskorrektur wurde sie frisch ausgeführt. Die ausdrückliche Wiederinstallation ließ 1.820 geprüfte Asset-, Szenen-, Katalog- und Konfigurationsdateien bytegenau gleich. Eine zusätzliche Byte- und YAML-Prüfung bestätigt zwei getrennte Korrekturketten: Vom ursprünglichen Stand zum endgültigen Bewohnerlayout wurden von 35 Erinnerungsinstanzen exakt 30 erlaubte Positionen geändert; von der ersten korrigierten Anordnung zum endgültigen Layout waren es sieben. Alle übrigen Werte und zum Zeitpunkt dieser Prüfung 611 andere authored Dateien blieben unverändert. Alle 160 PlayMode- und 69 Domain-Abhängigkeiten sind ebenfalls unverändert. `memory_grid_asset_diff.json`, `idempotence_receipt.json` und die nativen XML-Ergebnisse dokumentieren diese Nachweise.

Als zusätzliche Historie bleibt der ausführliche Assetassembly-Lauf mit 147 unterschiedlichen Fällen erhalten. Er wurde versehentlich ohne Klassenfilter gestartet und endete ursprünglich mit 146 bestandenen Fällen und einem Fehler wegen einer veralteten fest auf drei Stadtszenen begrenzten Testerwartung. Nach Umstellung dieser Erwartung auf den tatsächlichen Katalog bestand der gezielte native Wiederholungslauf. Der Bericht bewahrt dieses ursprüngliche Fehlerergebnis und die Korrekturhistorie; er bezeichnet die Historie nicht als frischen vollständigen Lauf nach der abschließenden Anordnungskorrektur.

Die gezielte native PlayMode-Regression umfasst **49 bestandene Fälle**, ohne Fehler oder übersprungene Tests. Sie prüft Stadtaufträge und Reisen, Szenenfehler, Bewohneroberflächen sowie die bestehenden Spielfluss-Verträge. Zusätzliche Fälle prüfen die Projektvoraussetzungen bei bereits ausreichender Bevölkerung und die Reisebeschriftung in allen fünf Sprachen. Vorherige Fehler und die folgenden Korrekturläufe bleiben im Nachweisbericht nachvollziehbar.

Nach der Kiefernkorrektur bestand die gefilterte native Assetprüfung erneut mit **12/12 Fällen**, einschließlich der um Kiefern erweiterten isolierten Erhaltungsprüfung. Die Wiederinstallation bestätigte erneut 1.820 Dateien bytegleich. Der separate Kiefernbeweis erlaubt ausschließlich die Positionswerte der zwei bestätigten Instanzen. Alle sieben endgültigen Bewohnerprefabs, 610 weitere Dateien außerhalb von Bewohnern und Ortsvisual sowie sämtliche Domain- und PlayMode-Eingaben blieben erhalten.

Der abschließende native Game-View-Lauf vom 7. Oktober 2026, 20:35:02 bis 20:40:11 UTC, endete regulär und erzeugte **77 frische PNGs**. Die getesteten Auflösungen sind 1.920 × 1.080, 1.366 × 768, 2.560 × 1.080 und 1.080 × 1.920; die Oberflächen wurden in allen fünf Sprachen geprüft. Die Layoutprüfung meldet null Überläufe. `gameview_receipt.json` bindet die Bilder an den endgültigen Fingerprint `4f90f14402f141247d407767483bcde2933cb6026c22709857b09cbac98f0508`.

Alle 77 endgültigen PNGs wurden anschließend tatsächlich angesehen: 13 Weltansichten, 16 Bewohner- und Erinnerungsansichten sowie 48 Oberflächenansichten. Der getrennte Coverageprüfer bestätigte genau 77 eindeutige aktuelle Dateipfade und SHA256-Werte; alte, fehlende oder doppelte Einträge werden abgelehnt. `visual_review.json` enthält die zusammengeführten Beobachtungen. Der abschließende Gesamtreport wurde ohne Freigabe unvollständiger Gates erfolgreich erzeugt und steht auf **`implementationStatus: verified` mit allen 13 erforderlichen Gates bestanden**. Persönliche visuelle Abnahme und Release bleiben ausdrücklich offen.
