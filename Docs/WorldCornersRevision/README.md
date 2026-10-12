# Neue Landschaftsorte an den Kartenrändern

Die Erweiterung ist in `Assets/Weedtopia/Game/Scenes/BurningHills.unity` gespeichert. In der Hierarchie liegt sie unter `BurningHills_Districts/CountrysideCorners`.

- **West_OrchardAndCiderPress:** Obstgarten mit Apfelpresse, Erntebäumen, Imkerplatz, Honigwagen und Anschluss an die Siedlung.
- **East_ConservatoryAndLookout:** Botanischer Garten mit Glashaus, Aussichtspavillon, Blumen- und Kräuterbeeten, Leseplatz und Teeecke. Ein Spazierweg verbindet ihn über den vorhandenen Teichweg mit der Farm.
- **West_MeadowPicnic:** Erweiterung des bestehenden Parks um eine Picknickwiese mit Obstbäumen und Honigwagen.
- **OrchardVisitorsAndGardenWildlife:** Zehn zusätzliche Spaziergänger und sechs Schmetterlinge nutzen den bestehenden gemeinsamen Präsentations- und Animationsablauf. Es gibt keinen zweiten Simulations- oder Animations-Taktgeber.

Alle statischen Inhalte sind in der Szene vorhanden. Wiederverwendbare Elemente sind Prefab-Instanzen. Es werden beim Spielstart keine Dekorationsobjekte erzeugt. Bauplätze, Wirtschaft, Speicherformat und Navigationsgrenzen bleiben unverändert.

## Grafiken

Sechs neue Einzelmotive wurden mit dem eingebauten `image_gen`-Werkzeug erzeugt: Apfelpresse, Glashaus, Aussichtspavillon, Apfelbaum, Schmetterlingsbeet und Honigwagen. Die ursprünglichen PNG-Dateien werden unverändert unter `Assets/Weedtopia/Production/WorldCorners/Art` gespeichert und durch sechs Prefabs unter `Prefabs` eingebunden. Das Blumenbeet wurde vor dem Import mit Imagegen freigestellt. Die vollständigen Prompts und Quellpfade stehen in `generation_sources.json`; Alphakanal, Sprite-Rechtecke und SHA-256-Werte stehen in `alpha_validation.json`.

111 Dekorationsinstanzen kombinieren die sechs neuen Motive mit vorhandenen Garten-Prefabs. Die neuen Wege sind interpolierte Kurven durch die geplanten Anschlusspunkte; Spaziergänger und Wegoberflächen verwenden dieselben Punkte. Im ersten Spielansicht-Durchlauf wurden die Pflanzvarianten, die Position des Pavillons unterhalb des Menübereichs und die Größe des Glashaus-Vorplatzes verbessert.

## Werkzeuge und Erhaltung

- Einmaliger, idempotenter Editor-Befehl: `Weedtopia > Production > Author Countryside Corners`.
- `WorldCornersAuthoring.RefineOwned` dient ausschließlich der ausdrücklichen Bearbeitung dieser neuen Gruppe in einem eigenen Batch-Editor. Bestehende Gruppen werden nicht ersetzt.
- `Tools/Implementation/prepare_world_corners.py` kopiert die Original-PNGs unverändert und erstellt den Importmanifest.
- `Tools/Implementation/verify_world_corners_preservation.py` vergleicht jeden ursprünglichen Szenenblock mit der Sicherung in `Originals/BurningHills.unity`.
- Die Vergleichsprüfung verlangt den Erhalt aller 8.571 ursprünglichen Szenenobjekte. Nur die Welt-Kindliste und das vorhandene Figurenarray dürfen zusätzliche Einträge bekommen; keine ursprünglichen Zeilen dürfen entfernt oder geändert werden.

`integration.json`, `preservation.json`, `validation.json` und die nativen XML-Ergebnisse dokumentieren den finalen Stand. `Screenshots` enthält tatsächliche Play-Mode-Spielansichten mit einem eigenen Diagnosespielstand. Der echte Spielerstand wird dafür nicht verwendet. `FirstReview` enthält die erste Bildprüfung vor der gestalterischen Nachbesserung.

Physische Touch-Bedienung und eine persönliche gestalterische Abnahme durch den Nutzer sind mit diesen automatisierten Prüfungen nicht verbunden.

## Abschlie�ende Pr�fung

- Unity 6000.3.9f1: 30/30 Assettests und 33/33 Play-Mode-Tests bestanden.
- Nach der abschlie�enden Versetzung einer Pflanzengruppe am Pavillon: 2/2 gezielte Platzierungs- und Wiederholbarkeitspr�fungen bestanden.
- 12 neue native Spielansicht-Aufnahmen des final gespeicherten Stands, einschlie�lich �bersicht, Details, 2560�1080 und 1080�1920. Der Pavillonvorplatz ist frei, die neuen Rundwege sind geschwungen, und die Motive besitzen korrekte Transparenz.
- Erhalt aller 8.571 urspr�nglichen Szenenobjekte best�tigt; 389 zus�tzliche serialisierte Objekte. Die vorhandene Welt-Kindliste und das Figurenarray wurden ausschlie�lich erweitert.
- Alle sechs finalen PNG-Dateien sind bytegleich mit ihren generierten Quelldateien.
