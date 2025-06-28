using System.Numerics;

namespace aogltf
{
    public readonly struct Bounds(Vector3 min, Vector3 max)
    {
        public Vector3 Min { get; } = min;
        public Vector3 Max { get; } = max;

        public static Bounds FromVertices(ReadOnlySpan<Vector3> vertices)
        {
            if (vertices.Length == 0)
                return new Bounds(Vector3.Zero, Vector3.Zero);

            Vector3 min = vertices[0];
            Vector3 max = vertices[0];

            for (int i = 1; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                if (vertex.X < min.X) min.X = vertex.X;
                if (vertex.Y < min.Y) min.Y = vertex.Y;
                if (vertex.Z < min.Z) min.Z = vertex.Z;

                if (vertex.X > max.X) max.X = vertex.X;
                if (vertex.Y > max.Y) max.Y = vertex.Y;
                if (vertex.Z > max.Z) max.Z = vertex.Z;
            }

            return new Bounds(min, max);
        }
    }
}