# Inventar und Verbrauchsitems

Die Sammlung wird durch ein Inventar mit zwölf sichtbaren Slots pro Seite ersetzt. Filter: **Alle**, **Worker / Karten**, **Beschleuniger**, **Vorräte**. Ein Klick öffnet die Detailansicht mit Wirkung, Seltenheit, Bestand und einer passenden Aktion. Worker zeigen ihren tatsächlichen Level, Rang, Eigenschaften und Einsatzort; von dort geht es zur bestehenden Teamverwaltung. Erfolge und Belohnungsverlauf bleiben über separate Schaltflächen erreichbar.

## Items und Vergabe

| Item | Wirkung | Erste Hauptquest |
| --- | --- | --- |
| 1-Minuten-Baubeschleuniger | bis zu 60 Sekunden auf einer Baustelle | `first_crop`, 2 Stück |
| 5-Minuten-Baubeschleuniger | bis zu 300 Sekunden | `grow_manager`; später `processing_manager` und jeder VIP-Abschluss |
| 15-Minuten-Baubeschleuniger | bis zu 900 Sekunden | `farm_delivery` |
| 30-Minuten-Baubeschleuniger | bis zu 1.800 Sekunden | `settlement_build` |
| 1-Stunden-Baubeschleuniger | bis zu 3.600 Sekunden | `settlement_daily` |
| 5-Stunden-Baubeschleuniger | bis zu 18.000 Sekunden | `industry_build` |
| 10-Stunden-Baubeschleuniger | bis zu 36.000 Sekunden | `industry_sell` |
| 24-Stunden-Baubeschleuniger | bis zu 86.400 Sekunden | `freeplay` und Stadtprojekt-Finale |
| Kartenpaket | 3 vorhandene Worker-Karten | `city_sales` |
| Spezialistenvertrag | 1 seltener Worker (Rare), Beruf frei wählbar, 2 Eigenschaften | `townhall_spin` |
| Geldkiste | 500 Geld | `city_sales` |
| Grinder-Beutel | 10 Grinder | `townhall_spin` |

Beide Stadtprojekt-Pläne vergeben zusätzliche Bauhilfen und Vorräte. Die Startwährung von 1.000 Geld, Gebäudekosten, Bauzeiten, bisherigen Questziele und bisherigen Belohnungen bleiben erhalten. Worker-Pakete und Verträge sind ab dem Stadtviertel verwendbar.

## Beschleuniger verwenden oder aufteilen

Eine laufende Baustelle auswählen, die Vorher-/Nachher-Zeit prüfen und den Verbrauch bestätigen. Während dieser kurzen Bestätigung pausiert die Simulation. Schließen, Zurück und ein Kategorienwechsel brechen die Bestätigung ab. Ein Beschleuniger betrifft ausschließlich die gewählte Baustelle und erzeugt keine zusätzliche Produktion, Lieferungen, Tagesbelohnungen oder Offline-Zeit.

Würde ein Teil der Zeit verfallen, steht dies vor dem Verbrauch im Detailbereich. Alternativ lassen sich größere Beschleuniger **ohne Zeitverlust aufteilen**, auch ohne laufende Baustelle:

- 24 Std. → 24 × 1 Std.
- 10 Std. → 2 × 5 Std.
- 5 Std. → 5 × 1 Std.
- 1 Std. → 2 × 30 Min.
- 30 Min. → 2 × 15 Min.
- 15 Min. → 3 × 5 Min.
- 5 Min. → 5 × 1 Min.

Das ist bewusst vorgesehen, weil die gegenwärtigen Bauzeit-Obergrenzen unter einer Stunde liegen. Große Belohnungen können so über mehrere Baustellen hinweg eingesetzt werden.

## Speicherung und Architektur

- `GameDefinitions.items` enthält Namen, Beschreibungen, Kategorie, Seltenheit, Icon und Wirkung; Quest- und Projektbelohnungen referenzieren Item-IDs.
- `GameState.inventory` speichert gestapelte Bestände. Karten und Worker bleiben in ihren bestehenden Systemen; Geld und Grinder erscheinen als Guthabenanzeige.
- `InventoryService` bietet read-only Abfragen sowie einmal verwendbare Bestätigungen. Der normale `GameSession.Transact`-Pfad speichert erfolgreiche Aktionen.
- `ConstructionProgress` vereinheitlicht normalen Bauabschluss, Grinder-Abschluss und Beschleuniger einschließlich Erstbau-Tagesanspruch.
- Alte Saves ohne Inventarfeld bleiben gültig. Historische Belohnungsbelege füllen verbrauchte Bestände nicht erneut auf. Bereits abgeschlossene Quests erhalten keine rückwirkenden Item-Ausschüttungen.
- Neue Item-Vergaben und Paket-Inhalte erscheinen in den bestehenden Belohnungseinblendungen. Lange Belege werden vollständig über mehrere Seiten angezeigt.
- Die statische UI ist als `InventoryTile.prefab`, `InventoryDetail.prefab` und überarbeitetes `RewardCollection.prefab` gespeichert. Laufzeitcode befüllt vorhandene Ansichten und erzeugt keine statische Inventar-Hierarchie.

## Editor und Nachweise

Expliziter, wiederholbarer Installer: **Weedtopia → Presentation → Install Categorized Inventory**. Er repariert nichts automatisch beim Import oder Editorstart und bewahrt bereits installierte Layouts. Die Vorversion liegt in `Originals/` einschließlich Szene, Skripten, Content und Reward-Prefabs.

`content_preservation.json` dokumentiert den Vergleich der unveränderten Wirtschaft und Weltdefinitionen. Die abschließenden Test- und Game-View-Ergebnisse stehen in `VALIDATION.md`.
