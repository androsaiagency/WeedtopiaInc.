# Weedtopia Inc – Gamedesign des implementierten Spiels

Stand: 5. Oktober 2026. Dieses Dokument beschreibt die produktive lokale Spielversion und ersetzt den älteren Stand mit Entwicklungsvisuals. Fachliche Inhaltsquelle ist `Assets/Weedtopia/Game/Content/BurningHills.json`, Content-Version 3. Detailnachweise und noch offene praktische Abnahmen bleiben in den verlinkten Revisionsberichten erhalten. Ein bestandener Techniknachweis erteilt keine Veröffentlichungsgenehmigung.

## Spielerlebnis und Einstieg

Der Spieler baut mit Rocko und den Bewohnern ein dauerhaftes Unternehmen auf. Die feste isometrische 2D/2.5D-Perspektive verbindet Farm, Stadt, Siedlung und Industrie. Produktion, Warenwege und Mitarbeiter machen den Fortschritt sichtbar. Persönliche Stadtgeschichten und gemeinsam geschaffene Orte begleiten das Wachstum über den Tutorialabschluss hinaus.

Ein neues Spiel beginnt in Burning Hills mit genau 1.000 Geld und 50 Grindern. Intro überspringen und erneut ansehen verändern diese Ausstattung nicht. Der Spielername hat 2–24 Zeichen; Profil und Avatar können später geändert werden. Die genehmigte erste Verkaufskette kostet 875 Geld und lässt ohne Geldprämien 125 Reserve. Diese Balance ist umgesetzt, kein noch offener Vorschlag. Siehe [Gebäudebalance](../BalanceRevision/validation.json).

Die ersten 16 Hauptaufträge erschließen schrittweise die vier Quartiere und führen bis zum freien Spiel. Eine zweite vollständige Folge mit 16 eigenen Hauptaufträgen erschließt Big Rock City. Automatisierte Fortschrittsläufe benutzen öffentliche Spielbefehle und belegen Finanzierbarkeit und Regelablauf. Ihre Simulationssekunden sind keine gemessene menschliche Spieldauer.

## Produktions- und Aufbauablauf

- **Farm:** Anbau, Verarbeitung und Farmlager bilden die erste Warenkette. Ernte verbleibt in begrenzten Puffern. Manuelle Arbeit aktiviert einen begrenzten Arbeitszeitraum; passende Manager arbeiten dauerhaft. Fahrer transportieren tatsächlich entnommene Fracht.
- **Stadt:** Das Stadtlager nimmt Lieferungen an und verteilt an Cafés und später die Butterfabrik. Verkauf verändert Geld und die zugehörigen Stadtstatistiken. Die erste Warenannahme ist bereits im Ruinenzustand möglich, damit der Tutorialpfad keinen Freischaltkreis erzeugt.
- **Siedlung:** Freie Bauplätze werden dauerhaft Wohnhaus oder Park. Wohnhäuser liefern Einwohner und tägliche Teamkarten. Die Wahl erhält mindestens eine erreichbare Haus- und Parkmöglichkeit. Hausmeister, Parkmanager und die garantierten Tutorialmitarbeiter werden über dieselben Mitarbeiterregeln zugewiesen.
- **Industrie:** Butterfabrik, Supermarkt und Bäckereien erweitern die Kette. Zwei veredelte Blüten ergeben eine Butter, eine Butter ergibt eine Backware. Verteilung berücksichtigt verfügbare Ziele; volle Ziele vernichten keine Lieferfracht. Der Supermarkt verwendet Einwohner als Wirtschaftsgrundlage und liefert tägliche Grinder.
- **Freies Spiel:** Ausbau, Produktion, Mitarbeiter, Karten, Inventar, Glücksrad, Stadtprojekte, Geschichten und Statistiken bleiben aktiv. Optionale Erweiterungen erzeugen keine zusätzliche Managerpflicht für bereits definierte Tutorialabschlüsse.

Der Inhaltskatalog enthält zwölf Gebäudedefinitionen einschließlich freier Plätze, zwölf Mitarbeiterrollen, 112 Gebäudeinstanzen und 70 Warenwege. Jede der beiden Städte besitzt 56 Gebäudeplätze und 35 Wege. Gebäude verwenden getrennte Ressourcenpuffer, tatsächliche Kapazitäten, Durchsatz, Bauzeit und Fahrerplätze. Die Ausbaustufen bis Level 5.000 verwenden die genehmigten Kostenkurven und zeitgesteuerten sichtbaren Tierwechsel.

Mitarbeiter haben Rolle, Seltenheit, Attribute, Level und Rang. Ihre Zuweisung, Slotwahl und Freistellung laufen über dasselbe Teamsystem. Grinderkäufe erfordern die Bestätigung des konkreten Angebots. Inventargegenstände und Bauzeitverkürzungen verwenden eigene Daten und gespeicherte Verbrauchsbelege; sie ersetzen keine Produktionsmengen.

## Zwei dauerhafte Städte

Burning Hills bleibt die Startstadt. Sobald dort einmal 1.000 Einwohner erreicht wurden, bietet Rocko die Übernahme von Big Rock City an. Ein späterer Einwohnerrückgang nimmt dieses Angebot nicht zurück. Die Annahme gewährt einmalig 2.500 Geld und zwei Teamkarten. Die neue Stadt produziert erst nach der Übernahme.

Beide Städte besitzen eigene Gebäude, Lieferungen, Hauptaufträge, Einwohnerzahlen, Tagesertragskonten, Stadtstatistiken, Geschichten und Erinnerungen. Geld, Grinder, Teamkarten, Inventar, verfügbare Mitarbeiter und Sammlung gehören zum gemeinsamen Unternehmen. Zugewiesene Mitarbeiter bleiben an ihrem Arbeitsplatz. Beide übernommenen Städte werden auch während der Abwesenheit weiterberechnet.

Der Stadtname oben links öffnet die Stadtwahl; der Auftragsbereich bietet Rockos Auftrag ebenfalls an. Reisen speichert vor dem Szenenwechsel. Fortsetzen öffnet die zuletzt besuchte Stadt. Burning Hills behält seine bereits gebauten Gebäude und seinen Fortschritt. Die neue Stadt liegt am alten Sandsteinbruch mit den Quartieren Felsgärten, Kupfergasse, Felsstufen und Altes Werkviertel. Ihre eigene Sandstein-/Kupferdarstellung erhält die ursprünglichen Gebäudeformen und Gameplay-Anker. Einzelheiten: [Big Rock City](../BigRockCity/README.md).

## Bewohnergeschichten und Gemeinschaft

Zehn dauerhafte Bewohner in Burning Hills und sechs in Big Rock City haben zusammen 64 persönliche Kapitel, 48 rotierende Alltagsaufträge und acht Gemeinschaftsprojekte. Ankunft und Kapitel hängen an gespeicherten Einwohnergrenzen. Eine einmal erreichte Grenze bleibt erhalten; ein freigeschalteter Bewohner verschwindet bei sinkender Einwohnerzahl nicht.

Persönliche Kapitel werden in Reihenfolge abgeschlossen und haben Vorrang vor wiederkehrenden Aufgaben. Ausgewählte Kapitel bieten gespeicherte Entscheidungen mit eigener Reaktion und sichtbarer Erinnerung. Diese Entscheidungen ändern keine Produktionsformeln. Die Bewohner bleiben dauerhaft in der Stadt. Erinnerungen überleben Laden und Städtewechsel.

Pro Stadt können höchstens drei Bewohner-/Gemeinschaftsaufträge gleichzeitig aktiv sein. Aufträge verfallen nicht. Produktions-, Liefer- und Gesprächsziele zählen neue Aktivitäten seit Annahme, Gebäudeziele den aktuellen Zustand. Wiederkehrende Aufgaben wachsen mit der Stadt und werden frühestens 300 Simulationssekunden nach ihrem Abschluss erneut angeboten. Erfüllte Aufgaben müssen ausdrücklich abgeholt werden; Belohnungen werden genau einmal vergeben. Einzelheiten: [Burning-Hills-Geschichten](../VipStories/README.md) und [Big-Rock-Geschichten](../BigRockCity/README.md).

Das vorhandene Stadtfest besitzt seinen Fortschritt, erzählten Abschluss und Wirtschaftsbonus. Zusätzliche Festdekoration und eine stärkere sichtbare Schlussinszenierung sind gestalterische Weiterarbeit; ihr Fehlen darf nicht als fehlende Implementierung des Fortschrittssystems beschrieben werden. Stand: [Stadtfest](../CityFestivalRevision/README.md).

## Welt, Oberfläche und Bedienung

Der Build enthält `Bootstrap.unity`, `BurningHills.unity` und `BigRockCity.unity`. Statische Weltobjekte, Bewohner, Kamera, Canvas, Interaktionsflächen und feste Fenster sind in Szenen und wiederverwendbaren Prefabs sichtbar angelegt. Explizite Installer ergänzen oder reparieren autorisierte Assets; normale Starts, Import und Domain Reload führen keine Szeneninstallation aus. Variable Listen verwenden vorhandene Zeilenprefabs; wechselnde Gebäudestufen verwenden den produktiven Visualprovider.

Die obere Statusleiste zeigt Stadt, Geld, Grinder und Einwohner; Einstellungen sind oben rechts erreichbar. Die untere runde Navigation bietet die bestehenden Ansichten einschließlich Übersicht, Team, Inventar, Glücksrad, Aufträge und Shop. Speichern geschieht automatisch. Gebäudedetails zeigen Zustand und Aktionen am gewählten Ort, Mitarbeiteransichten bieten Filter und Zuweisung, Bestätigungen machen Ausgabe oder Verbrauch konkret sichtbar.

Mausauswahl, Tastaturbewegung, Ziehen und Zoom sowie Touch-Pan/Pinch sind implementiert. Safe Area, Smartphone-Hoch-/Querformat, Displaydichte und UI-Skalierung werden durch die vorhandenen Layoutsysteme behandelt. Automatische Pointertests und gerenderte Bilder sind noch keine vollständige physische Geräteabnahme. Siehe [Smartphone-Oberfläche](../PhoneUiRevision/README.md).

## Grafik, Audio und Sprachen

Die 251 produktiven 2D-Gebäudevarianten sind integriert und ausdrücklich freigegeben. Burning Hills und Big Rock City verwenden eigene Darstellungskataloge. Die feste isometrische Kamera, Kollisionsflächen, manuelle Offsets und Gameplay-Anker bleiben erhalten. Richtungsanimationen mit acht Richtungen und sechs Frames sowie Bewohner-, Fahrzeug- und Umgebungsdarstellung sind vorhanden. Frühere Quellen und reversible Experimente bleiben archiviert.

Die eigene Schriftfamilie **Weedtopia Grove** mit Regular/Bold ist in den produktiven Szenen und Textprefabs integriert. Deutsch, Englisch, Spanisch, Französisch und Russisch verwenden dieselbe Familie. Sprachschlüssel, Platzhalter und die tatsächlichen Zeichenbestände werden geprüft. Sprachwechsel sind über die Einstellungen erreichbar. Vollständige muttersprachliche Redaktion und Fensterprüfung mit den längsten Texten bleiben eigene Qualitätsaufgaben. Siehe [Sprachen](../Localization/README.md) und [Schriften](../Typography/README.md).

Ein eigener 60-Sekunden-Musikloop und vier kurze Feedbackeffekte sind integriert. Die kommerzielle Stable-Audio-Berechtigung wurde vom Nutzer bestätigt und als eigener Beleg archiviert. Gerätemischung, Schleifen-Hörgefühl und persönliche Hörabnahme sind weiterhin offen. Eine technische Audiointegration stellt keine Hörfreigabe dar.

## Lokale Speicherung und Abwesenheit

Der gespeicherte Zustand enthält beide Städte, Lieferfracht, Baufortschritt, Mitarbeiter, Geschichten, Erinnerungen, Inventar, Tageskonten und Kauf-/Belohnungsbelege. JSON-Validierung, Prüfsumme, atomisches Schreiben, Backup und historische Migration sind vorhanden. Fehlende ursprüngliche Pflichtgebäude gelten als beschädigter Spielstand; neue Inhalte werden erst nach Prüfung der alten Struktur ergänzt.

Der aktuelle Inhalt begrenzt Produktionsnachrechnung auf acht Stunden und tägliche Nachträge auf sieben Tage. Offlineberechnung verwendet dieselben fachlichen Regeln, einschließlich verfügbarer Mitarbeiter, Puffergrenzen und VIP-Zeitfenster. Sie darf keine Ware erfinden oder löschen und keine Annahme von Aufträgen ersetzen. Lange Abwesenheit ist ein eigener Start-Performancefall; technische Optimierung wird gegen deterministische Vergleichsstände geprüft. Der aktuelle Arbeitsnachweis ist [Commercial Readiness](../CommercialReadiness/Release/README.md).

## Shop und kommerzielle Grenze

Siebzehn Produkte, Preisvorbereitung, fünf Kategorien und ein gemeinsames Shop-/Produktkarten-Prefab sind vorhanden. Editor und Development zeigen ausdrücklich **TESTSHOP · Keine echten Zahlungen**. Testkäufe sind kostenlos und wiederholbar. Der Release-Adapter ist deaktiviert und zeigt **Bald verfügbar**.

Der VIP-Pass umfasst dreißig rollierende Tage, täglich 50 Grinder mit gespeicherter Nachzahlung, 1,5-fache aktive Produktionsrate und 1,5-fachen Blütenverkauf. Verlängerung addiert Zeit, keine gestapelten Multiplikatoren. Für das VIP-Angebot existiert derzeit keine automatische reale Abbuchung. Gekauftes Geld zählt nicht als Warenverkauf für Quests.

`IShopPurchaseGateway` trennt Zahlungsautorisierung und Storepreise von Belohnungsvergabe. Stabile Transaktions-IDs und der dauerhafte Erfüllungsnachweis verhindern doppelte Belohnungen. Reales Billing-SDK, Storeprodukte, verifizierte Kaufbelege, ausstehende Käufe und Wiederherstellungsfälle sind noch offen. Siehe [Shop](../ShopRevision/README.md).

## Verbindliche Abnahmegrenzen

Die lokale Architektur und der umfangreiche Testbestand ergeben noch keinen Veröffentlichungskandidaten. Der Release-Preflight verlangt für den aktuellen Plattform-/Buildstand unabhängige akzeptierte Belege für `ui_overflow`, `target_profile`, `human_playthrough`, `audio_listening` und `audio_license`. Benutzerfreigabe der Gebäudekunst ist davon getrennt und liegt vor.

Der Fingerprint bindet nun Projekteinstellungen, Plattform, Buildoptionen, Szenen, Buildprofile, Ressourcen, Importmetadaten und Abhängigkeiten. Alte Belege werden durch diese Änderung nicht übernommen. Diagnosemessungen tragen keine Gerätefreigabe. Die offenen Aufgaben sind Gerätebedienung und -leistung, menschlicher Einstieg/Langzeitfortschritt, Audiohören, Sprachredaktion und echte Kaufprozesse. Storekonfiguration, Signierung, rechtliche Produktangaben, Update-/Wiederherstellungsprüfung und finaler reproduzierbarer Release-Build müssen vor der Veröffentlichung abgeschlossen werden.
