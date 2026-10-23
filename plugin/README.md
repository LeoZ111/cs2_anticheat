# Plugin (C#)

Hier entsteht das Fog-Plugin für den CS2-Server. Es ist in zwei Teile getrennt:

| Ordner | Was | Braucht CS2? |
| --- | --- | --- |
| `AntiCheat.Core` | Der **Kern**: Geometrie, Sicht, später Fog und Köder | nein |
| `AntiCheat.Core.Tests` | Tests für den Kern | nein |
| `AntiCheat.CounterStrikeSharp` | Der **Adapter**: verbindet den Kern mit dem Server (kommt später) | ja |

Warum getrennt? Siehe `docs/decisions/0003-plugin-core.md`.

## Einrichten (einmalig)

Du brauchst das **.NET 8 SDK** (ist auf Leos PC schon installiert). Prüfen:

```
dotnet --list-sdks
```

Dort muss eine Zeile mit `8.0.` stehen.

## Tests laufen lassen

Im Ordner `plugin`:

```
dotnet test
```

Am Ende steht `Bestanden!` (oder `Passed!`) mit der Zahl der Tests.

## Was schon da ist: Sichtlinien

Der Fog muss für jeden Spieler wissen: **Kann ich diesen Gegner sehen, oder ist eine Wand dazwischen?**

Dafür denken wir uns eine gerade Linie vom Auge des Spielers zum Gegner (`Segment`). Die Karte besteht aus vielen
kleinen **Dreiecken** (`Triangle`), so wie jedes 3D-Spiel. Wenn die Linie irgendein Dreieck durchstößt, ist die Sicht
blockiert.

- `Intersection.SegmentHitsTriangle`: Trifft eine Linie ein Dreieck? Das Verfahren heißt **Möller-Trumbore** und ist
  der Standard dafür, weil es sehr schnell ist.
- `LineOfSight.IsBlocked`: Prüft eine Linie gegen eine Liste von Dreiecken.

Im Moment wird jedes Dreieck einzeln geprüft. Eine echte Karte hat aber Hunderttausende Dreiecke. Darum kommt als
Nächstes eine **BVH**: ein Baum aus Kisten um die Dreiecke. Liegt die Linie nicht in einer Kiste, kann man alle
Dreiecke darin auf einmal überspringen.

Einheiten: CS2-Einheiten (ungefähr 2,5 cm), **Z zeigt nach oben**. Ein stehender Spieler hat die Augen bei etwa 64.
