# Stadt-Lager – verbindliche Familien-DNA

Funktion: Verteilt Farm-Ernte zu Coffee-Shops und Industrie; mehrere Ausgänge bleiben klar getrennt.

Silhouette: Abgerundeter städtischer Block mit gestaffelter Attika und zweigeteilter Verteilfront. Materialprioritäten: cream plaster, matte concrete, olive enamel, oxidized copper.

Reservierte maximale Stellfläche: **22 × 18 m**, maximale Höhe **30 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+9; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Annahme vorn links; Shop-Ausgang vorn mittig; Industrie-Ausgang vorn rechts. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierter Stadtblock mit zweigeteilter Ausgabe.
- T02: Dritte breite Verteilbucht.
- T03: Tiefe zusammenhängende Lade-Arkade.
- T04: Rückwärtige Sammelhalle.
- T05: Dauerhafter abgerundeter Verwaltungsflügel.
- T06: Zweiter klarer Industrie-Ausgabetrakt.
- T07: Runde zentrale Verteilwarte.
- T08: Breites städtisches Solardach.
- T09: Große emaillierte Sortierhalle.
- T10: Zweiter geschlossener Sortierflügel.
- T11: Hoher rückwärtiger Stadtspeicher.
- T12: Zweites zusammenhängendes Verteilgeschoss.
- T13: Große mehrteilige Ausgangsarkade.
- T14: Runder vertikaler Sortierkern.
- T15: Breiter oberer Speicherflügel.
- T16: Drittes Verteilgeschoss.
- T17: Kupfergefasster hoher Versorgungsturm.
- T18: Zweiter gekoppelter Versorgungsturm.
- T19: Grüngläserne obere Sortierhalle.
- T20: Solarterrasse über den Ausgangsportalen.
- T21: Gestaffelte städtische Logistikkrone.
- T22: Hohe zentrale Dispatch-Rotunde.
- T23: Zwei integrierte obere Verteilbrücken.
- T24: Dreiteilige Versorgungsspitze mit runder Zentralhaube.
