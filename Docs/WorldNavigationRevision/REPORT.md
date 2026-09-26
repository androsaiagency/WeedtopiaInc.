# Freie Welt-Navigation und Schnellzugriffe

Die Karte lässt sich mit einem Finger, linkem Mausziehen oder den vorhandenen Richtungstasten frei in beiden Achsen bewegen. Die ehemaligen Stadtteilachsen dienen weiterhin den optionalen Bereichsmarken, begrenzen die freie Bewegung jedoch nicht mehr. Zwei Finger, Mausrad und die bisherigen Zoomtasten können aus jedem Bereich bis zur Gesamtübersicht herauszoomen.

Am unteren Bildschirmrand stehen mittig fünf runde Buttons in der Reihenfolge **Farm, Stadt, Siedlung, Industrie, Aufträge**. Sie verwenden die vorhandenen Spiel-Icons ohne Textbeschriftungen. Die ersten vier zentrieren ihren Bereich auch nach vorherigem freien Wischen erneut. Die Aufträge öffnen das vorhandene Journal. Rathaus, Worker-Inventar, Übersicht, Speichern, Einstellungen/Pause und die übrigen Funktionen bleiben im eingeklappten Menü.

## Umsetzung

- `Assets/Weedtopia/Production/Prefabs/WorldNavigationButton.prefab`: wiederverwendbarer runder Button mit goldener Kontur, türkisem Hintergrund, Icon und kreisförmiger Trefferfläche.
- `Assets/Weedtopia/Production/Prefabs/WorldNavigationDock.prefab`: fünf Instanzen des Button-Prefabs, unten mittig verankert.
- `WorldNavigationBar`: Sichtbarkeit im laufenden Spiel und Anpassung an den sicheren Bildschirmbereich. Modale Dialoge haben Vorrang; der Gebäudeinspektor lässt die Leiste sichtbar.
- `DistrictMap`: im Inspector editierbare gemeinsame Kartenränder, Plan-Koordinaten `(-330, -340)` bis `(700, 475)`.
- `IsometricCameraRig`: freie Bewegung, einheitliche Zoomgrenzen und stabile Zielkoordinaten ohne Drift bei UI-Bedienung.
- `GameUi`: vorhandene Aktionen auf die neuen serialisierten Button-Referenzen gebunden. Eine Bereichsansicht verändert weder Spielfortschritt noch Gebäudefreischaltungen.
- Speichermeldungen liegen oberhalb der Leiste und fangen keine Welt-Eingaben ab.

Die UI ist in der Spielszene und in Prefabs gespeichert. Der explizite, wiederholbare Editor-Befehl lautet **Weedtopia → Presentation → Install World Navigation**. Er erzeugt keine statische UI im laufenden Spiel. Die bisherigen fünf Menübuttons sind deaktiviert archiviert; die Originaldateien vor dieser Änderung liegen unter `Originals/`.

## Nachweise

- Native Unity-PlayMode-Tests: **13/13 bestanden**, keine Fehler und keine übersprungenen Tests. Ergebnis: `../Implementation/Validation/20260926_113742_PlayMode_WorldNavigationFinal.xml`.
- Native Unity-EditMode-Assettests: **25/25 bestanden**, keine Fehler und keine übersprungenen Tests. Ergebnis: `../Implementation/Validation/20260926_113841_EditMode_WorldNavigationAssets.xml`.
- Geprüft wurden diagonale Touchbewegungen aus allen vier Bereichen, Wischen über den Fluss, das Erreichen sämtlicher Kartenränder, echte Eingabeverarbeitung virtueller Touch-/Mausgeräte, die fünf Button-Aktionen, unveränderte Freischaltungen, UI-Eingabesicherheit, Pinch-Zoom, Gebäudeauswahl und die bestehende Worker-/Fahrzeuginteraktion.
- `preservation.json`: alle ursprünglichen Szenenobjekte erhalten. Die 56 Gebäudekomponenten, 60 Fahrzeugkomponenten, 44 Routenkomponenten, 50 Richtungsanimatoren und die übrige Weltpräsentation sind gegenüber der Sicherung unverändert; die vorhandene `GameUi`-Komponente enthält die neuen UI-Verweise.

- **11 tatsächliche Game-View-Aufnahmen** liegen unter `Screenshots/`: Weltübersicht, vier Bereichsansichten, Aufträge, restliches Menü, zusätzliche Bildschirmformate und Gebäudeinspektor. `gameview_receipt.json` bestätigt den abgeschlossenen nativen Aufnahmelauf.
- Die Leiste wurde visuell in **1920×1080, 1366×768 mit UI-Skalierung 115 %, 2560×1080 und 1080×1920** geprüft. Die automatische Prüfung aller aufgenommenen Ansichten meldet **keine Textüberläufe und keine Bedienelemente außerhalb des sicheren Bildschirmbereichs** (`ui_overflow.json`).
- Die gespeicherten Weltkomponenten wurden auch nach den nativen Tests nochmals gegen die Sicherung geprüft. Alle eigenen Unity-Batchinstanzen sind beendet.

Ein physisches Touchgerät wurde nicht bedient; die Touch-Nachweise stammen aus Unitys Input System mit virtuellen Geräten. Die Aufnahmen dokumentieren den technischen und visuellen Stand, keine Abnahme durch den Nutzer.

![Freie Karte mit den fünf runden Schnellzugriffen](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/WorldNavigationRevision/Screenshots/01_overview.png>)
