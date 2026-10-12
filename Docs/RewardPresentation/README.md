# Questbelohnungen und Sammlung

## Im Spiel

- **Aufträge:** Unter dem aktuellen Auftrag stehen die konkreten Belohnungen als Vorschau. Vorschauen verändern weder Bestände noch Zufallszustand.
- **Belohnung abholen:** Nach erfolgreicher Vergabe und Speicherung erscheint eine goldene Kiste mit Lichtstrahlen, funkelnden Akzenten und nacheinander eingeblendeten Belohnungskarten. Die Simulation pausiert währenddessen. „Weiter“ kann die Enthüllung beschleunigen; anschließend schließt es die Anzeige. Die Belohnungen sind bereits vergeben.
- **Sammlung:** Erreichbar im Questbuch, unter „Mein Team“ und direkt aus der Belohnungsanzeige. „Bestand“ zeigt verfügbares Geld, Grinder, Mitarbeiterkarten und Mitarbeiter. Mitarbeiterkarten in der Sammlung öffnen die vorhandene Teamverwaltung.
- **Erfolge:** Die Gründungsurkunde wird aus dem Abschluss der Anfangsquests abgeleitet. Beim fertigen Stadtfest erscheinen der Titel „Feststadt“ und der tatsächlich aktive Verkaufsbonus. Diese Einträge zeigen bestehende Freischaltungen; das Öffnen der Sammlung vergibt keine zusätzlichen Boni.
- **Verlauf:** Die letzten 100 Belohnungen, neueste zuerst. Historische Geldbeträge sind keine verfügbaren Bestände. Ältere Spielstände erhalten keine erfundene rückwirkende Historie; bereits abgeschlossene Meilensteine bleiben sichtbar.
- Dieselbe Darstellung und Historie werden für VIP-Belohnungen, Tagesbelohnungen und neu gezogene Mitarbeiter verwendet. Ein fehlgeschlagener oder wiederholter Anspruch erzeugt keinen zusätzlichen Eintrag.

## Architektur und Pflege

`RewardHistory` enthält typisierte Belege über bereits ausgeführte Vergaben. `ProgressionService`, `CityProjectService`, `DailyService` und `WorkerService.Spin` zeichnen die von ihnen vergebenen Werte auf. Die vorhandenen Wirtschaftswerte und Belohnungsmengen bleiben erhalten.

`RewardCatalog` berechnet Vorschauen, verfügbare Bestände und dauerhafte Auszeichnungen unabhängig von Unity. Die Save-Erweiterung ist optional und additiv zum bestehenden Schema 2; der Validator prüft die neuen Einträge. Die Darstellung darf keine Währungen, Karten oder Mitarbeiter vergeben.

Alle festen UI-Elemente sind in der Szene bzw. fünf wiederverwendbaren Prefabs unter `Assets/Weedtopia/Production/Rewards` gespeichert. Es gibt keine Erzeugung fester UI-Objekte beim Spielstart. Belohnungskarten verwenden einen festen Pool; Sparkles und Lichtstrahlen benötigen keine Objekt-Spawns. Animationen verwenden unskalierte Zeit und keinen Gameplay-Zufall.

Der explizite Menüpunkt **Weedtopia → Presentation → Install Quest Rewards and Collection** richtet die Funktion ein. Ein erneuter Aufruf erhält installierte Prefabs und manuelle Anpassungen. Keine automatische Reparatur bei Import, Start oder Domain Reload. Das bestehende Navigationsdock mit sieben Buttons bleibt erhalten.

Die Musik wird nicht geändert. Die Enthüllung verwendet den vorhandenen kurzen Erfolgsklang und respektiert die Spiellautstärke.

## Prüfungen

Die separaten Source-, Unity-Test- und Game-View-Ergebnisse stehen in `Docs/Implementation/Validation` und diesem Ordner. Der Prüfstand ist in [VALIDATION.md](VALIDATION.md) zusammengefasst. Automatische Prüfungen sind keine persönliche visuelle Abnahme. Hörtest, Eingabegefühl am Zielgerät und die abschließende Bewertung des „Wow“-Effekts bleiben beim Spieler.

Art-Herkunft und exakter Generierungsprompt: [ART_PROVENANCE.md](ART_PROVENANCE.md).

Die vor Beginn vorgefundenen Versionen geänderter Dateien liegen unter `Originals`. Sie sind eine Sicherung des zuvor bereits uncommitteten Projektstands und keine Rücksetzung auf Git HEAD.

`Tools/Implementation/author_reward_content.py` aktualisiert ausschließlich die `reward.*`-Texte idempotent. Die daneben verbliebenen einmaligen Patch-Helfer `implement_rewards.py`, `integrate_reward_ui.py` und `integrate_reward_validation.py` dokumentieren die Erstintegration und sollen nicht erneut ausgeführt werden. Der reguläre Einstieg für Einrichtung und Reparatur ist der explizite Unity-Menüpunkt.
