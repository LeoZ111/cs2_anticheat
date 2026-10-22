"""Asking the Steam Web API which players are banned."""

import json
import os
import urllib.parse
import urllib.request

BANS_URL = "https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/"

# The API accepts at most 100 Steam IDs per request.
MAX_IDS_PER_REQUEST = 100


def get_api_key() -> str:
    """Return the Steam API key from the environment variable STEAM_API_KEY."""
    key = os.environ.get("STEAM_API_KEY", "").strip()
    if not key:
        raise RuntimeError("STEAM_API_KEY is not set. See tracker/README.md, Aufgabe 2.")
    return key


def build_bans_url(steam_ids: list[str], api_key: str) -> str:
    """Build the request URL for up to 100 Steam IDs."""
    if len(steam_ids) > MAX_IDS_PER_REQUEST:
        raise ValueError(f"At most {MAX_IDS_PER_REQUEST} Steam IDs per request.")
    query = urllib.parse.urlencode({"key": api_key, "steamids": ",".join(steam_ids)})
    return f"{BANS_URL}?{query}"


def fetch_bans_response(steam_ids: list[str], api_key: str) -> dict:
    """Ask the Steam API about up to 100 players and return the answer as a dict."""
    with urllib.request.urlopen(build_bans_url(steam_ids, api_key), timeout=10) as response:
        return json.load(response)


def parse_bans(response: dict) -> list[dict]:
    """Turn the Steam API answer into a simple list, one dict per player.

    The API answers like this (shortened):

        {"players": [{"SteamId": "76561198000000001", "VACBanned": true,
                      "NumberOfVACBans": 1, "NumberOfGameBans": 0, "DaysSinceLastBan": 12}]}

    gives:

        [{"steam_id": "76561198000000001", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 12}]
    """
    players = []
    for player in response["players"]:
        players.append({
            "steam_id": player["SteamId"],
            "vac_bans": player["NumberOfVACBans"],
            "game_bans": player["NumberOfGameBans"],
            "days_since_last_ban": player["DaysSinceLastBan"],
        })
    return players


def is_banned(player: dict) -> bool:
    """Return True if the player has at least one VAC ban or game ban."""
    return player["vac_bans"] > 0 or player["game_bans"] > 0


def fetch_bans(steam_ids: list[str], api_key: str) -> list[dict]:
    """Ask the Steam API about any number of players, in groups of 100."""
    players = []
    for start in range(0, len(steam_ids), MAX_IDS_PER_REQUEST):
        group = steam_ids[start:start + MAX_IDS_PER_REQUEST]
        players.extend(parse_bans(fetch_bans_response(group, api_key)))
    return players
