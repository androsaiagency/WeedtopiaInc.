# Great Kushville

Great Kushville ist die elfte Stadt (`great_kushville`, Präfix `gk_`, Inhaltsversion 12). Die alte Kanal- und Gartenmetropole hat bei ihrem schnellen Wachstum Wasserrechte, Warenwege und Nachbarschaften getrennt organisiert. Acht dauerhaft bleibende Bewohner verbinden die vier Viertel Kushgärten, Kanalmarkt, Terrassenhöfe und Gründerbogen wieder miteinander. Die Integration enthält 56 Bauplätze, 35 Routen, 18 Hauptaufgaben, 32 persönliche Kapitel, 24 wiederkehrende Aufgaben und drei Gemeinschaftsprojekte. Texte sind für Deutsch, Englisch, Spanisch, Französisch und Russisch verfasst.

## Freischaltung und Wohnkapazität

Die Einladung wird bei **5.000.000 gleichzeitig vorhandenen Einwohnern in den zehn bisherigen, bereits per Stadturkunde angenommenen Städten** freigeschaltet. Einwohner einer noch nicht angenommenen Stadt und die Einwohner von Great Kushville selbst tragen nicht zu dieser Voraussetzung bei. Der erreichte gemeinsame Höchststand wird gespeichert; ein späterer Bevölkerungsrückgang entfernt die Einladung nicht. Die Annahme der Stadturkunde vergibt einmalig 12.500 Geld und sieben Teamkarten. Erst danach ist die Reise verfügbar.

Damit diese Schwelle legal erreichbar ist, ändert sich ausschließlich der letzte Wohnhausbonus: `residentTierBonuses[24]` von **135 auf 475**. Dies wirkt erst bei **Stufe 5000**; die Grundkapazität des Hauses steigt dort von 260 auf **600 Einwohner**, alle früheren Stufen behalten ihre bisherigen Werte. Ein maximaler Bewohner-Betreuer mit Stufe 100, Rang 10 und dem Attribut Einwohner erreicht den Faktor 51, also `600 × 51 = 30.600` Einwohner je Haus. Pro Stadt bleiben höchstens **17 Wohnhäuser und mindestens ein Park** auf den vorhandenen 18 Wohnbauplätzen erhalten. Das theoretische Maximum der zehn bisherigen Städte beträgt daher `10 × 17 × 30.600 = 5.202.000`. Dieser rechnerische Nachweis beschreibt die Erreichbarkeit; die tatsächliche Freischaltung prüft die laufende Simulation und den gespeicherten gemeinsamen Meilenstein.

Geld, Sammlung und Team bleiben gemeinsam. Gebäude, Routen, Hauptaufgaben, Bewohnergeschichten, Erinnerungen und Aktivitätszähler gehören weiterhin ihrer jeweiligen Stadt. Beim Laden eines gültigen Spielstands der Version 11 ergänzt die Migration Great Kushville; vorhandene Städte, Zuweisungen und Geschichten bleiben erhalten. Die gezielte Wohnkapazitätsänderung betrifft auch deren Häuser auf Stufe 5000.

## Bewohner und sichtbare Orte

| Bewohner | ID | Tätigkeit | Erscheint ab lokalen Einwohnern |
| --- | --- | --- | ---: |
| Selma Wehr | `gk_selma` | Kanalingenieurin | 50 |
| Aurel Blatt | `gk_aurel` | Gärtner | 100 |
| Daria Markt | `gk_daria` | Marktkoordinatorin | 180 |
| Leandro Vale | `gk_leandro` | Bäcker | 250 |
| Kira Stieg | `gk_kira` | Terrassenarchitektin | 350 |
| Benno Bogen | `gk_benno` | Steinmetz und Bewahrer des Gründerbogens | 450 |
| Yuna Klang | `gk_yuna` | Chronistin der Stimmen der Stadt | 600 |
| Oskar Takt | `gk_oskar` | Kurier und Planer der Übergaben | 700 |

Jeder Bewohner besitzt vier persönliche Kapitel, drei wiederkehrende Aufgaben und dauerhaft gespeicherte sichtbare Erinnerungen. Persönliche Kapitel und gemeinschaftliche Aufgaben verlangen neue Aktivität seit ihrer Annahme. Wiederkehrende Aufgaben verwenden eine Pause von 300 Simulationssekunden und die gemeinsame Skalierungslogik.

| Projekt | Voraussetzung | Architektur | Planposition des Ortes |
| --- | --- | --- | --- |
| `gk_joint_water_compact` | 2.000 lokale Einwohner und Kapitel 1 aller acht Bewohner | `CanalAssemblyHall` | `(112, -90)` |
| `gk_joint_shared_exchange` | 8.000 lokale Einwohner und Kapitel 2 aller acht Bewohner | `ExchangeArcade` | `(-63, -167)` |
| `gk_joint_founders_walk` | 22.000 lokale Einwohner und Kapitel 4 aller acht Bewohner | `FoundersPromenade` | `(-173, -42)` |

Alle drei Orte besitzen separate, im Prefab gespeicherte Zustände für geschlossen, gemeinsame Wiederherstellung und abgeschlossen. Der geschlossene und der im Aufbau befindliche Zustand zeigen unterschiedliche Tönungen der eigenen Architektur sowie einen wiederverwendeten Bauzaun; der abgeschlossene Ort bleibt bestehen. Selmas drittes Kapitel (`gk_selma_chapter_3`) entscheidet zwischen `water_lab` und `canal_workshop`. Die Auswahl wird gespeichert und zeigt beim Wasserpakt entweder `CivicWaterLaboratory` oder `CanalKeeperWorkshop`, jeweils mit lokalem Planversatz `(0, -30)`. Zusätzlich existieren entsprechende separate Erinnerungen an Selmas Figur.

## Szene, Prefabs und Quellen

Alle Pfade beziehen sich auf den Projektstamm `D:\Unity Games\Weedtopia Inc\WeedtopiaInc`.

| Bestandteil | Pfad oder Hierarchie |
| --- | --- |
| Spielszene | `Assets/Weedtopia/Game/Scenes/GreatKushville.unity` |
| Gameplay-Einstieg | Der bestehende `GameRoot` der Stadtszene; `sceneCityId = great_kushville` |
| Produktionsassets | `Assets/Weedtopia/Production/GreatKushville/` |
| Bewohner-Welt | `Residents.prefab`, Szenenwurzel `GreatKushvilleResidents` |
| Bewohner-UI | `ResidentStories.prefab` |
| Gemeinschaftliche Orte | `Prefabs/CanalCommons.prefab`, Szenenwurzel `GreatKushvilleCanalCommons` |
| Weitere Ortsgruppen | `gk_kushgaerten`, `gk_kanalmarkt`, `gk_terrassenhoefe`, `gk_gruenderbogen` unter der Gemeinschaftswurzel |
| Kataloge und Navigation | `BuildingVisuals.asset`, `ResidentVisuals.asset`, `DistrictMap.asset` |
| Eigene Architektur | `Prefabs/CanalAssemblyHall.prefab`, `ExchangeArcade.prefab`, `FoundersPromenade.prefab`, `CanalKeeperWorkshop.prefab`, `TerraceGardenCourt.prefab`, `CivicWaterLaboratory.prefab`, `CivicMusicPavilion.prefab`, `CanalDispatchHouse.prefab` |
| Neue Originalgrafiken | `Art/resident_atlas.png`, `Art/architecture_atlas.png`, `Art/town_sign.png` |
| Dokumentierte Grafikaufträge | `Docs/GreatKushville/ArtSource/prompts.json`; dort sind auch verworfene Zwischenstände ausdrücklich gekennzeichnet |
| Inhalt und fünf Sprachkataloge | `Assets/Weedtopia/Game/Content/BurningHills.json`, `Assets/Weedtopia/Game/Localization/{de,en,es,fr,ru}.json` |
| Explizite Inhaltsautorisierung | `Tools/Implementation/author_great_kushville.py` |
| Explizite Unity-Autorisierung | `Assets/Weedtopia/Scripts/Editor/GameSetup/GreatKushvilleAuthoring.cs` und `GreatKushvilleCampaignAuthoring.cs` |
| Stadtsignatur | `Assets/Weedtopia/Scripts/Runtime/Unity/GreatKushvilleStamp.cs` |

Bewohner- und Architekturatlas besitzen jeweils vier Spalten und zwei Reihen. Der Importer legt pro Bewohner eine Körper- und eine Porträtscheibe an. Architektur-Prefabs haben einen unabhängigen Bodenanker und ein `Artwork`-Kind mit `CityLandmarkVisual`. Die bestehende feste isometrische Kamera und die 2D/2.5D-Darstellung werden weiterverwendet. Die 251 Gebäudevarianten übernehmen vorhandene Sprites, Interaktionsanker, Kollisionsflächen und Wrapper-Offsets mit einer eigenen Stadtpalette. Statische Bewohner, Erinnerungen, Bauzäune und Landmarken werden im Editor erstellt und als Prefabs beziehungsweise Szenenobjekte gespeichert.

## Installation und erneute Prüfung

Die Installation ist ausdrücklich und wiederholbar. Sie läuft weder automatisch beim Import noch beim Spielstart. Das gemeinsam referenzierte Kampagnen-Prefab erhält die zusätzliche serialisierte Reisezeile; dadurch steht sie in den vorhandenen Stadtszenen zur Verfügung, ohne deren Landschaft, Gebäude und Spielobjekte neu aufzubauen. Eine bereits vorhandene fremde Stadtidentität am Zielpfad wird nicht überschrieben. Bestehende Zielassets werden weiterverwendet; die Sicherheits- und Strukturprüfungen verhindern einen breiten Neuaufbau. Die Preservation-Prüfung belegt nach dem Lauf, ob zusätzliche Änderungen entstanden sind.

Vor dem ersten Änderungslauf die vorhandene Preservation-Baseline behalten beziehungsweise eine Baseline erfassen. Das Capture-Skript ersetzt eine bestehende Baseline nicht. Für einen Batch-Lauf müssen andere Unity-Instanzen geschlossen sein; der Runner bricht bei einem Unity-Prozess oder einer Projektsperre ab. Im bereits offenen passenden Editor ist nach dem Inhaltsimport der Menüpunkt `Weedtopia/Cities/Install or Repair Great Kushville` verfügbar. Ungespeicherte Szenenänderungen müssen vorher gesichert werden.

```powershell
Set-Location 'D:\Unity Games\Weedtopia Inc\WeedtopiaInc'
python -X utf8 .\Tools\Implementation\great_kushville_preservation.py capture
python -X utf8 .\Tools\Implementation\author_great_kushville.py
.\Tools\Implementation\Run-GreatKushville.ps1 -Mode Install -Label Integration
```

Der Runner verwendet standardmäßig Unity `6000.3.9f1` unter `D:\Unity Games\6000.3.9f1\Editor\Unity.exe`; `-UnityEditor` erlaubt einen ausdrücklich angegebenen passenden Editorpfad. Anschließend separat die Unity-Testgruppen `Weedtopia.Tests.GreatKushvilleCampaignTests` und `Weedtopia.Tests.Assets.GreatKushvilleAssetTests` in EditMode sowie `Weedtopia.Tests.PlayMode.GreatKushvillePlayModeTests` in PlayMode ausführen. Bestehende betroffene Regressionstests gehören zum selben Prüflauf.

```powershell
.\Tools\Implementation\Run-GreatKushville.ps1 -Mode Capture -Label GameView
python -X utf8 .\Tools\Implementation\great_kushville_preservation.py verify
python -X utf8 .\Tools\Implementation\great_kushville_prefab_preservation.py
```

Der Capture-Lauf verwendet beschleunigten diagnostischen Stadtfortschritt, echte Szenenwechsel und die produktiven Save-/Story-Pfade. Er erwartet 84 aktuelle Game-View-Aufnahmen, darunter 60 Aufnahmen für drei UI-Szenarien in fünf Sprachen und vier Auflösungen. Das ersetzt keinen menschlichen Durchlauf der langfristigen Progression. Ein wiederholter Installationslauf muss die bereits gespeicherten elf Stadtszenen und die eigenen Produktionsassets unverändert lassen; dies prüft der Assettest separat.

## Nachweise und offene Abnahmegrenzen

Diese Dokumentation setzt **keinen Prüfschritt auf bestanden**. Der aktuelle Stand ergibt sich aus frischen Receipts, vollständigen Logs und Testresultaten des jeweiligen Laufs. Ein vorhandenes altes Receipt, ein erfolgreicher Prozessabschluss oder ein Screenshot allein schließen keinen anderen Prüfschritt.

| Prüfschritt | Erforderlicher aktueller Nachweis | Quelle |
| --- | --- | --- |
| Inhalt und Lokalisierung | Vollständige IDs, 32 Kapitel, 24 Varianten, drei Projekte, fünf Sprachkataloge und konsistente Platzhalter | `content_receipt.json` |
| Unity-Import und Autorisierung | Fehlerfreier Import/Kompilierung und frischer erfolgreicher Installationsbericht | `authoring_receipt.json`, `Validation/*_Install*.json` und zugehörige Logs |
| Domain-/Save-/Migrationstests | Vollständige erfolgreiche EditMode-Ergebnisse einschließlich tatsächlicher Bevölkerung, Urkunde, Wohnkapazität, einmaliger Belohnung und Save-Rollback | Aktuelle Unity-Test-XMLs und Logs |
| Assets und Wiederholbarkeit | Alle elf Reisezeilen, Stadtobjekte, Originalgrafiken, Wrapper-Anker, Orte und Byte-Stabilität bei erneuter Installation | Aktuelle Assettest-Ergebnisse |
| Runtime und Offline-Fortsetzung | PlayMode-Ergebnisse für Reisen, elf Städte, beide Entscheidungen und Fortsetzung ohne verlorene oder doppelte Zustände | Aktuelle PlayMode-Test-XMLs und Logs |
| Game View und automatisches UI-Layout | Frisches Receipt mit 84 Aufnahmen, 60 Matrixaufnahmen, beide Entscheidungen, elf Städten und erfolgreicher Überlaufprüfung | `gameview_receipt.json`, `ui_overflow.json`, `Screenshots/`, `Validation/*_Capture*.json` |
| Erhalt bestehender Arbeit | Keine entfernten Altobjekte oder unerwarteten Altänderungen; nur erlaubte Inhalts-, Reisezeilen-, Migrations- und finale Wohnkapazitätsänderungen | `Preservation/baseline.json`, `Preservation/Before/`, `Preservation/result.json`, `Preservation/prefab_result.json` |
| Menschliche visuelle Abnahme | Ausdrückliche Freigabe der neuen Stadt und ihrer Grafiken durch den Nutzer | Bleibt bis zur ausdrücklichen Abnahme offen |
| Physisches Gerät, Touch, Geräteaudio und Player-Build | Separater tatsächlicher Test auf der jeweiligen Zielplattform | Wird durch Editor-Tests und Capture nicht bestätigt |

Der enge Prefab-Check verlangt byte-identische alte Stadtszenen samt Bootstrap und unveränderte alte Prefab-Overrides. Im Kampagnen-Prefab erlaubt er ausschließlich eine angehängte Reisezeile und deren zwei neue UI-Wurzeln samt verbundenen Komponenten; zusätzliche Änderungen an alten UI-Feldern, Transformationen oder Grafikreferenzen scheitern.

`content_receipt.json` dokumentiert die additive Inhaltsautorisierung; die gezielte letzte Wohnhausbonusänderung und die Gesamterhaltung werden zusätzlich durch Domain-Tests und `Preservation/result.json` belegt. Technische Integration, visuelle Zustimmung und Plattformfreigabe bleiben getrennte Aussagen.
