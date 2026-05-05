# RTR-Exercise-01_lu013

#Github Link
[https://github.com/lukasurbangamedev/RTR_Exercise1_lu013](https://github.com/lukasurbangamedev/RTR_Exercise1_lu013)


## Stack

- Complexes [Trex-Model](https://skfb.ly/EysY) von Sketchfab
- Unity Engine (auf Windows also DirectX Graphics API)
- C#


## Controls

Arrow UP oder Arrow Down um die anzahl der instances zu vergroesern.
I um instanced und non-instanced umzuschalten.


## Logging & Analyse

Ich habe die:
1. fps
2. deltaTimeS
3. renderingMode
4. numTris
5. numDrawCalls
6. objectCount
aufgenommen und in `Instancing_log.csv` gespeichter. 

In meinem Experiment habe ich die anzahl der instanzes erhoeht und veringert sowohl mit gpu instancing an als auch aus.


## Instanced und Non-Instanced

### Instanced
Ich benutzte Unity-URP und wenn bei dem Material GPU Instancing an ist verwendet Unity Automatisch GPU Instancing. 
GPU verlagert den Rendering Bottleneck von der CPU auf die GPU indem die CPU statt mehreren DrawCalls pro instanz nur einen einzelnen an die GPU schickt.
Die CPU braucht sehr lange um einen Draw Call zu senden waehrend die GPU diesen sehr schnell abarbeiten kann. 
Durch verminderung der Draw Calls wird es schneller.

### Non-Instanced
Beim Non-Instanced approach schickt die CPU pro Instanz jeweils Draw-Calls an die GPU. 
Falls moeglich versucht Unity jedoch mithilfe des SRP-Batchers die Instanzen zu batches um performance einsparen zu koennen.
In meinem Experiment konnte ich jedoch mithilfe des Frame Debuggers feststellen das sich trotzdem die anzahl der Draw-Calls drastisch erhoeht.



## Speedup factor calc
Ich habe den speed up faktor mit der formel frametime_non_instanced / frametime_instanced berechnet. 
die frametime ist aussagekraeftiger als FPS da sie die tatsaechliche dauer (also auch arbeit) des Frames anzeigt.



## External Sources
Ich habe fuer kleine fragen ChatGpt verwendet.
