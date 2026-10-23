"""Combines all features into one score per player and measures how many cheaters are caught at few false alarms.

Model: logistic regression (plain numpy). Evaluation: cross-validation by match, so a model is always scored on
matches it never saw. Positives: labelled cheaters. Negatives: players from matches without any cheater label
(players in cheater matches who are not labelled are left out, their labels are unreliable).

python -m detector.combine <detector_features.csv>
"""

import sys

import numpy as np
import pandas as pd

from detector.features import auc

FEATURES = ["never_seen_rate", "wallbang_rate", "headshot_rate", "hidden_tracking_rate", "silent_aim_p90",
            "recoil_wobble", "trigger_instant", "mouse_pull"]


def labelled(df: pd.DataFrame) -> pd.DataFrame:
    keep = df["cheater"] | ~df["match_has_cheater"]
    out = df[keep].copy()
    out["label"] = out["cheater"].astype(int)
    return out


def design(df: pd.DataFrame, medians: pd.Series, features=FEATURES) -> np.ndarray:
    """Feature matrix: missing values filled with the training median, plus one 'was missing' column per feature."""
    cols = []
    for f in features:
        v = df[f].astype(float)
        cols.append(v.fillna(medians[f]).to_numpy())
        cols.append(v.isna().astype(float).to_numpy())
    return np.column_stack(cols)


class Logistic:
    def __init__(self, l2: float = 1.0, steps: int = 3000, lr: float = 0.1):
        self.l2, self.steps, self.lr = l2, steps, lr

    def fit(self, x: np.ndarray, y: np.ndarray) -> "Logistic":
        self.mean = x.mean(axis=0)
        self.std = x.std(axis=0) + 1e-9
        z = (x - self.mean) / self.std
        w, b = np.zeros(z.shape[1]), 0.0
        for _ in range(self.steps):
            p = 1 / (1 + np.exp(-(z @ w + b)))
            grad = p - y
            w -= self.lr * (z.T @ grad / len(y) + self.l2 * w / len(y))
            b -= self.lr * grad.mean()
        self.w, self.b = w, b
        return self

    def score(self, x: np.ndarray) -> np.ndarray:
        return ((x - self.mean) / self.std) @ self.w + self.b


def folds_by_match(matches: pd.Series, k: int = 5, seed: int = 7) -> np.ndarray:
    """Fold number per row; all players of one match land in the same fold."""
    unique = np.array(matches.unique(), dtype=object)
    rng = np.random.default_rng(seed)
    rng.shuffle(unique)
    fold_of = {m: i % k for i, m in enumerate(unique)}
    return matches.map(fold_of).to_numpy()


def out_of_fold_scores(df: pd.DataFrame, features=FEATURES, k: int = 5) -> np.ndarray:
    folds = folds_by_match(df["match"], k)
    scores = np.zeros(len(df))
    for f in range(k):
        train, test = folds != f, folds == f
        medians = df.loc[train, features].median()
        model = Logistic().fit(design(df[train], medians, features), df.loc[train, "label"].to_numpy())
        scores[test] = model.score(design(df[test], medians, features))
    return scores


def caught_at_false_alarm_rate(scores: np.ndarray, labels: np.ndarray, rate: float) -> tuple[float, int]:
    """Share of cheaters above the threshold that lets at most `rate` of clean players through."""
    clean = np.sort(scores[labels == 0])
    allowed = int(np.floor(rate * len(clean)))
    threshold = clean[len(clean) - 1 - allowed] if allowed < len(clean) else -np.inf
    caught = scores[labels == 1] > threshold
    return float(caught.mean()), int((scores[labels == 0] > threshold).sum())


def report(df: pd.DataFrame) -> pd.DataFrame:
    data = labelled(df)
    labels = data["label"].to_numpy()
    rows = []
    sets = {"alle Merkmale": FEATURES,
            "nur Physik (Silent Aim, Rückstoß, Trigger, Maus)": ["silent_aim_p90", "recoil_wobble", "trigger_instant", "mouse_pull"],
            "nur Wissen": ["never_seen_rate", "wallbang_rate", "hidden_tracking_rate"],
            "ohne Kopfschuss-Quote": [f for f in FEATURES if f != "headshot_rate"]}
    for name, features in sets.items():
        scores = out_of_fold_scores(data, features)
        row = {"Merkmale": name, "AUC": round(auc(scores[labels == 1], scores[labels == 0]), 3)}
        for rate in (0.05, 0.01, 0.0):
            caught, false_alarms = caught_at_false_alarm_rate(scores, labels, rate)
            row[f"erkannt bei {rate:.0%} Fehlalarmen"] = f"{caught:.0%} ({false_alarms} FA)"
        rows.append(row)
    return pd.DataFrame(rows)


def main(argv: list[str]) -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    pd.set_option("display.width", 220)
    df = pd.read_csv(argv[0])
    data = labelled(df)
    print(f"{int(data['label'].sum())} Cheater, {int((data['label'] == 0).sum())} saubere Spieler, "
          f"{data['match'].nunique()} Matches")
    print(report(df).to_string(index=False))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
