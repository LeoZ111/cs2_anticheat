"""python -m detector <cs2cd folder> [max matches per set] -> per-player features and how well each separates cheaters."""

import sys
from pathlib import Path

import pandas as pd

from detector.features import auc, compute_all
from detector.match import load_cs2cd

FEATURE_COLUMNS = ["mouse_pull", "silent_aim_p90", "recoil_wobble", "trigger_instant", "never_seen_rate",
                   "hidden_tracking_rate", "wallbang_rate", "headshot_rate"]


def main(argv: list[str]) -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    pd.set_option("display.width", 200)
    root = Path(argv[0])
    limit = int(argv[1]) if len(argv) > 1 else 10_000
    frames = []
    for label_set in ["with_cheater_present", "no_cheater_present"]:
        files = sorted((root / label_set).glob("*.json"), key=lambda f: int(f.stem))[:limit]
        for f in files:
            try:
                frames.append(compute_all(load_cs2cd(f.with_suffix(""))))
                print(f"{label_set}/{f.stem} ok", flush=True)
            except Exception as error:  # report and continue with the next match
                print(f"{label_set}/{f.stem} FAILED: {error}", flush=True)
    df = pd.concat(frames, ignore_index=True)
    out = root.parent / "detector_features.csv"
    df.to_csv(out, index=False)

    cheaters = df[df["cheater"]]
    clean = df[~df["match_has_cheater"]]
    print(f"\n{len(cheaters)} cheaters, {len(clean)} players from clean matches. Per-player table: {out}")
    rows = []
    for col in FEATURE_COLUMNS:
        a = auc(cheaters[col], clean[col])
        rows.append({"feature": col, "cheater_median": cheaters[col].median(), "clean_median": clean[col].median(),
                     "AUC": a, "separation": max(a, 1 - a) if a == a else float("nan"),
                     "players_with_value": int(df[col].notna().sum())})
    print(pd.DataFrame(rows).round(3).to_string(index=False))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
