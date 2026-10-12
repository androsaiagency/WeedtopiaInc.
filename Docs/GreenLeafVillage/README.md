# Green Leaf Village — siebte Stadt

Green Leaf Village wird ab **250.000 Einwohnern in den ersten sechs Städten zusammen** angeboten. Rockos Mandat verwendet die bestehende Städteprogression und gewährt bei Annahme **einmalig 10.000 Geld und 6 Teamkarten als Aufbauhilfe**. Die sechs Quellen sind Burning Hills, Big Rock City, Seven Hills, Little Montana, Cassa Vadela und Mark Wood City. Der Bestand der neuen Stadt erhöht ihre eigene Freischaltquelle nicht.

Der neue Content ist Version **8**, City-ID `green_leaf_village`, Instanz-/Quest-/Bewohnerpräfix `gl_`, Szenepfad `Assets/Weedtopia/Game/Scenes/GreenLeafVillage.unity`. Alte Städte und Inhalte behalten ihre IDs, Versionsmarkierungen, Bedingungen und Texte.

## Geschichte und Identität

Green Leaf Village entstand als Genossenschaft von Pflanzengärten, Glashäusern, offenen Werkstätten und einem gemeinsamen Pflanzenarchiv. Nachbarn konnten Werkzeuge ausleihen, Arbeit übergeben und Wissen am selben Tisch ergänzen. Als Höfe, Lieferverträge und Aufzeichnungen einzeln privatisiert wurden, sollten alle Betriebe allein bestehen. Neue Zäune trennten die Wege. Wissen verschwand hinter Bürotüren, Werkzeuge hinter Schlüsseln, Familienrezepte in exklusiven Verträgen. Das Dorf verlor Arbeitsplätze und Bewohner; die Glashäuser verwilderten.

Die Bewohner waren daran unterschiedlich beteiligt. Elva rettete das Archiv und verschloss es dabei auch vor ihren Nachbarn. Theo versuchte, ohne gemeinsame Aufzeichnungen alles allein zu reparieren. Ruben empfahl Verträge, die zunächst Sicherheit versprachen, später aber den Austausch unterbrachen. Livia verlor die Kontrolle über ihre Rezepte und ihre freie Zeit. Die neue Geschichte gibt diesen Menschen Antworten und Beziehungen, statt eine einzige Schuldfigur festzuschreiben.

Rockos Einladung ist der Anfang eines Wiederaufbaus mit den bestehenden Wirtschaftsketten. Pflanztische, Werkbänke, Markt, Briefe und öffentliche Absprachen zeigen, wem dieser Ort wieder gehört. Das Leitmotiv lautet **„Wissen wächst gemeinsam“**. Das Wachstum soll es Menschen erlauben, Verantwortung abzugeben, Vertrauen neu zu prüfen und einen eigenen Platz zu finden.

Vier Viertel tragen diese Identität:

| Bestehende Zone | Name | Erzählfunktion |
| --- | --- | --- |
| Farm | Gewächshausgärten | Pflanzenarchiv, geteilte Aufzeichnungen, neu geöffnete Glashäuser |
| City | Blättermarkt | nachvollziehbare Cannabis-Blütensorten, offener Treffpunkt, öffentliche Versorgung |
| Settlement | Rankenhöfe | Wohnungen, gemeinsamer Alltag, ruhige Erinnerungs- und Rastplätze |
| Industry | Genossenschaftswerkstätten | Werkzeuge, Rezepte, Lernplätze und gemeinsame Produktionsketten |

Die Darstellung nutzt die bestehende feste isometrische Kamera sowie die vorhandenen 2D-/2.5D-Gebäude und wiederverwendbaren Orts-Prefabs. Statische Figuren, Erinnerungen und Landmark-Zustände werden vom expliziten Editor-Installer als Prefabs und Szenenobjekte angelegt.

Vier eigene gemalte Ortsillustrationen ergänzen diese Gebäude: gemeinsames Gewächshaus, Saatterrasse, Marktpergola und Genossenschaftstor. Alle vier besitzen echte Transparenz und werden in wiederverwendbaren Prefabs mit Bodenpivot und vorhandener Tiefensortierung gespeichert. Die erste Game-View-Prüfung zeigte zu einfache geometrische Formen; ihre Originaldateien und Vergleichsbilder liegen reversibel unter `ArtArchive`. Die Umstellung erhält die Prefab-GUIDs und gemeinsame Requisiten. Vor dem Entfernen der alten Geometrie prüft der Installer Original-Prüfsummen, den eigenen absoluten Zielpfad und sämtliche importierten Asset-Abhängigkeiten. Promptset und Bilddaten sind in `architecture_art_generation.json` und `architecture_art_validation.json` dokumentiert; die Einwohner- und Schildillustrationen in `art_generation.json` und `art_validation.json`.

## Acht dauerhafte Bewohner

Alle folgenden Schwellen beziehen sich auf **die Bevölkerung von Green Leaf Village selbst**. Gesamtbevölkerung wird nur für das Mandat verwendet. Eine Figur bleibt nach erledigten Kapiteln erhalten und hat drei Varianten wiederkehrender Alltagsaufträge mit dem bestehenden Wiederholungsintervall von 300 Sekunden.

| Bewohner | ID | Fokusinstanz | Kapitel 1 | Kapitel 2 | Kapitel 3 | Kapitel 4 |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Elva Grün | `gl_elva` | `gl_farm_warehouse` | 50 | 1.000 | 5.000 | 15.000 |
| Theo Voss | `gl_theo` | `gl_farm_grow_01` | 120 | 1.500 | 6.000 | 16.000 |
| Farah Noor | `gl_farah` | `gl_coffee_01` | 260 | 1.900 | 6.800 | 17.000 |
| Casper Rehm | `gl_casper` | `gl_farm_processing` | 500 | 2.400 | 7.600 | 17.500 |
| Livia Kern | `gl_livia` | `gl_butter_factory` | 850 | 2.900 | 8.400 | 18.000 |
| Ruben Feld | `gl_ruben` | `gl_supermarket` | 1.300 | 3.500 | 9.300 | 18.500 |
| Ada Wendt | `gl_ada` | `gl_town_hall` | 1.900 | 4.300 | 10.500 | 19.500 |
| Pekko Aalto | `gl_pekko` | `gl_city_warehouse` | 2.600 | 5.200 | 11.500 | 20.000 |

**Elva Grün** ist die Hüterin des Pflanzenarchivs. Sie hat das Wissen gerettet, vertraut anderen aber kaum noch. Theo braucht ihre Aufzeichnungen, Ada möchte auch die früheren Fehler bewahren. Ihre Kapitel führen vom alten Archivschild über Beete ohne Grenzpfahl zur Wahl zwischen offenem Leseschrank und Gartentisch. Am Ende kann Theo eine Gartenrunde übernehmen, während Elva ohne Notizbuch zum gemeinsamen Treffen kommt.

**Theo Voss** blieb bei den stillen Glashäusern und hält deren Niedergang für persönliches Versagen. Casper erinnert ihn daran, wie viele Türen damals verschlossen waren. Theo stellt den ersten Pflanztisch auf, zeigt auch misslungene Reparaturen und entscheidet zwischen Reparaturwerkstatt und offenem Lernplatz. Seine letzte Laterne steht für einen Feierabend, an dem ein anderer Mensch die Werkstatt betreuen kann.

**Farah Noor** erbte den Coffeeshop ihrer Tante und getrennte Sortenkarten für jeden Hof. Sie möchte nachvollziehbare Cannabis-Blüten und Gespräche über ihre Herkunft. Das fordert Rubens alte Lieferverträge heraus. Ihre Geschichte beginnt mit sichtbar zugeordneten Warenkisten, öffnet eine Terrasse und lässt zwischen Sitzplatz und lesbarer Sortentafel wählen. Später kann Farah einen Markttag übergeben und selbst Elva zuhören.

**Casper Rehm** sammelte Schlüssel, während Nachbarn auf Werkzeuge warteten. Theo will gemeinsam arbeiten; Livia fragt, wie gemeinsamer Zugriff zuverlässig bleiben kann. Casper öffnet seinen Eingang und seine Werkbank. Seine Wahl zwischen Werkzeugtausch und öffentlicher Lernbank verändert auch den Genossenschaftshof. Im vierten Kapitel kann er heimgehen, während Theo den Hof betreut.

**Livia Kern** verlor ihre Familienrezepte an einen exklusiven Vertrag. Sie versorgte Fremde, hatte aber kaum Zeit für Nachbarn. Farah bietet eine gemeinsame Versorgungsschicht an, Ruben muss seinen damaligen Rat erklären. Livia holt das Rezeptheft zurück und wählt einen Marktstand oder eine gemeinsame Lernwerkstatt. Ihre letzte Geschichte gibt ihr einen Platz als Gast, während andere die Arbeit übernehmen.

**Ruben Feld** versprach den Höfen Sicherheit durch getrennte Verträge. Als die Versorgung auseinanderbrach, blieben viele Fragen offen. Livia erwartet Antworten, Ada bewahrt seine Geschichte einschließlich seiner Versäumnisse. Ruben erläutert Verträge und Vorräte öffentlich und wählt Lesebuch oder Vorratstafel. Vertrauen entsteht anschließend durch gemeinsam überprüfbare Zusagen, nicht durch eine einzelne Entschuldigung.

**Ada Wendt** sammelte Briefe der Fortgezogenen. Elva fürchtet Vorwürfe, Ruben fürchtet, nur als Schuldiger erinnert zu werden. Ada benennt die leeren Höfe und legt Briefe neben neue Antworten. Sie wählt eine öffentliche Zeugentafel oder einen ruhigen Leseraum. Ihr letzter Baum erinnert auch an die Menschen, die geblieben sind.

**Pekko Aalto** ging, als Zäune die gemeinsamen Wege trennten. Er kehrt mit Briefen zurück und möchte mehr als ein Bote zwischen anderen sein. Ada hört seine eigene Geschichte, Ruben plant Übergaben mit ihm. Pekko erhält eine Laterne und verlässliche Pausen, wählt Wegkarte oder geschützten Rastplatz und sitzt schließlich nach der Lieferung bei Ada, ohne etwas abgeben zu müssen.

Die dauerhaften Beziehungen verbinden mehrere Geschichten: Elva–Theo teilen Wissen und Verantwortung; Theo–Casper sprechen über Reparaturen und frühere verschlossene Türen; Farah–Ruben klären Herkunft und Verträge; Livia–Ruben bearbeiten eine alte Zusage; Ada–Elva/Ruben verbinden Erinnerung mit Antworten; Ada–Pekko geben einer Rückkehr eine eigene Geschichte. Niemand verschwindet nach einem einzelnen Auftrag.

## Stadtentwicklung in vier Abschnitten

1. **Offene Türen:** Das Mandat führt zu Feld, Verarbeitung, Lagern und Blättermarkt. Die 18 Hauptquests verwenden die etablierte Folge von Bau, Personaleinsatz, Farm-Automatisierung, Cannabis-Blütenverkauf, Wohnhaus/Park, Tagesertrag und VIP-Belohnung.
2. **Gemeinsames Wissen:** Ab 600 lokalen Einwohnern bereitet die Hauptgeschichte das Pflanzenarchiv vor. Das eigene Gemeinschaftsprojekt öffnet bei 2.000 Einwohnern nach den ersten Kapiteln von Elva, Theo und Farah. Leseräume, Pflanzentische und offene Treffpunkte bleiben sichtbar.
3. **Verantwortung teilen:** Bei 3.800 Einwohnern werden Vorräte und Übergaben öffentlich erklärt. Ab 6.500 Einwohnern können Casper, Livia und Ruben nach ihren ersten Kapiteln den Genossenschaftshof öffnen. Caspers spätere gespeicherte Wahl zeigt Werkzeugtausch oder Lernbank am gemeinsamen Ort.
4. **Ein Dorf, das bleibt:** Bei 12.500 Einwohnern verbindet der Green Leaf Day die vier Viertel. Alle acht ersten Kapitel sind Voraussetzung des Projekts. Das Fest beendet weder persönliche Kapitel noch wiederkehrende Aufträge; die persönlichen Geschichten reichen bis 20.000 lokalen Einwohnern.

## Gemeinschaftsprojekte und sichtbare Orte

| Projekt | ID | Lokale Einwohner | Voraussetzung | Fortschritt seit Annahme |
| --- | --- | ---: | --- | --- |
| Offenes Pflanzenarchiv | `gl_joint_seed_library` | 2.000 | Elva, Theo, Farah: jeweils Kapitel 1 | 180 neue Cannabis-Blütenverkäufe im Coffeeshop |
| Genossenschaftshof | `gl_joint_coop_courtyard` | 6.500 | Casper, Livia, Ruben: jeweils Kapitel 1 | 340 neu gelieferte Waren |
| Green Leaf Day | `gl_joint_green_leaf_day` | 12.500 | alle acht: jeweils Kapitel 1 | 45 neu abgeschlossene Lieferungen |

Jedes Projekt erhält einen vorab angelegten Zustand vor dem Aufbau und einen sichtbaren Zustand nach Bevölkerung/Projektabschluss. Der Genossenschaftshof hat zusätzlich zwei vorab angelegte Varianten für Caspers Kapitel-3-Wahl. Die Figuren behalten vier Erinnerungsobjekte; Kapitel 3 hat zwei alternative Objekte. Die bestehenden `VipMemoryVisual`- und `CityPlaceProgressVisual`-Verträge steuern deren Sichtbarkeit.

| Bewohner | Kapitel-3-Wahl A | Kapitel-3-Wahl B |
| --- | --- | --- |
| Elva | `seed_cabinet` — offener Leseschrank | `garden_table` — Gartentisch |
| Theo | `repair_workshop` — Reparaturwerkstatt | `open_classroom` — Lernplatz |
| Farah | `welcome_patio` — gemeinsamer Sitzplatz | `menu_board` — Sortentafel |
| Casper | `tool_exchange` — Werkzeugtausch | `teaching_bench` — öffentliche Lernbank |
| Livia | `recipe_stall` — Rezeptstand | `shared_oven` — gemeinsame Lernwerkstatt |
| Ruben | `public_ledger` — öffentliches Lesebuch | `supply_board` — Vorratstafel |
| Ada | `witness_wall` — Zeugentafel | `reading_room` — ruhiges Lesezimmer |
| Pekko | `route_map` — Wegkarte | `rest_shelter` — geschützter Rastplatz |

## Spielmechaniken und Lokalisierung

Der neue Inhalt umfasst **56 Bauplätze, 35 Warenrouten, 18 Hauptquests, 8 dauerhafte Bewohner, 32 persönliche Kapitel, 24 wiederkehrende Varianten und 3 Gemeinschaftsprojekte**. Es gibt **315 neue Textschlüssel in Deutsch, Englisch, Spanisch, Französisch und Russisch**.

Persönliche Kapitel kombinieren den Ausbau einer bestehenden Gebäudefamilie mit einem bestehenden Fortschrittszähler. Die verwendeten Zähler sind Rohware, Verarbeitung, Cannabis-Blütenverkauf im Coffeeshop, Butterproduktion, Gebäckverkauf, gelieferte Waren und abgeschlossene Lieferungen. Fortschritt wird ab Annahme erfasst. Die Texte der Kapitel und Wiederholungsvarianten werden aus denselben Mengen/Zählern wie ihre Ziele zusammengesetzt.

Werkzeuge, Pflanztische, Rezepte, Briefe und Glashäuser sind Geschichte und sichtbare Erinnerungen. Für sie wird keine zweite Inventar-, Herstellungs-, Lieferung- oder Wirtschaftsarchitektur eingeführt. Money, Sammlung und Team bleiben geteilt; Gebäude, Quests, Bewohnerfortschritt und Zähler bleiben stadtspezifisch. Die bisherigen sechs Städte behalten ihre Simulation, Arbeiterzuweisungen und Offline-Fortschritte.

Die Hauptquest-Bevölkerungsziele sind 600, 2.000, 3.800, 6.500 und 12.500 lokale Einwohner. Die finale Hauptquest verlangt ebenfalls 12.500 lokale Einwohner sowie den bestehenden Supermarkt-Tagesertrag und die bestehende Automatisierung der Produktionskette. Die weiteren persönlichen Kapitel sind davon unabhängig bis 20.000 Einwohnern verfügbar.

## Explizite Werkzeuge

`Tools/Implementation/author_green_leaf_village.py` ergänzt den bestehenden Content und die fünf Lokalisierungen ausdrücklich beim direkten Aufruf. Es läuft weder beim Import noch beim Unity-Start oder Domain-Reload. Identische bestehende Green-Leaf-Einträge werden wiederverwendet, widersprüchliche Einträge werden vor dem ersten Schreibzugriff zurückgewiesen.

```powershell
& 'C:\Python312\python.exe' -X utf8 'Tools\Implementation\author_green_leaf_village.py'
```

Der Generator validiert vor dem Schreiben die sieben Stadt-/Gebäude-/Questbezüge, die sechs Freischaltquellen, Counts, lokale Meilensteine, Choice-IDs, frische Fortschrittsziele, Gemeinschaftsvoraussetzungen, vollständige Sprachen und identische Platzhalter. Er prüft, dass sämtliche vorherigen Inhalte und Locale-Einträge unverändert in ihrer ursprünglichen Reihenfolge erhalten bleiben.

`Tools/Implementation/verify_green_leaf_authoring.py` prüft den Generator ausschließlich auf temporären Kopien der gesicherten Version-7-Originaldateien. Die neun Vertragsprüfungen decken erste Autorierung, byteidentische Wiederholung, Erhalt einer vorhandenen Version 9 einschließlich unbekannter Erweiterungsdaten, Konfliktverweigerung für eine vorhandene Green-Leaf-Stadt, Konfliktverweigerung in jeder der fünf Sprachen und die fehlende sechste Freischaltquelle ab. Alle sechs Eingabedateien bleiben bei jedem Konflikt byteidentisch; es wird kein Erfolgsreceipt geschrieben. Auch die Live-Dateien wurden während dieser Prüfung per SHA256 als unverändert bestätigt.

Die Szenen-/Prefab-Integration erfolgt durch den dazugehörigen expliziten Editor-Installer. Die bereits offene Editorinstanz und vorhandene manuelle Zuordnungen werden berücksichtigt; der Content-Generator selbst startet oder schließt keine Unity-Instanz.

## Evidenz und offene Abnahme

Stand **9. Oktober 2026** ist die lokale Integration abgeschlossen. Der zentrale Bericht `validation_summary.json` verbindet die tatsächlich ausgeführten Prüfungen mit ihren Receipts und den verbleibenden Abnahmen:

| Gate | Stand | Evidenz |
| --- | --- | --- |
| Content, IDs, Ziele, Sprachen, Platzhalter und vorherige Einträge | bestanden | `content_authoring.json`, `content_validation.json` |
| Zweite Content-Ausführung | `changed=false` | `content_validation.json` |
| Byteidentische zweite Ausführung für Content und alle fünf Locales | bestanden, SHA256 für sechs Dateien | `content_idempotence.json` |
| Isolierte Authoring-Verträge, höhere Version und Konfliktverweigerung | 9/9 bestanden; Live-Content und Locales unverändert | `content_authoring_contracts.json` |
| Source-Compiler und aktueller Quellstand | 12 Assemblies / 318 C#-Dateien bestanden; Quell-, Content- und Locale-Prüfsummen aktuell | `source_compile.json`, `source_fingerprint_validation.json` |
| Unity-Import und explizite Szene-/Prefab-Installation | bestanden; keine statische Objekterzeugung zur Laufzeit | `authoring_receipt.json`, `Validation/20261009_061155_Install_PaintedArchitectureDependencyOrder.json` |
| EditMode: Kampagnen, Spielstände, Geschichten, Simulation, Progression, Lokalisierung | 215/215 bestanden, einschließlich 31 Green-Leaf-Fällen | `../Implementation/Validation/20261008_224439_EditMode_GreenLeafVillageRelevantDomain.xml` und `.json` |
| EditMode: Szene, 251 Gebäudevarianten, 13 Bildimporte, vier Architektur-Prefabs, Header, Idempotenz | 6/6 bestanden | `../Implementation/Validation/20261009_061244_EditMode_GreenLeafVillagePaintedAssets.xml` und `.json` |
| PlayMode: Stadtwechsel, bestehende Städtereisen und Bewohnerdialoge | 30/30 bestanden | `../Implementation/Validation/20261009_055150_PlayMode_GreenLeafVillageTravelAndStories.xml` und `.json` |
| PlayMode: Green Leaf nach endgültiger Darstellungskorrektur | 3/3 erneut bestanden; keine doppelte Zählung als neue Fälle | `../Implementation/Validation/20261009_061456_PlayMode_GreenLeafVillageFinalScene.xml` und `.json` |
| Game View: Schwelle, Mandat, Ankunft, Geschichten, Gemeinschaftsplätze, Städte-Rundreise | 42 frische Aufnahmen in 1920×1080, 1366×768, 2560×1080 und 1080×1920; fünf Sprachen; keine erkannten UI-Überläufe | `gameview_receipt.json`, `ui_overflow.json`, `Screenshots/`, `Validation/20261009_061609_Capture_PaintedFinalGameViews.json` |
| Technische visuelle Prüfung | bestanden; geometrische Erstfassung ersetzt, Headerabstand korrigiert, alle acht Porträts geprüft | `visual_review.json`, `ArtArchive/BeforeGameView/` |
| Erhaltung des vorgefundenen Projektstands | 6.648 Dateien geschützt; 6.637 unverändert; elf geprüfte Integrationsänderungen; sechs ursprüngliche Stadtszenen byteidentisch; keine unerwarteten Änderungen | `Preservation/result.json` |
| Asset-Referenzen | 302 neue Assets / 809 verschiedene Referenzen; keine fehlenden Metas, doppelten neuen GUIDs oder unaufgelösten Referenzen | `asset_reference_validation.json` |
| Startwirtschaft | erste zwölf Hauptquests mit 10.000 Geld und sechs Teamkarten ohne Einkommen vorheriger Städte, Premium oder Wiederherstellung geprüft | Domain-Test `LocalFoundationCompletesTwelveMainQuestsWithTheBridgeAndNoPreviousCityIncome` |
| Visuelle Benutzerabnahme | offen | direkte Bewertung durch den Benutzer |
| Physische Gerätebedienung und Geräteaudio | offen | tatsächlicher Gerätetest erforderlich |
| Android-/WebGL-Player und Veröffentlichung | offen | tatsächlicher Player-/Release-Test erforderlich |
| Muttersprachliche Prüfung aller Übersetzungen | offen | Sprach- und Platzhaltervollständigkeit sowie Darstellung sind technisch geprüft |
| Menschliches Durchspielen der vollständigen Stadtwirtschaft | offen | die Bildaufnahmen verwenden diagnostische Bevölkerungs-, Gebäude- und Zählerstände |

Insgesamt sind **251 unterschiedliche NUnit-Fälle** bestanden. Die drei abschließenden Green-Leaf-PlayMode-Wiederholungen sind zusätzliche Ausführungen derselben Fälle. Die projektweite Domain-Gesamtsuite wurde zugunsten der oben angegebenen relevanten Fixtures abgebrochen und wird nicht als bestanden gewertet; `Validation/full_domain_scope_change.json` dokumentiert diesen Umfang.

Die 42 Game-View-Bilder stammen aus einem getrennten Diagnose-Spielstand und echten Szenenwechseln. Sie zeigen auch die drei Baustufen der Gemeinschaftsplätze, beide Scrollabschnitte der Bewohnerliste, Caspers gespeicherte Lernbank und die Rückreise über Burning Hills und Mark Wood City. Diagnostische Bevölkerung, Gebäudelevel und Aktivitätszähler dienen der visuellen Abdeckung; sie belegen kein menschliches Durchspielen der gesamten Wirtschaft. Technische Bildprüfung ersetzt weder persönliche Kunstfreigabe noch physische Gerätebedienung. Es wurde keine Veröffentlichung vorgenommen.
