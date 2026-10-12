# Förderbänder: Sprites und Animation

Die neun Förderbandrouten in `Assets/Weedtopia/Game/Scenes/BurningHills.unity` verwenden jetzt ein in der Szene gespeichertes Netzwerk aus wiederverwendbaren Prefabs.

Nachtrag vom 28.09.2026: Die abgeschnittene Unterkante der Erntekisten ist durch einen Grafikversatz im gemeinsamen Prefab korrigiert. [Ursache und direkte Vorher/Nachher-Aufnahmen](../ConveyorCargoFix/README.md).

## Darstellung

- Bandbreite 6 statt bisher 1,3 Welteinheiten (etwa 4,6-fach).
- Neue transparente, gemalte Sprites für dunkel gerippte Laufflächen mit petrolfarbenen Stahlführungen, Messingdetails, runde Übergabeteller und gefüllte Erntekisten.
- 18 gemeinsame Bandabschnitte und 20 Anschlüsse; die acht Zuleitungen teilen ihre Hauptstrecke tatsächlich, statt überlappende Bänder zu zeichnen.
- Statische Seitenführungen, kontinuierlich laufendes Band und rotierende innere Übergabeteller. Getrennte Texturkoordinaten verhindern verzogene Übergänge und Sprünge beim Wiederholen der Animation.
- Kisten fahren mit 3,6 Welteinheiten pro Sekunde über geglättete Kurven; Ein- und Ausblenden an den Endpunkten und eine geringe Bewegung in der Höhe sorgen für ruhige Übergänge.
- Eine Laterne und eine Hecke neben der Verarbeitung wurden mit unveränderten Referenzen seitlich versetzt, damit sie nicht auf dem verbreiterten Band stehen.

## Assets und Aufbau

Die drei Originaldateien wurden mit dem eingebauten `image_gen`-Werkzeug erstellt und ohne Raster-Nachbearbeitung in das Projekt kopiert. Die vollständigen Prompts stehen in [ART_PROMPTS.md](ART_PROMPTS.md).

| Datei | Auflösung | Verwendung |
| --- | --- | --- |
| `Assets/Weedtopia/Production/Conveyors/Art/ConveyorStraight.png` | 2172 × 724 | Wiederholbare Lauffläche und feste Führungen |
| `Assets/Weedtopia/Production/Conveyors/Art/ConveyorJunction.png` | 1254 × 1254 | Runde Übergabeteller |
| `Assets/Weedtopia/Production/Conveyors/Art/HarvestTray.png` | 1254 × 1254 | Transparente Erntekiste |

`ConveyorSpan.prefab`, `ConveyorJunction.prefab` und `HarvestTray.prefab` liegen unter `Assets/Weedtopia/Production/Conveyors/Prefabs`. Die Hierarchie `BurningHills_Districts/PaintedConveyorNetwork` enthält die festen Bandteile. Jede ursprüngliche Route referenziert ihren bereits in der Szene angelegten Pool aus drei Kisten. Es werden dafür zur Laufzeit keine GameObjects erzeugt.

`ConveyorAuthoring.Apply` ist die explizite, wiederholbare Installation, erreichbar über `Weedtopia/Presentation/Install Painted Conveyors`. Sie läuft weder beim Import noch automatisch beim Spielstart. Bestehende manuelle Platzierungen werden bei späterem Aufruf nicht zurückgesetzt.

## Verbindung zum Spiel

`Simulation.ConveyorTransferred` meldet ausschließlich tatsächlich ausgeführte Transfers. Die visuelle Repräsentation verändert weder Bestände noch Transportzeiten, Questlogik oder Spielstände. Der Pool stellt den Warenfluss dar; die sichtbare Kistenzahl ist keine zusätzliche Bestandszählung. Ein Transfer, der das Ziel füllt, bleibt kurz sichtbar, bevor eine tatsächlich blockierte Strecke stoppt. Pause hält Band, Teller und Kisten an.

Die ursprünglichen Routen-IDs und Punkte bleiben unverändert. Die früheren Bandrenderer sind deaktiviert; die alten Kisten liegen unter `ArchivedConveyorCargo`. `preservation.json` prüft den Szenenvergleich mit dem unmittelbar vor dieser Änderung gesicherten Stand, einschließlich aller vorherigen Menü- und Weltänderungen. Keine der 8978 ursprünglichen serialisierten Szenenkomponenten wurde gelöscht.

## Prüfung

- 59/59 EditMode-Tests für Simulation, Ressourcen, Fortschritt, Lieferungen, Speicherung und die neuen Förderband-Assets bestanden: `20260927_183832_EditMode_ConveyorFinalRegression.xml`.
- 37/37 PlayMode-Tests bestanden: `20260927_183414_PlayMode_ConveyorRevision.xml`. Der Förderbandtest prüft echte Transfers, konstante Geschwindigkeit, Pause, Blockierung, unveränderten Spielzustand und einen konstanten Objektbestand.
- Der neue Vergleichstest führt dieselbe Simulation mit und ohne Transferbeobachter aus und vergleicht den vollständigen serialisierten Zustand.
- Nachweise der expliziten Installation, wiederholten Anwendung und des Szenenerhalts liegen als JSON bzw. XML vor. Die Unity-Testberichte liegen unter `Docs/Implementation/Validation`.
- Nach der letzten Deko-Platzierung wurden die beiden Asset- und Idempotenztests erneut ausgeführt: 2/2 bestanden (`20260927_184618_EditMode_ConveyorFinalScene.xml`).
- `Screenshots` enthält tatsächliche PlayMode-Game-View-Aufnahmen des Netzes, der Anschlüsse und der Bewegung bei 1920 × 1080, 1366 × 768, 2560 × 1080 und 1080 × 1920. Die Bewegung wird reproduzierbar in 48 Schritten zu 1/24 Sekunde aufgenommen; `motion_receipt.json` dokumentiert bewegte Routen und sichtbare Kisten. Die vorbereitete Diagnose verwendet einen eigenen Spielstand.
- Die Sichtprüfung betrifft die gerenderte Unity-Szene. Der daraus erstellte Clip ist kein Messwert für die Bildrate auf Zielgeräten und ersetzt keine persönliche visuelle Abnahme.

Reproduktion: `Tools/Implementation/Run-UnityValidation.ps1 -Mode ConveyorCapture -Label Review`. `Tools/Implementation/verify_conveyor_preservation.py` prüft die Änderungen gegen das gesicherte Szenenoriginal; `Tools/Implementation/package_conveyor_evidence.py` bündelt die Aufnahmen und aktuellen Prüfergebnisse.
