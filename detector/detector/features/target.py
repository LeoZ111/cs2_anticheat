"""For every tick: where is the enemy closest to a player's crosshair?"""

import numpy as np

from detector.angles import angles_to, wrap
from detector.match import Match

HEAD_BELOW_EYE = 2.0


def nearest_enemy(match: Match, p: int) -> dict[str, np.ndarray]:
    """Arrays over ticks: error (deg), signed pitch/yaw offset to that enemy, distance, enemy index (-1 = none)."""
    n = match.ticks
    eye_z = match.eye_z()
    best_err = np.full(n, np.inf)
    best = {"dpitch": np.full(n, np.nan), "dyaw": np.full(n, np.nan), "dist": np.full(n, np.nan), "enemy": np.full(n, -1)}
    for q in match.enemies(p):
        dx = match.x[q] - match.x[p]
        dy = match.y[q] - match.y[p]
        dz = (eye_z[q] - HEAD_BELOW_EYE) - eye_z[p]
        want_p, want_y = angles_to(dx, dy, dz)
        dp, dyw = wrap(want_p - match.pitch[p]), wrap(want_y - match.yaw[p])
        err = np.hypot(dp, dyw)
        err = np.where(match.alive[q] & match.alive[p], err, np.inf)
        better = err < best_err
        best_err = np.where(better, err, best_err)
        best["dpitch"] = np.where(better, dp, best["dpitch"])
        best["dyaw"] = np.where(better, dyw, best["dyaw"])
        best["dist"] = np.where(better, np.sqrt(dx * dx + dy * dy + dz * dz), best["dist"])
        best["enemy"] = np.where(better, q, best["enemy"])
    best["err"] = np.where(np.isfinite(best_err), best_err, np.nan)
    return best


def on_target(err: np.ndarray, dist: np.ndarray, half_width: float = 10.0, minimum: float = 1.0) -> np.ndarray:
    """Is the crosshair on the enemy's body? Body half width about 10 units, so the allowed angle shrinks with distance."""
    allowed = np.maximum(minimum, np.degrees(np.arctan2(half_width, np.maximum(dist, 1.0))))
    return np.nan_to_num(err, nan=np.inf) < allowed
