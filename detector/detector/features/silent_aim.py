"""Feature 2: hits that did not fly where the crosshair pointed (silent aim).

For every hit (bullet_damage), compare the bullet direction with where the attacker aimed,
including recoil (view + 2 x aim punch). Spread and recoil make a few degrees normal; much more is impossible.
"""

import numpy as np

from detector.angles import angle_between, view_vector
from detector.match import Match

RECOIL_SCALE = 2.0
MIN_HITS = 5


def feature(match: Match, p: int, ctx: dict) -> dict:
    hits = match.event_frame("bullet_damage")
    steamid = match.players[p]
    if hits.empty or "damage_dir_x" not in hits:
        return {"silent_aim_p90": np.nan, "silent_aim_hits": 0}
    hits = hits[hits["attacker_steamid"].astype(str) == steamid]
    ticks = hits["tick"].astype(int).to_numpy() - match.tick0
    ticks = ticks[(ticks >= 0) & (ticks < match.ticks)]
    if len(ticks) < MIN_HITS:
        return {"silent_aim_p90": np.nan, "silent_aim_hits": len(ticks)}
    hits = hits.iloc[: len(ticks)]
    aim = view_vector(match.pitch[p, ticks] + RECOIL_SCALE * match.punch_pitch[p, ticks],
                      match.yaw[p, ticks] + RECOIL_SCALE * match.punch_yaw[p, ticks])
    bullet = hits[["damage_dir_x", "damage_dir_y", "damage_dir_z"]].to_numpy(dtype=float)
    deviation = angle_between(aim, bullet)
    return {"silent_aim_p90": float(np.nanpercentile(deviation, 90)), "silent_aim_hits": len(ticks)}
