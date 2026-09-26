# Wohnhaus – verbindliche Familien-DNA

Funktion: Wachsende Wohnkapazität; Post-/Community-Nische deutet Worker-Karten an, ohne Gameplaywerte.

Silhouette: Weiche Giebel, tiefe Loggia und dauerhaftes Community-Postmodul neben dem Eingang. Materialprioritäten: warm plaster, reclaimed wood, terracotta, oxidized copper.

Reservierte maximale Stellfläche: **16 × 16 m**, maximale Höhe **38 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+8; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Hauseingang vorn mittig; Community-Nische vorn rechts; Versorgung links. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierte Notunterkunft mit Community-Nische.
- T02: Zusätzliches breites Wohnmodul.
- T03: Seitliche Erweiterung der bestehenden Eingangsloggia um zwei breite Rundbogenfelder.
- T04: Rückwärtiger Familienflügel.
- T05: Großes bewohnbares Dachgeschoss mit breiter zentraler Gaube.
- T06: Zweiter gestaffelter Wohnflügel.
- T07: Durchgehende breite Gemeinschaftsloggia.
- T08: Erstes vollständiges Obergeschoss.
- T09: Runder Community-Erker.
- T10: Zweites vollständiges Obergeschoss.
- T11: Große integrierte Gartengalerie.
- T12: Drittes vollständiges Obergeschoss.
- T13: Breiter rückwärtiger Wohnblock.
- T14: Viertes gestaffeltes Obergeschoss.
- T15: Zwei breite begrünte Gemeinschaftsloggien.
- T16: Fünftes kompaktes Obergeschoss.
- T17: Hoher vertikaler Gartenkern.
- T18: Gekoppelter zweiter Wohnturm.
- T19: Großes Solardach über den Wohntürmen.
- T20: Dick verglaste gemeinsame Wintergartenhalle.
- T21: Gestufte obere Wohngalerie.
- T22: Dritter kompakter rückwärtiger Wohnturm.
- T23: Breite gemeinsame begrünte Dachterrasse.
- T24: Bergförmige kupferne Wohndachkrone.

Höhenstaffel: frühe Wohnstufen T01–T07 etwa 4,8–6,5 m, mit bewohnbarem Dachgeschoss ab T05; erstes vollständiges Obergeschoss T08 bei etwa 8,8 m. Weitere Geschosse und Wohntürme steigen danach bis 38 m. Die Stufenhöhen folgen der Architektur, keiner linearen Levelskalierung.
