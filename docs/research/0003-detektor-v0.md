# 0003: Detektor Version 0, Unmögliches statt Gewohnheiten

> **Intern.** Enthält Detektor-Merkmale und Werte. Nie veröffentlichen.

**Datum:** 24.10.2026 · **Daten:** dieselben 40 Matches aus CS2CD wie in [0002](0002-cs2cd-cheater-vergleich.md)
(101 markierte Cheater, 199 Spieler aus sauberen Matches) · **Code:** `detector/`

## Idee

Closet Cheater tarnen ihr Verhalten. Die Spuren ihres Cheats in den Daten können sie aber nicht verhindern. Deshalb
sucht der Detektor nach Dingen, die für Menschen unmöglich sind:

1. **Drehung ohne Maus:** Ein Aimbot dreht die Sicht, ohne dass die Maus sich bewegt.
2. **Silent Aim:** Treffer fliegen nicht dahin, wohin Blick und Rückstoß zeigen.
3. **No-Recoil:** Rückstoß wird zu perfekt ausgeglichen.
4. **Triggerbot:** Schuss im selben Tick, in dem das Fadenkreuz den Gegner berührt.
5. **Wissen ohne Quelle:** Kills ohne vorherige Sicht, Tracking durch Wände.

Jedes Merkmal hat Tests mit erfundenen Daten, bei denen das Ergebnis bekannt ist (z. B. ein simulierter Aimbot, der zum
Gegner zieht).

> **Nachtrag:** Auf 200 Matches sind die Werte deutlich schlechter, siehe [0004](0004-detektor-200-matches.md).

## Ergebnis auf echten Daten

Trennschärfe: 0,5 = Münzwurf, 1,0 = perfekt.

| Merkmal | Cheater (Median) | Saubere (Median) | Trennschärfe | Spieler mit Wert | Bewertung |
| --- | --- | --- | --- | --- | --- |
| Kills ohne vorherige Sicht | 82 % | 16 % | **0,95** | 300 | stark |
| Wallbang-Kills | 50 % | 0 % | **0,88** | 300 | stark |
| Kopfschuss-Quote | 80 % | 40 % | **0,86** | 300 | stark, aber allein gefährlich |
| Silent Aim (90 % der Treffer weichen höchstens so weit ab) | 3,6° | 1,2° | **0,71** | 320 | neues Signal |
| Rückstoß-Wackeln beim Sprayen | 0,72° | 0,94° | **0,69** | 166 | neues Signal |
| Tracking durch Wände | 36 % | 23 % | 0,69 | 300 | mittel |
| Triggerbot (Schuss im 1. Tick auf dem Gegner) | 50 % | 19 % | (1,0) | **nur 18** | zu wenige Daten |
| Drehung ohne Maus | −0,03 | 0,01 | 0,56 | 133 | kaum Signal |

## Was das heißt

- **Silent Aim und No-Recoil sind echte neue Signale.** Sie hängen nicht von Gewohnheiten ab, sondern von Physik.
  Gerade das Rückstoß-Merkmal kann auch unauffällige Cheater treffen.
- **Triggerbot:** Die Richtung stimmt, aber im Datensatz gibt es sehr wenige Schuss-Ereignisse pro Spieler. Mit
  vollständigen Demos (eigener Server oder eigene `.dem`-Dateien) neu messen.
- **Drehung ohne Maus zeigt hier kaum etwas.** Mögliche Gründe:
  - Die Demo speichert nur eine Eingabe pro Tick, CS2 verarbeitet aber mehrere (Sub-Tick). Dadurch ist der Zusammenhang
    Maus ↔ Drehung verrauscht (Korrelation nur 0,7–0,9).
  - Viele Cheater im Datensatz nutzen vermutlich vor allem Wallhacks, keinen Aimbot.
  - Manche Aimbots erzeugen künstliche Mausbewegungen, dann passt Maus zu Drehung.
  - Auf **unserem Server** sehen wir jede einzelne Eingabe. Dort das Merkmal noch einmal prüfen.

## Grenzen

- Wie in 0002: wenige Matches, unvollständige Markierungen, Sicht über den Radar-Wert des Spiels.
- Trennschärfe pro Merkmal ist noch kein Urteil. Ein Bann darf nie an einem Merkmal hängen (siehe Konzept).

## Nächste Schritte

1. Mehr Matches (z. B. 100 + 100) und getrennt nach Rang auswerten.
2. Merkmale **kombinieren** (z. B. Wissen + Silent Aim + Rückstoß) und messen, wie viele Cheater bei sehr wenigen
   Fehlalarmen erkannt werden (Ziel aus dem Konzept: höchstens 1 Fehlalarm auf 10.000).
3. Nach dem Karten-Werkzeug: Sicht mit echter Geometrie und einem Geräusch-Modell (Wissens-Bilanz Version 1).
4. Auf dem Testserver mit den simulierten Cheats prüfen, ob jedes Merkmal „seinen“ Cheat findet.
