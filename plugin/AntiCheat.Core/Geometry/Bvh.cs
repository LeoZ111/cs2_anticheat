using System.Numerics;

namespace AntiCheat.Core.Geometry;

/// <summary>
/// Bounding volume hierarchy: a tree of boxes around the map triangles.
/// A sight line only has to test the triangles in boxes it actually passes through,
/// instead of all few hundred thousand triangles of a map.
/// </summary>
public sealed class Bvh
{
    private const int LeafSize = 4;

    // Flat node arrays (faster than objects). A node is a leaf if Count > 0.
    private readonly Vector3[] _min;
    private readonly Vector3[] _max;
    private readonly int[] _left;     // inner node: index of the left child (right child = left + 1)
    private readonly int[] _start;    // leaf: first index into _order
    private readonly int[] _count;    // leaf: number of triangles
    private readonly int[] _order;    // triangle indices, sorted so every leaf owns a range
    private readonly Triangle[] _triangles;
    private int _nodes;

    public int TriangleCount => _triangles.Length;
    public int NodeCount => _nodes;

    public Bvh(IReadOnlyList<Triangle> triangles)
    {
        _triangles = triangles.ToArray();
        int n = _triangles.Length;
        int maxNodes = Math.Max(1, 2 * n);
        _min = new Vector3[maxNodes];
        _max = new Vector3[maxNodes];
        _left = new int[maxNodes];
        _start = new int[maxNodes];
        _count = new int[maxNodes];
        _order = Enumerable.Range(0, n).ToArray();

        var centroids = new Vector3[n];
        var boxMin = new Vector3[n];
        var boxMax = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Triangle t = _triangles[i];
            boxMin[i] = Vector3.Min(t.A, Vector3.Min(t.B, t.C));
            boxMax[i] = Vector3.Max(t.A, Vector3.Max(t.B, t.C));
            centroids[i] = (t.A + t.B + t.C) / 3f;
        }

        _nodes = 1;
        Build(0, 0, n, centroids, boxMin, boxMax);
    }

    private void Build(int node, int start, int count, Vector3[] centroids, Vector3[] boxMin, Vector3[] boxMax)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        Vector3 cMin = new(float.MaxValue), cMax = new(float.MinValue);
        for (int i = start; i < start + count; i++)
        {
            int t = _order[i];
            min = Vector3.Min(min, boxMin[t]);
            max = Vector3.Max(max, boxMax[t]);
            cMin = Vector3.Min(cMin, centroids[t]);
            cMax = Vector3.Max(cMax, centroids[t]);
        }
        _min[node] = min;
        _max[node] = max;

        if (count <= LeafSize)
        {
            MakeLeaf(node, start, count);
            return;
        }

        // Split at the middle of the longest axis of the centroids.
        Vector3 extent = cMax - cMin;
        int axis = extent.X >= extent.Y && extent.X >= extent.Z ? 0 : extent.Y >= extent.Z ? 1 : 2;
        float middle = (Axis(cMin, axis) + Axis(cMax, axis)) / 2f;
        int split = Partition(start, count, centroids, axis, middle);
        if (split == start || split == start + count)
        {
            // All centroids on one side (e.g. identical): split in half by count.
            Array.Sort(_order, start, count, Comparer<int>.Create((a, b) => Axis(centroids[a], axis).CompareTo(Axis(centroids[b], axis))));
            split = start + count / 2;
        }

        int left = _nodes;
        _nodes += 2;
        _left[node] = left;
        _count[node] = 0;
        Build(left, start, split - start, centroids, boxMin, boxMax);
        Build(left + 1, split, start + count - split, centroids, boxMin, boxMax);
    }

    private void MakeLeaf(int node, int start, int count)
    {
        _start[node] = start;
        _count[node] = count;
    }

    private int Partition(int start, int count, Vector3[] centroids, int axis, float middle)
    {
        int i = start, j = start + count - 1;
        while (i <= j)
        {
            if (Axis(centroids[_order[i]], axis) < middle)
            {
                i++;
            }
            else
            {
                (_order[i], _order[j]) = (_order[j], _order[i]);
                j--;
            }
        }
        return i;
    }

    private static float Axis(Vector3 v, int axis) => axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;

    /// <summary>True if any triangle lies between Start and End.</summary>
    public bool IsBlocked(in Segment segment)
    {
        if (_triangles.Length == 0) return false;
        Vector3 origin = segment.Start;
        Vector3 direction = segment.End - segment.Start;
        Vector3 inverse = new(1f / direction.X, 1f / direction.Y, 1f / direction.Z);

        Span<int> stack = stackalloc int[128];
        int top = 0;
        stack[top++] = 0;
        while (top > 0)
        {
            int node = stack[--top];
            if (!HitsBox(origin, inverse, _min[node], _max[node])) continue;
            if (_count[node] > 0)
            {
                for (int i = _start[node]; i < _start[node] + _count[node]; i++)
                {
                    if (Intersection.RayHitsTriangle(origin, direction, _triangles[_order[i]], out float t) && t <= 1f)
                    {
                        return true;
                    }
                }
            }
            else
            {
                stack[top++] = _left[node];
                stack[top++] = _left[node] + 1;
            }
        }
        return false;
    }

    // Slab test for the segment origin + t * direction, t in [0, 1], against an axis-aligned box.
    private static bool HitsBox(Vector3 origin, Vector3 inverse, Vector3 min, Vector3 max)
    {
        Vector3 t1 = (min - origin) * inverse;
        Vector3 t2 = (max - origin) * inverse;
        Vector3 tMin = Vector3.Min(t1, t2);
        Vector3 tMax = Vector3.Max(t1, t2);
        float enter = MathF.Max(MathF.Max(tMin.X, tMin.Y), MathF.Max(tMin.Z, 0f));
        float exit = MathF.Min(MathF.Min(tMax.X, tMax.Y), MathF.Min(tMax.Z, 1f));
        return enter <= exit;
    }
}
