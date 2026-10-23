"""Feature 4: shots fired the moment the crosshair touches an enemy (triggerbot).

For every first shot (no shot in the ticks before), check whether the crosshair was on an enemy and since when.
Humans need reaction time between "on target" and "click". trigger_instant = share of on-target first shots
fired within 1 tick of the crosshair arriving.
"""

import numpy as np

from detector.features.target import on_target
from detector.match import Match

NOT_GUNS = ("knife", "bayonet", "grenade", "flashbang", "molotov", "incgrenade", "decoy", "c4", "taser", "smoke", "hegrenade")
SPRAY_GAP_TICKS = 10
LOOKBACK_TICKS = 32
MIN_SHOTS = 5
BODY_HALF_WIDTH = 16.0  # shots are fired at the body too, not only the head


def entry_delay(on: np.ndarray, shot: int, lookback: int = LOOKBACK_TICKS) -> int | None:
    """Ticks the crosshair was on target before the shot. None if it was on target for the whole lookback."""
    for back in range(0, lookback + 1):
        i = shot - back
        if i < 0 or not on[i]:
            return back - 1 if back > 0 else None
    return None


def feature(match: Match, p: int, ctx: dict) -> dict:
    fires = match.event_frame("weapon_fire")
    if fires.empty:
        return {"trigger_instant": np.nan, "trigger_shots": 0}
    fires = fires[fires["user_steamid"].astype(str) == match.players[p]]
    fires = fires[~fires["weapon"].astype(str).str.contains("|".join(NOT_GUNS))]
    ticks = np.sort(fires["tick"].astype(int).to_numpy() - match.tick0)
    ticks = ticks[(ticks >= 0) & (ticks < match.ticks)]
    if len(ticks) == 0:
        return {"trigger_instant": np.nan, "trigger_shots": 0}
    first_shots = ticks[np.concatenate([[True], np.diff(ticks) > SPRAY_GAP_TICKS])]

    near = ctx["nearest"]
    on = on_target(near["err"], near["dist"], half_width=BODY_HALF_WIDTH, minimum=1.5)
    delays = [d for s in first_shots if on[s] and (d := entry_delay(on, s)) is not None]
    if len(delays) < MIN_SHOTS:
        return {"trigger_instant": np.nan, "trigger_shots": len(delays)}
    delays = np.array(delays)
    return {"trigger_instant": float(np.mean(delays <= 1)), "trigger_shots": len(delays),
            "trigger_median_ms": float(np.median(delays) * 1000 / 64)}
