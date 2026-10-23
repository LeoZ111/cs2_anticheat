# 0002: Cheater gegen normale Spieler (40 Matches aus CS2CD)

> **Intern.** Enthält Detektor-Merkmale und Werte. Nie in öffentliche Texte, Videos oder Posts übernehmen.

**Datum:** 24.10.2026 · **Daten:** 20 Matches mit Cheatern und 20 ohne aus dem Datensatz
[CS2CD](https://huggingface.co/datasets/CS2CD/CS2CD.Counter-Strike_2_Cheat_Detection) (Loo & Lužkov, Lizenz
CC-BY-4.0). Die Daten liegen nur lokal in `tracker/data/`. Spieler sind dort schon anonymisiert (`Player_1` …).
**Methode:** gleiche Analyse wie in [0001](0001-erste-demo-analyse.md), für jeden Kill die 3 Sekunden davor, pro Spieler
zusammengefasst (nur Spieler mit mindestens 3 Kills).

## Gruppen

| Gruppe | Spieler |
| --- | --- |
| Markierte Cheater | 91 |
| Andere Spieler in Cheater-Matches | 35 |
| Spieler in Matches ohne Cheater | 174 |

**Auffällig:** In vielen Cheater-Matches sind fast **alle** Spieler als Cheater markiert (z. B. 9 von 9). Das passt zu
Valves **Trust Factor**: Cheater werden oft gegen andere Cheater gematcht. Die Matches liegen alle zwischen Gold Nova
und Distinguished Master Guardian.

## Ergebnis

Median pro Gruppe. **Trennschärfe** (AUC) zeigt, wie gut ein Merkmal allein Cheater von Spielern aus sauberen Matches
trennt: 0,5 = gar nicht, 1,0 = perfekt. Ein Wert unter 0,5 heißt, dass Cheater **niedrigere** Werte haben.
Dann ist 1 − AUC die Trennschärfe.

| Merkmal | Cheater | Andere (Cheater-Match) | Saubere Matches | Trennschärfe |
| --- | --- | --- | --- | --- |
| Kills ohne vorheriges „gesehen“ | **82 %** | 25 % | 16 % | **0,95** |
| Wallbang-Kills | **50 %** | 0 % | 0 % | **0,88** |
| Kopfschuss-Quote | **80 %** | 57 % | 40 % | **0,86** |
| Reaktionszeit (Median) | **203 ms** | 508 ms | 535 ms | **0,74** (kürzer) |
| Kills mit Tracking durch Wand (≥ 250 ms) | 36 % | 25 % | 23 % | 0,69 |
| Tracking durch Wand pro Kill (Median) | 109 ms | 78 ms | 39 ms | 0,66 |
| Kills durch Rauch | 0 % | 0 % | 0 % | 0,45 (kein Signal) |
| größter Flick (90. Perzentil) | 1,3° / Tick | 1,4° | 1,5° | 0,50 (kein Signal) |
| Ping (Median) | 36 ms | 39 ms | 51 ms | kein Signal |

## Was das bedeutet

1. **Wissen ist das stärkste Signal, nicht Zielen.** Die besten Merkmale sind „Kill ohne Sicht“ und „Wallbang“: Cheater
   **wissen**, wo Gegner sind. Genau das ist der Kern deines Konzepts (Ursachen statt Ergebnisse) und genau das, was
   der Fog verhindert und die Köder sichtbar machen.
2. **Flicks sind kein Signal.** Die meisten Cheater hier „snappen“ nicht auffällig, oder ihr Aimbot ist geglättet.
   Ein Detektor, der nur auf schnelle Mausbewegungen schaut, würde sie verpassen.
3. **Kopfschuss-Quote und Reaktion trennen gut, sind aber gefährlich.** Gute ehrliche Spieler haben auch hohe Werte.
   Nach dem Konzept dürfen sie nur zusammen mit Wissens-Merkmalen zählen.
4. **Rauch-Kills sind kein Signal.** Das bestätigt, dass man sie nie allein verwenden darf.
5. **Die meisten Cheater hier sind offensichtlich** (50 % Wallbangs, 80 % Kopfschuss). Das sind „Rage“- oder
   Halb-Rage-Cheater in Matches mit niedrigem Trust Factor. **Unauffällige Cheater** („Closet Cheater“) sind viel
   schwerer und in diesen Daten vermutlich kaum dabei.

## Grenzen (ehrlich)

- **Nur 40 Matches.** Für Aussagen über Ränge, Karten oder Cheat-Arten zu wenig.
- **Markierungen sind nicht perfekt.** Laut den Autoren sind nur Matches mit VAC-gebannten Spielern von Hand geprüft.
  In den „sauberen“ Matches können Cheater stecken, und der Label „kein Cheater“ in Cheater-Matches stimmt nur zu
  etwa 56 %.
- **Sichtbarkeit kommt vom Radar-Wert des Spiels** (`approximate_spotted_by`), nicht von echter Sichtlinie. Mit
  eigener Kartengeometrie werden „Kill ohne Sicht“ und „Tracking durch Wand“ genauer.
- **Trennschärfe pro Merkmal** heißt nicht, dass wir einen Detektor haben. Sie zeigt nur, **welche Merkmale es wert
  sind**, weiter untersucht zu werden.

## Nächste Schritte (Vorschlag)

- Mehr Matches auswerten (der Datensatz hat 317 mit Cheatern), getrennt nach Rang.
- Nach dem Karten-Werkzeug: Sichtbarkeit mit echter Geometrie neu berechnen.
- Gezielt nach **unauffälligen** Cheatern suchen: Spieler mit normaler Kopfschuss-Quote, aber vielen Kills ohne Sicht.
- Vergleichsbasis aus Pro-Demos (HLTV): Wie sehen sehr gute, ehrliche Spieler bei denselben Merkmalen aus?
