# cs2_anticheat

Forschungsprojekt für ein server-seitiges Anti-Cheat-System für Counter-Strike 2.

**Privat.** Dieses Repository enthält Konzepte und Code, die Cheatern helfen könnten, das System zu umgehen.
Nichts davon wird öffentlich geteilt, bevor es bewusst entschieden wurde.

## Ziele

1. **Fog of War:** Der Server schickt jedem Spieler nur, was er sehen oder hören darf. Wallhacks verlieren ihren Nutzen.
2. **Köder:** Gegner, die es nicht gibt. Wer auf sie reagiert, verrät seinen Wallhack.
3. **KI-Detektor:** Wertet Verhalten über viele Matches aus, mit sehr wenigen Fehlalarmen.
4. **Bann-Tracker:** Beobachtet, wen Valve wann bannt, als Vergleich und als Trainingsdaten.

Ehrliche Spieler sollen vom System nichts merken. Netzwerk- oder Leistungsprobleme dürfen nie wie Cheaten aussehen.

## Aufbau

| Ordner | Inhalt | Sprache |
| --- | --- | --- |
| `tracker/` | Bann-Tracker | Python |
| `docs/` | Architektur und Entscheidungen | Deutsch |
| `plugin/` | Fog-Plugin für CounterStrikeSharp (kommt später) | C# |

## Regeln

- **Keine Cheats.** Kein Cheat-Code, keine Umgehungen, keine echten Cheats beim Testen. Cheats werden nur auf dem
  eigenen Testserver simuliert.
- **Keine geheimen Details nach außen.** Köder-Regeln, Detektor-Merkmale und Schwellen bleiben in diesem Repository.
- **Keine persönlichen Daten im Repository.** Steam-IDs, Demos und API-Schlüssel liegen nur lokal (siehe `.gitignore`).
- **Nichts wird gemergt, was Leo nicht erklären kann.**
