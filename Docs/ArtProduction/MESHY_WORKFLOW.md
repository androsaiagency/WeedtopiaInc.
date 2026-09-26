# Manueller Meshy-Export und Unity-Empfang

Der [Gebäude-Asset-Index](BUILDING_ASSET_INDEX.md) verlinkt alle 251 Slots nach Familie und Levelband: Referenzbild, tatsächlicher Bildprompt, Meshy-/Texturprompt, Spezifikation, Settings und Unity-Prefab. Die aktuelle Freigabe steht im Manifest und in der hashgebundenen visuellen Abnahme; der Index ersetzt diese Prüfung nicht.

Dieser Auftrag bedient Meshy.ai nicht. Es gibt keine API, Anmeldung, Uploads oder Credit-Ausgaben. Die folgenden Schritte beschreiben die spätere manuelle Arbeit des Nutzers; sie wurden nicht ausgeführt. Die aktuelle Meshy-Weboberfläche wurde nicht geöffnet. Ihre Bezeichnungen, Verfügbarkeit und Kosten sind deshalb **nicht live verifiziert**. Falls `smart-topology` nicht angeboten wird, eine geeignete statische Triangle-Ausgabe wählen und die konkrete Entsprechung bei der späteren manuellen Abnahme dokumentieren; Achsen, Silhouette, PBR-Absicht und Budget bleiben unverändert.

## Einen geprüften Slot exportieren

1. In `BUILDING_ASSET_MANIFEST.json` oder der Tiermatrix einen Slot mit Status `accepted` wählen. `PROGRESS.json` zeigt den Gesamtstand und noch offene Slots. Das `mesh_input_3q.png` muss vorhanden, individuell und visuell freigegeben sein; vorbereitete Promptdateien allein sind kein fertiges Asset.
2. `asset_spec.de.md` lesen, `mesh_input_3q.png` öffnen, `meshy_prompt_en.txt` kopieren. Dieser englische Hauptprompt enthält sämtliche Ausschlüsse auch ohne separates Negativfeld. `texture_prompt_en.txt` ist ausschließlich für Texturabsicht vorgesehen.
3. Später manuell in Meshy `image_to_3d` verwenden, notfalls `text_to_3d`. Einstellungen aus `meshy_settings.json`: smart-topology falls vorhanden, Triangles, begründetes Ziel zwischen 6.000 und 15.000 Faces, Texturen 2048, PBR an, Rigging und Animation aus. Höchstens zwei Materialien; Glas nur bei Bedarf separat.
4. Ausgabe selbst auf Einzelasset, Silhouette, Front, geschlossene rekonstruierbare Formen, fehlende Kleinteile und saubere Texturen prüfen. Referenzbilder garantieren keine fehlerfreie Rekonstruktion.
5. Als **FBX mit vollständigem Texturordner** exportieren. Den Export zusammenlassen; Texturen nicht einzeln umbenennen oder aus dem Ordner ziehen. Den FBX-Namen nach Möglichkeit `WT_<building_id>_<tier_id>.fbx` nennen. Keine automatischen kostenpflichtigen Nachbearbeitungen sind Teil dieser Pipeline.

## Einmalige Unity-Vorbereitung

Die einmalige Vorbereitung ist durchgeführt. Der Menüpunkt **Weedtopia → Art Pipeline → Create or Repair Building Intake Scaffold** erzeugt oder repariert bei Bedarf fehlende leere Wrapper und den direkten ScriptableObject-Katalog. Bestehende Modelle, gültige Zuweisungen und manuelle Anpassungen bleiben erhalten. Der Menüpunkt kann erneut ausgeführt werden. Er startet weder Meshy noch eine Bildgenerierung.

Die Empfangsstruktur wurde am 14.09.2026 mit Unity 6000.3.9f1 tatsächlich importiert und serialisiert: 251 Wrapper-Prefabs und ein Katalog mit 251 direkten Referenzen. Alle Modellplätze sind absichtlich noch leer. Unity hat die Metadaten regulär erzeugt. Die 13 EditMode-Tests einschließlich wiederholter Scaffold-Erzeugung und Erhalt manueller Anpassungen sind bestanden; aktuelle Nachweise stehen in `Validation/unity_acceptance_status.json` und `Validation/unity_editmode_results.xml`. Die abschließende vollständige Produktionsprüfung und der reine Unity-Check sind ebenfalls bestanden; siehe [Abnahmebericht](ACCEPTANCE_REPORT.md).

## Einen Export zuweisen

1. Den vollständigen Exportordner unter `Assets/Weedtopia/Art/Buildings/Incoming/<building_id>/<tier_id>/` ablegen und Unity regulär importieren lassen.
2. **Weedtopia → Art Pipeline → Building Tier Intake** öffnen, Familie filtern oder Tier suchen.
3. Den Root des importierten FBX/Model-Prefabs aus dem Project-Fenster in **FBX / Model-Prefab** ziehen.
4. **Wrapper mit diesem Modell erstellen / aktualisieren** anklicken. Der eindeutige Zielpfad ist im Fenster sichtbar. Das Quellmodell wird nicht gelöscht, verschoben oder überschrieben.
5. Den Wrapper prüfen. Position, Rotation und Maßstab am `VisualRoot` korrigieren; Anker auf die echten Ports setzen. Ein Meter entspricht einer Unity-Einheit, oben +Y, Front +Z, Bodenbezug am reservierten Bauplatzmittelpunkt. Die frühe Gebäudefront soll trotz kleiner Grundform zur festen Anschlusskante passen.
6. **Nur validieren (alle Slots)** meldet unter anderem fehlende Modelle, abweichende Namen/Ordner, leere Materialien, unplausible Bounds und fehlende Katalogreferenzen. Front und Geometrie brauchen zusätzlich eine Sichtprüfung. Ein Hinweis ist keine automatische Freigabe.

Wrapper-Hierarchie: `WT_<building_id>_<tier_id>` mit `VisualRoot`, `InteractionAnchor`, `WorkerAnchor`, `DeliveryInputAnchor`, `DeliveryOutputAnchor`, `VfxAnchor`, `ColliderRoot`. Nur `VisualRoot` trägt den importierten Modellzweig. ColliderRoot bleibt zunächst leer; vereinfachte Collider werden sichtbar manuell geprüft. Keine beliebigen MeshCollider, Fahrzeuge, Förderbänder oder Worker werden eingebaut.

Beim Modellwechsel bleiben echte manuelle lokale Transformkorrekturen, manuell hinzugefügte Kinder und Anker erhalten. Unveränderte Geometriepositionen übernehmen die Defaults des neuen Quellmodells. Falls eine manuell bearbeitete Hierarchieposition im Ersatz fehlt, bricht das Update vor dem Speichern ab; diese Struktur muss bewusst geprüft werden. Das Tool verschweigt keinen verlorenen Offset.

## Lokale Prüfungen und Wiederaufnahme

Im Projektroot:

```powershell
python Docs/ArtProduction/Tools/validate_pipeline.py validate --data-only --report Docs/ArtProduction/Validation/metadata_validation.json
python Docs/ArtProduction/Tools/validate_pipeline.py validate
python Docs/ArtProduction/Tools/validate_pipeline.py sync-progress
python Docs/ArtProduction/Tools/test_validate_pipeline.py
python Docs/ArtProduction/Tools/compile_unity_sources.py
python Docs/ArtProduction/Tools/verify_preservation.py
```

Der vollständige Validator darf mit fehlenden Pflichtbildern, Showcases oder serialisierten Wrappern nicht grün werden. `--data-only` bestätigt ausschließlich die Produktionsdaten. Die Source-Kompilierung verwendet lokale Unity-Assemblies ohne Editorstart; sie führt keine Unity-Tests aus. Die bereits bestandenen EditMode-Tests liegen in `Weedtopia.Editor.BuildingArtPipeline.Tests` und können im Unity Test Runner wiederholt werden. Für einen konfliktfreien Batchlauf ist `Tools/run_unity_acceptance.ps1 -Mode EditMode` vorbereitet; `-Mode FinalValidate` führt nach erfolgreicher lokaler Vollständigkeitsprüfung die reine Unity-Validierung aus. Der Runner startet bei laufendem Unity-Prozess oder vorhandenem Lock keine weitere Instanz und verändert keinen Lock.

Bildgenerierung verwendet ausschließlich die im Auftrag zugelassene integrierte Bildfunktion, einen Aufruf pro Asset. Kein kostenpflichtiger CLI-/API-Fallback. Nach einer Unterbrechung `PROGRESS.json` lesen und nur vollständige, aktuell hashgebunden akzeptierte Slots überspringen. Der exakte nächste Slot steht unter `next_slot`; abgeschlossene Familien und Showcases werden separat gezählt. Bedienung und Creditverbrauch von Meshy bleiben ausschließlich der späteren manuellen Entscheidung des Nutzers vorbehalten.
