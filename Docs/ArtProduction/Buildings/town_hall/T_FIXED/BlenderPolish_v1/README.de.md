# Rathaus: lokale Blender-Aufpolierung

## Ziel

Die frühere Meshy-Fassung aus der vom Nutzer freigegebenen Vorschau bleibt gestalterische und geometrische Basis. In Blender werden nur nachweislich fehlerhafte Bereiche repariert. Silhouette, Proportionen, Solarpaneele, Rohre, Tanks, Begrünung, Wappenrahmen und die gelungene creme-salbei-kupferne Materialwirkung bleiben erhalten.

Es werden keine Meshy-API, keine externe Generierung und keine Credits verwendet.

## Erster Prüfstand

Am 20. September 2026 wurde das einzige lokal vorhandene FBX als getrennte Blender-Arbeitskopie importiert und aus vier Richtungen gerendert. Dieses FBX ist die später erzeugte vereinfachte v3-Fassung und entspricht nicht der fast guten Vorschau des Nutzers:

- Es besitzt keine große Wappenplatte an der Front.
- Die sichtbare Begrünung der freigegebenen Vorschau fehlt.
- Der mittlere Turm und der Eingangsbereich sind anders aufgebaut.
- Das Mesh besteht aus einem Objekt mit 8.036 Eckpunkten, 13.953 Dreiecken und 225 getrennten geometrischen Komponenten.
- Es verwendet einen UV-Layer und ein einziges bildprojiziertes PBR-Material.

Die v3-Arbeitskopie dient nur als technischer Vergleich. Sie wird nicht zur neuen Endfassung erklärt.

## Gefundene Quelle

Der unveränderte FBX-Export der früheren Meshy-Fassung liegt unter `Assets/Weedtopia/Art/Buildings/Incoming/town_hall/T_FIXED/Meshy_v2_Base`. Das Modell entspricht der fast guten Vorschau: Wappenplatte, Pflanzen, Solarpaneele, Tanks, Rohre und die Berg-/Flammenkrone sind vorhanden.

Die Quelle besteht aus einem Meshobjekt mit 8.540 Eckpunkten, 14.363 Dreiecken und 343 getrennten geometrischen Komponenten. Sie besitzt einen UV-Layer und ein PBR-Material mit Albedo-, Normal-, Roughness- und Metallic-Karte. Das Original bleibt unverändert.

## Geplante überprüfbare Stufen

1. Unveränderten Export importieren und vier Vorher-Ansichten rendern. Erledigt unter `Meshy_v2_Baseline`.
2. Topologie, offene Kanten, getrennte Teile, Normalen, UVs und Materialkarten protokollieren. Erledigt; zusätzlich liegen farbcodierte Komponentenansichten vor.
3. Nur echte Geometriefehler wie Löcher, überstehende Keile und schwebende Fragmente in einer Kopie reparieren. Begonnen mit einer inneren Turmhinterlegung.
4. Wappen als eigenes flaches, austauschbares Blender- und Unity-Teil neu aufbauen. In Stage 2 umgesetzt.
5. Fehlerhafte Fassaden-, Stufen- und Eingangsflächen über getrennte Materialzonen säubern; intakte Atlasbereiche bleiben erhalten. Stage 2 enthält Eingang, Stufen und zurückgesetzte Fenstereinsätze.
6. Vorderseite, beide Dreiviertelansichten und Rückseite erneut rendern und dem Nutzer vor dem Unity-Export zeigen. Stage 3 ist der aktuelle Sichtprüfpunkt.
7. Erst nach visueller Freigabe als neues FBX exportieren und in einen neuen Unity-Wrapper einsetzen. Noch offen.

## Dateien dieses Prüfstands

- `WT_town_hall_MeshyBase_polish_v1.blend`: unveränderte Arbeitskopie der lokal vorhandenen v3-Fassung
- `inspection_report.json`: technischer Importbericht
- `baseline_front.png`, `baseline_front_left.png`, `baseline_front_right.png`, `baseline_rear.png`: neutrale Vergleichsansichten
- `inspect_and_render.py`: wiederholbarer lokaler Blender-Prüflauf
- `Meshy_v2_Baseline`: unveränderte Vorher-Ansichten, Importbericht und Komponentenkarte der richtigen Quelle
- `Stage1_GeometryPatches`: verworfener erster Reparaturprototyp mit zu weit vorstehenden Seitenflächen
- `Stage2_InsetRepairs`: früherer Sichtprüfpunkt mit zu glatten Fensterersatzflächen
- `Stage3_OriginalWindows`: aktueller Prüfkandidat; ursprüngliche Front- und Seitenfenster sind wiederhergestellt
- `Stage4_TargetedBackings`: verworfener Versuch; Rückflächen erschienen als vorstehende Tafeln
- `Stage5_ComponentCleanup`: verworfener Versuch; die betroffenen Meshy-Komponenten enthielten auch Teile der Solarpaneele
- `build_polish_stage1.py` bis `build_polish_stage5.py`: vollständig wiederholbare und nachvollziehbare Reparaturversuche
