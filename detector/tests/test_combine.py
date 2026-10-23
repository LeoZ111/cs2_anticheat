import numpy as np
import pandas as pd
import pytest

from detector import combine


def fake_players(n_matches=40, seed=0):
    rng = np.random.default_rng(seed)
    rows = []
    for m in range(n_matches):
        has_cheater = m % 2 == 0
        for p in range(10):
            cheater = has_cheater and p < 2
            row = {"match": f"m{m}", "player": f"p{p}", "cheater": cheater, "match_has_cheater": has_cheater}
            for f in combine.FEATURES:
                row[f] = rng.normal(1.5 if cheater else 0.0, 1.0)
            row["trigger_instant"] = np.nan if p == 9 else row["trigger_instant"]  # missing values happen
            rows.append(row)
    return pd.DataFrame(rows)


def test_unlabelled_players_in_cheater_matches_are_left_out():
    data = combine.labelled(fake_players())
    assert not ((~data["cheater"]) & data["match_has_cheater"]).any()
    assert set(data["label"]) == {0, 1}


def test_folds_keep_matches_together():
    df = fake_players()
    folds = combine.folds_by_match(df["match"], k=5)
    assert df.assign(fold=folds).groupby("match")["fold"].nunique().max() == 1
    assert set(folds) == {0, 1, 2, 3, 4}


def test_logistic_separates_easy_data():
    rng = np.random.default_rng(1)
    x = np.vstack([rng.normal(0, 1, (200, 2)), rng.normal(3, 1, (200, 2))])
    y = np.array([0] * 200 + [1] * 200)
    scores = combine.Logistic().fit(x, y).score(x)
    assert scores[y == 1].mean() > scores[y == 0].mean() + 2


def test_out_of_fold_scores_find_cheaters():
    data = combine.labelled(fake_players())
    scores = combine.out_of_fold_scores(data)
    labels = data["label"].to_numpy()
    assert scores[labels == 1].mean() > scores[labels == 0].mean()


def test_caught_at_zero_false_alarms():
    scores = np.array([0.1, 0.2, 0.3, 0.9, 0.95, 0.25])
    labels = np.array([0, 0, 0, 1, 1, 1])
    caught, false_alarms = combine.caught_at_false_alarm_rate(scores, labels, 0.0)
    assert caught == pytest.approx(2 / 3)
    assert false_alarms == 0
