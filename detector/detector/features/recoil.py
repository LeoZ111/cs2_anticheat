"""Feature 3: recoil control that is too perfect (no-recoil).

While spraying, the weapon kicks up (aim punch). A human pulls the mouse down to compensate, never perfectly.
The real aim direction during a spray is pitch + 2 x punch. If it barely moves although the recoil is large,
the compensation is too perfect. recoil_wobble = median wobble of the real aim over all sprays (degrees, lower = more perfect).
"""

import numpy as np

from detector.match import Match

RECOIL_SCALE = 2.0
MIN_SPRAY_TICKS = 12
MIN_RECOIL_DEG = 2.0  # only sprays where the weapon really kicked


def spray_segments(shots_fired: np.ndarray, alive: np.ndarray, min_shots: int = 4) -> list[tuple[int, int]]:
    """Continuous tick ranges where the player is in a spray of at least min_shots bullets."""
    active = (np.nan_to_num(shots_fired) >= min_shots) & alive
    segments, start = [], None
    for i, on in enumerate(active):
        if on and start is None:
            start = i
        elif not on and start is not None:
            segments.append((start, i))
            start = None
    if start is not None:
        segments.append((start, len(active)))
    return segments


def feature(match: Match, p: int, ctx: dict) -> dict:
    wobbles = []
    for a, b in spray_segments(match.shots_fired[p], match.alive[p]):
        if b - a < MIN_SPRAY_TICKS:
            continue
        punch = match.punch_pitch[p, a:b]
        if np.nanmax(punch) - np.nanmin(punch) < MIN_RECOIL_DEG / RECOIL_SCALE:
            continue
        real = match.pitch[p, a:b] + RECOIL_SCALE * punch
        wobbles.append(float(np.nanstd(real)))
    if len(wobbles) < 3:
        return {"recoil_wobble": np.nan, "recoil_sprays": len(wobbles)}
    return {"recoil_wobble": float(np.median(wobbles)), "recoil_sprays": len(wobbles)}
