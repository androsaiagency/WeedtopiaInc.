# Burning Hills: Kupferhügel und Stadtleben

Stand: 2026-09-25T11:10:08.489424+00:00. Integriert und geprüft; die Gesamtspiel-Abnahme bleibt offen.

Die Gestaltung greift den Stadtnamen mit warm leuchtenden, kupferroten Felsstufen, begrünten Hügeln und zwei erhaltenen Ziegelöfen auf. Sie erzählen visuell von einem früheren Industrieort, den sich die Natur zurückholt. Die bestehenden Gebäude und Gärten bleiben der Mittelpunkt. Vierzehn Hügelmotive fassen die Außenbereiche ein; zwei Ortsschilder, sieben kleine Cafégruppen und vier Wimpelgruppen geben den Quartieren zusätzliche Treffpunkte. Fußwege und Lichtungen verbinden die alten Ofenplätze mit den bestehenden Spazierwegen.

Die Stadt hat 21 Spaziergänger und fünf Radfahrer auf gespeicherten Wegen. Sie bewegen sich mit unterschiedlichen Geschwindigkeiten und versetzten Startzeiten und machen an den Endpunkten kurze Pausen. Bewohner werden erst in passenden freigeschalteten, bebauten Bereichen sichtbar. Zwölf Schwalben und zwölf Schmetterlinge bewegen sich über Fluss, Teich und Gärten. Bestehende Laub- und Wasserbewegung bleibt erhalten.

Die Figuren nutzen jeweils zwei gemalte Bewegungsposen. Eine gemeinsame Präsentationssteuerung animiert die vorhandenen Instanzen, berücksichtigt den Pausenzustand und erzeugt keine Objekte während des Spielens. Sie schreibt keine Inventar-, Worker-, Liefer-, Quest- oder Speicherwerte. Dekoration hat keine Collider und fängt keine Gebäudeklicks ab. Alle festen Objekte, Wege, Materialien und Figuren sind unter `BurningHills_Districts/LivingBurningHills` in der Szene gespeichert; wiederholte Motive sind Prefabinstanzen. Die explizite Editor-Aktion heißt `Weedtopia/Production/Author Living Burning Hills` und verändert bei erneutem Aufruf keine Szenenbytes.

## Sichtprüfung und Nachbesserungen

Die neuen Atlasdateien wurden mit dem integrierten ImageGen erzeugt und einschließlich ihres Alphakanals bytegleich übernommen. Spritebereiche werden im Unity-Importer festgelegt; es gab keine lokale Pixelbearbeitung. Die erste Figurenfassung ist unter `Assets/Weedtopia/Production/LivingHills/SourceIterations` erhalten.

In der ersten Game-View-Prüfung wurden die isolierten Ofenplätze mit Wegen verbunden, Cafégrößen angepasst und Geh-/Tretposen überarbeitet. Eine weitere Nahkontrolle zeigte, dass der Sonnenschirm vor Coffee-Shop 1 Mila teilweise verdeckte. Die Sitzgruppe wurde daraufhin versetzt. Die früheren Bilder und Befunde bleiben unter `Iterations` erhalten; maßgeblich sind die unten verlinkten letzten Aufnahmen.

Die [Galerie](review.html) enthält 23 tatsächliche Game-View-Aufnahmen: Übersicht, Start- und Endbereiche aller vier Bezirke, Rathausgarten, Ortsschild, Caféstraße, Hügel, Ofengarten, Teich, Farmspazierweg, Gebäudemenüs und große Gebäudestufen. Die Sichtprüfung umfasst alle Kontaktbögen sowie vergrößerte Nahansichten. Die Café- und Teichpaare zeigen unterschiedliche Bewegungszeitpunkte; dies ist keine Aufzeichnung eines manuellen Durchspielens.

## Aktuelle technische Nachweise

| Prüfung | Ergebnis | Nachweis |
| --- | --- | --- |
| Unity EditMode | 77/77 bestanden | [XML](../Implementation/Validation/20260925_130210_EditMode_LivingHillsFinal2.xml) |
| Unity PlayMode | 26/26 bestanden | [XML](../Implementation/Validation/20260925_130641_PlayMode_LivingHillsFinal2.xml) |
| Source-Compiler | 12 Assemblies; aktuelle Quellenprüfsummen bestätigt | [Compiler](source_compile.json) |
| Game View | 23 frische Aufnahmen | [Aufnahmelauf](gameview_receipt.json), [Sichtprüfung](visual_review.json) |
| UI | 1920×1080, 1366×768/115 %, 2560×1080; keine gemeldeten Überläufe | [UI-Beleg](ui_overflow.json) |
| Windows-Development-Build | erstellt und in drei D3D11-Diagnoseszenarien gerendert | [Build](development_build.json), [Player](player_run.json), [Messdaten](player_profile.json) |
| Produktions-Preflight | keine neuen Asset-/Material-/Platzhalterbefunde; externe Abnahmen offen | [Preflight](production_preflight.json) |

Die neuen Tests prüfen gespeicherte Prefabs und Wegpunkte, kollisionsfreie Dekoration, Abstand der Spazierwege zu Bauplätzen, unveränderte Szene beim erneuten Installieren, sichtbare Bewegung, Pausieren, unveränderte Spielwerte und gleichbleibende Objektzahl. Die vollständigen bisherigen Tests einschließlich der Bäcker-Abholfahrten wurden erneut ausgeführt.

Der [Erhaltungsvergleich](preservation.json) umfasst 2579 ursprüngliche Dateien und 7541 ursprüngliche Szenenblöcke. Kein ursprüngliches Asset und kein ursprünglicher Szenenblock wurde entfernt. Bestehende Szenenkomponenten, Prefabzuweisungen, Gebäudezentrierung, Kunstquellen, 56 Bauplätze, Logistik, Bäckereistraßen und Spielinhalte bleiben unverändert. Im vorhandenen Welt-Transform wurde lediglich eine neue Kindhierarchie ergänzt. Zusätzlich wurden die Aufnahmefunktion und Tests sowie zwei explizite Editor-Hilfsfunktionen für neue Gartenwege ergänzt.

![Stadtleben](Screenshots/03_cafe_life_a.png)

![Kupferrote Hügel](Screenshots/05_copper_hills.png)

Die finalen neuen Kunstdateien sind [Landmarks.png](../../Assets/Weedtopia/Production/LivingHills/Art/Landmarks.png) und [TownLife_v2.png](../../Assets/Weedtopia/Production/LivingHills/Art/TownLife_v2.png). [Prompts](art_prompts.json), [Bewegungs-Nachbearbeitung](walking_refinement_prompt.txt), [Herkunft und Prüfsummen](art_provenance.json), [Assetregister](../ProductionFinish/VISUAL_ASSET_REGISTER.csv).

Fingerprint: `259cbef69474efb69ce6420852c0c35d0e3948a7f86fc575b4fc50d3f68e9866`. Die Agentenprüfung ersetzt keine persönliche Gestaltungsgenehmigung. Physische Touchbedienung sowie Zielgeräteprofil, manuelles Durchspielen, Hörprüfung und Audio-Lizenzabnahme bleiben offen. Kein Benutzereditor wurde geschlossen oder parallel gestartet.
