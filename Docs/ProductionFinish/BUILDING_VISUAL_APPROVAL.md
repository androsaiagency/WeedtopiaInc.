<!-- LAYOUT_REVISION_CURRENT -->
# Aktueller Stand nach Benutzerfreigabe

Die vier Gebäudeprototypen und die lokale Freistellung sind ausdrücklich freigegeben; 251 produktive Sprites und die Skizzen-Anordnung wurden integriert. Maßgeblich ist [Docs/LayoutRevision/REPORT.md](../LayoutRevision/REPORT.md).

Die folgenden Abschnitte dokumentieren den historischen Stand vor dieser Integration. Ihre damaligen offenen Gebäude-Gates und Testzahlen gelten nicht als aktueller Status.

---

# Gebäudeentscheidung – ausdrückliche Freigabe offen

Die Bitte um ein professionelles Spiel ist laut [Auftrag, Abschnitt 4.1](../Codex-Prompts/PRODUCTION_FINISH_NO_PLACEHOLDERS.md) keine Freigabe dieser vier Ansichten. Kein produktiver Wrapper und keine Katalogreferenz wurde umgestellt. Das ursprüngliche `PROGRESS.json` bleibt unverändert: `manualUserApproval: false`, `productionWrappersConverted: 0`.

## Konkrete Vorlage

![Rathaus und Coffee-Shop T01, T12, T24 in derselben Spielkamera](Screenshots/10_sprite_review_zoom26.png)

Die Aufnahmen stammen aus der tatsächlichen Unity Game View mit der festgelegten isometrischen Kamera. Rocko und dieselbe Straßenleuchte dienen als Maßstabsvergleich.

| Ansicht | Zweck |
| --- | --- |
| [Vierer-Vergleich, Zoom 26](Screenshots/10_sprite_review_zoom26.png) | Rathaus links, danach Coffee-Shop T01, T12 und T24; Silhouette, Front und Größenentwicklung direkt vergleichen. |
| [Fernansicht, Zoom 38](Screenshots/11_sprite_review_zoom38.png) | Lesbarkeit und Erkennbarkeit der Silhouetten auf Abstand. |
| [Rathaus nah, Zoom 14](Screenshots/12_townhall_zoom14.png) | Dach, Vorderseite, Vegetation, Freistellung und Bodenkante. |
| [Coffee-Shop T24 nah, Zoom 14](Screenshots/13_coffee_final_zoom14.png) | Finale Stufe im Detail; T12 am Bildrand als Vergleich. |

Die Nahansichten zentrieren jeweils ein Gebäude. Angeschnittene Nachbarobjekte am Rand sind kein Beschnitt des fokussierten Sprites. Die [Vergleichsseite](comparison.html) enthält zusätzlich Baseline und neue Spielansichten.

## Entscheidung

**Sind Rathaus sowie Coffee-Shop T01, T12 und T24 in dieser 2D-/2.5D-Richtung, Frontausrichtung, Bodenposition und Größenentwicklung als Grundlage für die 251 Einzelintegrationen freigegeben?** Alternativ bitte die konkrete Ansicht und gewünschte Korrektur benennen.

Eine Freigabe erlaubt die nächste Integrationsphase. Sie nimmt nicht automatisch alle 251 Freistellungen, Schatten, Speicherbudgets oder Tierübergänge ab. Jeder Slot benötigt weiterhin sein eigenes zugehöriges Quellbild, Aufbereitung, Zuordnung und QA. Die aktiven Entwicklungsgebäude im vollständigen Spiel bleiben bis zu dieser Integration ein Release-Blocker.

## Erhaltung und Szene

`Assets/Weedtopia/Production/Review/BuildingApproval.unity` ist vom Produktbuild ausgeschlossen. Die Szene verwendet die vier vorhandenen Prototyp-Prefabs auf dem neuen Terrain. Die akzeptierten Quellen, ursprünglichen Prototypen, produktiven Wrapper, Anker, Offsets und Katalogzuweisungen blieben unverändert.
