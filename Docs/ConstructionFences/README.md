# Baustellenzäune während eines Ausbaus

Die gemeinsamen Bauplätze enthalten jetzt ein in der Hierarchie gespeichertes, wiederverwendbares Zaun-Prefab. Es wird ausschließlich angezeigt, solange der vorhandene Zustand `buildingSeconds > 0` ist. Dadurch werden Neubau, Ausbau, gespeicherte laufende Baustellen und sofortige Fertigstellung über denselben Zustand abgedeckt. Pause lässt die Baustelle sichtbar; nach Abschluss verschwindet die Einfassung.

## Gestaltung und Dateien

Petrolfarbene Metallrahmen, Messingbefestigungen, transparentes Gitter, gelb-schwarze Warnflächen und Eckpfosten mit amberfarbenen Warnleuchten greifen die bestehende Gestaltung von Burning Hills auf.

- `Assets/Weedtopia/Production/Construction/Art/FencePanel.png`: 2172 × 724 Pixel, transparentes modulares Zaunelement.
- `Assets/Weedtopia/Production/Construction/Art/WarningPost.png`: 724 × 2172 Pixel, transparenter Eckpfosten.
- `Assets/Weedtopia/Production/Construction/Prefabs/ConstructionFence.prefab`: vier Zaunseiten und vier Eckpfosten, standardmäßig deaktiviert.
- `Assets/Weedtopia/Game/Prefabs/BuildingSite.prefab`: gemeinsamer Einbauort; alle 56 Szenenbauplätze erben die Referenz.

Die Grafiken stammen aus dem eingebauten `image_gen`-Werkzeug. Das Zaunelement wurde darin zusätzlich von farbigen Restpixeln in den Gitteröffnungen bereinigt. Die finalen PNGs wurden unverändert in das Projekt kopiert; [ART_PROMPTS.md](ART_PROMPTS.md) enthält das vollständige Prompt-Set.

## Verhalten und Architektur

`BuildingView` gibt Ausbauzustand und die Breite des aktuellen Gebäude-Sprites an `ConstructionFenceView` weiter. Die vier Seiten bilden einen geschlossenen räumlichen Rahmen. Ihre Länge passt sich der aktuellen Gebäudegröße an; die Zaunhöhe bleibt bei 3,2 Welteinheiten. Die Textur wird gekachelt, damit Gitter und Warnstreifen bei großen Gebäuden nicht auseinandergezogen werden.

Zwei Seiten liegen in der Sortierung hinter, zwei vor dem Gebäude. Die Eckpfosten sind auf die Spielkamera ausgerichtet. Das Prefab erzeugt zur Laufzeit keine GameObjects und hat keine Collider; die bestehende Gebäudeselektion bleibt zuständig. Die Darstellung verändert weder Bauzeit noch Kosten, Ressourcen oder Speicherung. Das Rathaus erhält über die gemeinsame Prefab die Referenz, zeigt ohne Ausbau-Timer aber keinen Zaun.

Die explizite Installation ist im Editor unter `Weedtopia/Presentation/Install Construction Fences` erreichbar. Ein erneuter Aufruf erhält das vorhandene Prefab. Es gibt keine automatische Reparatur beim Import oder Spielstart.

## Nachweise

`authoring_receipt.json` dokumentiert die 56 gebundenen Bauplätze. `preservation.json` prüft, dass die Szene bytegleich und alle 251 bestehenden Gebäude-Wrapper unverändert bleiben. Am gemeinsamen Bauplatz-Prefab werden lediglich das zusätzliche Kind und dessen Referenz ergänzt.

`Screenshots` enthält tatsächliche PlayMode-Game-View-Aufnahmen aller zehn ausbaubaren Gebäudefamilien, benachbarter großer Baustellen, Pause und Fertigstellung sowie vier Bildschirmformate. Die vorbereiteten Aufnahmen verwenden einen eigenen Diagnose-Spielstand. `gameview_receipt.json` weist die Aufnahmen aus; sie ersetzen keine persönliche visuelle Abnahme oder einen Leistungstest auf Zielgeräten.

39/39 PlayMode-Tests sind bestanden (`20260927_191616_PlayMode_ConstructionFences.xml`). Die beiden neuen Spieltests prüfen einen echten Upgrade-Kauf, Pause, Laden, normale und sofortige Fertigstellung sowie Größen und Sortierung an kleinen und großen Varianten aller zehn Gebäudefamilien. Die Präsentation lässt dabei die Spielstanddaten unverändert. Die Asset-Tests prüfen geerbte Prefab-Bindungen, fehlende zusätzliche Collider und wiederholbare Installation. Die konkreten Ergebnisse sind in `validation.json` zusammengefasst.

Reproduktion:

```powershell
Tools/Implementation/Run-UnityValidation.ps1 -Mode ConstructionFences -Label Install
Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -TestFilter Weedtopia.Tests.Assets.ConstructionFenceAssetTests -Label ConstructionFences
Tools/Implementation/Run-UnityValidation.ps1 -Mode PlayMode -Assembly Weedtopia.Game.PlayModeTests -Label ConstructionFences
Tools/Implementation/Run-UnityValidation.ps1 -Mode ConstructionCapture -Label Review
python Tools/Implementation/verify_construction_fences.py
```
