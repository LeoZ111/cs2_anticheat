namespace AntiCheat.Core.Geometry;

/// <summary>
/// Is there a wall between two points? First version: tests every triangle.
/// Later a BVH (a tree of boxes around the triangles) will skip most of them.
/// </summary>
public static class LineOfSight
{
    public static bool IsBlocked(in Segment segment, IReadOnlyList<Triangle> triangles)
    {
        for (int i = 0; i < triangles.Count; i++)
        {
            if (Intersection.SegmentHitsTriangle(segment, triangles[i]))
            {
                return true;
            }
        }
        return false;
    }
}
