# 0003: Kern und Server-Adapter getrennt, Kern zuerst

**Status:** vorgeschlagen (Leo entscheidet mit dem Merge)

## Frage

Wie bauen wir das C#-Plugin auf, und womit fangen wir an?

## Varianten

1. **Alles in einem Plugin:** Der Code redet direkt mit CounterStrikeSharp. Testen geht nur auf einem laufenden Server.
2. **Kern + Adapter:** Der Kern (Geometrie, Sicht, Fog, Köder) kennt CS2 nicht. Ein dünner Adapter übersetzt zwischen
   Server und Kern.

## Entscheidung

Variante 2. Wir fangen mit dem Kern an, noch ohne Server.

## Warum

- Den Kern kann man mit normalen Tests prüfen, in Millisekunden, ohne CS2 zu starten. Das spart viel Zeit.
- Für den Kern braucht man nur das .NET SDK (etwa 200 MB), nicht den CS2-Server (etwa 70 GB).
- Wenn CounterStrikeSharp sich ändert oder wegfällt, muss nur der Adapter angepasst werden.
- Für eine spätere Vorstellung bei Valve ist ein engine-unabhängiger Kern leichter zu zeigen und zu übertragen.

## Folgen

- Ordner `plugin/AntiCheat.Core` (Kern), `plugin/AntiCheat.Core.Tests` (Tests) und
  `plugin/AntiCheat.CounterStrikeSharp` (Adapter).
- Alles läuft auf **.NET 10**, weil CounterStrikeSharp (ab Version 1.0.377) das verlangt.
- Für Vektoren nehmen wir `System.Numerics.Vector3` aus .NET. Das ist schnell, weil der Prozessor mehrere Zahlen auf
  einmal rechnen kann (SIMD).
- Tests mit xUnit, dem üblichen Test-Werkzeug für C#.
