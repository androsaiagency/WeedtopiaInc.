# Burning Hills: Richtungsanimationen und Lieferwagen

Stand: 2026-09-25T21:11:05.803398+00:00. Die Bewegungsrevision ist integriert und anhand der unten genannten Prüfungen kontrolliert. Die Gesamtspiel-Abnahme bleibt offen.

Die drei Spaziergängertypen, Radfahrer und Lieferwagen verwenden neue gemalte Animationen aus erhöhter Perspektive. Jeder Bodentyp besitzt fünf gemalte Blickrichtungen mit je sechs Phasen. Drei geometrisch gespiegelte Ansichten ergänzen die acht Richtungen Ost, Nordost, Nord, Nordwest, West, Südwest, Süd und Südost. Rückansichten ersetzen die bisher rückwärts verschobenen Vorderansichten. Schwalben und Schmetterlinge besitzen sechs Flügelphasen und drehen sich stufenlos in ihre Flugrichtung. Insgesamt sind 162 Sprites aus sechs neuen Atlasdateien in sieben Animation-Sets integriert.

Die Geh-, Tret- und Fahrphasen folgen der zurückgelegten Strecke. Bei Stillstand steht das Motiv in einer Ruhephase; Spielpause hält die Bewegung an. Bestehende 50 Umgebungsakteure und alle 60 Instanzen des Lieferwagen-Pools sind an die neuen Sets gebunden. Wiederverwendbare Prefabs und die gespeicherte Szene enthalten die Zuweisungen. Die explizite Editor-Aktion lautet `Weedtopia/Production/Install Directional Motion`; erneutes Ausführen verändert die geprüften Szenen- und Prefabbytes nicht.

Der Wagen ist in der Seitenansicht rund 9,5 Welteinheiten lang und damit etwa doppelt so lang wie eine erwachsene Figur hoch ist. Für Lagerfahrer und Bäcker gilt dieselbe Fahrzeuggröße. Der Bodenkontakt sitzt zwischen den Rädern, der Ladungsindikator oberhalb des Daches. Leerfahrt zur Butterfabrik und beladene Rückfahrt verwenden die tatsächliche jeweilige Fahrtrichtung. Position und Animation folgen nun in jedem Bild dem vorhandenen Bruchteil der Simulationszeit; die 10-Hz-Aktualisierung der Oberfläche begrenzt die Fahrzeugbewegung nicht mehr.

## Sichtprüfung

Die [interaktive Galerie](review.html) erlaubt die Auswahl aller acht Richtungen und sechs Phasen. Ihre 65 Originalbilder stammen aus der tatsächlichen Unity Game View: 48 Richtungsbilder, Stadtstraße, Nord-/Südansicht, zwölf Wegpunkte einer Stadtlieferung einschließlich Kreuzung, Bäcker-Hin- und Rückfahrt sowie Übersicht. Kontaktbögen und vergrößerte Ansichten wurden kontrolliert. Die Bildfolgen verwenden kontrollierte Zeitpunkte und sind keine Echtzeitaufnahme eines manuellen Durchspielens.

In der ersten Sichtprüfung fiel auf, dass das eigene Figurenmaterial `SpriteRenderer.flipX` nicht berücksichtigt. Die westlichen Motive zeigten deshalb trotz richtiger Richtungsauswahl nach rechts. Die Korrektur spiegelt die Geometrie des Artwork-Kindobjekts und funktioniert mit beiden vorhandenen Materialfamilien. Ein zusätzlicher PlayMode-Test prüft diese geometrische Ausrichtung. Der verworfene Aufnahmestand samt Befund bleibt unter `Iterations/UnmirroredShader` erhalten. Die hier verlinkten Bilder wurden nach der Korrektur vollständig neu aufgenommen.

![Lieferwagen auf der Stadtstraße](Screenshots/city_van_05.png)

## Aktuelle Nachweise

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| Source-Compiler | 12 Assemblies; aktuelle Quellenprüfsummen bestätigt | [Compiler](source_compile.json) |
| Unity EditMode | 80/80 bestanden, nichts übersprungen | [XML](../Implementation/Validation/20260925_225841_EditMode_DirectionalFinal2.xml) |
| Unity PlayMode | 28/28 bestanden, nichts übersprungen | [XML](../Implementation/Validation/20260925_230311_PlayMode_DirectionalFinal2.xml) |
| Game View | 65 aktuelle Aufnahmen nach der Spiegelungskorrektur | [Aufnahmelauf](gameview_receipt.json), [Sichtprüfung mit Bildprüfsummen](visual_review.json) |
| UI-Nachprüfung | 1920×1080, 1366×768, 2560×1080 ohne gemeldeten Überlauf | [UI-Beleg](ui_overflow.json) |
| Windows-Development-Build | erstellt; drei D3D11-Szenarien mit je 300 Messbildern, zusätzlicher PNG-Nachweis | [Build](development_build.json), [Player](player_run.json), [Messdaten](player_profile.json) |
| Produktions-Preflight | blockiert (Exit-Code 1): vier externe Abnahmen offen; keine Asset-/Material-/Platzhalterbefunde | [Preflight](production_preflight.json) |
| Erhaltungsprüfung | 251 Gebäude-Wrapper und 290 vorhandene Kunst-/Audiodateien bytegleich; keine entfernten Szenenblöcke | [Vergleich](preservation.json) |

Die gezielten neuen Tests decken vollständige Richtungs-/Framezuweisungen, Prefabbindung, Fahrzeugmaßstab, Kamera-Verkürzung, Hin-/Rückrichtung, geometrische Spiegelung beider Shaderfamilien, Bewegung zwischen UI-Aktualisierungen und Stillstand bei Pause ab. Der erneute vollständige Testlauf umfasst außerdem die bestehende Spielmechanik, Worker-Zuweisung, Warenfluss, Bäckerabholung, Quests und Speichern/Laden.

Der Erhaltungsvergleich umfasst 2639 ursprüngliche Dateien und 7784 ursprüngliche Szenenblöcke. Keine ursprüngliche Datei und kein ursprünglicher Szenenblock wurde entfernt. An der vorhandenen Szene wurde ausschließlich die Präsentationskomponente der Umgebungsakteure um Animator-Verweise ergänzt; Gebäude, Bauplätze, Zuweisungen und Straßen bleiben erhalten. Die sechs Akteurs-Prefabs und das Fahrzeug-Prefab wurden gezielt erweitert. Simulation, Logistikdaten, Speicherformat und Kunstquellen bleiben erhalten. Die generierten Atlasdateien wurden einschließlich Alpha bytegleich übernommen; Zuschnitt und Bodenkontaktpunkte sind Unity-Importereinstellungen, keine lokale Pixelbearbeitung.

Die Kunstdateien sind [ResidentA.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/ResidentA.png), [ResidentB.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/ResidentB.png), [ResidentC.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/ResidentC.png), [Cyclist.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/Cyclist.png), [DeliveryVan.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/DeliveryVan.png) und [Wildlife.png](../../Assets/Weedtopia/Production/DirectionalMotion/Art/Wildlife.png). [Generierungs-Prompts](art_prompts.json), [Abstandskorrektur des Fahrzeugatlas](van_spacing_prompt.txt), [Herkunft und Prüfsummen](art_provenance.json), [Spritezuschnitte](slice_evidence.json), [erhaltene Generierungszwischenstaende](source_iterations.json), [Assetregister](../ProductionFinish/VISUAL_ASSET_REGISTER.csv).

Fingerprint: `a6af58e7bdf2a5949e8fcc79ddeecc2cdc43feeb1d20282c3a6d36e0da4471ab`. Zielgeräteprofil, manuelles Durchspielen, physische Touchbedienung, Hörprüfung und Audio-Lizenzabnahme bleiben offen. Die Agenten-Sichtprüfung stellt keine persönliche Gestaltungsgenehmigung oder Gesamtspielfreigabe dar. Kein Benutzereditor wurde geschlossen oder parallel gestartet.
