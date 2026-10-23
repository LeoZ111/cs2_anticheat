"""Feature 5: knowledge the player could not have had (version 0, uses the game's radar visibility).

For every kill, look at the 3 seconds before:
- never_seen: the attacker never had the victim "spotted" before the kill
- hidden_tracking: the crosshair followed the moving, hidden victim for at least 250 ms
Later: replace radar visibility with our own line of sight (map geometry) and add a sound model.
"""

import numpy as np

from detector.angles import angles_to, crosshair_error, wrap
from detector.match import TICKRATE, Match

WINDOW = 3 * TICKRATE
ON_TARGET_DEG = 4.0
TRACKING_MS = 250
MIN_KILLS = 3


def feature(match: Match, p: int, ctx: dict) -> dict:
    deaths = match.event_frame("player_death")
    steamid = match.players[p]
    if deaths.empty:
        return {"never_seen_rate": np.nan, "hidden_tracking_rate": np.nan, "wallbang_rate": np.nan, "kills": 0}
    mine = deaths[(deaths["attacker_steamid"].astype(str) == steamid) & (deaths["user_steamid"].astype(str) != steamid)]
    eye_z = match.eye_z()
    never_seen, tracking, wallbang, headshot = [], [], [], []
    for k in mine.itertuples():
        v = match.index(str(k.user_steamid))
        end = int(k.tick) - match.tick0
        if v is None or end < 1 or end >= match.ticks:
            continue
        sl = slice(max(0, end - WINDOW), end + 1)
        want_p, want_y = angles_to(match.x[v, sl] - match.x[p, sl], match.y[v, sl] - match.y[p, sl], eye_z[v, sl] - eye_z[p, sl])
        err = crosshair_error(match.pitch[p, sl], match.yaw[p, sl], want_p, want_y)
        seen = match.seen[p, v, sl]
        moving = np.abs(wrap(np.diff(want_y, prepend=want_y[0]))) > 0.15
        hidden_on = (~seen) & (err < ON_TARGET_DEG) & moving
        never_seen.append(not seen.any())
        tracking.append(hidden_on.sum() * 1000 / TICKRATE >= TRACKING_MS)
        wallbang.append(bool(getattr(k, "penetrated", 0)))
        headshot.append(bool(getattr(k, "headshot", False)))
    if len(never_seen) < MIN_KILLS:
        return {"never_seen_rate": np.nan, "hidden_tracking_rate": np.nan, "wallbang_rate": np.nan, "kills": len(never_seen)}
    return {
        "never_seen_rate": float(np.mean(never_seen)),
        "hidden_tracking_rate": float(np.mean(tracking)),
        "wallbang_rate": float(np.mean(wallbang)),
        "headshot_rate": float(np.mean(headshot)),
        "kills": len(never_seen),
    }
