# Codex-Auftrag: Weedtopia Inc. zu einem professionell wirkenden, vollständig spielbaren Spiel fertigstellen

Arbeite im bestehenden Unity-Projekt `D:\Unity Games\Weedtopia Inc\WeedtopiaInc`. Dies ist ein **Umsetzungsauftrag** für die aktuelle Burning-Hills-Version, kein Konzept- oder Prototypauftrag. Der spielbare Kern soll erhalten und gestalterisch zu einem stimmigen, fertig wirkenden Spiel ausgebaut werden. In keinem freigegebenen Spielbild dürfen Platzhaltergrafiken, Entwicklungsmodelle oder sichtbare Debug-Hilfen verbleiben.

Lies zuerst die wirksamen `AGENTS.md`-Anweisungen, dann `Docs/Codex-Prompts/IMPLEMENT_WEEDTOPIA_BURNING_HILLS.md`, `Docs/Implementation/IMPLEMENTATION_STATUS.md`, `Docs/Implementation/VALIDATION_REPORT.md`, `Docs/ArtProduction/PROGRESS.json`, `Docs/ArtProduction/ART_DIRECTION.md`, `Docs/ArtProduction/2D_BUILDING_ART_PIPELINE.md`, `Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json` und die aktuellen Szenen, Prefabs, Quelltexte und Testresultate. Prüfe den tatsächlichen Stand erneut. Falls sich Dateien seit diesem Auftrag verändert haben, gilt der aktuelle Projektzustand; überschreibe keine fremde oder manuelle Arbeit.

## 1. Warum dieser Auftrag nötig ist

Der Stand vom 21./22. September 2026 zeigt einen funktionierenden lokalen Spielkern mit Bootstrap- und Burning-Hills-Szene, Produktion, Quests, Workern, UI, Speicherständen und Freeplay. Der letzte Bericht nennt 50/50 EditMode- und 10/10 PlayMode-Tests. Diese Zahlen sind historische Evidenz und müssen nach deinen Änderungen erneut geprüft werden.

Die aktuelle Game View wirkt trotzdem eindeutig wie ein Prototyp:

- `Docs/Implementation/Validation/Screenshots/01_intro_1920x1080.png` zeigt Rockos Einführung als Text über einer unfertigen Welt. Es gibt weder eine ausgearbeitete Rocko-Darstellung noch ein inszeniertes Onboarding.
- `03_farm_backlog.png` und `07_industry_freeplay.png` zeigen generische geometrische Gebäude und Ruinen, Kapsel-/Kugel-Figuren, schlichte Block-Bodenflächen, primitive Lieferwege, viele schwebende Textlabels und große ungenutzte dunkle Flächen.
- Die aktive UI besteht weitgehend aus einfarbigen Rechtecken und `LegacyRuntime.ttf`; Währungen und Karten nutzen Textzeichen statt einer konsistenten, eigens gestalteten Ikonografie.
- Im HUD steht sogar „Entwicklungsvisuals · Gebäude-Art wartet auf visuelle Freigabe“.
- `Assets/Weedtopia/Game/Content/DevelopmentVisuals.asset` und `Assets/Weedtopia/Game/Prefabs/Development_*.prefab` werden noch als aktive Weltvisuals verwendet. `GameScaffold.cs` baut diese aus Unity-Primitiven; `Resident.prefab` ist ebenfalls eine einfache geometrische Figur. Zwei Clips heißen `development_accept.wav` und `development_blocked.wav`.
- Dagegen zeigen `10_sprite_review_zoom26.png`, `12_townhall_zoom14.png` und `13_coffee_final_zoom14.png` vier viel hochwertigere isolierte 2D-Prototypen. Sie sind noch nicht im produktiven Spiel eingebunden.
- Die 251 einzeln akzeptierten Gebäudebilder liegen überwiegend unter `Docs/ArtProduction/Buildings`; nur vier freigestellte Prüf-Sprites liegen bislang unter `Assets`. Die produktiven 251 Wrapper wurden noch nicht auf diese 2D-Darstellung umgestellt.

Das Ziel ist **nicht**, vorhandene Funktionalität neu zu bauen. Das Ziel ist, die gesamte sicht- und hörbare Spielerfahrung auf Produktionsqualität zu bringen und danach den vollständigen Hauptpfad erneut nachzuweisen.

## 2. Verbindliche Qualitätsentscheidung

Die gewählte visuelle Richtung bleibt:

> Mockup 1 als warme, gut lesbare Basis; die modulare Retro-Industrie-Technik aus Mockup 4; die Blue-Hour-/Afterglow-Atmosphäre aus Mockup 5.

Nutze die drei kopierten Stilreferenzen unter `Docs/ArtProduction/References/ChosenStyle/`, die fünf Layoutreferenzen unter `Docs/ArtProduction/References/Layouts/`, `ART_DIRECTION.md` und die 251 akzeptierten Gebäudebilder. Das Spiel ist eine isometrische Unity-Welt mit 3D-Koordinaten, echten Interaktions- und Lieferwegen, aber 2D-/2.5D-Gebäudevisuals. Die frühere Meshy-/FBX-/Blender-Rekonstruktion ist archiviert und wird nicht reaktiviert.

Die Bildsprache muss in **jeder** Zone zusammenpassen: warm reparierte Architektur und Begrünung, Kupfer-/Messingtechnik, lesbare Förder- und Logistikwege, optimistischer Wiederaufbau, klare Silhouetten und atmosphärisches Abendlicht. Das Thema ist 420-angehaucht und augenzwinkernd; keine fotorealistische Drogenwerbung, Gewaltästhetik oder generische Cyberpunk-Neonwelt.

„Ohne Platzhaltergrafiken“ gilt für die ausgelieferte Spielkonfiguration absolut. Es reicht nicht, das Wort `Development` umzubenennen oder Quader mit besseren Farben zu versehen. Ungeformte Unity-Primitive, einfarbige Blöcke, Standard-Controls, Unicode-Symbole als Icons, Dummy-Figuren, leere Portraitfelder, Text-über-leerer-Fläche und fehlende Gebäudevisuals gelten weiterhin als Platzhalter.

## 3. Anfang: vollständiger Visual-Gap-Audit

Erstelle `Docs/ProductionFinish/VISUAL_ASSET_REGISTER.csv` mit **jedem im Hauptpfad sichtbaren** Asset oder Asset-Slot und mindestens diesen Feldern:

`id, category, scene_or_screen, current_asset, final_asset, source_or_provenance, status, style_qa, unity_integration, gameview_qa, owner_note`

Kategorien mindestens:

1. 251 Gebäudezustände und Ruinen,
2. Rathaus und alle Zonengebäude im Tutorial,
3. Boden, Wege, Mauern, Parzellen, Gewässer/Vegetation, Stadtgrenzen und Hintergrund,
4. Förderbänder, Leitungen, Lieferwagen, Ladung, Verladestellen und Statusanzeigen,
5. Rocko, drei Farmbewohner, Stadt-/Siedlungs-/Industrie-Questgeber und Worker-Darstellungen,
6. Intro, Dialogportraits und erzählerische Bilder,
7. HUD, Menüs, Panels, Buttons, Slider, Dropdowns, Listen, Auswahlzustände und Overlays,
8. Icons für Geld, Grinder, Karten, Ressourcen, Workerrollen, Seltenheit, Quests und Zonen,
9. Partikel, Beleuchtung, Schatten, Auswahl-/Upgrade-/Lieferfeedback,
10. Audioelemente, soweit sie in der aktuellen Fassung hörbar sind.

Klassifiziere ehrlich als `production_ready`, `approved_source_not_integrated`, `prototype`, `missing` oder `archived`. Screenshots und vorhandene Dateinamen sind nicht allein ein Freigabebeleg. Erstelle ein priorisiertes Gap-Dokument mit konkreten Pfaden und einer Screenshot-Baseline. Trenne fachliche Testabnahme von visueller Abnahme.

## 4. Produktion der endgültigen Assets

### 4.1 Gebäude: alle 251 Zustände

- Verwende für jede Gebäudestufe das **zugehörige** akzeptierte Quellbild aus dem Manifest. Keine Wiederverwendung eines T01-Bildes für höhere Level und kein generischer Ersatz für eine fehlende Stufe.
- Freistellen und Sprite-Aufbereitung sind pro Slot überprüfbare Produktionsschritte: vollständige Silhouette, saubere Kanten, keine Hintergrundreste, keine abgeschnittenen Dächer/Pflanzen, kein Schachbrettmuster und keine inkonsistenten gebackenen Schatten.
- Erhalte Frontausrichtung, Bodenpivot, relative Maßstäbe, reservierte Stellfläche und die Familien-DNA über alle Tiers.
- Importiere mit passenden Einstellungen für Zielkamera und Zielplattform. Schärfe bei Nah-/Fernzoom, Kompression, Mipmaps, Alpha-Fringing und Texturspeicher sind zu prüfen, nicht zu raten.
- Integriere die finalen Sprites unter `VisualRoot` der vorhandenen Wrapper. Erhalte `InteractionAnchor`, `WorkerAnchor`, Lieferanker, `VfxAnchor`, `ColliderRoot`, manuelle Offsets und bestehende Quellassets.
- Verwende den vorhandenen `BuildingVisualCatalog` als produktive Levelzuordnung. Der aktive Spielpfad darf nicht dauerhaft auf `DevelopmentVisuals.asset` oder einer „wenn nicht vorhanden, zeige Quader“-Logik beruhen.
- Upgrades müssen in der laufenden Game View die passende neue Silhouette zeigen und Interaktion/Collider/Worker-/Lieferpunkte konsistent halten.
- Alle 251 Tiers brauchen einen exakten Asset- und Unity-QA-Eintrag; mindestens die in `ART_DIRECTION.md` vorgeschriebenen Stichproben zusätzlich eine tatsächliche Sichtprüfung. Ein Hash allein beweist keine Artqualität.

**Bestehendes Freigabegate:** `PROGRESS.json` hält fest, dass Rathaus sowie früher, mittlerer und finaler Coffee-Shop-Prototyp Game-View-bereit sind, die ausdrückliche visuelle Nutzerfreigabe aber fehlt. Die Bitte um ein professionelles Spiel ist noch **keine** Freigabe dieser konkreten vier Ansichten. Bereite eine kompakte, gut vergleichbare Freigabevorlage mit Screenshots in der tatsächlichen Spielkamera vor. Arbeite an allen anderen erlaubten Assets und Systemen weiter. Ändere die 251 produktiven Wrapper/Katalogreferenzen erst nach dieser ausdrücklichen Freigabe. Bleibt sie aus, melde die Endabnahme als offen; liefere niemals eine Platzhalter-Version als „fertig“ aus.

### 4.2 Welt und Zonen

Gestalte Farm, Stadt, Siedlung und Industriegebiet als zusammenhängendes Burning Hills, nicht als vier isolierte, flache Testplatten. Ersetze sichtbare Primitive durch authorierte, wiederverwendbare Produktions-Prefabs und passende Texturen/Sprites/Meshes. Erforderlich sind mindestens:

- ein bewusst komponierter Boden mit Wegen, Parzellenkanten, Reparaturspuren, Begrünung und klaren Zonenübergängen,
- optisch nachvollziehbare Förderband- und Straßenverbindungen entlang des tatsächlichen Ressourcenflusses,
- gestaltete Verladestellen, Lieferfahrzeuge und Ladungsanzeigen,
- passende Ruinen, Baustellenzustände und Upgrade-VFX,
- wiederverwendbare, zur Stilrichtung passende Bäume, Pflanzen, Schrott-/Wiederaufbau-Props, Leuchten, Schilder und kleine belebende Details,
- Hintergrund und Weltabschluss, damit Kamera-Pan und Zoom keine leeren dunkelblauen Flächen oder rohe Plattenkanten enthüllen,
- Tages-/Abendlicht, warme Fenster, Bodenkontakt und Sorting, sodass die 2D-Gebäude glaubwürdig in der 3D-Welt stehen.

Die Skizzen definieren die räumlichen Beziehungen, nicht den finalen Zeichenstil. Dekoration darf Produktionswege, Figuren, Klickflächen oder Lesbarkeit nicht verdecken. Ein komplettes Stadtbild muss auch auf dem weitesten spielbaren Zoom lesbar bleiben.

### 4.3 Figuren und Story-Präsentation

- Rocko erhält eine eigenständige, charaktervolle Weltfigur und mindestens ein fertiges Dialogportrait mit wiedererkennbarem Outfit und Ausdruck. Die Einleitung darf ihn nicht nur als beschreibenden Text nennen.
- Alle Questgeber erhalten passende, unterscheidbare Weltvisuals und Dialogportraits beziehungsweise ein konsistentes Portraitsystem; keine Kapsel-/Kugel-Menschen oder leeren Avatarfelder.
- Workerlisten und Spin zeigen fertig gestaltete Rollen-/Seltenheitsbilder, Karten und Ergebnisanimationen. Wiederverwendbare Portraitkomponenten sind erlaubt, solange jede sichtbare Kombination erkennbar und fertig wirkt.
- Leichte Idle-/Talk-/Work-/Walk-Animationen oder äquivalente 2.5D-Bewegung verleihen der Welt Leben. Keine starre Schaufensterstadt.
- Dialoge, Quests und Kamerafokus inszenieren Rockos Eröffnung sowie die Besuche der vier Zonen verständlich und zügig.

### 4.4 UI und Ikonografie

Ersetze die sichtbare Standard-uGUI-/Legacy-Font-Anmutung durch ein einheitliches, wiederverwendbares Interface-System:

- geeignete, eingebundene und rechtlich nutzbare Schrift(en) mit sauberer deutscher Darstellung,
- gestaltete, gern neunfach skalierbare Panels und Buttons mit konsistenten Abständen, Farben, Schatten, Rahmen und Fokus-/Hover-/Pressed-/Disabled-Zuständen,
- echte Icons für Geld, Grinder, Karten, Ressourcen, Worker, Seltenheiten, Zonen und Quests statt Unicode-Zeichen oder generischer Quadrate,
- klare visuelle Hierarchie: Welt bleibt sichtbar, Quest und nächster Schritt sind erkennbar, Detailpanel erscheint bei Auswahl, Spezialaktionen nur im passenden Kontext,
- Intro-/Dialogfenster mit Portrait, Questbelohnung, gutem Rhythmus und eindeutiger Interaktion,
- Worker-, Rathaus-, Statistik-, Journal-, Speicher- und Einstellungsansichten mit demselben Designsystem,
- professionelle Rückmeldungen für Kauf, fehlende Mittel, Grinderbestätigung, Lieferung, Lagerstau, Questabschluss und Upgrade.

Prüfe Lesbarkeit und Touch-Zielgrößen bei mindestens 1920×1080, 1366×768 und 2560×1080. Belege, dass keine Texte abgeschnitten sind, Panels nicht überlappen, keine UI-Elemente unter Safe Areas verschwinden und der Weltblick nicht dauerhaft von Rohdaten überdeckt wird. Spielerrelevante Werte bleiben zugänglich, aber schwebende Debug-/Leveltexte werden auf ein bewusstes, stilgerechtes Maß reduziert.

### 4.5 Bewegung, VFX und Audio

- Animationen und Partikel müssen den tatsächlichen Simulationszustand widerspiegeln: Ernte, Bandlauf, Stau, Be- und Entladen, Verkauf, Upgrade, Tagesbonus und Questabschluss.
- Nutze Pooling und gemeinsame Materialien; erzeugte Effekte dürfen den Idle-Spielbetrieb nicht belasten.
- `development_accept.wav` und `development_blocked.wav` dürfen nicht als finales Audiodesign ausgegeben werden. Ersetze sie durch passende, rechtlich sauber dokumentierte Sounds und eine konsistente Hintergrundmusik/Atmosphäre oder kennzeichne Audio ausdrücklich als offenen Release-Blocker. Keine zufälligen Stockclips, OS-TTS oder fremde Lizenzen ohne Nachweis.
- Keine kostenpflichtigen Dienste oder externen Käufe ohne Zustimmung. Falls originale Bild-/Audioerzeugungstools verfügbar sind, nutze sie innerhalb der geltenden Skill- und Toolregeln. Ist eine notwendige Produktionsquelle nicht zugänglich, dokumentiere genau das fehlende Asset und frage nur für diese konkrete Lücke nach Entscheidung. Fülle sie nicht mit einem Quader.

## 5. Erhalt des funktionierenden Spiels

Arbeite additiv. Die bestehende Domain-/Quest-/Worker-/Save-/Offline-Logik, IDs, 16 authorierten Instanzen, Levelgrenzen und visuellen Bandzuordnungen müssen erhalten bleiben. Neue Visuals dürfen keine Währungen duplizieren, Quests neu starten, Gebäudeauswahl verlieren, Lieferungen unterbrechen oder Savegames unlesbar machen.

Statische Welt, feste NPCs, UI-Hierarchie, Lieferwege und Bauplätze werden als Szenen/Prefabs authoriert, nicht unsichtbar pro Start aus Code zusammengesetzt. Wiederholte Objekte werden Prefabs. Das explizite `Weedtopia > Setup > Create or Repair Game Scaffold` muss neue Produktionsassets respektieren und darf sie bei erneutem Lauf nicht durch `Development_*`-Objekte ersetzen.

Behalte die Entwicklungsassets für Diagnose in einem klar bezeichneten Archiv, aber entferne sie nach vollständiger Ablösung aus dem produktiven Bootpfad und den Build-Szenen. Lösche keine Quellen ohne sichere Bestandsprüfung. Keine Commits, Pushes, Deployments, Backends oder Produktivserviceänderungen ohne gesonderten Auftrag. Bei offenem Unity Editor keine zweite Instanz starten und keine fremden Editorprozesse schließen.

`Assets/Scenes/SampleScene.unity` steht derzeit noch in den Build Settings. Erhalte die Datei, aber nimm sie aus der finalen Spiel-Buildliste, sofern sie weiterhin nur das Unity-Standardsample ist. Die produktive Buildliste darf nur tatsächlich benötigte Burning-Hills-Szenen enthalten.

## 6. Technische Qualitätssicherung gegen Platzhalter

Implementiere einen expliziten, wiederholbaren **Production Visual Preflight** als Editor-Menü und Test. Er muss die tatsächlich gebauten Szenen, Prefabreferenzen, aktiven Visual-Provider, Game-Content-Definitionen und erreichbaren UI-Pfade prüfen. Für den Release-/Production-Modus muss er fehlschlagen, wenn eines davon zutrifft:

- sichtbare Referenz auf `Development_*`, `DevelopmentVisuals.asset`, Review-Prefabs oder `development_*.wav`,
- eine Sample-/Testszene als erreichbarer Teil des Produkt-Builds,
- aktiver generischer Primitive-Fallback für Gebäude, NPCs, Fahrzeuge oder Weltprops,
- fehlendes final integriertes Visual für einen der 251 Gebäude-Slots,
- fehlendes Portrait oder Icon in einem erreichbaren Dialog/Panel,
- leeres Sprite, rosa Material, Missing Script, fehlende Pflichtreferenz oder unsichtbarer Collider,
- sichtbar eingeblendeter Entwicklungs-/Debugtext,
- Bild-/Text-/UI-Overflow an den geprüften Auflösungen,
- ein visueller Zustand ohne Art-Freigabe, der trotzdem als Produktionszustand markiert wurde.

Ein separates Entwicklungsprofil darf fehlende Assets sichtbar diagnostizieren. Der produktive Spielstart darf keinen unauffälligen Platzhalter nachladen und dann dennoch „Production Ready“ melden. Liste bei Fehlschlag exakt die betroffenen Szenen, Objektpfade, Slots und fehlenden Dateien.

Zusätzliche Prüfungen:

- Wechsel an allen Levelbandgrenzen und Rückkehr per Save/Load,
- 251 Tier-Referenzen ohne Lücke oder Doppelbelegung,
- Klickflächen, Sorting, Pivot, Schatten und Lieferanker nach Visualwechsel,
- keine unnötige gleichzeitige Ladung aller 251 hochauflösenden Texturen,
- Texture-/GPU-Speicher, Framezeit, GC und Draw Calls in Farm, Gesamtübersicht und Upgrade-Moment unter einer dokumentierten Zielplattform,
- keine Zunahme von unbehandelten Exceptions oder Testfehlern.

## 7. Visuelle Abnahme als echte Spielerfahrung

Erzeuge nach jeder größeren visuellen Etappe **aktuelle** Game-View-Screenshots und überprüfe sie tatsächlich. Stelle Baseline und neue Ansicht nebeneinander. Zusätzlich zu automatischen Tests führe den Hauptpfad manuell oder nachweisbar bedient durch; automatisierte Screenshots ersetzen weder menschliches Gameplay noch Zielgeräteprüfung.

Pflichtansichten:

1. Start-/Intro-Szene mit Rocko und lesbarem Dialog,
2. Farm vor und nach Automatisierung, inklusive Stau,
3. Stadt und Rathaus,
4. Siedlung mit gebautem Wohnhaus und Park,
5. Industriefluss,
6. Freeplay-Gesamtübersicht,
7. Gebäude-Upgrades bei Ruine, frühem, mittlerem und finalem Tier,
8. Worker-Karte, Spin, Workerzuweisung,
9. HUD/Detailpanel, Premiumbestätigung, Questjournal und Einstellungen,
10. dieselben relevanten Ausschnitte bei Nah-/Fernzoom und den oben genannten Auflösungen.

Prüfe pro Bild: klare Blickführung, Wiedererkennbarkeit der Zonen, Style-Kohärenz, Lesbarkeit, Schatten/Bodenkontakt, korrekte Sortierung, richtige Auflösung, keine sichtbaren Primitive, keine leeren Flächen oder Panels, kein Debugtext, keine abgeschnittenen Grafiken und keine unfertigen Effekte. Dokumentiere erkannte Mängel und korrigiere sie, statt Screenshots nur abzulegen.

Benutzerfreigaben, Unity-Import, EditMode, PlayMode, Game View, physisches Touchgerät, Audio-Abhören und Zielplattform-Build sind verschiedene Gates. Berichte jedes getrennt. Die vier isolierten 2D-Gebäudeprototypen brauchen noch ausdrückliche Nutzerfreigabe vor der produktiven 251er-Umstellung. Wo das Gate extern offen bleibt, setze alle anderen in-scope Verbesserungen fort, markiere den Release aber nicht als fertig.

## 8. Reihenfolge und Fortschrittsdokumentation

1. **Audit:** Reale Visual- und Audio-Lücken inventarisieren, Quellen sichern, aktuelle Tests/Baselines prüfen.
2. **Art-System:** Maßstab, Kamera, Beleuchtung, Palette, UI-Designsystem, Produktions-Prefab-Verträge und Asset-Budgets konkret festlegen.
3. **Qualitätsanker:** Eine zusammenhängende Farm-/Stadt-Spielansicht mit fertigem Boden, einem Gebäude, Rocko/NPC, Logistik, HUD und Dialog zu einer überzeugenden Referenz ausarbeiten. Game-View-Nachweis und gezielte visuelle Nutzerfreigabe der vorhandenen vier Gebäudeprototypen vorbereiten.
4. **Vollständiger Ausbau:** Nach dem Freigabegate alle 251 Gebäudezustände integrieren; alle übrigen Zonen, Figuren, Props, UI-Icons, Panels, VFX und Audio vervollständigen. Nicht beim Vertical Slice stoppen.
5. **Produktionsprüfung:** Platzhalter-Preflight, aktuelle EditMode-/PlayMode-Tests, Game-View-Matrix, Save-/Upgrade-/Lieferfluss, Performance und menschliche Bedienung ausführen; Befunde korrigieren.
6. **Handoff:** `Docs/ProductionFinish/STATUS.md`, Assetregister, Provenienz/Lizenzen, Qualitätsentscheidungen, aktuelle Screenshots und `Docs/ProductionFinish/VALIDATION_REPORT.md` liefern.

Der Status muss nach jedem Meilenstein unterscheiden zwischen `implementiert`, `in Unity importiert`, `automatisch getestet`, `in Game View visuell geprüft`, `vom Benutzer freigegeben` und `auf Zielgerät geprüft`. Keine Zahl und kein Screenshot ersetzt ein anderes Gate. Behalte die Hauptaufgabe im Blick und arbeite weiter, solange ein sicherer nächster Schritt ohne neue Zustimmung möglich ist.

## 9. Fertigdefinition

Melde Burning Hills nur dann als professionell spielbar und visuell fertig, wenn:

- Farm, Stadt, Siedlung und Industrie sowie der Hauptpfad bis Freeplay weiterhin funktionieren,
- der aktive Spielpfad kein `Development_*`-Visual, keine Primitive-Figur, keinen Roh-UI-Standardlook und keinen sichtbaren Platzhalter mehr enthält,
- alle 251 Gebäudezustände aus den freigegebenen Quellen korrekt und performant im Spiel eingebunden sind,
- Rocko, Questgeber, Worker, Fahrzeuge, Weltprops, UI, Icons, Dialoge und Feedback einen kohärenten endgültigen Stil besitzen,
- Intro, Welt, UI und Upgradezustände bei den Pflichtauflösungen tatsächlich angesehen und korrigiert wurden,
- der Production Visual Preflight vollständig besteht,
- aktuelle Source-/Unity-Import-/EditMode-/PlayMode-/Game-View-Ergebnisse dokumentiert sind,
- Savegames und vorhandene Systeme durch den visuellen Austausch nicht regressieren,
- die ausdrücklich erforderliche Nutzerfreigabe für die 2D-Massenintegration vorliegt,
- keine offenen Platzhalter durch „vorläufig“ oder „später“ aus der Abnahme herausdefiniert wurden.

Falls eines dieser Kriterien noch offen ist, benenne den konkreten Blocker und den kleinsten nächsten Schritt. Ein spielbarer Kern mit Entwicklungsvisuals ist **kein** abgeschlossenes professionelles Spiel.

Beginne jetzt mit dem Audit, sichere den Bestand und setze die Produktions-Finish-Arbeit um. Halte nicht nach einer Konzeptskizze, einer einzelnen polierten Szene oder einer Testzahl an.
