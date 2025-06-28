
using System.Numerics;

namespace aogltf
{
    public class ObjectNode
    {
        public MeshData? MeshData { get; set; } // Null if this is just a transform node
        public Vector3 Translation { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;
        public string? Name { get; set; }
        public List<ObjectNode> Children { get; set; } = new();
    }
}
