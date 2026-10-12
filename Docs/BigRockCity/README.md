# Big Rock City

Big Rock City erweitert Weedtopia um eine zweite spielbare Stadt. Rocko bietet die Übernahme an, sobald Burning Hills einmal mindestens 1.000 Einwohner erreicht hat. Sein Regierungsauftrag läuft zusätzlich zur bisherigen Hauptgeschichte und zu den Stadtgeschichten. Ein späterer Rückgang der Einwohnerzahl nimmt das Angebot nicht zurück.

Der Bürgermeister ist mit dem Startkapital verschwunden. Die Bewohner sind geblieben. Die neue Stadtgeschichte handelt davon, Versorgung, nachvollziehbare Entscheidungen und nachbarschaftliches Vertrauen wieder aufzubauen. Der Spieler bezahlt keine fremden Schulden und verliert keine Gebäude in Burning Hills.

## Einstieg und Reisen

- Der Stadtname links oben ist mit einem sichtbaren „Städte ▾“-Hinweis anklickbar. Unter **Aufträge** gibt es zusätzlich **Städte & Rockos Auftrag**.
- Bei 1.000 Einwohnern erscheint ein NEU-Hinweis sowie ein Auftragssymbol an Rockos bestehender Figur. Ein Gespräch mit Rocko öffnet seinen zusätzlichen Auftrag.
- Die Annahme aktiviert Big Rock City und vergibt einmalig 2.500 Geld und zwei Teamkarten als zweckfreie Überbrückungshilfe. Die eigentliche Wiederherstellung erfolgt über die neuen Aufträge und Gebäude.
- Beide Städte haben eigene Gebäude, Routen, Einwohnerzahlen, Hauptaufträge, Tagesertragskonten, Stadtstatistiken, Geschichten und Erinnerungen. Geld, Grinder, Teamkarten, verfügbare Arbeitskräfte und Sammlung gehören zum gemeinsamen Unternehmen.
- Bereits zugewiesene Mitarbeiter bleiben an ihrem Arbeitsplatz. Der Aufbau von Big Rock City liefert über die eigene Hauptquest die notwendigen Mitarbeiter. Laufende Lieferungen werden beim Wechsel nicht gelöscht und erscheinen nur in ihrer eigenen Stadt.
- Beide übernommenen Städte werden durch dieselbe deterministische Simulation weitergeführt, auch offline. Die gesperrte Stadt produziert nicht vor der Übernahme.
- Reisen speichert vor dem Laden. Fortsetzen öffnet die zuletzt besuchte Stadt. Der ursprüngliche Save-Dateiname bleibt erhalten.

## Die Stadt

Die feste isometrische 2D/2.5D-Kamera und das vertraute Aufbauprinzip bleiben erhalten. Die neue Stadt hat dieselben zwölf Definitionsfamilien einschließlich freier Bauplätze, 56 Gebäudeplätze und 35 eigene Warenwege. Elf sichtbare Gebäudefamilien erhalten einen eigenen Katalog mit allen 251 Visual-Bändern.

Big Rock City liegt an einem alten Sandsteinbruch. Die Grundgestaltung verbindet warme Sandsteinflächen, Kupfer, Petrol und eine trockenere Wiesenpalette. Fünf ausgearbeitete Felsgruppen bilden wiedererkennbare Orientierungspunkte zwischen den vertrauten Quartieren:

| Quartier | Charakter |
| --- | --- |
| Felsgärten | Farm und Verarbeitung zwischen warmen Felsrändern |
| Kupfergasse | Rathaus, Stadtlager und Cafés als Treffpunkt |
| Felsstufen | Wohnviertel, Parks und gemeinsame Gartenorte |
| Altes Werkviertel | Weitergenutzte Technik, Butterwerk, Markt und Bäckereien |

Die 251 bestehenden Building-Wrapper werden in einen eigenen Asset-Bereich kopiert. Der neue Materialshader verändert ausschließlich deren Darstellung. Formen der Ausbaustufen, Ressourcenpfade, Kollisionsflächen, manuelle Offsets und Gameplay-Anker bleiben erhalten. Originalmaterialien, Original-Wrapper und Burning Hills' Umgebung werden nicht umgefärbt.

## Die Hauptgeschichte

16 eigene, neu geschriebene Hauptaufträge führen von der ersten Ernte über echte Lieferungen zur Kupfergasse, dem offenen Rathaus, den Wohnvierteln und der selbstständigen Versorgung. Die erprobten Gebäudekosten, Rollen und Warensysteme werden wiederverwendet. So wird die zweite Stadt kein isolierter Minispielmodus.

Der Abschluss öffnet das freie Spiel in Big Rock City. Burning Hills behält seinen eigenen Queststand, das eigene Stadtprojekt und seine zehn VIPs. Die neuen Bewohnergeschichten entwickeln sich darüber hinaus weiter.

## Bewohnergeschichten

| Bewohner | Erste Begegnung | Spätere Kapitel | Persönlicher Bogen |
| --- | ---: | --- | --- |
| Berta Fels | 25 | 600 / 1.500 / 3.500 | Vom liegen gebliebenen Bauplan zur weitergegebenen Verantwortung |
| Levin Quitt | 75 | 750 / 2.000 / 4.000 | Vom leeren Tresor zu einem öffentlich erklärten Stadtetat |
| Samira Kupfer | 150 | 1.000 / 2.500 / 5.000 | Vom offenen Café zum gemeinsam getragenen Treffpunkt |
| Enno Funke | 300 | 1.250 / 3.000 / 6.000 | Von unbezahlter Technik zur offenen Reparaturwerkstatt |
| Lotte Klang | 500 | 1.500 / 3.500 / 7.000 | Von der stillen Bühne zum Fest der Bewohner |
| Ida Stein | 800 | 1.750 / 4.000 / 8.000 | Vom vergessenen Steinbruchrand zum gemeinsam gepflegten Felsgarten |

Jeder Bewohner hat vier persönliche Kapitel und drei rotierende Alltagsaufträge. Zusammen sind das 24 Kapitel und 18 wiederkehrende Varianten. Die jeweils dritte persönliche Geschichte bietet eine gespeicherte Entscheidung mit eigener Reaktion und passender Erinnerung. Wiederkehrende Anforderungen und Belohnungen wachsen mit der Stadt. Die Pause zwischen abgeschlossenen wiederkehrenden Aufträgen beträgt 300 Simulationssekunden; Aufträge verfallen nicht.

Persönliche Kapitel haben Vorrang, werden aber nicht übersprungen. Maximal drei Bewohner- oder Gemeinschaftsaufträge pro Stadt können gleichzeitig aktiv sein. Frische Produktion und Lieferungen zählen erst ab Annahme des jeweiligen Auftrags. Gebäudeziele und aktuelle Einwohnerzahlen verwenden ihren jeweiligen Stadtzustand.

Drei Gemeinschaftsprojekte verbinden die Bewohner:

1. **Die Bücher liegen auf dem Tisch** ab 1.200 Einwohnern: Berta, Levin und Samira eröffnen ein öffentliches Stadtgespräch.
2. **Grün zwischen großen Steinen** ab 2.500 Einwohnern: Berta, Enno und Ida gestalten einen gemeinsamen Felsgarten.
3. **Das Fest, dessen Echo bleibt** ab 5.000 Einwohnern: Alle sechs Bewohner tragen das neue Steinbruchfest.

Diese Projekte verlangen die ersten tatsächlich abgeschlossenen persönlichen Kapitel der Beteiligten. Die Figuren bleiben dauerhaft in der Stadt. Erinnerungen und gespeicherte Entscheidungen überleben Städtewechsel und das Laden des Spielstands.

## Architektur und Erhaltung

- `CityDefinition` beschreibt Ziele, Herkunft und Freischaltbedingungen als Daten. `CityCampaignService` behandelt Angebot, Annahme und Reise unabhängig von der Oberfläche.
- `ProgressionService` wählt die Hauptquestliste nach Stadt. Mitarbeiterbedingungen, Tagesaufträge und Journalansichten verwenden nur den jeweiligen Stadtzustand.
- `GameRoot` lädt die passende Szene über einen einmaligen `CitySceneHandoff`. Save-Pfad, Ranglistenpfad und Uhr werden beim Reisen gemeinsam weitergereicht. Tests und Bildaufnahmen behalten dadurch ihre isolierten Dateien auch beim Städtewechsel.
- `TransactCampaign` setzt bei fehlgeschlagenem Speichern Geld, Karten, Reiseort, Übernahme, Fortschritt und Belohnungsbelege zurück.
- Content-Version 1 und 2 werden vor der Ergänzung anhand des historischen Gebäudekatalogs validiert. Fehlende ursprüngliche Gebäude gelten weiterhin als beschädigter Spielstand. Erst anschließend werden die neue Stadt und ihre ungebauten Plätze ergänzt.
- Der explizite Installer erzeugt eigene Assets und ergänzt nur fehlende Verweise. Keine Reparatur bei Import, Start oder Domain-Reload. Feste UI, Bewohner und Landschaftsmotive sind als Prefabs und in der Szene sichtbar.

Die neuen Assets liegen unter `Assets/Weedtopia/Production/BigRockCity`, die neue Szene unter `Assets/Weedtopia/Game/Scenes/BigRockCity.unity`. Die sechs Bewohnerillustrationen, der Sandsteinfelsen und das eigene Ortsschild wurden mit dem eingebauten ImageGen-Werkzeug erstellt. Die unveränderten Alpha-Ausgaben und die vollständigen Prompts sind in `art_prompts.json` dokumentiert.

## Prüfwerkzeuge

Der Installer ist bereits auf die Szene angewendet, sobald ein erfolgreicher frischer `authoring_receipt.json` vorliegt. Das Menü lautet **Weedtopia / Cities / Install or Repair Big Rock City**. Versteckte Batchläufe starten nur ohne vorhandenen Unity-Prozess oder Lock und stellen die bisherigen Projekteinstellungen wieder her.

```powershell
& Tools/Implementation/Run-BigRockCity.ps1 -Mode Install -Label AuthoredCity
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.EditModeTests -Label BigRockCity
& Tools/Implementation/Run-UnityValidation.ps1 -Mode PlayMode -Assembly Weedtopia.Game.PlayModeTests -Label BigRockCity
& Tools/Implementation/Run-UnityValidation.ps1 -Mode EditMode -Assembly Weedtopia.Game.AssetTests -Label BigRockCityFinalAssets
& Tools/Implementation/Run-BigRockCity.ps1 -Mode Capture -Label GameView
python Tools/Implementation/big_rock_preservation.py verify
```

Aktuelle Ergebnisse werden in `validation_summary.json` zusammengeführt. Automatisierte Eingabe- und Spieltests, gerenderte Game Views, Bildprüfung, physische Touchbedienung und direkte visuelle Nutzerabnahme werden getrennt ausgewiesen. Diagnosebilder verwenden ausschließlich eigene Spielstände; reale Spielstände werden nicht bearbeitet.

Die großen Städte auf den Diagnosebildern sind absichtlich aufgebaute Wachstums-Fixtures. Geld, Einwohner, Gebäudestufen und Storyzähler dieser Bild-Fixtures belegen die Darstellung, nicht eine menschlich gespielte Aufbauzeit. Die vollständigen Tutorial-Tests durchlaufen die Gebäude- und Lieferfolgen dagegen über die öffentlichen Spielkommandos. Die Einladung wird zusätzlich an einer echten Berechnung mit acht Wohnhäusern und exakt 1.000 Einwohnern getestet.

Frühere fehlgeschlagene Läufe bleiben mit ihren ursprünglichen Belegen im Prüfverzeichnis erhalten. Der aktuelle Sammelbericht zeigt die zuletzt gelaufenen Prüfungen einschließlich Fehlern, falls eine Prüfung noch offen ist. Er ersetzt keine direkte visuelle Abnahme und keinen Geräte- oder WebGL-Build.

Die Testauswertung umfasst 211 Domain-/EditMode-Prüfungen, 68 PlayMode-Prüfungen und 43 Asset-/Editor-Prüfungen. Nach Korrekturen werden die betroffenen Tests gezielt wiederholt; im Sammelbericht stehen die ursprünglichen Gesamtläufe und die Wiederholungen, jeweils mit dem letzten Ergebnis pro Test. Die vollständige Tutorial-Folge beider Städte, einmalige Übernahmehilfe, alte Spielstände, Rückkehr nach Burning Hills und Fortsetzen in Big Rock City werden dabei tatsächlich geprüft.

Der abschließende Sammelbericht weist am 2. Oktober 2026 alle 322 Tests als bestanden aus. Alle zwölf Quellcode-Assemblies sind kompiliert, die überprüften Quellcode-Hashes aktuell. Der native Lauf `20261002_090143_Capture_FinalGameViews` liefert 30 frische PlayMode-Game-Views. Die Layoutprüfung findet in den vier genannten Auflösungen keine Überläufe; die Pixelprüfung ist in `visual_review.json` samt Bild-Hashes festgehalten. 268 neue Szenen-, Prefab- und Katalogdateien haben keine ungelösten GUIDs. 2.679 geschützte ursprüngliche Assets und alle 9.189 ursprünglichen Burning-Hills-Szenenobjekte sind erhalten.

Die Bildprüfung bestätigt die neue Stadtgestaltung und die Oberflächen. Bei starker Vergrößerung und im Hochformat bleiben die Kachelübergänge der übernommenen Bodenflächen erkennbar; auch die ursprüngliche Burning-Hills-Landschaft zeigt sie. Diese Prüfung ist keine direkte Nutzerabnahme und ersetzt keine physische Touchprüfung, Geräteaudio, einen menschlichen Gesamtdurchlauf oder einen WebGL-/Player-Build.

Bei der Entwicklung wurden zwei eindeutig eigene Diagnoseeinträge in der normalen Rangliste gefunden und entfernt. Die bisherigen Spielereinträge wurden dabei erhalten. Die Korrektur und deren Sicherung sind in `Preservation/score_isolation_receipt.json` dokumentiert. PlayMode-Tests vergleichen die normale Rangliste vor und nach jedem Test bytegenau; der Sammelbericht prüft zusätzlich, dass sie auch nach den Game-View-Aufnahmen unverändert ist.
