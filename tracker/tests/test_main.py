from tracker import __main__ as cli


def test_main_prints_summary_and_banned_players(tmp_path, monkeypatch, capsys):
    ids_file = tmp_path / "ids.txt"
    ids_file.write_text("76561198000000001\n76561198000000002\n")
    monkeypatch.setenv("STEAM_API_KEY", "test-key")
    monkeypatch.setattr(cli, "fetch_bans", lambda steam_ids, api_key: [
        {"steam_id": "76561198000000001", "vac_bans": 0, "game_bans": 0, "days_since_last_ban": 0},
        {"steam_id": "76561198000000002", "vac_bans": 1, "game_bans": 0, "days_since_last_ban": 12},
    ])

    assert cli.main([str(ids_file)]) == 0
    output = capsys.readouterr().out
    assert "Checked: 2 players, banned: 1" in output
    assert "76561198000000002: 1 VAC" in output
    assert "76561198000000001" not in output


def test_main_without_file_shows_usage(capsys):
    assert cli.main([]) == 2
    assert "Usage" in capsys.readouterr().out
