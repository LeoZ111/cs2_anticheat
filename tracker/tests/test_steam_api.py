import pytest

from tracker import steam_api
from tracker.steam_api import build_bans_url, fetch_bans, get_api_key, is_banned, parse_bans

# A made-up answer, shaped like the real Steam API answer. No internet needed.
FAKE_RESPONSE = {
    "players": [
        {
            "SteamId": "76561198000000001",
            "CommunityBanned": False,
            "VACBanned": False,
            "NumberOfVACBans": 0,
            "DaysSinceLastBan": 0,
            "NumberOfGameBans": 0,
            "EconomyBan": "none",
        },
        {
            "SteamId": "76561198000000002",
            "CommunityBanned": False,
            "VACBanned": True,
            "NumberOfVACBans": 2,
            "DaysSinceLastBan": 12,
            "NumberOfGameBans": 1,
            "EconomyBan": "none",
        },
    ]
}


# --- Aufgabe 2a: parse_bans ---


def test_parse_bans_gives_one_entry_per_player():
    assert parse_bans(FAKE_RESPONSE) == [
        {"steam_id": "76561198000000001", "vac_bans": 0, "game_bans": 0, "days_since_last_ban": 0},
        {"steam_id": "76561198000000002", "vac_bans": 2, "game_bans": 1, "days_since_last_ban": 12},
    ]


def test_parse_bans_with_no_players_gives_empty_list():
    assert parse_bans({"players": []}) == []


# --- Aufgabe 2b: is_banned ---


def test_player_without_bans_is_not_banned():
    assert is_banned({"steam_id": "1", "vac_bans": 0, "game_bans": 0, "days_since_last_ban": 0}) is False


def test_player_with_vac_ban_is_banned():
    assert is_banned({"steam_id": "1", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 3}) is True


def test_player_with_only_game_ban_is_banned():
    assert is_banned({"steam_id": "1", "vac_bans": 0, "game_bans": 1, "days_since_last_ban": 3}) is True


# --- Already done by Claude: these should be green right away ---


def test_api_key_comes_from_environment(monkeypatch):
    monkeypatch.setenv("STEAM_API_KEY", "test-key")
    assert get_api_key() == "test-key"


def test_missing_api_key_gives_clear_error(monkeypatch):
    monkeypatch.delenv("STEAM_API_KEY", raising=False)
    with pytest.raises(RuntimeError, match="STEAM_API_KEY"):
        get_api_key()


def test_url_contains_key_and_ids():
    url = build_bans_url(["76561198000000001", "76561198000000002"], "test-key")
    assert url.startswith("https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?")
    assert "key=test-key" in url
    assert "steamids=76561198000000001%2C76561198000000002" in url


def test_more_than_100_ids_in_one_url_is_refused():
    with pytest.raises(ValueError):
        build_bans_url([str(i) for i in range(101)], "test-key")


def test_fetch_bans_asks_in_groups_of_100(monkeypatch):
    groups = []

    def fake_fetch(steam_ids, api_key):
        groups.append(len(steam_ids))
        return {"players": []}

    monkeypatch.setattr(steam_api, "fetch_bans_response", fake_fetch)
    monkeypatch.setattr(steam_api, "parse_bans", lambda response: response["players"])
    fetch_bans([str(i) for i in range(250)], "test-key")
    assert groups == [100, 100, 50]
