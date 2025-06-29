using System.Numerics;

namespace aogltf
{
    public class MaterialData
    {
        public string? Name { get; set; } = "Default Material";
        public Vector4? BaseColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);
        public float MetallicFactor { get; set; } = 0.0f;
        public float RoughnessFactor { get; set; } = 1.0f;
        public Vector3? EmissiveFactor { get; set; } = Vector3.Zero;
        public int? BaseColorTextureIndex { get; set; }
        public int? NormalTextureIndex { get; set; }
        public string? AlphaMode { get; set; } = "OPAQUE";
        public float? AlphaCutoff { get; set; }
        public bool DoubleSided { get; set; } = false;

        public static MaterialData FromGltfMaterial(Material gltfMaterial)
        {
            return new MaterialData
            {
                Name = gltfMaterial.Name,
                BaseColor = gltfMaterial.PbrMetallicRoughness?.BaseColorFactor != null ? new Vector4(gltfMaterial.PbrMetallicRoughness.BaseColorFactor) : null,
                MetallicFactor = gltfMaterial.PbrMetallicRoughness?.MetallicFactor ?? 0.0f,
                RoughnessFactor = gltfMaterial.PbrMetallicRoughness?.RoughnessFactor ?? 1.0f,
                EmissiveFactor = gltfMaterial.EmissiveFactor != null ? new Vector3(gltfMaterial.EmissiveFactor) : null,
                BaseColorTextureIndex = gltfMaterial.PbrMetallicRoughness?.BaseColorTexture?.Index,
                NormalTextureIndex = gltfMaterial.NormalTexture?.Index,
                AlphaMode = gltfMaterial.AlphaMode ?? "OPAQUE",
                AlphaCutoff = gltfMaterial.AlphaCutoff,
                DoubleSided = gltfMaterial.DoubleSided ?? false
            };
        }
    }

    public class NodeData
    {
        public string Name { get; set; } = string.Empty;
        public Vector3? Translation { get; set; } = null;
        public Quaternion? Rotation { get; set; } = null;
        public Vector3? Scale { get; set; } = null;
        public List<int> ChildIndices { get; set; } = new();
        public int? MeshIndex { get; set; }
        public int? SourceMeshIndex { get; set; }
    }

    public class MeshData
    {
        public Vector3[] Vertices { get; private set; }
        public Vector3[] Normals { get; private set; }
        public Vector2[] UVs { get; private set; }
        public ushort[] Indices { get; private set; }
        public Bounds Bounds { get; private set; }
        public int? MaterialIndex { get; set; }
        public int? SourceMeshIndex { get; set; }

        public MeshData(Vector3[] vertices, ushort[] indices, Vector3[] normals, Vector2[] uvs, int? materialIndex = null)
        {
            Vertices = vertices;
            Indices = indices;
            Bounds = Bounds.FromVertices(vertices);
            Normals = normals;
            UVs = uvs;
            MaterialIndex = materialIndex;
        }
    }

    public class SceneData
    {
        public List<NodeData> Nodes { get; set; } = new();
        public List<MeshData> Meshes { get; set; } = new();
        public List<MaterialData> Materials { get; set; } = new();
        public int RootNodeIndex { get; set; } = 0;

        public NodeData? GetNode(int index) => index >= 0 && index < Nodes.Count ? Nodes[index] : null;
        public MeshData? GetMesh(int index) => index >= 0 && index < Meshes.Count ? Meshes[index] : null;
        public MaterialData? GetMaterial(int index) => index >= 0 && index < Materials.Count ? Materials[index] : null;
    }
}
