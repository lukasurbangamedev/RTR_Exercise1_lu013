# RTR-Exercise-01_lu013

#Github Link
[https://github.com/lukasurbangamedev/RTR_Exercise1_lu013](https://github.com/lukasurbangamedev/RTR_Exercise1_lu013)


## Stack

- Gun Model (https://sketchfab.com/3d-models/sci-fi-bullpup-rifle-1d3c8f11af294214a27fe8c64df0328e)
- Unity Engine (auf Windows also DirectX Graphics API)
- C#


## Controls

1 druecken fuer baseline.
2 fuer unoptimized.
3 fuer optimized.


## Logging & Analyse

Ich habe die:
1. timestamp
2. mode
3. fps
4. frametime_ms
5. render_texture_width
6. render_texture_height
7. secondary_camera_enabled
8. secondary_camera_update_rate
9. draw_calls
10. batches
11. tris
12. visible_object_count

gemessen.
 

Bei sekundaerer Kamera mit render texture verdoppeln sich bei mir die draw calls. Daher kommt der der starke drop
in fps. Von optimiert zu unoptimiert ist kein starker unterschied feststellbar. Aber die GPU braucht weniger pixel 
(also auch weniger fillrate). 


## External Sources
Ich habe in Exercise 05 ChatGpt und Claude fuer die methode welche die anzahl der sichtbaren objekte berechnet verwendet 
sowie ein wenig fuer die analyse. 
