# 0001: Python für Tracker und Auswertung, C# für das Plugin

**Status:** angenommen

## Frage

In welchen Sprachen bauen wir die Teile des Projekts?

## Entscheidung

- **Python** für Bann-Tracker, Demo-Auswertung, Messwerte und später den KI-Detektor.
- **C#** für das Fog-Plugin auf dem CS2-Server.

## Warum

- Leo lernt gerade Python. Die Python-Teile kann er so selbst schreiben und Stück für Stück übernehmen.
- Für Demos und KI gibt es in Python die besten Werkzeuge (z. B. `demoparser2`, pandas, PyTorch).
- CS2-Server-Plugins schreibt man mit CounterStrikeSharp, und das nutzt C#.

## Folgen

- Das Plugin schreibt zuerst Claude. Leo lernt den C#-Code zuerst zu lesen.
- Der Kern des Plugins bleibt unabhängig von CounterStrikeSharp, damit er später übertragbar ist.
