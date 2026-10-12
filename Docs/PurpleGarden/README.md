# Purple Garden

Purple Garden ist die achte Stadt der Kampagne. Rockos Mandat erscheint ab **500.000 gleichzeitig vorhandenen Einwohnern in den ersten sieben zugänglichen Städten**: Burning Hills, Big Rock City, Seven Hills, Little Montana, Cassa Vadela, Mark Wood City und Green Leaf Village. Das Spiel bewahrt den höchsten tatsächlich beobachteten gemeinsamen Stand; nach Erreichen der Schwelle bleibt die Einladung auch bei einem späteren Rückgang verfügbar. Purple Gardens eigener Bevölkerungsstand gehört zu ihrer lokalen Entwicklung.

Die neue Stadt verwendet Content-Version **9**, City-ID `purple_garden`, Instanz-/Quest-/Bewohnerpräfix `pg_` und die Szene `Assets/Weedtopia/Game/Scenes/PurpleGarden.unity`. Bei Annahme des Mandats gewährt das Spiel dem Spieler einmalig einen **Startzuschuss von 12.500 Geld und sieben Teamkarten**. Die Belohnung wird auch bei erneutem Mandatsaufruf nur einmal vergeben. Die Hauptquests und Bewohnergeschichten richten sich anschließend nach Purple Gardens eigener Bevölkerung.

## Geschichte und Stadtbild

Purple Garden war eine botanische Forschungsstadt. Violette Glashäuser, Wasserhöfe und ein Saatgutarchiv verbanden Forschung und Nachbarschaft. Als Leitungspläne verloren gingen und der Archivzugang privatisiert wurde, trockneten gemeinsame Gärten aus. Die Menschen hinter den Betrieben verloren den Zugang zu ihrem Wissen und die Möglichkeit, ihren Alltag gemeinsam zu organisieren.

Iris rettete Pflanzen, während Malik versuchte, die verlorenen Wasserwege allein wiederzufinden. Ada arbeitete am privaten Archiv und muss nun auch ihre damalige Verantwortung erklären. Theo durfte seine Reparaturzeichnungen nicht weitergeben. Viola, Jun, Ravi und Selma bringen Erinnerungen und Erfahrungen mit, die auf keiner offiziellen Forschungskarte standen.

Der Wiederaufbau verbindet Versorgung, offene Aufzeichnungen und geteilte Verantwortung. Das Leitmotiv lautet: **Wer eine Stadt pflegt, soll über ihre Zukunft mitentscheiden.** Die Bewohner dürfen widersprechen, Arbeit abgeben und selbst Gäste ihrer wieder lebendigen Stadt werden. Das Nachtblütenfest beendet ihre Geschichten nicht; gemeinsame Pflege und weitere persönliche Kapitel gehören weiterhin zum Alltag.

| Bestehende Zone | Eigener Viertelname | Bedeutung |
| --- | --- | --- |
| Farm | Violetthaine | gerettete Pflanzen, offene Beobachtungen und violette Glashäuser |
| City | Blütenmarkt | Herkunft der Cannabis-Blütensorten, Nachbarschaft und Nachtmarkt |
| Settlement | Wasserhöfe | gemeinsamer Alltag, Wohnen, Zuhören und ruhige Erinnerungsorte |
| Industry | Glashütten | geteilte Reparaturzeichnungen, Produktionsbetriebe und das offene Saatgutarchiv |

Die Darstellung verwendet die feste isometrische Kamera sowie die vorhandenen 2D-/2.5D-Gebäude und Requisiten-Prefabs. Vier eigene Orts-Prefabs tragen das Stadtbild: `VioletGlasshouse`, `WaterCourt`, `SeedArchive` und `BloomMarketPavilion`. Acht eigene Bewohnerillustrationen, ein Stadtschild und vier Architekturillustrationen liegen unter `Assets/Weedtopia/Production/PurpleGarden/Art/`. Figuren, Kapitel-Erinnerungen und Ortszustände werden durch den expliziten Editor-Installer als sichtbare Prefab- und Szenenobjekte angelegt.

## Acht dauerhafte Bewohner

Alle Schwellen dieser Tabelle beziehen sich auf **Purple Garden selbst**. Jede Person hat vier persönliche Kapitel und drei wiederkehrende Alltagsvarianten. Der bestehende Wiederholungsabstand beträgt 300 Simulationssekunden. Ein abgeschlossenes Kapitel und seine gespeicherte Wahl bleiben erhalten; die Person bleibt in der Stadt.

| Bewohner | ID | Rolle | Kapitel 1 | Kapitel 2 | Kapitel 3 | Kapitel 4 |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| Iris Voss | `pg_iris` | Hüterin der Nachtblüten | 50 | 1.000 | 5.000 | 15.000 |
| Malik Reed | `pg_malik` | Ingenieur der verlorenen Wasserwege | 120 | 1.500 | 6.000 | 16.000 |
| Viola Fenn | `pg_viola` | Gastgeberin am Blütenmarkt | 260 | 1.900 | 6.800 | 17.000 |
| Theo Moss | `pg_theo` | Restaurator der violetten Glashäuser | 500 | 2.400 | 7.600 | 17.500 |
| Jun Park | `pg_jun` | Gärtner der gemeinsamen Höfe | 850 | 2.900 | 8.400 | 18.000 |
| Ada Bloom | `pg_ada` | Archivarin der freigegebenen Samen | 1.300 | 3.500 | 9.300 | 18.500 |
| Ravi Sol | `pg_ravi` | Bäcker des Nachtmarkts | 1.900 | 4.300 | 10.500 | 19.500 |
| Selma Rain | `pg_selma` | Vermittlerin der Wasserhöfe | 2.600 | 5.200 | 11.500 | 20.000 |

**Iris Voss**: Iris blieb in der Forschungsstadt, als die Bewässerung ausfiel. Sie rettete letzte Pflanzen in Eimern und misstraut großen Versprechen. Malik braucht ihre Beobachtungen; Jun erinnert sie daran, dass Pflege auch Freude sein darf.

**Malik Reed**: Malik erbte unvollständige Leitungspläne seines Vaters. Er wollte die Stadt allein reparieren und übersah die Erinnerungen der Nachbarn. Iris kennt die trockenen Höfe; Selma hilft ihm, Fragen statt Befehle zu stellen.

**Viola Fenn**: Violas Coffeeshop war der Treffpunkt der Forschenden. Später durfte nur noch mit Ausweis hinein, wer zum privaten Archiv gehörte. Sie öffnet wieder für Nachbarn, muss aber erst lernen, auch schwierige Gespräche auszuhalten.

**Theo Moss**: Theo hielt die Glashäuser mit alten Scheiben zusammen. Die Archivverträge verboten ihm sogar Reparaturzeichnungen zu teilen. Nun sucht er eine Werkstatt, in der Können weitergegeben wird, ohne jemanden unentbehrlich zu machen.

**Jun Park**: Jun kehrte mit Samen seiner Großmutter zurück. Die alten Forschungsgärten kannten ihre Sorten nicht, obwohl viele Nachbarn sie pflegten. Er möchte Alltag und Forschung verbinden, ohne Erinnerungen in Besitzansprüche zu verwandeln.

**Ada Bloom**: Ada arbeitete am privaten Saatgutarchiv und glaubte, genaue Regeln schützten das Wissen. Als Nachbarn ihre eigenen Aufzeichnungen nicht mehr lesen durften, begann sie Kopien zu retten. Sie öffnet das Archiv und muss auch ihre frühere Rolle erklären.

**Ravi Sol**: Ravi belieferte nachts die Forschenden, bis niemand mehr an den leeren Tischen saß. Seine Familienrezepte erzählen von vielen Städten. Er will sie teilen, ohne wieder jede Nacht allein für alle zu arbeiten.

**Selma Rain**: Selma moderierte früher Sitzungen, deren Entscheidungen längst feststanden. Sie blieb, um auch denen zuzuhören, die nie eingeladen wurden. Jetzt sucht sie faire Gespräche, in denen Verantwortung und Widerspruch beide Platz haben.

Die Beziehungen verbinden mehrere Geschichten: Iris, Malik und Jun teilen Beobachtungen und Pflege. Selma hilft Malik, auch andere Stimmen auf seiner Karte zuzulassen. Theo und Ada öffnen Zeichnungen und Archivzugänge. Jun bringt die Herkunftsgeschichten seiner Großmutter ein. Viola lernt, schwierige Gespräche auszuhalten; Ravi bekommt einen eigenen Platz als Nachbar und Gast. Selma bewahrt Zustimmung, Widerspruch und offene Fragen.

Im dritten Kapitel erhält jede Person zwei eigene Entscheidungen. Ihre IDs werden im vorhandenen Story-Spielstand gespeichert und steuern vorab angelegte Erinnerungsobjekte:

| Bewohner | Wahl A | Wahl B |
| --- | --- | --- |
| Iris Voss | `twilight_walk` — Einen Abendrundgang erinnern | `quiet_garden` — Einen stillen Garten erinnern |
| Malik Reed | `public_map` — Eine öffentliche Karte erinnern | `care_roster` — Einen gemeinsamen Pflegeplan erinnern |
| Viola Fenn | `tasting_table` — Den offenen Probiertisch erinnern | `evening_circle` — Den Abendkreis erinnern |
| Theo Moss | `repair_team` — Das gemeinsame Reparaturteam wählen | `teaching_bench` — Die öffentliche Lernbank wählen |
| Jun Park | `seed_exchange` — Den Saatguttausch erinnern | `paired_gardens` — Die Gartenpatenschaften erinnern |
| Ada Bloom | `open_catalog` — Den offenen Katalog erinnern | `travelling_archive` — Das Wanderarchiv erinnern |
| Ravi Sol | `shared_recipes` — Die geteilten Rezepte erinnern | `neighbour_oven` — Den Nachbarschaftsofen erinnern |
| Selma Rain | `listening_bench` — Die Zuhörbank erinnern | `public_minutes` — Die öffentlichen Protokolle erinnern |

Theos Entscheidung hat zusätzlich einen sichtbaren Bezug zum gemeinsamen Saatgutarchiv: `repair_team` zeigt das Reparaturteam, `teaching_bench` die öffentliche Lernbank. Diese Varianten bleiben auch nach Speichern, Offline-Fortschritt und Städterundreise erhalten.

## Hauptgeschichte und Gemeinschaftsplätze

Die **18 Hauptquests** verwenden die bestehende Folge von Bau, Teamzuweisung, Automatisierung, Cannabis-Blütenverkauf, Wohnen/Park, Tagesertrag und VIP-Belohnung. Lokale Bevölkerungsschritte bei 600, 2.000, 3.800, 6.500 und 12.500 Einwohnern verbinden diese Wirtschaft mit den Bewohnern. Die letzte Hauptquest verlangt ebenfalls 12.500 lokale Einwohner sowie die bestehenden Produktions- und Tagesertragsbedingungen. Persönliche Kapitel reichen bis 20.000 Einwohnern weiter.

| Gemeinschaftsprojekt | ID | Lokale Einwohner | Voraussetzung | Fortschritt seit Annahme |
| --- | --- | ---: | --- | --- |
| Gemeinsamer Wasserhof | `pg_joint_water_court` | 2.000 | Iris, Malik und Viola: jeweils Kapitel 1 | 180 neue Cannabis-Blütenverkäufe im Coffeeshop |
| Wieder offenes Saatgutarchiv | `pg_joint_seed_archive` | 6.500 | Theo, Jun und Ada: jeweils Kapitel 1 | 340 neu gelieferte Waren |
| Erstes Nachtblütenfest | `pg_joint_night_bloom` | 12.500 | alle acht Bewohner: jeweils Kapitel 1 | 45 neu abgeschlossene Lieferungen |

Jeder Gemeinschaftsplatz besitzt einen Zustand vor dem Aufbau, einen Zustand mit gesammelten Materialien und einen abgeschlossenen Zustand. `CityPlaceProgressVisual` bindet diese Objekte an die lokale Bevölkerungsentwicklung und den gespeicherten Projektabschluss. Die persönliche Kapitelgeschichte nutzt `VipMemoryVisual` für dauerhafte Erinnerungen.

Wasserpläne, Saatgut, Glashäuser und Rezepte geben dem bestehenden Spielmodell ihre erzählerische Bedeutung. Die Aufgaben verwenden dessen Gebäudelevel, Rohwaren- und Verarbeitungserträge, Cannabis-Blütenverkäufe, Butterproduktion, Gebäckverkäufe, Warenlieferungen und abgeschlossene Lieferungen. Fortschritt wird ab Annahme eines Auftrags gemessen. Geld, Sammlung und Team werden gemeinsam verwaltet; Gebäude, Hauptquests, Bewohnergeschichten und Aktivitätszähler bleiben stadtspezifisch.

Der ergänzte Content umfasst **56 Bauplätze, 35 Warenrouten, 18 Hauptquests, acht dauerhafte Bewohner, 32 persönliche Kapitel, 24 wiederkehrende Varianten und drei Gemeinschaftsprojekte**. **315 Textschlüssel** liegen in Deutsch, Englisch, Spanisch, Französisch und Russisch vor. Technische Sprach- und Platzhalterprüfung sowie muttersprachliche Abnahme werden als eigene Nachweise geführt.

## Explizite Werkzeuge

Der Contentgenerator und der Unity-Installer werden ausdrücklich aufgerufen. Der Contentgenerator prüft Identitäten, frühere Einträge, Bedingungen, Übersetzungen und Platzhalter vor seinem ersten Schreibzugriff. Identische bereits vorhandene Purple-Garden-Einträge werden wiederverwendet; widersprüchliche Einträge werden zurückgewiesen. Der Editor-Installer ergänzt Szene, Prefabs, UI-Reisezeile und Build-Ziel.

Vom Projektverzeichnis aus:

```powershell
& 'C:\Python312\python.exe' -X utf8 'Tools\Implementation\author_purple_garden.py'
& 'C:\Python312\python.exe' -X utf8 'Tools\Implementation\compile_sources.py'
& 'Tools\Implementation\Run-PurpleGarden.ps1' -Mode Install -Label 'PurpleGarden'
& 'Tools\Implementation\Run-PurpleGarden.ps1' -Mode Capture -Label 'PurpleGarden'
& 'C:\Python312\python.exe' -X utf8 'Tools\Implementation\purple_garden_asset_references.py'
& 'C:\Python312\python.exe' -X utf8 'Tools\Implementation\purple_garden_preservation.py'
& 'C:\Python312\python.exe' -X utf8 'Tools\Implementation\purple_garden_report.py'
```

Alternativ ist der explizite Editor-Einstiegspunkt `Weedtopia.Editor.GameSetup.PurpleGardenAuthoring.Apply` verfügbar. Der Batch-Runner startet eine eigene Editorinstanz nur, wenn keine bestehende Unity-Instanz und kein Unity-Lock vorhanden sind. Ein offener Benutzereditor muss vor einem Batchlauf regulär durch den Benutzer beendet werden. Szenen- und Objektänderungen erfolgen beim Installeraufruf.

`PurpleGardenEvidence.Begin` arbeitet mit dem getrennten Diagnose-Spielstand `Docs/PurpleGarden/capture_save.json` und dessen `.scores`-Datei. Die vorherige Spracheinstellung wird nach dem Lauf wiederhergestellt. Die ausgeführten **51 Game-View-Aufnahmen** umfassen:

- die Grenze 499.999/500.000, Rockos Einladung, einmalige Mandatsbelohnung und unverbaute Ankunft;
- vier eigene Viertel und alle acht dauerhaften Bewohner einschließlich oberer/unterer Liste im Portraitformat;
- Theos aktive Entscheidung, abgeschlossene Erinnerung und alle drei Zustände der Gemeinschaftsplätze;
- UI bei 1920×1080, 1366×768, 2560×1080 und 1080×1920 sowie Storydarstellung in fünf Sprachen;
- tatsächliches Speichern/Laden und `GameRoot.Continue()` mit genau 64 Offline-Sekunden, Produktion in allen acht Städten und null Sekunden bei erneutem Offline-Aufruf;
- tatsächliche Szenenreisen durch die sieben vorherigen Städte und zurück nach Purple Garden mit erneuter Wahl- und Ortsprüfung.

Für die visuelle Abdeckung werden Bevölkerung, Gebäudelevel und Aktivitätszähler ausdrücklich als Diagnosezustände gesetzt. Der Erhaltungsnachweis vergleicht frühere Hauptquests, Bewohnergeschichten, Mandate, Gebäudefamilien/-level und Arbeiterzuordnungen gegen vorab gespeicherte Signaturen. Die sieben Quellen werden vor diesem Vergleich durch den bestehenden Storyservice aktualisiert, damit ihre zur Diagnosebevölkerung gehörenden Bewohnerzustände bereits vorhanden sind.

## Nachweise und offene Abnahmen

Stand **9. Oktober 2026** sind die explizite Unity-Installation, die relevanten Domain-/Asset-/PlayMode-Prüfungen und der tatsächliche Game-View-Lauf ausgeführt. Die drei finalen NUnit-Läufe bestehen aus **248 Domainfällen, fünf Assetfällen und 35 PlayMode-Fällen**, insgesamt **288 bestandenen Fällen ohne Fehler oder übersprungene Fälle**. Darunter sind **33 Purple-Garden-Domainfälle**. Die [Abschlussübersicht](validation_summary.json) führt diese lokale Integration als `passed_local_integration` mit `technicalIntegrationComplete=true` und ohne offene technische Gates. Die folgenden Links nennen den tatsächlich geprüften Umfang; persönliche Abnahmen werden weiterhin getrennt geführt.

| Gate | Aktueller Abschlussstatus | Zugehörige Evidenz |
| --- | --- | --- |
| Content, IDs, Ziele, fünf Sprachen, Platzhalter und Idempotenz | Content angelegt und live validiert; zweite Ausführung `changed=false`, alle sechs Eingabedateien byteidentisch | [Content](content_validation.json), [erste Ausführung](content_validation_first_run.json), [Idempotenz](idempotence_validation.json), [vorherige Einträge](preservation_validation.json) |
| Source-Compiler | 12 Assemblies / 325 C#-Dateien bestanden; aktueller Hashabgleich bestanden, keine geänderten/neuen/entfernten Quellen und keine geänderten Content-/Locale-Dateien seit ihrem Nachweis | [Compiler-Original](../Implementation/Validation/SourceCompile/result.json), [Compiler-Kopie](source_compile.json), [aktueller Hashabgleich](source_fingerprint_validation.json) |
| Unity-Import und explizite Szene-/Prefab-Installation | bestanden; 56 Bauplätze, 35 Routen, 251 vorhandene Gebäudevarianten, acht Bewohner und vier Architektur-Prefabs | [Authoring-Receipt](authoring_receipt.json), [Installationslauf](Validation/20261009_154826_Install_FirstBotanicalCity.json) |
| EditMode: Kampagnen, Geschichten, Spielstände, Offline-Modell, Wirtschaft und Lokalisierung | **248/248 bestanden**, einschließlich 33 Purple-Garden-Fällen | [Domain-JSON](../Implementation/Validation/20261009_155046_EditMode_PurpleGardenRelevantDomain.json), [Domain-XML](../Implementation/Validation/20261009_155046_EditMode_PurpleGardenRelevantDomain.xml) |
| EditMode: alle acht Reisezeilen, Szene-/Prefab-Verträge, 251 Gebäudevarianten, Illustrationimporte und idempotenter Installer | **5/5 bestanden** | [Asset-JSON](../Implementation/Validation/20261009_160659_EditMode_PurpleGardenAssets.json), [Asset-XML](../Implementation/Validation/20261009_160659_EditMode_PurpleGardenAssets.xml) |
| PlayMode: UI, Stadtwechsel, bestehende Städtereisen, Save/Continue, Bewohnerentscheidungen und Erinnerungen | **35/35 bestanden** | [PlayMode-JSON](../Implementation/Validation/20261009_160931_PlayMode_PurpleGardenTravelAndStories.json), [PlayMode-XML](../Implementation/Validation/20261009_160931_PlayMode_PurpleGardenTravelAndStories.xml) |
| Game View und vier UI-Auflösungen | **51 frische tatsächliche Aufnahmen**, fünf Sprachen, 1920×1080 / 1366×768 / 2560×1080 / 1080×1920; keine erkannten UI-Überläufe | [Capture-Lauf](Validation/20261009_161420_Capture_BotanicalGameViews.json), [Game-View-Receipt](gameview_receipt.json), [UI-Receipt](ui_overflow.json), [Aufnahmen](Screenshots/) |
| Acht-Städte-Save und Offline-Fortsetzung | alle acht Städte geladen; exakt 64 Offline-Sekunden; Produktion in jeder Stadt; Quests, Gebäude, Arbeiter und Wahl-Erinnerung erhalten; zweiter Offline-Aufruf **0 Sekunden** | Feld `saveOffline` im [Game-View-Receipt](gameview_receipt.json), [Offline-Anzeige](Screenshots/24_save_continue_offline_eight_cities.png), [Stadtliste danach](Screenshots/24b_eight_cities_after_offline_continue.png) |
| Startwirtschaft | erste zwölf Hauptquests mit dem einmaligen 12.500-Geld-/Sieben-Karten-Zuschuss und lokalem Einkommen ohne Erträge vorheriger Städte geprüft | Domainfall `PurpleGardenCampaignTests.LocalFoundationCompletesTwelveMainQuestsWithTheBridgeAndNoPreviousCityIncome` im [Domain-XML](../Implementation/Validation/20261009_155046_EditMode_PurpleGardenRelevantDomain.xml) |
| Erhaltung der sieben bestehenden Städte und des vorgefundenen Projektstands | 7.270 Dateien geschützt; 7.261 unverändert; neun geprüfte Integrationsänderungen; **alle sieben früheren Stadtszenen byteidentisch**; keine unerwarteten Änderungen oder entfernten Dateien | [Baseline](Preservation/baseline.json), [Erhaltungsreceipt](Preservation/result.json) |
| Asset-Referenzen | 302 neue Assets / 809 geprüfte verschiedene Referenzen; keine fehlenden Metas, doppelten GUIDs oder unaufgelösten Referenzen im geprüften Umfang | [Referenzreceipt](asset_reference_validation.json) |
| Technische Bilddateien, Transparenz und Spriteimporte | **13 Illustrationen geprüft**; Pixel-/PNG- und Importnachweise vorhanden | [Bildvalidierung](art_validation.json), [Bildgenerierung](art_generation.json), [Asset-XML](../Implementation/Validation/20261009_160659_EditMode_PurpleGardenAssets.xml) |
| Abschließende technische Sichtung der Game-View-Darstellung | alle **51 Aufnahmen in drei Übersichten**, **acht ausgewählte Aufnahmen in voller Größe** und beide Art-Kontaktbögen geprüft; keine beobachteten blockierenden Darstellungsfehler; Benutzerabnahme bleibt offen | [Visuelles Review](visual_review.json), [Aufnahmen](Screenshots/) |
| Visuelle Benutzerabnahme | **offen** | direkte Bewertung durch den Benutzer |
| Physische Gerätebedienung und Geräteaudio | **offen** | tatsächlicher Gerätetest |
| Android-/WebGL-Player und Veröffentlichung | **offen** | tatsächlicher Player-/Release-Test |
| Muttersprachliche Prüfung aller Übersetzungen | **offen** | menschliche Sprachprüfung |
| Menschliches Durchspielen aller 18 Hauptquests und der vollständigen Stadtwirtschaft | **offen** | tatsächlich gespielte Wirtschaft und Progression |

Die 51 Game-View-Bilder stammen aus einem isolierten Diagnose-Spielstand und echten Szenenwechseln. Sie zeigen die drei Baustufen der Gemeinschaftsplätze, Theos gespeicherte Lernbank, Portrait-Scrollabschnitte und Rückreisen durch die sieben bisherigen Städte. Bevölkerungs-, Gebäudelevel- und Aktivitätszählerstände sind ausdrücklich diagnostische Fixtures. Der Nachweis der ersten zwölf wirtschaftlich ausführbaren Hauptquests und der technischen Offline-Fortsetzung wird getrennt vom menschlichen Durchspielen aller 18 Hauptquests bewertet.

`Tools/Implementation/purple_garden_report.py` hat die tatsächlichen finalen Receipts in [validation_summary.json](validation_summary.json) zusammengeführt. Der finale Bericht bestätigt `passed_local_integration`, `technicalIntegrationComplete=true`, 288 bestandene NUnit-Fälle und keine offenen technischen Gates. Die technische Sichtung ist mit ihrem tatsächlichen Umfang in [visual_review.json](visual_review.json) dokumentiert. Es prüft die aktuellen Source-/Content-Hashes, NUnit-XML-Zähler, jüngste passende Läufe, alle 51 PNGs samt Dimensionen, Illustrationen, Referenzen und den Erhaltungsbaseline. Fehlende, fehlgeschlagene oder veraltete Gates werden als unvollständig ausgewiesen. Benutzerfreigabe, physische Bedienung, Geräteaudio und menschliches Durchspielen bleiben offen; `releaseAccepted` bleibt `false`.
