# Direkte Menüs und Stadtanzeige

Die Änderung ist in `Assets/Weedtopia/Game/Scenes/BurningHills.unity` installiert.

- Untere Leiste: Übersicht, Farm, Stadt, Siedlung, Industrie, Aufträge und Mein Team. Alle sieben Schaltflächen verwenden das vorhandene `WorldNavigationButton.prefab`.
- Oben links: Name der aktiven Stadt über den Inhaltsschlüssel `city.<activeCity>`, aktuell Burning Hills.
- Oben mittig: Geld, Grinder und Einwohner. Im schmalen Hochformat bleibt die Ressourcengruppe zentriert und rückt unter Stadtname und Einstellungen.
- Oben rechts: runder Einstellungen-Button aus demselben Button-Prefab.
- Mein Team: Kartenbestand, Spin, Freischaltungs-/Kartenhinweis und bestehende Worker-Verwaltung.
- Aufträge: aktuelle Quest mit Belohnung und Zielnavigation, Auftragsliste, Tagesbelohnungen und Aufbauhilfe.
- Rathaus und Speichern sind keine Navigationspunkte mehr. Das Rathausgebäude bleibt für den bestehenden Questbesuch und seine Statistikansicht erreichbar. Der Questtext erklärt den neuen Spin-Zugang.
- Die ehemaligen statischen Menüobjekte liegen deaktiviert unter `ArchivedTownMenu`; es gibt keinen Öffnungs-Handler mehr. Bestehende Referenzen und ursprüngliche Assets bleiben erhalten.

## Speicherung

Die bestehende Sitzung speichert alle 30 aktiven Spielsekunden, nach erfolgreichen Transaktionen und während des Intros. Zusätzlich speichert `GameRoot` ohne Erfolgsmeldung beim Pausieren, Fokusverlust und Beenden. Das Schließen der Einstellungen speichert die zuletzt gewählte Lautstärke und UI-Skalierung unmittelbar. Speicherfehler werden weiterhin angezeigt.

## Aufbau

Die festen UI-Objekte sind in Szene und Prefabs gespeichert. Zur Laufzeit wird dafür kein neues GameObject erzeugt. `ResourceCounter.prefab` wird für Geld, Grinder, Einwohner und Karten wiederverwendet. `CityStatusBar.prefab` enthält Stadtname, Ressourcen und Einstellungen; `CityStatusBar.cs` passt die Gruppen an die Safe Area an. Bestehende Dialoge passen sich ebenfalls an die verfügbare Safe Area an.

Der explizite Editor-Befehl lautet **Weedtopia → Presentation → Install Direct Menu Layout**. Er benötigt keinen automatischen Import-/Startup-Reparaturhandler. Er lehnt PlayMode und ungespeicherte Szenen ab und ist wiederholbar.

## Nachweise

- EditMode: **100/100 bestanden**, keine ausgelassenen Tests. Bericht: `../Implementation/Validation/20260927_173429_EditMode_DirectMenuRegression.json` und zugehörige XML-Datei.
- PlayMode: **36/36 bestanden**, keine ausgelassenen Tests. Bericht: `../Implementation/Validation/20260927_174049_PlayMode_DirectMenuRegression.json` und zugehörige XML-Datei.
- Neue Verhaltensprüfungen: direkter Touch-Zugang zu Übersicht, Team und Einstellungen; Speicherung geänderter Einstellungen ohne Speichern-Button; Kartenanzeige und genau ein Kartenverbrauch pro Spin mit gespeichertem Worker; Erreichbarkeit der Questfunktionen; Autosave nach 30 Sekunden.
- `preservation.json`: kein ursprüngliches Szenenobjekt entfernt; 8.925 von 8.960 ursprünglichen serialisierten Objekten unverändert. Die 35 Änderungen beschränken sich auf UI und Währungsreferenzen. 18 neue Szenenblöcke ergänzen Prefabinstanzen und Menüstruktur.
- `idempotence.json`: erneute Installation verändert die Szenendatei nicht.
- `gameview_receipt.json` und `Screenshots/`: echte Unity-PlayMode-Game-View-Aufnahmen für 1920×1080, 1366×768 bei UI-Skalierung 1,15, 2560×1080 und 1080×1920 sowie Teamansichten ohne Karten und vor Freischaltung.
- `ui_overflow.json`: **bestanden, 0 Probleme** über alle vier Auflösungen und 18 aktuelle Game-View-Aufnahmen. Der Fingerprint stimmt mit `gameview_receipt.json` überein.

Touch-Eingaben wurden im Unity Input System simuliert. Die Aufnahmen ersetzen weder einen physischen Gerätetest noch die gestalterische Abnahme durch den Benutzer.
