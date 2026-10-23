"""Angle helpers. CS2 convention: pitch positive = looking down, yaw 0 = +X, 90 = +Y. All in degrees."""

import numpy as np


def wrap(d):
    """Angle difference into -180..180."""
    return (np.asarray(d, dtype=float) + 180.0) % 360.0 - 180.0


def angles_to(dx, dy, dz):
    """Pitch and yaw that look along the vector (dx, dy, dz)."""
    dx, dy, dz = (np.asarray(v, dtype=float) for v in (dx, dy, dz))
    return -np.degrees(np.arctan2(dz, np.hypot(dx, dy))), np.degrees(np.arctan2(dy, dx))


def view_vector(pitch, yaw):
    """Unit vector the player looks along."""
    p, y = np.radians(np.asarray(pitch, dtype=float)), np.radians(np.asarray(yaw, dtype=float))
    return np.stack([np.cos(p) * np.cos(y), np.cos(p) * np.sin(y), -np.sin(p)], axis=-1)


def angle_between(a, b):
    """Angle in degrees between vectors a and b (last axis = xyz)."""
    a, b = np.asarray(a, dtype=float), np.asarray(b, dtype=float)
    cos = np.sum(a * b, axis=-1) / (np.linalg.norm(a, axis=-1) * np.linalg.norm(b, axis=-1))
    return np.degrees(np.arccos(np.clip(cos, -1.0, 1.0)))


def crosshair_error(pitch, yaw, want_pitch, want_yaw):
    """How many degrees the crosshair is away from the wanted direction."""
    return np.hypot(wrap(want_pitch - pitch), wrap(want_yaw - yaw))
