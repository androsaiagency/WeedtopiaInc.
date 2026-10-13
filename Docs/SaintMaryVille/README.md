# Saint Mary Ville

Saint Mary Ville ist die zehnte Stadt. Rockos Einladung erscheint, sobald in den neun bisherigen zugänglichen Städten zusammen mindestens 2.000.000 Einwohner leben. Es zählt der tatsächlich gleichzeitig erreichte Gesamtstand; die gespeicherte Einladung bleibt bei einem späteren Rückgang erhalten. Das einmalige Mandat gewährt 12.500 Geld und sieben Teamkarten.

Die eigene Stadtgeschichte und alle Bewohnerkapitel stehen in [STORY_CONTENT.md](STORY_CONTENT.md). Die produktiven Daten gehören zum vorhandenen `BurningHills.json`-Katalog; die neue Stadt verwendet dieselben Produktions-, Quest-, Bewohner-, Reise-, Save- und Offline-Systeme wie die übrigen Städte.

## Integration

- Stadt-ID `saint_mary_ville`, Szenenpfad `Assets/Weedtopia/Game/Scenes/SaintMaryVille.unity`, Content-Version 11, Präfix `sm_`.
- 56 Bauplätze, 35 lokale Warenrouten, 18 Hauptquests, vier Viertel: Auenbeete, Fährmarkt, Mary-Höfe und Uferwerkstätten.
- Sechs bleibende Bewohner: Mara Bell, Elias Reed, Rosa Marin, Finn Alder, Amira Vale und Hugo Mercer. Jeder besitzt vier persönliche Kapitel und drei wiederkehrende Auftragsvarianten.
- Drei Gemeinschaftsorte: Fährhof, Marys offenes Haus und Lichterabend. Ihr Aufbau und Elias' Entscheidung zwischen Fährwerkstatt und Flusslernhaus werden im bestehenden Story-System gespeichert und an sichtbare, vorab authorierte Ortsobjekte gebunden.
- Eigene Bewohnerillustrationen, sechs Architektur-Prefabs, Stadtbegrüßung und Farbpalette; 251 Gebäudedarstellungen verwenden vorhandene Sprites, Kollisionsbereiche und Spielanker.
- Geschlossene, wachsende und fertige Gemeinschaftsorte besitzen sichtbar unterschiedliche Bauphasen. Wiederverwendete Bauzäune, Material und Werkbänke stehen ausdrücklich im Prefab. Jedes neue Architektur-Prefab hat einen eigenen Bodenanker und ein neutrales Grafik-Kind, damit die vorhandene Sortierung vorbeilaufende Bewohner richtig einordnet.
- Marys Haus steht auf einer eigenen Uferfläche zwischen Siedlungsweg und Promenade. Bauzaun, Werkbank und Leseecke sind darauf abgestimmt; bestehende Straßen, Flussgeometrie, Grundstücke und Bewohnerwege werden dafür nicht verschoben.
- Statische Objekte stehen in Prefabs und Szene. Runtime-Code bindet Zustand und Eingaben an diese vorhandenen Objekte.
- Geld, Sammlung und Team sind gemeinsam; Gebäude, Warenrouten, Quests, Geschichten und Aktivitätszähler bleiben lokal. Alte Saves werden additiv ergänzt, ohne bisherige Gebäude oder zugewiesene Arbeiter zu ersetzen.

## Erreichbare Einwohnergrenze

Die bisherige letzte Hausstufe hatte Grundkapazität 145. Mit einem maximalen Einwohnerteam (Stufe 100, Rang 10; Faktor 51), 17 Häusern und einem Pflichtpark je Stadt reichten neun Städte nur bis 1.131.435 Einwohner.

Für das angeforderte neue Ziel steigt ausschließlich der optionale Bonus der Hausstufe 5.000 von 20 auf 135. Die Grundkapazität dieser Stufe beträgt jetzt 260; ein vollständig entwickeltes Haus erreicht 13.260 Einwohner. Neun Städte können so insgesamt 2.028.780 Einwohner aufnehmen. Alle Einwohnerwerte unter Stufe 5.000, Bauplätze, Pflichtparks, Kosten und Arbeiterregeln bleiben erhalten. Simulation und Gebäudeinspektor lesen bereits dieselbe datengetriebene Kapazitätsformel.

## Explizite Werkzeuge

Content: `python Tools/Implementation/author_saint_mary_ville.py`

Unity-Authoring: `Tools/Implementation/Run-SaintMaryVille.ps1 -Mode Install`

Game View: `Tools/Implementation/Run-SaintMaryVille.ps1 -Mode Capture`

Der Editor-Menüpunkt lautet `Weedtopia/Cities/Install or Repair Saint Mary Ville`. Installer laufen ausdrücklich, nicht automatisch bei Import oder Start. Vor einer Batch-Ausführung prüfen die Skripte, dass keine andere Unity-Instanz und kein Projekt-Lock existieren. Wiederholung prüft und ergänzt fehlende Integration; bereits vorhandene Assets werden erhalten.

## Belege und Abnahme

`validation_summary.json` bündelt konkrete NUnit-XML-Ergebnisse, Content-, Authoring-, Game-View- und Preservation-Belege. Der Ausgangsstand vor dieser Änderung ist in `Preservation/baseline.json` dokumentiert. Vorbestehende manuelle und uncommittete Änderungen gehören zu diesem Ausgangsstand.

Die erfolgreich abgeschlossenen betroffenen Tests umfassen **145 verschiedene NUnit-Fälle**: 114 Domain-/Save-/Simulationsfälle, zehn Assetfälle und 21 PlayMode-Fälle. Zusätzliche Wiederholungen der fünf Saint-Mary-Assetfälle nach Darstellungsreparaturen zählen nicht als weitere verschiedene Tests. Sie laden die gespeicherte Szene neu und prüfen Bauphasenfarben, Zaungeometrie, Bodenanker und wiederholte Installation.

Ein zuvor gestarteter kompletter Domain-Lauf wurde nach über 20 Minuten CPU-Zeit ohne NUnit-Ergebnis beendet. Er ist in `Validation/interrupted_full_domain.json` ausdrücklich als **unvollständig** dokumentiert. Die vollständige Domain-Suite wird daher nicht als bestanden ausgegeben; die gezielten neuen und betroffenen Regressionstests sind durch ihre tatsächlichen XML-Ergebnisse belegt.

Die Game-View-Prüfung umfasst **83 tatsächlich gerenderte Aufnahmen**, davon 60 Kombinationen aus drei UI-Situationen, fünf Sprachen und vier Auflösungen. Sie prüft beide gespeicherten Elias-Varianten, alle zehn Städte, Rückreise, Fortsetzen und Offline-Verarbeitung. Die Sprachmatrix enthält Deutsch, Englisch, Spanisch, Französisch und Russisch in 1920×1080, 1366×768, 2560×1080 und 1080×1920. Sichtprüfung einzelner Aufnahmen und automatischer UI-Überlaufcheck sind getrennt dokumentiert.

Der Preservation-Vergleich umfasst 8.537 Ausgangsdateien. Alle neun bisherigen Stadtszenen und Bootstrap bleiben byte-identisch; ältere Content- und Sprachschlüssel bleiben erhalten. Der gemeinsame Städtedialog erhält die zehnte Zeile additiv. Die beabsichtigte Hauskapazitätsänderung betrifft ausschließlich die letzte Stufe.

Game-View-Aufnahmen verwenden einen getrennten Testsave und diagnostischen Wachstumsschritten. Sie belegen die sichtbare Integration und gespeicherten Varianten. Die Domain-Tests prüfen die real berechnete Grenze bei 1.999.999 / 2.000.000 / 2.000.001 Einwohnern und die anfängliche Questkette über öffentliche Spielbefehle.

Persönliche visuelle Abnahme, vollständiges menschliches Durchspielen, Gerätebedienung, Geräteaudio, muttersprachliche Textprüfung sowie Android-/WebGL-Releaseabnahme werden durch diese lokalen Belege nicht ersetzt. Diese Abnahmen werden im Abschlussbeleg getrennt ausgewiesen.
