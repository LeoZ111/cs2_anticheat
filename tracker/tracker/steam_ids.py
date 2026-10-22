"""Reading Steam IDs from text files."""


def load_steam_ids(filename: str) -> list[str]:
    """Read a text file with one Steam ID per line and return the IDs as a list.

    Spaces and line breaks around an ID are removed. Empty lines are skipped.

    Example file:

        76561197960287930

        76561198000000001

    gives: ["76561197960287930", "76561198000000001"]
    """
    steam_ids = []
    with open(filename) as file:
        for line in file:
            steam_id = line.strip()
            if steam_id:
                steam_ids.append(steam_id)
    return steam_ids
