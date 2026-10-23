import numpy as np
import pandas as pd
import pytest

from detector.angles import angles_to, wrap
from detector.features import auc, compute_all
from detector.features import mouse_pull, recoil, trigger
from detector.features.target import nearest_enemy, on_target
from detector.match import from_frames

GAIN = -0.022  # degrees per mouse count (yaw goes down when the mouse goes right)


def make_match(attacker_yaw, mouse_dx, enemy_yaw_deg=0.0, distance=500.0, n=None, events=None):
    """Attacker at the origin, one enemy at distance in direction enemy_yaw_deg (same height)."""
    n = n or len(attacker_yaw)
    ex = distance * np.cos(np.radians(enemy_yaw_deg))
    ey = distance * np.sin(np.radians(enemy_yaw_deg))
    rows = []
    for t in range(n):
        rows.append({"tick": t, "steamid": "A", "X": 0.0, "Y": 0.0, "Z": 0.0, "pitch": 0.0, "yaw": attacker_yaw[t],
                     "duck_amount": 0.0, "is_alive": True, "team_num": 2, "usercmd_mouse_dx": mouse_dx[t],
                     "usercmd_mouse_dy": 0.0, "aim_punch_angle": [0.0, 0.0, 0.0], "shots_fired": 0,
                     "approximate_spotted_by": []})
        rows.append({"tick": t, "steamid": "E", "X": ex, "Y": ey, "Z": 0.0, "pitch": 0.0, "yaw": 180.0,
                     "duck_amount": 0.0, "is_alive": True, "team_num": 3, "usercmd_mouse_dx": 0.0,
                     "usercmd_mouse_dy": 0.0, "aim_punch_angle": [0.0, 0.0, 0.0], "shots_fired": 0,
                     "approximate_spotted_by": []})
    return from_frames("test", pd.DataFrame(rows), events or {}, cheaters=set())


def test_wrap_and_angles():
    assert wrap(190) == pytest.approx(-170)
    p, y = angles_to(100, 0, 100)
    assert p == pytest.approx(-45)
    assert y == pytest.approx(0)


def test_nearest_enemy_finds_the_offset():
    m = make_match([0.0] * 10, [0.0] * 10, enemy_yaw_deg=5.0)
    near = nearest_enemy(m, 0)
    assert near["dyaw"][0] == pytest.approx(5.0, abs=0.01)
    assert near["enemy"][0] == 1


def test_on_target_depends_on_distance():
    assert on_target(np.array([0.5]), np.array([1000.0]))[0]
    assert not on_target(np.array([3.0]), np.array([1000.0]))[0]
    assert on_target(np.array([3.0]), np.array([100.0]))[0]


def test_fit_gain_recovers_sensitivity():
    rng = np.random.default_rng(1)
    mouse = rng.integers(-40, 40, 2000).astype(float)
    turn = GAIN * mouse + rng.normal(0, 0.02, 2000)
    assert mouse_pull.fit_gain(turn, mouse) == pytest.approx(GAIN, rel=0.05)


def test_pull_score_signs():
    toward = np.array([1.0, -1.0, 1.0, -1.0])
    assert mouse_pull.pull_score(np.array([0.2, -0.2, 0.2, -0.2]), toward) == pytest.approx(1.0)
    assert mouse_pull.pull_score(np.array([-0.2, 0.2, -0.2, 0.2]), toward) == pytest.approx(-1.0)


def simulate_player(aimbot: bool, n=3000, seed=3):
    """Crosshair wanders around an enemy at yaw 0. Every turn comes from the mouse, plus noise.
    The aimbot adds a small pull toward the enemy that the mouse did not make."""
    rng = np.random.default_rng(seed)
    yaw = np.zeros(n)
    mouse = np.zeros(n)
    yaw[0] = 6.0
    for t in range(1, n):
        # the player keeps the crosshair near the enemy with the mouse (fully explained by the mouse)
        mouse[t] = rng.integers(-30, 31) + (40 if yaw[t - 1] > 8 else -40 if yaw[t - 1] < -8 else 0)
        step = GAIN * mouse[t] + rng.normal(0, 0.05)
        if aimbot:
            step += -0.15 * np.sign(yaw[t - 1])  # pull toward yaw 0 that the mouse did not make
        yaw[t] = yaw[t - 1] + step
    return make_match(yaw, mouse)


def test_mouse_pull_is_near_zero_for_honest_and_high_for_aimbot():
    honest = mouse_pull.feature(m := simulate_player(False), 0, {"nearest": nearest_enemy(m, 0)})
    bot = mouse_pull.feature(m := simulate_player(True), 0, {"nearest": nearest_enemy(m, 0)})
    assert abs(honest["mouse_pull"]) < 0.15
    assert bot["mouse_pull"] > 0.4


def test_spray_segments():
    shots = np.array([0, 1, 2, 4, 5, 6, 0, 4, 4, 0], dtype=float)
    alive = np.ones(10, dtype=bool)
    assert recoil.spray_segments(shots, alive) == [(3, 6), (7, 9)]


def test_entry_delay():
    on = np.array([False, False, True, True, True, False], dtype=bool)
    assert trigger.entry_delay(on, 2) == 0   # shot in the first tick on target
    assert trigger.entry_delay(on, 4) == 2   # on target for two ticks before
    assert trigger.entry_delay(np.ones(50, dtype=bool), 40) is None


def test_trigger_instant_for_a_triggerbot():
    # crosshair sweeps across the enemy at yaw 0; the "bot" fires the first tick it is on target
    n = 600
    yaw = np.full(n, 20.0)
    fires = []
    for start in range(10, n - 40, 50):
        for i in range(30):
            yaw[start + i] = 15.0 - i  # passes 0 at i = 15
        fires.append({"tick": start + 15, "user_steamid": "A", "weapon": "weapon_ak47"})
    m = make_match(yaw, np.zeros(n), events={"weapon_fire": fires})
    result = trigger.feature(m, 0, {"nearest": nearest_enemy(m, 0)})
    assert result["trigger_instant"] == pytest.approx(1.0)


def test_compute_all_returns_one_row_per_player():
    df = compute_all(make_match([0.0] * 20, [0.0] * 20))
    assert list(df["player"]) == ["A", "E"]


def test_auc():
    assert auc([3, 4, 5], [0, 1, 2]) == pytest.approx(1.0)
    assert auc([0, 1], [0, 1]) == pytest.approx(0.5)
