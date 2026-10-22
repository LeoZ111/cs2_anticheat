# Bann-Tracker

Der Bann-Tracker merkt sich, mit wem wir gespielt haben, und prüft regelmäßig über die Steam-API, ob diese Spieler
später gebannt wurden. So sehen wir, wen Valve wann bannt und wen nicht.

Er entsteht in kleinen Aufgaben. Jede Aufgabe ist ein echtes Stück des Trackers.

| Aufgabe | Was | Status |
| --- | --- | --- |
| 1 | Steam-IDs aus einer Datei lesen | offen |
| 2 | Bann-Status über die Steam-API abfragen | kommt |
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
