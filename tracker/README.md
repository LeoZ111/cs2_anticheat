# Bann-Tracker

Der Bann-Tracker merkt sich, mit wem wir gespielt haben, und prüft regelmäßig über die Steam-API, ob diese Spieler
später gebannt wurden. So sehen wir, wen Valve wann bannt und wen nicht.

Er entsteht in kleinen Aufgaben. Jede Aufgabe ist ein echtes Stück des Trackers.

| Aufgabe | Was | Status |
| --- | --- | --- |
| 1 | Steam-IDs aus einer Datei lesen | fertig (Lösung von Claude) |
| 2 | Bann-Status über die Steam-API abfragen | fertig (Lösung von Claude) |
| 3 | Ergebnisse in einer kleinen Datenbank speichern | offen |
| 4 | Täglicher Abgleich und Bericht | kommt |

## Einrichten (einmalig)

Du brauchst Python 3.10 oder neuer. Öffne ein Terminal im Ordner `tracker`.

**Windows (PowerShell):**

```
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install -e ".[dev]"
```

**Linux / macOS:**

```
python3 -m venv .venv
source .venv/bin/activate
python -m pip install -e ".[dev]"
```

Was passiert da?

- `venv` legt eine **virtuelle Umgebung** an: einen eigenen Python-Bereich nur für dieses Projekt.
- `Activate` schaltet sie ein. Das musst du in jedem neuen Terminal wiederholen.
- `pip install` installiert das Projekt und `pytest`, das Werkzeug für die Tests.

## Tests laufen lassen

```
python -m pytest
```

Grün (`passed`) heißt: Der Code macht, was die Tests verlangen. Rot (`failed`) heißt: noch nicht.
Am Anfang sind die Tests von Aufgabe 1 rot. Das ist richtig so, denn die Funktion gibt es noch nicht.

## Aufgabe 1: Steam-IDs aus einer Datei lesen

**Datei:** `tracker/steam_ids.py`, Funktion `load_steam_ids`

Später schreiben wir die Steam-IDs aller Mitspieler aus unseren Demos in eine Textdatei, eine ID pro Zeile.
Deine Funktion liest so eine Datei und gibt die IDs als Liste zurück.

Die Funktion soll:

1. die Datei öffnen,
2. Zeile für Zeile lesen,
3. Leerzeichen, Tabs und Zeilenumbrüche am Anfang und Ende jeder Zeile entfernen,
4. leere Zeilen überspringen,
5. alle IDs in einer Liste zurückgeben, in der Reihenfolge aus der Datei.

Lösche die Zeile mit `raise NotImplementedError(...)` und schreib deinen Code an ihre Stelle.

**Fertig bist du, wenn `python -m pytest` vier grüne Tests zeigt.**

Lies auch die Tests in `tests/test_steam_ids.py`. Jeder Test ist ein kleines Beispiel dafür, was deine Funktion können
muss. `tmp_path` ist ein vorübergehender Ordner, den pytest für jeden Test anlegt und danach wieder löscht.

### Hinweise (nur lesen, wenn du feststeckst)

<details>
<summary>Hinweis 1: Datei öffnen</summary>

Mit `with open(filename) as file:` öffnest du eine Datei. Am Ende des `with`-Blocks wird sie automatisch geschlossen.

</details>

<details>
<summary>Hinweis 2: Zeilen lesen</summary>

`for line in file:` geht die Datei Zeile für Zeile durch. Jede `line` ist ein Text, meist mit `\n` am Ende.

</details>

<details>
<summary>Hinweis 3: Leerzeichen entfernen</summary>

Texte haben die Methode `.strip()`. Sie entfernt Leerzeichen, Tabs und Zeilenumbrüche am Anfang und Ende.

</details>

<details>
<summary>Hinweis 4: Leere Zeilen</summary>

Ein leerer Text `""` zählt in einem `if` als falsch. `if text:` ist also nur wahr, wenn etwas drinsteht.

</details>

Wenn du fertig bist: Erklär in 3–5 Sätzen in eigenen Worten, was deine Funktion macht. Das kommt in den Pull Request.


## Aufgabe 2: Bann-Status über die Steam-API abfragen

**Datei:** `tracker/steam_api.py`, Funktionen `parse_bans` (2a) und `is_banned` (2b)

Eine **API** ist eine Art Briefkasten für Programme: Unser Programm schickt Steam eine Anfrage („Sind diese Spieler
gebannt?“), und Steam antwortet mit Daten im Format **JSON**. In Python wird daraus ein `dict` mit Listen darin.

Das Abschicken der Anfrage ist schon fertig (von Claude): `get_api_key`, `build_bans_url`, `fetch_bans_response` und
`fetch_bans`. Deine Aufgabe ist das **Auswerten der Antwort**.

Die Tests brauchen **kein Internet und keinen API-Schlüssel**. Sie benutzen eine ausgedachte Antwort (`FAKE_RESPONSE`
in `tests/test_steam_api.py`), die genauso aufgebaut ist wie die echte. So eine nachgebaute Antwort nennt man **Mock**.

### 2a: `parse_bans(response)`

Steam antwortet mit vielen Feldern und Namen wie `NumberOfVACBans`. Wir wollen nur vier davon, mit einfachen Namen.
Deine Funktion soll:

1. die Liste unter `response["players"]` durchgehen,
2. für jeden Spieler ein neues `dict` bauen mit den Schlüsseln `steam_id`, `vac_bans`, `game_bans` und
   `days_since_last_ban` (die Werte kommen aus `SteamId`, `NumberOfVACBans`, `NumberOfGameBans`, `DaysSinceLastBan`),
3. alle diese `dict`s als Liste zurückgeben.

### 2b: `is_banned(player)`

Bekommt ein `dict`, wie es `parse_bans` baut. Gibt `True` zurück, wenn der Spieler mindestens einen VAC-Bann **oder**
mindestens einen Game-Bann hat, sonst `False`.

**Fertig bist du, wenn `python -m pytest` nur grüne Tests zeigt.**

### Hinweise (nur lesen, wenn du feststeckst)

<details>
<summary>Hinweis 1: An einen Wert im dict kommen</summary>

`response["players"]` gibt dir die Liste der Spieler. Jeder Spieler ist wieder ein `dict`, also z. B.
`player["SteamId"]`.

</details>

<details>
<summary>Hinweis 2: Ein neues dict bauen</summary>

Ein `dict` schreibst du mit geschweiften Klammern: `{"name": wert, "anderer_name": anderer_wert}`.
Den Aufbau mit Liste und `.append(...)` kennst du schon aus Aufgabe 1.

</details>

<details>
<summary>Hinweis 3: „oder“ in Python</summary>

`a > 0 or b > 0` ist `True`, wenn mindestens eins von beiden stimmt. So ein Vergleich ist schon selbst `True` oder
`False`, du kannst ihn direkt mit `return` zurückgeben.

</details>

### Mit echtem Schlüssel ausprobieren

1. Schlüssel holen auf steamcommunity.com/dev/apikey (bei „Domain“ reicht z. B. `localhost`). **Nie in eine Datei im
   Repo schreiben und nie teilen**, auch nicht mit Claude.
2. Einmalig speichern (PowerShell). Der Befehl fragt nach dem Schlüssel, so landet er nicht im Befehlsverlauf:

   ```
   [Environment]::SetEnvironmentVariable("STEAM_API_KEY", (Read-Host "Steam API Key"), "User")
   ```

   Danach ein **neues** Terminal öffnen, erst dort ist der Schlüssel da.
3. Eine Datei `data/ids.txt` anlegen, eine Steam-ID pro Zeile. Der Ordner `data/` wird nie committet.
4. Im Ordner `tracker`:

   ```
   python -m tracker data/ids.txt
   ```

   Ausgabe: wie viele Spieler geprüft wurden, wie viele gebannt sind, und die gebannten IDs.
## Aufgabe 3: Ergebnisse in einer kleinen Datenbank speichern

**Datei:** `tracker/database.py`, Funktionen `save_check` (3a) und `get_checks` (3b)

Eine **Datenbank** ist wie eine Tabelle in Excel, nur für Programme. Wir nehmen **SQLite**: Die ganze Datenbank ist
eine einzige Datei (z. B. `data/bans.db`), und SQLite ist schon in Python eingebaut (`import sqlite3`).

Mit der Datenbank redet man in der Sprache **SQL**. Du brauchst nur zwei Befehle:

- `INSERT`: eine neue Zeile hinzufügen
- `SELECT`: Zeilen lesen

Unsere Tabelle heißt `ban_checks`. Jede Zeile ist **eine Abfrage für einen Spieler an einem Tag**:

| steam_id | checked_at | vac_bans | game_bans | days_since_last_ban |
| --- | --- | --- | --- | --- |
| 76561198000000001 | 2026-10-22 | 0 | 0 | 0 |
| 76561198000000001 | 2026-10-23 | 1 | 0 | 0 |

Hier sieht man: Am 23.10. ist ein VAC-Bann aufgetaucht. Warum wir jede Abfrage speichern und nicht nur den letzten
Stand, steht in `docs/decisions/0002-ban-history.md`.

Schon fertig (von Claude): `open_database` (öffnet die Datei und legt die Tabelle an) und `save_checks` (speichert eine
ganze Liste und nimmt das heutige Datum).

### 3a: `save_check(connection, player, checked_at)`

Fügt **eine** Zeile in `ban_checks` ein. Die Werte kommen aus dem `dict` `player` (so wie es `parse_bans` baut) und aus
`checked_at`. Die Spalte `id` füllt SQLite selbst.

### 3b: `get_checks(connection, steam_id)`

Liest alle Zeilen für **einen** Spieler, **älteste zuerst**, und gibt sie als Liste von `dict`s zurück, jedes mit
`checked_at`, `vac_bans`, `game_bans` und `days_since_last_ban`.

**Fertig bist du, wenn `python -m pytest` nur grüne Tests zeigt.**

### Hinweise (nur lesen, wenn du feststeckst)

<details>
<summary>Hinweis 1: SQL aus Python schicken</summary>

`connection.execute("SQL-Befehl", (wert1, wert2))` schickt einen Befehl. Jedes `?` im Befehl wird der Reihe nach durch
einen Wert aus dem Tupel ersetzt. Ein Beispiel steht in `tests/test_database.py` in `insert_row`.

</details>

<details>
<summary>Hinweis 2: Warum `?` und nicht die Werte direkt in den Text?</summary>

Würde man Werte direkt in den SQL-Text kleben, könnte ein komischer Wert den Befehl verändern (das nennt man
**SQL-Injection**). Mit `?` behandelt SQLite den Wert immer nur als Wert. Also: immer `?` benutzen.

</details>

<details>
<summary>Hinweis 3: Zeilen lesen</summary>

`SELECT spalte1, spalte2 FROM ban_checks WHERE steam_id = ? ORDER BY checked_at` wählt die Zeilen aus.
`connection.execute(...).fetchall()` gibt dir eine Liste von Tupeln, z. B. `[("2026-10-22", 0, 0, 0)]`.
Jedes Tupel kannst du in einer `for`-Schleife auspacken: `for checked_at, vac_bans, game_bans, days in rows:`

</details>