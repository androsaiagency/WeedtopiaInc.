# Cassa Vadela – die Stadt der geteilten Brunnen

Cassa Vadela erweitert die Kampagne als fünfte Stadt. Das Mandat wird ab **50.000 Einwohnern zusammen in Burning Hills, Big Rock City, Seven Hills und Little Montana** verfügbar. Die fünfte Stadt zählt nicht in ihren eigenen Auslöser. Die bestehende Kampagnenlogik bewertet eine gleichzeitig gemessene Summe und bewahrt ein bereits qualifiziertes Mandat; einzelne historische Höchststände werden nicht nachträglich addiert. Die Annahme gewährt einmalig **5.000 Geld und vier Teamkarten**.

Die mediterrane Terrassenstadt lebte vom gemeinsamen Markt, kleinen Werkhöfen und einem alten Wassernetz. Nach dessen Ausfall gingen Versorgung, Arbeit und Begegnungen verloren. Ines bewahrte die Reparaturzettel, Mateo sein Saatgut, Lucia das Familienbuch und Sofia die Muster ihrer Lehrmeisterin. Jeder hielt etwas fest; niemand konnte damit allein die Stadt erhalten. Der Wiederaufbau beginnt mit einer verlässlichen Farm und Warenwegen. Danach werden die alten Orte wieder Teil des Alltags, und die Bewohner entscheiden selbst, welche Form ihre Zusammenarbeit bekommt.

Die Geschichte behandelt das Wassernetz als Teil von Vergangenheit, Gespräch und sichtbaren Erinnerungsorten. Die spielbaren Aufgaben verwenden die vorhandenen Gebäude-, Personal-, Waren-, Liefer-, Wohnraum- und Ertragsverträge. Es wurde keine zusätzliche Wasserökonomie eingeführt.

## Stadt und Orte

| Viertel | Bestehende Spielzone | Zweck in der Geschichte |
| --- | --- | --- |
| Oliventerrassen | Farm | Mateos gemeinsamer Garten, Ines’ Vorratstisch, bestehende Anbau- und Verarbeitungskette |
| Brunnenmarkt | City | Lucias offene Tische, Elios Zuhörkreis, Warenlager und bestehende Cafés |
| Sonnenhang | Settlement | Wohnraum, offene Parks, Rastpunkte und gemeinsamer Terrassenweg |
| Alter Keramikhof | Industry | Rafaels Werkbank, Sofias Muster, Amaras Herkunftsnotizen und vorhandene Butter-/Markt-/Bäckereikette |

Der Szenenzielpfad ist `Assets/Weedtopia/Game/Scenes/CassaVadela.unity`. Stadt-ID: `cassa_vadela`; Gebäude- und Routenpräfix: `cv_`; Katalog- und Einführungsversion: **6**. Alle 56 Instanzen und 35 stadtinternen Routen übernehmen die etablierten Familien und Warenverträge. Szene und sichtbare Story-Orte werden über die expliziten Editor-Werkzeuge des Projekts erstellt und repariert; der Content-Generator selbst startet Unity nicht.

## Acht dauerhafte Einwohner

Die Zahlen in der Tabelle beziehen sich auf **die Einwohner von Cassa Vadela**, nicht auf die Freischaltsumme der ersten vier Städte. Die Reihenfolge der persönlichen Kapitel bleibt verbindlich, und jede Geschichte verlangt neue Aktivität nach Annahme. Alle acht Personen bleiben nach ihren vier Kapiteln in der Stadt.

| Person und ID | Erscheint | Kapitel 1 / 2 / 3 / 4 | Eigener Konflikt und verbundene Geschichte |
| --- | ---: | --- | --- |
| Ines Fontana – `cv_ines` | 0 | 0 / 600 / 3.000 / 8.000 | Verantwortung teilen: Rafael übernimmt ihre Notizen; Lucia hält einen Stuhl frei. Am Ende darf sie ihre Schlüssel ablegen und Elio zuhören. |
| Mateo Oliva – `cv_mateo` | 75 | 75 / 900 / 3.500 / 8.500 | Den Gemeinschaftsgarten öffnen: Sofia beschriftet, Lucia versorgt, Nico bringt Saatgut zurück. Pflege wird Einladung statt Zugangshürde. |
| Lucia Bellini – `cv_lucia` | 200 | 200 / 1.200 / 4.000 / 9.000 | Familienrezept und eigene Stimme: Sie entdeckt die Änderungen ihrer Großmutter und setzt mit Sofias Hilfe den eigenen Namen auf die Karte. |
| Rafael Costa – `cv_rafael` | 400 | 400 / 1.600 / 4.500 / 9.500 | Reparieren und lehren: Sofia bekommt einen zweiten Platz an der Werkbank. Rafael lernt machbare Zusagen und einen pünktlichen Feierabend. |
| Sofia Marés – `cv_sofia` | 700 | 700 / 2.000 / 5.000 / 10.000 | Tradition und offene Werkstatt: Mateo bringt einen schiefen Topf vieler Hände, Amara benennt die Lehrmeisterin. Sofia lässt fremde Spuren stehen. |
| Elio Varela – `cv_elio` | 1.100 | 1.100 / 2.500 / 5.500 / 10.500 | Zuhören statt lauter spielen: Nico zeigt Ruhezeiten, Lucia erzählt zuerst. Beim Terrassenabend hört Elio ein ganzes Lied der Nachbarn an. |
| Amara Sol – `cv_amara` | 1.600 | 1.600 / 3.000 / 6.500 / 11.500 | Ehrliche gemeinsame Erinnerung: Ines und Nico erinnern einen Tag verschieden. Die Chronik nennt Widersprüche und lässt Raum für fehlende Stimmen. |
| Nico Venturi – `cv_nico` | 2.200 | 2.200 / 3.600 / 7.000 / 12.000 | Verlässliche Wege und Grenzen: Elio trägt Rastpunkte ein, Ines erklärt ein rechtzeitiges Nein. Nico kommt zum Fest als Gast. |

Jede Person hat drei wiederkehrende Auftragsvarianten: einen Morgen am Brunnenmarkt mit Blütenverkäufen im Coffeeshop, eine Versorgungsrunde mit gelieferten Waren und einen Abend mit Gebäckverkäufen. Die Basismengen sind 30, 60 und 40. Die bestehende `VipStoryRules.RecurringScale` skaliert frische Zähler mit lokaler Bevölkerung; die vorhandene Obergrenze bleibt erhalten. Nach einem Abschluss gelten 300 Sekunden Pause. Es gibt kein Ablaufdatum und keine erneute einmalige Erinnerung für dieselbe Alltagsrunde.

## Gespeicherte Entscheidungen

Jedes dritte persönliche Kapitel besitzt zwei eigene, in allen fünf Sprachen verfasste Entscheidungen. Keine Wahl löscht die Person, ihre spätere Geschichte oder die drei wiederkehrenden Varianten. Die Wahl-ID wird über den bestehenden Story-Service gespeichert. Das vierte Kapitel greift die Entscheidung als weiterhin bestehende Form des Ortes auf.

| Bewohner | Wahl-ID A | Wahl-ID B | Dauerhafte Folge |
| --- | --- | --- | --- |
| Ines | `care_rota` | `open_ledger` | Feste Pflege- und Pausenzeiten oder ein öffentliches Wartungsbuch mit Fragen, Zuständigkeiten und offenen Arbeiten |
| Mateo | `seed_library` | `shared_tables` | Beschriftete Saatgutbibliothek oder offene Gartentische |
| Lucia | `family_recipe` | `own_recipe` | Kommentierte Familienkarte mit eigenen Ergänzungen oder Lucias selbst benannte Rezepttafel |
| Rafael | `repair` | `teach` | Gemeinsames Ersatzteilregal oder offene Lerntafel; beide Wege planen machbare Werkstattzeiten |
| Sofia | `tradition` | `open_workshop` | Mustergalerie mit Herkunftsgeschichten oder freie Keramikwerkbank |
| Elio | `listening_stage` | `quiet_sessions` | Wechselnde Stimmen im Zuhörkreis oder ruhige Hauskonzerte mit vereinbarten Zeiten |
| Amara | `testimony` | `shared_mosaic` | Einzelne Zeugnisse samt Widersprüchen oder ein Mosaik mit sichtbaren Quellen und freien Feldern |
| Nico | `fixed_rounds` | `shared_routes` | Feste Abholzeiten oder Karten für Vertretungen; beide erhalten seine Pausen |

## Drei Gemeinschaftsprojekte

Alle Voraussetzungen beziehen sich auf bereits abgeschlossene erste Kapitel der beteiligten Bewohner. Ein Projekt ist ein eigener Story-Auftrag. Die Hauptquestkette verlangt seine Annahme oder seinen Abschluss nicht und kann weiterlaufen, während die persönlichen Geschichten wachsen.

| Projekt-ID | Lokale Einwohner | Beteiligte | Neue Aktivität nach Annahme |
| --- | ---: | --- | --- |
| `cv_joint_fountain_market` | 1.800 | Ines, Mateo, Lucia | 120 Blütenverkäufe im Coffeeshop (`CoffeeSold`, Kind 5) |
| `cv_joint_open_ceramic_yard` | 4.500 | Rafael, Sofia, Amara | 240 gelieferte Waren (`DeliveredGoods`, Kind 8) |
| `cv_joint_terrace_walk` | 9.000 | Alle acht | 30 abgeschlossene Lieferungen (`Deliveries`, Kind 9) |

Der Brunnenmarkt verbindet Vorräte, Pflanzen und Lucias Tisch. Der Keramikhof verbindet Werkzeug, Muster und ihre Urheber. Der Terrassenweg verbindet Markt, Garten, Hof und Rastpunkte; Pausen und Ruhezeiten gehören zum Plan. Das Fest endet, der Alltag der Bewohner geht weiter.

## Hauptquests und Produktionsweg

Die 18 Hauptquests übernehmen die bestehenden öffentlichen Bedingungen und Belohnungen der erprobten Seven-Hills-Grundkette, mit lokalen Instanzzielen, eigenen Texten und späteren Bevölkerungsschwellen. Keine Aufgabe verlangt eine neue Gebäudeart oder eine externe Stadtinstanz.

| Reihenfolge | ID | Auslöser / Aufgabe |
| ---: | --- | --- |
| 1 | `cv_welcome` | Mit Rocko sprechen; Mandat und Geschichte kennenlernen |
| 2 | `cv_first_garden` | Erstes Feld reparieren |
| 3 | `cv_first_harvest` | Erste Rohware ernten; Farmhelfer erhalten |
| 4 | `cv_processing` | Helfer zuteilen, Jaro sprechen, Verarbeitung reparieren |
| 5 | `cv_farm_store` | Verarbeitung besetzen, Nia sprechen, Farmlager reparieren |
| 6 | `cv_farm_routes` | Lagerleitung und Fahrer besetzen, Lieferung abschließen, Farm automatisieren; Brunnenmarkt öffnen |
| 7 | `cv_market_reopen` | Stadtlager und erstes Café reparieren |
| 8 | `cv_first_meeting` | Stadtlager und Café besetzen, erste Blüten verkaufen; Sonnenhang öffnen |
| 9 | `cv_homes` | Wohnhaus und Park errichten |
| 10 | `cv_everyday` | Personal zuteilen, Tagesertrag sammeln, VIP-Auftrag abschließen und Belohnung abholen; Keramikhof öffnen |
| 11 | `cv_workyard` | Butterwerk, Supermarkt und erste Bäckerei reparieren |
| 12 | `cv_local_supply` | Die drei Betriebe automatisieren, Butter produzieren und Gebäck verkaufen |
| 13 | `cv_fountain_market` | 400 lokale Einwohner; Gemeinschaftsprojekt ab 1.800 ankündigen |
| 14 | `cv_returning` | 1.100 lokale Einwohner; Elio und neue Stimmen |
| 15 | `cv_shared_routes` | 2.200 lokale Einwohner; Nico und Amaras unterschiedliche Wege |
| 16 | `cv_open_ceramic_yard` | 4.500 lokale Einwohner; offener Werkhof |
| 17 | `cv_terrace_walk` | 9.000 lokale Einwohner; gemeinsamen Weg ankündigen |
| 18 | `cv_home` | 9.000 lokale Einwohner, Supermarktertrag abholen, vorhandene Industrieautomation; freier Ausbau |

## Lokalisierung und Erhaltung

`Tools/Implementation/author_cassa_vadela.py` ist das explizite, additive Authoring-Werkzeug. Es verweigert Konflikte mit bestehenden IDs und Textschlüsseln und prüft alle vorgeschlagenen Daten vor dem ersten Schreiben. Es fügt 299 Schlüssel mit manuell verfassten deutschen, englischen, spanischen, französischen und russischen Fassungen hinzu. Namen bleiben als Eigennamen in allen fünf Sprachen gleich. Platzhalter und alle referenzierten Textschlüssel werden geprüft.

Der erste erfolgreiche Lauf erhielt sämtliche bestehenden Einträge der vier Städte und sämtliche bisherigen Lokalisierungseinträge semantisch unverändert. Der zweite Lauf schrieb weder Content noch Lokalisierungen erneut: alle sechs Dateien waren bytegleich. Ein unabhängiger Vergleich mit `Preservation/Before/Assets/Weedtopia/Game/Content/BurningHills.json` bestätigte alle ursprünglichen Listenpräfixe und übrigen Felder; nur der freigegebene Versionsanstieg 5 → 6 ist ausgenommen. Belege und SHA-256-Werte stehen in `content_validation.json`.

Die Content-Prüfung bestätigt Authoring, Textvollständigkeit, lokale Referenzen und Erhaltung. Compiler-, Unity-Import-, EditMode-, PlayMode-, Game-View- und Gerätebelege werden gesondert geführt. Ein erfolgreicher Generatorlauf allein bestätigt keine visuelle Abnahme und keinen vollständigen Spieltest.


## Semantische Nachprüfung

Die Nachprüfung gegen die tatsächliche Simulation erkannte und korrigierte eine irreführende Verkaufsanweisung: Der Coffeeshop verkauft veredelte Blüten (`Resource.Refined`), keine Kaffee-Tassen. Fünf Cassa-spezifische Schlüssel wurden in allen fünf Sprachen berichtigt: `cv.quest.first_meeting.body`, die zugehörige `.dialogue`, `cv.story.ines.chapter.2.body`, `cv.story.lucia.chapter.1.body` und `cv.goal.coffee`. Tassen und Stühle bleiben als Atmosphäre in den Geschichten. Der Generator akzeptiert für diese Revision ausschließlich die zuvor geprüften alten Textliterale; unbekannte Änderungen verweigert er weiterhin. Die vollständigen Vorher-/Nachher-Texte stehen in `reviewedTextRevision` des Content-Belegs. Gebäude, Bedingungen, Mengen und Belohnungen blieben unverändert.

`narrativeAudit` enthält die Prüfung von Freischaltung und Aufbauhilfe, tatsächlichen Warenketten, neuen Zählern seit Auftragsannahme, Projektmengen, Bewohnerverbindungen und gespeicherten Ortsentscheidungen. Die spanischen, französischen und russischen Fassungen wurden auf grobe Bedeutungstreue geprüft. Das ist keine Behauptung einer muttersprachlichen menschlichen Endredaktion.

Die Prüfung trennte außerdem den generischen VIP-Zähler von persönlichen Bewohnerkapiteln: `cv.quest.everyday.body` und die zugehörige `.dialogue` verlangen jetzt ausdrücklich einen VIP-Auftrag mit abgeholter Belohnung. Ines’ persönliche Liefergeschichte wird als parallele Geschichte benannt. Ein Abschluss im Story-Service erhöht den generischen `vipCompleted`-Zähler nicht. Auch diese zwei Schlüssel sind mit exakten geprüften Vorher-Texten abgesichert; der Spielvertrag wurde unverändert erhalten.

## Integration und explizite Werkzeuge

Im laufenden Spiel führt **Städte** unter dem aktuellen Stadtnamen zur Kampagnenübersicht. Nach dem gemeinsam erreichten 50.000er-Meilenstein zeigt Rocko dort das neue Mandat. Nach dessen einmaliger Annahme ist **Reisen** in der Cassa-Vadela-Zeile verfügbar. Die lokale Hauptquest startet mit Rockos Ankunftsgespräch; Bewohnergeschichten und Gemeinschaftsprojekte sind über die vorhandene Geschichtenansicht erreichbar. Ein späteres Fortsetzen lädt die zuletzt aktive Stadt samt ihren gespeicherten Entscheidungen und Projekten.

Die Szene, Gebäudekataloge, acht Bewohner, Erinnerungsobjekte, drei Gemeinschaftsorte und alle UI-Zeilen sind als Assets beziehungsweise Prefabs gespeichert. Die statischen Stadtobjekte werden nicht beim Spielstart im Code erzeugt. Die feste isometrische Kamera, Gebäudefüße, Klickbereiche, Produktionsanker und Warenverträge bleiben erhalten. Cassa besitzt eigene warme Sandstein-/Terrakottamaterialien und 251 Varianten-Wrapper für die vorhandenen Gebäudefamilien. Zwölf neu erzeugte transparente Bilder liefern die Bewohner, das Stadtschild, den Terrassengarten und beide Brunnenzustände; die Prompts und Herkunft sind in `art_prompts.json` gespeichert.

Der Brunnen zeigt vor dem Gemeinschaftsprojekt ausschließlich trockene Becken. Auch bei gewachsener Bevölkerung bleibt das Wasser aus. Erst der gespeicherte Abschluss von `cv_joint_fountain_market` aktiviert den restaurierten, fließenden Brunnen. Der gemeinsame Präsentationsbaustein behält sein bisheriges Verhalten für die vier vorhandenen Städte; nur dieser Cassa-Ort ersetzt den Zwischenzustand nach Fertigstellung. Die zusätzliche Routenprüfung verschob ausschließlich zwei noch unveränderte Cassa-Dekorationsanker aus den bestehenden Transportwegen. Eigene manuelle Positionen werden dabei nicht überschrieben.

`CassaVadelaCampaignAuthoring` erweitert das gemeinsame Stadtmenü um die fünfte Reisezeile. Version-5-Spielstände werden nach der bisherigen Validierung um Cassa ergänzt. Gemeinsames Geld, Teamkarten und Sammlung bleiben gemeinsam; Gebäude, Warenwege, persönliche Geschichten, Entscheidungen und Projekte bleiben je Stadt getrennt. Die bereits aktive Stadt und vorhandene Mitarbeiterzuweisungen bleiben erhalten.

Die Unity-Menüaktion lautet **Weedtopia → Cities → Install or Repair Cassa Vadela**. Sie ist ausdrücklich auszulösen und ergänzt beziehungsweise repariert nur zugeordnete Cassa-Assets oder fehlende Referenzen. Es gibt keine automatische Installation bei Import, Start oder Domain-Reload. Wiederholtes Ausführen erhält manuelle Namen, Positionen, fremde Karten und zugewiesene Bilder.

Aus der Projektwurzel sind die Nachweise wiederholbar:

```powershell
python Tools/Implementation/author_cassa_vadela.py
& Tools/Implementation/Run-CassaVadela.ps1 -Mode Install -Label FinalStages
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.EditModeTests -Label CassaVadelaDomain
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.AssetTests -TestFilter 'BigRockCityAssetTests;SevenHillsAssetTests;LittleMontanaAssetTests;CassaVadelaAssetTests' -Label CassaVadelaAssets
& Tools/Implementation/Run-UnityValidation.ps1 -Mode PlayMode -Assembly Weedtopia.Game.PlayModeTests -TestFilter 'CityCampaignUiTests;CitySceneFailureUiTests;VipStoryUiTests;GameFlowTests;CassaVadelaFountainPlayModeTests' -Label CassaVadelaPlay
& Tools/Implementation/Run-CassaVadela.ps1 -Mode Capture -Label FinalGameViews
python Tools/Implementation/cassa_vadela_preservation.py
python Tools/Implementation/cassa_vadela_asset_references.py
python Tools/Implementation/cassa_vadela_visual_manifest.py
python Tools/Implementation/cassa_vadela_report.py
```

Die Unity-Runner starten nur einen eigenen, versteckten Editor, wenn kein Unity-Prozess und keine Projektsperre vorhanden sind. Spielansichten verwenden `capture_save.json` samt eigener Score-Datei. Die sichtbaren späteren Ausbaustufen sind ausdrücklich diagnostische Wachstumsfixtures; sie ersetzen keinen vollständigen menschlichen Kampagnendurchlauf. Der separate Aufbau-Nachweis verwendet öffentliche Spielbefehle mit dem tatsächlichen Startbudget.

Nach einem neuen Capture müssen dessen Bilder erneut tatsächlich betrachtet und die drei Pixelprüfungsberichte mit den aktuellen SHA-256-Werten versehen werden. Das Manifest-Werkzeug prüft ausschließlich diese ausdrücklich aufgezeichneten Prüfungen; es ersetzt keine Sichtprüfung. Es verweigert veraltete Hashes/Fingerprints, fehlende Aufnahmen, doppelte Einträge und fehlgeschlagene Befunde.

## Verifizierter lokaler Projektstand vom 8. Oktober 2026

Die explizite Installation wurde mit Unity 6000.3.9f1 ausgeführt. Der letzte Installationsbeleg ist `Validation/20261008_072341_Install_FinalStages.json`; die abschließenden 80 Spielansichten stammen aus `Validation/20261008_073301_Capture_FinalGameViews.json`. Die Bildgrößen wurden sowohl am gerenderten Frame als auch an der gespeicherten PNG geprüft. `ui_overflow.json` bestätigt die vier Größen 1920×1080, 1366×768, 2560×1080 und 1080×1920 ohne gemeldeten Layoutüberlauf.

| Prüfung | Ergebnis und Bedeutung |
| --- | --- |
| Source/Compiler | 12 Assemblies bestanden; der Abschlussbericht prüft die gespeicherten Quellhashes gegen die heutigen Dateien. |
| Native EditMode: Domain | 429 unterschiedliche Prüfungen bestanden, einschließlich der gezielt wiederholten 28 Content-Prüfungen. |
| Native EditMode: Assets | Letzter Lauf: 22 von 22 bestanden, einschließlich der bisherigen Stadt-Asset-Prüfungen, Cassa-Referenzen, expliziter Wiederholbarkeit und Erhaltung manueller Änderungen. |
| Native PlayMode | 52 unterschiedliche Prüfungen bestanden nach gezielten Wiederholungen. Reisen, Fortsetzen, Mandat und einmalige Aufbauhilfe sowie die gespeicherten Brunnenzustände wurden nach der letzten visuellen Reparatur erneut geprüft. |
| Tatsächliche Pixelprüfung | Alle 80 finalen Spielansichten geprüft: 19 Kampagnen-/Ankunfts-/Projektbilder, 26 UI-/Sprachbilder und 35 Welt-/Bewohnerbilder. Der Gesamtbeleg enthält genau die aktuelle Dateimenge und deren aktuelle Hashes, ohne fehlende oder doppelte Einträge. Die Weltprüfung kombiniert vollständige Contact-Sheets mit gezielten Ansichten in Originalauflösung. |
| Aufbau mit öffentlichem Spielvertrag | Die ersten zwölf Hauptquests sind mit 5.000 Startgeld und vier Teamkarten in 1.587 simulierten Sekunden erreichbar. Danach bleiben 3.427,6 Geld, sieben Teamkarten und 51 Grinder. Keine Premiumausgabe, keine Recovery-Hilfe, kein Einkommen aus früheren Städten; deren Personal blieb erhalten. |
| Referenzen | 299 neue Produktionsassets und die Szene mit 804 unterschiedlichen GUID-Verweisen geprüft; keine unauflösbaren Verweise, keine fehlenden Metas und keine doppelten eigenen GUIDs. |
| Erhaltung | 8.380 vorher vorhandene Dateien verglichen: keine entfernten Dateien und keine unerwarteten Änderungen. Die 13 Änderungen liegen ausschließlich in der dokumentierten Integrationsliste; 8.367 Dateien sind bytegleich. Die vier ursprünglichen Szenendateien und alle bisherigen Content-/Lokalisierungseinträge sind erhalten. |

Die vollständigen Belege stehen in `validation_summary.json`, `foundation_simulation.json`, `Preservation/result.json` und `asset_reference_validation.json`. Testfälle werden über ihren vollständigen Namen dedupliziert; Wiederholungen erhöhen die angegebenen Testzahlen nicht. Die früheren fehlgeschlagenen Läufe bleiben in `testAttemptHistory` sichtbar, darunter korrigierte Testfixtures für Szenenreferenzen, Unicode-Erwartungen und Voraussetzungen des Brunnenprojekts.

Die erste tatsächliche Bildprüfung fand verdeckte Erinnerungsobjekte bei Elio, Lucia und Amara sowie ungünstige Ausschnitte von Brunnen und Terrassengarten. Die Korrektur verschob ausschließlich die nachweislich unveränderten eigenen Erinnerungs-/Marktobjekte und passte den Ausschnitt der diagnostischen Aufnahmen an. Die Spielkamera und Bewohnerpositionen blieben unverändert. Die alten Aufnahmen und Befunde sind unter `Validation/20261008_065205_VisualBeforeRepair` archiviert; die erneute Prüfung wird mit den aktuellen Datei-Hashes in `visual_review.json` zusammengeführt. Einzelheiten und die Bedingungen zum Schutz manueller Änderungen stehen in `scene_visual_repair.json`.

Die sechs gemeldeten Bilddefekte wurden im finalen Capture als korrigiert verifiziert. `validation_summary.json` führt alle 13 lokalen Prüfschritte als bestanden. Der gemeinsame finale Fingerprint von Capture, Layout- und Pixelbeleg lautet `f1e711cbee85b5bd99517efe75269e08963251dc19b2a771ca83a8c0bbb92a58`; es gibt keine veralteten Quellhashes oder Bildhashes im Abschlussbericht.

Die lokale technische Integration und die tatsächliche Bildprüfung sind getrennt von einer persönlichen Art-Abnahme geführt. Physischer Touch, Geräteaudio, ein neuer Player-/APK-Build, ein vollständiger menschlicher Kampagnendurchlauf und die menschliche Endredaktion aller Sprachfassungen sind mit diesen Nachweisen nicht abgeschlossen. Die entsprechenden Abnahmefelder bleiben ausdrücklich offen.
