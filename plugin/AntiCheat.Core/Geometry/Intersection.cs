using System.Numerics;

namespace AntiCheat.Core.Geometry;

/// <summary>Ray and segment tests against triangles.</summary>
public static class Intersection
{
    // Below this, the ray counts as parallel to the triangle.
    private const float ParallelEpsilon = 1e-8f;

    /// <summary>
    /// Möller–Trumbore ray-triangle test. Both sides of the triangle count as hits.
    /// </summary>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Where the ray goes. Does not need to be normalized.</param>
    /// <param name="triangle">The triangle to test.</param>
    /// <param name="t">On a hit: the hit point is origin + t * direction, with t &gt;= 0.</param>
    public static bool RayHitsTriangle(Vector3 origin, Vector3 direction, in Triangle triangle, out float t)
    {
        t = 0f;
        Vector3 edge1 = triangle.B - triangle.A;
        Vector3 edge2 = triangle.C - triangle.A;

        Vector3 p = Vector3.Cross(direction, edge2);
        float determinant = Vector3.Dot(edge1, p);
        if (MathF.Abs(determinant) < ParallelEpsilon)
        {
            return false;
        }

        float inverseDeterminant = 1f / determinant;
        Vector3 fromA = origin - triangle.A;

        float u = Vector3.Dot(fromA, p) * inverseDeterminant;
        if (u < 0f || u > 1f)
        {
            return false;
        }

        Vector3 q = Vector3.Cross(fromA, edge1);
        float v = Vector3.Dot(direction, q) * inverseDeterminant;
        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        t = Vector3.Dot(edge2, q) * inverseDeterminant;
        return t >= 0f;
    }

    /// <summary>True if the segment crosses the triangle somewhere between Start and End.</summary>
    public static bool SegmentHitsTriangle(in Segment segment, in Triangle triangle)
    {
        return RayHitsTriangle(segment.Start, segment.End - segment.Start, triangle, out float t) && t <= 1f;
    }
}
