# Gameplay-Architektur

Stand: 5. Oktober 2026. Implementierte Zwei-Stadt-Version mit produktiver 2D/2.5D-Darstellung, Bewohnergeschichten, Inventar, Glücksrad, fünf Sprachen, eigener Schrift und Testshop. Inhaltsquelle ist `Assets/Weedtopia/Game/Content/BurningHills.json` mit Content-Version 3. Dieses Dokument beschreibt die bestehenden Grenzen; einzelne historische Prüfläufe bleiben in ihren Originalberichten erhalten.

## Schichten und austauschbare Grenzen

| Assembly | Verantwortung |
| --- | --- |
| `Weedtopia.Domain` | Zahlen, Definitionen, serialisierbarer Zustand, deterministische Simulation, Worker, Ausbau, Stories-/Shop-/Inventardaten; `noEngineReferences` |
| `Weedtopia.Application` | Session und Transaktionen, `IClock`/`ISaveStore`, Intro, Hauptquests, Tageskonten, Offline, Bewohnergeschichten, Stadtwechsel, Shop, Ranglistenvertrag; `noEngineReferences` |
| `Weedtopia.Persistence` | Atomischer JSON-Save, SHA-256, Backup/Migration, Datenvalidierung, lokale Rangliste |
| `Weedtopia.Content` | Explizites `GameContent`-ScriptableObject, JSON-TextAsset und autorisierter Sprachkatalog |
| `Weedtopia.Unity` | Composition Root, Szenenübergabe, Kamera/Input, Views, uGUI, Audio und Lokalisierungsdarstellung |
| `Weedtopia.Buildings.Presentation` | Bestehender Gebäudekatalog, Wrapper, Visualprovider und bedarfsbezogener Spritecache |
| `Weedtopia.Editor.GameSetup` | Explizite Installation/Reparatur, Assetprüfungen, isolierte Belegläufe, Build- und Releaseprüfung |
| `Weedtopia.Editor.BuildingArtPipeline` | Bestehende Gebäude-Artpipeline mit Manifest, Intake und referenzsicherer Einbindung |

Domain und Application kennen keine `GameObject`-, Sprite-, Prefab- oder UI-Referenzen. Produktionsformeln und Speicherschritte gehören nicht in Fenstercontroller. `GameRoot` verbindet Uhr, Zustandsspeicher, Session, Visualprovider und Oberfläche über serialisierte Referenzen. `GameUi` und die spezialisierten Ansichten übersetzen Eingaben in Anwendungsbefehle.

Eine andere Speicherung verwendet den `ISaveStore`-Vertrag; UI und Weltansichten können über ihre Präsentationsgrenzen ersetzt werden. Eine spätere Onlinerangliste benötigt einen echten `ILeaderboardService`-Adapter mit Authentifizierung und Servervalidierung. `DisabledOnlineLeaderboard` führt derzeit keine Netzwerkanfrage aus. Keine lokale Rangliste ist als Onlinefunktion auszugeben.

## Inhaltsdaten und stabile Identität

Familien-ID, feste Gebäudeinstanz-ID und City-ID sind unabhängig vom sichtbaren Gebäudeprefab. Der produktive Katalog enthält zwölf Definitionsfamilien einschließlich freier Bauplätze, 112 Instanzen, 70 Routen, zwölf Rollen und 32 Hauptquests. Die Simulation arbeitet an Zuständen; Warenmengen sind keine Weltobjekte.

`GameContent.Load` lädt und validiert Definitionen, IDs, Rollen, Routen, Kurven, Attributpools, Hauptquest- und Storybedingungen sowie Texte. Zusätzliche Systeme verwenden explizite Definitionsdaten. Big Rock City ist eine vollständig definierte zweite Stadt; es gibt keinen auswählbaren leeren Platzhalter als Folgestadt.

Lokalisierung ist vom Datenmodell getrennt. `LocalizationService` liefert Schlüsseltexte, Platzhalter und Sprachkultur. `LanguageCatalog.asset` besitzt die fünf Sprachdateien und die Regular-/Bold-Fonts. `GameText` ist die Unity-Präsentationsbrücke. Spracheinstellungen werden gespeichert, Domainzustände behalten stabile Schlüssel/IDs statt übersetzter Identität. Zeichendeckung wird am tatsächlichen Textbestand beider Weedtopia-Grove-Schnitte geprüft.

## Produktion und deterministische Zeit

Die Simulation verarbeitet fachliche Sekundentakte in fester Reihenfolge: Bauabschlüsse, Entladen, nachgelagerte Produktion/Verkauf, Förderwege, Abfahrt und manuelle Restzeit. Restsekunden werden gesammelt; die Reihenfolge und gespeicherte Fracht müssen bei Optimierungen erhalten bleiben. Preise, Kapazitäten, Bauzeiten, Mitarbeiterwirkungen und VIP-Zeitintervalle bleiben in Domain/Definitionen.

Lieferungen besitzen Route, Mitarbeiter-ID, tatsächliche Fracht und Restzeit. Beim Beladen wird Ware aus dem Ursprung entnommen, beim Entladen in das Ziel geschrieben. Bei vollen Zielen bleibt Fracht erhalten. Die Szene stellt aktive Lieferungen über einen bestehenden Pool dar. Der Pool ist Darstellung, keine Quelle wirtschaftlicher Wahrheit.

Die übernommene Stadtmenge bestimmt, welche Städte arbeiten. Beide aktiven Städte werden in derselben fachlichen Simulation weitergeführt; gesperrte Städte produzieren nicht. Offlinefortschritt verwendet denselben Regelkern mit autorisierten zeitlichen Grenzen. Ein schnellerer Ablauf muss mit identischem Startzustand zu identischem fachlichem Ergebnis führen. Startdauer und wirtschaftliche Vergleichsprüfung sind getrennte Nachweise.

## Session, Speicherung und Fehler

`GameSession` ist die Anwendungs- und Transaktionsgrenze. `ISaveStore` kapselt die persistente Darstellung. Befehlswege führen Regeln aus und schreiben den Zustand, statt dem UI direkte Dateioperationen zu erlauben. Speicherausnahmen werden an der Anwendungs-/Präsentationsgrenze behandelt und dürfen keine mehrfachen Belohnungen oder widersprüchliche angezeigte Befehlsresultate erzeugen.

`LocalSaveStore` schreibt JSON mit Prüfsumme und atomischem Dateiaustausch. Backup und Recovery erhalten den vorherigen validen Stand; historische Migration prüft zuerst die damalige Pflichtstruktur. Für v1/v2 wird erst danach die neue Stadt mit ihren ungebauten Plätzen ergänzt. Der bestehende Save-Dateiname bleibt erhalten. Tests und Aufnahmen verwenden eigene Pfade und eigene Ranglisten.

Der gespeicherte Zeit-Höchststand verhindert wiederholte Gutschriften bei rückwärts laufender lokaler Uhr. Tägliche Nachträge, Offlineproduktion und VIP-Tageszahlungen besitzen ihre jeweiligen Grenzen. Persönliche Spielerdateien gehören nicht zu Diagnose-Fixtures. Die genaue Start- und Speicherhärtung des heutigen Arbeitsstands wird im aktuellen Commercial-Readiness-Bericht geprüft; historische Tests gelten für ihre damaligen Quellhashes.

## Hauptgeschichte, Bewohner und Stadtwechsel

`ProgressionService` wählt Hauptquests und Bedingungen für die jeweilige Stadt. `CityCampaignService` verarbeitet Einwohnermeilenstein, Rockos Angebot, einmalige Übernahmehilfe und Reise. Stadtgebäude, Lieferungen, Tageskonten, Statistik und Geschichten sind getrennt; Geld, Grinder, Karten, Inventar und verfügbarer Mitarbeiterbestand gehören zum gemeinsamen Unternehmen.

`CitySceneHandoff` übergibt Zustand, Uhr, Save- und Ranglistenpfad sowie gegebenenfalls Offline-/Recoveryzusammenfassung an die Zielszene. Reisen speichert vor dem Laden. Fortsetzen öffnet die zuletzt besuchte Stadt. Temporäre Diagnosepfade bleiben damit auch über eine Reise isoliert.

`VipStoryService` verwaltet die sechzehn dauerhaften Bewohner, 64 Kapitel, 48 wiederkehrenden Varianten und acht Gemeinschaftsprojekte. Freischaltungen, Kapitelindex, aktive Aufgaben, Startzähler, Entscheidungen, Belohnungsbelege und Erinnerungen werden gespeichert. Produktions- und Lieferziele messen neue Aktivitäten seit Annahme; aktuelle Gebäudeziele bleiben Zustandsabfragen. Persönliche Kapitel, wiederkehrende Aufgaben und Gemeinschaftsprojekte verwenden denselben maximalen aktiven Auftragsumfang pro Stadt.

Storyoberfläche, anklickbare Personen und Erinnerungselemente sind eigene Präsentationskomponenten mit wiederverwendbaren Prefabs. Sie erzeugen keine zweite Questwirtschaft. Die Hauptgeschichte, Stadtprojekte, Glücksrad und Bewohnergeschichten bleiben eigenständige Use Cases auf demselben Zustand.

## Inventar, Belohnungen und Zahlungsgrenze

`InventoryService`, `LuckyWheelService` und die Belohnungsdarstellung verwenden stable IDs, gespeicherte Verbrauchs-/Ausgabebelege und vorhandene UI-Prefabs. Vorschau, Schließen oder Abbrechen vergibt keine Belohnung. Das Glücksrad und aktive Shopzahlungen sperren relevante doppelte Eingaben. Wirtschaftliche Gutschrift und visuelle Animation sind getrennt.

`IShopPurchaseGateway` kapselt asynchrone Storeauflistung und verifizierte Autorisierung. `TestShopPurchaseGateway` wird ausschließlich im Editor/Development-Profil gewählt. `UnavailableShopPurchaseGateway` bleibt der aktuelle Release-Standard. Geplante Europreise sind keine bestätigten Storepreise. Reales Billing muss verifizierte Produkt-/Transaktions-IDs und lokalisierte Storepreise liefern.

`ShopService` prüft Autorisierung und Kanal, vergibt Waren und persistiert die dauerhafte Erfüllung. Sein unbegrenzter Transaktionsschutz ist unabhängig von der begrenzten sichtbaren Belohnungshistorie. Speicherfehler nehmen eine nicht dauerhaft übernommene Shopgutschrift zurück. Der VIP-Zeitplan gilt für beide übernommene Städte und überschneidende Offlineintervalle. Storeprodukte, realer SDK-Adapter, Kaufbelegprüfung sowie ausstehende und wiederhergestellte Käufe bleiben eigene Integrationsarbeit.

## Autorisierte Welt und bedarfsgesteuerte Darstellung

Statische Szenenobjekte, UI, Bauplätze, Interaktionsflächen, Wege, Bewohner und Landschaft sind serialisiert in der Hierarchie beziehungsweise in Prefabs. Explizite Installer ergänzen fehlende eigene Referenzen, schützen fremde/manual zugewiesene Assets und verweigern ungespeicherte Szenen. Normale Starts, Import und Domain Reload installieren keine Welt.

Alle 251 produktiven Wrapper und ihre akzeptierte 2D/2.5D-Darstellung sind angebunden und freigegeben. `IBuildingVisualProvider` bleibt die Schnittstelle; Burning Hills und Big Rock City verwenden ihre eigenen Kataloge. Der historische Entwicklungsprovider bleibt von der produktiven Auswahl getrennt. `BuildingView` wechselt nur bei tatsächlichem Prefab-/Tierwechsel.

`BuildingSpriteCache` lädt nur angezeigte Ressourcensprites, zählt Besitzer und entlädt nicht mehr benötigte Bilder. Katalog-/Prefabreferenzen laden nicht pauschal alle 251 Gebäudetexturen. Mobile Importkompression und Downloadgröße sind davon getrennte Plattformaufgaben. Originalgrafiken, Ressourcenpfade, GUIDs, Wrapperoffsets, Clickflächen und Gameplay-Anker werden erhalten.

Die Kamera bleibt fest isometrisch, ohne freie räumliche Drehung. Richtungsanimationen steuern bewegte Charaktere/Fahrzeuge über acht Ansichten mit sechs Frames. Oberfläche, Kamera und Simulation laufen über eigene Aktualisierungspfade; große Listen dürfen die Fachsimulation nicht neu aufbauen.

## Releaseidentität und Nachweise

`ProductionVisualPreflight` prüft aktive Build-Szenen und ihre produktiven Referenzen. Er verwendet die aktive Buildprofil-Szenenliste, sofern ein Profil existiert, sonst die globalen Build-Szenen. Er öffnet keine Build-Szene, solange eine vorhandene Szene oder das aktive Buildprofil ungespeichert ist.

`ReleaseFingerprint` bildet Schema 2 aus gestreamten SHA-256-Dateihashes und dem expliziten Buildkontext. Eingänge umfassen Skripte/Assemblies, produktive Assets, Szenenabhängigkeiten, nicht direkt referenzierte Resources/StreamingAssets, Importmetadaten, sämtliche Projekteinstellungen, aktive Profilabhängigkeiten und Package-Manifeste. Der Kontext bindet Unity-Version, Plattform, Buildoptionen, Szenenreihenfolge, Profil, Definitionssymbole, Scriptingbackend, Stripping, Grafik-APIs und relevante Android-/Standalone-Bauparameter. Fehlende Pflichtinputs werden aufgenommen und blockieren die Freigabe.

`release_inputs.json` dokumentiert alle Eingänge; `production_preflight.json` enthält denselben Fingerprint und Kontext. Der Buildguard verwendet die tatsächlich gemeldete Buildplattform und Bauoptionen. Development-Builds sind lokale Kandidaten und öffnen keine Releasefreigabe. Alte Belege mit einem früheren Fingerprint werden nicht automatisch übernommen.

Die fünf unabhängigen Release-Gates sind `ui_overflow`, `target_profile`, `human_playthrough`, `audio_listening` und `audio_license`. Sie verlangen jeweils einen ausdrücklich bestandenen, passenden Beleg. Gebäudekunstfreigabe, SourceCompile, Unity-Import, EditMode, PlayMode, gerenderte Game View, Pointerprüfung, physische Bedienung und Hören behalten ihre jeweils eigene Aussagekraft.

`ProductionProfileEvidence` liefert ausschließlich Editor-Diagnostik. Sie trennt Messfenster pro Ansicht, Spitzen-Allokationen, Median/p95/Maximalframe und Überschreitungen der 30-/60-FPS-Framebudgets. Texturspeicher ist ein finaler Editor-Snapshot, kein GPU-VRAM-Wert. Der Beleg schreibt weiterhin `targetAccepted=false` und `releaseAccepted=false`. Gerät, Build und Spielsituation müssen für eine echte Zielgeräteabnahme separat dokumentiert werden. Hinweise und heutige Nachweise: [Releasearbeit](../CommercialReadiness/Release/README.md).
