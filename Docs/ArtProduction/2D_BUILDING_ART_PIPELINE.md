# Weedtopia: 2D-/2.5D-Gebäudepipeline

## Entscheidung

Die Gebäudedarstellung wechselt von Meshy-Rekonstruktionen zu vorgerenderten isometrischen 2D-Sprites. Die vorhandenen 251 akzeptierten Einzelbilder bleiben die gestalterische Quelle. Meshy wird für Weedtopia nicht weiter als Produktionsweg verfolgt; bestehende FBX-, Blender- und Diagnoseartefakte bleiben unverändert archiviert.

Der Wechsel betrifft ausschließlich die Präsentation unter `VisualRoot`. Das vorhandene Katalog-, Levelband-, Wrapper- und Ankersystem bleibt erhalten.

## Warum 2D hier besser passt

- Die akzeptierten Bilder besitzen bereits die gewünschte Silhouette, Materialwirkung, Begrünung und Detaildichte.
- Es entstehen keine rekonstruierten Löcher, Keile, verzogenen Embleme oder Projektionsartefakte.
- Alle 251 Levelzustände können exakt ihrer freigegebenen Bildvorlage entsprechen.
- Eine feste isometrische Kamera benötigt keine frei drehbaren Rückseiten.
- Renderingkosten und technische Varianz sind deutlich niedriger als bei 251 individuellen Fremdmeshes.
- Gebäude-Upgrades bleiben einfache, deterministische Prefabwechsel über den vorhandenen `BuildingVisualCatalog`.

## Unity-Aufbau

Jeder Gebäude-Wrapper behält `VisualRoot`, `InteractionAnchor`, `WorkerAnchor`, Lieferanker, `VfxAnchor` und `ColliderRoot`. Unter `VisualRoot` liegt ein authoriertes `SpriteVisual` mit:

- `SpriteRenderer`
- Bodenpivot in der Mitte der unteren Bildkante
- `BuildingSpriteVisual` für die Ausrichtung zur aktiven Kamera
- slotbezogener Skalierung auf die in der Spezifikation definierte Weltgröße

Klickflächen, Wegfindung, Lieferpunkte, Worker, VFX und spätere Animationen bleiben eigenständige Unity-Objekte. Es werden keine statischen Hierarchieobjekte zur Laufzeit erzeugt.

## Vorgesehene 2.5D-Erweiterungen

- separate weiche Bodenschatten unter den Gebäuden
- optional getrennte Licht-/Fenstermasken für Abend- und Nachtzustände
- separate Rauch-, Dampf-, Blatt- und Maschinen-VFX an `VfxAnchor`
- dezente Auswahlkontur über ein Material oder ein zweites Sprite
- korrektes Sorting anhand des Bodenpivots
- optionale Normal- beziehungsweise Höhenkarten nur dort, wo sie die feste Kameraansicht sichtbar verbessern

## Produktionsreihenfolge

1. Rathaus als isolierten Prototyp erstellen und in Game View mit der tatsächlichen Kamera prüfen.
2. Einen frühen, mittleren und finalen Zustand einer Gebäudefamilie als zweite Stichprobe umsetzen.
3. Pivot, Maßstab, Klickfläche, Sorting, Zoomschärfe und Speicherverbrauch festlegen.
4. Erst nach bestandener Stichprobe die übrigen 247 Bilder freistellen und als Sprites importieren.
5. Wrapper idempotent auf Sprite-Visuals umstellen und Katalog-/Levelbandtests erneut ausführen.
6. Game View, Zielauflösung und tatsächliches Zielgerät getrennt abnehmen.

## Aktueller Prototyp

Das Rathausbild wurde ohne Architekturänderung transparent freigestellt und unter `Assets/Weedtopia/Art/Buildings/Sprites/town_hall/WT_town_hall_T_FIXED_2D_v1.png` gespeichert. Der Prototyp wird getrennt vom produktiven Katalog erzeugt. Der derzeitige 3D-Wrapper wird erst nach ausdrücklicher visueller Freigabe ersetzt.
