# Great Kushville – Integrationsbericht

Quelle: `Docs/GreatKushville/validation_summary.json`, Stand `2026-10-13T00:34:05.454540+00:00`. Dieser Bericht liest ausschließlich dieses abschließende Summary. Status: **`passed_local_integration`**.

Great Kushville ist als elfte Stadt integriert. Die alte Kanal- und Gartenmetropole verlor bei ihrem schnellen Wachstum gemeinsame Wasserrechte, Warenwege und Begegnungsorte. Die vier Viertel Kushgärten, Kanalmarkt, Terrassenhöfe und Gründerbogen werden durch die dauerhaften Bewohnergeschichten wieder verbunden.

Die Einladung erscheint bei **5.000.000 gleichzeitig vorhandenen Einwohnern in den zehn bisherigen, per Stadturkunde angenommenen Städten**. Ein erreichter gemeinsamer Höchststand bleibt gespeichert. Die Stadturkunde vergibt einmalig 12.500 Geld und sieben Teamkarten; anschließend ist die Reise möglich. Geld, Sammlung und Team bleiben gemeinsam, Gebäude, Routen, Aufgaben, Aktivität und Bewohnergeschichten bleiben stadtspezifisch.

## Geschichte und Bewohner

Die Stadt besitzt 56 Bauplätze, 35 Routen, 18 Hauptaufgaben, acht dauerhaft bleibende Bewohner, 32 persönliche Kapitel, 24 wiederkehrende Aufgaben und drei Gemeinschaftsprojekte. Nach abgeschlossenen Kapiteln bleiben Personen, Entscheidungen und sichtbare Erinnerungen in der Stadt erhalten.

| Bewohner | ID | Geschichte |
| --- | --- | --- |
| Selma Wehr | `gk_selma` | Kanalingenieurin; öffnet Wasserwissen und gemeinsame Verantwortung |
| Aurel Blatt | `gk_aurel` | Gärtner; verbindet sein Gartenbuch mit dem gemeinsamen Wasserplan |
| Daria Markt | `gk_daria` | Marktkoordinatorin; schafft faire Lieferfenster und offene Absprachen |
| Leandro Vale | `gk_leandro` | Bäcker; verbindet alte Handwerksräume mit neuen Rezepten |
| Kira Stieg | `gk_kira` | Terrassenarchitektin; hält gemeinsame Zugänge und Hofmitten offen |
| Benno Bogen | `gk_benno` | Steinmetz; bewahrt den Gründerbogen und öffnet ihn für neue Geschichten |
| Yuna Klang | `gk_yuna` | Chronistin; bewahrt Stimmen, Erinnerungen und gemeinsame Entscheidungen |
| Oskar Takt | `gk_oskar` | Kurierplaner; organisiert zuverlässige Übergaben und geteilte Dienste |

Der Wasserpakt (`gk_joint_water_compact`) folgt ab 2.000 lokalen Einwohnern den ersten Kapiteln aller acht Bewohner; der Umschlagplatz (`gk_joint_shared_exchange`) folgt ab 8.000 Einwohnern deren zweiten Kapiteln; der Gründerweg (`gk_joint_founders_walk`) folgt ab 22.000 Einwohnern deren vierten Kapiteln. Alle Orte zeigen separate gespeicherte Zustände für geschlossen, Wiederherstellung und abgeschlossen. Selmas drittes Kapitel entscheidet dauerhaft zwischen dem offenen Wasserlabor (`water_lab`) und der Kanalwerkstatt (`canal_workshop`).

Die neue Szene liegt unter `Assets/Weedtopia/Game/Scenes/GreatKushville.unity`; Stadtassets unter `Assets/Weedtopia/Production/GreatKushville/`. Bewohner und Orte stehen in `Residents.prefab`, `ResidentStories.prefab` und `Prefabs/CanalCommons.prefab`. Acht eigene Architektur-Prefabs und originale transparente Bewohner-, Architektur- und Schildgrafiken ergänzen die feste isometrische 2D/2.5D-Darstellung. Die Grafikaufträge sind in `Docs/GreatKushville/ArtSource/prompts.json` dokumentiert.

## Gezielte Wohnkapazitätsänderung

Der einzige produktive Unterschied an bisherigen Gebäudedefinitionen betrifft den letzten Wohnhausbonus: **135 → 475**, wirksam ausschließlich auf **Stufe 5000**. Die Grundkapazität steigt dort von 260 auf 600; frühere Stufen bleiben gleich. Ein maximaler Bewohner-Betreuer erreicht den Faktor 51 und damit **30.600 Einwohner je Haus**. Mit **17 Häusern** und **1 erhaltenem Park je Stadt** erreichen die **10 bisherigen Städte** theoretisch **5.202.000 Einwohner**. Der im Summary geprüfte tatsächliche Simulations- und Save-Pfad bleibt maßgeblich für die Freischaltung.

## Tatsächlich abgeschlossene Testläufe

Das Summary bestätigt **200 unterschiedliche bestandene NUnit-Fälle** bei **200 unterschiedlichen Fällen**. Wiederholungen werden in dieser Zahl nicht doppelt gezählt. Die vollständige Domain-Regressionssuite ist nicht als abgeschlossen bestätigt. Die unten aufgeführten tatsächlich abgeschlossenen Läufe bestimmen den Umfang der Testaussage.

| Lauf | Modus | Gesamt | Bestanden | Fehlgeschlagen | Übersprungen | Unentschieden |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `20261013_014325_EditMode_GreatKushvilleFinalDomainAndRegression` | EditMode | 163 | 163 | 0 | 0 | 0 |
| `20261013_020123_EditMode_GreatKushvilleFinalAssetsAndPreservation` | EditMode | 11 | 11 | 0 | 0 | 0 |
| `20261013_020700_PlayMode_GreatKushvilleFinalRuntimeAndTravel` | PlayMode | 26 | 26 | 0 | 0 | 0 |

Die Test-XMLs und ihre Hashes stehen im Summary. Die lokalen Tests umfassen die Bevölkerungsgrenze, einmalige Stadturkundenbelohnung, elf Städte, Save-/Migrations-/Offline-Verhalten, Bewohnerentscheidungen, die statischen Assets und die wiederholbare Installation. Ihr exakter Umfang ergibt sich aus den aufgeführten Läufen; ein nicht abgeschlossenes zusätzliches Gesamtpaket wird nicht als bestanden dargestellt.

## Game View und Erhalt bestehender Arbeit

Der abgeschlossene Capture-Lauf `20261013_022419_Capture_FramedCanalMetropolis` hat Status `passed` und Exitcode `0`. Das Summary enthält **84 tatsächliche Aufnahmen** und **60 Matrixaufnahmen** für `de, en, es, fr, ru` in den Auflösungen 1080 × 1920, 1366 × 768, 1920 × 1080, 2560 × 1080. Die Matrix umfasst Anfangsaufgabe, letzte Reisezeile und Selmas Entscheidung. Die weiteren Aufnahmen belegen Ankunft, Ortsentwicklung, beide Entscheidungen, Offline-Fortsetzung und Reisen durch die Städte.

| Runtime-Nachweis | Im Summary bestätigt |
| --- | --- |
| Gespeicherte Geschichten | Im Summary bestätigt |
| Wohnhausanzeige stimmt mit Simulation überein | Im Summary bestätigt |
| Rückreise zur bisherigen Stadt | Im Summary bestätigt |
| Alle elf Städte besucht | Im Summary bestätigt |
| Elf-Städte-Offline-Fortsetzung | Im Summary bestätigt |
| Beide Selma-Entscheidungen | Im Summary bestätigt |
| Unabhängige Bodenanker der Architektur | Im Summary bestätigt |

Die strikte Preservation-Prüfung bestätigt **10 byte-identische bisherige Stadtszenen** sowie Bootstrap und unveränderte alte Prefab-Overrides. Das gemeinsame Kampagnen-Prefab erweitert seine Reiseliste von **10 auf 11 Zeilen**. Nur die zusätzliche Referenz und zwei UI-Kinder samt **14 neuen verbundenen Dokumenten** wurden ergänzt. Alte Transformationen, Grafikreferenzen und Overrides bleiben unverändert. Insgesamt erfasst der Erhaltungsbericht **9.155 geschützte Dateien**.

## Prüfung wiederholen

Die genaue Reihenfolge und Pfade stehen in `Docs/GreatKushville/README.md`. Die Installation läuft ausdrücklich über `Weedtopia/Cities/Install or Repair Great Kushville` oder den Batch-Runner. Der Batch-Runner startet nur ohne vorhandene Unity-Instanz oder Projektsperre. Eine vorhandene Preservation-Baseline bleibt erhalten.

```powershell
Set-Location 'D:\Unity Games\Weedtopia Inc\WeedtopiaInc'
python -X utf8 .\Tools\Implementation\author_great_kushville.py
.\Tools\Implementation\Run-GreatKushville.ps1 -Mode Install -Label Integration
# EditMode- und PlayMode-Testgruppen separat ausführen und Ergebnisdateien behalten.
.\Tools\Implementation\Run-GreatKushville.ps1 -Mode Capture -Label GameView
python -X utf8 .\Tools\Implementation\great_kushville_preservation.py verify
python -X utf8 .\Tools\Implementation\great_kushville_prefab_preservation.py
# Erst nach close_great_kushville.py mit den tatsächlichen Testreceipts:
python -X utf8 .\Tools\Implementation\write_great_kushville_report.py
```

## Grenzen der Abnahme

| Bereich | Tatsächlicher Summary-Status |
| --- | --- |
| Lokale technische Integration | Im Summary bestätigt |
| Vollständige Domain-Regressionssuite | Offen / nicht bestätigt |
| Vollständiger menschlicher Spieldurchlauf | Offen / nicht bestätigt |
| Physisches Gerät / Touch / Geräteaudio | Offen / nicht bestätigt |
| Ausdrückliche visuelle Nutzerfreigabe | Offen / nicht bestätigt |
| Muttersprachliche Prüfung | Offen / nicht bestätigt |
| Release-Abnahme | Offen / nicht bestätigt |

Beschleunigte diagnostische Wachstumsstände wurden verwendet. Die Aufnahmen zeigen tatsächlich gerenderte Spielzustände und prüfen produktive Runtime-/Save-Pfade; sie bestätigen keinen vollständigen menschlichen Langzeitdurchlauf. Editor- und automatisierte Bildprüfungen erteilen keine Nutzerfreigabe oder Geräte-/Release-Abnahme.
