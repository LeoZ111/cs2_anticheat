# Bann-Tracker

Der Bann-Tracker merkt sich, mit wem wir gespielt haben, und prüft regelmäßig über die Steam-API, ob diese Spieler
später gebannt wurden. So sehen wir, wen Valve wann bannt und wen nicht.

Er entsteht in kleinen Aufgaben. Jede Aufgabe ist ein echtes Stück des Trackers.

| Aufgabe | Was | Status |
| --- | --- | --- |
| 1 | Steam-IDs aus einer Datei lesen | fertig (Lösung von Claude) |
| 2 | Bann-Status über die Steam-API abfragen | offen |
| 3 | Ergebnisse in einer kleinen Datenbank speichern | kommt |
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

### Später: mit echtem Schlüssel ausprobieren (freiwillig)

Einen eigenen Schlüssel bekommst du auf steamcommunity.com/dev/apikey. **Nie in eine Datei im Repo schreiben und nie
teilen.** Für ein PowerShell-Fenster setzt du ihn so (gilt nur, bis du das Fenster schließt):

```
$env:STEAM_API_KEY = "dein-schlüssel"
```