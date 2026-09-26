# Supermarkt – verbindliche Familien-DNA

Funktion: Versorgung und wachsende Verkaufsfläche; ein einziges goldenes Zahnrad deutet Grinder-Bonus an.

Silhouette: Breite weiche Verkaufshalle mit vorgezogener Eingangshaube und einem goldenen Zahnradmotiv. Materialprioritäten: warm plaster, sage enamel, matte concrete, mustard gold.

Reservierte maximale Stellfläche: **22 × 18 m**, maximale Höhe **25 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+9; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Haupteingang mittig; Warenannahme links; Ausgabebucht rechts. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierte Verkaufshalle mit goldener Zahnradplakette.
- T02: Zweiter breiter Eingangsflügel.
- T03: Neue durchgehende seitliche Einkaufsarkade mit drei massiven Bögen.
- T04: Rückwärtiger Versorgungstrakt.
- T05: Hohe breite zentrale Lichtgewölbehalle über der bestehenden Verkaufsmitte.
- T06: Zweite breite Verkaufshalle.
- T07: Große runde Eingangshaube.
- T08: Zusammenhängendes solargedecktes Hallendach.
- T09: Runder emaillierter Kühlversorgungskern.
- T10: Breiter zusätzlicher Sortimentsflügel.
- T11: Großer rückwärtiger Warenpuffer.
- T12: Zweites vollständiges Verkaufsgeschoss.
- T13: Breite gestützte Kundengalerie.
- T14: Zweiter hoher Versorgungskern.
- T15: Großer geschlossener Abholtrakt.
- T16: Drittes kompaktes Verkaufsgeschoss.
- T17: Vertikales begrüntes Versorgungsatrium.
- T18: Zweiter gekoppelter oberer Marktflügel.
- T19: Grüngläserner Dachmarkt.
- T20: Großes kupfergefasstes Solardach.
- T21: Gestufte städtische Einkaufsrotunde.
- T22: Breite gemeinsame Versorgungsgalerie.
- T23: Zwei obere kompakte Markthallen.
- T24: Große runde Marktdachkrone mit einem Goldzahnrad.
