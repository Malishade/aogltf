using System.Numerics;

namespace aogltf
{
    public class MeshData
    {
        public Vector3[] Vertices { get; private set; }

        public Vector3[] Normals { get; private set; }

        public Vector2[] UVs { get; private set; }

        public ushort[] Indices { get; private set; }

        public Bounds Bounds { get; private set; }

        public MeshData(Vector3[] vertices, ushort[] indices, Vector3[] normals, Vector2[] uvs)
        {
            Vertices = vertices;
            Indices = indices;
            Bounds = Bounds.FromVertices(vertices);
            Normals = normals;
            UVs = uvs;
        }

        public MeshData(Vector3[] vertices, ushort[] indices, Vector3[] normals)
        {
            Vertices = vertices;
            Indices = indices;
            Bounds = Bounds.FromVertices(vertices);
            Normals = normals;
        }

        public MeshData(Vector3[] vertices, ushort[] indices)
        {
            Vertices = vertices;
            Indices = indices;
            Bounds = Bounds.FromVertices(vertices);
        }
    }
}
