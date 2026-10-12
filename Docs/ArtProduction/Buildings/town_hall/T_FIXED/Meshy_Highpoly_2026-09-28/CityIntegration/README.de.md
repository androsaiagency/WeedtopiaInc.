> Aktualisierung 2026-09-29: Die zu dunkle Rathausdarstellung wurde aufgehellt. [Aktuelle Aufnahmen und Materialänderung](../CityLighting/README.de.md). Die folgenden Aufnahmen dokumentieren die erste Integration.

# Freigegebenes 3D-Rathaus in Burning Hills

Stand: 2026-09-28T21:06:49.562013+00:00

Das Rathaus verwendet im bestehenden produktiven Gebäude-Slot jetzt das freigegebene 3D-Modell. Alle anderen 250 Gebäude-Slots behalten ihre bisherige Darstellung. Der Katalog-GUID, die Stadt-Szene, Standort, Spielanker und Gebäude-ID sind unverändert.

## Ansehen

Unity: `Assets/Weedtopia/Game/Scenes/BurningHills.unity` öffnen, Play starten, Stadtviertel über das Rathaus-Symbol anwählen und das Rathaus anklicken. Es wird wie die übrigen Gebäude über den bestehenden Katalog dargestellt.

- [Stadtumgebung](Screenshots/01_city_context.png)
- [Rathaus nah](Screenshots/02_town_hall_close.png)
- [Auswahl mit Informationsfenster](Screenshots/03_selected.png)
- [Statistiken](Screenshots/04_town_hall_statistics.png)
- [Rocko-Dialog](Screenshots/05_rocko_dialog.png)

## Technischer Stand

- LOD0: 50.000, LOD1: 20.000, LOD2: 12.000 Dreiecke. Je ein Material und drei 2K-Karten. Einfacher Box-Collider für Sockel und Turm, keine teure Mesh-Kollision.
- Produktiver Wrapper: `Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab`.
- Wiederverwendbarer Modell-Prefab: `Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928/WT_TownHall_LOD.prefab`.
- Der Modell-Prefab nutzt dieselben freigegebenen FBX- und Texturdateien wie der Prüfstand. Dadurch bleiben deren GUIDs erhalten; es gibt keine duplizierten Binärdateien.
- Die Detailkamera berücksichtigt jetzt sowohl Sprites als auch stabile Grenzen der 3D-Detailstufen. Grenzen werden aus der aktuellen visuellen Instanz ermittelt.
- Ursprünglicher Wrapper unter `Backup/`; Sprite im produktiven Wrapper deaktiviert erhalten. Der alte Meshy-FBX-Verweis wurde aus dem produktiven Wrapper entfernt, damit seine Texturen nicht mitgeladen werden. Originaldateien bleiben bestehen.
- Explizite Installation: `Weedtopia > Art Pipeline > 3D Town Hall > Install Approved Model In City`. Erneute Sprite-Reparatur respektiert diesen Modell-Slot.

## Nachweise

- EditMode: **4/4 bestanden** – vollständige LODs/2K-Karten, ausgeschlossene alte Abhängigkeiten, wiederholbare Installation, Szenenreferenzen und Produktionsprüfung.
- PlayMode: **19/19 bestanden** – bestehende Gebäudeinteraktionen sowie virtuelle Maus-/Touchauswahl des 3D-Rathauses, Kameraausschnitt, Rückkehr, Statistik-/Dialogfenster und stabile Grenzen über alle LODs.
- 3039 Bestandsdateien per Hash geprüft; nur die sieben geplanten Bestandsdateien geändert. Szene, Katalog, weitere Wrapper, Spielinhalt und Einstellungen unverändert.
- 9 echte Game-View-Aufnahmen bei laufendem Spiel, separater Diagnose-Spielstand. Keine Nutzerspielstände geändert.
- Maschinenlesbare Belege: `integration.json`, `summary.json`, `gameview_receipt.json`, `preservation_after.json`.

## Verbleibende Bewertung

Die Freigabe der 2K-Texturen und LODs ist dokumentiert. Die neue Stadtkomposition und Beleuchtung sind als eigener sichtbarer Zwischenstand vorbereitet; ihre Nutzerfreigabe wird nicht aus den Tests abgeleitet. In der vorhandenen Stadtbeleuchtung erscheint das 3D-Modell dunkler als die gemalten Nachbargebäude.

Player-Bildrate, Zielgerät, reale Touchbedienung und Gesamtspiel-Release sind damit nicht abgenommen. Die drei getrennten Textursätze haben rechnerisch zusammen rund 48 MiB BC5/BC7-Daten inklusive Mipmaps; ein größerer Bestand unterschiedlicher 3D-Gebäude braucht weiterhin ein Gesamtbudget. Meshy wurde nicht bedient, keine externen Credits verbraucht.
