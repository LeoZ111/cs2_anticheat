from tracker.steam_ids import load_steam_ids


def write_file(tmp_path, content):
    """Create a temporary text file with the given content and return its path."""
    path = tmp_path / "ids.txt"
    path.write_text(content)
    return str(path)


def test_reads_one_id_per_line(tmp_path):
    filename = write_file(tmp_path, "76561197960287930\n76561198000000001\n76561198000000002\n")
    assert load_steam_ids(filename) == [
        "76561197960287930",
        "76561198000000001",
        "76561198000000002",
    ]


def test_skips_empty_lines(tmp_path):
    filename = write_file(tmp_path, "76561197960287930\n\n\n76561198000000001\n")
    assert load_steam_ids(filename) == ["76561197960287930", "76561198000000001"]


def test_removes_spaces_around_ids(tmp_path):
    filename = write_file(tmp_path, "  76561197960287930  \n\t76561198000000001\n")
    assert load_steam_ids(filename) == ["76561197960287930", "76561198000000001"]


def test_empty_file_gives_empty_list(tmp_path):
    filename = write_file(tmp_path, "")
    assert load_steam_ids(filename) == []
