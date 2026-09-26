# Texturreparatur nach bestandenem Geometrie-Lauf

Der v3-Meshy-Lauf erzeugte nach Nutzerprüfung keine Löcher mehr. Seine bildprojizierte Textur ist wegen flächiger Artefakte abgelehnt. Die geschlossene Geometrie bleibt deshalb erhalten und darf nicht erneut generiert oder ersetzt werden.

Nächster zulässiger Schritt ist ausschließlich eine Retexturierung des vorhandenen Meshes mit `meshy_retexture_prompt_en_v4.txt`. Vor dem Start werden Modus, Kosten und die Option zum unveränderten Erhalt der Geometrie geprüft. Ein kostenpflichtiger Retextur-Lauf benötigt eine separate Freigabe.

Abnahme: große ruhige Materialflächen, klare Grenzen an vorhandenen Bauteilnähten, keine Projektionsschlieren, keine zufälligen Flecken, keine Fassadengrafik und kein Emblem. Das Wappen bleibt ein separates Unity-Decal. Falls die Retexturierung erneut Artefakte erzeugt, wird die Meshy-Textur verworfen und das geschlossene Mesh mit Unity-Materialien ausgeliefert.
