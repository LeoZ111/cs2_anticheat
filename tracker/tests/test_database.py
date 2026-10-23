import datetime

import pytest

from tracker import database
from tracker.database import get_checks, open_database, save_check, save_checks

PLAYER = {"steam_id": "76561198000000001", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 12}


@pytest.fixture
def connection(tmp_path):
    """A fresh, empty database for each test. pytest deletes it afterwards."""
    connection = open_database(str(tmp_path / "bans.db"))
    yield connection
    connection.close()


def insert_row(connection, steam_id, checked_at, vac_bans, game_bans, days_since_last_ban):
    """Put a row in directly, so the tests for get_checks do not depend on save_check."""
    connection.execute(
        "INSERT INTO ban_checks (steam_id, checked_at, vac_bans, game_bans, days_since_last_ban) "
        "VALUES (?, ?, ?, ?, ?)",
        (steam_id, checked_at, vac_bans, game_bans, days_since_last_ban),
    )


def all_rows(connection):
    return connection.execute(
        "SELECT steam_id, checked_at, vac_bans, game_bans, days_since_last_ban FROM ban_checks ORDER BY id"
    ).fetchall()


# --- Aufgabe 3a: save_check ---


def test_save_check_stores_one_row(connection):
    save_check(connection, PLAYER, "2026-10-22")
    assert all_rows(connection) == [("76561198000000001", "2026-10-22", 1, 0, 12)]


def test_save_check_twice_keeps_both_rows(connection):
    save_check(connection, PLAYER, "2026-10-22")
    save_check(connection, {**PLAYER, "vac_bans": 2, "days_since_last_ban": 0}, "2026-10-23")
    assert all_rows(connection) == [
        ("76561198000000001", "2026-10-22", 1, 0, 12),
        ("76561198000000001", "2026-10-23", 2, 0, 0),
    ]


# --- Aufgabe 3b: get_checks ---


def test_get_checks_returns_checks_of_one_player_oldest_first(connection):
    insert_row(connection, "76561198000000001", "2026-10-23", 1, 0, 0)
    insert_row(connection, "76561198000000002", "2026-10-22", 0, 0, 0)
    insert_row(connection, "76561198000000001", "2026-10-22", 0, 0, 0)
    assert get_checks(connection, "76561198000000001") == [
        {"checked_at": "2026-10-22", "vac_bans": 0, "game_bans": 0, "days_since_last_ban": 0},
        {"checked_at": "2026-10-23", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 0},
    ]


def test_get_checks_for_unknown_player_gives_empty_list(connection):
    assert get_checks(connection, "76561198000000009") == []


# --- Already done by Claude: these should be green right away ---


def test_open_database_creates_table(connection):
    assert all_rows(connection) == []


def test_data_survives_closing_and_opening_again(tmp_path):
    path = str(tmp_path / "bans.db")
    first = open_database(path)
    insert_row(first, "76561198000000001", "2026-10-22", 0, 0, 0)
    first.commit()
    first.close()
    second = open_database(path)
    assert len(all_rows(second)) == 1
    second.close()


def test_save_checks_saves_every_player_with_todays_date(connection, monkeypatch):
    saved = []
    monkeypatch.setattr(database, "save_check", lambda conn, player, checked_at: saved.append((player, checked_at)))
    save_checks(connection, [PLAYER, PLAYER])
    today = datetime.date.today().isoformat()
    assert saved == [(PLAYER, today), (PLAYER, today)]
