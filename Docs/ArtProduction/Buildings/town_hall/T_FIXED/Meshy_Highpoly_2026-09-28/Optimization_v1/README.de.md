# Rathaus: erster überprüfbarer Vereinfachungsvergleich

Stand: 28. September 2026. Isolierter 3D-Versuch auf ausdrücklichen Nutzerwunsch; der aktuelle 2D-Spielstand bleibt erhalten.

## Jetzt ansehen

`REVIEW.html` im Browser öffnen. Links steht das Original, rechts der aktuelle 50.000-Dreiecke-Kandidat. Regler und Ansichtsauswahl ermöglichen den direkten Bildvergleich.

- `REVIEW_front_3q.jpg`: Vorderseite nebeneinander.
- `REVIEW_rear_3q.jpg`: Rückseite nebeneinander.
- `REVIEW_detail_entrance.png`: Original, misslungene reine Reduktion und verbesserte Fassung im Detail.
- `REVIEW_detail_crown.png`: gleicher Vergleich für die Krone.
- Aktuelles Modell: `Rebaked_50000_UVv2/TownHall_50000_Rebaked.glb`.
- Öffnungsfertige Blender-Prüfszene: `Rebaked_50000_UVv2/TownHall_50000_Review.blend`.

## Messwerte

| Merkmal | Original | Aktueller Kandidat |
|---|---:|---:|
| Dreiecke | 3.335.710 | 50.000 |
| Im GLB gespeicherte Vertices | 1.841.806 | 75.074 |
| Materialanzahl | 1 | 1 |
| Dateigröße, dezimale MB | 130,70 | 58,01 |
| Farbkartenauflösung | 4.096² | 4.096² |
| Normalenkarte | 4.096² | 4.096² |
| Metallic/Roughness | 2.048² | 2.048² |

98,501 % weniger Dreiecke. Die niedrigere Blender-Vertexzahl von 23.970 entsteht durch Zusammenführen deckungsgleicher Vertices; UV-Grenzen benötigen beim Export wieder getrennte Vertices. Die exportierten 75.074 sind für den Vergleich mit dem GLB-Original maßgeblich.

Die Dateigröße ist keine Messung des GPU-Speichers. Die neu erzeugten Texturen sind PNGs; Plattformkompression, Mipmaps und niedrigere Auflösungen wurden noch nicht optimiert.

## Versuche und Ergebnis

1. Original unverändert importiert; ein Mesh, ein PBR-Material und drei eingebettete Texturen. Drei feste Kameraansichten und gemeinsame Beleuchtung angelegt.
2. Unabhängige Reduktionen auf 100.000, 50.000 und 25.000 Dreiecke. Alle drei wurden mit Original-UVs und Originalmaterial gerendert. Sichtbar verzogene Wappen, Paneele und Fassaden: nicht zur Integration geeignet. `Vergleich.html` dokumentiert diese verworfenen Versuche.
3. Neue UV-Aufteilung und Übertragung von Base Color, Tangent Normal, Roughness und Metallic vom hochauflösenden Original auf die 50.000er-Geometrie. Erster Versuch wegen schlechter UV-Flächennutzung zu unscharf; unter `Rebaked_50000` archiviert.
4. UV-Abstände korrigiert und sämtliche Karten neu berechnet. `Rebaked_50000_UVv2` ist der aktuelle Vergleichskandidat. Wappen, Paneelraster und Fassadenlinien sind wieder erkennbar. Kleine Pflanzen-, Form- und Oberflächendetails weichen vom Original ab. Agenten-Sichtprüfung ist keine Nutzerfreigabe.
5. Den GLB-Export erneut in Blender importiert. Genau 50.000 Dreiecke, ein Material, ein UV-Layer, null offene und null nicht-manifold Kanten nach dem Import mit zusammengeführten Vertices. Dies ist keine umfassende Prüfung auf Selbstüberschneidungen oder jede visuelle Unregelmäßigkeit.
6. Vorderansicht nach erneutem Import stimmt nahezu pixelgenau mit dem Bake-Vorschaurender überein: mittlere absolute Kanalabweichung jeweils unter 0,0002 auf der Skala 0–255. Original-SHA-256 ist unverändert.

## Noch offen

- Nutzervergleich von Wappen, Krone, Pflanzen, Eingang und Rückseite.
- Bei visueller Zustimmung: kleinere Textureinstellungen vergleichen, LOD-Stufen erstellen, einfache Collider, Maßstab/Bodenpivot und Unity-Materialimport prüfen.
- Leistung in einer echten Stadtansicht mit vielen Gebäuden auf dem Zielgerät messen. Aus diesem einzelnen Blender-Test wird keine garantierte FPS oder allgemeine Gebäudeanzahl abgeleitet.

Es wurden keine Unity-Szenen, Produktions-Prefabs oder Kataloge verändert. Meshy wurde nicht bedient, nichts hochgeladen und keine externen Credits verbraucht. Das Original wurde nicht überschrieben. Fortschritt und nächste Schritte stehen in `PROGRESS.json`; Messbelege in `comparison_validation.json` und `Rebaked_50000_UVv2/roundtrip_report.json`.
