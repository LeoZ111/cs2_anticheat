# cs2_anticheat — Arbeitsanleitung für Claude

Projekt von Leo (GitHub LeoZ111): ein server-seitiges Anti-Cheat-System für CS2 (Fog of War, Köder, KI-Detektor,
Bann-Tracker). Leo leitet das Projekt und lernt dabei programmieren (aktuell Python-Grundlagen). Ziel ist, das System
später Valve zu zeigen.

## Sprache
- Mit Leo: Deutsch, einfach und ohne Fachjargon, Fachbegriffe beim ersten Mal erklären.
- Code, Bezeichner, Commit-Messages: Englisch.
- Dokumentation in `docs/` und READMEs: Deutsch.

## Zusammenarbeit (kein Vibe Coding)
- Leo entscheidet. Bei Architektur-Fragen Varianten vorschlagen und erklären, nicht selbst festlegen.
- Nie direkt auf `main` pushen. Alles über Pull Requests, Leo mergt. Ausnahme war nur der erste Commit (leeres Repository).
- Kleine Pull Requests, höchstens ein paar hundert Zeilen. Jeder PR hat eine Erklärung auf Deutsch: was, warum,
  wie man es testet. Bei wichtigen Teilen 2–3 Verständnisfragen an Leo.
- **Leo-Aufgaben** sind mit `TODO(leo)` markiert. Dafür schreibt Claude Tests und Erklärungen, aber **nie die Lösung**.
  Steckt Leo fest: Hinweise geben, Schritt für Schritt, nicht den fertigen Code.
- Leos Rolle wächst mit: Python-Teile (Tracker, Auswertung, später KI) sind seine, das C#-Plugin schreibt zuerst Claude.

## Sicherheit und Daten
- Kein Cheat-Code, keine Anleitungen zum Umgehen von VAC oder anderen Anti-Cheats. Cheats nur server-seitig simulieren.
- Interna (Köder-Regeln, Detektor-Merkmale, Schwellen) nie in öffentliche Texte, Videos oder Posts übernehmen.
- Keine Steam-IDs, Demos, Datenbanken oder API-Schlüssel committen. Der Steam-API-Schlüssel kommt aus der
  Umgebungsvariable `STEAM_API_KEY`.
- Strikt getrennt von Leos anderem Projekt (Shiny): kein Code und keine Verweise von dort.

## Prüfen
- Python: `cd tracker && python -m pytest`. Tests, die eine offene `TODO(leo)`-Aufgabe prüfen, dürfen fehlschlagen.
- Nichts behaupten, was nicht gebaut oder getestet wurde. Im Spiel testen kann nur Leo.
