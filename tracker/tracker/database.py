"""Storing ban check results in a small SQLite database."""

import datetime
import sqlite3

# One row per player per check. We keep every check, so we can see later *when* a ban appeared.
SCHEMA = """
CREATE TABLE IF NOT EXISTS ban_checks (
    id INTEGER PRIMARY KEY,
    steam_id TEXT NOT NULL,
    checked_at TEXT NOT NULL,
    vac_bans INTEGER NOT NULL,
    game_bans INTEGER NOT NULL,
    days_since_last_ban INTEGER NOT NULL
)
"""


def open_database(path: str) -> sqlite3.Connection:
    """Open (or create) the database file and make sure the table exists."""
    connection = sqlite3.connect(path)
    connection.execute(SCHEMA)
    return connection


def save_check(connection: sqlite3.Connection, player: dict, checked_at: str) -> None:
    """Store one check result for one player.

    player looks like what parse_bans returns:
        {"steam_id": "76561198000000001", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 12}
    checked_at is the date of the check, e.g. "2026-10-22".
    """
    # TODO(leo): Aufgabe 3a, siehe tracker/README.md
    raise NotImplementedError("Aufgabe 3a: siehe tracker/README.md")


def get_checks(connection: sqlite3.Connection, steam_id: str) -> list[dict]:
    """Return all stored checks for one player, oldest first.

    Each check is a dict like:
        {"checked_at": "2026-10-22", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 12}
    """
    # TODO(leo): Aufgabe 3b, siehe tracker/README.md
    raise NotImplementedError("Aufgabe 3b: siehe tracker/README.md")


def save_checks(connection: sqlite3.Connection, players: list[dict], checked_at: str | None = None) -> None:
    """Store the results of one check run for all players. checked_at defaults to today."""
    if checked_at is None:
        checked_at = datetime.date.today().isoformat()
    for player in players:
        save_check(connection, player, checked_at)
    connection.commit()
