using System.Numerics;

namespace aogltf
{
    public class StaticMeshData
    {
        public Vector3[] Vertices { get; private set; } 
        public ushort[] Indices { get; private set; }
        public Bounds Bounds { get; private set; }

        public StaticMeshData (Vector3[] vertices, ushort[] indices)
        {
            Vertices = vertices;
            Indices = indices;
            Bounds = Bounds.FromVertices(vertices);
        }
    }
}
