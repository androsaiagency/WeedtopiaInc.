# Textprüfung vom 6. Oktober 2026

Die aktuelle Prüfung umfasst sämtliche 1.710 Texte pro Sprache in Deutsch, Englisch,
Spanisch, Französisch und Russisch. Alle Einträge werden auf Schlüsselabdeckung,
Duplikate, leere Werte, Platzhalter, Klammerstruktur, Zeilenumbrüche und ersetzte Zeichen
geprüft. Beide ausgelieferten Weedtopia-Grove-Schriften decken alle 206 erforderlichen
Zeichen ab. Die technische Prüfung ist von der sprachlichen Gesamtfreigabe getrennt.

185 gezielt geprüfte Schlüssel sind in `Tools/Localization/reviewed_ui.json` dokumentiert.
Die Korrekturen betreffen die sichtbare Oberfläche, Fehlermeldungen, Mitarbeiterkarten,
Rekrutierung, Baustellen, Belohnungen, Währungen und Bestätigungen. Die wichtigsten
semantischen Fehler waren vertauschte Geld-/Grinder-Produktnamen in allen vier
Fremdsprachen, angeblicher Erhalt statt Ausgabe von Premiumwährung, „Abreißen“ statt
„Ausbau“ und die Übersetzung einer Gebäude-Ausbaustufe als Tier. Weitere Korrekturen
betreffen Plattenspieler statt Rekrutierung sowie verlorene statt „verrottende“ Restzeit.

Die deutsche Oberfläche enthält jetzt „Spielen“, „Ausbauen“, „Mitarbeiter“, „Gegenstand“,
„Stufe“, „Aufgabe“ und „Rekrutierung“. Die Seltenheitsstufen heißen „Gewöhnlich“,
„Selten“, „Sehr selten“, „Extrem selten“ und „Legendär“. Zugehörige deutsche Dialog- und
Aufgabentexte sowie der Content-Fallback verwenden dieselben Begriffe. Die Währung
Grinder, Figuren- und Stadtnamen bleiben als benannte Spielbegriffe erhalten. Manager,
Coffeeshop, Shop, Team, Avatar, Park und Farm wurden einzeln im deutschen Kontext
beurteilt; sie sind gebräuchliche deutsche Wörter. Wortgleichheit allein ist deshalb
kein Beleg für eine fehlende Übersetzung. Der Audit führt alle wortgleichen Einträge
mit Schlüssel, Sprache und Begründung auf.

Die Quellprüfung erfasst 127 Laufzeitdateien, 396 Szenen-/Prefabdateien und 1.255
Contentfelder mit Textschlüsselreferenzen. Sie inventarisiert alle serialisierten
Captionfelder und prüft alle tatsächlich gebundenen `LocalizedLabel`-Schlüssel.
Erkennbare deutsche Wortreste in Fremdsprachen, eindeutige englische UI-Wörter im
deutschen Katalog und deutsche Literaltexte in indirekten Laufzeitformattern werden
als konkrete Fehlerkandidaten ausgewiesen. Diese Wortlisten erkennen nicht jede
denkbare Grammatik- oder Stilfrage.

Die tatsächliche Unity-Hierarchieprüfung deckte 20 verschiedene fehlende statische
Captionwerte auf, darunter Gebäudedetails, nächste Stufe, Stadtauswahl, Shopkategorien,
Namensfrage, Neustarttext, Aufgabenschaltflächen und Inventarverknüpfungen. Alle
erhielten stabile Schlüssel. Die explizite Autorierung verarbeitet die vollständigen
Produktions-Prefabordner und arbeitet nach Abhängigkeiten: verschachtelte Vorlagen
werden vor Eltern und Szenen repariert. Das Authoring-Receipt nennt verbleibende
ungebundene Captionkandidaten; der strikte Python-Audit verlangt eine aktuelle Prüfung
ohne solche Kandidaten. Die Reparatur berücksichtigt auch die archivierten Oberflächen.

Alte, ausdrücklich geprüfte Captionquellen stehen in
`Tools/Localization/reviewed_caption_sources.json` beziehungsweise
`legacy_caption_sources.json`. Nur der Editor verwendet diese Listen zur reparierbaren
Zuordnung vorhandener Beschriftungen auf stabile Schlüssel. Zur Laufzeit rendern
`LocalizedLabel` beziehungsweise die zuständigen Presenter den ausgewählten Katalog.
Es gibt keinen sprachabhängigen Mechanik- oder Datenbankpfad.

Die drei Inventar-Zeiteinheiten besitzen eigene Formatvorlagen für Stunden, Minuten und
Sekunden. Die Stadtauswahl verwendet einen nachgewiesenen Abwärtspfeil aus beiden
Spielschriften. Der vorher verwendete kleine Dreieckspfeil fehlte in diesen Schriften.

Die anschließende Game-View-Prüfung führte zu gezielten Textkorrekturen: Der deutsche
Restzeittext erzeugt mit abgekürzten Einheiten keinen doppelten Punkt mehr, der
englische Premiumtitel verwendet den Plural „Grinders“, das französische Glücksrad
heißt „Roue de la chance“, und der spanische Gratishinweis verwendet „Gratis“.
Die französische Premiumbestätigung setzt den dynamischen Gebäudenamen in Anführungszeichen,
damit die Formulierung keinen falschen Artikel voraussetzt.

Alle acht Baubeschleuniger von einer Minute bis 24 Stunden wurden in ihren Namen,
kurzen Inventartiteln und Beschreibungen erneut geprüft. Die fremdsprachigen
Inventartitel besitzen kurze erste Zeilen, damit die Dauer in der zweiten Zeile im
vorhandenen Kachelbereich Platz findet. Die Dauer war bei den kurzen Ein-Minuten-Titeln
im Katalog bereits vorhanden, wurde aber durch den langen Titel visuell verdrängt.
Echte deutsche „Std.“-Reste in fremdsprachigen Stundentiteln und ein englisches
„expire“ in der russischen 24-Stunden-Beschreibung wurden ebenfalls korrigiert.
Die Beschreibungen nennen die Verkürzung um höchstens die jeweilige Dauer.
Die numerischen Gegenstandswerte und Spielregeln bleiben erhalten.

Der Quellaudit vergleicht jetzt für sämtliche acht Baubeschleuniger in allen fünf
Sprachen die drei Textfelder mit der tatsächlichen Sekundenzahl aus `items.value`:
120 Prüfungen erkennen fehlende oder falsche Zahlen und unübersetzte Zeiteinheiten.
Für die kurzen Inventartitel muss die passende Dauer in der letzten Zeile stehen.
Damit reicht ein lediglich gefüllter Textschlüssel für diese Gegenstände nicht mehr
als bestandener Nachweis aus.

Es wurden keine Texte an externe Übersetzungsdienste übertragen. Eine vollständige
muttersprachliche Prüfung sämtlicher Erzähltexte in allen vier Fremdsprachen ist
weiterhin offen. Game-View-, Eingabe-, Geräte- und Erhaltungsnachweise werden separat
im Abschlussbericht der UI-Korrektur geführt.

Nach einer expliziten Autorierung lassen sich die strikten Textprüfungen wiederholen:

```powershell
python -X utf8 Tools/Localization/validate_catalogs.py
python -X utf8 Tools/Localization/audit_current.py --require-authored
```

Die Ergebnisse stehen in `Docs/Localization/catalog_validation.json`,
`Docs/Localization/authoring_receipt.json` und
`Docs/LocalizationRevision/2026-10-06/current_text_audit.json`.

Die explizite Autorierung und der strikte Quellaudit vor den letzten Game-View-
Textkorrekturen sind bestanden. Das dabei erzeugte Receipt enthält keine
ungebundenen Captionkandidaten. Der zweite
Autorierungsdurchlauf änderte keine Datei; die Reparatur ist für den geprüften
Projektstand idempotent. Der strikte Audit meldet null Kataloginvariantenfehler,
fehlende referenzierte Schlüssel, Fallback-Abweichungen, erkennbare deutsche
Wortreste in Fremdsprachen, eindeutige englische UI-Wörter im deutschen Katalog
und deutsche Literaltexte in den untersuchten Laufzeitformattern.
Die letzten Textkorrekturen bestehen erneut die Katalog-, Platzhalter-, Schrift-
und 120 Dauerprüfungen. Ein erneuter Autorierungs- und Game-View-Lauf prüft den
abschließenden Projektstand; dessen Ergebnis wird getrennt im Hauptbericht geführt.

Die getrennte Erhaltungsprüfung bestätigte alle 7.676 erfassten ursprünglichen
Transformationen und 2.038 GameObject-Identitäten. Unity-EditMode-, PlayMode- und
Game-View-Ergebnisse werden vom Hauptlauf separat festgehalten; diese Quell- und
Autorierungsbelege ersetzen keine Eingabeprüfung auf einem Gerät und keine
muttersprachliche sprachliche Gesamtabnahme.
