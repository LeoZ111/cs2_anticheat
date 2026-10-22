"""Check a file of Steam IDs for bans: python -m tracker data/ids.txt"""

import sys

from tracker.steam_api import fetch_bans, get_api_key, is_banned
from tracker.steam_ids import load_steam_ids


def main(argv: list[str]) -> int:
    if len(argv) != 1:
        print("Usage: python -m tracker <file with one Steam ID per line>")
        return 2
    steam_ids = load_steam_ids(argv[0])
    players = fetch_bans(steam_ids, get_api_key())
    banned = [player for player in players if is_banned(player)]
    print(f"Checked: {len(players)} players, banned: {len(banned)}")
    for player in banned:
        print(
            f"  {player['steam_id']}: {player['vac_bans']} VAC, {player['game_bans']} game, "
            f"last ban {player['days_since_last_ban']} days ago"
        )
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
