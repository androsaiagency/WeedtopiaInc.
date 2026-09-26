# Park – verbindliche Familien-DNA

Funktion: Erholung und VIP-/Community-Treffpunkt als erkennbare Park-Parzelle; keine Quest-UI.

Silhouette: Niedrige ovale Einfassung mit zentralem Wegbogen und rundem Community-Pavillon hinten. Materialprioritäten: reclaimed wood, warm stone, muted green foliage, oxidized copper.

Reservierte maximale Stellfläche: **16 × 16 m**, maximale Höhe **18 m**. Ein Unity-Meter entspricht einer Unity-Einheit. Die reservierte Fläche wird nicht als Podest gerendert. Das frühe Visual nutzt nur einen Teil der Fläche; zunehmende Kapazität wächst bevorzugt nach hinten und oben. Die in den Slots genannten Maße sind Richtwerte für die Art-Produktion, keine Gameplaywerte.

Front bleibt lokal **+Z**, oben **+Y**, Pivot am Bodenmittelpunkt der reservierten Parzelle. Die Vorderkante bleibt bei z=+8; ein kleineres Visual darf seinen Körper nach hinten von dieser Frontkante entwickeln. Im Bild erscheint die +Z-Front links unten, die +X-Seite rechts unten. Endgültiger geometrischer Pivot und Anschlüsse werden am Wrapper manuell geprüft.

Anschlüsse: Zugang vorn mittig; Pflegezugang links; Community-Pavillon auf rückwärtiger Achse. InteractionAnchor am vorderen Hauptzugang, WorkerAnchor seitlich des Servicebereichs, DeliveryInputAnchor/DeliveryOutputAnchor passend zu den beschriebenen Ports, VfxAnchor über dem Funktionskern. ColliderRoot bleibt leer bis zur sichtbaren vereinfachten Collider-QA. Fahrzeuge, Förderbänder und Worker bleiben eigenständige spätere Prefabs.

T01 ist der verbindliche Formanker. T00 zeigt seine reparierbare Vorgeschichte; danach wird T02 aus T01 weiterentwickelt, nicht aus T00. Jede folgende Stufe nutzt T01 plus ihren direkten akzeptierten Vorgänger. Bei Gebäuden bleiben frühere Funktionsvolumen integriert und dürfen kompakt gestapelt werden; keine 24 unabhängigen Anbauten. Beim Anbau ersetzt jede Kategorie die vorherige vollständig und hält die vorgegebene exakte Modulzahl ein.

## Ausbaumerkmale

- T01: Reparierter kleiner Rundpavillon mit einem Beet.
- T02: Zusätzliche breite erhöhte Pflanzterrasse hinter dem Pavillon.
- T03: Neue seitliche Sitzarkade mit drei massiven Rundbögen außerhalb des bisherigen Pavillondachs.
- T04: Breiter rückwärtiger Community-Anbau mit eigenem zweitem Runddach.
- T05: Dauerhafter Rundweg mit Pflanzeinfassung.
- T06: Erste große kompakte Baumkrone über der rückwärtigen Pflanzterrasse.
- T07: Breite gestützte Pergola.
- T08: Große ruhige Brunnenmulde ohne Fontäne.
- T09: Breite runde Gartenrotunde auf der hinteren erhöhten Terrasse.
- T10: Zweite vollständige Pergola-Arkade.
- T11: Breite leicht erhöhte Gartenterrasse.
- T12: Zweigeschossiger Community-Pavillon.
- T13: Großes rückwärtiges Gartenhaus.
- T14: Zusätzliche obere Pflanzterrasse.
- T15: Breite grüngläserne Orangerie.
- T16: Drittes gestuftes Pavillongeschoss.
- T17: Vertikaler Gartenkern hinter dem Zugang.
- T18: Zweiter gekoppelter Gartenkern.
- T19: Große Kupfer-Solarpergola.
- T20: Breite gestützte Gartengalerie.
- T21: Oberste städtische Baumterrasse.
- T22: Hoher runder VIP-/Community-Pavillon.
- T23: Zwei gekoppelte Orangerieflügel.
- T24: Zentrale botanische Kuppelkrone über dem Park.

Höhenstaffel: Die Slot-Gesamthöhe enthält Baumkronen. Architektur und Bäume werden in jeder Asset-Spezifikation getrennt beziffert: T01-Pavillon 2,8 m; T06–T08 Bäume bis 4,8 m bei 3,3–3,5 m hoher Architektur. Das zweite Pavillongeschoss entsteht erst T12. Die Endstufe erreicht insgesamt höchstens 18 m.
