# 0004: Detektor auf 200 Matches, und was wirklich trägt

> **Intern.** Enthält Detektor-Merkmale und Werte. Nie veröffentlichen.

**Datum:** 24.10.2026 · **Daten:** 100 Matches mit und 100 ohne Cheater aus CS2CD (472 markierte Cheater, 999 Spieler
aus sauberen Matches) · **Code:** `detector/` (`python -m detector`, `python -m detector.combine`)

## Ergebnis pro Merkmal

| Merkmal | 40 Matches (0003) | **200 Matches** |
| --- | --- | --- |
| Kills ohne vorherige Sicht | 0,95 | **0,82** |
| Kopfschuss-Quote | 0,86 | **0,80** |
| Wallbang-Kills | 0,88 | **0,77** |
| Tracking durch Wände | 0,69 | 0,64 |
| Silent Aim | 0,71 | 0,59 |
| Triggerbot (nur 88 Spieler mit Wert) | (1,0) | 0,57 |
| Drehung ohne Maus | 0,56 | 0,56 |
| Rückstoß zu ruhig | 0,69 | 0,55 |

## Kombination (logistische Regression, getestet nur auf unbekannten Matches)

| Merkmale | Trennschärfe | erkannt bei 5 % Fehlalarmen | bei 1 % | bei 0 % (von 999 Sauberen) |
| --- | --- | --- | --- | --- |
| alle | 0,83 | 30 % | 7 % | 0 % |
| nur Wissen | 0,80 | 23 % | 6 % | 1 % |
| nur Physik | 0,71 | 7 % | 2 % | 0 % |

## Ehrliche Bewertung

1. **Die 40-Match-Ergebnisse waren zu gut.** Die ersten 20 Cheater-Matches haben zufällig besonders offensichtliche
   Cheater (Median 50 % Wallbangs). In den übrigen 80 sind es 33 %, und alle Merkmale trennen schlechter.
   Lehre: **nie auf wenigen Daten feiern**, immer auf neuen Daten nachprüfen.
2. **Vom Ziel sind wir weit weg.** Das Konzept will höchstens 1 Fehlalarm auf 10.000. Mit den Merkmalen aus Demos
   erkennen wir bei 1 % Fehlalarmen nur 7 % der Cheater. Für Banns ist das unbrauchbar.
3. **Wissen bleibt das beste Signal**, aber mit der Radar-Sicht des Spiels ist es zu ungenau.
4. **Die Physik-Merkmale sind in Valve-Demos schwach.** Wahrscheinlich, weil die Demos Eingaben nur pro Tick speichern
   (Sub-Tick geht verloren) und weil die meisten Cheater hier Wallhacks nutzen.
5. **Die Markierungen begrenzen die Messung:** In den „sauberen“ Matches stecken laut den Autoren vermutlich auch
   Cheater. Ein Teil der „Fehlalarme“ sind vielleicht echte Cheater.

## Was daraus folgt

Das bestätigt den Plan im Konzept, und zwar deutlich:

- **Nur aus Demos wird das nichts mit wenigen Fehlalarmen.** Die starken Werkzeuge brauchen den **eigenen Server**:
  - **Fog**: Cheater bekommen das Wissen gar nicht erst.
  - **Köder**: Ein Fake-Gegner, den nur ein Cheater sehen kann. Wer reagiert, hat einen Beweis statt einer
    Wahrscheinlichkeit. Das ist der einzige Weg zu „1 Fehlalarm auf 10.000“.
  - **Volle Eingaben pro Sub-Tick** auf dem Server, für die Physik-Merkmale.
- **Das Karten-Werkzeug ist die Grundlage für alles drei**: echte Sichtlinien für Fog, Köder und eine genaue
  Wissens-Bilanz.

## Nächste Schritte

1. Karten-Werkzeug: Kartengeometrie aus den CS2-Dateien lesen (Grundlage für Fog, Köder, Wissens-Bilanz).
2. Wissens-Bilanz mit echter Sicht neu messen, auf denselben 200 Matches.
3. Detektor auf dem Testserver gegen die simulierten Cheats prüfen (volle Eingaben).
