"""Feature 1: turns the mouse did not cause, and whether they pull toward the enemy.

For an honest player: view change = mouse movement x sensitivity (+ noise from how the demo samples input).
The noise is random. An aimbot adds turns without mouse movement that point toward the target.
mouse_pull = share of the unexplained turning that points toward the nearest enemy (-1..1, honest about 0).
"""

import numpy as np

from detector.angles import wrap
from detector.match import Match

NEAR_DEG = 10.0  # only ticks where an enemy is near the crosshair
MIN_TICKS = 200


def fit_gain(turn: np.ndarray, mouse: np.ndarray) -> float:
    """Degrees per mouse count (sensitivity x m_yaw), as a robust median over ticks with clear movement."""
    ok = np.isfinite(turn) & np.isfinite(mouse) & (np.abs(mouse) >= 3) & (np.abs(turn) < 15)
    if ok.sum() < 50:
        return float("nan")
    return float(np.median(turn[ok] / mouse[ok]))


def pull_score(residual: np.ndarray, toward: np.ndarray) -> float:
    """Share of the residual turning that goes toward the target. toward = signed angle to the target."""
    ok = np.isfinite(residual) & np.isfinite(toward) & (toward != 0)
    total = np.sum(np.abs(residual[ok]))
    if total == 0:
        return float("nan")
    return float(np.sum(residual[ok] * np.sign(toward[ok])) / total)


def feature(match: Match, p: int, ctx: dict) -> dict:
    alive = match.alive[p]
    turn_yaw = wrap(np.diff(match.yaw[p], prepend=np.nan))
    turn_pitch = wrap(np.diff(match.pitch[p], prepend=np.nan))
    both_alive = alive & np.roll(alive, 1)
    turn_yaw = np.where(both_alive, turn_yaw, np.nan)
    turn_pitch = np.where(both_alive, turn_pitch, np.nan)

    gain_yaw = fit_gain(turn_yaw, match.mouse_dx[p])
    gain_pitch = fit_gain(turn_pitch, match.mouse_dy[p])
    if np.isnan(gain_yaw):
        return {"mouse_pull": np.nan, "mouse_pull_ticks": 0}

    res_yaw = turn_yaw - gain_yaw * match.mouse_dx[p]
    # Players hardly move the mouse up/down; without enough vertical movement we only use yaw.
    res_pitch = turn_pitch - gain_pitch * match.mouse_dy[p] if np.isfinite(gain_pitch) else np.full_like(turn_pitch, np.nan)

    near = ctx["nearest"]
    # direction to the target *before* this tick's turn
    toward_yaw = np.roll(near["dyaw"], 1)
    toward_pitch = np.roll(near["dpitch"], 1)
    selected = np.roll(np.nan_to_num(near["err"], nan=np.inf), 1)
    selected = (selected < NEAR_DEG) & (selected > 0.3)
    if selected.sum() < MIN_TICKS:
        return {"mouse_pull": np.nan, "mouse_pull_ticks": int(selected.sum())}

    residual = np.concatenate([res_yaw[selected], res_pitch[selected]])
    toward = np.concatenate([toward_yaw[selected], toward_pitch[selected]])
    return {"mouse_pull": pull_score(residual, toward), "mouse_pull_ticks": int(selected.sum())}
