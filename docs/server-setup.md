# Testserver einrichten (Windows)

Der Testserver ist ein eigener CS2-Server nur für dieses Projekt. Er liegt **getrennt** vom normalen Spiel in
`D:\cs2server`. So landet nie etwas von Metamod oder unseren Plugins im Spiel, mit dem wir online spielen (sonst
könnte VAC anspringen).

## Was ist was?

- **SteamCMD:** Valves Programm, um Server herunterzuladen und aktuell zu halten.
- **Dedicated Server:** CS2 ohne Spielfenster. Man verbindet sich mit dem normalen Spiel darauf.
- **Metamod:Source:** lädt Erweiterungen (Plugins) in den Server.
- **CounterStrikeSharp:** baut auf Metamod auf und erlaubt Plugins in C#.

## Ordner

```
D:\cs2server\
  steamcmd\            SteamCMD
  cs2\                 der Server (Kopie des Spiels, von SteamCMD aktuell gehalten)
  downloads\           heruntergeladene Zip-Dateien
  start-server.bat     startet den Server
  update-server.bat    bringt den Server auf den neuesten Stand
```

## Einmalig einrichten

1. **SteamCMD** von Valve laden (`steamcmd.zip`) und nach `D:\cs2server\steamcmd` entpacken.
2. **Spiel kopieren statt herunterladen** (spart etwa 60 GB Download):
   ```
   robocopy "D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive" "D:\cs2server\cs2" /E /MT:16
   ```
3. **Server prüfen und aktualisieren.** SteamCMD lädt nur, was sich vom Spiel unterscheidet. Beim allerersten Start
   aktualisiert sich SteamCMD selbst und beendet sich; dann einfach noch einmal starten.
   ```
   D:\cs2server\steamcmd\steamcmd.exe +force_install_dir D:\cs2server\cs2 +login anonymous +app_update 730 validate +quit
   ```
4. **Metamod 2.0** (Windows, von mms.alliedmods.net) nach `D:\cs2server\cs2\game\csgo` entpacken.
5. **CounterStrikeSharp** (`counterstrikesharp-with-runtime-windows-*.zip`, von GitHub) ebenfalls nach
   `D:\cs2server\cs2\game\csgo` entpacken.
6. In **`D:\cs2server\cs2\game\csgo\gameinfo.gi`** (nur im Server-Ordner!) unter der Zeile
   `Game_LowViolence csgo_lv` diese Zeile einfügen:
   ```
   Game    csgo/addons/metamod
   ```
   **Achtung:** Nach jedem Server-Update überschreibt Valve diese Datei. Dann die Zeile wieder einfügen.

## Starten und verbinden

1. `D:\cs2server\start-server.bat` doppelklicken. Ein Konsolenfenster öffnet sich, der Server startet auf `de_dust2`.
   Fragt Windows nach der Firewall: Für Spielen am selben PC ist das nicht nötig.
2. Im Server-Fenster prüfen:
   ```
   meta list
   css_plugins list
   ```
   Dort müssen CounterStrikeSharp und unser Plugin `AntiCheat` stehen.
3. Im normalen CS2 die Konsole öffnen und eingeben:
   ```
   connect 127.0.0.1:27015
   ```

## Bessere Bots: CS2-Bot-Improver (nur Teile davon)

Für bessere Bots und die Stufe `legend` nutzen wir Teile von
[CS2-Bot-Improver](https://github.com/ed0ard/CS2-Bot-Improver) (Version 1.4.6, Lizenz AGPL-3.0).
**Kein Code davon kommt in dieses Repository.** Unser Plugin schickt nur Befehle wie `bot_add_ct "ZywOo"`.

Eingebaut aus `CS2BotImprover_rules_unchanged.zip` (Sicherung vorher in `D:\cs2server\backups\before-botimprover`):

| Teil | Wohin |
| --- | --- |
| BotController, BotVision (Metamod-Plugins) | `addons/BotController`, `addons/BotVision` + ihre `.vdf` in `addons/metamod` |
| BotAI, BotBuy, BotControllerImpl, BotState, NadeSystem | `addons/counterstrikesharp/plugins/...` |
| 0Harmony, BotControllerApi | `addons/counterstrikesharp/shared/...` |
| Granaten-Daten | `addons/counterstrikesharp/data/NadeSystem` |
| Pro-Bot-Profile | `overrides/botprofile.vpk` |

In `gameinfo.gi` steht dafür **vor** der Metamod-Zeile:
```
Game    csgo/overrides/botprofile.vpk
```

**Bewusst nicht eingebaut:**
- das mitgelieferte Metamod und CounterStrikeSharp (älter als unseres)
- BotAimImprover: Sein „Kopfschuss-Zielen“ schaltet `mp_damage_headshot_only` für **alle** Spieler um.
- BotRandomizer und BotHider (Skins und Profile für Bots, brauchen wir nicht)
- gameinfo.gi, Spielregel-Dateien und das Panel-Programm
- **Nie im normalen Spiel installieren.** Dort könnte VAC anspringen.

## Plugin neu aufspielen

Im Ordner `plugin` des Repos: `.\deploy.ps1`. Danach im Server-Fenster `css_plugins reload AntiCheat` oder den
Server neu starten.
