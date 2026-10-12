# Rathaus: Helligkeit an die gemalte Stadt angepasst

Stand: 2026-09-29T04:36:00.003718+00:00

Die mittlere Grundaufhellung (0,65) ist im produktiven Rathaus-Prefab für alle drei LODs gespeichert. Die räumliche Beleuchtung, Normalen und Materialdetails bleiben erhalten; die Farbtextur liefert zusätzlich eine gleichmäßige Grundhelligkeit. Die neue Darstellung ist lokal umgesetzt und im tatsächlichen Spielmodus aufgenommen. Die persönliche Sichtfreigabe steht aus.

## Ansehen

- [Vorher – Nahansicht](Compare/Screenshots/fill_00_close.png)
- [Jetzt – Nahansicht](Final/Screenshots/02_town_hall_close.png)
- [Jetzt – zwischen den Nachbargebäuden](Final/Screenshots/01_city_context.png)
- [Jetzt – ausgewählt](Final/Screenshots/03_selected.png)
- [LOD1](Final/Screenshots/04_lod1.png), [LOD2](Final/Screenshots/05_lod2.png)

## Änderung

Drei eigene Materialien unter `Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928/Materials` verwenden die vorhandenen 2K-Karten. Die URP-Lit-Emission nutzt dieselbe Farbtextur als Grundaufhellung. URP 17 benötigt dafür das Material-Flag `BakedEmissive`; die Renderer sind jedoch nicht als `ContributeGI` markiert und es wurde kein Licht-Bake ausgeführt. Es wurden keine zusätzlichen Lichtobjekte, neuen Texturbilder oder neuen Shader eingeführt. Die ursprünglichen Prüfstandmaterialien bleiben unverändert.

Vier Stärken wurden im Spiel verglichen: 0,00 / 0,40 / 0,65 / 0,90. 0,65 hellt die Cremefassade und das Grün deutlich auf; 0,90 reduziert den Kontrast stärker als nötig. Die Einstellung lässt sich explizit über `Weedtopia > Art Pipeline > 3D Town Hall > Apply City Brightness` wiederherstellen.

## Verifikation

- Unity importiert und speichert den Modell-Prefab erfolgreich; alle neun ursprünglichen 2K-Texturbindungen geprüft.
- Acht Vergleichsaufnahmen und sechs abschließende Game-View-Aufnahmen nach erneutem Laden des gespeicherten Prefabs.
- 2927 Bestands-Assets und Einstellungen per Hash geprüft. Einzige Änderung an diesen Bestandsdateien: die drei Materialzuweisungen im Rathaus-Modell-Prefab. Drei Stadtmaterialien kommen hinzu.
- Stadt-Szene, Wrapper, Katalog, Geometrie, Texturbilder, Colliders und andere Gebäude unverändert. Die Gameplay-Prüfungen vom vorherigen Integrationsschritt wurden für diese reine Materialänderung nicht erneut ausgeführt.
- Maschinenlesbare Belege: `material_receipt.json`, `Final/gameview_receipt.json`, `preservation.json`, `summary.json`.

Keine Meshy-Nutzung und keine externen Credits.
