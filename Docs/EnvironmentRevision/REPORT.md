# Burning Hills: natürliche Umgebung

Stand: 2026-09-24T21:37:21.374964+00:00. Die Umgebungsüberarbeitung ist integriert und erneut geprüft. Die endgültige Gesamtspielabnahme bleibt offen.

## Sichtbares Ergebnis

Die bisher dünn verteilten kleinen Bäume und harten Bodenflächen wurden durch eine zusammenhängende Gartenlandschaft ersetzt: Eichen, Birken, Ahorn und Weiden in lockeren Gruppen, blühende Sträucher und Vorgärten, schattige Wiesenbereiche, moosiges Natursteinpflaster und weich auslaufende Erdwege. Vier kleine Gartenhöfe enthalten Brunnen oder Pergolen, Sitzplätze und Pflanzungen. Ein bewachsener Teich, Schilf und Felsen ergänzen die geschwungenen Flussufer. Die Verbindung zur entfernten Farm führt über eine Steinbrücke. Blätter und Wasser bewegen sich dezent über Shader.

Die Umsetzung orientiert sich an der üppigen, gemalten Gartengestaltung der Nutzerreferenz. Die akzeptierte Gebäudearchitektur und Bezirksanordnung bleiben erhalten. Die ersten Game-View-Iterationen wurden nachgebessert: weniger greller Rasen, ruhigere Wasserfarben ohne sichtbare Kachelbrüche, unregelmäßigere Uferbäume, klare Brückenränder und Gartenmöbel außerhalb der Gehwege. Lampen stehen mit Abstand zu den Bewohnern.

Eine zusätzliche Nahkontrolle zeigte, dass ein Baumwipfel am südlichen Stadtrand einen Coffee Shop teilweise verdeckte. Die Waldplatzierung berücksichtigt jetzt auch die projizierte Kronenhöhe vor Gebäudezugängen. Die finale Aufnahme `05_City_expansion.png` zeigt alle drei vorderen Shops frei. Der vorherige Befund bleibt in `garden_review4_findings.json` dokumentiert; Tests und Aufnahmen wurden nach der Korrektur vollständig erneuert.

Aktuell sind 1018 Pflanzeninstanzen und 149 weitere Gartenobjekte erfasst; vorhandene Lampen sind darin nicht mitgezählt. 16 Sprite-Prefabtypen sind vorbereitet, davon 15 in der Szene genutzt. Das kleine gemalte Fußbrückenmotiv bleibt als Reserve erhalten; die befahrbare Flussquerung verwendet ein eigenes Naturstein-Brückenprefab.

[Vorher/Nachher-Galerie und alle Spielansichten](comparison.html). [Stadt](Screenshots/04_City_start.png), [Siedlung](Screenshots/04_Settlement_start.png), [Teich](Screenshots/environment_232_152.png), [Flussquerung](Screenshots/13_farm_delivery.png).

## Integration und Erhaltung

Die Landschaft ist im Editor authoriert und unter `BurningHills_Districts/NaturalLandscape` in der gespeicherten Szene sichtbar. Wiederholte Pflanzen und Gartenobjekte sind Prefabinstanzen. Die statische Umgebung wird beim Spielen nicht aus Code erzeugt. Dekoration besitzt keine Collider und kann die Gebäudeauswahl nicht abfangen. Wind und Wasser benötigen keine Update-Komponente pro Pflanze.

Die Originalumgebung bleibt in ihrer bisherigen Hierarchie inaktiv erhalten. Das explizite Menü `Weedtopia/Production/Author Natural Landscape` erkennt den vorhandenen Installationsstempel und verändert bei erneutem Aufruf keine Szenenbytes. `RebuildOwned` dient ausschließlich der bewussten Iteration in einer eigenen Batch-Instanz; es ist kein automatischer Import- oder Start-Hook und darf nach manuellen Landschaftsänderungen nicht blind erneut ausgeführt werden.

Der SHA-256-Vergleich umfasst 2052 vorhandene Dateien: keine fehlt, alle bisherigen Kunstquellen, Gebäude-Wrapper und übrigen geschützten Assets sind unverändert. Innerhalb der Szene sind alle 1558 ursprünglichen Gameplay-Komponenten und Prefabinstanzen unverändert. Vorhandene Szenenobjekte wurden nicht gelöscht. 56 Bauplätze, 35 Routen, Routenpunkte, Fahrzeiten, Inventar, Worker, Bewohner und UI-Verknüpfungen bleiben erhalten. Verändert wurden die aktiven Umgebungshierarchien sowie Straßen-Meshes und -Materialien.

[Ausgangssicherung und Vergleich](preservation.json). Die beiden geänderten bestehenden Dateien sind `BurningHills.unity` und `LayoutVisualEvidence.cs`; die übrige Implementierung und Kunst wurden ergänzt. Projektoptionen, Benutzerspielstand und offene Editorinstanzen bleiben geschützt. Vor jedem Unity-Start prüft der Runner Prozesse und Lockfile; alle verwendeten Unity-Prozesse wurden selbst gestartet und beendet.

## Aktuelle Nachweise

| Prüfung | Ergebnis | Beleg |
| --- | --- | --- |
| Unity EditMode | 66/66 bestanden | [XML](../Implementation/Validation/20260924_232653_EditMode_WoodlandVisibilityFinal.xml), [Lauf](../Implementation/Validation/20260924_232653_EditMode_WoodlandVisibilityFinal.json) |
| Unity PlayMode | 14/14 bestanden | [XML](../Implementation/Validation/20260924_233108_PlayMode_WoodlandVisibilityFinal.xml), [Lauf](../Implementation/Validation/20260924_233108_PlayMode_WoodlandVisibilityFinal.json) |
| Source-Compiler | 12 Assemblies kompiliert; Quellenprüfsummen aktuell | [Compilerbeleg](../Implementation/Validation/SourceCompile/result.json) |
| Game View | 79 tatsächliche PlayMode-Aufnahmen, davon 51 Gebäudezustände; alle als Kontaktbögen geprüft, zentrale Umweltansichten zusätzlich in voller Auflösung | [Aufnahmebeleg](gameview_receipt.json), [Agentenprüfung](visual_review.json) |
| UI | 1920×1080, 1366×768 mit 115% UI-Skalierung, 2560×1080 ohne gemeldeten Überlauf | [UI-Prüfung](../ProductionFinish/Evidence/ui_overflow.json) |
| Animierte Natur | zwei aufeinanderfolgende Aufnahmen derselben Kameraposition; Wasser und Laub bewegen sich | [Bildvergleich](motion_evidence.json) |
| Windows-Build | Development-Build erfolgreich, eigene Ausführung mit aktivierter Grafik | [Build](../ProductionFinish/Evidence/development_build.json), [Player](../ProductionFinish/Evidence/player_run.json) |
| Produktions-Preflight | keine Asset-, Material- oder sichtbaren Entwicklungsplatzhalter-Befunde; externe Abnahme-Gates offen | [Preflight](../ProductionFinish/Evidence/production_preflight.json) |

Die zwei ergänzten EditMode-Tests prüfen die gespeicherten Prefabinstanzen, Spritezuordnung, Shader, fehlende Dekorations-Collider sowie das unveränderte Szenenresultat bei erneutem Installer-Aufruf. Die bestehenden PlayMode-Tests prüfen weiterhin Spiellogik, Logistik, Navigation und Auswahl in den vier Bezirken.

Der Windows-Diagnoselauf nutzt NVIDIA GeForce RTX 3070 mit Direct3D11 bei 1920×1080 und 300 Messframes pro Szenario. Die drei resultierenden PNGs wurden angesehen. Er verwendet explizite URP-Renderanfragen im Batchmodus und misst keine normale Fensterpräsentation oder Eingabelatenz. Die Zahlen sind keine Zielgeräte-FPS-Freigabe; Texture-Runtime-Speicher ist keine VRAM-Messung.

| Szenario | Median / P95 / Maximum | Max. Draw Calls | Texture-Runtime-Speicher |
| --- | --- | --- | --- |
| automated_farm | 1.16 / 1.74 / 4.42 ms | 134 | 285.1 MiB |
| freeplay_overview | 3.62 / 4.50 / 5.67 ms | 2513 | 317.1 MiB |
| upgrade_completion | 1.66 / 2.18 / 28.83 ms | 159 | 317.1 MiB |

Alle Nachweise beziehen sich auf Fingerprint `07dc930216a8b6df754537c7083cfcd9395bd2c1f22ee02ae55b4b52fd51bf29`. Die früheren Layout- und Umgebungs-Iterationsaufnahmen bleiben historische Vergleichsbelege. Nur die hier verlinkte finale Aufnahmeprüfung beschreibt diesen Stand.

## Quellen und noch offene Gesamtfreigabe

Drei neue Originalatlanten wurden mit dem integrierten ImageGen erzeugt und ohne Pixeländerung nach Unity übernommen: `Woodland.png`, `Gardens.png` und `Ground.png` unter `Assets/Weedtopia/Production/Environment/Art`. Sprite-Rechtecke wurden anhand vorhandener Alpha-Grenzen vermessen; Farbwirkung und Bewegung erfolgen über Materialien. Die Nutzerreferenz wurde als Gestaltungsrichtung verwendet, nicht als kopierte Spielgrafik.

[Vollständige Prompts](art_prompts.json), [Dateiherkunft und Prüfsummen](art_provenance.json), [aktualisiertes Assetregister](../ProductionFinish/VISUAL_ASSET_REGISTER.csv), [Registervergleich](register_receipt.json).

Diese Sichtprüfung ist eine Agentenprüfung, keine vorweggenommene Zustimmung des Benutzers zur neuen Umgebung oder zum Gesamtspiel. Die vier Gebäudeprototypen bleiben ausdrücklich freigegeben. Noch offen sind `target_profile, human_playthrough, audio_listening, audio_license`. Das Gesamtspiel wird deshalb weiterhin nicht als vollständig abgenommen oder releasefertig bezeichnet.
