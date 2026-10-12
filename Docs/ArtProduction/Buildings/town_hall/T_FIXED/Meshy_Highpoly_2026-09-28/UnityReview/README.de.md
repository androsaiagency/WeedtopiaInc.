> Fortschreibung 2026-09-28: 2K-Texturen und alle drei LODs wurden vom Nutzer freigegeben. Das Rathaus ist inzwischen gezielt in die Stadt integriert. Aktueller Stand: [Stadtintegration](../CityIntegration/README.de.md). Die Angaben unten dokumentieren den vorherigen isolierten Prüfstand.

# Rathaus: Unity-Prüfstand mit Detailstufen

Stand: 2026-09-28T20:38:28.781489+00:00

Der Nutzer hat den 50.000-Dreiecke-Ausgangspunkt visuell freigegeben. Die neuen 2K-Texturen und 20.000/12.000-Stufen sind weitere Prüfkandidaten.

## Ergebnisse

- Unity: 6000.3.9f1; GPU: NVIDIA GeForce RTX 3070; CPU: AMD Ryzen 9 3950X 16-Core Processor .
- Laufzeitspeicher laut Unity für LOD0-Texturen: 96.00 MiB (4K/4K/2K) bzw. 32.00 MiB (2K/2K/2K).
- Alle drei getrennten LOD-Textursätze zusammen: 96.01 MiB.
- Das Modell ist gleichmäßig auf das reservierte 20 × 18 × 22-m-Feld eingepasst, nicht verzerrt.
- Ein Material je Detailstufe; Standalone-Texturen BC7, Normalen BC5; Mipmaps; kein Read/Write.
- Ausgemusterter 6.400-Dreiecke-Versuch: Krone und Turm stark beschädigt, nicht im Prefab verwendet.

## Render-Vergleich

1280 × 720, bestehende URP, synchroner Renderaufruf plus 1-Pixel-GPU-Readback; je fünf Warmläufe und 20 Messungen. **Kein FPS-Test des fertigen Spiels.**

| Instanzen | LOD | Dreiecke | Sichtbare Instanzen | Median ms | P95 ms |
|---:|---:|---:|---:|---:|---:|
| 25 | 0 | 1250000 | 25 | 8.31 | 13.03 |
| 25 | 1 | 500000 | 25 | 2.11 | 2.48 |
| 25 | 2 | 300000 | 25 | 1.82 | 2.18 |
| 100 | 0 | 5000000 | 100 | 3.83 | 4.42 |
| 100 | 1 | 2000000 | 100 | 2.54 | 3.14 |
| 100 | 2 | 1200000 | 100 | 2.25 | 2.67 |

Alle Instanzen teilen dieselben Assets. Speicher für 100 verschiedene Gebäudetexturen ist dadurch nicht gemessen. Das einmalige Rathaus darf ein höheres Nahbudget besitzen als häufige Wohngebäude.

## Ansehen

- `texture_comparison.jpg`: 4K und 2K in Blender.
- `lod_comparison.jpg`: drei Detailstufen bei gleicher Nahansicht; die niedrigeren Stufen sind für kleinere Bildschirmdarstellung bestimmt.
- `single_4K.png`, `single_2K.png`: tatsächliche Unity-Bilder.
- `city_auto_LOD.png`: Unity-Szene mit 100 Instanzen und automatischer LOD-Auswahl.
- Unity-Szene: `Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928/TownHall_100_Review.unity`.
- Prefab: `Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928/WT_TownHall_3D_Review_LOD.prefab`.

Produktion, Katalog und Spielszene wurden nicht ersetzt. Nächste Entscheidung: neue Darstellung und LOD-Übergänge sichten; anschließend gegebenenfalls gezielte Integration und Player-/Zielgerätemessung.

## Messgrenzen und zusätzliche Nachweise

- Die oben genannten Speicherzahlen stammen aus dem Editor-Profiler, nicht aus einer isolierten GPU-Residency-Messung. Die reine BC5/BC7-Texturmenge mit Mipmaps beträgt rechnerisch 48.00 MiB für LOD0 mit 4K-Farb-/Normalenkarte und 16.00 MiB für LOD0 mit 2K-Karten. GPU-Treiber-Overhead und ein Player-Profil sind damit nicht gemessen.
- Die bestehende PC_Renderpipeline hat 50 m Schattendistanz; die Übersichtskamera steht über 400 m entfernt. Der Stadtvergleich misst daher keine flächendeckenden Echtzeitschatten.
- Keine Figuren, Partikel, Wirtschaftssimulation oder 100 unterschiedlichen Material-/Textursätze im Prüfstand.
- Native Mesh-Bounds im JSON sind lokale FBX-Koordinaten; importierte Objekttransformationen skalieren und drehen diese in die Welt.
- Methode: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipeline.StandardRequest.html
