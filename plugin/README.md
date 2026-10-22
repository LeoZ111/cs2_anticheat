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
| `!` | Alle Befehle alphabetisch, mit Erklärung |
| `!b` | Alle Befehle, die mit „b“ anfangen |
| `!bot_s pro` | Abkürzung: passt der Anfang zu genau einem Befehl, wird er ausgeführt |
| `!bot_skill` | Menü mit allen Stufen im Chat, Auswahl mit `!1` bis `!5` |
| `!bot_skill noob` / `mid` / `hard` / `pro` / `legend` | Bots werden nacheinander ersetzt, mit dieser Stärke |
| `!map mirage` | Karte wechseln (`!map` allein zeigt alle Karten) |
| `!money max` / `normal` / `5000` | Geld: max = 16000 und kaufen überall, normal = wie Competitive, oder ein Betrag |
| `!warmup off` / `on` | Aufwärmphase beenden oder neu starten |
| `!roundtime 1,5` | Rundenzeit in Minuten, ab der nächsten Runde |
| `!nades off` / `less` / `normal` / `more` / `max` | Wie viele Granaten Bots werfen (NadeSystem) |
| `!bot_weapon awp` / `normal` | Waffe aller Bots ab der nächsten Runde (`bot_loadout`) |

Mit `/` statt `!` sehen die anderen den Befehl nicht im Chat.

Echtes Tab-Vervollständigen beim Tippen gibt es in CS2 nicht: Der Server sieht eine Nachricht erst nach Enter.
Darum zeigt `!` + Anfang + Enter die passenden Befehle. **Jeder neue Chat-Befehl muss in `AntiCheatPlugin.cs` bei
`ChatHelp.Register` eingetragen werden**, sonst taucht er in der Liste nicht auf. Die Bots werden im Abstand von einer halben Sekunde
ersetzt. Alle auf einmal hat Spieler mit einem „Überlauf“-Fehler vom Server geworfen.

Für Fehlersuche gibt es in der Server-Konsole `css_ac_bots`: zeigt Rundenstatus und die Positionen aller Bots.

noob bis pro nutzen CS2s eigene Bot-Stärke (`bot_difficulty` 0 bis 3, gilt für alle Bots). **Legend** ersetzt die
Gegner-Bots durch Bot-Profile eines echten Pro-Teams (zufällig: Vitality, Spirit, Falcons, FURIA oder NAVI). Das
braucht die Bot-Plugins von CS2-Bot-Improver auf dem Testserver, siehe `docs/server-setup.md`.

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
