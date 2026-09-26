# Farm-Veredelung – verbindliche Familien-DNA

Funktion: Veredelt Ernte zwischen Anbau und Farm-Lager; Durchsatz wird über Tank- und Hallenvolumen sichtbar.

Silhouette: Weiche Giebelhalle mit rundem Veredelungstank rechts und breitem, eingefasstem Ernteportal. Materialprioritäten: warm plaster, sage enamel, aged corrugated metal, brass.

Reservierte maximale Stellfläche: **18 × 16 m**, maximale Höhe **28 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+8; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Ernteeingang vorn links; veredelter Ausgang vorn rechts; Wartung hinten. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierte Ein-Tank-Halle.
- T02: Zweiter großer Veredelungstank.
- T03: Überdachtes breites Ernteportal.
- T04: Rückwärtiger Werkstattflügel.
- T05: Dauerhafte verputzte Seitenhalle.
- T06: Dritter hoher Veredelungstank.
- T07: Breiter geschlossener Ausgabetrakt.
- T08: Solardach über der Veredelungshalle.
- T09: Großer emaillierter Reaktordom.
- T10: Zweiter Reaktordom auf gemeinsamem Sockel.
- T11: Kräftige integrierte Rohrbrücke.
- T12: Zweigeschossige Durchsatzhalle.
- T13: Rückwärtige automatisierte Hallensektion.
- T14: Hoher runder Filterturm.
- T15: Großer Ausgabepuffer als Zylindertank.
- T16: Zweites vollständiges Hallenobergeschoss.
- T17: Vertikaler Veredelungsschacht mit grünem Glas.
- T18: Gekoppelter zweiter Vertikalschacht.
- T19: Breites Kupfer-Solardach.
- T20: Kompakter oberer Kreislauftankring.
- T21: Gestufte obere Veredelungsterrasse.
- T22: Hoher gemeinsamer Prozesskern.
- T23: Zwei abgerundete obere Hallenflügel.
- T24: Burning-Hills-Krone aus drei verbundenen Tankkuppeln.
