# Sprachwechsel und lesbare Dialoge – Abschluss am 7. Oktober 2026

Die gemeldeten Fehler sind im Unity-Projekt behoben. Die abschließenden Prüfungen beziehen sich auf den Stand nach der letzten Inventar- und Textkorrektur. Der Arbeitsordner trägt das Startdatum 6. Oktober; die letzten Testläufe endeten nach Mitternacht deutscher Ortszeit.

## Verhalten und Ursachen

Die Sprachliste berechnet ihren Platz oberhalb und unterhalb des Auswahlfeldes aus dem tatsächlichen sicheren Bildschirmbereich. Sie öffnet auf der geeigneten Seite und begrenzt ihren scrollbaren Ausschnitt auf diesen Platz. Deutsch und die letzte Sprache sind bei minimaler und maximaler UI-Skalierung erreichbar. Die bisherige feste Höhe und die einmalige automatische Umkehr des Unity-Dropdowns konnten die erste Zeile außerhalb des Bildschirms platzieren.

Fehlende Sprachbindungen und bereits gerenderte Textcaches verursachten gemischte Sprachen nach einem Wechsel. Statische Beschriftungen verwenden jetzt geprüfte stabile Schlüssel. Kopierte Überschriften holen den aktuellen Text auch aus inaktiven Quellen. Offene Premiumangebote, Gespräche, Belohnungen, Rekrutierungsansichten, Inventarinhalte sowie Profil- und Shopmeldungen werden neu gerendert. Die zugehörigen Spielaktionen werden dabei nicht wiederholt; laufende Angebote und Fortschritt bleiben erhalten.

Der Premiumdialog nutzt die vorhandenen Controls mit einem Layout für die verfügbare Breite. Der Dialogkörper bleibt unskaliert. Text, Beschriftungen und Buttonhöhe haben Untergrenzen von 22, 20 und 50 dichteunabhängigen Displayeinheiten. Im schmalen Hochformat stehen die Aktionen untereinander. Die Bestätigung beschreibt die Ausgabe und Abbuchung nach ausdrücklicher Bestätigung.

Das Inventar übersetzt unter anderem Bauzeit, Register, Kategorien, Zeiteinheiten und Stufen. Kacheln reservieren getrennte Flächen für Icon, Mengenanzeige und Titel. Die tatsächlich benötigte Texthöhe bestimmt die Kachelhöhe; zusätzlicher Platz wird im bestehenden Scrollinhalt bereitgestellt. Lange Titel werden vollständig angezeigt. Die acht vorhandenen Einträge bleiben als nominelle Kapazität erhalten. Die Baubeschleuniger haben kompakte mehrsprachige Titel mit ihrer tatsächlichen Dauer.

## Textprüfung

Alle 1.710 Einträge je Sprache in Deutsch, Englisch, Spanisch, Französisch und Russisch wurden technisch geprüft. 185 Schlüssel wurden gezielt sprachlich überarbeitet. Die Korrekturen umfassen Währungen, Premiumbestätigungen, Gebäudeausbau, Mitarbeiter, Seltenheitsstufen, Rekrutierung, Glücksrad, Inventar und bisher ungebundene Beschriftungen.

Der gerichtete Daueraudit prüft acht Baubeschleuniger × drei Textfelder × fünf Sprachen, also 120 Felder, gegen den jeweiligen numerischen Itemwert. Fehlende Dauer, falsche Zahl und eine deutsche Einheit im englischen Text wurden in drei gezielten Mutationen erkannt. Die numerischen Itemwerte und die übrigen Gameplaydaten wurden dabei nicht verändert.

Der strikte Audit erfasst 127 Laufzeitdateien, 396 Szenen-/Prefabdateien, 1.255 Contentreferenzen, 276 gebundene Labelschlüssel und 465 Captionfelder. Es verbleiben keine ungebundenen Captionkandidaten, fehlenden Referenzen, deutschen Fallback-Abweichungen oder erkannten Literal-/Sprachrestfehler. Beide Grove-Schriften decken alle 206 erforderlichen Zeichen ab. Vollständige muttersprachliche Prüfung sämtlicher Erzähltexte ist weiterhin ein offener eigener Nachweis.

## Abschließende Nachweise

| Prüfung | Ergebnis | Beleg |
|---|---|---|
| Explizite Unity-Installation | Bestanden; zweiter Durchlauf ändert keine Datei | [install.json](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/install.json>) |
| EditMode: Kataloge, beide Städte, Premium- und Inventargeometrie | 17/17 bestanden, 0 übersprungen | [Testresultate](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/Implementation/Validation/20261007_000552_EditMode_LocalizationRevisionFinal.xml>) |
| PlayMode: Sprachwechsel, Presenter, Shop, Belohnungen und Spielfluss | 46/46 bestanden, 0 übersprungen | [Testresultate](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/Implementation/Validation/20261007_000740_PlayMode_LocalizationRevisionFinal.xml>) |
| Game View | 31 frische PNG, 700 Assertions bestanden; 43 Raycast-/Pointerprüfungen und 40 Prüfungen der Inventarflächen | [gameview_receipt.json](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/gameview_receipt.json>) |
| Kataloge, Referenzen, Dauerwerte und Textbindungen | Bestanden; 0 Fehler | [current_text_audit.json](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/current_text_audit.json>) |
| Schlüssel, Platzhalter, Zeilenumbrüche und Schriften | Bestanden | [catalog_validation.json](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/Localization/catalog_validation.json>) |
| Erhaltung der ursprünglichen Assets und Einstellungen | Bestanden; 1.226 Baseline-Dateien geprüft, 7.676 Transformationen und 2.038 GameObject-Identitäten erhalten | [preservation.json](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/preservation.json>) |

Die Game-View-Matrix umfasst 1920×1080, 1644×762, 2340×1080 und 1080×1920, Dichte 1 und 3 sowie die reale UI-Slider-Spanne 0,85 bis 1,15. Alle fünf Sprachen und beide Städte sind enthalten. Die Auswahl erfolgt in den relevanten Fällen über echte Unity-Raycasts und Pointerevents. Dies ist eine automatisierte Game-View-Prüfung; eine physische Touchprüfung auf dem Handy wurde nicht durchgeführt.

Die visuelle Nachprüfung bestätigt erreichbares Deutsch im Querformat, deutsche Einstellungen im Hochformat, übersetzte und große Premiumcontrols, vollständige EN/ES/FR Inventartitel mit Dauer sowie den korrigierten deutschen Satz „Davon verfallen 30 Sek.“. Bilder des ersten Reviewstands wurden in `BeforeFinalReview` erhalten; die Dateien unter `Screenshots` zeigen den finalen Stand.

Ausgewählte finale Aufnahmen:

- [Deutsch im Querformat-Dropdown bei maximaler Skalierung](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/Screenshots/phone-max-ru-dropdown.png>)
- [Deutsche Einstellungen im Hochformat nach Sprachwechsel](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/Screenshots/portrait-max-ru-deutsch.png>)
- [Englische Premiumbestätigung](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/Screenshots/en-premium-landscape.png>)
- [Englisches Inventar mit vollständiger Dauer und getrennten Flächen](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/Screenshots/en-inventory-landscape.png>)
- [Deutsches Inventardetail im Hochformat](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/Screenshots/de-inventory-portrait-detail.png>)

## Reproduzierbarkeit und Grenze

Die Autorierung läuft ausdrücklich über `Tools/Localization/Install-LocalizationRevision.ps1`; der Capture über `Tools/Localization/Run-LocalizationRevision.ps1`. Beide verhindern einen zweiten Unity-Prozess und sichern die vier relevanten Projekt-/Renderereinstellungen. Die Captures verwenden isolierte Speicherdateien und stellen die ursprüngliche Sprachpräferenz wieder her. Am Ende läuft kein Unity-Prozess und es bleibt kein UnityLockfile zurück. Vorhandene fremde Änderungen wurden erhalten; es wurde kein Commit, Push oder Deployment durchgeführt. Projekttexte wurden nicht an externe Übersetzungsdienste übertragen.

Diese Lieferung enthält die Projektkorrekturen und lokalen Prüfungen. Eine neue Android-APK wurde in diesem Durchlauf nicht gebaut; eine vorhandene Handyinstallation enthält die Korrekturen erst nach einem neuen Build.

Der detaillierte sprachliche Umfang und die Grenzen des Reviews stehen in [text_review.md](<D:/Unity Games/Weedtopia Inc/WeedtopiaInc/Docs/LocalizationRevision/2026-10-06/text_review.md>).
