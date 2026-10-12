# Inventar und Glücksrad in der unteren Leiste

Die untere Leiste in Burning Hills enthält jetzt neun runde Buttons: Übersicht, Farm, Stadt, Siedlung, Industrie, Aufträge, Mein Team, **Inventar**, **Glücksrad**. Die beiden neuen Einstiege stehen rechts neben Mein Team.

Das Inventar verwendet das vorhandene Kisten-Symbol. Das Glücksrad hat ein eigenes kleines Rad-Symbol; ein Stern am Button zeigt den noch verfügbaren Gratis-Dreh an. Der Stern verschwindet nach dem gespeicherten Dreh und erscheint wieder beim täglichen Reset. Die bestehenden Einstiege in Aufträgen und Inventar sind ebenfalls weiterhin nutzbar.

Die Buttons sind Instanzen des bestehenden `WorldNavigationButton.prefab` in `WorldNavigationDock.prefab`. Die gesamte Leiste ist auf 1.008 Referenzpixel erweitert, die neun Buttons stehen im Abstand von 112 Referenzpixeln. Der vorhandene `WorldNavigationBar` passt die Leiste an den sicheren Bildschirmbereich an. Es werden keine statischen Controls zur Laufzeit erzeugt.

`MenuLayoutAuthoring` erweitert das vorhandene Prefab und bindet die neuen Einstiege über die bestehende `GameUi`-Navigation. Ein erneuter Aufruf des expliziten Installers erhält Szene, Dock und beide GUID-Dateien bytegleich. Der ältere World-Navigation-Installer erkennt ebenfalls die erweiterte Leiste.

## Prüfung

- **7/7 PlayMode-Tests bestanden**: neue Einstiege mit simuliertem Touch, vorhandene Navigation und Settings, Gratis-Stern, keine Vergabe beim bloßen Öffnen, vorhandene Glücksrad-Funktion.
- **3/3 Asset-Tests bestanden**: neun wiederverwendbare runde Controls, erreichbare Weltziele, Referenzen und wiederholte Installation.
- **12/12 Assemblies im Source-Compiler bestanden**; die kompilierten Quelldateien entsprechen dem aktuellen Stand.
- **14 echte Game-View-Aufnahmen**: Welt/Leiste, Inventar und Glücksrad in 1920×1080, 1366×768 mit UI-Skalierung 1,15, 2560×1080 und 1080×1920; zusätzlich Gewinnpräsentation und Leiste nach dem Gratis-Dreh.
- **Keine Textüberläufe oder Aktionen außerhalb des sicheren Bereichs** in diesen Aufnahmen.
- Alle bisherigen Objekt-IDs von Szene und Dock sowie beide Asset-GUIDs bleiben erhalten. Gameplay-, Inventar- und Preisregeln wurden für diese Änderung nicht bearbeitet.

Die Tests und Aufnahmen verwenden getrennte Diagnose-Speicherdateien. Physische Touch-Bedienung und persönliche Grafikabnahme sind durch diese automatischen Prüfungen nicht ersetzt.

Die Originaldateien liegen in `Originals/`, der aktuelle Nachweis in `completion_receipt.json`, die Aufnahmen in `Screenshots/`. In Unity lässt sich die Installation über **Weedtopia → Presentation → Install Direct Menu Layout** wiederholen.
