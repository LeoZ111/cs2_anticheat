"""Loads one match into per-player, per-tick arrays (grids), so features can work with plain numpy.

Supported format: CS2CD (<n>.json with events + <n>.parquet with ticks), which is the same as demoparser2 output.
"""

import json
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
import pandas as pd

TICKRATE = 64
EYE_HEIGHT = 64.0
DUCK_DROP = 18.0

TICK_COLUMNS = [
    "tick", "steamid", "X", "Y", "Z", "pitch", "yaw", "duck_amount", "is_alive", "team_num",
    "usercmd_mouse_dx", "usercmd_mouse_dy", "aim_punch_angle", "shots_fired", "approximate_spotted_by",
]


@dataclass
class Match:
    name: str
    players: list[str]
    cheaters: set[str]
    tick0: int
    # grids: [player, tick]
    x: np.ndarray
    y: np.ndarray
    z: np.ndarray
    pitch: np.ndarray
    yaw: np.ndarray
    duck: np.ndarray
    alive: np.ndarray
    team: np.ndarray
    mouse_dx: np.ndarray
    mouse_dy: np.ndarray
    punch_pitch: np.ndarray
    punch_yaw: np.ndarray
    shots_fired: np.ndarray
    # seen[a, v, tick]: player a is listed as having spotted player v (game's radar visibility)
    seen: np.ndarray
    events: dict = field(default_factory=dict)
    info: dict = field(default_factory=dict)

    @property
    def ticks(self) -> int:
        return self.x.shape[1]

    def index(self, steamid: str) -> int | None:
        try:
            return self.players.index(steamid)
        except ValueError:
            return None

    def eye_z(self) -> np.ndarray:
        return self.z + EYE_HEIGHT - DUCK_DROP * np.nan_to_num(self.duck)

    def enemies(self, p: int) -> list[int]:
        team = np.nanmax(self.team[p]) if np.isfinite(self.team[p]).any() else np.nan
        return [q for q in range(len(self.players)) if q != p and np.nanmax(self.team[q]) != team]

    def event_frame(self, name: str) -> pd.DataFrame:
        return pd.DataFrame(self.events.get(name, []))


def load_cs2cd(base: Path) -> Match:
    """base = path without suffix, e.g. data/cs2cd/with_cheater_present/0"""
    base = Path(base)
    meta = json.load(open(base.with_suffix(".json"), encoding="utf-8"))
    t = pd.read_parquet(base.with_suffix(".parquet"), columns=TICK_COLUMNS)
    t = t.dropna(subset=["X", "pitch"])
    return from_frames(
        name=f"{base.parent.name}/{base.name}",
        ticks=t,
        events=meta,
        cheaters={c["steamid"] for c in meta.get("cheaters", [])},
        info=(meta.get("CSstats_info") or [{}])[0],
    )


def from_frames(name: str, ticks: pd.DataFrame, events: dict, cheaters: set[str], info: dict | None = None) -> Match:
    players = sorted(ticks["steamid"].astype(str).unique())
    idx = {p: i for i, p in enumerate(players)}
    tick0 = int(ticks["tick"].min())
    n = int(ticks["tick"].max()) - tick0 + 1
    rows = ticks["steamid"].astype(str).map(idx).to_numpy()
    cols = (ticks["tick"] - tick0).to_numpy().astype(int)

    def grid(values, fill=np.nan):
        g = np.full((len(players), n), fill, dtype=float)
        g[rows, cols] = values
        return g

    punch = ticks["aim_punch_angle"] if "aim_punch_angle" in ticks else pd.Series([None] * len(ticks))
    punch_p = punch.map(lambda v: float(v[0]) if v is not None and len(v) > 0 else 0.0).to_numpy()
    punch_y = punch.map(lambda v: float(v[1]) if v is not None and len(v) > 1 else 0.0).to_numpy()

    seen = np.zeros((len(players), len(players), n), dtype=bool)
    if "approximate_spotted_by" in ticks:
        for r, c, sb in zip(rows, cols, ticks["approximate_spotted_by"]):
            if sb is None or len(sb) == 0:
                continue
            for a in sb:
                a = idx.get(str(a))
                if a is not None:
                    seen[a, r, c] = True

    def col(name, default=np.nan):
        return ticks[name].to_numpy(dtype=float) if name in ticks else np.full(len(ticks), default)

    return Match(
        name=name, players=players, cheaters=set(cheaters), tick0=tick0,
        x=grid(col("X")), y=grid(col("Y")), z=grid(col("Z")),
        pitch=grid(col("pitch")), yaw=grid(col("yaw")), duck=grid(col("duck_amount", 0.0), 0.0),
        alive=grid(col("is_alive", 1.0), 0.0) > 0.5, team=grid(col("team_num")),
        mouse_dx=grid(col("usercmd_mouse_dx")), mouse_dy=grid(col("usercmd_mouse_dy")),
        punch_pitch=grid(punch_p, 0.0), punch_yaw=grid(punch_y, 0.0),
        shots_fired=grid(col("shots_fired", 0.0), 0.0),
        seen=seen, events=events, info=info or {},
    )
