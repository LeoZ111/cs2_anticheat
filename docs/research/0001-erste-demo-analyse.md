# 0001: Erste Demo-Analyse (Wingman, Vertigo)

**Datum:** 23.10.2026 · **Quelle:** eigene Demo von Leo (offizieller Valve-Server, Wingman 2 gegen 2, 16 Runden,
35 Kills) · **Werkzeug:** `demoparser2` (Python), Analyse-Skript lokal, Demo nicht im Repository.

Spieler sind hier anonymisiert: **L** (Leo), **M** (Mitspieler), **A** und **B** (Gegner).

## Was wir gemessen haben

Für jeden Kill wurden die 3 Sekunden davor angeschaut (64 Ticks pro Sekunde):

- **Reaktion:** Zeit vom ersten Tick, in dem der Täter das Opfer laut Spiel „gesehen“ hat (`approximate_spotted_by`,
  das ist die Radar-Sichtbarkeit), bis zum Kill.
- **Fadenkreuz auf verstecktem Gegner:** wie lange das Fadenkreuz höchstens 4° vom Kopf des Opfers entfernt war,
  obwohl der Täter es noch nicht sehen konnte.
- **Flick:** größte Drehung der Sicht innerhalb eines Ticks kurz vor dem Kill.
- Dazu aus dem Kill-Event: Kopfschuss, Wallbang, durch Rauch, geblendet.

## Ergebnisse

| Spieler | Kills | Kopfschuss | Reaktion (Median) | Kills „nie gesehen“ | Kills durch Rauch | größter Flick |
| --- | --- | --- | --- | --- | --- | --- |
| A | 14 | 57 % | 234 ms | 6 | 4 | 3,0° / Tick |
| B | 10 | 60 % | 243 ms | 0 | 0 | 2,1° / Tick |
| M | 8 | 62 % | 266 ms | 1 | 1 | 1,3° / Tick |
| L | 3 | 0 % | 758 ms | 1 | 0 | 0,8° / Tick |

Die Konten von A und B sind alt (2004 und 2012) und haben keinen Bann.

**Ergebnis: Diese Demo zeigt keinen klaren Cheater.**
- A hat 4 Kills durch Rauch und 6 Kills, bei denen das Spiel das Opfer nie als „gesehen“ markiert hat. Das ist
  auffällig, aber: 3 davon waren durch Rauch (Rauch blockiert die Radar-Sichtbarkeit), die anderen auf kurze Distanz.
  Rauch-Spam ist ein normaler Spielzug.
- B hat drei Kills mit 31 ms „Reaktion“. Das ist zu schnell für einen Menschen, liegt hier aber vermutlich daran, dass
  die Radar-Sichtbarkeit verspätet umspringt (siehe unten).
- **Alle vier** Spieler hatten oft das Fadenkreuz auf versteckten Gegnern, auch Leo und M. Das ist normales
  „Crosshair Placement“: Man zielt vorher dahin, wo Gegner meistens herkommen.

## Was wir fürs Anti-Cheat daraus lernen

1. **Die Radar-Sichtbarkeit des Spiels ist kein guter Maßstab für „konnte er ihn sehen?“** Sie ist grob, berücksichtigt
   Rauch anders und springt verzögert um. Wir brauchen eine **eigene Sicht-Berechnung** mit Kartengeometrie und Rauch.
   Das ist genau die Wissens-Schicht des Fog. Das Karten-Werkzeug ist damit die wichtigste nächste Baustelle, für den
   Fog **und** für den Detektor.
2. **Vorzielen auf versteckte Gegner ist normal.** Ein Detektor darf nicht „Fadenkreuz auf verstecktem Gegner“ zählen,
   sondern muss prüfen, ob das Fadenkreuz einem **sich bewegenden** versteckten Gegner **folgt** (Tracking durch die
   Wand). Statisches Vorzielen auf bekannte Ecken muss erlaubt sein.
3. **Reaktionszeit braucht den richtigen Startpunkt:** ab dem Moment, in dem die Information beim Spieler ankam
   (Sicht **oder** Geräusch, plus Ping). Das stand schon im Konzept und bestätigt sich hier.
4. **Kills durch Rauch und Wallbangs allein sagen nichts.** Sie dürfen nie allein ein Signal sein (steht auch im
   Konzept).
5. **Ein Match ist viel zu wenig.** 35 Kills reichen nicht, um irgendwen zu beurteilen. Wir brauchen viele Demos und
   einen **Vergleichswert pro Rang**, damit wir wissen, was „normal“ ist.
6. **Die Werkzeugkette funktioniert:** Demo lesen, Positionen und Blickwinkel pro Tick, Kills und Steam-IDs (für den
   Bann-Tracker) gehen alle mit `demoparser2`.

## Nächste Schritte (Vorschlag)

- Analyse-Skript in den Tracker übernehmen (Python, Leos Teil), damit jede neue Demo automatisch ausgewertet wird und
  die Steam-IDs in die Tracker-Liste kommen.
- Nach dem Karten-Werkzeug: Sichtbarkeit mit eigener Geometrie neu berechnen und diese Demo noch einmal auswerten.
