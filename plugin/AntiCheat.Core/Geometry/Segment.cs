using System.Numerics;

namespace AntiCheat.Core.Geometry;

/// <summary>A straight line from Start to End, e.g. from a player's eye to a point on an enemy's hitbox.</summary>
public readonly record struct Segment(Vector3 Start, Vector3 End);
