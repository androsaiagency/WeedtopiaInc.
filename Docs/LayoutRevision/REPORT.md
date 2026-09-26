# Burning Hills — genehmigte Bezirks- und Gebäudeintegration

Stand: 2026-09-23T04:32:14.987749+00:00. **Implementiert und technisch geprüft; Gesamtspiel nicht final abgenommen.**

## Ergebnis

Die Szene folgt der Benutzerskizze: Industrie links, Siedlung oberhalb der Stadt, Farm deutlich rechts außerhalb des bebauten Bereichs. Eine Landstraße mit Flussquerung verbindet Farm-Lager und Stadt-Lager. Die Fahrt umfasst 196,083 Welteinheiten und 15 Simulationssekunden pro Lieferung gemäß bestehendem Routenmodell. Das Stadt-Lager beliefert sämtliche Coffee Shops und die Butterfabrik mit Fahrern. Förderbänder verbinden Anbau, Verarbeitung und Farm-Lager sowie Butterfabrik und Bäckereien.

| Bezirk | Erweiterung | Belegte bzw. freischaltbare Standorte |
| --- | --- | --- |
| Farm | nach rechts | 8 Anbauplätze, Verarbeitung, Farm-Lager |
| Stadt | nach unten | 12 Coffee Shops, Rathaus, Stadt-Lager |
| Siedlung | nach oben | 18 Plätze für Wohnhäuser oder Parks |
| Industrie | nach links | 12 Bäckereien, Butterfabrik, Supermarkt |

56 Standorte und 35 Routen sind als Szenenobjekte und Daten angelegt. Bezirksnavigation: Schieberegler/Pfeile, Mausrad entlang der Bezirksrichtung, Ziehen mit rechter oder mittlerer Maustaste; Strg+Mausrad zoomt. Die Übersicht zeigt alle Bezirke. Die vorhandene Touch-Bedienung bleibt erhalten; physischer Touch wurde nicht abgenommen.

Die ausdrücklich freigegebenen vier Prototypen sind unverändert als Bilder übernommen. Für die restlichen 247 Slots wurde die ausdrücklich gewählte lokale Freistellung verwendet. Originale RGB-Pixel bleiben identisch; ausschließlich Alpha und bei sieben randberührenden Motiven transparenter Außenabstand wurden ergänzt. Alle 251 Wrapper verwenden ihren eigenen passenden Sprite, darunter jede der 25 Ausbaustufen der zehn Familien und das feste Rathaus. Ruinen gehören zum vorgesehenen Wiederaufbau, sie sind keine Entwicklungsvisuals. Der aktive Spielpfad nutzt den Produktionskatalog und keinen Development-Fallback.

Katalog, GUIDs und alle sieben bestehenden Anker je Wrapper bleiben erhalten. Die ursprünglichen 16 Standort-IDs bleiben erhalten. Die Migration ergänzt nur die 40 neuen Plätze; vorhandene Gebäude, Worker, Geld und laufende Fracht bleiben bestehen. Die vorherige Umgebung liegt inaktiv unter `Archive_PreviousPresentationLayout`. Straßen, Grundstücke, Bäume und Props sind im Editor authoriert und in der Hierarchie sichtbar.

## Aktuelle Nachweise

| Prüfung | Ergebnis | Evidenz |
| --- | --- | --- |
| Unity EditMode | 64/64, 0 fehlgeschlagen | [20260923_020753_EditMode_FinalAlphaAssets](../Implementation/Validation/20260923_020753_EditMode_FinalAlphaAssets.json) |
| Unity PlayMode | 14/14, 0 fehlgeschlagen | [20260923_062523_PlayMode_FinalAlphaGameplay](../Implementation/Validation/20260923_062523_PlayMode_FinalAlphaGameplay.json) |
| Source-Compiler | 12 Assemblies erfolgreich; alle Quellenprüfsummen weiterhin identisch | [SourceCompile/result.json](../Implementation/Validation/SourceCompile/result.json) |
| Bestanderhaltung | 3576 Baseline-Dateien; keine fehlt; geschützte Quellen unverändert; 251 Ankervergleiche gleich | [preservation.json](preservation.json) |
| Sprite-Aufbereitung | 251 Einzelzuordnungen, 31 ursprüngliche Kontaktbögen und drei Korrekturbögen angesehen; vier Prototypen bytegleich und 247 Quellbilder RGB-identisch | [Cutouts/manifest.json](Cutouts/manifest.json), [Pixelvergleich](Cutouts/pixel_verification.json) |
| Tatsächliche Game View | 73 aktuelle Screenshots einschließlich 51 Gebäudezuständen und drei Auflösungen | [gameview_receipt.json](gameview_receipt.json), [visual_review.json](visual_review.json) |
| UI | aktuelle Prüfung in 1920×1080, 1366×768 mit 115% UI-Skalierung und 2560×1080 | [ui_overflow.json](../ProductionFinish/Evidence/ui_overflow.json) |
| Windows-Diagnosebuild | erfolgreich gebaut und ausgeführt; drei gerenderte Szenarien à 300 Messframes | [development_build.json](../ProductionFinish/Evidence/development_build.json), [player_run.json](../ProductionFinish/Evidence/player_run.json) |
| Produktions-Preflight | keine Asset-/Platzhalterfehler; unabhängige Abnahme-Gates offen | [production_preflight.json](../ProductionFinish/Evidence/production_preflight.json) |

Der erste EditMode-Lauf meldete einen Fehler im erneuten Presentation-Installer-Aufruf: Ein inaktiver Installationsstempel wurde nicht erkannt. Der Installer prüft diesen nun einschließlich inaktiver Objekte vor jeglichem Import. Die dabei unbeabsichtigt geänderten Dateien BuildingSite.prefab und DeliveryTruck.prefab wurden aus der SHA-verifizierten Ausgangssicherung wiederhergestellt. Im ersten PlayMode-Lauf war ein Stadtbewohner vom Seitenpanel verdeckt; die Platzierung am inneren Gehweg korrigiert das. Weitere Diagnosen belegten native Physik-Ray-Misses trotz rechnerisch getroffener Box. Die Auswahl prüft jetzt die vorhandenen authorierten Boxen direkt unter Berücksichtigung von Rotation und Skalierung. Der neue Test klickt außerdem Gebäude aller vier Erweiterungsbereiche vor und nach dem größten Spritewechsel. Die vollständigen Wiederholungsläufe oben prüfen die Korrekturen; die fehlgeschlagenen Protokolle bleiben erhalten.

Die Game-View-Iteration korrigierte zu kleine frühe Gebäudestufen, harte Grundstückskanten, Dreiecksverengungen an Straßenbiegungen, transparente Bodenflächen vor nördlichen Gebäuden und den zu unauffälligen Navigationsgriff. Die aktuelle Galerie zeigt die korrigierte Version. Die Aufnahmen laufen nach Kameraruhe; Kunst-Nahansichten verwenden explizite Diagnosestände, die den Spielstand des Benutzers nicht verändern.

Eine zusätzliche Prüfung neutralweißer Flächen verglich 67 Kandidaten mit den Originalmotiven. 28 bestätigte Hintergrundöffnungen in 23 Bildern wurden ausschließlich im Alpha-Kanal korrigiert, etwa zwischen Rohrbögen und unter offenen Solardächern. 39 tatsächlich gemalte Wandflächen oder Lichtreflexe blieben erhalten. Alle 23 Korrekturen wurden auf drei Kontaktbögen geprüft; repräsentative Ausbaustufen dieser Familien sind außerdem in der neu aufgenommenen Game View enthalten. [Einzelentscheidungen und Vorher/Nachher-Prüfsummen](Cutouts/WhiteGapReview/decisions.json).

Der Zwischenlauf `20260923_013503_EditMode_FinalDistrictAssets` wurde als ausschließlich eigener Testprozess kontrolliert beendet: NUnit verglich rund 630 MB als einzelne Byte-Collection-Elemente. Der Test berechnet jetzt für jede vollständig gelesene Datei SHA-256 vor und nach dem Installer-Aufruf. Der unterbrochene Lauf gilt ausdrücklich nicht als bestanden; nur die oben verlinkte vollständige Wiederholung zählt.

Der lokale Windows-Diagnoselauf verwendet NVIDIA GeForce RTX 3070 mit Direct3D11 bei 1920×1080 und expliziten URP-Renderanfragen: automated_farm: Median 0.86 ms, P95 1.26 ms, 11 geladene Gebäudesprites; freeplay_overview: Median 2.23 ms, P95 2.64 ms, 15 geladene Gebäudesprites; upgrade_completion: Median 1.10 ms, P95 1.56 ms, 15 geladene Gebäudesprites. Die Werte gelten für diesen Development-Build auf diesem Rechner. Sie sind keine Leistungszusage für andere Geräte; Texture-Runtime-Speicher ist keine VRAM-Messung. Die drei PNGs neben dem Profil belegen tatsächlich gerenderten Inhalt.

| Szenario, je 300 Messframes | Median / P95 / Maximum | Max. Draw Calls / Batches | Max. GC-Allokation pro Frame / GC-Sammlungen | Texture-Runtime-Speicher | Geladene Gebäudesprites |
| --- | --- | --- | --- | --- | --- |
| automated_farm | 0.86 / 1.26 / 4.00 ms | 128 / 126 | 71,218 B / 3 | 267.8 MiB | 11 |
| freeplay_overview | 2.23 / 2.64 / 4.67 ms | 1511 / 1506 | 101,240 B / 2 | 299.8 MiB | 15 |
| upgrade_completion | 1.10 / 1.56 / 25.97 ms | 161 / 161 | 1,921,020 B / 4 | 299.8 MiB | 15 |

Der Prüflauf lief ohne sichtbare Fensterpräsentation im Batchmodus mit aktivierter Direct3D11-Grafik und expliziten URP-Renderanfragen. Die Framezeiten sind deshalb keine Messung der Bildausgabe oder Eingabelatenz eines normal bedienten Spiels und keine Zielgeräte-FPS-Freigabe. Insbesondere die Draw-Call-Zahl der Gesamtübersicht und die Spitze beim Tierwechsel müssen gegen das noch festzulegende Zielgerät bewertet werden. Der erste verborgene Fensterlauf pausierte bei `runInBackground=0`; ausschließlich dieser eigene Diagnoseprozess wurde beendet und nicht als bestanden gewertet. [Unterbrechungsbeleg](player_interruption.json). Die Spieler-Projekteinstellung wurde dafür nicht geändert. [Unity-Dokumentation zum Player-Batchmodus](https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html).

## Offen für endgültige Abnahme

Die Gebäude-Prototypfreigabe und die Erlaubnis zur lokalen Freistellung liegen vor; dazu ist keine erneute Entscheidung nötig. Offen sind die unabhängigen Gates: `target_profile, human_playthrough, audio_listening, audio_license`. Automatische Hauptpfad-, Screenshot- und Leistungstests ersetzen weder den menschlichen Spieldurchlauf noch Audio-Abhören, Lizenzfreigabe oder eine vereinbarte Zielgeräteabnahme. `productionReady` bleibt deshalb false. Sämtliche hier genannten Game-View-Prüfungen sind Agentenprüfungen, keine neue Nutzerfreigabe.

Die verbleibende Zielplattformentscheidung und die Prüfung des Spielgefühls betreffen das endgültige Release, nicht die bereits genehmigte Serienintegration.

[Konkrete Schritte für die vier offenen Abnahmen](REMAINING_ACCEPTANCE.md). [Vergleichsgalerie und alle aktuellen Spielansichten](comparison.html).
