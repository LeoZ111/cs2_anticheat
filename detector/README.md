# Detektor (Version 0)

> **Intern.** Merkmale und Schwellen nie veröffentlichen.

Sucht in Match-Daten nach Dingen, die für Menschen **physikalisch oder logisch unmöglich** sind, statt nach
Gewohnheiten. Liest Daten im Format von `demoparser2` (z. B. der Datensatz CS2CD).

## Merkmale

| Datei | Merkmal | Idee |
| --- | --- | --- |
| `features/mouse_pull.py` | Drehung ohne Maus | Der Teil der Sichtdrehung, den die Maus nicht erklärt, zeigt bei einem Aimbot zum Gegner |
| `features/silent_aim.py` | Silent Aim | Treffer fliegen woanders hin, als Blick plus Rückstoß zeigen |
| `features/recoil.py` | No-Recoil | Beim Sprayen bleibt die echte Zielrichtung zu ruhig |
| `features/trigger.py` | Triggerbot | Schuss genau im Tick, in dem das Fadenkreuz den Gegner berührt |
| `features/knowledge.py` | Wissen ohne Quelle | Kills ohne vorherige Sicht, Tracking durch Wände (Version 0: Radar-Sicht des Spiels) |

## Benutzen

```
cd detector
..\tracker\.venv\Scripts\python -m pip install -e ".[dev]"
..\tracker\.venv\Scripts\python -m pytest
..\tracker\.venv\Scripts\python -m detector ..\tracker\data\cs2cd
```

Der letzte Befehl rechnet alle Merkmale für alle Spieler aus und zeigt, wie gut jedes Merkmal Cheater von Spielern
aus sauberen Matches trennt. Ergebnisse stehen in `docs/research/0003-detektor-v0.md`.

Die Daten liegen nur lokal in `tracker/data/` und kommen nie ins Repository.
