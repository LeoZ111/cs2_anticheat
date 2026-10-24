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

### Simulierte Cheats (nur im Testmodus)

Der Testmodus ist an, wenn im Plugin-Ordner auf dem Server die Datei `testmode.enabled` liegt (dann auch nach jedem
Neuladen und Neustart), oder für den Moment mit `ac_testmode 1`. **Auf einem echten Server diese Datei nie anlegen.**

| Im Chat | Was passiert |
| --- | --- |
| `!cheat` | Liste aller Cheats und ob sie an sind |
| `!cheat radar` | Radar-Hack: alle Gegner auf deinem Radar |
| `!cheat esp` | ESP/Wallhack: Gegner leuchten durch Wände, nur für dich |
| `!cheat aim_legit` | Aimbot legit: **nur beim Schießen** sanft zum **sichtbaren** Kopf (12°), Körpertreffer 1,5× stärker |
| `!cheat aim_rage` | Aimbot rage: **nur beim Schießen** auf den nächsten **sichtbaren** Kopf, **jeder Treffer zählt als Kopfschuss** |
| `!cheat trigger` | Triggerbot-Hilfe: zeigt **FEUER**, solange dein Fadenkreuz auf einem sichtbaren Gegner ist |
| `!cheat speed` | Speedhack: 50 % schneller |
| `!cheat bhop` | Bunnyhop (gilt für alle auf dem Server, weil CS2 das nur für alle kann) |

`!cheat esp` schaltet um, `!cheat esp on` / `off` schaltet fest. Alles läuft auf dem Server. Wie echte Cheats
nutzen die Cheats nur Gegner, die der Server an dein Spiel schickt. Mit Fog werden sie also blind.
**Warum so:** In CS2 entscheidet das Spiel des Spielers, wohin die Kugel fliegt. Dreht der Server die Sicht dauernd,
kämpft das gegen die Maus und ruckelt. Deshalb dreht der Aimbot nur beim Schießen und nur zu Gegnern, die laut
Kartengeometrie (`MapSight`) wirklich sichtbar sind. Dazu werden **echte Treffer verstärkt** (Schadens-Hook,
`HitUpgrade` im Kern). **Fehlschüsse** bleiben Fehlschüsse, denn echtes Silent Aim bräuchte ein eigenes C++-Plugin.
Ein Server kann für einen Menschen nicht abdrücken, deshalb zeigt der Triggerbot nur einen Hinweis.

Echtes Tab-Vervollständigen beim Tippen gibt es in CS2 nicht: Der Server sieht eine Nachricht erst nach Enter.
Darum zeigt `!` + Anfang + Enter die passenden Befehle. **Jeder neue Chat-Befehl muss in `AntiCheatPlugin.cs` bei
`ChatHelp.Register` eingetragen werden**, sonst taucht er in der Liste nicht auf. Die Bots werden im Abstand von einer halben Sekunde
ersetzt. Alle auf einmal hat Spieler mit einem „Überlauf“-Fehler vom Server geworfen.

Für Fehlersuche gibt es in der Server-Konsole `css_ac_bots`: zeigt Rundenstatus und die Positionen aller Bots.

noob bis pro nutzen CS2s eigene Bot-Stärke (`bot_difficulty` 0 bis 3, gilt für alle Bots). **Legend** ersetzt die
Gegner-Bots durch Bot-Profile eines echten Pro-Teams (zufällig: Vitality, Spirit, Falcons, FURIA oder NAVI). Das
braucht die Bot-Plugins von CS2-Bot-Improver auf dem Testserver, siehe `docs/server-setup.md`.

## Fog of War (Version 1)

Der Server schickt jedem Spieler nur Gegner, die er **sehen oder hören** kann. Alle anderen fehlen in seinem Spiel
komplett, also kann auch kein Wallhack sie anzeigen.

- **Kern:** `AntiCheat.Core/Fog/FogSystem.cs` (mit Tests). Pro Tick und pro Paar (Spieler, Gegner) gilt:
  - Teammates, Tote, Zuschauer: immer senden
  - näher als 160 Einheiten: senden
  - **sichtbar**: Sichtlinie von den Augen zu einem von 5 Punkten (Kopf, Brust, Füße, beide Schultern) ist frei.
    Das gilt auch für die **vorhergesagte** Position nach Ping + 0,1 s, gegen das „Aufploppen“ um Ecken.
  - **gerade noch gesehen**: noch 8 Ticks weiter senden, gegen Flackern
  - **hörbar**: rennt (schneller als 135) und näher als 1.100, oder hat geschossen (1 s lang, bis 2.500)
  - sonst: **nicht senden**
- **Plugin:** `AntiCheat.CounterStrikeSharp/Fog/FogModule.cs` entfernt versteckte Gegner (und ihre Waffen) aus dem,
  was der Server an jeden Spieler schickt (`CheckTransmit`).
- **Wände:** `plugins/AntiCheat/maps/<karte>.actri`. `deploy.ps1` erzeugt sie für alle 11 Karten automatisch
  (nach einem CS2-Update: `.\deploy.ps1 -RebuildMaps`).

| Befehl | Was |
| --- | --- |
| `!fog` / `!fog on` / `!fog off` | Fog an/aus (nur Testmodus) |
| `ac_fog 1` | dasselbe in der Server-Konsole oder Config |
| `css_fog_stats` (Konsole) | Rechenzeit pro Tick, Sichtlinien pro Tick, Anteil versteckter Gegner |

**Gemessen auf Dust2 mit 10 Bots:** 0,35 ms pro Tick im Schnitt, 532 Sichtlinien pro Tick, 92 % der Gegner-Paare
versteckt.

**Zusammen mit `!cheat esp`:** Der Fog registriert sich vor den Cheats. Die simulierten Cheats sehen also nur, was
der Fog durchlässt, genau wie echte Cheats.

**Noch nicht:** Rauch blockiert noch keine Sicht. Geräusche werden nur grob behandelt (das Leck wird später gemessen).
Fenster und dünne Gitter zählen als Wand, wenn sie in der Kollision so stehen.

## Karten-Werkzeug

`AntiCheat.MapTool` liest die **Kollisions-Geometrie** einer CS2-Karte (`maps/<karte>.vpk` →
`world_physics.vmdl_c`) mit der Bibliothek [ValveResourceFormat](https://github.com/ValveResourceFormat/ValveResourceFormat)
(MIT-Lizenz) und schreibt sie als Dreiecke in eine `.actri`-Datei (Format: `AntiCheat.Core/Geometry/MapGeometry.cs`).

```
dotnet run --project AntiCheat.MapTool -c Release -- D:\cs2server\cs2\game\csgo\maps\de_dust2.vpk ..\tracker\data\maps\de_dust2.actri
dotnet run --project AntiCheat.MapTool -c Release -- check ..\tracker\data\maps\de_dust2.actri [paare.csv]
```

Die Karten-Dateien sind aus Valves Spieldaten erzeugt und kommen **nicht ins Repository** (`tracker/data/` wird ignoriert).

Jedes Dreieck hat eine **Ebene**: `default` (normale Wände), `playerclip`/`npcclip` (unsichtbare Wände nur für Spieler),
`csgo_grenadeclip`, `passbullets` (Kugeln fliegen durch), `sky`. Für **Sicht** zählen alle außer den Clips und
`passbullets` (`Check.BlocksSight`).

**Dust2:** 435.649 Dreiecke, eingelesen in 2 s. BVH gebaut in 0,2 s. **0,8 µs pro Sichtlinie**, also etwa 620
Sichtlinien in 0,5 ms. Verglichen mit der Sicht-Markierung des Spiels in 154.109 Spieler-Paaren aus echten Matches:
Wenn unsere Linie blockiert ist, sagt das Spiel nur in 0,2 % der Fälle „gesehen“.

## Was schon da ist: Sichtlinien

Der Fog muss für jeden Spieler wissen: **Kann ich diesen Gegner sehen, oder ist eine Wand dazwischen?**

Dafür denken wir uns eine gerade Linie vom Auge des Spielers zum Gegner (`Segment`). Die Karte besteht aus vielen
kleinen **Dreiecken** (`Triangle`), so wie jedes 3D-Spiel. Wenn die Linie irgendein Dreieck durchstößt, ist die Sicht
blockiert.

- `Intersection.SegmentHitsTriangle`: Trifft eine Linie ein Dreieck? Das Verfahren heißt **Möller-Trumbore** und ist
  der Standard dafür, weil es sehr schnell ist.
- `LineOfSight.IsBlocked`: Prüft eine Linie gegen eine Liste von Dreiecken.

- `Bvh`: ein Baum aus Kisten um die Dreiecke. Liegt die Linie nicht in einer Kiste, werden alle Dreiecke darin auf
  einmal übersprungen. Ein Test vergleicht die BVH mit „jedes Dreieck prüfen“ an 2.000 Zufallslinien.

Einheiten: CS2-Einheiten (ungefähr 2,5 cm), **Z zeigt nach oben**. Ein stehender Spieler hat die Augen bei etwa 64.
