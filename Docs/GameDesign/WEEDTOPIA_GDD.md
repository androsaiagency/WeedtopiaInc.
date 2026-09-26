# Weedtopia Inc – Burning Hills

Stand: 21. September 2026. Implementierte lokale Spielversion mit Entwicklungsvisuals. Maßgebliche Vorgabe bleibt `Docs/Codex-Prompts/IMPLEMENT_WEEDTOPIA_BURNING_HILLS.md`; die Freigabe der 251 produktiven 2D-Gebäudeslots ist ausstehend.

## Einstieg

Burning Hills wird gemeinsam wieder aufgebaut. Die feste isometrische Perspektive zeigt Farm, Stadt, Siedlung und Industrie. Rocko ist der Bürgermeister und begleitet den Einstieg. Deutsche Textschlüssel tragen Intro, Aufgaben, Fehler und Bedienelemente. Ein Name hat 2–24 Zeichen. Startausstattung sind genau 1.000 Geld und 50 Grinder. Intro überspringen und erneut ansehen verändern diese Ausstattung nicht. Die aktuellen NPC-Körper sind wiederverwendete Entwicklungs-Prefabs; finale Charakterillustrationen, Animation und Sprachaufnahmen sind nicht produziert.

## Spielablauf

- Farm: zwei Anbauplätze, eine Veredelung und ein Lager. Mila erklärt den Anbau, Jaro die Verarbeitung, Nia die Logistik. Ernte wächst im begrenzten Anbaupuffer. Manuelle Ernte gibt den Transport für 30 Sekunden frei; der Manager übernimmt dauerhaft. Andere Tätigkeiten laufen zeitweise manuell oder dauerhaft mit Manager. Der Fahrer bringt tatsächlich entnommene Fracht in das Stadt-Lager.
- Stadt: ein Lager verteilt an zwei Coffee-Shops und später die Butterfabrik. Verkauf erhöht Geld und Statistiken. Das Rathaus bietet Stadtstatistik, lokale Bestenliste, Worker-Spin und Tagesabholung. Für die erste Lieferung nimmt das Stadt-Lager schon im Ruinenzustand Fracht an. Die Freischaltung hängt damit nicht von einem noch gesperrten Ausbau ab.
- Siedlung: vier freie Plätze werden dauerhaft zu Wohnhaus oder Park. Der letzte Platz bleibt für den noch fehlenden Typ reserviert. Häuser geben Einwohner und täglich Karten; Parks erzeugen erreichbare VIP-Aufträge. Hausmeister und Parkmanager beeinflussen passende Werte. Das Tutorial gewährt beide Spezialisten garantiert und verlangt deren Zuweisung.
- Industrie: Butterfabrik, Supermarkt und zwei Bäckereien. Zwei veredelte Blüten ergeben eine Butter, eine Butter ergibt eine Backware. Stadtfahrer bedienen verfügbare Ziele zyklisch. Bäckereien verkaufen Backwaren, der Supermarkt verdient anhand der Einwohner und liefert täglich Grinder.
- Freeplay: Nach 16 abgeholten Hauptquests bleiben Ausbau, Karten, Worker, VIP, Produktion und Statistik aktiv. Zweite Anbau-/Verkaufsgebäude sind optionale Erweiterungen. Ihre frühzeitige Errichtung erzeugt keine zusätzliche Managerpflicht für den Tutorialabschluss.

Weitere Städte haben stabile City-IDs und getrennte Zustände/Statistiken. Es gibt keine anwählbare leere Folgestadt und keinen behaupteten Onlinebetrieb.

## Welt und Bedienung

`Assets/Weedtopia/Game/Scenes/Bootstrap.unity` lädt `BurningHills.unity`. Die Spielszene enthält 16 Gebäudeplätze, vier Zonen, Förderwege/Straßen, NPC-Instanzen, Kamera, Canvas, EventSystem und einen Pool mit 48 Lieferfahrzeugen. Statische Geometrie/UI entstehen durch den expliziten Editor-Installer und liegen serialisiert vor. Laufzeitinstanzen sind auf wechselnde Gebäudevisuals und variable Listenzeilen beschränkt.

Linksklick wählt einen Bauplatz. WASD/Pfeile bewegen die Kamera, rechte/mittlere Maustaste ziehen sie, Mausrad oder Plus/Minus zoomen. Zonen-Schaltflächen und „Zum Questziel“ fokussieren Orte. UI verhindert World-Clicks. Touch-Pan/Pinch sind implementiert, aber noch nicht auf einem echten Touchgerät abgenommen.

Das Detailpanel zeigt Level/Tier, vier getrennte Ressourcenpuffer, Kapazität je Puffer, Durchsatz, Bauzeit und Fahrerplätze. Ausbau, Ernte, Handbetrieb, Worker und Dialog sind dort erreichbar. Wohnhaus-/Parkwahl funktioniert auf freien Siedlungsplätzen. Die Workeransicht bietet Rollenfilter, Seltenheit/Attribute, Level/Rang, Zielgebäude, Slotwahl, Zuweisung und Freistellung. Grinder werden erst nach Bestätigung des konkreten Angebots ausgegeben.

Pause hält die Simulation an. Einstellungen speichern Lautstärke und UI-Skalierung. Gespeichert wird nach erfolgreichen Befehlen, alle 30 laufenden Sekunden und beim Pausieren/Beenden. Weiterladen zeigt Offline-/Recoveryergebnisse.

## Darstellung und Abnahmegrenzen

Entwicklungs-Prefabs, Auswahlrahmen, farbiger Produktionsstatus, bewegte Transportwagen und kurze eigene synthetische Bestätigungs-/Fehlertöne sind integriert. Finale Musik, Sprache und Geräte-Audio sind offen.

Der produktive Gebäudekatalog ist angebunden, `productionVisualsApproved` bleibt false. Rathaus und Coffee-Shop T01/T12/T24 besitzen eine isolierte Review-Szene mit Bodenpivot, Kameraausrichtung, Tiefensortierung und Bodencollider. Game-View-Aufnahmen dienen der ausdrücklichen Nutzerfreigabe. Die bestehende 3D-Rathauszuweisung und sämtliche ursprünglichen Art-Quellen bleiben erhalten.

Der deterministische Fortschrittssimulator benutzt öffentliche Spielbefehle ohne Geld-, Quest- oder Freischaltungsinjektion. Seine Sekunden sind Simulationszeit bei schneller automatischer Bedienung, kein menschlicher Spielzeitwert. Langzeitbalancing, manueller vollständiger Spielpfad, Touch, Audio und Profiling bleiben gesonderte Abnahmen. Aktuelle Zähler/Rohdateien: `Docs/Implementation/VALIDATION_REPORT.md`.
