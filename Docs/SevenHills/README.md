# Seven Hills

Seven Hills ist die dritte Stadt von Weedtopia. Der entlassene Bürgermeister hat mit dem Luxusprojekt **Seven Heights** Wohnraum und öffentliche Wege verdrängt. Die eigene Geschichte **„Sieben Hügel, ein Zuhause“** führt von der Wiederherstellung der Versorgung über zurückkehrende Nachbarn zur gemeinsamen Nutzung von Höfen, Terrassen und Aussichtspunkten.

Stadt-ID: `seven_hills`. Szene: `Assets/Weedtopia/Game/Scenes/SevenHills.unity`. Produktiver Inhalt: `Assets/Weedtopia/Game/Content/BurningHills.json`, Content-Version **4**.

## Freischaltung und Unternehmen

Die Freischaltung verwendet die **gleichzeitig vorhandenen Einwohner in Burning Hills und Big Rock City zusammen**:

`Residents(burning_hills) + Residents(big_rock_city) >= 5000`

Die Verteilung ist beliebig; auch 5.000 + 0 genügt. Es gibt keine zusätzliche Mindestzahl pro Stadt und keinen vorgeschalteten Abschluss der bisherigen Hauptgeschichten. Eine noch nicht übernommene Stadt trägt keine Einwohner zur Summe bei. Die Annahme des Big-Rock-City-Auftrags ist keine zusätzliche Voraussetzung, wenn Burning Hills allein die Schwelle erreicht.

`CityDefinition.unlockSources` benennt die beiden Quellen. `CityCampaignService` verwendet die aktuelle Simulationsberechnung und hält den höchsten tatsächlich gemeinsam beobachteten Stand in `CityState.charterResidentMilestone` fest. Dadurch bleibt das Angebot nach einem späteren Bevölkerungsrückgang und nach dem Laden verfügbar. Die unabhängigen historischen Höchststände der beiden Städte werden nicht addiert. Bereits vorhandene Spielstände über der Schwelle erhalten das Angebot bei der Aktualisierung der Kampagnenmeilensteine.

Rockos Regierungsauftrag erscheint in der gemeinsamen Stadtauswahl. Ein angebotener Auftrag kann direkt über die entsprechende Stadtzeile geöffnet werden. Die Annahme gewährt einmalig **2.500 Geld und zwei Teamkarten**. Wiederholte Annahmen zahlen nichts aus. Nach der Annahme bleibt die Stadt zugänglich; Reisen speichert vor dem Szenenwechsel. Fehlgeschlagenes Speichern setzt die Kampagnentransaktion zurück. Fortsetzen lädt die zuletzt besuchte Stadt.

Geld, Grinder, Teamkarten, Inventar und Team gehören zum gemeinsamen Unternehmen. Gebäude, Einwohner, Routen, Hauptaufträge, Tageserträge, Statistiken, Bewohnergeschichten und Erinnerungen gehören zur jeweiligen Stadt. Zugewiesene Mitarbeiter bleiben an ihrem Arbeitsplatz. Alle übernommenen Städte simulieren weiter, auch während Reisen und Offline-Fortschritt; eine gesperrte Stadt produziert nicht.

Der bestehende Big-Rock-City-Vertrag bleibt bei 1.000 einmal erreichten Einwohnern in Burning Hills. Spielstände der Content-Versionen 1–3 werden zuerst gegen ihren historischen Bestand validiert und anschließend additiv auf Version 4 ergänzt. Seven Hills erhält dabei 56 neue Plätze und einen eigenen Stadtzustand. Bestehende Gebäude, Arbeitskräfte, Lieferungen, Queststände, Inventar und Storyentscheidungen werden erhalten. Ein beschädigter ursprünglicher Bestand wird nicht durch neue Stadtobjekte verdeckt.

## Stadtgestaltung und Aufbau

Die feste isometrische Kamera, 2D-/2.5D-Gebäudedarstellung und bestehenden Produktionsketten bleiben erhalten. Seven Hills besitzt **56 Gebäudeplätze, 35 eigene Warenrouten und einen eigenen Katalog mit 251 Gebäudedarstellungen** für die elf sichtbaren Gebäudefamilien. Einschließlich freier Bauplätze werden dieselben zwölf Definitionsfamilien wie in den bisherigen Städten verwendet. Der Stadtkatalog verwendet die bestehenden Formen, Ausbaugrenzen, Kollisionsflächen und Gameplay-Anker mit eigenen Materialien und Farbgebung.

Die sieben gestalteten Hügel liegen in vier vertrauten Funktionsvierteln. Helle Fassaden, grüne und petrolfarbene Dächer, violette Blüten und begrünte Terrassen geben der Stadt ihre eigene Palette.

| Viertel | Hügel | Funktion |
| --- | --- | --- |
| Terrassengärten | Quellhügel, Morgenhügel | Anbau, Verarbeitung und Farmlager |
| Altstadtbogen | Treppenhügel, Markthügel | Stadtlager, Cafés und öffentliche Treffpunkte |
| Panoramaquartier | Aussichtshügel, Wolkenhügel | Wohnhäuser, Parks und Aussichtspunkte |
| Werkhof | Verbindungshügel | Butterwerk, Supermarkt, Bäckereien und Verbindung der Viertel |

18 eigene Hauptaufträge bilden sechs Erzählphasen. Die ersten zwölf Aufträge stellen Farm, Warenwege, Caféversorgung, Wohnraum, Parks und Industrie über die bestehenden Spielkommandos her. Die anschließenden Kapitel verwenden die Einwohnerzahl **von Seven Hills selbst**:

| Erzählphase | Fortschritt |
| --- | --- |
| Die Stadt hinter den Absperrungen | Ankunft und erste Versorgung |
| Hier wohnt noch jemand | Wohnraum und Morgenhof; Bevölkerungskapitel ab 250 |
| Die Rückkehrer | Ab 750 Einwohnern |
| Oben und unten | Ab 1.500 Einwohnern |
| Das schönste leere Gebäude | Öffentliche Luxusterrasse ab 3.000 Einwohnern |
| Sieben Hügel, ein Zuhause | Panoramaring und Abschluss ab 5.000 Einwohnern |

Eine höhere Einwohnerzahl überspringt keine Hauptaufträge. Nach dem letzten Auftrag beginnt freies Spiel. Bewohnergeschichten und Gemeinschaftsprojekte bleiben eigenständige, optionale Fortschrittsketten. Insbesondere verlangt der Hauptquestabschluss nicht, dass zuvor sämtliche persönlichen Geschichten oder der gemeinsame Panoramaring abgeschlossen wurden.

## Dauerhafte Bewohner und Projekte

| Bewohner-ID | Name | Erstes Auftreten | Weitere persönliche Kapitel |
| --- | --- | ---: | --- |
| `sh_mila` | Mila Morgen, Lehrerin | Bei Ankunft / 0 | 250 / 1.500 / 5.000 |
| `sh_tariq` | Tariq Trepp, Kurier | 50 | 750 / 2.000 / 5.000 |
| `sh_juna` | Juna Faden, Schneiderin | 150 | 750 / 2.500 / 5.000 |
| `sh_emil` | Emil Weitwinkel, Fotograf | 300 | 1.000 / 3.000 / 5.000 |
| `sh_safiya` | Safiya Platz, Vermittlerin | 500 | 1.250 / 3.000 / 5.000 |
| `sh_clara` | Clara Steig, Stadtführerin | 800 | 1.750 / 3.500 / 5.000 |
| `sh_anton` | Anton Wolk, Wetterbeobachter | 1.200 | 2.000 / 4.000 / 5.000 |

Jeder Bewohner besitzt vier persönliche Kapitel und drei rotierende Alltagsaufträge: insgesamt **28 Kapitel und 21 wiederkehrende Varianten**. Die dritte persönliche Geschichte besitzt jeweils zwei gespeicherte Entscheidungen. Safiyas dritte Geschichte entscheidet zwischen einem ruhigen Nachbarschaftshof und einer lebendigen Terrasse mit kleinen Ständen. Erinnerungen und die zugehörigen Varianten sind als statische Objekte authored und reagieren auf gespeicherte Abschlüsse und Entscheidungen.

Persönliche Kapitel bleiben in Reihenfolge. Maximal drei Bewohner- oder Gemeinschaftsaufträge pro Stadt sind gleichzeitig aktiv. Produktion, Verkäufe und Lieferungen zählen für Storyziele erst ab Annahme; die Annahme speichert die Ausgangswerte und Ziele. Gebäudeziele verwenden den Stadtbestand. Wiederkehrende Ziele und Geldbelohnungen skalieren mit dem gespeicherten Bevölkerungsschritt, warten nach Abschluss 300 Simulationssekunden und verfallen nicht.

| Gemeinschaftsprojekt | Einwohner in Seven Hills | Teilnehmer |
| --- | ---: | --- |
| `sh_joint_morning_courtyard` – Der Morgenhof gehört uns | 1.250 | Mila, Juna, Safiya |
| `sh_joint_connection_terrace` – Die Verbindungsterrasse | 3.000 | Tariq, Clara, Safiya, Emil |
| `sh_joint_panorama_ring` – Der Panoramaring | 5.000 | Alle sieben Bewohner |

Jedes Projekt verlangt zuerst den tatsächlich abgeschlossenen ersten persönlichen Auftrag aller Beteiligten. Der Panoramaring fordert danach 20 neue abgeschlossene Lieferungen. Bereits vor Annahme gelieferte Waren oder erfüllte Lieferungen bezahlen keinen neuen Storyauftrag.

## Assets, Lokalisierung und explizites Authoring

Die Assets liegen unter `Assets/Weedtopia/Production/SevenHills`. `SevenHillsAuthoring` erzeugt beziehungsweise ergänzt eigene Kataloge, Materialien und Prefabs und bindet sie in der neuen Szene. Die bestehende zweite Stadtszene liefert die aktuellen gemeinsamen UI-, Shop-, Bewegungs- und Lokalisierungsverträge. Die alten Stadtinstaller werden dabei nicht aufgerufen.

`Residents.prefab`, die sieben Bewohnerprefabs, `ResidentStories.prefab` und `Prefabs/SevenTerraces.prefab` enthalten die festen Figuren, Erinnerungen, Oberflächen und Landschaftsmotive. Diese Objekte sind in Prefabs und Szene serialisiert und in der Hierarchie sichtbar. Das Gameplay baut keine statische Stadtoberfläche zur Laufzeit nach. Das Reparaturverfahren ergänzt fehlende eigene Referenzen und Objekte; manuelle vorhandene Gestaltung wird nicht pauschal neu erstellt. Import, Spielstart und Domain-Reload lösen keine automatische Reparatur aus.

Unter `Art` liegen **neun neue PNG-Dateien**: sieben Bewohnerillustrationen, `terrace_landmark.png` und `town_sign.png`. Sie wurden mit dem eingebauten **ImageGen-Werkzeug** erzeugt. `art_prompts.json` dokumentiert Herkunft, Motive und Prompts. `asset_audit.json` prüft die neun Bilder einschließlich Transparenz, Abmessungen und SHA-256 sowie neue Metadaten, GUID-Eindeutigkeit und Referenzen. Die Rohbilder bleiben eigene Assets; vorhandene Gebäudegrafiken werden nicht durch einen neuen Asset-Import überschrieben.

**262 neue Textschlüssel** sind vollständig in `de`, `en`, `es`, `fr` und `ru` vorhanden. Dazu gehören Regierungseinladung, gemeinsame Freischaltschwelle, Stadtauswahl, Viertel, Hauptaufträge, gesprochene Dialogvarianten, Bewohnerbiografien, Kapitel, Entscheidungen, Erinnerungen und Zieltexte. Vorherige Texte und Übersetzungen bleiben erhalten. Die Schlüssel `campaign.combined_locked` und `campaign.view_offer` sind Teil der gemeinsamen Kampagnenoberfläche.

`author_seven_hills.py` ist explizit, additiv und idempotent. Vorhandene IDs oder Texte mit abweichendem Inhalt werden nicht überschrieben. `validate_seven_hills_content.py` vergleicht alte Inhalte und Tunings mit der Baseline und rekonstruiert nach Entfernen ausschließlich der ergänzten Textschlüssel die ursprünglichen fünf Sprachdateien; ihre SHA-256-Werte müssen mit der Baseline übereinstimmen.

## Installation, Reparatur und Prüfung

Im sicheren Editorzustand lautet das Menü **Weedtopia / Cities / Install or Repair Seven Hills**. Es arbeitet über `SevenHillsAuthoring.Apply`. Ein frischer erfolgreicher `authoring_receipt.json` bestätigt die Anwendung. Der Batchwrapper verweigert den Start bei einem vorhandenen Unity-Prozess oder Lock; ein vorhandener Editor wird nicht geschlossen. Batchprozesse laufen verborgen und erhalten die gesicherten Projekteinstellungen.

Die folgenden Befehle werden vom Repositoryverzeichnis aus ausgeführt. Unity-Läufe müssen nacheinander und ohne vorhandenen Editor oder Lock erfolgen:

```powershell
python Tools/Implementation/author_seven_hills.py
python Tools/Implementation/validate_seven_hills_content.py
python Tools/Implementation/compile_sources.py
& Tools/Implementation/Run-SevenHills.ps1 -Mode Install -Label SevenHillsRepair
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.EditModeTests -Label SevenHillsDomain
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.AssetTests -Label SevenHillsAssets
& Tools/Implementation/Run-UnityValidation.ps1 -Mode PlayMode -Assembly Weedtopia.Game.PlayModeTests -Label SevenHillsPlay
& Tools/Implementation/Run-SevenHills.ps1 -Mode Capture -Label SevenHillsGameViews
python Tools/Implementation/seven_hills_preservation.py verify
python Tools/Implementation/audit_seven_hills_assets.py
python Tools/Implementation/seven_hills_visual_review.py
python Tools/Implementation/seven_hills_report.py
```

`compile_sources.py` prüft ausschließlich die Quellcode-Assemblies gegen die installierten Unity-Referenzen. Unity-Import, EditMode, PlayMode und gerenderte Game Views sind eigene Prüfungen. Die vollständigen Läufe prüfen auch die bestehenden Städte. Gezielt lassen sich `SevenHillsCampaignTests`, `SevenHillsContentTests` und `SevenHillsAssetTests` über `-TestFilter` ausführen.

Aktuelle Ergebnisse werden in **`validation_summary.json`** zusammengeführt. Dazu gehören die zugehörigen frischen XML-, JSON- und Logdateien; ein alter erfolgreicher Beleg ersetzt keinen aktuellen Lauf. Die laufende Integrationsprüfung darf erst nach einem frischen Abschluss als bestanden ausgewiesen werden.

Der Abschlussstand vom **7. Oktober 2026** ist **`implementationStatus: verified`**: Alle 13 technischen Gates sind bestanden. Er umfasst zwölf fehlerfrei kompilierte Assemblies, 377 bestandene Domainfälle mit dokumentierter gezielter Wiederholung, den erneuten vollständigen Assetlauf mit 142/142 Fällen und den erneuten vollständigen PlayMode-Lauf mit 125/125 Fällen. Die aktuelle Bildfolge enthält **72 tatsächlich gerenderte und als Bilder angesehene Ansichten** in 1920×1080, 1366×768, 2560×1080 und 1080×1920 sowie Texte in allen fünf Sprachen. Angeforderte, gerenderte und gespeicherte Maße stimmen in jeder Aufnahme überein; die Textüberlaufprüfung meldet null Fehler. `visual_review.json` enthält den vollständigen Bildabgleich mit aktuellem Fingerprint und 72 SHA-256-Werten.

Der finale Erhaltungsabgleich kontrolliert **4.144 gesicherte Dateien**, ohne unerwartete Änderungen oder entfernte Dateien. Die 39 vorgesehenen Änderungen betreffen ausschließlich Inhalte, gemeinsame Integrationspunkte, Tests und die Build-Szenenliste; beide bestehenden Stadtszenen bleiben bytegleich. Der Assetaudit prüft 617 eigene Dateien ohne fehlende Metadaten, doppelte eigene GUIDs oder unaufgelöste Referenzen. Das Angebot, die Annahme und die erste Reise verwenden im Capture die vorhandenen UI-Schaltflächen.

`SevenHillsContentTests.FoundationReachesLocalSupplyUsingPublicCommandsWithoutRecoveryOrPremium` durchläuft die **ersten zwölf Hauptaufträge über öffentliche Spielkommandos und tatsächliche Simulation**. Die angenommene Regierungseinladung ist die Testvorbedingung; der Aufbau injiziert keine Ressourcen, Arbeiter, Gebäude- oder Questfortschritte, nutzt keine Premiumausgaben und kein Recovery-Geld. Der bestandene native Test erreicht nach **1.856 Simulationssekunden** den Auftrag `sh_morning_courtyard`; Butter wurde hergestellt und Gebäck verkauft. `foundation_simulation.json` dokumentiert 50 anfängliche Grinder, einen regulären Tagesertrags-Grinder, 51 finale Grinder, **null ausgegebene Grinder und keinen Recovery-Zufluss**. Die Belohnungshistorie wird dabei vollständig gegen den tatsächlichen Zufluss abgeglichen.

Der vollständige Domainlauf prüfte 377 Fälle. Die anfänglichen vier Fehler betrafen drei funktionale Offline-Watchdogs und die fehlende Berücksichtigung des Tagesertrags im neuen Foundation-Test. Nach einer ausschließlich auf Testcode begrenzten Korrektur bestanden alle 27 Fälle der beiden betroffenen Testgruppen erneut. Die Summary erhält beide Läufe und verwendet das aktuelle XML-Ergebnis jedes tatsächlich wiederholten Testfalls. Das funktionale Wartezeitlimit bleibt begrenzt und ist keine Performance-Abnahme; die eigentliche Offline-Simulation wurde nicht verändert.

Die native Assetprüfung besteht nach den visuellen Korrekturen erneut mit **142/142 Fällen**, einschließlich der bytegleichen wiederholten Seven-Hills-Installation mit 610 kontrollierten Dateien. Die vollständige neue PlayMode-Prüfung besteht mit **125/125 Fällen**, ohne Fehler und ohne übersprungene Fälle. Sie ergänzt echte Sprachwechsel durch alle fünf Sprachen und Rockos gewöhnliche Ankunftskonversation ohne Stadtangebot. Der frühere Lauf mit 123 Fällen sowie die gezielte erfolgreiche Wiederholung der vier korrigierten Szenenfehler-/Startup-Fixtures bleiben nachvollziehbar erhalten. Die Startup-Benchmarks verwenden jetzt ausdrücklich 168 Gebäude und 177 besetzte Arbeitsplätze sowie optional 10.000 freie Inventarmitarbeiter. Ihr neuer Scope und ihre Messwerte stehen unter `Benchmarks`; sie sind kein Zeitvergleich mit einer kleineren historischen Welt.

Die erste vollständige Bildprüfung fand genau einen überlaufenden neuen Belohnungstitel. `caption_fix.json` und die sechs Originaldateien unter `CaptionFix` belegen, dass ausschließlich der Wert von `sh.joint.panorama_ring.memory` in den fünf Sprachen und im eingebetteten deutschen Text gekürzt wurde. Die kanonischen JSON-Dokumente sind bei ausgeblendetem einzelnen Beschriftungswert identisch. Die unveränderten Modell-/Assetprüfungen bleiben getrennt von der frischen Prüfung dieser Darstellung; die Summary verlangt dafür zusätzlich `nativeCaptionFreshProof`, vollständige neue Game Views, Sprach-/Inhaltsprüfung und eine visuelle Prüfung der aktuellen Bilddateien.

Andere Storytests verwenden ausdrücklich vorbereitete Zustände, um alle 28 Kapitel, drei Projekte, 21 wiederkehrenden Varianten, Annahme-Ausgangswerte, Entscheidungen, doppelte Auszahlung und Save-Roundtrips zu prüfen. Kampagnentests verwenden echte Einwohnerberechnungen für die Grenze 4.999 / 5.000, verschiedene Verteilungen, dauerhafte Freischaltung und alte Spielstände. Diese Belege haben unterschiedliche Aussagekraft und werden getrennt geführt.

`SevenHillsEvidence` erstellt echte gerenderte PlayMode-Game-Views mit **isolierten Diagnosespielständen** und tatsächlichen Szenenreisen. Die Szenen mit ausgewachsenen Häusern, vielen Einwohnern und abgeschlossenen Geschichten sind **Wachstums-Fixtures**: Sie belegen Darstellung und Oberflächen, keine menschlich gespielte Aufbauzeit. Die Bildfolge prüft zusätzlich tatsächliche UI-Aktionen bei Einladung, Ankunft, Questfokus, Dialog, Bau und Storyentscheidung. `gameview_receipt.json`, `ui_overflow.json` und die Bilddateien dokumentieren diesen Lauf.

Die Bildprüfung betrachtet die tatsächlichen PNG-Pixel. Ihre Teilberichte unter `VisualReview` übernehmen die exakten Pfade aus der aktuellen Capture-Receipt, deren Fingerprint und den SHA-256 jedes angesehenen Bilds. `seven_hills_visual_review.py` führt diese Berichte zusammen und verweigert fehlende, doppelte oder nachträglich veränderte Bilder; das Script selbst betrachtet keine Bilder. Nach einem neuen Capture ist eine neue tatsächliche Bildprüfung nötig, bevor dieser Zusammenführungsschritt einen belastbaren Abschluss ergibt.

Die Prüfung des vorherigen 66-Bilder-Laufs fand neben dem inzwischen gekürzten Belohnungstitel eine unpassende Position Emils vor einem hohen Wohnhaus, ein nicht aktualisiertes festes Storylabel beim Sprachwechsel und Fehler in der Capturesequenz. Der Stand einschließlich der tatsächlichen Bildberichte ist unter `Validation/20261007_074131_Capture_FinalGameViews_review` archiviert. Die gezielten Korrekturen versetzen ausschließlich Emil auf die vorhandene Wohnstraße, aktualisieren die festen Storylabels ohne neue Listener und führen Rockos Figurenansprache und Rathausaktion auf dieselbe Kampagnenroute. Die neue Bildfolge enthält zusätzlich die vollständig sichtbaren unteren Reisezeilen in vier Auflösungen und schließt die Belohnungsanzeige vor den beiden Weltansichten von Milas Erinnerungen. Ihre erneute Verifikation wird in den aktuellen Receipts geführt.

Die feste Größe wird in der tatsächlich aktiven Game-View-Gruppe gesetzt, einschließlich Android. Vor jeder Aufnahme müssen `Screen.width` und `Screen.height` passen; anschließend werden die PNG-Maße geprüft. `captureDimensions` dokumentiert angeforderte, gerenderte und gespeicherte Maße pro Bild. Der erste Rathausfokus richtet die Kamera auf Rocko, während dessen Gebäudeinspektor bis zur Freischaltung des Stadtviertels geschlossen bleibt. Das Gespräch mit dem Character ist bereits möglich; die bestehende gemeinsame Gebäudeselektion kann beim Fokus kurz eine Sperrmeldung anzeigen.

Die Bildberichte dokumentieren zwei kleinere gestalterische beziehungsweise geerbte Textbefunde: Der anfangs geschlossene Quellhügel wird nur durch eine kleine Verfallsmarkierung von seiner späteren gepflegten Terrasse unterschieden. Bestehende Reiseübersetzungen in vier Fremdsprachen und die gemeinsame Formulierung über zwei Städte sind unverändert aus der Baseline übernommen; die neuen Seven-Hills-Texte sind vollständig lokalisiert. Diese Beobachtungen bleiben bei der visuellen Nutzerabnahme sichtbar und werden nicht als fehlende technische Prüfergebnisse ausgeblendet.

Direkte visuelle Nutzerabnahme, physische Touchbedienung, Geräteaudio, ein menschlicher Gesamtdurchlauf und Android-/WebGL-/Player-Builds bleiben gesonderte Gates. PNGs, automatisierte UI-Eingaben, Testresultate oder ein Prozess-Exit schließen diese Gates nicht. Die technische Integration allein erklärt das Spiel nicht zur abgenommenen Produktionsversion.
