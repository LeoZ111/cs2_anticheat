# 0002: Jede Bann-Abfrage speichern, nicht nur den aktuellen Stand

**Status:** vorgeschlagen (Leo entscheidet mit dem Merge von Aufgabe 3)

## Frage

Wie speichert der Bann-Tracker die Ergebnisse der Steam-Abfragen?

## Varianten

1. **Nur den aktuellen Stand:** eine Zeile pro Spieler, die bei jeder Abfrage überschrieben wird.
2. **Jede Abfrage als neue Zeile (Verlauf):** pro Spieler und Abfrage-Tag eine Zeile in der Tabelle `ban_checks`.

## Entscheidung

Variante 2: der Verlauf.

## Warum

- Der Tracker soll zeigen, **wann** Valve bannt. Dafür muss man sehen, an welchem Tag sich ein Wert geändert hat.
  Bei Variante 1 ist der alte Wert weg.
- Die Datenmenge bleibt klein: auch 1.000 Spieler mal 365 Tage sind für SQLite wenig.
- Eine einzige Tabelle ist einfacher zu lernen als mehrere Tabellen mit Verknüpfungen.

## Folgen

- Aufgabe 4 (täglicher Abgleich) vergleicht die letzte mit der vorletzten Abfrage eines Spielers.
- Die Datenbank liegt unter `data/` und wird nie committet (persönliche Daten).
- Falls die Datei später zu groß wird, kann man unveränderte Tage zusammenfassen. Das ist erst nötig, wenn es wirklich
  ein Problem ist.
