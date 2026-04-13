# RTR-Exercise-01_lu013

#Github Link
[https://github.com/lukasurbangamedev/RTR_Exercise1_lu013](https://github.com/lukasurbangamedev/RTR_Exercise1_lu013)


## Stack

- Complexes [Trex-Model](https://skfb.ly/EysY) von Sketchfab
- Unity Engine (auf Windows also DirectX Graphics API)
- C#


## Controls

Die Kamera Bewegung funktioniert exakt wie im Unity Scene Editor. 
Also LMB gedrueckt halten um sich anschliessend mit WASD (Vor, Links, Zurueck, Rechts) und Q/E (hoch, runter) zu bewegen.
Wenn man die Leertaste gedrueckt haelt rotiert sich der Dinosaurier.

## Parameter

Man kann die Geschwindigkeit der Kamera bewegungen ueber den Unity-Inspector oder das C# Script anpassen.
Ebenso kann man die Geschwindigkeit der Rotation des Dinosauriers im Unity-Inspector oder C# Script anpassen.

## Logging & Analyse

Ich habe die:
1. deltaTime in Sekunden (deltaTimeS)
2. Distanz der Kamera zum Dino pivot (cameraDistance)
3. Position der Kamera (word-space) (cameraPosition)
4. Die Angulare Geschwindigkeit des Dinosauriers (rotationAngularSpeed)
aufgenommen und in `log.csv` gespeichter. 

In meinem Experiment habe ich:
1. die kamera bewegt. 
2. unterschiedliche distanzen zum dino ausprobiert. 
3. nur den dino rotieren lassen
4. Den Dino rotieren lassen und gleichzeitig die Kamera bewegt.
5. nichts getan.



## External Sources
Ich habe fuer kleine fragen ChatGpt verwendet.
