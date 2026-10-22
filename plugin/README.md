# Plugin (C#)

Hier entsteht das Fog-Plugin für den CS2-Server. Es ist in zwei Teile getrennt:

| Ordner | Was | Braucht CS2? |
| --- | --- | --- |
| `AntiCheat.Core` | Der **Kern**: Geometrie, Sicht, später Fog und Köder | nein |
| `AntiCheat.Core.Tests` | Tests für den Kern | nein |
| `AntiCheat.CounterStrikeSharp` | Der **Adapter**: verbindet den Kern mit dem Server | ja |

Warum getrennt? Siehe `docs/decisions/0003-plugin-core.md`.

## Einrichten (einmalig)

Du brauchst das **.NET 10 SDK** (ist auf Leos PC schon installiert). CounterStrikeSharp verlangt .NET 10. Prüfen:

```
dotnet --list-sdks
```

Dort muss eine Zeile mit `10.0.` stehen.

## Plugin auf den Testserver bringen

Im Ordner `plugin`:

```
.\deploy.ps1
```

Das baut das Plugin und kopiert es nach `D:\cs2server\cs2\game\csgo\addons\counterstrikesharp\plugins\AntiCheat`.
Wie der Server eingerichtet und gestartet wird, steht in `docs/server-setup.md`.

## Tests laufen lassen

Im Ordner `plugin`:

```
dotnet test
```

Am Ende steht `Bestanden!` (oder `Passed!`) mit der Zahl der Tests.

## Testmodus-Befehle (nur Testserver)

| Im Chat | Was passiert |
| --- | --- |
| `!enemy_skill noob` / `mid` / `hard` / `pro` / `legend` | Alle Bots werden neu eingesetzt, mit dieser Stärke |

Mit `/` statt `!` sehen die anderen den Befehl nicht im Chat.

Die Stufen nutzen CS2s eigene Bot-Stärke (`bot_difficulty` 0 bis 3). **Legend** ist im Moment noch wie Pro. Besser als
Pro wird es erst mit unserer eigenen Bot-Steuerung (Zielen, Bewegung, Reaktion auf Geräusche), die später kommt.
`bot_difficulty` gilt für alle Bots, also auch für die im eigenen Team.

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
