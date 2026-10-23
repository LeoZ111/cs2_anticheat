"""Per-player features. Each feature module has one function feature(match, p, ctx) -> dict.

INTERNAL: never publish these features or their thresholds.
"""

import numpy as np
import pandas as pd

from detector.features import knowledge, mouse_pull, recoil, silent_aim, trigger
from detector.features.target import nearest_enemy
from detector.match import Match

FEATURES = [mouse_pull, silent_aim, recoil, trigger, knowledge]


def compute_all(match: Match) -> pd.DataFrame:
    rows = []
    for p, steamid in enumerate(match.players):
        ctx = {"nearest": nearest_enemy(match, p)}
        row = {"match": match.name, "player": steamid, "cheater": steamid in match.cheaters,
               "match_has_cheater": bool(match.cheaters), "map": match.info.get("map"), "rank": match.info.get("avg_rank")}
        for module in FEATURES:
            row.update(module.feature(match, p, ctx))
        rows.append(row)
    return pd.DataFrame(rows)


def auc(positive, negative) -> float:
    """Chance that a random cheater has a higher value than a random clean player. 0.5 = no signal."""
    pos = np.asarray(positive, float)
    neg = np.asarray(negative, float)
    pos, neg = pos[~np.isnan(pos)], neg[~np.isnan(neg)]
    if len(pos) == 0 or len(neg) == 0:
        return float("nan")
    ranks = pd.Series(np.concatenate([pos, neg])).rank().to_numpy()
    return float((ranks[: len(pos)].sum() - len(pos) * (len(pos) + 1) / 2) / (len(pos) * len(neg)))
