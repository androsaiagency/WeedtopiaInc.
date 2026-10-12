# Mehrsprachigkeit

Jede der fünf Sprachdateien enthält aktuell **1.681 Texte**. Die Sprachen Deutsch, Englisch, Spanisch, Französisch und Russisch werden vollständig mit dem Spiel ausgeliefert. Der Spieler wählt sie unter **Einstellungen → Spiel → Sprache**. Die Namen der Sprachen erscheinen in ihrer eigenen Sprache. Die Auswahl gilt für beide Städte und wird unabhängig vom Spielstand in der lokalen Einstellung `Weedtopia.Language` gespeichert. Standard ist Deutsch.

## Dateien und Zuständigkeiten

- `Assets/Weedtopia/Game/Localization/de.json`: deutscher Referenzkatalog.
- `Assets/Weedtopia/Game/Localization/en.json`, `es.json`, `fr.json`, `ru.json`: Übersetzungen mit denselben stabilen Schlüsseln.
- `LanguageCatalog.asset`: explizite Referenzen auf Sprachdateien, Anzeigenamen und Zahlen-/Datumsformate; wird vom bestehenden `BurningHillsContent.asset` referenziert.
- `LanguageSelector.prefab`: wiederverwendbare Sprachauswahl; beide Stadt-Szenen enthalten eine sichtbare Instanz in ihrer Einstellungs-Hierarchie.
- `LocalizationService`: Unity-unabhängige, indizierte Textsuche und Sprachevents. `GameDefinitions.Text` bekommt diese Textquelle injiziert. Simulation und Speicherdaten bleiben sprachunabhängig.
- `LocalizedLabel`: Bindung statischer, im Editor angelegter Beschriftungen. Spielername und Texteingaben werden nicht übersetzt. Dynamische Anzeigen bleiben beim jeweiligen Presenter.
- `Docs/Localization/catalog_validation.json`: Vollständigkeit, Platzhalter und Buchstabenabdeckung der Schriften.

Die ursprünglichen deutschen Texte im bestehenden Content-JSON bleiben für die Kompatibilität von älteren Werkzeugen erhalten. Für neue Oberflächentexte ist der Sprachkatalog zuständig. Beim normalen Laden wird er vor der Content-Validierung injiziert.

## Neue Texte hinzufügen

1. Einen dauerhaften, sprechenden Schlüssel wählen, z. B. `settings.notifications` oder `story.berta.chapter5.title`. Den Schlüssel bei späteren Textkorrekturen beibehalten.
2. Einen Eintrag mit diesem Schlüssel in **jede** Sprachdatei eintragen. Beispiel:

```json
{"key":"settings.notifications","value":"Benachrichtigungen"}
```

3. Statische UI: am bestehenden Textobjekt `LocalizedLabel` hinzufügen und den Schlüssel setzen. Dynamische UI: `root.Definitions.Text(key)` bzw. `root.Definitions.Format(key, arguments)` verwenden. In statischen Präsentationsformatierern ist `GameText` verfügbar; Domain-/Application-Code verwendet die injizierten Definitionszugriffe.
4. Ganze Sätze als Vorlage speichern, z. B. `{0}: auf Stufe {1} ausbauen`. Keine Sätze aus übersetzten Wortfragmenten zusammensetzen. Platzhalter dürfen ihre Reihenfolge ändern; Nummern, Häufigkeit und Formatbezeichner müssen erhalten bleiben.
5. Unity-Menü **Weedtopia → Localization → Validate Catalogs** ausführen. Fehlende Übersetzungen müssen vor der Freigabe ergänzt werden. Laufzeit-Fallback ist Deutsch; ganz unbekannte Schlüssel erscheinen sichtbar als `[key]`.

Die aus der bestehenden Oberfläche migrierten Schlüssel unter `presentation.*` und `authored.*` sind ebenfalls dauerhaft. Ihre kurzen Hash-Anteile unterscheiden ursprünglich gleichartig benannte Texte; sie werden bei Textkorrekturen nicht neu erzeugt. `source_inventory.json` dokumentiert den ursprünglichen Wortlaut.

## Eine weitere Sprache hinzufügen

1. Den deutschen JSON-Katalog als neue Datei kopieren und `language` auf den neuen Sprachcode setzen, z. B. `it`. Alle Werte übersetzen.
2. In `LanguageCatalog.asset` einen Eintrag ergänzen: Code `it`, eigener Name `Italiano`, Culture `it-IT`, Referenz auf `it.json`.
3. Die Schriften auf alle benötigten Zeichen prüfen. Bei einer neuen Schrift deren Lizenz dokumentieren.
4. Katalogvalidierung und die Sprachwechsel-Tests ausführen. Die Einstellungs-Auswahl liest den Katalog; für neue Sprachen ist keine feste Auswahlliste im Laufzeitcode zu erweitern.

## Installation und Prüfung

**Weedtopia → Localization → Install or Repair** ist eine ausdrücklich ausgeführte, wiederholbare Installation. Sie ergänzt Textbindungen und fehlende Sprachwahl-Referenzen in beiden Städten und den bestehenden UI-Prefabs. Sie läuft nicht automatisch beim Import oder beim Start. Manuelle offene Szenen müssen zuvor gespeichert sein. Bereits hinzugefügte Sprachkatalog-Einträge und eigene Referenzen bleiben erhalten.

Die Übersetzungs-Erstfassung wurde ausschließlich auf dem vorhandenen lokalen Ollama-Modell erzeugt. Zentrale Menütexte, Satzvorlagen, Einleitung, Hauptauftrag-Anweisungen beider Städte, Gebäudenamen, Rollen und Shop-Zeitpakete wurden anschließend zusätzlich geprüft und überarbeitet. Die geprüften Formulierungen sind in `reviewed_core_overrides.txt` und `reviewed_gameplay_overrides.txt` dokumentiert; maßgeblich bleiben die ausgelieferten Sprachdateien. Im Spiel selbst werden weder das Modell noch ein Internetzugang benötigt. Eine vollständige muttersprachliche Redaktion der Bewohnergeschichten ist ein eigener offener Qualitätsnachweis.

`Tools/Localization/Archive/2026-10-04/` enthält die einmaligen Erstellungswerkzeuge als historische Dokumentation. Diese archivierten Skripte sind nicht für die laufende Pflege vorgesehen. Der Cache und die Übersteuerungsdateien sind keine zur Laufzeit geladenen Daten. Für reguläre spätere Änderungen die Sprachdateien direkt bearbeiten. Die aktive Prüfung `Tools/Localization/validate_catalogs.py` liest nur die tatsächlich ausgelieferten Dateien und prüft gleiche Schlüssel, leere/mehrfache Einträge, Platzhalter, Zeilenumbrüche und Schriftzeichen. `audit.py` prüft zusätzlich Textreferenzen und die erhaltenen Ausgangsdaten.

Individuelle Gebietsbezeichnungen werden über `DistrictRange.labelKey` lokalisiert; die bestehenden deutschen Labels und Kamera-Geometrien bleiben erhalten. Automatische Textbindungen werden ausschließlich an statischen Beschriftungen verwendet. Die Installation entfernt doppelte oder an dynamischen Presenter-Feldern angelegte Bindungen.

Die aktuelle Validierung ist in `validation_summary.json` zusammengefasst. Sprachwahl und Beibehaltung des Fortschritts werden in tatsächlichen PlayMode-Tests geprüft. Game-View-Bilder werden mit isoliertem Prüfspielstand aufgenommen; persönliche Spracheinstellungen werden danach zurückgesetzt. Die Screenshots ersetzen weder physische Gerätetests noch eine muttersprachliche Redaktion.
