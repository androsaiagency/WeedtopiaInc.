# Gameplay-Architektur

`Weedtopia.Domain` kennt keine Unity-Typen. `Amount`, Definitionsdaten, serialisierbare Zustände, Simulation, WorkerService und UpgradeService bilden den fachlichen Kern. Die Simulation akzeptiert Sekunden, sammelt Restzeit und verarbeitet feste Sekundentakte. `GameRoot` verbindet Uhr, Persistenz, Session, UI und Welt über serialisierte Referenzen. `GameUi` übersetzt Eingaben in Services, enthält keine Produktions- oder Speicherformeln.

Gebäudeinstanzen verwenden eine feste Instanz-ID, Familien-ID und CityId. Familien definieren Kurven und kompatible Workerrollen. Die Darstellung wird über den bestehenden `IBuildingVisualProvider` aufgelöst. Domainzustand besitzt keine GameObject-, Sprite- oder Prefab-Referenzen.

Transport ist ein gespeicherter Zustand mit Route, Worker-ID, Fracht und Restzeit. Waren werden beim Laden aus dem Ursprung entnommen und erst beim Entladen ins Ziel geschrieben. Volle Ziele löschen keine Fracht. Die Szene besitzt einen Pool von 48 Fahrzeugvisuals; Warenmengen sind niemals GameObjects. Die Simulation verarbeitet Bauabschlüsse, Entladen, downstream Produktion/Verkauf, Förderwege, Abfahrt und manuelle Restzeit in fester Reihenfolge.

Authorierte Daten liegen unter `Assets/Weedtopia/Game`. Der explizite Installer darf vorhandene Referenzen nur ergänzen, nicht ersetzen. Szenen, Canvas, Kamera, Zonen, Bauplätze und feste Interaktionsflächen werden als Assets/Hierarchie gespeichert. Das 2D-Produktionsgate bleibt unabhängig von dieser fachlichen Integration.

Alle 251 vorhandenen Produktions-Wrapper und der bestehende Katalog bleiben bis zur visuellen Freigabe erhalten. Entwicklungsvisuals verwenden einen getrennten Provider; der echte Katalog wird über dieselbe Schnittstelle angebunden. Direkte Katalogreferenzen und deren Speicherkosten müssen nach M9 gesondert gemessen werden.

## Assembly-Grenzen

| Assembly | Verantwortung |
| --- | --- |
| Weedtopia.Domain | Zahlen, Definitionen, Zustand, Simulation, Worker, Ausbau; noEngineReferences |
| Weedtopia.Application | Session, IClock/ISaveStore, Intro, Quest, Tag, Offline, Ranglistenvertrag; noEngineReferences |
| Weedtopia.Persistence | Atomischer JSON-Save mit SHA256, Backup/Migration, lokale Rangliste |
| Weedtopia.Content | Explizites ScriptableObject mit JSON-TextAsset |
| Weedtopia.Unity | Composition Root, Kamera/Input, Views, uGUI, Audiohook |
| Weedtopia.Editor.GameSetup | Explizite Installation, Reparatur, Assetvalidierung und Batch-Aufnahmen |

Die bestehenden Buildings.Presentation-/ArtPipeline-Assemblies bleiben eigenständig. Domain und Application kennen weder Unity-Objektreferenzen noch Dateipfade. UI, Speicherung und Visualprovider können über ihre jeweiligen Grenzen ersetzt werden. Die spätere Datenbank benötigt einen ISaveStore-Adapter; die spätere Onlinerangliste einen ILeaderboardService mit eigener Authentifizierung/Servervalidierung. Der aktuelle DisabledOnlineLeaderboard führt keine Netzwerkanfrage aus.

Content wird beim Sessionaufbau auf IDs, Rollen, Routen, Kurven, Attributpools, Questziele und Texte validiert. `BurningHills.json` ist die authorierte Inhaltsquelle. Questbedingungen und Belohnungen sind Daten. Weitere Städte brauchen echte Definitionen, Wege und Quests, bevor sie auswählbar werden.

Der Installer läuft nur über `Weedtopia > Setup > Create or Repair Game Scaffold`, erzeugt fehlende eigene Assets und ergänzt fehlende Pflichtreferenzen. Schmutzige Szenen werden abgewiesen. Eine bestehende Szene ohne GameRoot wird geschützt und zur Prüfung gemeldet. `VisualEvidence` wird nur mit ausdrücklich gesetztem eigenen Batch-Sessionflag aktiv; normale Starts/Domain Reloads verändern keine Szene. Batchstarter prüfen Unity-Prozesse und Projektlock vor jedem Lauf.

`BuildingView` wechselt ein Visual nur bei einem tatsächlichen Prefab-/Tierwechsel. UI-Anzeigen werden zehnmal pro Sekunde aktualisiert, Simulation und Kamerasteuerung laufen unabhängig davon. Vor der späteren 251er-Spriteintegration ist ein bedarfsgesteuerter Ladeweg erforderlich. Vier isolierte Texturen belegen derzeit zusammen rund 32 MiB importierten Texturspeicher; dies ersetzt keinen Zielgeräte-Profilerlauf.
