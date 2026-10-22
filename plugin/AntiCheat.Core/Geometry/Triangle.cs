using System.Numerics;

namespace AntiCheat.Core.Geometry;

/// <summary>A triangle of the map geometry, given by its three corners (CS2 units, Z is up).</summary>
public readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C);
