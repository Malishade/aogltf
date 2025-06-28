using System.Numerics;

namespace aogltf
{
    public readonly struct StaticMeshData(Vector3[] vertices, ushort[] indices)
    {
        public Vector3[] Vertices { get; } = vertices ?? throw new ArgumentNullException(nameof(vertices));
        public ushort[] Indices { get; } = indices ?? throw new ArgumentNullException(nameof(indices));
        public Bounds Bounds { get; } = Bounds.FromVertices(vertices);
    }
}
