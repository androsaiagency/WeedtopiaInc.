# Weedtopia – tägliches Glücksrad

Das Glücksrad ist in Burning Hills direkt über den **runden Rad-Button in der unteren Leiste** sowie über **Aufträge → Glücksrad** und **Inventar → Glücksrad** erreichbar. Der runde Kisten-Button öffnet das Inventar. Ein Stern am Rad-Button zeigt den verfügbaren Gratis-Dreh an. Das Rad ist ein eigenes, petrolgrünes und goldenes UI mit Leuchtrand, animierter Drehung und vorhandenen Weedtopia-Item-Symbolen. Es kopiert keine Grafik aus der Referenz.

## Spielregeln

- Pro UTC-Kalendertag werden zehn unterschiedliche Items aus dem konfigurierten Pool zufällig ausgewählt. Jede verbliebene Belohnung hat dieselbe Gewinnchance. Ausgeloste Felder verschwinden nach der Animation.
- Preise für die zehn Drehs: **0, 50, 100, 200, 500, 1.000, 2.000, 2.000, 2.000, 2.000 Grinder**. Insgesamt kosten die neun optionalen Drehs 9.850 Grinder. Danach ist das Rad für den Tag leer.
- Kostenpflichtige Drehs erfordern eine Bestätigung mit dem exakten Preis. Fehlendes Guthaben, Abbruch und wiederverwendete Bestätigungen lösen keinen Dreh aus.
- Um **00:00 UTC** erneuern sich alle zehn Felder und der kostenlose Dreh gemeinsam. Der sichtbare Countdown benutzt dieselbe Zeitquelle. Verpasste Tage sammeln keine kostenlosen Drehs an. Die bestehende höchste gespeicherte Zeit verhindert einen zusätzlichen Gratis-Dreh durch Zurückstellen der Uhr.
- Gewinne sind Inventar-Items, werden nicht automatisch benutzt und lassen sich dort inspizieren, öffnen bzw. aufteilen. Der Pool enthält alle acht Baubeschleuniger, Kartenpaket, Spezialistenvertrag, Geldkiste und Grinder-Beutel. 1-Minuten-Beschleuniger werden dreifach, 5-Minuten-Beschleuniger zweifach, alle anderen Items einfach vergeben.
- Gewinn, Feldverbrauch, Grinder-Abzug, Zufallszustand und Quittung werden vor dem Animationsbeginn gemeinsam gespeichert. Bei einem Speicherfehler stellt der Anwendungsdienst diese Änderungen wieder her. Schließen überspringt die Animation und bewahrt den bereits gespeicherten Gewinn im Inventar und Verlauf.

## Architektur und Pflege

Die Regeln und der tägliche Zustand sind unabhängig von Unity. `LuckyWheelService` verwendet die injizierte Uhr; das Rad hat einen eigenen gespeicherten Zufallszustand und verändert die Rekrutierungs-Zufallsfolge nicht. `GameSession` bildet die Speichergrenze. Die optionale `GameState.luckyWheel` ergänzt alte Spielstände ohne Änderung der Schema-Version. Die bestehende Save-Prüfung validiert den Zustand und Quittungen.

`wheelRewards` in `Assets/Weedtopia/Game/Content/BurningHills.json` ist der explizite, erweiterbare Pool. IDs und Mengen müssen zum Item-Katalog passen; mindestens zehn unterschiedliche Einträge sind erforderlich. Die Preiskurve und zehn Felder sind die verbindlichen Regeln dieser Funktion.

Alle statischen Controls sind in `Assets/Weedtopia/Production/LuckyWheel/LuckyWheel.prefab` angelegt. Die zehn Felder sind Instanzen von `WheelPrize.prefab`. Der native UI-Graphic zeichnet die Geometrie ohne Laufzeit-GameObject-Erzeugung oder externe Bildressourcen. Der explizite Editor-Installer ist **Weedtopia → Presentation → Install Daily Lucky Wheel**. Er verwendet vorhandene Prefabs und bewahrt bereits installierte Geometrie und manuelle Referenzen.

## Manuell prüfen

1. Neues Spiel oder vorhandenen Spielstand öffnen; Intro abschließen. Unter Aufträgen oder im Inventar das Glücksrad öffnen.
2. Items anklicken und Beschreibung prüfen. Kostenlos drehen: zehn Felder werden neun, das Guthaben bleibt unverändert und der Gewinn erscheint nach der Drehung in der bestehenden Belohnungspräsentation.
3. Den ersten bezahlten Dreh öffnen und abbrechen: kein Abzug. Erneut öffnen und bestätigen: 50 Grinder werden einmal abgezogen. Bei fehlendem Guthaben bleibt der nächste Dreh deaktiviert.
4. Während einer Drehung schließen oder nach einem Gewinn neu starten. Gewinn und verbleibende Felder müssen erhalten bleiben.
5. Das erhaltene Item im Inventar ansehen. Die Countdown-Anzeige und Bedienbarkeit auf dem eigenen Bildschirm prüfen.

Automatische Ergebnisse und tatsächliche Game-View-Bilder werden neben diesem Dokument gespeichert. Eine bestandene automatische Prüfung ersetzt keine persönliche Abnahme von Look & Feel und Bedienung.
