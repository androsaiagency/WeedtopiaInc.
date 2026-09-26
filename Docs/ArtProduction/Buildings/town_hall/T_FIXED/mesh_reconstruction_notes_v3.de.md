# Rathaus: korrigierter Meshy-Aufbau

Die bisherigen Meshy-Läufe aus `mesh_input_3q.png` und `mesh_input_3q_v2_clean.png` sind wegen reproduzierbarer Löcher, Fassadenkeile, verzogener Wappengeometrie und defekter Eingangsstufen abgelehnt. Wiederholen oder Retexturieren derselben Geometrie gilt nicht als Reparatur.

`mesh_input_3q_v3_mesh_base.png` ist ausschließlich die Referenz für die geschlossene Grundarchitektur. Das Blattwappen darf nicht in die Meshy-Eingabe zurückkopiert und nicht durch Meshy als Relief, Ausschnitt oder Textur rekonstruiert werden. Fenster bleiben in der Rekonstruktionsvorlage matt und undurchsichtig. Pflanzen verdecken keine tragenden Kanten.

Das Wappen wird nach einem bestandenen Geometrie-Check in Unity als separates flaches Decal beziehungsweise als sichtbares Kindobjekt des Rathaus-Wrappers angebracht. Dadurch bleibt es austauschbar, wird nicht verzerrt und kann keine Löcher in der Rathausgeometrie verursachen. `town_hall_leaf_decal_v1.png` ist ein vorläufiges austauschbares Decal-Asset und keine geometrische Meshy-Referenz.

Ein neuer Meshy-Lauf ist nur akzeptabel, wenn die zentrale Frontwand, beide sichtbaren Seiten, Eingang, Vordach, sämtliche Stufen und der Übergang zum Turm geschlossen sind. Die Geometrie wird vor Export aus mehreren Blickwinkeln geprüft. Ein sichtbares Loch, ein schwebendes Fragment oder ein grüner Fassadenkeil führt zur Ablehnung.
