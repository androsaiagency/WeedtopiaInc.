# Farmgarten und Flussanschluss – 26.09.2026

Die Änderung ist in `Assets/Weedtopia/Game/Scenes/BurningHills.unity` gespeichert.

## Gestaltung

- Steinweg, Brückendeck und niedrige Brückenmauern folgen derselben weichen Kurve. Das zuvor diagonal überstehende Brückenende entfällt.
- Der isolierte `FarmOrchardWalk` ist mit seinem Randstreifen in `NaturalLandscape/FarmGardenRefinement/InactiveOriginalPath` deaktiviert archiviert.
- Ein Zugang verbindet den Steinweg beim Farm-Lagerhaus mit dem vorhandenen Pergola- und Sitzplatz. Ein geschlossener Gartenweg führt um Bäume, Blüten und eine zusätzliche Bank.
- 16 vorhandene Bäume samt Blumen wurden aus der gleichmäßigen Reihe in lockere Gruppen versetzt. Sechs niedrige Blumeninseln beleben die Fläche vor den Anbauplätzen.
- Der vorhandene Spaziergänger benutzt den Zugang; zwei bestehende Radfahrer fahren auf dem Gartenrundweg. Die benachbarten Schmetterlinge wurden an die Blumenbeete versetzt. Animationen und Figurenanzahl bleiben erhalten.
- Sämtliche statischen Objekte sind in der Szene sichtbar gespeichert. Wiederholte Dekorationen verwenden vorhandene Prefabs; die gebogenen Brückenmauern verwenden ein gemeinsames neues Prefab mit zwei Meshvarianten.

## Prüfung

- Unity 6000.3.9f1: Import und Anwendung erfolgreich.
- Vorhandene Assettests: **24/24 bestanden**, keine Fehler oder übersprungenen Tests. Beleg: `Docs/Implementation/Validation/20260926_092931_EditMode_FarmGarden.xml`.
- Nach der abschließenden Geometriekorrektur: **4/4 betroffene Landschafts- und Figurenroutentests bestanden**. Beleg: `Docs/Implementation/Validation/20260926_093455_EditMode_FarmGardenFinal.xml`.
- Tatsächliche Play-Mode-Aufnahmen liegen unter `Before/Screenshots` und `After/Screenshots`; die jeweiligen `gameview_receipt.json` dokumentieren den Lauf. Die diagnostischen Spielstände liegen ausschließlich in diesem Dokumentationsordner.
- Die tatsächlichen gespeicherten Wegmeshes wurden auf endliche Koordinaten, korrekte Dreiecksorientierung und degenerierte Dreiecke geprüft. `verification.json` enthält das Ergebnis und die Prüfung der geschützten Szenenbestandteile.
- Die erste Installation wurde erneut angewendet: Szenendatei bytegleich (`idempotence.json`).
- Alle ursprünglichen Szenenobjekte bleiben erhalten. Die serialisierten 56 Gebäude-, 44 Lieferstrecken-, 60 Fahrzeug- und 50 Animatorkomponenten sind bytegleich. Bei den Laufzeitkomponenten wurden ausschließlich die dekorativen Bewegungswege in `LivingTownPresentation` angepasst.
- Bestehende Art-, Gebäude-, Content-, Prefab- und Projekteinstellungsdateien bleiben unverändert. Unter den vorbestehenden Assets wurden ausschließlich die Spielszene und das Editor-Werkzeug für Vergleichsaufnahmen geändert; die Gartengeometrie und das gezielte Authoring-Werkzeug sind zusätzliche Dateien.

Die Aufnahmen wurden visuell geprüft. Diese technische und visuelle Kontrolle ist keine vorweggenommene gestalterische Freigabe durch den Nutzer.

## Wartung

- `Weedtopia > Production > Refine Farm River And Garden`: explizite, wiederholbar sichere Erstinstallation. Bereits installierte Szenen werden nicht erneut aufgebaut.
- `Weedtopia > Production > Refresh Farm Garden Path Meshes`: explizite Aktualisierung der neu angelegten Wegmeshes und zugehörigen Spazierwege. Das Werkzeug schützt Play Mode und ungespeicherte Szenenänderungen.
- `python Tools/Implementation/verify_farm_garden.py`: Geometrie- und Bestandserhaltungsprüfung.
- `Tools/Implementation/Run-UnityValidation.ps1 -Mode FarmGardenCapture`: neue Vergleichsaufnahmen, nur bei geschlossenem Unity-Editor.

Originalszene und ursprüngliche Werkzeugdateien: `Originals/`. Das Werkzeug startet weder automatisch beim Import noch zur Laufzeit einen Szenenumbau.
