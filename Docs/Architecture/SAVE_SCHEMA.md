# Save-Schema 2

Lokaler JSON-Envelope, Format 1: Payload als JSON-String plus SHA-256-Prüfsumme. Die Prüfsumme erkennt beschädigte Daten; sie ist keine Signatur und kein Anti-Cheat-Schutz.

Der Payload enthält Schema-/Inhaltsversion, Namen, Introfortschritt, exakte Premium-/Kartenzähler, skalierbare Geldwerte, RNG-Zustand, UTC-Höchstzeit, Simulationsrestzeit, Städte, Gebäudeinstanzen, Bauzeiten, vier Produktionspuffer, Fahrerfracht, Workerattribute/Zuweisungen, Questhistorie, Tagesabholungen, VIP-Aufträge, Statistiken und Einstellungen. Keine Unity-Objektreferenzen.

Schreiben: validieren → UTF-8 in .tmp → Flush(true) → exakte Tempdatei erneut validieren → atomisch ersetzen. Eine gültige bisherige Hauptdatei wird .bak. Eine beschädigte Hauptdatei wird diagnostisch archiviert und ersetzt, ohne das letzte valide Backup zu überschreiben. Laden prüft zuerst Hauptdatei, dann Backup. Beide ungültig: kein Teilzustand wird übernommen; die UI zeigt Recovery-/Neustartoptionen.

Migration 1→2: UTC-Höchstzeit mindestens auf lastUtcTicks setzen, Tages-Tiercheckpoint aus dem vorhandenen Level ergänzen, anschließend vollständige aktuelle Validierung. Unbekannte zukünftige Versionen werden abgewiesen. Die Migration wird mit einer künstlichen Version-1-Datei geprüft.

Offlinezeit ist max(0, UTC−gespeicherte Höchstzeit), auf acht Stunden begrenzt. Die Höchstzeit wird vor der Simulation vorgezogen und danach gespeichert. Wiederholtes Laden desselben gespeicherten Ergebnisses dupliziert keinen Ertrag. Zurückgestellte Uhren erzeugen keine Offline-/Tagesprämie. Ein weit vorgestellter lokaler Zeitstempel kann spätere Belohnungen blockieren; es gibt bewusst keinen behaupteten Zeitserver.

Die spätere Online-Rangliste darf diesem Save nicht vertrauen. DTOs enthalten Spieleranzeige, CityId, Gesamtverdienst, Spielzeit und Freeplaystatus. Ein echter Server braucht Authentifizierung, eigene Fortschrittsvalidierung und Missbrauchsschutz. Der vorhandene Onlineadapter führt keinerlei Netzwerkaufruf aus.
