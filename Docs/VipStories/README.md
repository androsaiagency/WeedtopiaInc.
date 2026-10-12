# VIP-Stadtgeschichten

Die zehn VIPs werden mit der Einwohnerzahl dauerhaft in Burning Hills freigeschaltet. Sie sind anklickbare, in Prefabs angelegte Stadtbewohner. Über **Aufträge → Stadtgeschichten** sind ihre Geschichten ebenfalls erreichbar. Die Übersicht zeigt noch gesperrte Bewohner und ihre Einwohnergrenze.

## Bewohner und Fortschritt

| Bewohner | Rolle | Ankunft | Persönliche Kapitel bei Einwohnerzahl |
|---|---|---:|---|
| Toni „Express“ Faber | Kurier | 50 | 50 / 250 / 1.000 / 3.000 |
| Juno Vega | Musikerin | 100 | 100 / 400 / 1.500 / 4.000 |
| Rosa „Munchies“ Bellini | Cafébetreiberin | 200 | 200 / 600 / 2.000 / 5.000 |
| Dr. Mira Grün | Botanikerin | 350 | 350 / 1.000 / 2.500 / 6.000 |
| Kalle „Chrom“ König | Mechaniker | 500 | 500 / 1.250 / 3.000 / 6.500 |
| Alma Morgen | Architektin | 750 | 750 / 1.750 / 3.500 / 7.000 |
| Selma „Scoop“ Winter | Journalistin | 1.000 | 1.000 / 2.500 / 4.500 / 7.500 |
| Oskar Fundstück | Sammler | 1.500 | 1.500 / 3.000 / 5.000 / 8.000 |
| Dalia „Nachtfalter“ Noor | Veranstalterin | 2.000 | 2.000 / 3.500 / 6.000 / 9.000 |
| Valentina Vale | Investorin | 3.000 | 3.000 / 4.500 / 7.000 / 10.000 |

Jede Person hat eine eigene Biografie, Begrüßung, vier aufeinanderfolgende Kapitel und drei rotierende Alltagsaufträge. Einzelne Kapitel bieten zwei Entscheidungen; die gewählte Antwort wird gespeichert und verändert den Erinnerungstext sowie das zugehörige sichtbare Objekt bei der Person. Diese Entscheidungen ändern keine Produktionsformeln.

Bereits erreichte Einwohnergrenzen bleiben gespeichert. Ein freigeschalteter Bewohner bleibt daher auch nach sinkender Einwohnerzahl in der Stadt. Höhere Einwohnerzahlen überspringen keine Kapitel. Zwischen den Kapiteln und nach der persönlichen Geschichte kehren Alltagsaufträge zurück. Sie werden frühestens 300 Simulationssekunden nach einem abgeschlossenen Alltagsauftrag angeboten. Ihre Produktions- und Liefermengen sowie Geldprämien wachsen mit der erreichten Stadtgröße bis zum Faktor zehn. Ein verfügbares persönliches Kapitel hat Vorrang.

Es gibt höchstens drei gleichzeitig angenommene Aufträge, einschließlich Gemeinschaftsprojekten. Aufträge verfallen nicht. Produktions-, Liefer- und Gesprächsziele zählen neue Aktivitäten ab Annahme; Gebäudestufen zählen den aktuellen Zustand. Offline-Simulation kann Produktions- und Lieferziele erfüllen. Belohnungen müssen anschließend ausdrücklich abgeholt werden und werden pro Abschluss genau einmal ausgegeben.

## Gemeinschaftsprojekte

| Projekt | Einwohner | Beteiligte / Voraussetzung |
|---|---:|---|
| Ein Abend unter Lichterketten | 1.500 | Juno, Rosa, Toni; ihre ersten Kapitel |
| Die Geschichte im alten Wagen | 3.000 | Kalle, Oskar, Selma; ihre ersten Kapitel |
| Ein Garten für alle | 5.000 | Mira, Alma, Rosa; ihre ersten Kapitel |
| Unsere erste große Ausstellung | 8.000 | Oskar, Selma, Juno; ihre ersten Kapitel |
| Zehn Stimmen, eine Stadt | 10.000 | Alle zehn Bewohner; sämtliche vierten Kapitel |

Die verbindlichen Titel, Texte, Ziele und Belohnungen stehen im Inhaltskatalog; die Kurzbezeichnungen hier dienen als Übersicht. Erinnerungsstücke sind dauerhaft über den Geschichtenbereich lesbar, auch wenn ältere Belohnungsbelege aus der begrenzten Belohnungshistorie herausfallen. Die musikalischen Ereignisse sind erzählte Inhalte; dieser Ausbau enthält keine neuen Musik- oder Sprachaufnahmen.

## Aufbau und Pflege

- `Assets/Weedtopia/Game/Content/BurningHills.json`: Daten und deutsche Texte; bestehende Inhalte bleiben erhalten.
- `Tools/Implementation/author_vip_stories.py`: nachvollziehbare Quelle der zehn Biografien, 40 Kapitel, 30 Alltagsaufträge und fünf Gemeinschaftsprojekte. Neue VIP-Texte werden ergänzt; bestehende Textwerte werden nicht überschrieben.
- `Scripts/Runtime/Domain/VipStories.cs`: Daten, Fortschrittszustände und Inhaltsprüfung ohne Unity-Abhängigkeit.
- `Scripts/Runtime/Application/VipStoryService.cs`: Freischaltung, Angebot, Annahme, Ziele, Abschluss und Erinnerungen.
- `Scripts/Runtime/Persistence/VipStorySaveValidator.cs`: strenge Speicherprüfung und Wiederherstellung vollständig leerer optionaler Klassen nach Unity-JSON-Serialisierung.
- `GameSession.TransactStory`: Speichertransaktion mit Rücknahme von Questzustand, Geld, Inventar und Belohnungsbelegen bei Schreibfehlern.
- `Scripts/Runtime/Unity/VipStoriesUi.cs`, `VipStoryActor.cs`, `VipTownPresentation.cs`: austauschbare Präsentation und Eingabe mit serialisierten Referenzen.
- `Assets/Weedtopia/Production/VipStories`: eigener visueller Katalog, zehn Illustrationen, gemeinsame NPC- und Zeilen-Prefabs, Personenvarianten, Stadt- und UI-Prefab.

Die Menüaktion **Weedtopia → Stories → Install or Repair VIP Stories** ergänzt ausschließlich fehlende eigene Assets und Verweise. Sie ersetzt keine vorhandenen VIP-Prefabs und läuft nicht automatisch bei Import oder Domain Reload. Vorhandene Gebäude, Landschaft, Kameraführung, Audio, Charaktere und andere Auftragssysteme werden nicht neu aufgebaut. Alle festen NPCs, Erinnerungselemente, 15 Übersichtszeilen, sechs Zielzeilen und der feste Journal-Einstieg sind in Prefabs sichtbar angelegt. Laufzeitcode steuert Sichtbarkeit, Texte und Interaktion. Der Journal-Einstieg verwendet das vorhandene Listenzeilen-Prefab als Basis und wird beim Aktualisieren des Journals erhalten.

**Weedtopia → Stories → Refine VIP Presentation** ist eine gesonderte, explizite Aktion. Sie ergänzt fehlenden Textabstand im eigenen Geschichten-Prefab, richtet das gemeinsame Questzeichen als Kind der zur Kamera ausgerichteten Illustration aus und setzt die zehn eigenen NPC-Standorte auf die hier geprüften freien Positionen. Diese Aktion darf nicht zur Erhaltung späterer manueller VIP-Standortkorrekturen verwendet werden, da sie die Standorte bewusst auf den geprüften Ausgangswert zurücksetzt. Wiederholte unveränderte Aufrufe erhalten Dateien und GUIDs.

Die zehn transparenten PNG-Illustrationen wurden mit dem eingebauten ImageGen-Werkzeug erstellt. `art_prompts.json` hält die genauen Beschreibungen und Herkunftspfade fest. Ein Sprite-Import liefert jeweils Ganzkörperdarstellung und Porträtausschnitt. Erinnerungsobjekte verwenden vorhandene Stadt- und Requisiten-Prefabs.

## Prüfen

Die Runner verweigern den Start, sobald ein anderer Unity-Editor oder eine Projektsperre vorhanden ist. Sie starten ausschließlich einen eigenen versteckten Batch-Editor und erhalten bestehende Projekteinstellungen. Diagnosebilder und Tests verwenden eigene Spielstände, nicht den Spieler-Spielstand.

```powershell
# Explizite Installation; zweiter Lauf muss bestehende Dateien erhalten.
& Tools/Implementation/Run-VipStories.ps1 -Mode Install -Label VIP

# Regeln, Speichern, Produktion, bestehende Spielsysteme.
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Label VIP -Assembly Weedtopia.Game.EditModeTests

# Prefab-Vollständigkeit und bytegenaue Idempotenz.
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Label VIPAssets -Assembly Weedtopia.Game.AssetTests -TestFilter Weedtopia.Tests.Assets.VipStoryAssetTests

# Wirkliche Szene, NPC-Auswahl, UI, Belohnung, Continue und Pointer-Raycasts.
& Tools/Implementation/Run-UnityValidation.ps1 -Mode PlayMode -Label VIPUI -Assembly Weedtopia.Game.PlayModeTests -TestFilter Weedtopia.Tests.PlayMode.VipStoryUiTests

# Gerenderte Game-View-Bilder, mehrere Bildschirmformate.
& Tools/Implementation/Run-VipStories.ps1 -Mode Capture -Label VIP

# Vergleich mit der vor Umsetzung gesicherten Szene und den geschützten Assets.
python Tools/Implementation/vip_story_preservation.py verify
```

Tests und Bildnachweise belegen jeweils nur ihre eigene Prüfart. Automatische Pointer-Raycasts ersetzen keinen manuellen Maus- oder Touch-Test auf einem Zielgerät. Gerenderte Diagnosebilder stellen keine gestalterische Freigabe durch den Nutzer dar.

Der abgeschlossene Prüfstand mit konkreten Berichten wird in `validation_summary.json` festgehalten.
